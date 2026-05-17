/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Library-wide purge of update/DLC entries.
 *
 * What it does:
 *   Reads every persisted LocalPkgIndexer manifest (PS3 / PSP / PSV / Wii U / 3DS)
 *   via `LibraryPurgeCandidates.EnumerateAll()`, builds a path → kind/title-id
 *   lookup, then walks `PluginHelper.DataManager.GetAllGames()` once. For each
 *   library entry whose `ApplicationPath` matches a known update or DLC path, the
 *   game is offered as a purge candidate. Selected entries are removed from the
 *   LaunchBox library via `IDataManager.TryRemoveGame` — the underlying file on
 *   disk is NEVER touched, only the library record.
 *
 * Scope deliberately limited to standalone files (PkgPath set, ArchivePath null
 * in the manifest entry). Zip-wrapped collections are skipped because a wrapper
 * may also contain the base game and we cannot tell from the manifest alone.
 * See LibraryPurgeCandidates.cs for the rationale.
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;

namespace ArchiveCacheManager
{
    public class LibraryPurgeWindow : Form
    {
        private DataGridView mGrid;
        private Label mSummary;
        private Button mSelectAll;
        private Button mSelectNone;
        private Button mSelectSafe;
        private Button mRefresh;
        private Button mPurge;
        private Button mClose;

        private readonly List<MatchRow> mMatches = new List<MatchRow>();

        public bool RefreshLaunchBox { get; private set; }

        private class MatchRow
        {
            public IGame Game;
            public LibraryPurgeCandidate Candidate;
        }

        public LibraryPurgeWindow()
        {
            BuildUi();
            UserInterface.ApplyTheme(this);
            Shown += (s, e) => RebuildMatches();
        }

        private void BuildUi()
        {
            Text = "Purge Library Entries (Update / DLC / Theme / System / Demo / Other)";
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(820, 520);
            ClientSize = new Size(960, 600);

            mGrid = new DataGridView
            {
                Location = new Point(12, 12),
                Size = new Size(936, 500),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoGenerateColumns = false,
                MultiSelect = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                EditMode = DataGridViewEditMode.EditOnEnter,
                BackgroundColor = SystemColors.Window,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            };

            var colSelect = new DataGridViewCheckBoxColumn { Name = "Select", HeaderText = "", Width = 36, Resizable = DataGridViewTriState.False };
            var colTitle  = new DataGridViewTextBoxColumn  { Name = "Title", HeaderText = "Title", Width = 260, ReadOnly = true };
            var colKind   = new DataGridViewTextBoxColumn  { Name = "Kind", HeaderText = "Kind", Width = 64, ReadOnly = true };
            var colPlat   = new DataGridViewTextBoxColumn  { Name = "Platform", HeaderText = "Platform", Width = 110, ReadOnly = true };
            var colLbPlat = new DataGridViewTextBoxColumn  { Name = "LbPlatform", HeaderText = "LB Platform", Width = 130, ReadOnly = true };
            var colTid    = new DataGridViewTextBoxColumn  { Name = "TitleId", HeaderText = "Title ID", Width = 100, ReadOnly = true };
            var colPath   = new DataGridViewTextBoxColumn  { Name = "Path", HeaderText = "Application Path", Width = 220, ReadOnly = true };

            mGrid.Columns.AddRange(colSelect, colTitle, colKind, colPlat, colLbPlat, colTid, colPath);
            mGrid.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (mGrid.CurrentCell is DataGridViewCheckBoxCell)
                    mGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };

            mSummary = new Label
            {
                Location = new Point(12, 522),
                Size = new Size(560, 17),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Text = "Scanning…",
            };

