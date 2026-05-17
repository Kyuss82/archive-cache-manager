/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Right-click "Fetch PSP Updates..." — PSP counterpart of FetchPs3UpdatesMenuItem.
 * Shares the same Ps3UpdateWindow UI, with a PSP-specific SonyUpdateContext (its
 * own cache root + offline flag from Config.PspUpdate*). Sony's update endpoint is
 * identical for PS3 and PSP titles (`a0.ww.np.dl.playstation.net/tpl/np/...`), only
 * the staging path-transform differs (strip USRDIR/CONTENT/ for PSP).
 */
using System;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;

namespace ArchiveCacheManager
{
    class FetchPspUpdatesMenuItem : IGameMenuItemPlugin
    {
        public bool SupportsMultipleGames => false;
        public string Caption => "Fetch PSP Updates...";
        public Image IconImage => Resources.icon16x16;
        public bool ShowInLaunchBox => true;
        public bool ShowInBigBox => false;

        public bool GetIsValidForGame(IGame selectedGame) =>
            selectedGame != null && Config.MatchesPspPkgPlatform(selectedGame.Platform);

        public bool GetIsValidForGames(IGame[] selectedGames) => false;

        public void OnSelected(IGame[] selectedGames) { }

        public void OnSelected(IGame selectedGame)
        {
            string archivePath = !string.IsNullOrWhiteSpace(selectedGame.ApplicationPath)
                ? PathUtils.GetAbsolutePath(selectedGame.ApplicationPath)
                : null;

            string titleId = TryInferTitleId(archivePath);
            string outputDir = !string.IsNullOrWhiteSpace(Config.PspUpdateOutputPath)
                ? Config.PspUpdateOutputPath
                : (archivePath != null ? Path.GetDirectoryName(archivePath) : PathUtils.GetLaunchBoxRootPath());

            using (var window = new Ps3UpdateWindow(titleId, outputDir, SonyUpdateContext.ForPsp()))
            {
                window.ShowDialog();
            }
        }

        private static string TryInferTitleId(string archivePath)
        {
            if (string.IsNullOrWhiteSpace(archivePath) || !File.Exists(archivePath)) return null;
            try
            {
                string ext = Path.GetExtension(archivePath);
                if (string.Equals(ext, ".pkg", StringComparison.OrdinalIgnoreCase))
                {
                    using (var fs = File.OpenRead(archivePath)) return ReadTitleIdFromPkgStream(fs);
                }
                if (string.Equals(ext, ".zip", StringComparison.OrdinalIgnoreCase))
                {
                    using (var zip = ZipFile.OpenRead(archivePath))
                    {
                        var pkg = zip.Entries.FirstOrDefault(e => e.FullName.EndsWith(".pkg", StringComparison.OrdinalIgnoreCase));
                        if (pkg != null) { using (var s = pkg.Open()) return ReadTitleIdFromPkgStream(s); }
                    }
                }
                if (string.Equals(ext, ".7z", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(ext, ".rar", StringComparison.OrdinalIgnoreCase))
                {
                    string tempDir = Path.Combine(Path.GetTempPath(), "ACM_PspUpdInfer_" + Guid.NewGuid().ToString("N"));
                    try
                    {
                        Directory.CreateDirectory(tempDir);
                        var z = new Zip();
                        if (!z.Extract(archivePath, tempDir, new[] { "*.pkg" })) return null;
                        string p = Directory.GetFiles(tempDir, "*.pkg", SearchOption.AllDirectories).FirstOrDefault();
                        if (p != null) { using (var fs = File.OpenRead(p)) return ReadTitleIdFromPkgStream(fs); }
                    }
                    finally { try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); } catch { } }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(string.Format("FetchPspUpdatesMenuItem: TITLE_ID inference failed for {0}: {1}", archivePath, ex.Message));
            }
            return null;
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
            if (header[0] != 0x7F || header[1] != (byte)'P' || header[2] != (byte)'K' || header[3] != (byte)'G') return null;
            int end = 0x30;
            while (end < 0x30 + 48 && header[end] != 0) end++;
            string cid = Encoding.ASCII.GetString(header, 0x30, end - 0x30);
            int dash = cid.IndexOf('-');
            int under = dash >= 0 ? cid.IndexOf('_', dash + 1) : -1;
            return dash >= 0 && under > dash + 1 ? cid.Substring(dash + 1, under - dash - 1) : null;
        }
    }
}
