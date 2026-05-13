using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ArchiveCacheManager
{
    public class WadBuildResult
    {
        public string TmdSourceName;
        public int TmdVersion;
        public string OutputPath;
        public bool Success;
        public string ErrorMessage;
    }

    public static class WadStaging
    {
        private static readonly Regex ContentNameRegex = new Regex(@"^[0-9a-fA-F]{8}$", RegexOptions.Compiled);

        public static List<WadBuildResult> BuildFromGameArchive(string archivePath, string outputDir, string baseName)
        {
            var results = new List<WadBuildResult>();

            if (!File.Exists(archivePath))
            {
                results.Add(new WadBuildResult { Success = false, ErrorMessage = string.Format("Archive not found: {0}", archivePath) });
                return results;
            }

            if (!SharpiiWad.IsAvailable())
            {
                results.Add(new WadBuildResult { Success = false, ErrorMessage = string.Format("Sharpii-NetCore.exe not found in {0}", PathUtils.GetExtractorRootPath()) });
                return results;
            }

            string tempDir = Path.Combine(Path.GetTempPath(), "ACM_WadBuild_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                var extractor = new Zip();
                if (!extractor.Extract(archivePath, tempDir))
                {
                    results.Add(new WadBuildResult { Success = false, ErrorMessage = string.Format("Failed to extract archive: {0}", archivePath) });
                    return results;
                }

                string workDir = ResolveWorkDir(tempDir);

                var tmdFiles = DiscoverTmds(workDir);
                if (tmdFiles.Count == 0)
                {
                    results.Add(new WadBuildResult { Success = false, ErrorMessage = "No TMD file found in archive (expected 'tmd' or 'tmd.<version>')." });
                    return results;
                }

                var (normOk, normErr) = NormalizeSharedFiles(workDir);
                if (!normOk)
                {
                    results.Add(new WadBuildResult { Success = false, ErrorMessage = normErr });
                    return results;
                }

                string sharedTik = Path.Combine(workDir, "tik");
                string sharedCert = Path.Combine(workDir, "cert");

                string titleId = null;
                try
                {
                    titleId = TmdReader.ReadTitleId(tmdFiles.Last().Path);
                }
                catch (Exception ex)
                {
                    Logger.Log(string.Format("Failed to read title ID from TMD: {0}", ex.Message));
                }

                bool ticketReady = File.Exists(sharedTik);
                if (!ticketReady && !string.IsNullOrEmpty(titleId))
                {
                    string cachedTicket = TryGetCachedTicket(titleId);
                    if (cachedTicket != null)
                    {
                        Logger.Log(string.Format("Ticket cache hit for title {0} -> {1}", titleId, cachedTicket));
                        File.Copy(cachedTicket, sharedTik, true);
                        ticketReady = true;
                    }
                    else
                    {
                        Logger.Log(string.Format("Ticket missing, downloading from NUS for title {0}", titleId));
                        ticketReady = NusFetcher.DownloadTicket(titleId, sharedTik);
                        if (ticketReady)
                        {
                            TryStoreCachedTicket(titleId, sharedTik);
                        }
                    }
                }

                bool certReady = File.Exists(sharedCert);
                if (!certReady)
                {
                    string source = LocateCertSource(workDir);
                    if (source != null)
                    {
                        File.Copy(source, sharedCert, true);
                        certReady = true;
                    }
                    else
                    {
                        byte[] embedded = LoadEmbeddedCert();
                        if (embedded != null)
                        {
                            File.WriteAllBytes(sharedCert, embedded);
                            certReady = true;
                            Logger.Log("Wii build: using embedded cert.sys (no override in Extractors).");
                        }
                    }
                }

                Directory.CreateDirectory(outputDir);

                bool multipleTmds = tmdFiles.Count > 1;

                foreach (var tmd in tmdFiles)
                {
                    var result = new WadBuildResult
                    {
                        TmdSourceName = tmd.Name,
                        TmdVersion = tmd.Version,
                    };

                    string outName = multipleTmds
                        ? string.Format("{0}.v{1}.wad", baseName, tmd.Version)
                        : string.Format("{0}.wad", baseName);
                    result.OutputPath = Path.Combine(outputDir, outName);

                    if (!ticketReady)
                    {
                        result.Success = false;
                        result.ErrorMessage = string.Format(
                            "Ticket (cetk) for title {0} is missing.\r\n\r\n" +
                            "The public NUS only serves system titles (IOS, channels). Virtual Console / WiiWare titles " +
                            "are not available without a Wii Shop account and have been offline since the Shop closed.\r\n\r\n" +
                            "To fix this, add a 'cetk' file (also named 'title.tik' or '{0}.tik') inside the game's archive next to the tmd, " +
                            "or place '{0}.tik' in the WadCetkCachePath folder configured in archive-cache-manager.ini.",
                            string.IsNullOrEmpty(titleId) ? "<unknown>" : titleId);
                        results.Add(result);
                        continue;
                    }

                    if (!certReady)
                    {
                        result.Success = false;
                        result.ErrorMessage = string.Format(
                            "Wii cert file not found.\r\n\r\nPlace a 'cert' file (extracted from any complete WAD) in:\r\n{0}",
                            PathUtils.GetExtractorRootPath());
                        results.Add(result);
                        continue;
                    }

                    string stagingDir = Path.Combine(tempDir, "stage_v" + tmd.Version);
                    try
                    {
                        Directory.CreateDirectory(stagingDir);
                        StageVersion(workDir, stagingDir, tmd.Path, sharedTik, sharedCert);

                        var (ok, stdout, stderr, exitCode) = SharpiiWad.PackWad(stagingDir, result.OutputPath);
                        if (ok)
                        {
                            result.Success = true;
                        }
                        else
                        {
                            result.Success = false;
                            string detail = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
                            result.ErrorMessage = string.Format("Sharpii failed (exit {0}). {1}", exitCode, detail);
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Success = false;
                        result.ErrorMessage = ex.Message;
                        Logger.Log(ex.ToString(), Logger.LogLevel.Exception);
                    }

                    results.Add(result);
                }

                return results;
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString(), Logger.LogLevel.Exception);
                results.Add(new WadBuildResult { Success = false, ErrorMessage = ex.Message });
                return results;
            }
            finally
            {
                TryDeleteDirectory(tempDir);
            }
        }

        private static string ResolveWorkDir(string tempDir)
        {
            string[] entries = Directory.GetFileSystemEntries(tempDir);
            if (entries.Length == 1 && Directory.Exists(entries[0]))
            {
                return entries[0];
            }
            return tempDir;
        }

        private class TmdEntry
        {
            public string Path;
            public string Name;
            public int Version;
        }

        private static List<TmdEntry> DiscoverTmds(string workDir)
        {
            return Directory.GetFiles(workDir, "tmd*", SearchOption.TopDirectoryOnly)
                .Where(p =>
                {
                    string n = Path.GetFileName(p);
                    return string.Equals(n, "tmd", StringComparison.OrdinalIgnoreCase)
                        || n.StartsWith("tmd.", StringComparison.OrdinalIgnoreCase);
                })
                .Select(p => new TmdEntry
                {
                    Path = p,
                    Name = Path.GetFileName(p),
                    Version = ParseTmdVersion(Path.GetFileName(p))
                })
                .OrderBy(t => t.Version)
                .ToList();
        }

        private static (bool ok, string error) NormalizeSharedFiles(string workDir)
        {
            string cetkPath = Path.Combine(workDir, "cetk");
            string tikPath = Path.Combine(workDir, "tik");
            if (File.Exists(cetkPath) && !File.Exists(tikPath))
            {
                File.Copy(cetkPath, tikPath, true);
            }

            var contents = Directory.GetFiles(workDir, "*", SearchOption.TopDirectoryOnly)
                .Where(p => ContentNameRegex.IsMatch(Path.GetFileName(p)))
                .ToArray();

            foreach (var content in contents)
            {
                string appPath = content + ".app";
                if (!File.Exists(appPath))
                {
                    File.Move(content, appPath);
                }
            }

            if (!Directory.GetFiles(workDir, "*.app", SearchOption.TopDirectoryOnly).Any())
            {
                return (false, "No content files (.app) found in archive.");
            }

            return (true, null);
        }

        private static void StageVersion(string workDir, string stagingDir, string tmdPath, string sharedTik, string sharedCert)
        {
            foreach (string f in Directory.GetFiles(workDir, "*.app", SearchOption.TopDirectoryOnly))
            {
                File.Copy(f, Path.Combine(stagingDir, Path.GetFileName(f)), true);
            }
            File.Copy(sharedTik, Path.Combine(stagingDir, "tik"), true);
            File.Copy(sharedCert, Path.Combine(stagingDir, "cert"), true);
            File.Copy(tmdPath, Path.Combine(stagingDir, "tmd"), true);
        }

        private static int ParseTmdVersion(string fileName)
        {
            if (string.Equals(fileName, "tmd", StringComparison.OrdinalIgnoreCase)) return 0;
            int dot = fileName.IndexOf('.');
            if (dot < 0 || dot == fileName.Length - 1) return 0;
            return int.TryParse(fileName.Substring(dot + 1), out int v) ? v : 0;
        }

        private static string TryGetCachedTicket(string titleId)
        {
            string cacheDir = ResolveCetkCacheDir();
            if (cacheDir == null) return null;

            string path = Path.Combine(cacheDir, titleId + ".tik");
            return File.Exists(path) && new FileInfo(path).Length > 0 ? path : null;
        }

        private static void TryStoreCachedTicket(string titleId, string ticketPath)
        {
            string cacheDir = ResolveCetkCacheDir();
            if (cacheDir == null) return;

            try
            {
                Directory.CreateDirectory(cacheDir);
                File.Copy(ticketPath, Path.Combine(cacheDir, titleId + ".tik"), true);
            }
            catch (Exception ex)
            {
                Logger.Log(string.Format("Failed to write ticket cache for title {0}: {1}", titleId, ex.Message));
            }
        }

        private static string ResolveCetkCacheDir()
        {
            string configured = Config.WadCetkCachePath;
            if (string.IsNullOrWhiteSpace(configured)) return null;
            return PathUtils.GetAbsolutePath(configured);
        }

        private const string EmbeddedCertResourceName = "ArchiveCacheManager.Packagers.cert.sys";

        private static byte[] LoadEmbeddedCert()
        {
            var asm = typeof(WadStaging).Assembly;
            using (var stream = asm.GetManifestResourceStream(EmbeddedCertResourceName))
            {
                if (stream == null) return null;
                using (var ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    return ms.ToArray();
                }
            }
        }

        private static string LocateCertSource(string workDir)
        {
            foreach (string name in new[] { "cert.sys", "Root.cert" })
            {
                string p = Path.Combine(workDir, name);
                if (File.Exists(p)) return p;
            }

            string extractorRoot = PathUtils.GetExtractorRootPath();
            foreach (string name in new[] { "cert", "cert.sys", "Root.cert" })
            {
                string p = Path.Combine(extractorRoot, name);
                if (File.Exists(p)) return p;
            }

            return null;
        }

        private static void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(string.Format("Failed to delete temp dir {0}: {1}", path, ex.Message));
            }
        }
    }
}
