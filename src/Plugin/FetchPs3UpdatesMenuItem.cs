/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Right-click "Fetch PS3 Updates..." menu item — queries Sony's update server
 * for the selected game's TITLE_ID and downloads any listed patches.
 *
 * Title-ID inference is best-effort: if the game's ApplicationPath is a .pkg
 * or a .zip carrying a .pkg, we read the first 192 bytes (the unencrypted PKG
 * header) and extract content_id → TITLE_ID. Otherwise the dialog opens with
 * an empty field for the user to type in.
 */
using System;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows.Forms;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;

namespace ArchiveCacheManager
{
    class FetchPs3UpdatesMenuItem : IGameMenuItemPlugin
    {
        public bool SupportsMultipleGames => false;
        public string Caption => "Fetch PS3 Updates...";
        public Image IconImage => Resources.icon16x16;
        public bool ShowInLaunchBox => true;
        public bool ShowInBigBox => false;

        public bool GetIsValidForGame(IGame selectedGame) =>
            selectedGame != null && Config.MatchesPs3PkgPlatform(selectedGame.Platform);

        public bool GetIsValidForGames(IGame[] selectedGames) => false;

        public void OnSelected(IGame[] selectedGames) { }

        public void OnSelected(IGame selectedGame)
        {
            string archivePath = !string.IsNullOrWhiteSpace(selectedGame.ApplicationPath)
                ? PathUtils.GetAbsolutePath(selectedGame.ApplicationPath)
                : null;

            string titleId = TryInferTitleId(archivePath);
            string outputDir = !string.IsNullOrWhiteSpace(Config.Ps3UpdateOutputPath)
                ? Config.Ps3UpdateOutputPath
                : (archivePath != null ? Path.GetDirectoryName(archivePath) : PathUtils.GetLaunchBoxRootPath());

            using (var window = new Ps3UpdateWindow(titleId, outputDir))
            {
                window.ShowDialog();
            }
        }

        /// <summary>
        /// Derive the 9-character TITLE_ID for the selected game. Tries, in order:
        ///   1. The source archive (PKG header for .pkg / .zip / .7z / .rar; PARAM.SFO for .iso).
        ///   2. The plugin's archive cache — useful when the source ISO is PSN-encrypted and
        ///      therefore unreadable by 7-Zip's UDF support, but a prior launch has already
        ///      decrypted it via PS3Dec into the cache.
        /// Returns null if every path fails.
        /// </summary>
        private static string TryInferTitleId(string archivePath)
        {
            if (string.IsNullOrWhiteSpace(archivePath) || !File.Exists(archivePath)) return null;

            string id = TryFromSourceArchive(archivePath);
            if (!string.IsNullOrWhiteSpace(id)) return id;

            // Fallback: the source might be encrypted (e.g. PSN-style PS3 ISO needing PS3Dec).
            // The decrypted ISO often sits in the plugin's archive cache after a prior launch —
            // 7-Zip can read UDF/ISO9660 out of that without re-decryption.
            id = TryFromArchiveCache(archivePath);
            if (!string.IsNullOrWhiteSpace(id))
            {
                Logger.Log(string.Format("FetchPs3UpdatesMenuItem: TITLE_ID '{0}' recovered from plugin cache (source was unreadable).", id));
            }
            return id;
        }

