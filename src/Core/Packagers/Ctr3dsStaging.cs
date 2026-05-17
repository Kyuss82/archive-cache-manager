/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * LaunchBox-facing helper around Ctr3dsDecryptor. Handles the "input may be a
 * .zip/.7z/.rar containing a .3ds" case by extracting the inner ROM first, then
 * piping it through Ctr3dsDecryptor to produce a decrypted .3ds in the output
 * folder. Mirrors the WadStaging / Ps3PkgStaging pattern.
 */
using System;
using System.IO;
using System.Linq;

namespace ArchiveCacheManager
{
    public static class Ctr3dsStaging
    {
        public static Ctr3dsDecryptResult DecryptFromGameArchive(string archivePath, string outputDir, string baseName)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "ACM_Ctr3dsStage_" + Guid.NewGuid().ToString("N"));
            string workInput = archivePath;
            bool cleanup = false;

            try
            {
                string ext = Path.GetExtension(archivePath);
                bool isArchive = string.Equals(ext, ".zip", StringComparison.OrdinalIgnoreCase)
                              || string.Equals(ext, ".7z",  StringComparison.OrdinalIgnoreCase)
                              || string.Equals(ext, ".rar", StringComparison.OrdinalIgnoreCase);

                if (isArchive)
                {
                    Directory.CreateDirectory(tempDir);
                    cleanup = true;
                    var zip = new Zip();
                    if (!zip.Extract(archivePath, tempDir, new[] { "*.3ds", "*.cci" }))
                    {
                        return new Ctr3dsDecryptResult { Success = false, ErrorMessage = "Failed to extract .3ds from source archive." };
                    }
                    string rom = Directory.GetFiles(tempDir, "*.3ds", SearchOption.AllDirectories).FirstOrDefault()
                              ?? Directory.GetFiles(tempDir, "*.cci", SearchOption.AllDirectories).FirstOrDefault();
                    if (rom == null)
                    {
                        return new Ctr3dsDecryptResult { Success = false, ErrorMessage = "Archive does not contain a .3ds / .cci file." };
                    }
                    workInput = rom;
                }
                else if (!string.Equals(ext, ".3ds", StringComparison.OrdinalIgnoreCase)
                      && !string.Equals(ext, ".cci", StringComparison.OrdinalIgnoreCase))
                {
                    return new Ctr3dsDecryptResult { Success = false, ErrorMessage = string.Format("Unsupported input extension '{0}'.", ext) };
                }

                Directory.CreateDirectory(outputDir);
                string outputPath = Path.Combine(outputDir, baseName + ".3ds");
                return Ctr3dsDecryptor.Decrypt(workInput, outputPath, msg => Logger.Log("Ctr3dsStaging: " + msg));
            }
            finally
            {
                if (cleanup)
                {
                    try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); } catch { }
                }
            }
        }
    }
}
