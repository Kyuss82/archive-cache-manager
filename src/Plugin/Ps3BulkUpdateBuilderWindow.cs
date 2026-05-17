/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Bulk PS3 update-DB builder. Enumerates every game in the LaunchBox library whose
 * platform matches Ps3PkgPlatform, derives the TITLE_ID from each archive (reusing
 * FetchPs3UpdatesMenuItem's inference helpers via reflection-free duplication
 * — kept here so this window has zero plugin dependencies on the menu item), then
 * for each title queries Sony, caches the manifest, and (optionally) downloads
 * every patch PKG into Ps3UpdateCachePath.
 *
 * Single hand-rolled Form (no Designer partial) — the UI is just a status list,
 * a progress bar, and three buttons.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;

namespace ArchiveCacheManager
{
    public class Ps3BulkUpdateBuilderWindow : Form
    {
        private ListBox mList;
        private CheckBox mManifestsOnly;
        private CheckBox mForceRefresh;
        private ProgressBar mProgress;
        private Label mStatus;
        private Button mStartButton;
        private Button mCancelButton;
        private Button mCloseButton;

        private CancellationTokenSource mCts;
        private bool mRunning;
        private readonly SonyUpdateContext mCtx;
        private readonly Func<string, bool> mPlatformGate;
        private readonly IGame[] mPreselected;

        public Ps3BulkUpdateBuilderWindow(SonyUpdateContext ctx = null, Func<string, bool> platformGate = null, IGame[] preselected = null)
        {
            mCtx = ctx ?? SonyUpdateContext.ForPs3();
            mPlatformGate = platformGate ?? Config.MatchesPs3PkgPlatform;
            mPreselected = preselected;
            BuildUi();
            UserInterface.ApplyTheme(this);
            PopulateLibrary();
        }

        private void BuildUi()
        {
            Text = string.Format("Build {0} Update DB", mCtx.Platform);
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new System.Drawing.Size(640, 460);
            ClientSize = new System.Drawing.Size(720, 540);

            mList = new ListBox
            {
                Location = new System.Drawing.Point(12, 12),
                Size = new System.Drawing.Size(696, 360),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                IntegralHeight = false,
                HorizontalScrollbar = true,
            };

            mManifestsOnly = new CheckBox
            {
                Text = "Save manifests only (skip PKG downloads — fast first pass)",
                Location = new System.Drawing.Point(12, 384),
                AutoSize = true,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                Checked = false,
            };
            mForceRefresh = new CheckBox
            {
                Text = "Re-fetch manifests / re-verify PKGs already present",
                Location = new System.Drawing.Point(12, 405),
                AutoSize = true,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                Checked = false,
            };

            mProgress = new ProgressBar
            {
                Location = new System.Drawing.Point(12, 432),
                Size = new System.Drawing.Size(696, 18),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            };
            mStatus = new Label
            {
                Location = new System.Drawing.Point(12, 455),
                Size = new System.Drawing.Size(696, 17),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Text = "Ready.",
            };

            mStartButton = new Button
            {
                Text = "Start",
                Location = new System.Drawing.Point(480, 490),
                Width = 70, Height = 27,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            };
            mStartButton.Click += async (s, e) => await DoRun();

            mCancelButton = new Button
            {
                Text = "Cancel",
                Location = new System.Drawing.Point(556, 490),
                Width = 70, Height = 27,
                Enabled = false,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            };
            mCancelButton.Click += (s, e) => mCts?.Cancel();

            mCloseButton = new Button
            {
                Text = "Close",
                Location = new System.Drawing.Point(632, 490),
                Width = 76, Height = 27,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            };
            mCloseButton.Click += (s, e) => Close();

            Controls.AddRange(new Control[] {
                mList, mManifestsOnly, mForceRefresh,
                mProgress, mStatus,
                mStartButton, mCancelButton, mCloseButton,
            });

            CancelButton = mCloseButton;
        }

        private List<(IGame Game, string ArchivePath)> mPs3Games = new List<(IGame, string)>();

        private void PopulateLibrary()
        {
            try
            {
                // When the caller passes an explicit selection (right-click on N highlighted titles),
                // honour that exactly — no library scan, no platform gate, the user already chose.
                // Falling back to the full library scan only when nothing was preselected keeps the
                // "right-click on any one PS3 game → process every PS3 game" entry point alive.
                IGame[] source = (mPreselected != null && mPreselected.Length > 0)
                    ? mPreselected
                    : PluginHelper.DataManager.GetAllGames();
                bool gateNeeded = mPreselected == null || mPreselected.Length == 0;

                foreach (var g in source)
                {
                    if (g == null) continue;
                    if (gateNeeded && !mPlatformGate(g.Platform)) continue;
                    string archive = !string.IsNullOrWhiteSpace(g.ApplicationPath)
                        ? PathUtils.GetAbsolutePath(g.ApplicationPath)
                        : null;
                    mPs3Games.Add((g, archive));
                    mList.Items.Add(string.Format("{0,-12}   {1}", "(pending)", g.Title ?? "<no title>"));
                }
            }
            catch (Exception ex)
            {
                Logger.Log(string.Format("Ps3BulkUpdateBuilder: library scan failed: {0}", ex), Logger.LogLevel.Exception);
            }

            string scope = (mPreselected != null && mPreselected.Length > 0) ? "selected" : "library";
            mStatus.Text = string.Format("Found {0} {1} games ({2}).", mPs3Games.Count, mCtx.Platform, scope);
            mStartButton.Enabled = mPs3Games.Count > 0;
        }