        private static string TryFromSourceArchive(string archivePath)
        {
            try
            {
                string ext = Path.GetExtension(archivePath);

                if (string.Equals(ext, ".pkg", StringComparison.OrdinalIgnoreCase))
                {
                    using (var fs = File.OpenRead(archivePath))
                    {
                        return ReadTitleIdFromPkgStream(fs);
                    }
                }

                if (string.Equals(ext, ".zip", StringComparison.OrdinalIgnoreCase))
                {
                    using (var zip = ZipFile.OpenRead(archivePath))
                    {
                        var pkgEntry = zip.Entries.FirstOrDefault(e =>
                            e.FullName.EndsWith(".pkg", StringComparison.OrdinalIgnoreCase));
                        if (pkgEntry != null)
                        {
                            using (var es = pkgEntry.Open()) return ReadTitleIdFromPkgStream(es);
                        }

                        var sfoEntry = zip.Entries.FirstOrDefault(e =>
                            e.FullName.EndsWith("PARAM.SFO", StringComparison.OrdinalIgnoreCase));
                        if (sfoEntry != null)
                        {
                            using (var es = sfoEntry.Open()) return ReadTitleIdFromSfoStream(es);
                        }
                        return null;
                    }
                }

                if (string.Equals(ext, ".iso", StringComparison.OrdinalIgnoreCase))
                {
                    return ExtractSingleAndReadTitleId(archivePath, "PS3_GAME/PARAM.SFO", ReadTitleIdFromSfoFile);
                }

                if (string.Equals(ext, ".7z", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(ext, ".rar", StringComparison.OrdinalIgnoreCase))
                {
                    return ExtractSingleAndReadTitleId(archivePath, "*.pkg", ReadTitleIdFromPkgFile)
                        ?? ExtractSingleAndReadTitleId(archivePath, "PARAM.SFO", ReadTitleIdFromSfoFile);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(string.Format("FetchPs3UpdatesMenuItem: source-archive TITLE_ID inference failed for {0}: {1}", archivePath, ex.Message));
            }
            return null;
        }

        private static string TryFromArchiveCache(string archivePath)
        {
            try
            {
                string cacheDir = PathUtils.ArchiveCachePath(archivePath, checkOldFormat: true);
                if (!Directory.Exists(cacheDir)) return null;

                // 1. Bare PARAM.SFO already extracted in the cache (PKG flow).
                string sfoInCache = Directory.GetFiles(cacheDir, "PARAM.SFO", SearchOption.AllDirectories).FirstOrDefault();
                if (sfoInCache != null)
                {
                    string id = Sfo.ReadTitleId(sfoInCache);
                    if (!string.IsNullOrWhiteSpace(id)) return id;
                }

                // 2. Decrypted ISO in the cache (PS3Dec flow) — read PARAM.SFO via 7-Zip's UDF.
                foreach (string iso in Directory.GetFiles(cacheDir, "*.iso", SearchOption.TopDirectoryOnly))
                {
                    string id = ExtractSingleAndReadTitleId(iso, "PS3_GAME/PARAM.SFO", ReadTitleIdFromSfoFile);
                    if (!string.IsNullOrWhiteSpace(id)) return id;
                }

                // 3. EBOOT.BIN sitting in a USRDIR/ inside the cache (PKG flow): no SFO nearby?
                //    Some PSN PKGs ship PARAM.SFO embedded in EBOOT.BIN only — fall through.
            }
            catch (Exception ex)
            {
                Logger.Log(string.Format("FetchPs3UpdatesMenuItem: cache-fallback TITLE_ID inference failed for {0}: {1}", archivePath, ex.Message));
            }
            return null;
        }

        /// <summary>
        /// Extract a single matching entry from any 7z-supported archive (.iso/.7z/.rar) into a
        /// temp directory, hand the on-disk path to <paramref name="reader"/>, and return its
        /// result. Returns null if the entry isn't found.
        /// </summary>
        private static string ExtractSingleAndReadTitleId(string archivePath, string includePattern, Func<string, string> reader)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "ACM_Ps3UpdateInfer_" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(tempDir);
                var zip = new Zip();
                if (!zip.Extract(archivePath, tempDir, new[] { includePattern }))
                {
                    return null;
                }
                string targetFileName = Path.GetFileName(includePattern.TrimStart('*'));
                bool isPkgGlob = includePattern.EndsWith(".pkg", StringComparison.OrdinalIgnoreCase);
                string match = isPkgGlob
                    ? Directory.GetFiles(tempDir, "*.pkg", SearchOption.AllDirectories).FirstOrDefault()
                    : Directory.GetFiles(tempDir, targetFileName, SearchOption.AllDirectories).FirstOrDefault();
                return match != null ? reader(match) : null;
            }
            finally
            {
                try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); } catch { }
            }
        }

        private static string ReadTitleIdFromPkgFile(string path)
        {
            using (var fs = File.OpenRead(path)) return ReadTitleIdFromPkgStream(fs);
        }

        private static string ReadTitleIdFromSfoFile(string path) => Sfo.ReadTitleId(path);

        private static string ReadTitleIdFromSfoStream(Stream s)
        {
            using (var ms = new MemoryStream())
            {
                s.CopyTo(ms);
                var map = Sfo.Parse(ms.ToArray());
                return map.TryGetValue("TITLE_ID", out string id) ? id.Trim() : null;
            }
        }

        private static string ReadTitleIdFromPkgStream(Stream s)
        {
            byte[] header = new byte[192];
            int read = 0;
            while (read < header.Length)
            {
                int n = s.Read(header, read, header.Length - read);
                if (n <= 0) return null;
                read += n;
            }
            if (header[0] != 0x7F || header[1] != (byte)'P' || header[2] != (byte)'K' || header[3] != (byte)'G')
                return null;

            // content_id at 0x30, 48 ASCII bytes NUL-padded. Layout: "RR0000-TITLEID000_00-XXXXXXXXXXXXXXXX".
            int end = 0x30;
            while (end < 0x30 + 48 && header[end] != 0) end++;
            string contentId = System.Text.Encoding.ASCII.GetString(header, 0x30, end - 0x30);
            int dash = contentId.IndexOf('-');
            int under = dash >= 0 ? contentId.IndexOf('_', dash + 1) : -1;
            if (dash >= 0 && under > dash + 1)
            {
                return contentId.Substring(dash + 1, under - dash - 1);
            }
            return null;
        }
    }
}
