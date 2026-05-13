/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Part of the Wii U / 3DS / Wii / DSi on-launch packager additions to the
 * Archive Cache Manager plugin (original by fraganator, LGPL 2.1). This file
 * is licensed under LGPL 2.1 or (at your option) any later version.
 */
using System.IO;

namespace ArchiveCacheManager
{
    /// <summary>
    /// On-launch wrapper that converts a Wii U CDN-dump archive (.zip/.7z/.rar) into a Cemu .wua
    /// inside the plugin cache. Underlying pipeline is WiiuStaging (CDecrypt + zarchive).
    /// </summary>
    public class WiiuWuaExtractor : Extractor
    {
        public override string Name() => "Wii U WUA";

        public override bool AlwaysCache => true;

        public override string GetExtractorPath() => CDecryptInvoker.GetExecutablePath();

        public static bool SupportedType(string archivePath) => Zip.SupportedType(archivePath);

        public override long GetSize(string archivePath, string fileInArchive = null)
        {
            return DiskUtils.GetFileSize(archivePath);
        }

        public override string[] List(string archivePath)
        {
            string baseName = Path.GetFileNameWithoutExtension(archivePath);
            return new[] { baseName + ".wua" };
        }

        public override bool Extract(string archivePath, string cachePath, string[] includeList = null, string[] excludeList = null)
        {
            string baseName = Path.GetFileNameWithoutExtension(archivePath);

            bool prevPackAsWua = Config.WiiuPackAsWua;
            Config.WiiuPackAsWua = true;

            WiiuBuildResult result;
            try
            {
                result = WiiuStaging.BuildFromGameArchive(archivePath, cachePath, baseName);
            }
            finally
            {
                Config.WiiuPackAsWua = prevPackAsWua;
            }

            if (!result.Success)
            {
                Logger.Log(string.Format("WiiuWuaExtractor: build failed: {0}", result.ErrorMessage ?? "<no detail>"));
                return false;
            }

            string expected = Path.Combine(cachePath, baseName + ".wua");
            if (!File.Exists(expected))
            {
                Logger.Log(string.Format(
                    "WiiuWuaExtractor: build reported success but no .wua at {0}. " +
                    "This usually means zarchive.exe is missing from the Extractors folder.",
                    expected));
                return false;
            }

            return true;
        }
    }
}