        private async Task DoRun()
        {
            if (mRunning || mPs3Games.Count == 0) return;
            mRunning = true;
            mCts = new CancellationTokenSource();
            mStartButton.Enabled = false;
            mCancelButton.Enabled = true;
            mProgress.Maximum = mPs3Games.Count;
            mProgress.Value = 0;

            int withManifest = 0, withoutManifest = 0, totalUpdates = 0, downloaded = 0, reused = 0, failed = 0;

            for (int i = 0; i < mPs3Games.Count; i++)
            {
                if (mCts.IsCancellationRequested) break;
                var (game, archive) = mPs3Games[i];
                mProgress.Value = i;
                mStatus.Text = string.Format("[{0}/{1}] {2}…", i + 1, mPs3Games.Count, game.Title);

                string titleId = TryInferTitleIdForBulk(archive);
                if (string.IsNullOrWhiteSpace(titleId))
                {
                    mList.Items[i] = string.Format("{0,-12}   {1}", "no TITLE_ID", game.Title);
                    failed++;
                    continue;
                }

                List<Ps3UpdateInfo> updates;
                try
                {
                    string cached = Ps3UpdateFetcher.ResolveCachedManifestPath(titleId, mCtx);
                    bool haveCachedXml = !string.IsNullOrEmpty(cached) && File.Exists(cached);
                    if (haveCachedXml && !mForceRefresh.Checked)
                    {
                        updates = Ps3UpdateFetcher.ParseUpdateXml(File.ReadAllText(cached));
                        withManifest++;
                    }
                    else
                    {
                        // QueryAsync writes the manifest to cache on success.
                        SonyUpdateContext queryCtx = mCtx;
                        if (mForceRefresh.Checked)
                        {
                            queryCtx = new SonyUpdateContext
                            {
                                Platform = mCtx.Platform,
                                CacheRoot = mCtx.CacheRoot,
                                OfflineMode = false,
                                PathTransform = mCtx.PathTransform,
                            };
                        }
                        updates = await Ps3UpdateFetcher.QueryAsync(titleId, mCts.Token, queryCtx).ConfigureAwait(true);
                        if (updates.Count > 0 || haveCachedXml) withManifest++;
                        else withoutManifest++;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(string.Format("Ps3BulkUpdateBuilder: query failed for {0} ({1}): {2}", titleId, game.Title, ex));
                    mList.Items[i] = string.Format("{0,-12}   {1}   ({2})", titleId, game.Title, "query failed");
                    failed++;
                    continue;
                }

                totalUpdates += updates.Count;

                if (updates.Count == 0)
                {
                    mList.Items[i] = string.Format("{0,-12}   {1}   (no updates)", titleId, game.Title);
                    continue;
                }

                if (mManifestsOnly.Checked)
                {
                    mList.Items[i] = string.Format("{0,-12}   {1}   ({2} update(s), manifest only)", titleId, game.Title, updates.Count);
                    continue;
                }

                int dlOk = 0, dlFail = 0, dlReused = 0;
                foreach (var info in updates)
                {
                    if (mCts.IsCancellationRequested) break;
                    string pkgFile = SafeForFilename(string.Format("{0}-v{1}.pkg", titleId, info.Version));
                    string pkgPath = Path.Combine(Ps3UpdateFetcher.ResolveCacheRoot(mCtx), titleId, pkgFile);
                    bool haveValid = File.Exists(pkgPath) && (string.IsNullOrEmpty(info.Sha1Sum) || FileSha1Matches(pkgPath, info.Sha1Sum));
                    if (haveValid && !mForceRefresh.Checked)
                    {
                        dlReused++;
                        continue;
                    }
                    try
                    {
                        mStatus.Text = string.Format("[{0}/{1}] {2} — downloading v{3}…",
                            i + 1, mPs3Games.Count, titleId, info.Version);
                        Directory.CreateDirectory(Path.GetDirectoryName(pkgPath));
                        await Ps3UpdateFetcher.DownloadAsync(info, pkgPath, null, mCts.Token).ConfigureAwait(true);
                        dlOk++;
                    }
                    catch (Exception ex)
                    {
                        dlFail++;
                        Logger.Log(string.Format("Ps3BulkUpdateBuilder: download failed for {0} v{1}: {2}", titleId, info.Version, ex));
                    }
                }
                downloaded += dlOk;
                reused += dlReused;
                failed += dlFail;
                mList.Items[i] = string.Format("{0,-12}   {1}   ({2} update(s): {3} downloaded, {4} reused{5})",
                    titleId, game.Title, updates.Count, dlOk, dlReused,
                    dlFail > 0 ? string.Format(", {0} failed", dlFail) : "");
            }

            mProgress.Value = mProgress.Maximum;
            mStatus.Text = string.Format(
                "Done. Manifests cached: {0}. Updates listed: {1}. PKGs downloaded: {2}, reused: {3}, failed: {4}.",
                withManifest, totalUpdates, downloaded, reused, failed);
            mRunning = false;
            mStartButton.Enabled = true;
            mCancelButton.Enabled = false;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (mRunning) mCts?.Cancel();
            base.OnFormClosing(e);
        }

        private static bool FileSha1Matches(string path, string expectedHexLower)
        {
            try
            {
                using (var fs = File.OpenRead(path))
                using (var sha = System.Security.Cryptography.SHA1.Create())
                {
                    byte[] hash = sha.ComputeHash(fs);
                    string got = BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
                    return string.Equals(got, expectedHexLower, StringComparison.OrdinalIgnoreCase);
                }
            }
            catch { return false; }
        }

        private static string SafeForFilename(string s)
        {
            if (string.IsNullOrEmpty(s)) return "x";
            foreach (char c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return s;
        }

        // === TITLE_ID inference — mirrors FetchPs3UpdatesMenuItem.TryInferTitleId logic.
        //     Duplicated here so this Form's flow stays independent of the menu item.
        private static string TryInferTitleIdForBulk(string archivePath)
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
                        var sfo = zip.Entries.FirstOrDefault(e => e.FullName.EndsWith("PARAM.SFO", StringComparison.OrdinalIgnoreCase));
                        if (sfo != null)
                        {
                            using (var ms = new MemoryStream())
                            using (var s = sfo.Open()) { s.CopyTo(ms); var map = Sfo.Parse(ms.ToArray()); return map.TryGetValue("TITLE_ID", out string id) ? id.Trim() : null; }
                        }
                    }
                    return null;
                }
                if (string.Equals(ext, ".iso", StringComparison.OrdinalIgnoreCase))
                {
                    return ExtractAndRead(archivePath, "PS3_GAME/PARAM.SFO", Sfo.ReadTitleId);
                }
                if (string.Equals(ext, ".7z", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(ext, ".rar", StringComparison.OrdinalIgnoreCase))
                {
                    return ExtractAndRead(archivePath, "*.pkg", p => { using (var fs = File.OpenRead(p)) return ReadTitleIdFromPkgStream(fs); })
                        ?? ExtractAndRead(archivePath, "PARAM.SFO", Sfo.ReadTitleId);
                }

                // Fallback: archive cache (decrypted by a prior launch).
                string cacheDir = PathUtils.ArchiveCachePath(archivePath, checkOldFormat: true);
                if (Directory.Exists(cacheDir))
                {
                    string sfo = Directory.GetFiles(cacheDir, "PARAM.SFO", SearchOption.AllDirectories).FirstOrDefault();
                    if (sfo != null) return Sfo.ReadTitleId(sfo);
                    foreach (string iso in Directory.GetFiles(cacheDir, "*.iso", SearchOption.TopDirectoryOnly))
                    {
                        string id = ExtractAndRead(iso, "PS3_GAME/PARAM.SFO", Sfo.ReadTitleId);
                        if (!string.IsNullOrWhiteSpace(id)) return id;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(string.Format("Ps3BulkUpdateBuilder: TITLE_ID inference failed for {0}: {1}", archivePath, ex.Message));
            }
            return null;
        }

        private static string ExtractAndRead(string archivePath, string includePattern, Func<string, string> reader)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "ACM_BulkInfer_" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(tempDir);
                var zip = new Zip();
                if (!zip.Extract(archivePath, tempDir, new[] { includePattern })) return null;
                bool pkgGlob = includePattern.EndsWith(".pkg", StringComparison.OrdinalIgnoreCase);
                string match = pkgGlob
                    ? Directory.GetFiles(tempDir, "*.pkg", SearchOption.AllDirectories).FirstOrDefault()
                    : Directory.GetFiles(tempDir, Path.GetFileName(includePattern), SearchOption.AllDirectories).FirstOrDefault();
                return match != null ? reader(match) : null;
            }
            finally { try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); } catch { } }
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
