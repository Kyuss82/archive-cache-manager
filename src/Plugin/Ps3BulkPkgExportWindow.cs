/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Bulk PS3 PKG raw exporter. Sibling to PsvBulkVpkBuilderWindow: enumerates
 * every game in the LaunchBox library whose platform matches `Ps3PkgPlatform`,
 * then for each one runs `Ps3PkgRawExporter.ExportFromGameArchive` to pull the
 * raw .pkg (+ companion .rap from the archive or the NPS DB) into the chosen
 * output folder. No AES-CTR decryption is performed — the user then bulk-imports
 * the resulting .pkg files via RPCS3 → File → Install Packages/Raps.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;

namespace ArchiveCacheManager
{
    public class Ps3BulkPkgExportWindow : Form
    {
        private ListBox mList;
        private Label mOutputLabel;
        private TextBox mOutputPath;
        private Button mOutputBrowse;
        private CheckBox mSkipExisting;
        private ProgressBar mProgress;
        private Label mStatus;
        private Button mStartButton;
        private Button mCancelButton;
        private Button mCloseButton;

        private CancellationTokenSource mCts;
        private bool mRunning;
        private readonly IGame[] mPreselected;

        public Ps3BulkPkgExportWindow(IGame[] preselected = null)
        {
            mPreselected = preselected;
            BuildUi();
            UserInterface.ApplyTheme(this);
            PopulateLibrary();
        }

        private void BuildUi()
        {
            Text = "Export PS3 PKGs (for RPCS3 install)";
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new System.Drawing.Size(640, 480);
            ClientSize = new System.Drawing.Size(720, 560);

            mList = new ListBox
            {
                Location = new System.Drawing.Point(12, 12),
                Size = new System.Drawing.Size(696, 340),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                IntegralHeight = false,
                HorizontalScrollbar = true,
            };

            mOutputLabel = new Label
            {
                Text = "Output folder (empty = next to source archive):",
                Location = new System.Drawing.Point(12, 364),
                AutoSize = true,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
            };
            mOutputPath = new TextBox
            {
                Location = new System.Drawing.Point(12, 384),
                Size = new System.Drawing.Size(596, 20),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Text = Config.Ps3PkgOutputPath ?? string.Empty,
            };
            mOutputBrowse = new Button
            {
                Text = "Browse…",
                Location = new System.Drawing.Point(614, 382),
                Width = 94, Height = 24,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            };
            mOutputBrowse.Click += (s, e) =>
            {
                using (var dlg = new FolderBrowserDialog())
                {
                    if (!string.IsNullOrWhiteSpace(mOutputPath.Text)) dlg.SelectedPath = PathUtils.GetAbsolutePath(mOutputPath.Text);
                    if (dlg.ShowDialog(this) == DialogResult.OK) mOutputPath.Text = dlg.SelectedPath;
                }
            };

            mSkipExisting = new CheckBox
            {
                Text = "Skip if a .pkg with the same content id is already in the output folder",
                Location = new System.Drawing.Point(12, 414),
                AutoSize = true,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                Checked = true,
            };

            mProgress = new ProgressBar
            {
                Location = new System.Drawing.Point(12, 444),
                Size = new System.Drawing.Size(696, 18),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            };
            mStatus = new Label
            {
                Location = new System.Drawing.Point(12, 468),
                Size = new System.Drawing.Size(696, 17),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Text = "Ready.",
            };

            mStartButton = new Button
            {
                Text = "Start",
                Location = new System.Drawing.Point(480, 504),
                Width = 70, Height = 27,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            };
            mStartButton.Click += async (s, e) => await DoRun();

            mCancelButton = new Button
            {
                Text = "Cancel",
                Location = new System.Drawing.Point(556, 504),
                Width = 70, Height = 27,
                Enabled = false,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            };
            mCancelButton.Click += (s, e) => mCts?.Cancel();

            mCloseButton = new Button
            {
                Text = "Close",
                Location = new System.Drawing.Point(632, 504),
                Width = 76, Height = 27,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            };
            mCloseButton.Click += (s, e) => Close();

            Controls.AddRange(new Control[] {
                mList, mOutputLabel, mOutputPath, mOutputBrowse, mSkipExisting,
                mProgress, mStatus,
                mStartButton, mCancelButton, mCloseButton,
            });

            CancelButton = mCloseButton;
        }

        private List<(IGame Game, string ArchivePath)> mGames = new List<(IGame, string)>();

