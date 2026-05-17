/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Bulk PSV VPK builder. Enumerates every game in the LaunchBox library whose
 * platform matches `PsvPkgPlatform`, then for each game runs the same
 * `PsvPkgStaging.BuildFromGameArchive` pipeline that the right-click
 * "Create PSV VPK..." menu uses — producing one `<baseName>.vpk` per title in
 * the configured `PsvPkgOutputPath` (or a folder of the user's choice). The
 * resulting .vpks carry the NPDRM klicensee inside `sce_sys/package/work.bin`,
 * so Vita3K boots them after a simple drag-drop install.
 *
 * Single hand-rolled Form (no Designer partial). Same shape as
 * Ps3BulkUpdateBuilderWindow so the two feel familiar side by side.
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
    public class PsvBulkVpkBuilderWindow : Form
    {
        private ListBox mList;
        private Label mOutputLabel;
        private TextBox mOutputPath;
        private Button mOutputBrowse;
        private CheckBox mSkipExisting;
        private CheckBox mAddToLibrary;
        private ProgressBar mProgress;
        private Label mStatus;
        private Button mStartButton;
        private Button mCancelButton;
        private Button mCloseButton;

        private CancellationTokenSource mCts;
        private bool mRunning;
        private readonly IGame[] mPreselected;

        public PsvBulkVpkBuilderWindow(IGame[] preselected = null)
        {
            mPreselected = preselected;
            BuildUi();
            UserInterface.ApplyTheme(this);
            PopulateLibrary();
        }

        private void BuildUi()
        {
            Text = "Build PSV VPKs";
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new System.Drawing.Size(640, 500);
            ClientSize = new System.Drawing.Size(720, 580);

            mList = new ListBox
            {
                Location = new System.Drawing.Point(12, 12),
                Size = new System.Drawing.Size(696, 350),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                IntegralHeight = false,
                HorizontalScrollbar = true,
            };

            mOutputLabel = new Label
            {
                Text = "Output folder (empty = next to source archive):",
                Location = new System.Drawing.Point(12, 374),
                AutoSize = true,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
            };
            mOutputPath = new TextBox
            {
                Location = new System.Drawing.Point(12, 394),
                Size = new System.Drawing.Size(596, 20),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Text = Config.PsvPkgOutputPath ?? string.Empty,
            };
            mOutputBrowse = new Button
            {
                Text = "Browse…",
                Location = new System.Drawing.Point(614, 392),
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
                Text = "Skip if <baseName>.vpk already exists in the output folder",
                Location = new System.Drawing.Point(12, 424),
                AutoSize = true,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                Checked = true,
            };
            mAddToLibrary = new CheckBox
            {
                Text = "Add produced .vpk files to the LaunchBox library",
                Location = new System.Drawing.Point(12, 445),
                AutoSize = true,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                Checked = Config.PsvPkgAddToLibrary,
            };

            mProgress = new ProgressBar
            {
                Location = new System.Drawing.Point(12, 475),
                Size = new System.Drawing.Size(696, 18),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            };
            mStatus = new Label
            {
                Location = new System.Drawing.Point(12, 498),
                Size = new System.Drawing.Size(696, 17),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Text = "Ready.",
            };

            mStartButton = new Button
            {
                Text = "Start",
                Location = new System.Drawing.Point(480, 530),
                Width = 70, Height = 27,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            };
            mStartButton.Click += async (s, e) => await DoRun();

            mCancelButton = new Button
            {
                Text = "Cancel",
                Location = new System.Drawing.Point(556, 530),
                Width = 70, Height = 27,
                Enabled = false,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            };
            mCancelButton.Click += (s, e) => mCts?.Cancel();

            mCloseButton = new Button
            {
                Text = "Close",
                Location = new System.Drawing.Point(632, 530),
                Width = 76, Height = 27,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            };
            mCloseButton.Click += (s, e) => Close();

            Controls.AddRange(new Control[] {
                mList, mOutputLabel, mOutputPath, mOutputBrowse,
                mSkipExisting, mAddToLibrary,
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
                    if (gateNeeded && !Config.MatchesPsvPkgPlatform(g.Platform)) continue;
                    string archive = !string.IsNullOrWhiteSpace(g.ApplicationPath)
                        ? PathUtils.GetAbsolutePath(g.ApplicationPath)
                        : null;
                    if (string.IsNullOrWhiteSpace(archive)) continue;
                    if (!PsvPkgExtractor.SupportedType(archive)) continue;
                    mGames.Add((g, archive));
                    mList.Items.Add(string.Format("{0,-12}   {1}", "(pending)", g.Title ?? "<no title>"));
                }
            }
            catch (Exception ex)
            {
                Logger.Log(string.Format("PsvBulkVpkBuilder: library scan failed: {0}", ex), Logger.LogLevel.Exception);
            }

            string scope = (mPreselected != null && mPreselected.Length > 0) ? "selected" : "library";
            mStatus.Text = string.Format("Found {0} PSV PKG games ({1}).", mGames.Count, scope);
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
            mAddToLibrary.Enabled = false;
            mProgress.Maximum = mGames.Count;
            mProgress.Value = 0;

            string outputRoot = mOutputPath.Text.Trim();
            int built = 0, skipped = 0, failed = 0, librarised = 0;

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
                string baseName = Path.GetFileNameWithoutExtension(archive);
                string vpkPath = Path.Combine(outputDir, baseName + ".vpk");

                if (mSkipExisting.Checked && File.Exists(vpkPath))
                {
                    mList.Items[i] = string.Format("{0,-12}   {1}   (skipped — .vpk already exists)", "skipped", game.Title);
                    skipped++;
                    continue;
                }

                PsvPkgBuildResult result;
                try
                {
                    result = await Task.Run(() =>
                        PsvPkgStaging.BuildFromGameArchive(archive, outputDir, baseName, packageAsVpk: true),
                        mCts.Token).ConfigureAwait(true);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    Logger.Log(string.Format("PsvBulkVpkBuilder: build threw for {0}: {1}", game.Title, ex), Logger.LogLevel.Exception);
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

                built++;
                if (mAddToLibrary.Checked && !string.IsNullOrEmpty(result.VpkPath))
                {
                    if (AddVpkToLibrary(game, result.VpkPath)) librarised++;
                }
                mList.Items[i] = string.Format("{0,-12}   {1}   ({2} files, {3} dirs)",
                    result.TitleId ?? "OK", game.Title, result.FilesWritten, result.DirectoriesCreated);
            }

            mProgress.Value = mProgress.Maximum;
            mStatus.Text = string.Format("Done. Built: {0}, skipped: {1}, failed: {2}{3}.",
                built, skipped, failed,
                mAddToLibrary.Checked ? string.Format(", added to library: {0}", librarised) : string.Empty);

            mStartButton.Enabled = false; // single-shot — user closes the window when satisfied
            mCancelButton.Enabled = false;
            mRunning = false;
        }

        private static bool AddVpkToLibrary(IGame sourceGame, string vpkPath)
        {
            try
            {
                var newGame = PluginHelper.DataManager.AddNewGame(sourceGame.Title);
                newGame.ApplicationPath = vpkPath;
                newGame.Platform = sourceGame.Platform;
                newGame.SortTitle = sourceGame.Title;
                newGame.Source = "Archive Cache Manager (PSV VPK)";
                PluginHelper.DataManager.Save();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log(string.Format("PsvBulkVpkBuilder: AddToLibrary failed for {0}: {1}", sourceGame.Title, ex.Message));
                return false;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (mRunning)
            {
                mCts?.Cancel();
            }
            base.OnFormClosing(e);
        }
    }
}
