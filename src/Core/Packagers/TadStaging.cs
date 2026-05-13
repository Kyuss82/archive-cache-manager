using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ArchiveCacheManager
{
    public class TadBuildResult
    {
        public string TmdSourceName;
        public int TmdVersion;
        public string OutputPath;
        public bool Success;
        public string ErrorMessage;
    }

    public static class TadStaging
    {
        private static readonly Regex ContentNameRegex = new Regex(@"^[0-9a-fA-F]{8}$", RegexOptions.Compiled);

        public static List<TadBuildResult> BuildFromGameArchive(string archivePath, string outputBaseDir, string baseName)
        {
            var results = new List<TadBuildResult>();

            if (!File.Exists(archivePath))
            {
                results.Add(new TadBuildResult { Success = false, ErrorMessage = string.Format("Archive not found: {0}", archivePath) });
                return results;
            }

            string tempDir = Path.Combine(Path.GetTempPath(), "ACM_TadBuild_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                var extractor = new Zip();
                if (!extractor.Extract(archivePath, tempDir))
                {
                    results.Add(new TadBuildResult { Success = false, ErrorMessage = string.Format("Failed to extract archive: {0}", archivePath) });
                    return results;
                }

                string workDir = ResolveWorkDir(tempDir);

                var tmdFiles = DiscoverTmds(workDir);
                if (tmdFiles.Count == 0)
                {
                    results.Add(new TadBuildResult { Success = false, ErrorMessage = "No TMD found in archive (expected 'tmd', 'tmd.<version>' or 'title.tmd')." });
                    return results;
                }

                NormalizeContents(workDir);

                WiiuParsedTmd anyTmd;
                try
                {
                    anyTmd = WiiuTmd.Parse(tmdFiles[0].Path);
                }
                catch (Exception ex)
                {
                    results.Add(new TadBuildResult { Success = false, ErrorMessage = string.Format("Failed to parse TMD: {0}", ex.Message) });
                    return results;
                }

                string titleId = anyTmd.TitleId.ToString("x16");
                Logger.Log(string.Format("TAD build: title ID = {0}, TMD count = {1}", titleId, tmdFiles.Count));

                string ticketPath = LocateTicket(workDir);
                if (ticketPath == null)
                {
                    ticketPath = Path.Combine(workDir, "cetk");
                    string cached = TryGetCachedTicket(titleId);
                    if (cached != null)
                    {
                        Logger.Log(string.Format("TAD build: ticket cache hit for {0}", titleId));
                        File.Copy(cached, ticketPath, true);
                    }
                    else
                    {
                        Logger.Log(string.Format("TAD build: ticket missing, downloading from DSi NUS for {0}", titleId));
                        if (!NusFetcher.DownloadTwlTicket(titleId, ticketPath))
                        {
                            string err = string.Format(
                                "Ticket (cetk) is missing and could not be downloaded from DSi NUS for title {0}.",
                                titleId);
                            foreach (var tmd in tmdFiles)
                            {
                                results.Add(new TadBuildResult
                                {
                                    TmdSourceName = tmd.Name,
                                    TmdVersion = tmd.Version,
                                    Success = false,
                                    ErrorMessage = err
                                });
                            }
                            return results;
                        }
                        TryStoreCachedTicket(titleId, ticketPath);
                    }
                }

                byte[] ticketBytes = TicketUtils.TruncateToBody(File.ReadAllBytes(ticketPath));

                Directory.CreateDirectory(outputBaseDir);
                bool multipleTmds = tmdFiles.Count > 1;

                foreach (var tmd in tmdFiles)
                {
                    var result = new TadBuildResult
                    {
                        TmdSourceName = tmd.Name,
                        TmdVersion = tmd.Version,
                    };

                    WiiuParsedTmd parsedTmd;
                    try
                    {
                        parsedTmd = WiiuTmd.Parse(tmd.Path);
                    }
                    catch (Exception ex)
                    {
                        result.Success = false;
                        result.ErrorMessage = string.Format("Failed to parse {0}: {1}", tmd.Name, ex.Message);
                        results.Add(result);
                        continue;
                    }

                    var contents = MapContentsToTmd(workDir, parsedTmd);
                    if (contents.Count == 0)
                    {
                        result.Success = false;
                        result.ErrorMessage = string.Format("No content (.app) files matching {0} were found in the archive.", tmd.Name);
                        results.Add(result);
                        continue;
                    }

                    byte[] rawTmd = File.ReadAllBytes(tmd.Path);
                    byte[] tmdBytes = parsedTmd.BodySize > 0 && parsedTmd.BodySize < rawTmd.Length
                        ? rawTmd.AsSpan(0, parsedTmd.BodySize).ToArray()
                        : rawTmd;

                    string outName = multipleTmds
                        ? string.Format("{0}.v{1}.tad", baseName, tmd.Version)
                        : string.Format("{0}.tad", baseName);
                    result.OutputPath = Path.Combine(outputBaseDir, outName);

                    try
                    {
                        TadBuilder.Build(result.OutputPath, tmdBytes, ticketBytes, contents, footer: null);
                        result.Success = true;
                    }
                    catch (Exception ex)
                    {
                        result.Success = false;
                        result.ErrorMessage = string.Format("TAD assembly failed: {0}", ex.Message);
                    }

                    results.Add(result);
                }

                return results;
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString(), Logger.LogLevel.Exception);
                results.Add(new TadBuildResult { Success = false, ErrorMessage = ex.Message });
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
                        || string.Equals(n, "title.tmd", StringComparison.OrdinalIgnoreCase)
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

        private static void NormalizeContents(string workDir)
        {
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
        }

        private static int ParseTmdVersion(string fileName)
        {
            if (string.Equals(fileName, "tmd", StringComparison.OrdinalIgnoreCase)) return 0;
            if (string.Equals(fileName, "title.tmd", StringComparison.OrdinalIgnoreCase)) return 0;
            int dot = fileName.IndexOf('.');
            if (dot < 0 || dot == fileName.Length - 1) return 0;
            return int.TryParse(fileName.Substring(dot + 1), out int v) ? v : 0;
        }

        private static string LocateTicket(string workDir)
        {
            foreach (string name in new[] { "cetk", "title.tik", "ticket" })
            {
                string p = Path.Combine(workDir, name);
                if (File.Exists(p)) return p;
            }
            return null;
        }

        private static List<TadContentEntry> MapContentsToTmd(string workDir, WiiuParsedTmd tmd)
        {
            var list = new List<TadContentEntry>();
            foreach (var c in tmd.Contents)
            {
                string fileName = string.Format("{0:x8}.app", c.Id);
                string path = FindFileCaseInsensitive(workDir, fileName);
                if (path == null) continue;
                list.Add(new TadContentEntry { Index = c.Index, AppPath = path, Size = c.Size });
            }
            return list;
        }

        private static string FindFileCaseInsensitive(string dir, string name)
        {
            foreach (string p in Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly))
            {
                if (string.Equals(Path.GetFileName(p), name, StringComparison.OrdinalIgnoreCase))
                    return p;
            }
            return null;
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
                Logger.Log(string.Format("Failed to cache DSi ticket for {0}: {1}", titleId, ex.Message));
            }
        }

        private static string ResolveCetkCacheDir()
        {
            string configured = Config.TadCetkCachePath;
            if (string.IsNullOrWhiteSpace(configured)) return null;
            return PathUtils.GetAbsolutePath(configured);
        }

        private static void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path)) Directory.Delete(path, true);
            }
            catch (Exception ex)
            {
                Logger.Log(string.Format("Failed to delete dir {0}: {1}", path, ex.Message));
            }
        }
    }
}
