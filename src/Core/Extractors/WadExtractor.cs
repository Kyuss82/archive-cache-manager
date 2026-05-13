/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Part of the Wii U / 3DS / Wii / DSi on-launch packager additions to the
 * Archive Cache Manager plugin (original by fraganator, LGPL 2.1). This file
 * is licensed under LGPL 2.1 or (at your option) any later version.
 */
using System.IO;
using System.Linq;

namespace ArchiveCacheManager
{
    /// <summary>
    /// On-launch wrapper that builds Wii .wad files inside the plugin cache from a CDN-dump archive.
    /// Underlying pipeline is WadStaging (Sharpii-NetCore). When the archive carries multiple TMDs
    /// (base + updates), every successful WAD is kept in the cache and the highest-version one is
    /// flagged as SelectedFile so LaunchBox launches it as the primary.
    /// </summary>
    public class WadExtractor : Extractor
    {
        public override string Name() => "Wii WAD";

        public override string GetExtractorPath() => SharpiiWad.GetExecutablePath();

        public static bool SupportedType(string archivePath) => Zip.SupportedType(archivePath);

        public override long GetSize(string archivePath, string fileInArchive = null)
        {
            return DiskUtils.GetFileSize(archivePath);
        }

        public override string[] List(string archivePath)
        {
            string baseName = Path.GetFileNameWithoutExtension(archivePath);
            return new[] { baseName + ".wad" };
        }

        public override bool Extract(string archivePath, string cachePath, string[] includeList = null, string[] excludeList = null)
        {
            string baseName = Path.GetFileNameWithoutExtension(archivePath);

            var results = WadStaging.BuildFromGameArchive(archivePath, cachePath, baseName);
            var successful = results.Where(r => r != null && r.Success && !string.IsNullOrEmpty(r.OutputPath) && File.Exists(r.OutputPath)).ToList();

            if (successful.Count == 0)
            {
                string detail = results.FirstOrDefault(r => !string.IsNullOrEmpty(r?.ErrorMessage))?.ErrorMessage ?? "<no detail>";
                Logger.Log(string.Format("WadExtractor: build failed: {0}", detail));
                return false;
            }

            var primary = successful.OrderByDescending(r => r.TmdVersion).First();
            Logger.Log(string.Format("WadExtractor: primary WAD = {0} (TMD v{1})", primary.OutputPath, primary.TmdVersion));

            // For multi-TMD archives we keep all built WADs (base + updates) in the cache, but Dolphin
            // launches a single file. Persist the highest-version WAD as SelectedFile so the fake-7z
            // listing later in this launch — and SaveGameInfo on subsequent launches — both pick it.
            if (successful.Count > 1)
            {
                string selected = Path.GetFileName(primary.OutputPath);
                LaunchInfo.Game.SelectedFile = selected;
                LaunchInfo.Game.Save();
                GameIndex.SetSelectedFile(LaunchInfo.Game.GameId, selected);
            }

            return true;
        }
    }
}