            mSelectAll    = new Button { Text = "Check All",       Location = new Point(12,  548), Width = 88, Height = 27, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            mSelectNone   = new Button { Text = "Uncheck All",     Location = new Point(106, 548), Width = 96, Height = 27, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            mSelectSafe   = new Button { Text = "Check Update+DLC", Location = new Point(208, 548), Width = 130, Height = 27, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            mRefresh      = new Button { Text = "Refresh",         Location = new Point(344, 548), Width = 80, Height = 27, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            mPurge        = new Button { Text = "Purge Selected",  Location = new Point(746, 548), Width = 120, Height = 27, Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
            mClose        = new Button { Text = "Close",           Location = new Point(872, 548), Width = 76,  Height = 27, Anchor = AnchorStyles.Bottom | AnchorStyles.Right };

            mSelectAll.Click  += (s, e) => SetAllChecked(true);
            mSelectNone.Click += (s, e) => SetAllChecked(false);
            mSelectSafe.Click += (s, e) => SetCheckedForKind(k => LibraryPurgeKind.IsDefaultChecked(k));
            mRefresh.Click    += (s, e) => RebuildMatches();
            mPurge.Click      += (s, e) => DoPurge();
            mClose.Click      += (s, e) => Close();

            Controls.AddRange(new Control[] { mGrid, mSummary, mSelectAll, mSelectNone, mSelectSafe, mRefresh, mPurge, mClose });
            CancelButton = mClose;
        }

        private void RebuildMatches()
        {
            mGrid.Rows.Clear();
            mMatches.Clear();
            mSummary.Text = "Scanning…";
            Application.DoEvents();

            Dictionary<string, LibraryPurgeCandidate> byPath;
            try
            {
                byPath = LibraryPurgeCandidates.EnumerateAll()
                    .GroupBy(c => c.PathKey, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Logger.Log(string.Format("LibraryPurgeWindow: manifest scan failed: {0}", ex), Logger.LogLevel.Exception);
                mSummary.Text = "Failed to read manifests — see log.";
                return;
            }

            if (byPath.Count == 0)
            {
                mSummary.Text = "No Local Mirror manifests found. Run the Local PKG Indexer first.";
                mPurge.Enabled = false;
                return;
            }

            int scanned = 0;
            try
            {
                foreach (var g in PluginHelper.DataManager.GetAllGames())
                {
                    if (g == null || string.IsNullOrWhiteSpace(g.ApplicationPath)) continue;
                    scanned++;

                    string normalized = LibraryPurgeCandidates.NormalizeForLookup(PathUtils.GetAbsolutePath(g.ApplicationPath));
                    if (string.IsNullOrEmpty(normalized)) continue;
                    if (!byPath.TryGetValue(normalized, out var candidate)) continue;

                    mMatches.Add(new MatchRow { Game = g, Candidate = candidate });
                    int idx = mGrid.Rows.Add(
                        LibraryPurgeKind.IsDefaultChecked(candidate.Kind),
                        g.Title ?? "<no title>",
                        candidate.Kind,
                        candidate.Platform.ToString(),
                        g.Platform ?? string.Empty,
                        candidate.TitleId ?? string.Empty,
                        g.ApplicationPath
                    );
                    mGrid.Rows[idx].Cells["Path"].ToolTipText = g.ApplicationPath ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(string.Format("LibraryPurgeWindow: library walk failed: {0}", ex), Logger.LogLevel.Exception);
                mSummary.Text = "Library walk failed — see log.";
                return;
            }

            // Per-kind tally for the summary line.
            var byKind = mMatches
                .GroupBy(m => m.Candidate.Kind ?? "?")
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => string.Format("{0} {1}", g.Count(), g.Key))
                .ToArray();
            string breakdown = byKind.Length > 0 ? string.Join(", ", byKind) : "0 matched";
            mSummary.Text = string.Format(
                "Scanned {0} library entries — {1} matched ({2}). Update+DLC ticked by default; files on disk are NOT touched.",
                scanned, mMatches.Count, breakdown);
            mPurge.Enabled = mMatches.Count > 0;
        }

        private void SetAllChecked(bool value)
        {
            foreach (DataGridViewRow row in mGrid.Rows)
                row.Cells["Select"].Value = value;
        }

        private void SetCheckedForKind(Func<string, bool> shouldCheck)
        {
            for (int i = 0; i < mGrid.Rows.Count && i < mMatches.Count; i++)
            {
                string kind = mMatches[i].Candidate.Kind ?? string.Empty;
                mGrid.Rows[i].Cells["Select"].Value = shouldCheck(kind);
            }
        }

        private void DoPurge()
        {
            var selected = new List<MatchRow>();
            for (int i = 0; i < mGrid.Rows.Count; i++)
            {
                if (mGrid.Rows[i].Cells["Select"].Value is bool b && b)
                    selected.Add(mMatches[i]);
            }

            if (selected.Count == 0)
            {
                mSummary.Text = "Nothing selected.";
                return;
            }

            // Per-kind tally for the confirmation dialog so the user knows exactly what they're removing.
            var kindTally = selected
                .GroupBy(m => m.Candidate.Kind ?? "?")
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => string.Format("  • {0}: {1}", g.Key, g.Count()))
                .ToArray();

            var confirm = MessageBox.Show(this,
                string.Format("Remove {0} entr{1} from the LaunchBox library?\r\n\r\n{2}\r\n\r\n" +
                              "Only the library records will be deleted — the underlying files on disk will NOT be touched.\r\n\r\n" +
                              "Updates/DLCs will still be installable through the plugin's update manager " +
                              "(the indexed source files remain on disk).",
                              selected.Count, selected.Count == 1 ? "y" : "ies",
                              string.Join("\r\n", kindTally)),
                "Purge Library Entries", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            int removed = 0, failed = 0;
            foreach (var m in selected)
            {
                try
                {
                    if (PluginHelper.DataManager.TryRemoveGame(m.Game))
                    {
                        removed++;
                        Logger.Log(string.Format("LibraryPurge: removed library entry '{0}' (TID {1}, {2}).",
                            m.Game.Title, m.Candidate.TitleId, m.Candidate.Kind));
                    }
                    else
                    {
                        failed++;
                        Logger.Log(string.Format("LibraryPurge: TryRemoveGame returned false for '{0}'.", m.Game.Title));
                    }
                }
                catch (Exception ex)
                {
                    failed++;
                    Logger.Log(string.Format("LibraryPurge: exception removing '{0}': {1}", m.Game.Title, ex), Logger.LogLevel.Exception);
                }
            }

            try { PluginHelper.DataManager.Save(true); }
            catch (Exception ex) { Logger.Log(string.Format("LibraryPurge: Save failed: {0}", ex), Logger.LogLevel.Exception); }

            if (removed > 0) RefreshLaunchBox = true;

            mSummary.Text = string.Format("Removed {0} entr{1} from library, {2} failed.",
                removed, removed == 1 ? "y" : "ies", failed);

            RebuildMatches();
        }
    }
}
