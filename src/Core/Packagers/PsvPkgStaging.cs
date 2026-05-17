/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * PS Vita .pkg → folder-tree (and optionally → .vpk archive) pipeline.
 *
 * PSV PKGs use the standard PSP/PSV container shape (pkg_type=0x0002), but with
 * an extra wrapping layer on the AES key: the data key is derived in-line by
 * PsPkgReader (main_key = AES-ECB(pkg_vita_X, pkg_data_riv)). The decrypted file
 * layout already matches Vita3K's ux0:app/&lt;TITLE_ID&gt;/ expectations — sce_sys/,
 * eboot.bin, module/*.suprx, etc. — so the staging just needs to walk the item
 * table via PsPkgUnpacker and write files preserving in-PKG paths.
 *
 * Two output modes:
 *   • Folder-tree: &lt;outputBaseDir&gt;/&lt;baseName&gt;/    (default, on-launch cache)
 *   • VPK archive: &lt;outputBaseDir&gt;/&lt;baseName&gt;.vpk (zip with the folder contents,
 *     for the right-click menu — Vita3K's "Install .vpk" handles the rest).
 */
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace ArchiveCacheManager
{
    public class PsvPkgBuildResult
    {
        public bool Success;
        public string ErrorMessage;

        public string InstallRoot;       // absolute path to <baseName>/
        public string VpkPath;            // null when caller didn't ask for VPK packaging
        public string TitleId;            // 9 chars (e.g. "PCSB00963")
        public string ContentId;          // full content_id from PKG header
        public uint   ContentType;
        public int    FilesWritten;
        public int    DirectoriesCreated;
        public string LicenseRifPath;     // if NPS DB had a zRIF and we staged a .rif
    }

    public static class PsvPkgStaging
    {
        public static PsvPkgBuildResult BuildFromGameArchive(string archivePath, string outputBaseDir, string baseName, bool packageAsVpk)
        {
            var result = new PsvPkgBuildResult();

            if (!File.Exists(archivePath))
            {
                result.ErrorMessage = string.Format("Archive not found: {0}", archivePath);
                return result;
            }

            string tempDir = Path.Combine(Path.GetTempPath(), "ACM_PsvPkgBuild_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                string pkgPath = ResolvePkgFile(archivePath, tempDir);
                if (pkgPath == null)
                {
                    result.ErrorMessage = "No .pkg file found in the source archive.";
                    return result;
                }

                PsParsedPkg pkg;
                try
                {
                    pkg = PsPkgReader.Parse(pkgPath);
                }
                catch (Exception ex)
                {
                    result.ErrorMessage = string.Format("PKG header parse failed: {0}", ex.Message);
                    return result;
                }

                if (!pkg.Header.IsRetailPspOrPsv || pkg.ContentType < 0x14 || pkg.ContentType > 0x18)
                {
                    result.ErrorMessage = string.Format(
                        "Not a retail PSV PKG (type=0x{0:X4}, content_type={1}). Use PS3/PSP flows for those.",
                        pkg.Header.PkgType, pkg.ContentType);
                    return result;
                }

                result.ContentId   = pkg.Header.ContentId;
                result.ContentType = pkg.ContentType;
                result.TitleId     = DeriveTitleId(pkg);
                if (string.IsNullOrWhiteSpace(result.TitleId))
                {
                    result.ErrorMessage = string.Format("Could not derive TITLE_ID from content_id '{0}'.", pkg.Header.ContentId);
                    return result;
                }

                Directory.CreateDirectory(outputBaseDir);
                string installRoot = Path.Combine(outputBaseDir, baseName);
                if (Directory.Exists(installRoot)) PkgStagingUtils.TryDeleteDirectory(installRoot, "PsvPkgStaging");
                Directory.CreateDirectory(installRoot);
                result.InstallRoot = installRoot;

                var unpacked = PsPkgUnpacker.Unpack(pkgPath, pkg, installRoot,
                    progress: msg => Logger.Log("PsvPkgStaging: " + msg));
                result.FilesWritten        = unpacked.FilesWritten;
                result.DirectoriesCreated  = unpacked.DirectoriesCreated;

                // Resolve the NPDRM klicensee from the NPS DB *before* zipping. The decoded RIF is
                // also written into `sce_sys/package/work.bin` inside installRoot so the resulting
                // .vpk carries it — Vita3K reads the klicensee from there at boot to decrypt the
                // NPDRM eboot.bin. This is exactly what pkg2zip + NPS Browser do.
                TryInstallLicenseFromNps(result);

                if (Config.PsvAutoInstallDlcs && !string.IsNullOrWhiteSpace(result.TitleId))
                {
                    try
                    {
                        // PSV DLC ships under <Vita3K>/ux0/addcont/<TID>/<contentid_suffix>/. Vita3K
                        // reads the resulting tree without per-DLC license action — the .rif we
                        // already staged for the base content covers the whole title family.
                        var dlcResult = Ps3DlcInstaller.Run(result.TitleId, installRoot, SonyUpdateContext.ForPsv(), null, msg => Logger.Log("PsvPkgStaging: " + msg));
                        Logger.Log(string.Format(
                            "PsvPkgStaging: DLC install for {0} — {1} installed, {2} skipped, {3} failed.",
                            result.TitleId, dlcResult.Installed, dlcResult.Skipped, dlcResult.Failed));
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(string.Format("PsvPkgStaging: DLC install threw for {0}: {1}", result.TitleId, ex), Logger.LogLevel.Exception);
                    }
                }

                if (packageAsVpk)
                {
                    string vpkPath = Path.Combine(outputBaseDir, baseName + ".vpk");
                    try { if (File.Exists(vpkPath)) File.Delete(vpkPath); } catch { }
                    ZipFile.CreateFromDirectory(installRoot, vpkPath, CompressionLevel.NoCompression, includeBaseDirectory: false);
                    result.VpkPath = vpkPath;
                    Logger.Log(string.Format("PsvPkgStaging: packaged VPK at {0}", vpkPath));
                }

                TryInstallUpdates(result);

                result.Success = result.FilesWritten > 0;
                if (!result.Success && string.IsNullOrEmpty(result.ErrorMessage))
                {
                    result.ErrorMessage = "PKG decoded but produced 0 files (all entries skipped — see log).";
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
                PkgStagingUtils.TryDeleteDirectory(tempDir, "PsvPkgStaging");
            }
        }

        private static string ResolvePkgFile(string archivePath, string tempDir)
        {
            if (Path.GetExtension(archivePath).Equals(".pkg", StringComparison.OrdinalIgnoreCase))
            {
                return archivePath;
            }
            var zip = new Zip();
            if (!zip.Extract(archivePath, tempDir, new[] { "*.pkg" })) return null;
            return Directory.GetFiles(tempDir, "*.pkg", SearchOption.AllDirectories).FirstOrDefault();
        }

        /// <summary>
        /// PSV content IDs look like "EP4395-PCSB00963_00-G000000000000885". TITLE_ID is the
        /// 9-character chunk between the first '-' and the '_'.
        /// </summary>
        private static string DeriveTitleId(PsParsedPkg pkg)
        {
            string cid = pkg.Header.ContentId ?? string.Empty;
            int dash = cid.IndexOf('-');
            int under = dash >= 0 ? cid.IndexOf('_', dash + 1) : -1;
            if (dash >= 0 && under > dash + 1) return cid.Substring(dash + 1, under - dash - 1);
            return !string.IsNullOrWhiteSpace(pkg.TitleId) ? pkg.TitleId.Trim() : null;
        }

        // TryDeleteDirectory moved to PkgStagingUtils in v2.73.

        /// <summary>
        /// Look up the title in the NoPayStation DB (when configured) and, if a zRIF is listed,
        /// decode it into a binary .rif and place it under Vita3K's license folder for the title.
        /// Vita3K will load this file at boot time so the NPDRM eboot.bin decrypts cleanly.
        /// </summary>
        private static void TryInstallLicenseFromNps(PsvPkgBuildResult r)
        {
            if (string.IsNullOrWhiteSpace(r.TitleId) || string.IsNullOrWhiteSpace(r.ContentId)) return;

            var entry = NpsDb.LookupByContentId(r.ContentId) ?? NpsDb.LookupByTitleId(r.TitleId);
            if (entry == null || string.IsNullOrWhiteSpace(entry.Zrif))
            {
                Logger.Log(string.Format(
                    "PsvPkgStaging: no zRIF in NPS DB for {0} ({1}). Vita3K will refuse to boot the NPDRM eboot until a license is installed manually.",
                    r.TitleId, r.ContentId));
                return;
            }

            byte[] rif;
            try { rif = Zrif.Decode(entry.Zrif); }
            catch (Exception ex)
            {
                Logger.Log(string.Format("PsvPkgStaging: zRIF decode failed for {0}: {1}", r.TitleId, ex.Message), Logger.LogLevel.Exception);
                return;
            }

            // Drop the decoded RIF as `sce_sys/package/work.bin` inside the extracted tree (and so
            // also into the .vpk that ZipFile.CreateFromDirectory will package up next). Vita3K
            // reads the NPDRM klicensee from this file at app boot — it's how NPS Browser / pkg2zip
            // produce playable PSV titles. The file is bit-identical to a standalone `<contentid>.rif`.
            if (!string.IsNullOrEmpty(r.InstallRoot))
            {
                try
                {
                    string workDir = Path.Combine(r.InstallRoot, "sce_sys", "package");
                    Directory.CreateDirectory(workDir);
                    string workPath = Path.Combine(workDir, "work.bin");
                    File.WriteAllBytes(workPath, rif);
                    Logger.Log(string.Format("PsvPkgStaging: NPS zRIF → work.bin ({0} bytes) staged at {1}.", rif.Length, workPath));
                }
                catch (Exception ex)
                {
                    Logger.Log(string.Format("PsvPkgStaging: failed to write sce_sys/package/work.bin: {0}", ex.Message), Logger.LogLevel.Exception);
                }
            }

            string vita3kData = ResolveVita3kDataRoot();
            if (string.IsNullOrEmpty(vita3kData))
            {
                Logger.Log(string.Format(
                    "PsvPkgStaging: zRIF available for {0} but PsvVita3kDataPath not configured — drop the RIF manually into <Vita3K>/ux0/license/app/{0}/{1}.rif.",
                    r.TitleId, r.ContentId));
                return;
            }

            try
            {
                // Vita3K (≥ v0.2.x) reads PSV NPDRM licenses from `ux0/license/<TID>/<contentid>.rif`,
                // NOT `ux0/license/app/<TID>/…` — the extra "app" middle directory was the pkg2zip
                // output convention but Vita3K does not look there. Write to the modern path; also
                // mirror to the legacy "app" path so people running pre-v0.2 Vita3K builds still work.
                string modernDir = Path.Combine(vita3kData, "ux0", "license", r.TitleId);
                Directory.CreateDirectory(modernDir);
                string rifPath = Path.Combine(modernDir, r.ContentId + ".rif");
                File.WriteAllBytes(rifPath, rif);
                r.LicenseRifPath = rifPath;
                Logger.Log(string.Format("PsvPkgStaging: NPS zRIF → license installed at {0} ({1} bytes).", rifPath, rif.Length));

                // Legacy mirror — safe to keep, Vita3K just ignores it.
                try
                {
                    string legacyDir = Path.Combine(vita3kData, "ux0", "license", "app", r.TitleId);
                    Directory.CreateDirectory(legacyDir);
                    File.WriteAllBytes(Path.Combine(legacyDir, r.ContentId + ".rif"), rif);
                }
                catch { /* mirror is best-effort */ }
            }
            catch (Exception ex)
            {
                Logger.Log(string.Format("PsvPkgStaging: failed to write Vita3K license for {0}: {1}", r.TitleId, ex.Message), Logger.LogLevel.Exception);
            }
        }

        /// <summary>
        /// When Config.PsvAutoInstallUpdates is on, query Sony's HMAC-signed PSV update endpoint
        /// for the installed title and stage every published patch into
        /// &lt;Vita3K&gt;/ux0/patch/&lt;TITLE_ID&gt;/. Vita3K auto-merges patches over the base app on next
        /// launch. Network failures are non-fatal.
        /// </summary>
        private static void TryInstallUpdates(PsvPkgBuildResult r)
        {
            if (!Config.PsvAutoInstallUpdates) return;
            if (string.IsNullOrWhiteSpace(r.TitleId)) return;

            string vita3kData = ResolveVita3kDataRoot();
            if (string.IsNullOrEmpty(vita3kData))
            {
                Logger.Log("PsvPkgStaging: auto-update enabled but no Vita3K data folder resolved — skipping.");
                return;
            }
            string patchDir = Path.Combine(vita3kData, "ux0", "patch", r.TitleId);

            try
            {
                var ctx = SonyUpdateContext.ForPsv();
                var res = Ps3UpdateInstaller.Run(r.TitleId, patchDir, msg => Logger.Log("PsvPkgStaging: " + msg), ctx);
                Logger.Log(string.Format(
                    "PsvPkgStaging: auto-update install for {0} — {1} queried, {2} downloaded, {3} reused, {4} installed, {5} failed.",
                    r.TitleId, res.Queried, res.Downloaded, res.Reused, res.Installed, res.Failed));
            }
            catch (Exception ex)
            {
                Logger.Log(string.Format("PsvPkgStaging: auto-update threw for {0}: {1}", r.TitleId, ex), Logger.LogLevel.Exception);
            }
        }

        private static string ResolveVita3kDataRoot()
        {
            string configured = Config.PsvVita3kDataPath;
            if (!string.IsNullOrWhiteSpace(configured)) return PathUtils.GetAbsolutePath(configured);

            // Best-effort default for the standard Vita3K Windows install (per-user AppData).
            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (!string.IsNullOrEmpty(appdata))
            {
                string candidate = Path.Combine(appdata, "Vita3K", "Vita3K");
                if (Directory.Exists(Path.Combine(candidate, "ux0"))) return candidate;
            }
            return null;
        }
    }
}
