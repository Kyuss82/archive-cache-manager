/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * On-launch wrapper that turns a PS3 .pkg-bearing archive (.zip/.7z/.rar) — or a
 * bare .pkg — into a decrypted, RPCS3-bootable game folder inside the plugin
 * cache. Underlying pipeline is Ps3PkgStaging.
 *
 * Output shape differs from the Wii / 3DS / DSi extractors: those produce a
 * single .wad / .cia / .tad file; a PS3 install is an entire directory tree
 * (USRDIR/EBOOT.BIN plus PARAM.SFO, PIC*.PNG, …). The extractor records the
 * absolute path to EBOOT.BIN on LaunchInfo so the GameLaunching glue can
 * redirect RPCS3 at it.
 *
 * List() returns the relative path of EBOOT.BIN within the cache directory so
 * the plugin's fake-7z file listing remains consistent — LaunchBox happily
 * launches a "file inside the archive" whose path includes subdirectories.
 */
using System.IO;
using System.Linq;

namespace ArchiveCacheManager
{
    public class Ps3PkgExtractor : Extractor
    {
        // Filled in by Extract() so List() (called after Extract on the same instance) can
        // report the actual EBOOT path. Falls back to a sensible default for pre-extract calls.
        private string mLastEbootRelative;
        private string mLastBaseName;

        public override string Name() => "PS3 PKG";

        public override bool AlwaysCache => true;

        public override string GetExtractorPath() => null;

        public static bool SupportedType(string archivePath)
        {
            return Zip.SupportedType(archivePath) || PathUtils.HasExtension(archivePath, new[] { ".pkg" });
        }

        public override long GetSize(string archivePath, string fileInArchive = null)
        {
            // Decrypted output is roughly 1.0–1.1× source size; over-estimate slightly so the
            // cache-room calculation doesn't undersize before extraction completes.
            return (long)(DiskUtils.GetFileSize(archivePath) * 1.1);
        }

        public override string[] List(string archivePath)
        {
            string baseName = Path.GetFileNameWithoutExtension(archivePath);
            // Pre-extract: produce a stable placeholder so LaunchBox's pre-launch file
            // prediction has something to match against. Post-extract: return the real EBOOT
            // path under <baseName>/.
            string rel = (mLastBaseName == baseName && !string.IsNullOrEmpty(mLastEbootRelative))
                ? Path.Combine(baseName, mLastEbootRelative)
                : Path.Combine(baseName, "USRDIR", "EBOOT.BIN");
            return new[] { rel };
        }

        public override bool Extract(string archivePath, string cachePath, string[] includeList = null, string[] excludeList = null)
        {
            string baseName = Path.GetFileNameWithoutExtension(archivePath);
            mLastBaseName = baseName;

            var result = Ps3PkgStaging.BuildFromGameArchive(archivePath, cachePath, baseName);

            if (!result.Success)
            {
                Logger.Log(string.Format("Ps3PkgExtractor: build failed: {0}", result.ErrorMessage ?? "<no detail>"));
                return false;
            }

            mLastEbootRelative = result.EbootRelativePath;

            if (string.IsNullOrEmpty(result.EbootPath) || !File.Exists(result.EbootPath))
            {
                Logger.Log(string.Format(
                    "Ps3PkgExtractor: install completed but EBOOT.BIN not located under {0}. " +
                    "RPCS3 may not be able to boot this title without manual intervention.",
                    result.InstallRoot));
                // Still report success — the install tree is on disk and the user can browse it.
            }
            else
            {
                // Persist EBOOT location for the GameLaunching glue. Stored as a path
                // relative to the cache directory so the launcher can resolve it back later.
                string ebootRelToCache = result.EbootPath.Substring(cachePath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                LaunchInfo.Game.SelectedFile = ebootRelToCache;
                LaunchInfo.Game.Save();
                GameIndex.SetSelectedFile(LaunchInfo.Game.GameId, ebootRelToCache);

                Logger.Log(string.Format("Ps3PkgExtractor: EBOOT.BIN = {0}", result.EbootPath));
            }

            int failed = result.Steps.Count(s => !s.Success);
            if (failed > 0)
            {
                Logger.Log(string.Format("Ps3PkgExtractor: {0} of {1} PKG install steps failed (continuing).",
                    failed, result.Steps.Count));
            }

            return true;
        }
    }
}
