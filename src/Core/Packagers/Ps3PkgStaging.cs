/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * High-level orchestrator that turns a CDN-/scene-style archive (.zip/.7z/.rar)
 * carrying one or more PS3 .pkg files (+ optional .rap licence files) into a
 * decrypted, RPCS3-bootable game folder inside the plugin cache.
 *
 * Pipeline (mirrors WadStaging / CiaStaging / WiiuStaging structure):
 *   1. Extract source archive to a private temp dir via the existing Zip helper.
 *   2. Enumerate every .pkg + .rap inside.
 *   3. Sort PKGs base → update → DLC by reading content_type from each PKG's
 *      metadata block. Base games install first; patches and DLC merge over the
 *      same install root.
 *   4. For each PKG, run PsPkgUnpacker against a single install root under the
 *      caller-supplied outputBaseDir. Multi-PKG sets share one root so RPCS3
 *      sees a normal "patched + DLC" install layout.
 *   5. RAP files are persisted alongside the install (under a sibling _rap/ folder)
 *      and, when the user has configured Ps3RpcsExdataPath, copied into RPCS3's
 *      dev_hdd0/home/00000001/exdata/ so PSN licences are picked up automatically.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ArchiveCacheManager
{
    public class Ps3PkgBuildResult
    {
        public bool Success;
        public string ErrorMessage;

        public string InstallRoot;        // absolute path to the merged install
        public string TitleId;            // first PKG's title id (e.g. "NPEB00000")
        public string EbootPath;          // absolute path to USRDIR/EBOOT.BIN (or alt) if found
        public string EbootRelativePath;  // relative to InstallRoot

        public List<Ps3PkgInstallStep> Steps = new List<Ps3PkgInstallStep>();
        public List<Ps3RapStaged> Raps = new List<Ps3RapStaged>();
    }

    public class Ps3PkgInstallStep
    {
        public string SourcePkg;
        public string ContentId;
        public string TitleId;
        public uint ContentType;
        public int FilesWritten;
        public int DirectoriesCreated;
        public bool Success;
        public string ErrorMessage;
    }

    public class Ps3RapStaged
    {
        public string ContentId;
        public string SourcePath;
        public string StagedRapPath;     // <InstallRoot>/_rap/<contentId>.rap
        public string ExdataCopyPath;    // null if Ps3RpcsExdataPath is not configured
    }

    public static class Ps3PkgStaging
    {
        public static Ps3PkgBuildResult BuildFromGameArchive(string archivePath, string outputBaseDir, string baseName)
        {
            var result = new Ps3PkgBuildResult();

            if (!File.Exists(archivePath))
            {
                result.ErrorMessage = string.Format("Archive not found: {0}", archivePath);
                return result;
            }

            string tempDir = Path.Combine(Path.GetTempPath(), "ACM_Ps3PkgBuild_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                if (PkgStagingUtils.LooksLikePkg(archivePath))
                {
                    // Single bare .pkg — copy into tempDir as-is, no archive extraction needed.
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

                // Parse every PKG header first so we can sort base → patch → DLC.
                // Reject anything that isn't a retail PS3 package — PSP/PSV PKGs go through PspPkgStaging.
                var parsed = new List<(string Path, PsParsedPkg Pkg)>();
                foreach (string p in pkgs)
                {
                    try
                    {
                        var pkg = PsPkgReader.Parse(p);
                        if (!pkg.Header.IsRetailPs3)
                        {
                            Logger.Log(string.Format(
                                "Ps3PkgStaging: skipping non-PS3 PKG '{0}' (type=0x{1:X4}, drm={2}).",
                                p, pkg.Header.PkgType, pkg.DrmType));
                            continue;
                        }
                        parsed.Add((p, pkg));
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(string.Format("Ps3PkgStaging: skipping unreadable PKG '{0}': {1}", p, ex.Message));
                    }
                }

                if (parsed.Count == 0)
                {
                    result.ErrorMessage = "No readable retail PS3 .pkg files in the archive.";
                    return result;
                }

                parsed.Sort((a, b) => InstallOrderRank(a.Pkg.ContentType).CompareTo(InstallOrderRank(b.Pkg.ContentType)));

                // Take the first PKG's title id as the canonical id for the install root.
                string canonicalTitleId = parsed[0].Pkg.TitleId;
                result.TitleId = canonicalTitleId;

                Directory.CreateDirectory(outputBaseDir);
                string installRoot = Path.Combine(outputBaseDir, baseName);
                if (Directory.Exists(installRoot))
                {
                    PkgStagingUtils.TryDeleteDirectory(installRoot, "Ps3PkgStaging");
                }
                Directory.CreateDirectory(installRoot);
                result.InstallRoot = installRoot;

                foreach (var (pkgPath, pkg) in parsed)
                {
                    var step = new Ps3PkgInstallStep
                    {
                        SourcePkg = Path.GetFileName(pkgPath),
                        ContentId = pkg.Header.ContentId,
                        TitleId = pkg.TitleId,
                        ContentType = pkg.ContentType,
                    };

                    try
                    {
                        var unpacked = PsPkgUnpacker.Unpack(pkgPath, pkg, installRoot);
                        step.FilesWritten = unpacked.FilesWritten;
                        step.DirectoriesCreated = unpacked.DirectoriesCreated;
                        step.Success = true;

                        if (result.EbootRelativePath == null && unpacked.EbootRelativePath != null)
                        {
                            result.EbootRelativePath = unpacked.EbootRelativePath;
                            result.EbootPath = Path.Combine(installRoot, unpacked.EbootRelativePath);
                        }

                        Logger.Log(string.Format(
                            "Ps3PkgStaging: installed {0} (content {1}, type {2}): {3} files, {4} dirs.",
                            step.SourcePkg, step.ContentId, step.ContentType,
                            step.FilesWritten, step.DirectoriesCreated));
                    }
                    catch (Exception ex)
                    {
                        step.Success = false;
                        step.ErrorMessage = ex.Message;
                        Logger.Log(string.Format("Ps3PkgStaging: PKG install failed for {0}: {1}", pkgPath, ex.Message), Logger.LogLevel.Exception);
                    }

                    result.Steps.Add(step);
                }

                // Fallback EBOOT lookup if no item was tagged EBOOT.BIN by the unpacker.
                if (result.EbootPath == null)
                {
                    string ebootCandidate = Directory.GetFiles(installRoot, "EBOOT.BIN", SearchOption.AllDirectories).FirstOrDefault();
                    if (ebootCandidate != null)
                    {
                        result.EbootPath = ebootCandidate;
                        result.EbootRelativePath = ebootCandidate.Substring(installRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
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

                if (result.Success && Config.Ps3AutoInstallUpdates && !string.IsNullOrWhiteSpace(result.TitleId))
                {
                    try
                    {
                        var updateResult = Ps3UpdateInstaller.Run(result.TitleId, installRoot, msg => Logger.Log("Ps3PkgStaging: " + msg));
                        Logger.Log(string.Format(
                            "Ps3PkgStaging: auto-update install for {0} — {1} queried, {2} downloaded, {3} reused, {4} installed, {5} failed.",
                            result.TitleId, updateResult.Queried, updateResult.Downloaded, updateResult.Reused, updateResult.Installed, updateResult.Failed));
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(string.Format("Ps3PkgStaging: auto-update install threw for {0}: {1}", result.TitleId, ex), Logger.LogLevel.Exception);
                    }
                }

                if (result.Success && Config.Ps3AutoInstallDlcs && !string.IsNullOrWhiteSpace(result.TitleId))
                {
                    try
                    {
                        string exdataDir = ResolveExdataDir();
                        var dlcResult = Ps3DlcInstaller.Run(result.TitleId, installRoot, SonyUpdateContext.ForPs3(), exdataDir, msg => Logger.Log("Ps3PkgStaging: " + msg));
                        Logger.Log(string.Format(
                            "Ps3PkgStaging: DLC install for {0} — {1} installed, {2} skipped, {3} failed.",
                            result.TitleId, dlcResult.Installed, dlcResult.Skipped, dlcResult.Failed));
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(string.Format("Ps3PkgStaging: DLC install threw for {0}: {1}", result.TitleId, ex), Logger.LogLevel.Exception);
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
                PkgStagingUtils.TryDeleteDirectory(tempDir, "Ps3PkgStaging");
            }
        }

        private static void StageRaps(string[] sourceRaps, string installRoot, Ps3PkgBuildResult result)
        {
            string rapStageDir = Path.Combine(installRoot, "_rap");
            Directory.CreateDirectory(rapStageDir);

            string exdataDir = ResolveExdataDir();
            if (exdataDir != null)
            {
                try { Directory.CreateDirectory(exdataDir); }
                catch (Exception ex)
                {
                    Logger.Log(string.Format("Ps3PkgStaging: cannot create RPCS3 exdata dir '{0}': {1}", exdataDir, ex.Message));
                    exdataDir = null;
                }
            }

            foreach (string src in sourceRaps)
            {
                string cid = Path.GetFileNameWithoutExtension(src);
                var staged = new Ps3RapStaged
                {
                    ContentId = cid,
                    SourcePath = src,
                };

                try
                {
                    staged.StagedRapPath = Path.Combine(rapStageDir, cid + ".rap");
                    File.Copy(src, staged.StagedRapPath, true);

                    if (exdataDir != null)
                    {
                        staged.ExdataCopyPath = Path.Combine(exdataDir, cid + ".rap");
                        File.Copy(src, staged.ExdataCopyPath, true);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(string.Format("Ps3PkgStaging: failed to stage RAP {0}: {1}", src, ex.Message));
                }

                result.Raps.Add(staged);
            }

            if (exdataDir == null && sourceRaps.Length > 0)
            {
                Logger.Log(string.Format(
                    "Ps3PkgStaging: {0} RAP file(s) staged under {1} but Ps3RpcsExdataPath is not configured — " +
                    "copy them into RPCS3's dev_hdd0/home/00000001/exdata/ manually to unlock PSN licences.",
                    sourceRaps.Length, rapStageDir));
            }
        }

        /// <summary>
        /// Fallback when the source archive has no .rap alongside the .pkg: look up the title's
        /// content_id in the NoPayStation DB and, if a RAP hex string is listed, materialise it as
        /// a 16-byte .rap file in the staging _rap/ folder + copy to RPCS3 exdata.
        /// </summary>
        private static void TryStageRapFromNps(string installRoot, Ps3PkgBuildResult result)
        {
            foreach (var step in result.Steps)
            {
                if (!step.Success || string.IsNullOrWhiteSpace(step.ContentId)) continue;

                var entry = NpsDb.LookupByContentId(step.ContentId) ?? NpsDb.LookupByTitleId(step.TitleId);
                if (entry == null || string.IsNullOrWhiteSpace(entry.RapHex)) continue;

                byte[] rapBytes = PkgStagingUtils.HexToBytes(entry.RapHex);
                if (rapBytes == null || rapBytes.Length != 16)
                {
                    Logger.Log(string.Format("Ps3PkgStaging: NPS RAP for {0} is not 32 hex chars — skipping.", step.ContentId));
                    continue;
                }

                try
                {
                    string rapStageDir = Path.Combine(installRoot, "_rap");
                    Directory.CreateDirectory(rapStageDir);
                    string staged = Path.Combine(rapStageDir, step.ContentId + ".rap");
                    File.WriteAllBytes(staged, rapBytes);

                    string staged2 = null;
                    string exdataDir = ResolveExdataDir();
                    if (exdataDir != null)
                    {
                        try
                        {
                            Directory.CreateDirectory(exdataDir);
                            staged2 = Path.Combine(exdataDir, step.ContentId + ".rap");
                            File.WriteAllBytes(staged2, rapBytes);
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(string.Format("Ps3PkgStaging: failed to copy NPS RAP to exdata for {0}: {1}", step.ContentId, ex.Message));
                        }
                    }

                    result.Raps.Add(new Ps3RapStaged
                    {
                        ContentId = step.ContentId,
                        SourcePath = "<NoPayStation TSV>",
                        StagedRapPath = staged,
                        ExdataCopyPath = staged2,
                    });
                    Logger.Log(string.Format("Ps3PkgStaging: NPS RAP installed for {0} → {1}", step.ContentId, staged2 ?? staged));
                }
                catch (Exception ex)
                {
                    Logger.Log(string.Format("Ps3PkgStaging: NPS RAP write failed for {0}: {1}", step.ContentId, ex.Message), Logger.LogLevel.Exception);
                }
            }
        }

        // HexToBytes / LooksLikePkg / TryDeleteDirectory moved to PkgStagingUtils in v2.73.

        private static string ResolveExdataDir()
        {
            string configured = Config.Ps3RpcsExdataPath;
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return PathUtils.GetAbsolutePath(configured);
            }

            // Fallback: derive from the launching emulator's path. RPCS3 is portable by default
            // and keeps its dev_hdd0 alongside rpcs3.exe.
            string emulatorPath = LaunchInfo.Game?.EmulatorPath;
            if (!string.IsNullOrWhiteSpace(emulatorPath))
            {
                string exeDir = Path.GetDirectoryName(emulatorPath);
                if (!string.IsNullOrWhiteSpace(exeDir))
                {
                    string derived = Path.Combine(exeDir, "dev_hdd0", "home", "00000001", "exdata");
                    Logger.Log(string.Format("Ps3PkgStaging: Ps3RpcsExdataPath not configured, auto-derived from emulator path: {0}", derived));
                    return derived;
                }
            }
            return null;
        }

        /// <summary>
        /// Lower rank = installed first. Base games go before patches; patches before DLC.
        /// Unknown content types are installed last (safest default).
        /// </summary>
        private static int InstallOrderRank(uint contentType)
        {
            // Numbering matches the values published on PSDevWiki for PKG metadata record 0x02.
            switch (contentType)
            {
                case 0x01: return 0; // PSP game (rare on PS3 PKG)
                case 0x04: return 0; // PS3 game
                case 0x06: return 0; // PSP / demo
                case 0x09: return 1; // PS3 licence / theme
                case 0x0A: return 0; // PS3 application
                case 0x05: return 2; // PS3 patch / update
                case 0x07: return 3; // PS3 theme
                case 0x0B: return 4; // PS3 DLC
                case 0x0C: return 4; // PS3 DLC variant
                default:   return 5;
            }
        }

    }
}
