/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * UI for the PS3 update fetcher. Single hand-rolled Windows.Forms dialog (no
 * separate Designer file) — given how the rest of the plugin uses Designer
 * partials, the extra file isn't worth the noise for a 9-control form.
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ArchiveCacheManager
{
    public class Ps3UpdateWindow : Form
    {
        private TextBox mTitleIdBox;
        private Button mQueryButton;
        private CheckedListBox mUpdatesList;
        private TextBox mOutputPathBox;
        private Button mOutputBrowseButton;
        private ProgressBar mProgress;
        private Label mStatusLabel;
        private Button mDownloadButton;
        private Button mCloseButton;

        private List<Ps3UpdateInfo> mUpdates = new List<Ps3UpdateInfo>();
        private CancellationTokenSource mCts;
        private bool mBusy;
        private readonly SonyUpdateContext mCtx;

        public Ps3UpdateWindow(string initialTitleId, string defaultOutputDir, SonyUpdateContext ctx = null)
        {
            mCtx = ctx ?? SonyUpdateContext.ForPs3();
            BuildUi();
            mTitleIdBox.Text = initialTitleId ?? string.Empty;
            mOutputPathBox.Text = defaultOutputDir ?? string.Empty;
            UserInterface.ApplyTheme(this);
        }

        private void BuildUi()
        {
            Text = string.Format("Fetch {0} Updates", mCtx.Platform);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ClientSize = new System.Drawing.Size(640, 420);

            var titleIdLabel = new Label { Text = "Title ID:", Location = new System.Drawing.Point(12, 15), AutoSize = true };
            mTitleIdBox = new TextBox
            {
                Location = new System.Drawing.Point(80, 12),
                Width = 200,
                CharacterCasing = CharacterCasing.Upper,
                MaxLength = 12
            };
            mQueryButton = new Button
            {
                Text = "Query",
                Location = new System.Drawing.Point(290, 10),
                Width = 90,
                Height = 25,
            };
            mQueryButton.Click += async (s, e) => await DoQuery();

            mUpdatesList = new CheckedListBox
            {
                Location = new System.Drawing.Point(12, 50),
                Size = new System.Drawing.Size(616, 230),
                CheckOnClick = true,
                IntegralHeight = false,
                HorizontalScrollbar = true,
            };

            var outputLabel = new Label { Text = "Output:", Location = new System.Drawing.Point(12, 290), AutoSize = true };
            mOutputPathBox = new TextBox
            {
                Location = new System.Drawing.Point(80, 287),
                Width = 450,
                MaxLength = 260,
            };
            mOutputBrowseButton = new Button
            {
                Text = "Browse...",
                Location = new System.Drawing.Point(540, 285),
                Width = 88,
                Height = 25,
            };
            mOutputBrowseButton.Click += (s, e) => BrowseOutput();

            mProgress = new ProgressBar
            {
                Location = new System.Drawing.Point(12, 325),
                Size = new System.Drawing.Size(616, 18),
            };
            mStatusLabel = new Label
            {
                Location = new System.Drawing.Point(12, 348),
                Size = new System.Drawing.Size(616, 17),
                Text = "Ready.",
            };

            mDownloadButton = new Button
            {
                Text = "Download Selected",
                Location = new System.Drawing.Point(412, 380),
                Width = 134,
                Height = 27,
                Enabled = false,
            };
            mDownloadButton.Click += async (s, e) => await DoDownload();

            mCloseButton = new Button
            {
                Text = "Close",
                Location = new System.Drawing.Point(552, 380),
                Width = 76,
                Height = 27,
            };
            mCloseButton.Click += (s, e) => Close();

            Controls.AddRange(new Control[] {
                titleIdLabel, mTitleIdBox, mQueryButton,
                mUpdatesList,
                outputLabel, mOutputPathBox, mOutputBrowseButton,
                mProgress, mStatusLabel,
                mDownloadButton, mCloseButton,
            });

            AcceptButton = mQueryButton;
            CancelButton = mCloseButton;
        }

        private async Task DoQuery()
        {
            string titleId = mTitleIdBox.Text.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(titleId))
            {
                MessageBox.Show("Enter a Title ID (e.g. BLES01047, NPEB00321).", "Fetch PS3 Updates",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            BeginBusy("Querying Sony update server…");
            mUpdatesList.Items.Clear();
            mUpdates.Clear();
            mDownloadButton.Enabled = false;

            try
            {
                mCts = new CancellationTokenSource();
                mUpdates = await Ps3UpdateFetcher.QueryAsync(titleId, mCts.Token, mCtx);
            }
            catch (Exception ex)
            {
                EndBusy(string.Format("Query failed: {0}", ex.Message));
                Logger.Log(string.Format("Ps3UpdateWindow: query failed for {0}: {1}", titleId, ex), Logger.LogLevel.Exception);
                return;
            }

            if (mUpdates.Count == 0)
            {
                EndBusy("No updates listed for this title.");
                return;
            }

            ulong total = 0;
            foreach (var u in mUpdates)
            {
                mUpdatesList.Items.Add(FormatUpdateRow(u), true);
                total += u.Size;
            }
            mDownloadButton.Enabled = true;
            EndBusy(string.Format("Found {0} update(s), total {1}.", mUpdates.Count, FormatBytes(total)));
        }

        private async Task DoDownload()
        {
            if (string.IsNullOrWhiteSpace(mOutputPathBox.Text))
            {
                MessageBox.Show("Pick an output folder first.", "Fetch PS3 Updates",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string outputDir = PathUtils.GetAbsolutePath(mOutputPathBox.Text.Trim());
            try { Directory.CreateDirectory(outputDir); }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Cannot create output folder:\r\n{0}", ex.Message),
                    "Fetch PS3 Updates", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var selected = new List<Ps3UpdateInfo>();
            foreach (int idx in mUpdatesList.CheckedIndices)
            {
                if (idx >= 0 && idx < mUpdates.Count) selected.Add(mUpdates[idx]);
            }
            if (selected.Count == 0)
            {
                MessageBox.Show("Tick at least one update to download.", "Fetch PS3 Updates",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            BeginBusy("Downloading…");
            mCts = new CancellationTokenSource();

            int success = 0, failed = 0;
            for (int i = 0; i < selected.Count; i++)
            {
                var info = selected[i];
                string fileName = string.Format("{0}-v{1}.pkg",
                    SafeForFilename(mTitleIdBox.Text.Trim().ToUpperInvariant()),
                    SafeForFilename(info.Version));
                string outputPath = Path.Combine(outputDir, fileName);

                mProgress.Style = ProgressBarStyle.Continuous;
                mProgress.Value = 0;
                if (info.Size > 0 && info.Size <= int.MaxValue)
                {
                    mProgress.Maximum = (int)info.Size;
                }
                else
                {
                    mProgress.Maximum = 1; // unknown size — leave at 0% until done
                }

                mStatusLabel.Text = string.Format("[{0}/{1}] {2} ({3})…",
                    i + 1, selected.Count, fileName, FormatBytes(info.Size));

                try
                {
                    long sizeForProgress = info.Size > int.MaxValue ? int.MaxValue : (long)info.Size;
                    var progress = new Progress<long>(bytes =>
                    {
                        if (sizeForProgress > 0)
                        {
                            mProgress.Value = (int)Math.Min(bytes, sizeForProgress);
                        }
                    });
                    await Ps3UpdateFetcher.DownloadAsync(info, outputPath, progress, mCts.Token);
                    success++;
                }
                catch (Exception ex)
                {
                    failed++;
                    Logger.Log(string.Format("Ps3UpdateWindow: download failed for {0}: {1}", info.Url, ex), Logger.LogLevel.Exception);
                }
            }

            EndBusy(string.Format("Downloaded {0} of {1}. {2}",
                success, selected.Count,
                failed > 0 ? string.Format("{0} failed — see log.", failed) : "All OK."));
        }

        private void BrowseOutput()
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.ShowNewFolderButton = true;
                string current = string.IsNullOrWhiteSpace(mOutputPathBox.Text)
                    ? PathUtils.GetLaunchBoxRootPath()
                    : PathUtils.GetAbsolutePath(mOutputPathBox.Text);
                dlg.SelectedPath = Directory.Exists(current) ? current : PathUtils.GetLaunchBoxRootPath();
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    mOutputPathBox.Text = PathUtils.GetRelativePath(PathUtils.GetLaunchBoxRootPath(), dlg.SelectedPath);
                }
            }
        }

        private void BeginBusy(string status)
        {
            mBusy = true;
            mQueryButton.Enabled = false;
            mDownloadButton.Enabled = false;
            mTitleIdBox.Enabled = false;
            mProgress.Style = ProgressBarStyle.Marquee;
            mStatusLabel.Text = status;
        }

        private void EndBusy(string status)
        {
            mBusy = false;
            mQueryButton.Enabled = true;
            mDownloadButton.Enabled = mUpdates.Count > 0;
            mTitleIdBox.Enabled = true;
            mProgress.Style = ProgressBarStyle.Continuous;
            mProgress.Value = 0;
            mStatusLabel.Text = status;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (mBusy && mCts != null)
            {
                mCts.Cancel();
            }
            base.OnFormClosing(e);
        }

        private static string FormatUpdateRow(Ps3UpdateInfo u)
        {
            return string.Format("v{0,-6}   {1,10}   sha1={2}   sysver={3}",
                u.Version ?? "?", FormatBytes(u.Size), u.Sha1Sum ?? "?",
                string.IsNullOrEmpty(u.SystemVer) ? "—" : u.SystemVer);
        }

        private static string FormatBytes(ulong bytes)
        {
            if (bytes == 0) return "?";
            double v = bytes;
            string[] units = { "B", "KB", "MB", "GB" };
            int u = 0;
            while (v >= 1024 && u < units.Length - 1) { v /= 1024; u++; }
            return string.Format("{0:0.##} {1}", v, units[u]);
        }

        private static string SafeForFilename(string s)
        {
            if (string.IsNullOrEmpty(s)) return "x";
            foreach (char c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return s;
        }
    }
}
