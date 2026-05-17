/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * On-launch wrapper that turns a PSV .pkg-bearing archive (.zip/.7z/.rar) — or a
 * bare .pkg — into a `<baseName>.vpk` file inside the plugin cache.
 *
 * Why a .vpk and not a directly-runnable folder: Vita3K's CLI does not accept a
 * bare path to a decrypted eboot — `-r` wants an installed app, and there's no
 * `--vpk install-and-run` flag. In v2.30..v2.38 we tried various ways to make
 * LaunchBox feed Vita3K something it could run (folder tree + `-F -r eboot.bin`,
 * powershell wrapper to fix CWD, etc.); none of them produced a reliable
 * end-to-end auto-launch. So the practical contract is now:
 *
 *   • ACM produces a clean .vpk in the cache.
 *   • The user drag-drops that .vpk onto Vita3K once (first launch). Vita3K
 *     installs the app under ux0:app/<TITLE_ID>/, and on subsequent launches it
 *     boots straight from there.
 *   • The NPDRM license is auto-staged (ux0:license/app/<TITLE_ID>/...) by the
 *     shared staging code below, so the NPDRM eboot decrypts cleanly.
 *
 * Folder-tree output is also produced by PsvPkgStaging but immediately torn
 * down so the cache only holds the .vpk.
 */
using System;
using System.IO;

namespace ArchiveCacheManager
{
    public class PsvPkgExtractor : Extractor
    {
        public override string Name() => "PSV PKG";

        public override bool AlwaysCache => true;

        public override string GetExtractorPath() => null;

        public static bool SupportedType(string archivePath)
        {
            return Zip.SupportedType(archivePath) || PathUtils.HasExtension(archivePath, new[] { ".pkg" });
        }

        public override long GetSize(string archivePath, string fileInArchive = null)
        {
            return (long)(DiskUtils.GetFileSize(archivePath) * 1.1);
        }

        public override string[] List(string archivePath)
        {
            string baseName = Path.GetFileNameWithoutExtension(archivePath);
            return new[] { baseName + ".vpk" };
        }

        public override bool Extract(string archivePath, string cachePath, string[] includeList = null, string[] excludeList = null)
        {
            string baseName = Path.GetFileNameWithoutExtension(archivePath);

            var result = PsvPkgStaging.BuildFromGameArchive(archivePath, cachePath, baseName, packageAsVpk: true);
            if (!result.Success)
            {
                Logger.Log(string.Format("PsvPkgExtractor: build failed: {0}", result.ErrorMessage ?? "<no detail>"));
                return false;
            }

            if (string.IsNullOrEmpty(result.VpkPath) || !File.Exists(result.VpkPath))
            {
                Logger.Log(string.Format("PsvPkgExtractor: build reported success but no .vpk at {0}.",
                    result.VpkPath ?? "<null>"));
                return false;
            }

            // Drop the intermediate folder tree — the .vpk carries everything Vita3K needs.
            try
            {
                if (!string.IsNullOrEmpty(result.InstallRoot) && Directory.Exists(result.InstallRoot))
                {
                    Directory.Delete(result.InstallRoot, true);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(string.Format("PsvPkgExtractor: failed to clean intermediate folder {0}: {1}", result.InstallRoot, ex.Message));
            }

            string vpkRel = Path.GetFileName(result.VpkPath);
            LaunchInfo.Game.SelectedFile = vpkRel;
            LaunchInfo.Game.Save();
            GameIndex.SetSelectedFile(LaunchInfo.Game.GameId, vpkRel);
            Logger.Log(string.Format(
                "PsvPkgExtractor: VPK ready at {0} (TITLE_ID={1}). Drag-drop onto Vita3K to install; license already staged under ux0:license/app/{1}/.",
                result.VpkPath, result.TitleId));
            return true;
        }
    }
}