        private void PopulateLibrary()
        {
            try
            {
                IGame[] source = (mPreselected != null && mPreselected.Length > 0)
                    ? mPreselected
                    : PluginHelper.DataManager.GetAllGames();
                bool gateNeeded = mPreselected == null || mPreselected.Length == 0;

                foreach (var g in source)
                {
                    if (g == null) continue;
                    if (gateNeeded && !Config.MatchesPs3PkgPlatform(g.Platform)) continue;
                    string archive = !string.IsNullOrWhiteSpace(g.ApplicationPath)
                        ? PathUtils.GetAbsolutePath(g.ApplicationPath)
                        : null;
                    if (string.IsNullOrWhiteSpace(archive)) continue;
                    if (!Ps3PkgExtractor.SupportedType(archive)) continue;
                    mGames.Add((g, archive));
                    mList.Items.Add(string.Format("{0,-12}   {1}", "(pending)", g.Title ?? "<no title>"));
                }
            }
            catch (Exception ex)
            {
                Logger.Log(string.Format("Ps3BulkPkgExport: library scan failed: {0}", ex), Logger.LogLevel.Exception);
            }

            string scope = (mPreselected != null && mPreselected.Length > 0) ? "selected" : "library";
            mStatus.Text = string.Format("Found {0} PS3 PKG games ({1}).", mGames.Count, scope);
            mStartButton.Enabled = mGames.Count > 0;
        }

        private async Task DoRun()
        {
            if (mRunning || mGames.Count == 0) return;
            mRunning = true;
            mCts = new CancellationTokenSource();
            mStartButton.Enabled = false;
            mCancelButton.Enabled = true;
            mOutputBrowse.Enabled = false;
            mOutputPath.Enabled = false;
            mSkipExisting.Enabled = false;
            mProgress.Maximum = mGames.Count;
            mProgress.Value = 0;

            string outputRoot = mOutputPath.Text.Trim();
            int exported = 0, skipped = 0, failed = 0, withRap = 0;

            for (int i = 0; i < mGames.Count; i++)
            {
                if (mCts.IsCancellationRequested) break;
                var (game, archive) = mGames[i];
                mProgress.Value = i;
                mStatus.Text = string.Format("[{0}/{1}] {2}…", i + 1, mGames.Count, game.Title);
                Application.DoEvents();

                string outputDir = !string.IsNullOrEmpty(outputRoot)
                    ? PathUtils.GetAbsolutePath(outputRoot)
                    : Path.GetDirectoryName(archive);

                if (mSkipExisting.Checked && OutputAlreadyHasPkg(outputDir, game.ApplicationPath))
                {
                    mList.Items[i] = string.Format("{0,-12}   {1}   (skipped — output exists)", "skipped", game.Title);
                    skipped++;
                    continue;
                }

                Ps3PkgRawExportResult result;
                try
                {
                    result = await Task.Run(() =>
                        Ps3PkgRawExporter.ExportFromGameArchive(archive, outputDir),
                        mCts.Token).ConfigureAwait(true);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    Logger.Log(string.Format("Ps3BulkPkgExport: build threw for {0}: {1}", game.Title, ex), Logger.LogLevel.Exception);
                    mList.Items[i] = string.Format("{0,-12}   {1}   (error: {2})", "fail", game.Title, ex.Message);
                    failed++;
                    continue;
                }

                if (!result.Success)
                {
                    mList.Items[i] = string.Format("{0,-12}   {1}   ({2})", "fail", game.Title, result.ErrorMessage ?? "no detail");
                    failed++;
                    continue;
                }

                exported++;
                if (result.RapPaths.Count > 0) withRap++;
                mList.Items[i] = string.Format("{0,-12}   {1}   ({2} pkg, {3} rap{4})",
                    result.TitleId ?? "OK", game.Title, result.PkgPaths.Count, result.RapPaths.Count,
                    result.RapSource != null ? " src=" + result.RapSource : "");
            }

            mProgress.Value = mProgress.Maximum;
            mStatus.Text = string.Format("Done. Exported: {0} (with RAP: {1}), skipped: {2}, failed: {3}.",
                exported, withRap, skipped, failed);

            mStartButton.Enabled = false;
            mCancelButton.Enabled = false;
            mRunning = false;
        }

        private static bool OutputAlreadyHasPkg(string outputDir, string sourceArchivePath)
        {
            // Heuristic — we don't want to re-parse every source PKG just to check existence,
            // so we look for any .pkg in outputDir whose name starts with the LaunchBox
            // base name. Cheap and Good Enough for the bulk skip case.
            if (!Directory.Exists(outputDir)) return false;
            string baseName = Path.GetFileNameWithoutExtension(sourceArchivePath ?? string.Empty);
            if (string.IsNullOrEmpty(baseName)) return false;
            foreach (string pkg in Directory.EnumerateFiles(outputDir, "*.pkg"))
            {
                string pkgName = Path.GetFileNameWithoutExtension(pkg);
                if (pkgName.IndexOf(baseName, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (mRunning) mCts?.Cancel();
            base.OnFormClosing(e);
        }
    }
}
