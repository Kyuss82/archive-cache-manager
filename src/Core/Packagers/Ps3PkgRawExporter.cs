/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * "Export PS3 PKG" pipeline: takes a `.zip`/`.7z`/`.rar` carrying one or more
 * Sony-signed PS3 `.pkg` files (+ optional `.rap` license), pulls the bare
 * `.pkg` and `.rap` out raw — no AES-CTR decryption — and drops them in the
 * caller-supplied output folder. This is the NoPayStation Browser / pkg2zip
 * "i just want the installable files" workflow: the user then opens RPCS3 →
 * File → Install Packages/Raps, points it at the resulting files, and RPCS3
 * itself handles the NPDRM dance against `dev_hdd0/game/&lt;TID&gt;/`.
 *
 * That makes a big difference for NPDRM titles like *Amy* (NPEB00768): our
 * on-launch flow's decrypted-folder-in-cache approach doesn't satisfy RPCS3's
 * NPDRM klicensee lookup (the eboot decrypts to byte-identical output, but
 * RPCS3 wants the title under its own `dev_hdd0/game/&lt;TID&gt;/` to find the
 * companion rap). Letting RPCS3 install the .pkg sidesteps the whole problem.
 *
 * For the `.rap`:
 *   • If the source archive carries a `.rap`, that one wins (it's whatever the
 *     scene release packaged).
 *   • Otherwise we look the content id up in the NPS DB (`NpsDb`) and synthesise
 *     a `.rap` from the hex column there.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ArchiveCacheManager
{
    public class Ps3PkgRawExportResult
    {
        public bool Success;
        public string ErrorMessage;

        public string TitleId;              // first PKG's title id (e.g. "NPEB00768")
        public string ContentId;            // first PKG's content id (e.g. "EP4295-NPEB00768_00-HDDBOOTAMY000001")
        public List<string> PkgPaths    = new List<string>();     // absolute paths of exported .pkg files
        public List<string> RapPaths    = new List<string>();     // absolute paths of exported .rap files
        public string RapSource;            // "archive" / "NPS DB" / null when there is none
    }

    public static class Ps3PkgRawExporter
    {
        /// <summary>
        /// Extract every .pkg + .rap from <paramref name="archivePath"/> into
        /// <paramref name="outputDir"/> without decrypting anything.
        /// Output filenames are based on the PKG's content id (so multiple PKGs
        /// don't collide) — e.g. <c>EP4295-NPEB00768_00-…pkg</c> +
        /// <c>EP4295-NPEB00768_00-….rap</c>. Returns immediately if the archive
        /// has no .pkg in it.
        /// </summary>
        public static Ps3PkgRawExportResult ExportFromGameArchive(string archivePath, string outputDir)
        {
            var result = new Ps3PkgRawExportResult();

            if (!File.Exists(archivePath))
            {
                result.ErrorMessage = string.Format("Archive not found: {0}", archivePath);
                return result;
            }

            string tempDir = Path.Combine(Path.GetTempPath(), "ACM_Ps3PkgRawExport_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                if (LooksLikePkg(archivePath))
                {
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

                var rapsInArchive = Directory.GetFiles(tempDir, "*.rap", SearchOption.AllDirectories);

                Directory.CreateDirectory(outputDir);

                foreach (string srcPkg in pkgs)
                {
                    PsParsedPkg parsed;
                    try
                    {
                        parsed = PsPkgReader.Parse(srcPkg);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(string.Format("Ps3PkgRawExporter: parse failed for {0}: {1}", Path.GetFileName(srcPkg), ex), Logger.LogLevel.Exception);
                        continue;
                    }

                    if (parsed.Header.PkgType != 0x0001)
                    {
                        Logger.Log(string.Format(
                            "Ps3PkgRawExporter: skipping {0} (type=0x{1:X4}, not a PS3 PKG).",
                            Path.GetFileName(srcPkg), parsed.Header.PkgType));
                        continue;
                    }

                    string contentId = parsed.Header.ContentId ?? Path.GetFileNameWithoutExtension(srcPkg);
                    string tid = parsed.TitleId ?? DeriveTitleId(contentId);
                    if (string.IsNullOrEmpty(result.TitleId))   result.TitleId   = tid;
                    if (string.IsNullOrEmpty(result.ContentId)) result.ContentId = contentId;

                    string destPkg = Path.Combine(outputDir, SafeFile(contentId) + ".pkg");
                    File.Copy(srcPkg, destPkg, true);
                    result.PkgPaths.Add(destPkg);
                    Logger.Log(string.Format("Ps3PkgRawExporter: {0} → {1} ({2:N0} bytes).",
                        Path.GetFileName(srcPkg), destPkg, new FileInfo(destPkg).Length));

                    string rapPath = TryExportRap(rapsInArchive, contentId, outputDir, out string rapSource);
                    if (!string.IsNullOrEmpty(rapPath))
                    {
                        result.RapPaths.Add(rapPath);
                        result.RapSource = rapSource;
                    }
                }

                result.Success = result.PkgPaths.Count > 0;
                if (!result.Success && string.IsNullOrEmpty(result.ErrorMessage))
                {
                    result.ErrorMessage = "Archive parsed but no PS3 .pkg files were exported (see log).";
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
                TryDeleteDirectory(tempDir);
            }
        }

        private static string TryExportRap(string[] rapsInArchive, string contentId, string outputDir, out string source)
        {
            source = null;
            if (string.IsNullOrEmpty(contentId)) return null;

            // 1. archive-bundled .rap with matching content id
            string archiveRap = rapsInArchive.FirstOrDefault(p =>
                string.Equals(Path.GetFileNameWithoutExtension(p), contentId, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(archiveRap))
            {
                string destRap = Path.Combine(outputDir, contentId + ".rap");
                File.Copy(archiveRap, destRap, true);
                source = "archive";
                Logger.Log(string.Format("Ps3PkgRawExporter: bundled RAP for {0} → {1}.", contentId, destRap));
                return destRap;
            }

            // 2. NPS DB rap hex
            var nps = NpsDb.LookupByContentId(contentId);
            if (nps == null) nps = NpsDb.LookupByTitleId(DeriveTitleId(contentId));
            if (nps != null && !string.IsNullOrWhiteSpace(nps.RapHex))
            {
                try
                {
                    byte[] rapBytes = HexToBytes(nps.RapHex);
                    if (rapBytes.Length == 16)
                    {
                        string destRap = Path.Combine(outputDir, contentId + ".rap");
                        File.WriteAllBytes(destRap, rapBytes);
                        source = "NPS DB";
                        Logger.Log(string.Format("Ps3PkgRawExporter: NPS RAP for {0} → {1}.", contentId, destRap));
                        return destRap;
                    }
                    Logger.Log(string.Format("Ps3PkgRawExporter: NPS RAP for {0} has wrong byte length ({1}); skipped.", contentId, rapBytes.Length));
                }
                catch (Exception ex)
                {
                    Logger.Log(string.Format("Ps3PkgRawExporter: NPS RAP decode failed for {0}: {1}", contentId, ex.Message));
                }
            }

            Logger.Log(string.Format("Ps3PkgRawExporter: no RAP available for {0} — RPCS3 will refuse to boot NPDRM titles without one.", contentId));
            return null;
        }

        private static bool LooksLikePkg(string archivePath) =>
            !string.IsNullOrEmpty(archivePath) &&
            string.Equals(Path.GetExtension(archivePath), ".pkg", StringComparison.OrdinalIgnoreCase);

        private static string DeriveTitleId(string contentId)
        {
            if (string.IsNullOrWhiteSpace(contentId)) return null;
            int dash = contentId.IndexOf('-');
            int under = dash >= 0 ? contentId.IndexOf('_', dash + 1) : -1;
            if (dash >= 0 && under > dash + 1) return contentId.Substring(dash + 1, under - dash - 1);
            return null;
        }

        private static string SafeFile(string name)
        {
            if (string.IsNullOrEmpty(name)) return "output";
            foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            return name;
        }

        private static byte[] HexToBytes(string hex)
        {
            hex = hex.Trim();
            if (hex.Length % 2 != 0) throw new FormatException("RAP hex string has odd length.");
            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return bytes;
        }

        private static void TryDeleteDirectory(string path)
        {
            try { if (Directory.Exists(path)) Directory.Delete(path, true); }
            catch (Exception ex) { Logger.Log(string.Format("Ps3PkgRawExporter: failed to delete {0}: {1}", path, ex.Message)); }
        }
    }
}
