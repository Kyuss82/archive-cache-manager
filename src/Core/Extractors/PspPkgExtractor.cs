/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * On-launch wrapper that turns a PSP .pkg-bearing archive (.zip/.7z/.rar) — or
 * a bare .pkg — into a decrypted, PPSSPP-bootable memstick folder inside the
 * plugin cache. Underlying pipeline is PspPkgStaging.
 *
 * Output shape mirrors PPSSPP's memstick:
 *   <cache>/<baseName>/PSP/GAME/<TITLE_ID>/EBOOT.PBP   ← LaunchInfo.SelectedFile
 *   <cache>/<baseName>/PSP/LICENSE/<contentId>.rap
 *
 * The EBOOT.PBP is NPDRM-encrypted; PPSSPP decrypts it at runtime using the .rap
 * that PspPkgStaging dropped into its configured memstick LICENSE folder.
 */
using System.IO;
using System.Linq;

namespace ArchiveCacheManager
{
    public class PspPkgExtractor : Extractor
    {
        private string mLastEbootRelative;
        private string mLastBaseName;

        public override string Name() => "PSP PKG";

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
            string rel = (mLastBaseName == baseName && !string.IsNullOrEmpty(mLastEbootRelative))
                ? Path.Combine(baseName, mLastEbootRelative)
                : Path.Combine(baseName, "PSP", "GAME", "UNKNOWN", "EBOOT.PBP");
            return new[] { rel };
        }

        public override bool Extract(string archivePath, string cachePath, string[] includeList = null, string[] excludeList = null)
        {
            string baseName = Path.GetFileNameWithoutExtension(archivePath);
            mLastBaseName = baseName;

            var result = PspPkgStaging.BuildFromGameArchive(archivePath, cachePath, baseName);

            if (!result.Success)
            {
                Logger.Log(string.Format("PspPkgExtractor: build failed: {0}", result.ErrorMessage ?? "<no detail>"));
                return false;
            }

            mLastEbootRelative = result.EbootRelativePath;

            if (string.IsNullOrEmpty(result.EbootPath) || !File.Exists(result.EbootPath))
            {
                Logger.Log(string.Format(
                    "PspPkgExtractor: install completed but EBOOT.PBP not located under {0}. " +
                    "PPSSPP may not be able to boot this title without manual intervention.",
                    result.InstallRoot));
            }
            else
            {
                string ebootRelToCache = result.EbootPath.Substring(cachePath.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                LaunchInfo.Game.SelectedFile = ebootRelToCache;
                LaunchInfo.Game.Save();
                GameIndex.SetSelectedFile(LaunchInfo.Game.GameId, ebootRelToCache);

                Logger.Log(string.Format("PspPkgExtractor: EBOOT.PBP = {0}", result.EbootPath));
            }

            int failed = result.Steps.Count(s => !s.Success);
            if (failed > 0)
            {
                Logger.Log(string.Format("PspPkgExtractor: {0} of {1} PKG install steps failed (continuing).",
                    failed, result.Steps.Count));
            }

            return true;
        }
    }
}
