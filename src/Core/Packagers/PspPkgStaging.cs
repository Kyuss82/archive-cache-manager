/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * High-level orchestrator that turns a PSN-style archive (.zip/.7z/.rar) carrying
 * one or more PSP .pkg files (+ optional .rap licence files) into a PPSSPP-bootable
 * memstick-style folder inside the plugin cache.
 *
 * Output layout (mirrors PPSSPP's expected memstick structure):
 *   <outputBaseDir>/<baseName>/PSP/GAME/<TITLE_ID>/EBOOT.PBP
 *                                                  PARAM.SFO
 *                                                  ICON0.PNG  (etc.)
 *   <outputBaseDir>/<baseName>/PSP/LICENSE/<content_id>.rap
 *
 * Pipeline (parallels Ps3PkgStaging):
 *   1. Extract source archive to a private temp dir via the existing Zip helper.
 *   2. Enumerate every .pkg + .rap inside; reject anything that isn't a retail
 *      PSP/PSV PKG (pkg_type=0x0002), since the PS3 flow handles 0x0001.
 *   3. For each PKG, derive the TITLE_ID from the content_id (chars 7..15) and
 *      drive PsPkgUnpacker into PSP/GAME/<TITLE_ID>/. Updates and DLC merge over
 *      the same TITLE_ID — install order is base → patch → DLC by content_type.
 *   4. RAP files land under <install>/PSP/LICENSE/ and, when the user has
 *      configured Config.PspPpssppLicensePath, are also copied into PPSSPP's real
 *      memstick LICENSE folder so the NPDRM EBOOT.PBP decrypts at runtime.
 *
 * Note: the EBOOT.PBP that comes out of a PSP PKG is NPDRM-encrypted. PPSSPP
 * decrypts it on the fly using the .rap, so we do not (and cannot, without per-
 * console keys) decrypt the EBOOT itself here.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ArchiveCacheManager
{
    public class PspPkgBuildResult
    {
        public bool Success;
        public string ErrorMessage;

        public string InstallRoot;        // absolute path to <baseName>/
        public string GameDir;            // absolute path to PSP/GAME/<TITLE_ID>/
        public string TitleId;            // 9 chars, derived from content_id
        public string EbootPath;          // absolute path to EBOOT.PBP if found
        public string EbootRelativePath;  // relative to InstallRoot

        public List<PspPkgInstallStep> Steps = new List<PspPkgInstallStep>();
        public List<PspRapStaged> Raps = new List<PspRapStaged>();
    }

    public class PspPkgInstallStep
    {
        public string SourcePkg;
        public string ContentId;
        public string TitleId;
        public uint ContentType;
        public uint DrmType;
        public int FilesWritten;
        public int DirectoriesCreated;
        public bool Success;
        public string ErrorMessage;
    }

    public class PspRapStaged
    {
        public string ContentId;
        public string SourcePath;
        public string StagedRapPath;       // <InstallRoot>/PSP/LICENSE/<contentId>.rap
        public string MemstickCopyPath;    // copy in PPSSPP's real LICENSE folder; null if not configured
    }

    public static class PspPkgStaging
    {
        public static PspPkgBuildResult BuildFromGameArchive(string archivePath, string outputBaseDir, string baseName)
        {
            var result = new PspPkgBuildResult();

            if (!File.Exists(archivePath))
            {
                result.ErrorMessage = string.Format("Archive not found: {0}", archivePath);
                return result;
            }

            string tempDir = Path.Combine(Path.GetTempPath(), "ACM_PspPkgBuild_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                if (PkgStagingUtils.LooksLikePkg(archivePath))
                {
                    File.Copy(archivePath, Path.Combine(tempDir, Path.GetFileName(archivePath)), true);
                }
                else
                {
                    var extractor = new Zip();
                    if (!extractor.Extract(archivePath, tempDir))
                    {
                        result.ErrorMessage = string.Format("Failed to extract archive: {0}", archivePath);
                        return result;
                    }
                }

                var pkgs = Directory.GetFiles(tempDir, "*.pkg", SearchOption.AllDirectories);
                if (pkgs.Length == 0)
                {
                    result.ErrorMessage = "No .pkg files found inside the archive.";
                    return result;
                }

                var raps = Directory.GetFiles(tempDir, "*.rap", SearchOption.AllDirectories);

                var parsed = new List<(string Path, PsParsedPkg Pkg)>();
                foreach (string p in pkgs)
                {
                    try
                    {
                        var pkg = PsPkgReader.Parse(p);
                        if (!pkg.Header.IsRetailPspOrPsv)
                        {
                            Logger.Log(string.Format(
                                "PspPkgStaging: skipping non-PSP/PSV PKG '{0}' (type=0x{1:X4}, drm={2}).",
                                p, pkg.Header.PkgType, pkg.DrmType));
                            continue;
                        }
                        parsed.Add((p, pkg));
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(string.Format("PspPkgStaging: skipping unreadable PKG '{0}': {1}", p, ex.Message));
                    }
                }

                if (parsed.Count == 0)
                {
                    result.ErrorMessage = "No readable retail PSP/PSV .pkg files in the archive.";
                    return result;
                }

                parsed.Sort((a, b) => InstallOrderRank(a.Pkg.ContentType).CompareTo(InstallOrderRank(b.Pkg.ContentType)));

                string canonicalTitleId = DeriveTitleId(parsed[0].Pkg);
                if (string.IsNullOrWhiteSpace(canonicalTitleId))
                {
                    result.ErrorMessage = string.Format(
                        "Could not derive TITLE_ID from first PKG's content_id '{0}'. Expected a PSN-style 'XX0000-AAAAAA9999_00-...' identifier.",
                        parsed[0].Pkg.Header.ContentId);
                    return result;
                }
                result.TitleId = canonicalTitleId;

                Directory.CreateDirectory(outputBaseDir);
                string installRoot = Path.Combine(outputBaseDir, baseName);
                if (Directory.Exists(installRoot))
                {
                    PkgStagingUtils.TryDeleteDirectory(installRoot, "PspPkgStaging");
                }
                Directory.CreateDirectory(installRoot);
                result.InstallRoot = installRoot;

                string gameDir = Path.Combine(installRoot, "PSP", "GAME", canonicalTitleId);
                Directory.CreateDirectory(gameDir);
                result.GameDir = gameDir;

                foreach (var (pkgPath, pkg) in parsed)
                {
                    var step = new PspPkgInstallStep
                    {
                        SourcePkg = Path.GetFileName(pkgPath),
                        ContentId = pkg.Header.ContentId,
                        TitleId = pkg.TitleId,
                        ContentType = pkg.ContentType,
                        DrmType = pkg.DrmType,
                    };

                    try
                    {
                        // PSP PSN PKGs store files under "USRDIR/CONTENT/<name>"; PPSSPP expects
                        // them directly under PSP/GAME/<TITLE_ID>/. Strip the prefix per entry.
                        var unpacked = PsPkgUnpacker.Unpack(pkgPath, pkg, gameDir,
                            pathTransform: PspPkgPathTransform);
                        step.FilesWritten = unpacked.FilesWritten;
                        step.DirectoriesCreated = unpacked.DirectoriesCreated;
                        step.Success = true;

                        if (result.EbootRelativePath == null && unpacked.EbootRelativePath != null)
                        {
                            string ebootAbsolute = Path.Combine(gameDir, unpacked.EbootRelativePath);
                            result.EbootPath = ebootAbsolute;
                            result.EbootRelativePath = ebootAbsolute.Substring(installRoot.Length)
                                .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        }

                        Logger.Log(string.Format(
                            "PspPkgStaging: installed {0} (content {1}, type {2}, drm {3}): {4} files, {5} dirs.",
                            step.SourcePkg, step.ContentId, step.ContentType, step.DrmType,
                            step.FilesWritten, step.DirectoriesCreated));
                    }
                    catch (Exception ex)
                    {
                        step.Success = false;
                        step.ErrorMessage = ex.Message;
                        Logger.Log(string.Format("PspPkgStaging: PKG install failed for {0}: {1}", pkgPath, ex.Message), Logger.LogLevel.Exception);
                    }

                    result.Steps.Add(step);
                }

                if (result.EbootPath == null)
                {
                    string ebootCandidate = Directory.GetFiles(gameDir, "EBOOT.PBP", SearchOption.AllDirectories).FirstOrDefault();
                    if (ebootCandidate != null)
                    {
                        result.EbootPath = ebootCandidate;
                        result.EbootRelativePath = ebootCandidate.Substring(installRoot.Length)
                            .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    }
                }

                if (raps.Length > 0)
                {
                    StageRaps(raps, installRoot, result);
                }
                else
                {
                    TryStageRapFromNps(installRoot, result);
                }

                result.Success = result.Steps.Any(s => s.Success);
                if (!result.Success)
                {
                    result.ErrorMessage = result.Steps.FirstOrDefault(s => !s.Success)?.ErrorMessage ?? "All PKG installations failed.";
                }

                if (result.Success && Config.PspAutoInstallUpdates && !string.IsNullOrWhiteSpace(result.TitleId))
                {
                    try
                    {
                        var pspCtx = SonyUpdateContext.ForPsp();
                        var updateResult = Ps3UpdateInstaller.Run(result.TitleId, gameDir, msg => Logger.Log("PspPkgStaging: " + msg), pspCtx);
                        Logger.Log(string.Format(
                            "PspPkgStaging: auto-update install for {0} — {1} queried, {2} downloaded, {3} reused, {4} installed, {5} failed.",
                            result.TitleId, updateResult.Queried, updateResult.Downloaded, updateResult.Reused, updateResult.Installed, updateResult.Failed));
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(string.Format("PspPkgStaging: auto-update install threw for {0}: {1}", result.TitleId, ex), Logger.LogLevel.Exception);
                    }
                }

                if (result.Success && Config.PspAutoInstallDlcs && !string.IsNullOrWhiteSpace(result.TitleId))
                {
                    try
                    {
                        string licenseDir = !string.IsNullOrWhiteSpace(Config.PspPpssppLicensePath)
                            ? PathUtils.GetAbsolutePath(Config.PspPpssppLicensePath)
                            : null;
                        var dlcResult = Ps3DlcInstaller.Run(result.TitleId, gameDir, SonyUpdateContext.ForPsp(), licenseDir, msg => Logger.Log("PspPkgStaging: " + msg));
                        Logger.Log(string.Format(
                            "PspPkgStaging: DLC install for {0} — {1} installed, {2} skipped, {3} failed.",
                            result.TitleId, dlcResult.Installed, dlcResult.Skipped, dlcResult.Failed));
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(string.Format("PspPkgStaging: DLC install threw for {0}: {1}", result.TitleId, ex), Logger.LogLevel.Exception);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString(), Logger.LogLevel.Exception);
                result.Success = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
            finally
            {
                PkgStagingUtils.TryDeleteDirectory(tempDir, "PspPkgStaging");
            }
        }

        private static void StageRaps(string[] sourceRaps, string installRoot, PspPkgBuildResult result)
        {
            string licenseDir = Path.Combine(installRoot, "PSP", "LICENSE");
            Directory.CreateDirectory(licenseDir);

            string memstickLicense = ResolveMemstickLicenseDir();
            if (memstickLicense != null)
            {
                try { Directory.CreateDirectory(memstickLicense); }
                catch (Exception ex)
                {
                    Logger.Log(string.Format("PspPkgStaging: cannot create PPSSPP LICENSE dir '{0}': {1}", memstickLicense, ex.Message));
                    memstickLicense = null;
                }
            }

            foreach (string src in sourceRaps)
            {
                string cid = Path.GetFileNameWithoutExtension(src);
                var staged = new PspRapStaged
                {
                    ContentId = cid,
                    SourcePath = src,
                };

                try
                {
                    staged.StagedRapPath = Path.Combine(licenseDir, cid + ".rap");
                    File.Copy(src, staged.StagedRapPath, true);

                    if (memstickLicense != null)
                    {
                        staged.MemstickCopyPath = Path.Combine(memstickLicense, cid + ".rap");
                        File.Copy(src, staged.MemstickCopyPath, true);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(string.Format("PspPkgStaging: failed to stage RAP {0}: {1}", src, ex.Message));
                }

                result.Raps.Add(staged);
            }

            if (memstickLicense == null && sourceRaps.Length > 0)
            {
                Logger.Log(string.Format(
                    "PspPkgStaging: {0} RAP file(s) staged under {1} but PspPpssppLicensePath is not configured — " +
                    "copy them into PPSSPP's memstick PSP/LICENSE/ folder manually so the NPDRM EBOOT.PBP can decrypt at runtime.",
                    sourceRaps.Length, licenseDir));
            }
        }

        /// <summary>
        /// Fallback when the source archive has no .rap alongside the .pkg: look up the title's
        /// content_id in the NoPayStation DB and, if a RAP hex string is listed, materialise it
        /// as a 16-byte .rap file in the staging PSP/LICENSE/ folder + copy to PPSSPP memstick.
        /// </summary>
        private static void TryStageRapFromNps(string installRoot, PspPkgBuildResult result)
        {
            foreach (var step in result.Steps)
            {
                if (!step.Success || string.IsNullOrWhiteSpace(step.ContentId)) continue;

                var entry = NpsDb.LookupByContentId(step.ContentId) ?? NpsDb.LookupByTitleId(step.TitleId);
                if (entry == null || string.IsNullOrWhiteSpace(entry.RapHex)) continue;

                byte[] rapBytes = PkgStagingUtils.HexToBytes(entry.RapHex);
                if (rapBytes == null || rapBytes.Length != 16)
                {
                    Logger.Log(string.Format("PspPkgStaging: NPS RAP for {0} is not 32 hex chars — skipping.", step.ContentId));
                    continue;
                }

                try
                {
                    string licenseStageDir = Path.Combine(installRoot, "PSP", "LICENSE");
                    Directory.CreateDirectory(licenseStageDir);
                    string staged = Path.Combine(licenseStageDir, step.ContentId + ".rap");
                    File.WriteAllBytes(staged, rapBytes);

                    string staged2 = null;
                    string memstickLicense = ResolveMemstickLicenseDir();
                    if (memstickLicense != null)
                    {
                        try
                        {
                            Directory.CreateDirectory(memstickLicense);
                            staged2 = Path.Combine(memstickLicense, step.ContentId + ".rap");
                            File.WriteAllBytes(staged2, rapBytes);
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(string.Format("PspPkgStaging: failed to copy NPS RAP to PPSSPP LICENSE for {0}: {1}", step.ContentId, ex.Message));
                        }
                    }

                    result.Raps.Add(new PspRapStaged
                    {
                        ContentId = step.ContentId,
                        SourcePath = "<NoPayStation TSV>",
                        StagedRapPath = staged,
                        MemstickCopyPath = staged2,
                    });
                    Logger.Log(string.Format("PspPkgStaging: NPS RAP installed for {0} → {1}", step.ContentId, staged2 ?? staged));
                }
                catch (Exception ex)
                {
                    Logger.Log(string.Format("PspPkgStaging: NPS RAP write failed for {0}: {1}", step.ContentId, ex.Message), Logger.LogLevel.Exception);
                }
            }
        }

        // HexToBytes / LooksLikePkg / TryDeleteDirectory moved to PkgStagingUtils in v2.73.

        private static string ResolveMemstickLicenseDir()
        {
            string configured = Config.PspPpssppLicensePath;
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return PathUtils.GetAbsolutePath(configured);
            }

            // Fallback: derive from the launching emulator's path. PPSSPP portable keeps its
            // memstick under <ppsspp.exe folder>\memstick\PSP\LICENSE\ (when INSTALLED.TXT is
            // not present, otherwise it lives in %USERPROFILE%\Documents\PPSSPP\ — we can't
            // reliably detect that variant without reading PPSSPP's config, so portable wins).
            string emulatorPath = LaunchInfo.Game?.EmulatorPath;
            if (!string.IsNullOrWhiteSpace(emulatorPath))
            {
                string exeDir = Path.GetDirectoryName(emulatorPath);
                if (!string.IsNullOrWhiteSpace(exeDir))
                {
                    string derived = Path.Combine(exeDir, "memstick", "PSP", "LICENSE");
                    Logger.Log(string.Format("PspPkgStaging: PspPpssppLicensePath not configured, auto-derived from emulator path: {0}", derived));
                    return derived;
                }
            }
            return null;
        }

        /// <summary>
        /// PSN content IDs look like "EP0001-UCES01285_00-XXXXXXXXXXXXXXXX". The TITLE_ID is the
        /// 9-character chunk after the first hyphen and before the underscore.
        /// </summary>
        private static string DeriveTitleId(PsParsedPkg pkg)
        {
            string cid = pkg.Header.ContentId ?? string.Empty;
            int dash = cid.IndexOf('-');
            int underscore = cid.IndexOf('_', dash + 1);
            if (dash >= 0 && underscore > dash + 1)
            {
                return cid.Substring(dash + 1, underscore - dash - 1);
            }

            // Fallback to the title_id metadata record, if present.
            if (!string.IsNullOrWhiteSpace(pkg.TitleId))
            {
                return pkg.TitleId.Trim();
            }
            return null;
        }

        /// <summary>
        /// Lower rank = installed first. Base games go before patches; patches before DLC.
        /// Unknown content types are installed last (safest default).
        /// </summary>
        private static int InstallOrderRank(uint contentType)
        {
            switch (contentType)
            {
                case 0x06: return 0; // PSP game (minis / demos / classics)
                case 0x07: return 0; // PSP theme / wallpaper bundle
                case 0x09: return 0; // PSP / PSV licence carrier
                case 0x05: return 2; // patch / update
                case 0x0B: return 4; // DLC
                case 0x0C: return 4; // DLC variant
                default:   return 5;
            }
        }

        private static readonly string[] PspPrefixes = { "USRDIR/CONTENT/", "USRDIR/" };

        /// <summary>
        /// PSP PSN PKGs encode their files relative to a virtual memstick root ("USRDIR/CONTENT/…").
        /// PPSSPP expects them directly under PSP/GAME/&lt;TITLE_ID&gt;/, so strip the prefix. Exposed
        /// publicly so the Sony auto-update installer can reuse it when staging patch PKGs.
        /// </summary>
        public static string PspPkgPathTransform(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            string n = name.Replace('\\', '/').TrimStart('/');
            foreach (string p in PspPrefixes)
            {
                if (n.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                {
                    return n.Substring(p.Length);
                }
            }
            return n;
        }

    }
}
