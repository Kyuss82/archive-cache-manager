/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Helper for the offline `local_pkg_index.json` consumers. `LocalPkgIndexer`
 * records each PKG either as a bare file path (`PkgPath`) or as a wrapper-archive
 * reference (`ArchivePath` + `ArchiveEntry`) — typical when the user mirrors
 * NPS Browser scene packs which ship one .pkg + one .rap inside a .zip.
 *
 * The installers (`Ps3UpdateInstaller`, `Ps3DlcInstaller`) need an actual file
 * on disk to feed to PsPkgUnpacker / PsPkgReader; this helper materialises that
 * path on demand, extracting the .pkg into a temp folder when necessary, and
 * tells the caller whether to clean up afterwards.
 *
 * For the companion .rap, `ResolveRapBytes` returns the binary directly
 * (16 bytes) without staging a temp file — the caller writes it wherever it
 * needs to go (exdata, PSP/LICENSE, …).
 */
using System;
using System.IO;
using System.IO.Compression;

namespace ArchiveCacheManager
{
    public static class LocalManifestResolver
    {
        /// <summary>
        /// Returns a filesystem path containing the .pkg referenced by <paramref name="entry"/>.
        /// When the entry lives in a wrapper archive, the .pkg is extracted to a unique temp
        /// folder and <paramref name="needsCleanup"/> is set so the caller calls
        /// <see cref="CleanupExtracted"/> after it's done.
        /// </summary>
        public static string ResolvePkgPath(LocalPkgEntry entry, out bool needsCleanup)
        {
            needsCleanup = false;
            if (entry == null) return null;

            // Bare .pkg on disk — happy path.
            if (!string.IsNullOrWhiteSpace(entry.PkgPath) && File.Exists(entry.PkgPath))
            {
                return entry.PkgPath;
            }

            // Wrapper archive — only .zip is fully supported (System.IO.Compression).
            if (!string.IsNullOrWhiteSpace(entry.ArchivePath) && File.Exists(entry.ArchivePath)
                && !string.IsNullOrWhiteSpace(entry.ArchiveEntry))
            {
                string ext = Path.GetExtension(entry.ArchivePath);
                if (!string.Equals(ext, ".zip", StringComparison.OrdinalIgnoreCase))
                {
                    throw new NotSupportedException(string.Format(
                        "Wrapper format {0} is not supported (extract {1} to .zip or to a bare .pkg).",
                        ext, entry.ArchivePath));
                }
                string tempDir = Path.Combine(Path.GetTempPath(), "ACM_LocalPkgExtract_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDir);
                string outPath = Path.Combine(tempDir, Path.GetFileName(entry.ArchiveEntry));
                using (var fs = new FileStream(entry.ArchivePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var zip = new ZipArchive(fs, ZipArchiveMode.Read))
                {
                    var ze = zip.GetEntry(entry.ArchiveEntry);
                    if (ze == null)
                        throw new FileNotFoundException(string.Format("Entry '{0}' not found in {1}.", entry.ArchiveEntry, entry.ArchivePath));
                    ze.ExtractToFile(outPath, overwrite: true);
                }
                needsCleanup = true;
                Logger.Log(string.Format("LocalManifestResolver: extracted {0}!{1} → {2} ({3:N0} bytes).",
                    entry.ArchivePath, entry.ArchiveEntry, outPath, new FileInfo(outPath).Length));
                return outPath;
            }

            return null;
        }

        /// <summary>Loads the 16-byte RAP for an entry, either from <c>RapPath</c> or from <c>RapArchiveEntry</c>.</summary>
        public static byte[] ResolveRapBytes(LocalPkgEntry entry)
        {
            if (entry == null) return null;

            if (!string.IsNullOrWhiteSpace(entry.RapPath) && File.Exists(entry.RapPath))
            {
                try { return File.ReadAllBytes(entry.RapPath); }
                catch (Exception ex) { Logger.Log(string.Format("LocalManifestResolver: failed to read RAP {0}: {1}", entry.RapPath, ex.Message)); }
            }

            if (!string.IsNullOrWhiteSpace(entry.ArchivePath) && File.Exists(entry.ArchivePath)
                && !string.IsNullOrWhiteSpace(entry.RapArchiveEntry)
                && string.Equals(Path.GetExtension(entry.ArchivePath), ".zip", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using (var fs = new FileStream(entry.ArchivePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (var zip = new ZipArchive(fs, ZipArchiveMode.Read))
                    {
                        var re = zip.GetEntry(entry.RapArchiveEntry);
                        if (re != null)
                        {
                            using (var es = re.Open())
                            using (var ms = new MemoryStream())
                            {
                                es.CopyTo(ms);
                                return ms.ToArray();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(string.Format("LocalManifestResolver: failed to read RAP entry {0}!{1}: {2}", entry.ArchivePath, entry.RapArchiveEntry, ex.Message));
                }
            }
            return null;
        }

        public static void CleanupExtracted(string extractedPkgPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(extractedPkgPath)) return;
                string dir = Path.GetDirectoryName(extractedPkgPath);
                if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
            }
            catch (Exception ex)
            {
                Logger.Log(string.Format("LocalManifestResolver: failed to cleanup {0}: {1}", extractedPkgPath, ex.Message));
            }
        }
    }
}
