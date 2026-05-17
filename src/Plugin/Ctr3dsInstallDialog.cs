/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Dialog opened by `Ctr3dsInstallUpdatesMenuItem`. Resolves the game's title_id,
 * looks up matching update + DLC entries in `Ctr3dsLocalCache/local_rom_index.json`,
 * and lets the user multi-select which ones to install by invoking the configured
 * 3DS emulator with the CIA path. Citra/Azahar/Lime3DS all accept a CIA path as a
 * positional arg and pop up their install UI; we wait for each to exit before
 * moving on.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;

namespace ArchiveCacheManager
{
    public class Ctr3dsInstallDialog : Form
    {
        private readonly IGame mGame;

        private ListView mList;
        private Label mStatus;
        private TextBox mEmulatorPath;
        private Button mBrowseEmulator;
        private Button mInstallButton;
        private Button mCloseButton;
        private TextBox mLog;

        public Ctr3dsInstallDialog(IGame game)
        {
            mGame = game ?? throw new ArgumentNullException(nameof(game));
            BuildUi();
            UserInterface.ApplyTheme(this);
            PopulateList();
        }

        private void BuildUi()
        {
            Text = string.Format("Install 3DS Updates + DLCs — {0}", mGame.Title);
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(720, 480);
            ClientSize = new Size(880, 560);

            var lblIntro = new Label
            {
                Text = "Indexed updates + DLCs for this title from the local 3DS mirror. Select which to install via Citra / Azahar / Lime3DS, then click Install.",
                Location = new Point(12, 12),
                Size = new Size(856, 36),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                AutoEllipsis = false,
            };

            mList = new ListView
            {
                Location = new Point(12, 56),
                Size = new Size(856, 290),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                View = View.Details,
                CheckBoxes = true,
                FullRowSelect = true,
                GridLines = true,
            };
            mList.Columns.Add("Type",       80);
            mList.Columns.Add("Title ID",   150);
            mList.Columns.Add("Source",     430);
            mList.Columns.Add("Size",       80);

            var lblEmu = new Label
            {
                Text = "3DS emulator (citra-qt.exe / azahar.exe / lime3ds.exe):",
                Location = new Point(12, 358),
                AutoSize = true,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
            };
            mEmulatorPath = new TextBox
            {
                Location = new Point(12, 378),
                Size = new Size(750, 22),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Text = TryDeriveEmulatorPath(),
            };
            mBrowseEmulator = new Button
            {
                Text = "Browse…",
                Location = new Point(770, 376),
                Size = new Size(98, 24),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            };
            mBrowseEmulator.Click += (s, e) =>
            {
                using (var dlg = new OpenFileDialog { Filter = "Executables (*.exe)|*.exe|All files (*.*)|*.*" })
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK) mEmulatorPath.Text = dlg.FileName;
                }
            };

