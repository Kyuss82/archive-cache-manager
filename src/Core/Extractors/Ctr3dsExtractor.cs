/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * On-launch decryption wrapper for 3DS .3ds / .cci ROMs. Produces a decrypted
 * .3ds inside the plugin cache so Azahar / Citra / Lime3DS load it directly,
 * regardless of whether they have aes_keys.txt installed. Mirrors the PS3Dec /
 * Ps3PkgExtractor / WadExtractor pattern.
 */
using System.IO;
using System.Linq;

namespace ArchiveCacheManager
{
    public class Ctr3dsExtractor : Extractor
    {
        public override string Name() => "3DS Decrypt";

        public override bool AlwaysCache => true;

        public override string GetExtractorPath() => null;

        public static bool SupportedType(string archivePath)
        {
            return PathUtils.HasExtension(archivePath, new[] { ".3ds", ".cci", ".zip", ".7z", ".rar" });
        }

        public override long GetSize(string archivePath, string fileInArchive = null)
        {
            // Decrypted output is the same size as the source. For zipped input the decompressed
            // size is queried via the underlying Zip extractor.
            string ext = Path.GetExtension(archivePath).ToLowerInvariant();
            if (ext == ".zip" || ext == ".7z" || ext == ".rar")
            {
                return new Zip().GetSize(archivePath, fileInArchive);
            }
            return new FileInfo(archivePath).Length;
        }

        public override string[] List(string archivePath)
        {
            string baseName = Path.GetFileNameWithoutExtension(archivePath);
            return new[] { baseName + ".3ds" };
        }

        public override bool Extract(string archivePath, string cachePath, string[] includeList = null, string[] excludeList = null)
        {
            string baseName = Path.GetFileNameWithoutExtension(archivePath);
            var result = Ctr3dsStaging.DecryptFromGameArchive(archivePath, cachePath, baseName);

            if (!result.Success)
            {
                Logger.Log(string.Format("Ctr3dsExtractor: decryption failed: {0}", result.ErrorMessage ?? "<no detail>"));
                return false;
            }

            Logger.Log(string.Format("Ctr3dsExtractor: decrypted {0} partition(s), {1} skipped → {2}",
                result.PartitionsDecrypted, result.PartitionsSkipped, result.OutputPath));
            return true;
        }
    }
}
