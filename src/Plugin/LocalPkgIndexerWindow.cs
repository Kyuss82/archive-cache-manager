/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Unified local-mirror indexer hub. Five tabs (PS3 / PSP / PSV / Wii U / 3DS),
 * each owning its own folder list, log viewer, manifest browser and scan
 * pipeline. The legacy constructor `new LocalPkgIndexerWindow(LocalPkgPlatform)`
 * still works — it just opens the hub with that platform's tab active.
 *
 * What changed in v2.61:
 *   • Single Form for all five platforms (was: 5 separate windows).
 *   • Folder list as a ListView with [Path | Exists? | Files] columns + drag-drop
 *     from Explorer. "Save folders" is its own button so you can update the list
 *     without re-running the scan.
 *   • Real progress bar — `LocalPkgIndexer.CountFiles` pre-counts what the scan
 *     will visit so the percentage is meaningful instead of a marquee.
 *   • Stop button works mid-scan — `LocalPkgIndexer.BuildIndex` now takes a
 *     CancellationToken and aborts at the next iteration when cancelled.
 *   • Log is a ListView with [Type | Source | Detail] columns, colour-coded
 *     (green idx / grey skip / red error / dim seen) and four filter checkboxes
 *     to mute/show each type. Right-click a row to copy or reveal in Explorer.
 *   • Manifest viewer below the log: TreeView reading the persisted JSON, one
 *     root node per title id with collapsed Updates(N) / DLCs(M) children.
 *   • "Scan ALL platforms" button at the bottom runs all five sequentially.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ArchiveCacheManager
{
    public class LocalPkgIndexerWindow : Form
    {
        private TabControl mTabs;
        private Button mScanAllButton;
        private Button mCloseButton;
        private Label mGlobalStatus;
        private readonly Dictionary<LocalPkgPlatform, PlatformTab> mTabsByPlatform = new Dictionary<LocalPkgPlatform, PlatformTab>();

        // Backwards-compatible entry point — callers that used the old "one window per
        // platform" pattern still work; we just pre-select the matching tab.
        public LocalPkgIndexerWindow(LocalPkgPlatform initialPlatform)
        {
            BuildUi();
            UserInterface.ApplyTheme(this);
            if (mTabsByPlatform.TryGetValue(initialPlatform, out var tab)) mTabs.SelectedTab = tab.Page;
        }

        public LocalPkgIndexerWindow() : this(LocalPkgPlatform.Ps3) { }

        private void BuildUi()
        {
            Text = "Local Mirror Indexer";
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new System.Drawing.Size(880, 620);
            ClientSize = new System.Drawing.Size(1000, 720);

            mTabs = new TabControl
            {
                Location = new System.Drawing.Point(10, 10),
                Size = new System.Drawing.Size(980, 660),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
            };

            foreach (LocalPkgPlatform platform in new[]
                { LocalPkgPlatform.Ps3, LocalPkgPlatform.Psp, LocalPkgPlatform.Psv, LocalPkgPlatform.Wiiu, LocalPkgPlatform.Ctr3ds })
            {
                var tab = new PlatformTab(this, platform);
                mTabsByPlatform[platform] = tab;
                mTabs.TabPages.Add(tab.Page);
            }

            mTabs.SelectedIndexChanged += (s, e) =>
            {
                var page = mTabs.SelectedTab;
                if (page == null) return;
                foreach (var t in mTabsByPlatform.Values)
                    if (t.Page == page) t.OnActivated();
            };

            mScanAllButton = new Button
            {
                Text = "Scan ALL platforms",
                Location = new System.Drawing.Point(10, 678),
                Width = 160, Height = 30,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
            };
            mScanAllButton.Click += async (s, e) => await ScanAll();

            mGlobalStatus = new Label
            {
                Location = new System.Drawing.Point(180, 685),
                Size = new System.Drawing.Size(680, 18),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Text = "Ready.",
            };

            mCloseButton = new Button
            {
                Text = "Close",
                Location = new System.Drawing.Point(910, 678),
                Width = 80, Height = 30,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            };
            mCloseButton.Click += (s, e) => Close();

            Controls.AddRange(new Control[] { mTabs, mScanAllButton, mGlobalStatus, mCloseButton });
            CancelButton = mCloseButton;

            // Activate the first tab so its manifest tree loads immediately.
            mTabs.HandleCreated += (s, e) =>
            {
                if (mTabs.SelectedTab != null)
                    foreach (var t in mTabsByPlatform.Values)
                        if (t.Page == mTabs.SelectedTab) t.OnActivated();
            };
        }

        private async Task ScanAll()
        {
            mScanAllButton.Enabled = false;
            try
            {
                foreach (var t in mTabsByPlatform.Values)
                {
                    mGlobalStatus.Text = string.Format("Scanning {0} …", t.Platform);
                    mTabs.SelectedTab = t.Page;
                    await t.RunScan();
                    if (t.LastWasCancelled) { mGlobalStatus.Text = "Cancelled."; break; }
                }
                if (!mTabsByPlatform.Values.Any(t => t.LastWasCancelled))
                    mGlobalStatus.Text = "All platforms scanned.";
            }
            finally
            {
                mScanAllButton.Enabled = true;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            foreach (var t in mTabsByPlatform.Values) t.CancelIfRunning();
            base.OnFormClosing(e);
        }

        // ───────────────────────────────────────────────────────────────────────
        // One PlatformTab per platform. Owns its own folders / log / manifest / scan.
        // ───────────────────────────────────────────────────────────────────────
        private class PlatformTab
        {
            public readonly LocalPkgPlatform Platform;
            public readonly TabPage Page;
            public bool LastWasCancelled;

            private readonly LocalPkgIndexerWindow mOwner;

            private ListView mFolders;
            private Button mAddFolder, mRemoveFolder, mSaveFolders, mOpenFolder;
            private Button mScanButton, mStopButton;
            private ProgressBar mProgress;
            private Label mCounters, mCurrentFile;
            private SplitContainer mBottomSplit;
            private ListView mLogList;
            private CheckBox mFltIdx, mFltSkip, mFltError, mFltSeen;
            private TreeView mManifestTree;
            private Label mManifestStatus;

            private readonly List<LogEntry> mLogStore = new List<LogEntry>();
            private CancellationTokenSource mCts;
            private bool mRunning;
            private bool mManifestLoadedOnce;

            public PlatformTab(LocalPkgIndexerWindow owner, LocalPkgPlatform platform)
            {
                mOwner = owner;
                Platform = platform;
                Page = new TabPage(platform.ToString().ToUpperInvariant());
                BuildTabContents();
                LoadConfiguredFolders();
            }

            // ── UI construction ──────────────────────────────────────────────
            private void BuildTabContents()
            {
                // Folders pane
                var lblFolders = new Label
                {
                    Text = "Folders to scan (recursive; drop folders here from Explorer):",
                    Location = new System.Drawing.Point(8, 8),
                    AutoSize = true,
                };
                mFolders = new ListView
                {
                    Location = new System.Drawing.Point(8, 30),
                    Size = new System.Drawing.Size(720, 120),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                    View = View.Details,
                    FullRowSelect = true,
                    GridLines = true,
                    AllowDrop = true,
                };
                mFolders.Columns.Add("Path", 540);
                mFolders.Columns.Add("Status", 70);
                mFolders.Columns.Add("Files", 80);
                mFolders.DragEnter += (s, e) =>
                {
                    e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
                };
                mFolders.DragDrop += (s, e) =>
                {
                    if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
                    var dropped = (string[])e.Data.GetData(DataFormats.FileDrop);
                    foreach (var p in dropped)
                    {
                        if (string.IsNullOrEmpty(p) || !Directory.Exists(p)) continue;
                        if (!FoldersContains(p)) AddFolderRow(p);
                    }
                    UpdateFoldersFileCount();
                };

                mAddFolder = new Button { Text = "Add…",       Location = new System.Drawing.Point(736, 30),  Width = 116, Height = 26, Anchor = AnchorStyles.Top | AnchorStyles.Right };
                mRemoveFolder = new Button { Text = "Remove",  Location = new System.Drawing.Point(736, 60),  Width = 116, Height = 26, Anchor = AnchorStyles.Top | AnchorStyles.Right };
                mSaveFolders = new Button { Text = "Save list",Location = new System.Drawing.Point(736, 90),  Width = 116, Height = 26, Anchor = AnchorStyles.Top | AnchorStyles.Right };
                mOpenFolder = new Button { Text = "Open in Explorer", Location = new System.Drawing.Point(736, 120), Width = 116, Height = 26, Anchor = AnchorStyles.Top | AnchorStyles.Right };

                mAddFolder.Click += (s, e) =>
                {
                    using (var dlg = new FolderBrowserDialog())
                    {
                        if (dlg.ShowDialog(Page.FindForm()) == DialogResult.OK
                            && !string.IsNullOrEmpty(dlg.SelectedPath)
                            && !FoldersContains(dlg.SelectedPath))
                        {
                            AddFolderRow(dlg.SelectedPath);
                            UpdateFoldersFileCount();
                        }
                    }
                };
                mRemoveFolder.Click += (s, e) =>
                {
                    foreach (ListViewItem it in mFolders.SelectedItems.Cast<ListViewItem>().ToList()) mFolders.Items.Remove(it);
                };
                mSaveFolders.Click += (s, e) => { PersistFolders(); mOwner.mGlobalStatus.Text = string.Format("{0} folder list saved.", Platform); };
                mOpenFolder.Click += (s, e) =>
                {
                    if (mFolders.SelectedItems.Count == 0) return;
                    string p = mFolders.SelectedItems[0].Text;
                    if (Directory.Exists(p)) try { Process.Start(new ProcessStartInfo("explorer.exe", "\"" + p + "\"") { UseShellExecute = true }); } catch { }
                };

                // Scan controls row
                mScanButton = new Button { Text = "Scan + Save", Location = new System.Drawing.Point(8, 158), Width = 110, Height = 28 };
                mStopButton = new Button { Text = "Stop",        Location = new System.Drawing.Point(122, 158), Width = 70,  Height = 28, Enabled = false };
                mScanButton.Click += async (s, e) => await RunScan();
                mStopButton.Click += (s, e) => mCts?.Cancel();

                // Filter checkboxes
                mFltIdx   = new CheckBox { Text = "idx",   Location = new System.Drawing.Point(206, 162), Width = 48, Checked = true };
                mFltSkip  = new CheckBox { Text = "skip",  Location = new System.Drawing.Point(256, 162), Width = 56, Checked = true };
                mFltError = new CheckBox { Text = "error", Location = new System.Drawing.Point(316, 162), Width = 58, Checked = true };
                mFltSeen  = new CheckBox { Text = "seen",  Location = new System.Drawing.Point(378, 162), Width = 58, Checked = false };
                foreach (var cb in new[] { mFltIdx, mFltSkip, mFltError, mFltSeen })
                    cb.CheckedChanged += (s, e) => RefreshLogView();

                mProgress = new ProgressBar
                {
                    Location = new System.Drawing.Point(442, 162),
                    Size = new System.Drawing.Size(286, 18),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                    Style = ProgressBarStyle.Continuous,
                };
                mCounters = new Label
                {
                    Location = new System.Drawing.Point(442, 184),
                    Size = new System.Drawing.Size(410, 16),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                    Text = "—",
                };
                mCurrentFile = new Label
                {
                    Location = new System.Drawing.Point(8, 196),
                    Size = new System.Drawing.Size(848, 16),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                    Text = "",
                    ForeColor = System.Drawing.SystemColors.GrayText,
                };

                // Bottom split: log + manifest
                mBottomSplit = new SplitContainer
                {
                    Location = new System.Drawing.Point(8, 220),
                    Size = new System.Drawing.Size(844, 388),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                    Orientation = Orientation.Vertical,
                    SplitterDistance = 500,
                };

                mLogList = new ListView
                {
                    Dock = DockStyle.Fill,
                    View = View.Details,
                    FullRowSelect = true,
                    GridLines = false,
                    Font = new System.Drawing.Font("Consolas", 8.5f),
                    VirtualMode = false,
                };
                mLogList.Columns.Add("Type", 56);
                mLogList.Columns.Add("Source", 220);
                mLogList.Columns.Add("Detail", 700);
                mLogList.ContextMenuStrip = BuildLogContextMenu();
                mBottomSplit.Panel1.Controls.Add(mLogList);

                mManifestTree = new TreeView
                {
                    Dock = DockStyle.Fill,
                    Font = new System.Drawing.Font("Segoe UI", 8.5f),
                    HideSelection = false,
                };
                mManifestTree.NodeMouseDoubleClick += (s, e) =>
                {
                    if (e.Node?.Tag is string p && File.Exists(p))
                        try { Process.Start(new ProcessStartInfo("explorer.exe", "/select,\"" + p + "\"") { UseShellExecute = true }); } catch { }
                };
                mManifestStatus = new Label
                {
                    Dock = DockStyle.Top,
                    Height = 22,
                    Text = "Indexed titles (no scan yet)",
                    ForeColor = System.Drawing.SystemColors.GrayText,
                    TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                };
                mBottomSplit.Panel2.Controls.Add(mManifestTree);
                mBottomSplit.Panel2.Controls.Add(mManifestStatus);

                Page.Controls.AddRange(new Control[] {
                    lblFolders, mFolders,
                    mAddFolder, mRemoveFolder, mSaveFolders, mOpenFolder,
                    mScanButton, mStopButton,
                    mFltIdx, mFltSkip, mFltError, mFltSeen,
                    mProgress, mCounters, mCurrentFile,
                    mBottomSplit,
                });
            }

            private ContextMenuStrip BuildLogContextMenu()
            {
                var cm = new ContextMenuStrip();
                var copyItem = new ToolStripMenuItem("Copy line");
                copyItem.Click += (s, e) =>
                {
                    if (mLogList.SelectedItems.Count > 0)
                        Clipboard.SetText(string.Join("\t", Enumerable.Range(0, mLogList.Columns.Count)
                            .Select(i => mLogList.SelectedItems[0].SubItems[i].Text)));
                };
                var revealItem = new ToolStripMenuItem("Reveal in Explorer");
                revealItem.Click += (s, e) =>
                {
                    if (mLogList.SelectedItems.Count == 0) return;
                    string src = mLogList.SelectedItems[0].SubItems[1].Text;
                    int bang = src.IndexOf('!');
                    string real = bang > 0 ? src.Substring(0, bang) : src;
                    if (File.Exists(real)) try { Process.Start(new ProcessStartInfo("explorer.exe", "/select,\"" + real + "\"") { UseShellExecute = true }); } catch { }
                };
                cm.Items.Add(copyItem);
                cm.Items.Add(revealItem);
                return cm;
            }

            // ── Folder list helpers ──────────────────────────────────────────
            private bool FoldersContains(string path)
            {
                foreach (ListViewItem it in mFolders.Items)
                    if (string.Equals(it.Text, path, StringComparison.OrdinalIgnoreCase)) return true;
                return false;
            }

            private ListViewItem AddFolderRow(string path)
            {
                var item = new ListViewItem(new[] { path, Directory.Exists(path) ? "OK" : "missing", "—" });
                if (!Directory.Exists(path)) item.ForeColor = System.Drawing.Color.IndianRed;
                mFolders.Items.Add(item);
                return item;
            }

            private void UpdateFoldersFileCount()
            {
                foreach (ListViewItem it in mFolders.Items)
                {
                    string p = it.Text;
                    if (!Directory.Exists(p)) { it.SubItems[1].Text = "missing"; it.SubItems[2].Text = "—"; continue; }
                    try
                    {
                        int n = LocalPkgIndexer.CountFiles(Platform, new[] { p });
                        it.SubItems[1].Text = "OK";
                        it.SubItems[2].Text = n.ToString();
                    }
                    catch { it.SubItems[2].Text = "?"; }
                }
            }

            private void LoadConfiguredFolders()
            {
                var folders = LocalPkgIndexer.FoldersForPlatform(Platform);
                mFolders.Items.Clear();
                foreach (var f in folders) AddFolderRow(f);
            }

            private void PersistFolders()
            {
                var items = new List<string>();
                foreach (ListViewItem it in mFolders.Items) items.Add(it.Text);
                string csv = string.Join("|", items);
                switch (Platform)
                {
                    case LocalPkgPlatform.Ps3:    Config.Ps3LocalPkgFolders    = csv; break;
                    case LocalPkgPlatform.Psp:    Config.PspLocalPkgFolders    = csv; break;
                    case LocalPkgPlatform.Psv:    Config.PsvLocalPkgFolders    = csv; break;
                    case LocalPkgPlatform.Wiiu:   Config.WiiuLocalRomFolders   = csv; break;
                    case LocalPkgPlatform.Ctr3ds: Config.Ctr3dsLocalRomFolders = csv; break;
                }
                Config.Save();
            }

            // ── Tab activation: lazy-load manifest tree + file counts ────────
            public void OnActivated()
            {
                if (!mManifestLoadedOnce)
                {
                    mManifestLoadedOnce = true;
                    LoadManifestIntoTree();
                    UpdateFoldersFileCount();
                }
            }

            private void LoadManifestIntoTree()
            {
                mManifestTree.BeginUpdate();
                mManifestTree.Nodes.Clear();
                var manifest = LocalPkgIndexer.Load(Platform);
                if (manifest == null || manifest.Titles.Count == 0)
                {
                    mManifestStatus.Text = "Indexed titles (no manifest yet — run a scan)";
                    mManifestTree.EndUpdate();
                    return;
                }
                int totUpd = 0, totDlc = 0;
                foreach (var kv in manifest.Titles.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
                {
                    string tid = kv.Key;
                    var t = kv.Value;
                    var node = new TreeNode(string.Format("{0}   ({1} update, {2} DLC)", tid, t.Updates.Count, t.Dlcs.Count));
                    foreach (var u in t.Updates)
                    {
                        node.Nodes.Add(new TreeNode("Update — " + DescribeEntry(u)) { Tag = ResolveDisplayPath(u) });
                        totUpd++;
                    }
                    foreach (var d in t.Dlcs)
                    {
                        node.Nodes.Add(new TreeNode("DLC    — " + DescribeEntry(d)) { Tag = ResolveDisplayPath(d) });
                        totDlc++;
                    }
                    mManifestTree.Nodes.Add(node);
                }
                mManifestStatus.Text = string.Format("Indexed titles: {0} titles, {1} updates, {2} DLCs (double-click an entry to reveal in Explorer)",
                    manifest.Titles.Count, totUpd, totDlc);
                mManifestTree.EndUpdate();
            }

            private static string DescribeEntry(LocalPkgEntry e)
            {
                string src = e.ArchivePath != null ? Path.GetFileName(e.ArchivePath) : Path.GetFileName(e.PkgPath ?? "<unknown>");
                string size = e.Size > 0 ? string.Format(" [{0:N0} MB]", e.Size / 1024 / 1024) : "";
                string cid = !string.IsNullOrEmpty(e.ContentId) ? "  " + e.ContentId : "";
                return src + size + cid;
            }

            private static string ResolveDisplayPath(LocalPkgEntry e)
            {
                return e.ArchivePath ?? e.PkgPath;
            }

            // ── Log ──────────────────────────────────────────────────────────
            private static readonly System.Drawing.Color ColorIdx   = System.Drawing.Color.FromArgb(0, 128, 0);
            private static readonly System.Drawing.Color ColorSkip  = System.Drawing.Color.FromArgb(110, 110, 110);
            private static readonly System.Drawing.Color ColorError = System.Drawing.Color.FromArgb(192, 0, 0);
            private static readonly System.Drawing.Color ColorSeen  = System.Drawing.Color.FromArgb(160, 160, 200);

            private void AppendLog(LogEntry e)
            {
                mLogStore.Add(e);
                if (PassesFilter(e)) AddLogRowToView(e);
            }

            private bool PassesFilter(LogEntry e)
            {
                switch (e.Type)
                {
                    case "idx":   return mFltIdx.Checked;
                    case "skip":  return mFltSkip.Checked;
                    case "error": return mFltError.Checked;
                    case "seen":  return mFltSeen.Checked;
                    default:      return true;
                }
            }

            private void AddLogRowToView(LogEntry e)
            {
                var item = new ListViewItem(new[] { e.Type, e.Source ?? "", e.Detail ?? "" });
                switch (e.Type)
                {
                    case "idx":   item.ForeColor = ColorIdx;   break;
                    case "skip":  item.ForeColor = ColorSkip;  break;
                    case "error": item.ForeColor = ColorError; break;
                    case "seen":  item.ForeColor = ColorSeen;  break;
                }
                mLogList.Items.Add(item);
                item.EnsureVisible();
            }

            private void RefreshLogView()
            {
                mLogList.BeginUpdate();
                mLogList.Items.Clear();
                foreach (var e in mLogStore) if (PassesFilter(e)) AddLogRowToView(e);
                mLogList.EndUpdate();
            }

            // ── Scan ─────────────────────────────────────────────────────────
            public async Task RunScan()
            {
                if (mRunning) return;
                if (mFolders.Items.Count == 0)
                {
                    UserInterface.ErrorDialog("Add at least one folder before scanning.");
                    return;
                }
                LastWasCancelled = false;
                mRunning = true;
                mCts = new CancellationTokenSource();
                mScanButton.Enabled = false;
                mStopButton.Enabled = true;
                mAddFolder.Enabled = false;
                mRemoveFolder.Enabled = false;
                mSaveFolders.Enabled = false;
                mLogStore.Clear();
                mLogList.Items.Clear();
                mProgress.Value = 0;
                mCurrentFile.Text = "";

                var folders = new List<string>();
                foreach (ListViewItem it in mFolders.Items) folders.Add(it.Text);
                PersistFolders();

                // Pre-pass count for the real progress bar.
                int total = 0;
                try { total = await Task.Run(() => LocalPkgIndexer.CountFiles(Platform, folders)); }
                catch { total = 0; }
                mProgress.Maximum = Math.Max(1, total);
                mCounters.Text = string.Format("Pre-counted {0:N0} files. Scanning…", total);

                LocalPkgManifest manifest = null;
                string savedPath = null;
                try
                {
                    string lastSeen = null;
                    manifest = await Task.Run(() =>
                        LocalPkgIndexer.BuildIndex(Platform, folders, prog =>
                        {
                            if (Page.FindForm()?.IsDisposed ?? true) return;
                            string currentFile = prog.CurrentFile;
                            string logLine = prog.LogLine;
                            int seen = prog.FilesSeen;
                            int idx = prog.Indexed, upd = prog.Updates, dlc = prog.Dlcs, skip = prog.Skipped, err = prog.Errors;
                            bool freshSeen = currentFile != null && currentFile != lastSeen;
                            if (freshSeen) lastSeen = currentFile;
                            Page.FindForm()?.BeginInvoke((Action)(() =>
                            {
                                int v = Math.Min(mProgress.Maximum, seen);
                                if (v != mProgress.Value) mProgress.Value = v;
                                int pct = total > 0 ? Math.Min(100, seen * 100 / total) : 0;
                                mCounters.Text = string.Format("{0} / {1} ({2}%) — idx {3} (upd {4}, dlc {5}), skip {6}, err {7}",
                                    seen, total, pct, idx, upd, dlc, skip, err);
                                mCurrentFile.Text = currentFile != null ? "…" + Path.GetFileName(currentFile) : "";
                                if (freshSeen) AppendLog(new LogEntry("seen", currentFile, ""));
                                if (!string.IsNullOrEmpty(logLine)) AppendLog(LogEntry.Parse(logLine));
                            }));
                        }, mCts.Token), mCts.Token).ConfigureAwait(true);

                    savedPath = LocalPkgIndexer.Save(manifest, Platform);
                }
                catch (OperationCanceledException) { AppendLog(new LogEntry("error", "(scan)", "cancelled by user")); LastWasCancelled = true; }
                catch (Exception ex)               { AppendLog(new LogEntry("error", "(scan)", ex.Message)); }

                mScanButton.Enabled = true;
                mStopButton.Enabled = false;
                mAddFolder.Enabled = true;
                mRemoveFolder.Enabled = true;
                mSaveFolders.Enabled = true;
                mRunning = false;

                if (manifest != null && !LastWasCancelled)
                {
                    int totalUpd = 0, totalDlc = 0;
                    foreach (var t in manifest.Titles) { totalUpd += t.Value.Updates.Count; totalDlc += t.Value.Dlcs.Count; }
                    mCounters.Text = string.Format("Done. {0} titles, {1} updates, {2} DLCs.", manifest.Titles.Count, totalUpd, totalDlc);
                    AppendLog(new LogEntry("idx", "(done)", string.Format("{0} titles indexed, manifest at {1}", manifest.Titles.Count, savedPath ?? "<unknown>")));
                    LoadManifestIntoTree();
                }
                else if (LastWasCancelled)
                {
                    mCounters.Text = "Cancelled.";
                }
            }

            public void CancelIfRunning() { if (mRunning) mCts?.Cancel(); }
        }

        // ── Log model ─────────────────────────────────────────────────────────
        private struct LogEntry
        {
            public string Type;
            public string Source;
            public string Detail;

            public LogEntry(string type, string source, string detail)
            {
                Type = type;
                Source = source;
                Detail = detail;
            }

            // Parses the `[type] source — detail` shape emitted by LocalPkgIndexer.EmitLog.
            // Falls back to (type=type, source=rest, detail="") when the pattern doesn't match.
            public static LogEntry Parse(string line)
            {
                if (string.IsNullOrEmpty(line)) return new LogEntry("", "", "");
                string type = ""; string body = line;
                if (line.Length > 2 && line[0] == '[')
                {
                    int close = line.IndexOf(']');
                    if (close > 0 && close < line.Length - 1)
                    {
                        type = line.Substring(1, close - 1).Trim();
                        body = line.Substring(close + 1).TrimStart();
                    }
                }
                int sep = body.IndexOf(" — ", StringComparison.Ordinal);
                if (sep < 0) sep = body.IndexOf(": ", StringComparison.Ordinal);
                if (sep > 0)
                    return new LogEntry(type, body.Substring(0, sep).Trim(), body.Substring(sep + 1).TrimStart(' ', '—', ':'));
                return new LogEntry(type, body.Trim(), "");
            }
        }
    }
}