            mLog = new TextBox
            {
                Location = new Point(12, 410),
                Size = new Size(856, 100),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 8.5f),
            };

            mStatus = new Label
            {
                Location = new Point(12, 520),
                Size = new Size(530, 18),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Text = "",
            };
            mInstallButton = new Button
            {
                Text = "Install selected",
                Location = new Point(680, 516),
                Size = new Size(110, 27),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            };
            mInstallButton.Click += async (s, e) => await DoInstall();
            mCloseButton = new Button
            {
                Text = "Close",
                Location = new Point(796, 516),
                Size = new Size(72, 27),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            };
            mCloseButton.Click += (s, e) => Close();
            CancelButton = mCloseButton;

            Controls.AddRange(new Control[] {
                lblIntro, mList,
                lblEmu, mEmulatorPath, mBrowseEmulator,
                mLog, mStatus, mInstallButton, mCloseButton,
            });
        }

        private void PopulateList()
        {
            mList.Items.Clear();

            string archive = PluginUtils.GetArchivePath(mGame, null);
            ulong? tid = Ctr3dsTitleIdResolver.Resolve(archive);
            if (tid == null)
            {
                mStatus.Text = string.Format("Title ID couldn't be resolved from {0}.", Path.GetFileName(archive ?? "<no archive>"));
                mInstallButton.Enabled = false;
                return;
            }
            uint low = (uint)(tid.Value & 0xFFFFFFFF);
            string baseTid = "00040000" + low.ToString("X8");

            var manifest = LocalPkgIndexer.Load(LocalPkgPlatform.Ctr3ds);
            if (manifest == null || manifest.Titles == null)
            {
                mStatus.Text = "No 3DS mirror manifest. Run the indexer first (Local Mirror Folders → 3DS).";
                mInstallButton.Enabled = false;
                return;
            }
            if (!manifest.Titles.TryGetValue(baseTid, out var bucket) || bucket == null)
            {
                mStatus.Text = string.Format("No updates / DLCs indexed for base TID {0}.", baseTid);
                mInstallButton.Enabled = false;
                return;
            }

            int rows = 0;
            foreach (var u in bucket.Updates ?? new List<LocalPkgEntry>())
            {
                if (!IsInstallable(u)) continue;
                AddRow("Update", u);
                rows++;
            }
            foreach (var d in bucket.Dlcs ?? new List<LocalPkgEntry>())
            {
                if (!IsInstallable(d)) continue;
                AddRow("DLC", d);
                rows++;
            }

            mStatus.Text = string.Format("Game TID={0:X16}, base={1}: {2} installable entries. Tick boxes and click Install.",
                tid.Value, baseTid, rows);
            mInstallButton.Enabled = rows > 0;
        }

        // Today we only install actual CIA files (the manifest also indexes raw TMDs from NUS
        // dumps but those can't be installed without cdecrypt). Filter to entries whose source
        // is a `.cia` on disk.
        private static bool IsInstallable(LocalPkgEntry e)
        {
            if (string.IsNullOrEmpty(e?.PkgPath)) return false;
            if (e.PkgPath.IndexOf('!') >= 0) return false;   // zip-wrapper entries, can't pass to Citra directly
            if (!e.PkgPath.EndsWith(".cia", StringComparison.OrdinalIgnoreCase)) return false;
            return File.Exists(e.PkgPath);
        }

        private void AddRow(string kind, LocalPkgEntry e)
        {
            string size = e.Size > 0 ? string.Format("{0:N0} MB", e.Size / 1024 / 1024) : "—";
            var item = new ListViewItem(new[] { kind, e.TitleId ?? "", e.PkgPath, size }) { Tag = e };
            mList.Items.Add(item);
        }

        private async System.Threading.Tasks.Task DoInstall()
        {
            string emu = mEmulatorPath.Text.Trim();
            if (string.IsNullOrEmpty(emu) || !File.Exists(PathUtils.GetAbsolutePath(emu)))
            {
                UserInterface.ErrorDialog("Set the 3DS emulator executable path first.");
                return;
            }
            string emuAbs = PathUtils.GetAbsolutePath(emu);

            var selected = mList.CheckedItems.Cast<ListViewItem>().ToList();
            if (selected.Count == 0)
            {
                UserInterface.ErrorDialog("Tick at least one update / DLC to install.");
                return;
            }

            mInstallButton.Enabled = false;
            mBrowseEmulator.Enabled = false;
            mLog.Clear();
            AppendLog(string.Format("Installing {0} title(s) via {1}…", selected.Count, Path.GetFileName(emuAbs)));

            int ok = 0, fail = 0;
            foreach (var it in selected)
            {
                if (!(it.Tag is LocalPkgEntry e)) continue;
                AppendLog(string.Format("→ {0}  {1}", it.SubItems[0].Text, e.PkgPath));
                try
                {
                    int rc = await System.Threading.Tasks.Task.Run(() => InvokeEmulatorInstall(emuAbs, e.PkgPath));
                    if (rc == 0) { ok++; AppendLog(string.Format("    [ok]   rc=0")); }
                    else         { fail++; AppendLog(string.Format("    [fail] rc={0}", rc)); }
                }
                catch (Exception ex)
                {
                    fail++;
                    AppendLog("    [error] " + ex.Message);
                    Logger.Log(string.Format("Ctr3dsInstallDialog: invoke failed for {0}: {1}", e.PkgPath, ex), Logger.LogLevel.Exception);
                }
            }

            mStatus.Text = string.Format("Done. {0} installed, {1} failed.", ok, fail);
            mInstallButton.Enabled = true;
            mBrowseEmulator.Enabled = true;
        }

        private static int InvokeEmulatorInstall(string emulatorAbs, string ciaPath)
        {
            // Citra/Azahar/Lime3DS take a CIA file as positional arg and present an install
            // dialog (or proceed silently on Azahar-recent builds). We wait for the process to
            // exit so subsequent installs don't trample each other.
            var p = new Process();
            p.StartInfo.FileName = emulatorAbs;
            p.StartInfo.Arguments = "\"" + ciaPath + "\"";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.WorkingDirectory = Path.GetDirectoryName(emulatorAbs) ?? "";
            p.Start();
            p.WaitForExit();
            return p.ExitCode;
        }

        private string TryDeriveEmulatorPath()
        {
            try
            {
                if (mGame == null) return "";
                var emu = PluginHelper.DataManager.GetEmulatorById(mGame.EmulatorId);
                if (emu != null && !string.IsNullOrWhiteSpace(emu.ApplicationPath))
                    return PathUtils.GetAbsolutePath(emu.ApplicationPath);
            }
            catch { }
            return "";
        }

        private void AppendLog(string line)
        {
            if (mLog.IsDisposed) return;
            mLog.AppendText(line + Environment.NewLine);
        }
    }
}
