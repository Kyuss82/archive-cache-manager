using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Drawing;
using Unbroken.LaunchBox.Plugins;
using System.Threading.Tasks;

namespace ArchiveCacheManager
{
    public partial class NewConfigWindow : Form
    {
        public bool RefreshLaunchBox = false;

        private Dictionary<string, Bitmap> emulatorIcons;

        public NewConfigWindow()
        {
            InitializeComponent();

            UserInterface.SetDoubleBuffered(cacheDataGridView, true);
            UserInterface.SetDoubleBuffered(emulatorPlatformConfigDataGridView, true);
            UserInterface.ApplyTheme(this);

            // v2.62 GUI rework: hide redundant tab strip, expand Packaging into a sub-tree
            // with one node per platform, add a search box above the side navigator.
            SetupNavReworkV262();
            // v2.65: rebuild the Packaging area from scratch with FlowLayout + GroupBoxes,
            // adds a top-level "Local Mirror Folders" TreeView node, removes the 5 per-platform
            // "Manage Local PKG Folders…" buttons. Supersedes v2.64's path-compaction logic.
            RebuildPackagingV265();
            // v2.67: re-apply the dark theme after rebuilding so the new Labels, Buttons and
            // the rehomed CheckBoxes/CheckedListBoxes pick up the themed ForeColor (default
            // black ForeColor on the dark BackColor was rendering label text invisibly).
            UserInterface.ApplyTheme(this);

            emulatorIcons = new Dictionary<string, Bitmap>();
            foreach (var emulator in PluginHelper.DataManager.GetAllEmulators())
            {
                try
                {
                    Bitmap icon = ShellIcon.GetShellIcon(PathUtils.GetAbsolutePath(emulator.ApplicationPath), ShellIcon.IconSize.Small);
                    if (icon != null)
                    {
                        emulatorIcons.Add(emulator.Title, icon);
                    }
                }
                catch (Exception)
                {
                }
            }

            foreach (DataGridViewColumn column in cacheDataGridView.Columns)
            {
                UserInterface.SetColumnMinimumWidth(column);
            }
            foreach (DataGridViewColumn column in emulatorPlatformConfigDataGridView.Columns)
            {
                UserInterface.SetColumnMinimumWidth(column);
            }

            RefreshLaunchBox = false;

            // Right-click "Index Local PS3/PSP/PSV PKG Folders..." moved into a button so users
            // who reach for Tools → Manage → ACM Settings find the folder editor here too.
            ps3LocalPkgFoldersButton.Click += (s, e) => OpenIndexerWindow(LocalPkgPlatform.Ps3);
            pspLocalPkgFoldersButton.Click += (s, e) => OpenIndexerWindow(LocalPkgPlatform.Psp);
            psvLocalPkgFoldersButton.Click += (s, e) => OpenIndexerWindow(LocalPkgPlatform.Psv);
            wiiuLocalRomFoldersButton.Click += (s, e) => OpenIndexerWindow(LocalPkgPlatform.Wiiu);
            ctr3dsLocalRomFoldersButton.Click += (s, e) => OpenIndexerWindow(LocalPkgPlatform.Ctr3ds);

            Config.Load();

            versionLabel.Text = CacheManager.VersionString;

            emulatorPlatformConfigDataGridView.Rows.Clear();

            var actionItems = (emulatorPlatformConfigDataGridView.Columns["Action"] as DataGridViewComboBoxColumn).Items;
            var launchPathItems = (emulatorPlatformConfigDataGridView.Columns["LaunchPath"] as DataGridViewComboBoxColumn).Items;
            var m3uNameItems = (emulatorPlatformConfigDataGridView.Columns["M3uName"] as DataGridViewComboBoxColumn).Items;
            foreach (var config in Config.GetAllEmulatorPlatformConfig())
            {
                string[] priorityInfo = config.Key.Split(new string[] { @"\" }, StringSplitOptions.RemoveEmptyEntries);
                string priorityEmulator = priorityInfo[0].Trim();
                string priorityPlatform = priorityInfo[1].Trim();

                if (string.Equals(priorityEmulator, "All", StringComparison.InvariantCultureIgnoreCase) &&
                    string.Equals(priorityPlatform, "All", StringComparison.InvariantCultureIgnoreCase))
                {
                    emulatorPlatformConfigDataGridView.Rows.Insert(0, new object[] { priorityEmulator,
                                                                                priorityPlatform,
                                                                                config.Value.FilenamePriority,
                                                                                actionItems[(int)config.Value.Action],
                                                                                launchPathItems[(int)config.Value.LaunchPath],
                                                                                config.Value.MultiDisc,
                                                                                m3uNameItems[(int)config.Value.M3uName],
                                                                                config.Value.SmartExtract,
                                                                                config.Value.Chdman,
                                                                                config.Value.DolphinTool,
                                                                                config.Value.ExtractXiso,
                                                                                config.Value.PS3dec,
                                                                                config.Value.WiiuCacheOnLaunch,
                                                                                config.Value.CiaCacheOnLaunch,
                                                                                config.Value.WadCacheOnLaunch,
                                                                                config.Value.TadCacheOnLaunch,
                                                                                config.Value.Ps3PkgCacheOnLaunch,
                                                                                config.Value.PspPkgCacheOnLaunch,
                                                                                config.Value.Ctr3dsCacheOnLaunch,
                                                                                config.Value.PsvPkgCacheOnLaunch});
                }
                else
                {
                    emulatorPlatformConfigDataGridView.Rows.Add(new object[] { priorityEmulator,
                                                                          priorityPlatform,
                                                                          config.Value.FilenamePriority,
                                                                          actionItems[(int)config.Value.Action],
                                                                          launchPathItems[(int)config.Value.LaunchPath],
                                                                          config.Value.MultiDisc,
                                                                          m3uNameItems[(int)config.Value.M3uName],
                                                                          config.Value.SmartExtract,
                                                                          config.Value.Chdman,
                                                                          config.Value.DolphinTool,
                                                                          config.Value.ExtractXiso,
                                                                          config.Value.PS3dec,
                                                                          config.Value.WiiuCacheOnLaunch,
                                                                          config.Value.CiaCacheOnLaunch,
                                                                          config.Value.WadCacheOnLaunch,
                                                                          config.Value.TadCacheOnLaunch,
                                                                          config.Value.Ps3PkgCacheOnLaunch,
                                                                          config.Value.PspPkgCacheOnLaunch,
                                                                          config.Value.Ctr3dsCacheOnLaunch,
                                                                          config.Value.PsvPkgCacheOnLaunch});
                }
            }
            emulatorPlatformConfigDataGridView.ClearSelection();

            treeView1.SelectedNode = treeView1.Nodes[0];

            updateCheckCheckBox.Checked = (bool)Config.UpdateCheck;
            standaloneExtensions.Text = Config.StandaloneExtensions;
            metadataExtensions.Text = Config.MetadataExtensions;
            bypassPathCheckCheckBox.Checked = Config.BypassPathCheck;
            ps3KeyPath.Text = Config.PS3KeyPath;
            ps3UseIsoMountLauncherCheckBox.Checked = Config.Ps3UseIsoMountLauncher;

            string[] platformNames;
            try
            {
                platformNames = PluginHelper.DataManager.GetAllPlatforms()
                    .Select(p => p.Name)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .OrderBy(n => n)
                    .ToArray();
            }
            catch (Exception)
            {
                platformNames = new string[0];
            }

            PopulatePlatformList(wadPlatform, platformNames, Config.WadPlatform);
            wadOutputPath.Text = Config.WadOutputPath;
            wadCetkCachePath.Text = Config.WadCetkCachePath;
            wadAddToLibraryCheckBox.Checked = Config.WadAddToLibrary;

            PopulatePlatformList(wiiuPlatform, platformNames, Config.WiiuPlatform);
            wiiuOutputPath.Text = Config.WiiuOutputPath;
            wiiuCommonKey.Text = Config.WiiuCommonKey;
            wiiuTitleKeyPassword.Text = Config.WiiuTitleKeyPassword;
            wiiuAddToLibraryCheckBox.Checked = Config.WiiuAddToLibrary;
            wiiuCemuKeysPath.Text = Config.WiiuCemuKeysPath;

            PopulatePlatformList(ciaPlatform, platformNames, Config.CiaPlatform);
            ciaOutputPath.Text = Config.CiaOutputPath;
            ciaCetkCachePath.Text = Config.CiaCetkCachePath;
            ciaAddToLibraryCheckBox.Checked = Config.CiaAddToLibrary;

            PopulatePlatformList(tadPlatform, platformNames, Config.TadPlatform);
            tadOutputPath.Text = Config.TadOutputPath;
            tadCetkCachePath.Text = Config.TadCetkCachePath;
            tadAddToLibraryCheckBox.Checked = Config.TadAddToLibrary;

            PopulatePlatformList(ps3PkgPlatform, platformNames, Config.Ps3PkgPlatform);
            ps3PkgOutputPath.Text = Config.Ps3PkgOutputPath;
            ps3RpcsExdataPath.Text = Config.Ps3RpcsExdataPath;
            ps3PkgAddToLibraryCheckBox.Checked = Config.Ps3PkgAddToLibrary;
            ps3PkgAutoInstallToRpcs3CheckBox.Checked = Config.Ps3PkgAutoInstallToRpcs3;
            ps3AutoInstallDlcsCheckBox.Checked = Config.Ps3AutoInstallDlcs;
            pspAutoInstallDlcsCheckBox.Checked = Config.PspAutoInstallDlcs;
            psvAutoInstallDlcsCheckBox.Checked = Config.PsvAutoInstallDlcs;

            PopulatePlatformList(pspPkgPlatform, platformNames, Config.PspPkgPlatform);
            pspPkgOutputPath.Text = Config.PspPkgOutputPath;
            pspPpssppLicensePath.Text = Config.PspPpssppLicensePath;
            pspPkgAddToLibraryCheckBox.Checked = Config.PspPkgAddToLibrary;

            ps3AutoInstallUpdatesCheckBox.Checked = Config.Ps3AutoInstallUpdates;
            ps3UpdateCachePath.Text = Config.Ps3UpdateCachePath;
            ps3UpdateOfflineModeCheckBox.Checked = Config.Ps3UpdateOfflineMode;

            pspAutoInstallUpdatesCheckBox.Checked = Config.PspAutoInstallUpdates;
            pspUpdateCachePath.Text = Config.PspUpdateCachePath;
            pspUpdateOfflineModeCheckBox.Checked = Config.PspUpdateOfflineMode;

            PopulatePlatformList(ctr3dsPlatform, platformNames, Config.Ctr3dsPlatform);
            ctr3dsKeysPath.Text = Config.Ctr3dsKeysPath;
            ctr3dsSeedDbPath.Text = Config.Ctr3dsSeedDbPath;
            ctr3dsOutputPath.Text = Config.Ctr3dsOutputPath;
            ctr3dsAddToLibraryCheckBox.Checked = Config.Ctr3dsAddToLibrary;

            PopulatePlatformList(psvPkgPlatform, platformNames, Config.PsvPkgPlatform);
            psvPkgOutputPath.Text = Config.PsvPkgOutputPath;
            psvPkgAddToLibraryCheckBox.Checked = Config.PsvPkgAddToLibrary;
            psvVita3kDataPath.Text = Config.PsvVita3kDataPath;
            npsDbPath.Text = Config.NpsDbPath;
            psvAutoInstallUpdatesCheckBox.Checked = Config.PsvAutoInstallUpdates;
            psvUpdateCachePath.Text = Config.PsvUpdateCachePath;
            psvUpdateOfflineModeCheckBox.Checked = Config.PsvUpdateOfflineMode;

            updateCacheInfo(true);
            updateEnabledState();
        }

        private void OpenIndexerWindow(LocalPkgPlatform platform)
        {
            try
            {
                using (var win = new LocalPkgIndexerWindow(platform)) win.ShowDialog(this);
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString(), Logger.LogLevel.Exception);
                UserInterface.ErrorDialog("Unexpected error opening the local PKG indexer. See log for details.");
            }
        }

        /// <summary>
        /// Update the enabled state of all controls based on the current configuration and selection.
        /// </summary>
        private void updateEnabledState()
        {
            string path = PathUtils.GetAbsolutePath(Config.CachePath);
            bool cachePathExists = Directory.Exists(path);

            openInExplorerButton.Enabled = cachePathExists;
            deleteAllButton.Enabled = (cacheDataGridView.Rows.Count > 0);
            deleteSelectedButton.Enabled = (cacheDataGridView.SelectedRows.Count > 0);

            if (emulatorPlatformConfigDataGridView.SelectedRows.Count == 1)
            {
                // Don't allow deletion of the default All / All file priority.
                if (string.Equals(emulatorPlatformConfigDataGridView.SelectedRows[0].Cells[0].Value.ToString(), "All", StringComparison.InvariantCultureIgnoreCase) &&
                    string.Equals(emulatorPlatformConfigDataGridView.SelectedRows[0].Cells[1].Value.ToString(), "All", StringComparison.InvariantCultureIgnoreCase))
                {
                    deletePriorityButton.Enabled = false;
                }
                else
                {
                    deletePriorityButton.Enabled = true;
                }
            }
            else
            {
                deletePriorityButton.Enabled = false;
            }
        }

        /// <summary>
        /// Updates the cache summary text and optionally the cached item table.
        /// </summary>
        /// <param name="updateDataGrid"></param>
        private void updateCacheInfo(bool updateDataGrid = false)
        {
            if (updateDataGrid)
            {
                updateCacheDataGrid();
            }

            double cacheSizeUsed = 0;
            double keepSize = 0;

            try
            {
                foreach (DataGridViewRow row in cacheDataGridView.Rows)
                {
                    double size = Convert.ToDouble(row.Cells["ArchiveSize"].Value);
                    if (!Convert.ToBoolean(row.Cells["Keep"].Value))
                    {
                        cacheSizeUsed += size;
                    }
                    else
                    {
                        keepSize += size;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log(e.ToString(), Logger.LogLevel.Exception);
            }

            cacheSummaryTextBox.Text =      string.Format("Cache Path:  {0}", PathUtils.GetAbsolutePath(Config.CachePath));
            cacheSummaryTextBox.Text += string.Format("\r\nCache Size:  {0:n1} MB / {1:n1} MB ({2:n1}%)", cacheSizeUsed, Config.CacheSize, (cacheSizeUsed / Convert.ToDouble(Config.CacheSize)) * 100.0);
            cacheSummaryTextBox.Text += string.Format("\r\n Keep Size:  {0:n1} MB", keepSize);
            //cacheSummaryTextBox.Text += string.Format("\r\nArchives In Cache: {0}", cacheDataGridView.Rows.Count);
        }

        /// <summary>
        /// Updates the cache table with data from disk.
        /// </summary>
        private void updateCacheDataGrid()
        {
            cacheDataGridView.Rows.Clear();

            try
            {
                foreach (string directory in Directory.GetDirectories(PathUtils.GetAbsolutePath(Config.CachePath), "*", SearchOption.TopDirectoryOnly))
                {
                    GameInfo gameInfo = new GameInfo(Path.Combine(directory, PathUtils.GetGameInfoFileName()));

                    if (gameInfo.InfoLoaded)
                    {
                        cacheDataGridView.Rows.Add(new object[] { directory, Path.GetFileName(gameInfo.ArchivePath),
                                                   gameInfo.Platform, gameInfo.DecompressedSize / 1048576.0, gameInfo.KeepInCache });
                    }
                }
            }
            catch (Exception)
            {
            }

            if (cacheDataGridView.RowCount > 0)
            {
                cacheDataGridView.Rows[0].Selected = true;
            }
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            var config = Config.GetAllEmulatorPlatformConfigByRef();
            config.Clear();

            var actionItems = (emulatorPlatformConfigDataGridView.Columns["Action"] as DataGridViewComboBoxColumn).Items;
            var launchPathItems = (emulatorPlatformConfigDataGridView.Columns["LaunchPath"] as DataGridViewComboBoxColumn).Items;
            var m3uNameItems = (emulatorPlatformConfigDataGridView.Columns["M3uName"] as DataGridViewComboBoxColumn).Items;
            foreach (DataGridViewRow row in emulatorPlatformConfigDataGridView.Rows)
            {
                string key = Config.EmulatorPlatformKey(row.Cells[0].Value.ToString(), row.Cells[1].Value.ToString());
                config.Add(key, new Config.EmulatorPlatformConfig());
                config[key].FilenamePriority = row.Cells[2].Value == null ? string.Empty : row.Cells[2].Value.ToString();
                config[key].Action = (Config.Action)actionItems.IndexOf(row.Cells[3].Value);
                config[key].LaunchPath = (Config.LaunchPath)launchPathItems.IndexOf(row.Cells[4].Value);
                config[key].MultiDisc = Convert.ToBoolean(row.Cells[5].Value);
                config[key].M3uName = (Config.M3uName)m3uNameItems.IndexOf(row.Cells[6].Value);
                config[key].SmartExtract = Convert.ToBoolean(row.Cells[7].Value);
                config[key].Chdman = Convert.ToBoolean(row.Cells[8].Value);
                config[key].DolphinTool = Convert.ToBoolean(row.Cells[9].Value);
                config[key].ExtractXiso = Convert.ToBoolean(row.Cells[10].Value);
                config[key].PS3dec = Convert.ToBoolean(row.Cells[11].Value);
                config[key].WiiuCacheOnLaunch = Convert.ToBoolean(row.Cells[12].Value);
                config[key].CiaCacheOnLaunch = Convert.ToBoolean(row.Cells[13].Value);
                config[key].WadCacheOnLaunch = Convert.ToBoolean(row.Cells[14].Value);
                config[key].TadCacheOnLaunch = Convert.ToBoolean(row.Cells[15].Value);
                config[key].Ps3PkgCacheOnLaunch = Convert.ToBoolean(row.Cells[16].Value);
                config[key].PspPkgCacheOnLaunch = Convert.ToBoolean(row.Cells[17].Value);
                config[key].Ctr3dsCacheOnLaunch = Convert.ToBoolean(row.Cells[18].Value);
                config[key].PsvPkgCacheOnLaunch = Convert.ToBoolean(row.Cells[19].Value);
            }

            Config.UpdateCheck = updateCheckCheckBox.Checked;
            Config.StandaloneExtensions = standaloneExtensions.Text;
            Config.MetadataExtensions = metadataExtensions.Text;
            Config.BypassPathCheck = bypassPathCheckCheckBox.Checked;
            Config.PS3KeyPath = ps3KeyPath.Text;
            Config.Ps3UseIsoMountLauncher = ps3UseIsoMountLauncherCheckBox.Checked;

            Config.WadPlatform = SerializePlatformList(wadPlatform);
            Config.WadOutputPath = wadOutputPath.Text.Trim();
            Config.WadCetkCachePath = wadCetkCachePath.Text.Trim();
            Config.WadAddToLibrary = wadAddToLibraryCheckBox.Checked;
            Config.WiiuPlatform = SerializePlatformList(wiiuPlatform);
            Config.WiiuOutputPath = wiiuOutputPath.Text.Trim();
            Config.WiiuCommonKey = wiiuCommonKey.Text.Trim();
            Config.WiiuTitleKeyPassword = wiiuTitleKeyPassword.Text.Trim();
            Config.WiiuAddToLibrary = wiiuAddToLibraryCheckBox.Checked;
            Config.WiiuCemuKeysPath = wiiuCemuKeysPath.Text.Trim();

            Config.CiaPlatform = SerializePlatformList(ciaPlatform);
            Config.CiaOutputPath = ciaOutputPath.Text.Trim();
            Config.CiaCetkCachePath = ciaCetkCachePath.Text.Trim();
            Config.CiaAddToLibrary = ciaAddToLibraryCheckBox.Checked;

            Config.TadPlatform = SerializePlatformList(tadPlatform);
            Config.TadOutputPath = tadOutputPath.Text.Trim();
            Config.TadCetkCachePath = tadCetkCachePath.Text.Trim();
            Config.TadAddToLibrary = tadAddToLibraryCheckBox.Checked;

            Config.Ps3PkgPlatform = SerializePlatformList(ps3PkgPlatform);
            Config.Ps3PkgOutputPath = ps3PkgOutputPath.Text.Trim();
            Config.Ps3RpcsExdataPath = ps3RpcsExdataPath.Text.Trim();
            Config.Ps3PkgAddToLibrary = ps3PkgAddToLibraryCheckBox.Checked;
            Config.Ps3PkgAutoInstallToRpcs3 = ps3PkgAutoInstallToRpcs3CheckBox.Checked;
            Config.Ps3AutoInstallDlcs = ps3AutoInstallDlcsCheckBox.Checked;
            Config.PspAutoInstallDlcs = pspAutoInstallDlcsCheckBox.Checked;
            Config.PsvAutoInstallDlcs = psvAutoInstallDlcsCheckBox.Checked;

            Config.PspPkgPlatform = SerializePlatformList(pspPkgPlatform);
            Config.PspPkgOutputPath = pspPkgOutputPath.Text.Trim();
            Config.PspPpssppLicensePath = pspPpssppLicensePath.Text.Trim();
            Config.PspPkgAddToLibrary = pspPkgAddToLibraryCheckBox.Checked;

            Config.Ps3AutoInstallUpdates = ps3AutoInstallUpdatesCheckBox.Checked;
            Config.Ps3UpdateCachePath = ps3UpdateCachePath.Text.Trim();
            Config.Ps3UpdateOfflineMode = ps3UpdateOfflineModeCheckBox.Checked;

            Config.PspAutoInstallUpdates = pspAutoInstallUpdatesCheckBox.Checked;
            Config.PspUpdateCachePath = pspUpdateCachePath.Text.Trim();
            Config.PspUpdateOfflineMode = pspUpdateOfflineModeCheckBox.Checked;

            Config.Ctr3dsPlatform = SerializePlatformList(ctr3dsPlatform);
            Config.Ctr3dsKeysPath = ctr3dsKeysPath.Text.Trim();
            Config.Ctr3dsSeedDbPath = ctr3dsSeedDbPath.Text.Trim();
            Config.Ctr3dsOutputPath = ctr3dsOutputPath.Text.Trim();
            Config.Ctr3dsAddToLibrary = ctr3dsAddToLibraryCheckBox.Checked;

            Config.PsvPkgPlatform = SerializePlatformList(psvPkgPlatform);
            Config.PsvPkgOutputPath = psvPkgOutputPath.Text.Trim();
            Config.PsvPkgAddToLibrary = psvPkgAddToLibraryCheckBox.Checked;
            Config.PsvVita3kDataPath = psvVita3kDataPath.Text.Trim();

            string prevNpsPath = Config.NpsDbPath;
            Config.NpsDbPath = npsDbPath.Text.Trim();
            if (!string.Equals(prevNpsPath, Config.NpsDbPath, StringComparison.OrdinalIgnoreCase)) NpsDb.Invalidate();

            Config.PsvAutoInstallUpdates = psvAutoInstallUpdatesCheckBox.Checked;
            Config.PsvUpdateCachePath = psvUpdateCachePath.Text.Trim();
            Config.PsvUpdateOfflineMode = psvUpdateOfflineModeCheckBox.Checked;

            // v2.75+: flush runtime-bound CheckBoxes (Wii U / 3DS auto-install flags etc.).
            FlushBoundCheckWrites();

            Config.Save();

            try
            {
                foreach (DataGridViewRow row in cacheDataGridView.Rows)
                {
                    GameInfo gameInfo = new GameInfo(Path.Combine(row.Cells["ArchivePath"].Value.ToString(), PathUtils.GetGameInfoFileName()));
                    bool keep = Convert.ToBoolean(row.Cells["Keep"].Value);
                    if (gameInfo.KeepInCache != keep)
                    {
                        gameInfo.KeepInCache = keep;
                        gameInfo.Save();
                    }
                }
            }
            catch (Exception exception)
            {
                Logger.Log(exception.ToString(), Logger.LogLevel.Exception);
            }

            this.Close();
        }

        private void addPriorityButton_Click(object sender, EventArgs e)
        {
            EmulatorPlatformSelectionWindow window = new EmulatorPlatformSelectionWindow();

            window.ShowDialog(this);

            if (window.DialogResult == DialogResult.OK)
            {
                foreach (DataGridViewRow row in emulatorPlatformConfigDataGridView.Rows)
                {
                    if (string.Equals(row.Cells["Emulator"].Value.ToString(), window.Emulator, StringComparison.InvariantCultureIgnoreCase) &&
                        string.Equals(row.Cells["Platform"].Value.ToString(), window.Platform, StringComparison.InvariantCultureIgnoreCase))
                    {
                        row.Selected = true;
                        return;
                    }
                }

                int index = emulatorPlatformConfigDataGridView.Rows.Add(new object[] { window.Emulator,
                                                                                       window.Platform,
                                                                                       string.Empty,
                                                                                       emulatorPlatformConfigDataGridView[3, 0].Value,
                                                                                       emulatorPlatformConfigDataGridView[4, 0].Value,
                                                                                       emulatorPlatformConfigDataGridView[5, 0].Value,
                                                                                       emulatorPlatformConfigDataGridView[6, 0].Value,
                                                                                       emulatorPlatformConfigDataGridView[7, 0].Value,
                                                                                       emulatorPlatformConfigDataGridView[8, 0].Value,
                                                                                       emulatorPlatformConfigDataGridView[9, 0].Value,
                                                                                       emulatorPlatformConfigDataGridView[10, 0].Value,
                                                                                       emulatorPlatformConfigDataGridView[11, 0].Value,
                                                                                       emulatorPlatformConfigDataGridView[12, 0].Value,
                                                                                       emulatorPlatformConfigDataGridView[13, 0].Value,
                                                                                       emulatorPlatformConfigDataGridView[14, 0].Value,
                                                                                       emulatorPlatformConfigDataGridView[15, 0].Value });
                emulatorPlatformConfigDataGridView.Rows[index].Selected = true;
            }
        }

        private void deletePriorityButton_Click(object sender, EventArgs e)
        {
            emulatorPlatformConfigDataGridView.Rows.Remove(emulatorPlatformConfigDataGridView.SelectedRows[0]);
            emulatorPlatformConfigDataGridView.ClearSelection();
        }

        private void extensionPriorityDataGridView_SelectionChanged(object sender, EventArgs e)
        {
            updateEnabledState();
        }

        private void pluginLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            pluginLink.LinkVisited = true;
            PluginUtils.OpenURL("https://forums.launchbox-app.com/files/file/234-archive-cache-manager/");
        }

        private void forumLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            forumLink.LinkVisited = true;
            PluginUtils.OpenURL("https://forums.launchbox-app.com/topic/35010-archive-cache-manager/");
        }

        private void sourceLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            sourceLink.LinkVisited = true;
            PluginUtils.OpenURL("https://github.com/fraganator/archive-cache-manager");
        }

        private void ps3KeyPathBrowseButton_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            string browsePath = PathUtils.GetAbsolutePath(ps3KeyPath.Text);

            dialog.SelectedPath = Directory.Exists(browsePath) ? browsePath : PathUtils.GetLaunchBoxRootPath();
            dialog.ShowNewFolderButton = true;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                ps3KeyPath.Text = PathUtils.GetRelativePath(PathUtils.GetLaunchBoxRootPath(), dialog.SelectedPath);
            }
        }

        private static void PopulatePlatformList(CheckedListBox list, string[] allPlatforms, string savedCsv)
        {
            var saved = new HashSet<string>(
                (savedCsv ?? string.Empty).Split(';')
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s)),
                StringComparer.InvariantCultureIgnoreCase);

            list.Items.Clear();
            foreach (string name in allPlatforms)
            {
                int idx = list.Items.Add(name);
                if (saved.Contains(name)) list.SetItemChecked(idx, true);
            }

            // Preserve any saved entries that no longer exist in LaunchBox so they're not silently dropped on save.
            foreach (string s in saved)
            {
                if (!allPlatforms.Any(p => string.Equals(p, s, StringComparison.InvariantCultureIgnoreCase)))
                {
                    int idx = list.Items.Add(s);
                    list.SetItemChecked(idx, true);
                }
            }
        }

        private static string SerializePlatformList(CheckedListBox list)
        {
            return string.Join(";", list.CheckedItems.Cast<object>().Select(o => o.ToString().Trim()).Where(s => !string.IsNullOrEmpty(s)));
        }

        private void wiiuCemuKeysPathBrowseButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "keys.txt|keys.txt|All files (*.*)|*.*";
                dialog.Title = "Locate Cemu keys.txt";

                string current = wiiuCemuKeysPath.Text.Trim();
                string seed = string.IsNullOrEmpty(current)
                    ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Cemu")
                    : PathUtils.GetAbsolutePath(current);
                if (File.Exists(seed))
                {
                    dialog.InitialDirectory = Path.GetDirectoryName(seed);
                    dialog.FileName = Path.GetFileName(seed);
                }
                else if (Directory.Exists(seed))
                {
                    dialog.InitialDirectory = seed;
                }

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    wiiuCemuKeysPath.Text = dialog.FileName;
                }
            }
        }

        private void wadOutputPathBrowseButton_Click(object sender, EventArgs e) => BrowseFolderInto(wadOutputPath);
        private void wadCetkCachePathBrowseButton_Click(object sender, EventArgs e) => BrowseFolderInto(wadCetkCachePath);
        private void wiiuOutputPathBrowseButton_Click(object sender, EventArgs e) => BrowseFolderInto(wiiuOutputPath);
        private void ciaOutputPathBrowseButton_Click(object sender, EventArgs e) => BrowseFolderInto(ciaOutputPath);
        private void ciaCetkCachePathBrowseButton_Click(object sender, EventArgs e) => BrowseFolderInto(ciaCetkCachePath);
        private void tadOutputPathBrowseButton_Click(object sender, EventArgs e) => BrowseFolderInto(tadOutputPath);
        private void tadCetkCachePathBrowseButton_Click(object sender, EventArgs e) => BrowseFolderInto(tadCetkCachePath);
        private void ps3PkgOutputPathBrowseButton_Click(object sender, EventArgs e) => BrowseFolderInto(ps3PkgOutputPath);
        private void ps3RpcsExdataPathBrowseButton_Click(object sender, EventArgs e) => BrowseFolderInto(ps3RpcsExdataPath);
        private void pspPkgOutputPathBrowseButton_Click(object sender, EventArgs e) => BrowseFolderInto(pspPkgOutputPath);
        private void pspPpssppLicensePathBrowseButton_Click(object sender, EventArgs e) => BrowseFolderInto(pspPpssppLicensePath);
        private void ps3UpdateCachePathBrowseButton_Click(object sender, EventArgs e) => BrowseFolderInto(ps3UpdateCachePath);
        private void pspUpdateCachePathBrowseButton_Click(object sender, EventArgs e) => BrowseFolderInto(pspUpdateCachePath);
        private void ctr3dsKeysPathBrowseButton_Click(object sender, EventArgs e) => BrowseFileInto(ctr3dsKeysPath, "Text files|*.txt|All files|*.*");
        private void ctr3dsSeedDbPathBrowseButton_Click(object sender, EventArgs e) => BrowseFileInto(ctr3dsSeedDbPath, "Binary files|*.bin|All files|*.*");
        private void ctr3dsOutputPathBrowseButton_Click(object sender, EventArgs e) => BrowseFolderInto(ctr3dsOutputPath);
        private void psvPkgOutputPathBrowseButton_Click(object sender, EventArgs e) => BrowseFolderInto(psvPkgOutputPath);
        private void psvVita3kDataPathBrowseButton_Click(object sender, EventArgs e) => BrowseFolderInto(psvVita3kDataPath);
        private void npsDbPathBrowseButton_Click(object sender, EventArgs e) => BrowseFolderInto(npsDbPath);
        private void psvUpdateCachePathBrowseButton_Click(object sender, EventArgs e) => BrowseFolderInto(psvUpdateCachePath);

        private void BrowseFileInto(TextBox target, string filter)
        {
            using (var dlg = new OpenFileDialog { Filter = filter })
            {
                string current = string.IsNullOrWhiteSpace(target.Text) ? PathUtils.GetLaunchBoxRootPath() : PathUtils.GetAbsolutePath(target.Text);
                if (System.IO.File.Exists(current)) dlg.FileName = current;
                else dlg.InitialDirectory = System.IO.Directory.Exists(current) ? current : PathUtils.GetLaunchBoxRootPath();
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    target.Text = PathUtils.GetRelativePath(PathUtils.GetLaunchBoxRootPath(), dlg.FileName);
                }
            }
        }

        private void BrowseFolderInto(TextBox target)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            string browsePath = string.IsNullOrWhiteSpace(target.Text)
                ? PathUtils.GetLaunchBoxRootPath()
                : PathUtils.GetAbsolutePath(target.Text);
            dialog.SelectedPath = Directory.Exists(browsePath) ? browsePath : PathUtils.GetLaunchBoxRootPath();
            dialog.ShowNewFolderButton = true;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                target.Text = PathUtils.GetRelativePath(PathUtils.GetLaunchBoxRootPath(), dialog.SelectedPath);
            }
        }

        private void configureCacheButton_Click(object sender, EventArgs e)
        {
            string cachePath = Config.CachePath;

            CacheConfigWindow window = new CacheConfigWindow();

            window.ShowDialog(this);

            if (window.DialogResult == DialogResult.OK)
            {
                updateCacheInfo(!PathUtils.ComparePaths(cachePath, Config.CachePath));
                updateEnabledState();
            }
        }

        private void openInExplorerButton_Click(object sender, EventArgs e)
        {
            string path = PathUtils.GetAbsolutePath(Config.CachePath);

            Process.Start("explorer.exe", path);
        }

        private void refreshButton_Click(object sender, EventArgs e)
        {
            updateCacheInfo(true);
            updateEnabledState();
        }

        private void deleteSelectedButton_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in cacheDataGridView.SelectedRows)
            {
                var dir = row.Cells["ArchivePath"].Value.ToString();
                Logger.Log(string.Format("Manually deleting cached item \"{0}\".", dir));

                string linkSource = PathUtils.ReadLinkSourceFromArchiveCache(dir);
                if (!string.IsNullOrEmpty(linkSource))
                    DiskUtils.DeleteDirectory(linkSource);

                DiskUtils.DeleteDirectory(dir);
                cacheDataGridView.Rows.Remove(row);
            }

            updateCacheInfo();
            updateEnabledState();

            RefreshLaunchBox = true;
        }

        private void cacheDataGridView_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (cacheDataGridView.CurrentCell is DataGridViewCheckBoxCell)
            {
                cacheDataGridView.CommitEdit(DataGridViewDataErrorContexts.Commit);
                updateCacheInfo();
            }
        }

        private void deleteAllButton_Click(object sender, EventArgs e)
        {
            Logger.Log("Manually deleting entire cache.");

            CacheManager.ClearCacheSpace(long.MaxValue, true);

            updateCacheInfo(true);
            updateEnabledState();

            RefreshLaunchBox = true;
        }

        private void cacheDataGridView_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            if (e.ColumnIndex == cacheDataGridView.Columns["Archive"].Index)
            {
                Bitmap icon = UserInterface.GetMediaIcon(cacheDataGridView.Rows[e.RowIndex].Cells["ArchivePlatform"].Value.ToString());

                if (icon != null)
                {
                    UserInterface.DrawCellIcon(e, icon);
                }
            }
        }

        private void multiDiscSupportCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            updateEnabledState();
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            // Top-level node tagged with a TabPage → switch to that page (covers v2.65's new
            // "Local Mirror Folders" root node, which isn't backed by a tabControl1 index).
            // Packaging sub-node → switch to Packaging, select the right platform sub-tab.
            // Otherwise top-level: switch by index (legacy 5-tab layout).
            if (e.Node.Tag is PackagingSubNav nav)
            {
                tabControl1.SelectTab(treeView1.Nodes.IndexOf(e.Node.Parent));
                if (nav.SubTab != null) packagingSubTabs.SelectedTab = nav.SubTab;
                if (nav.Anchor != null && nav.Anchor.Parent is ScrollableControl sc)
                    sc.ScrollControlIntoView(nav.Anchor);
                FlashControl(nav.Anchor);
            }
            else if (e.Node.Tag is TabPage targetTab)
            {
                tabControl1.SelectTab(targetTab);
            }
            else if (e.Node.Parent == null)
            {
                int idx = e.Node.Index;
                if (idx >= 0 && idx < tabControl1.TabCount) tabControl1.SelectTab(idx);
            }
        }

        private void emulatorPlatformConfigDataGridView_CellMouseEnter(object sender, DataGridViewCellEventArgs e)
        {
            DataGridView dataGridView = sender as DataGridView;
            if (dataGridView.Cursor != Cursors.IBeam &&
                e.RowIndex >= 0 &&
                dataGridView.Columns[e.ColumnIndex] is DataGridViewTextBoxColumn &&
                !(dataGridView.Columns[e.ColumnIndex] as DataGridViewTextBoxColumn).ReadOnly)
            {
                dataGridView.Cursor = Cursors.IBeam;
            }
        }

        private void emulatorPlatformConfigDataGridView_CellMouseLeave(object sender, DataGridViewCellEventArgs e)
        {
            DataGridView dataGridView = sender as DataGridView;
            if (dataGridView.Cursor != Cursors.Default)
            {
                dataGridView.Cursor = Cursors.Default;
            }
        }

        private void emulatorPlatformConfigDataGridView_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            if (e.ColumnIndex == emulatorPlatformConfigDataGridView.Columns["Emulator"].Index)
            {
                Bitmap icon = null;
                string emulator = emulatorPlatformConfigDataGridView.Rows[e.RowIndex].Cells["Emulator"].Value.ToString();

                if (string.Equals(emulator, "All", StringComparison.InvariantCultureIgnoreCase))
                {
                    icon = Resources.joystick;
                }
                else
                {
                    emulatorIcons.TryGetValue(emulator, out icon);
                }

                if (icon != null)
                {
                    UserInterface.DrawCellIcon(e, icon);
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // v2.62 — Navigation rework + settings search.
        // The Designer file is kept as-is; everything below tweaks the live form
        // post-InitializeComponent. Goals:
        //   • Hide the redundant TabControl tab strip (TreeView is the only nav).
        //   • Expand "Packaging" into per-platform sub-nodes that jump straight to
        //     the matching section inside the Sony/Nintendo sub-tab.
        //   • Search box that indexes every Label / CheckBox / GroupBox / Button
        //     text and jumps to the matching control on click.
        // ─────────────────────────────────────────────────────────────────────
        private class PackagingSubNav
        {
            public TabPage SubTab;
            public Control Anchor;
        }

        private TextBox mSearchBox;
        private ListBox mSearchResults;
        private List<(Control Control, string Title, TabPage MainTab, TabPage SubTab)> mSearchIndex;
        private Color? mFlashOriginalBack;
        private Timer mFlashTimer;
        private Control mFlashTarget;

        private void SetupNavReworkV262()
        {
            HideTabStrip(tabControl1);
            HideTabStrip(packagingSubTabs);

            // Shift TreeView down by 28px to make room for the search TextBox at top.
            int origTop = treeView1.Top;
            treeView1.Top = origTop + 28;
            treeView1.Height = Math.Max(80, treeView1.Height - 28);

            mSearchBox = new TextBox
            {
                Location = new Point(treeView1.Left, origTop),
                Size = new Size(treeView1.Width, 22),
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                PlaceholderText = "Search settings…",
            };
            mSearchBox.TextChanged += (s, e) => DoSearch(mSearchBox.Text);
            mSearchBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Down && mSearchResults.Visible && mSearchResults.Items.Count > 0)
                {
                    mSearchResults.Focus();
                    mSearchResults.SelectedIndex = 0;
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    mSearchBox.Clear();
                    HideSearchResults();
                }
            };
            Controls.Add(mSearchBox);
            mSearchBox.BringToFront();

            mSearchResults = new ListBox
            {
                Location = new Point(treeView1.Right + 8, origTop),
                Size = new Size(Math.Max(380, ClientSize.Width - treeView1.Right - 32), 200),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Visible = false,
                Font = new Font("Segoe UI", 8.5f),
                IntegralHeight = false,
            };
            mSearchResults.Click += (s, e) => JumpToSelectedSearchResult();
            mSearchResults.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { JumpToSelectedSearchResult(); e.Handled = true; }
                else if (e.KeyCode == Keys.Escape) { HideSearchResults(); mSearchBox.Focus(); }
            };
            Controls.Add(mSearchResults);
            mSearchResults.BringToFront();

            // v2.65 builds Packaging sub-nodes itself (different tab structure).
            // Reset selection to the first node so the AfterSelect handler runs once and the
            // initial layout is consistent with the new tree.
            if (treeView1.Nodes.Count > 0) treeView1.SelectedNode = treeView1.Nodes[0];
        }

        private static void HideTabStrip(TabControl tc)
        {
            // Combination that hides the tab header band on every Windows visual style.
            tc.Appearance = TabAppearance.FlatButtons;
            tc.SizeMode = TabSizeMode.Fixed;
            tc.ItemSize = new Size(0, 1);
            tc.Multiline = false;
        }


        // ── Search ──────────────────────────────────────────────────────────
        private void BuildSearchIndex()
        {
            mSearchIndex = new List<(Control, string, TabPage, TabPage)>();
            foreach (TabPage main in tabControl1.TabPages)
            {
                if (main == tab5PackagingSettings)
                {
                    foreach (TabPage sub in packagingSubTabs.TabPages)
                        IndexControlsIn(sub, main, sub);
                }
                else
                {
                    IndexControlsIn(main, main, null);
                }
            }
        }

        private void IndexControlsIn(Control parent, TabPage mainTab, TabPage subTab)
        {
            foreach (Control c in parent.Controls)
            {
                string label = ControlSearchText(c);
                if (!string.IsNullOrWhiteSpace(label))
                    mSearchIndex.Add((c, label, mainTab, subTab));
                if (c.HasChildren) IndexControlsIn(c, mainTab, subTab);
            }
        }

        private static string ControlSearchText(Control c)
        {
            if (c is Label || c is CheckBox || c is RadioButton || c is GroupBox || c is Button)
                return string.IsNullOrEmpty(c.Text) ? null : c.Text;
            return null;
        }

        private void DoSearch(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                HideSearchResults();
                return;
            }
            if (mSearchIndex == null) BuildSearchIndex();
            string q = query.Trim();
            var matches = mSearchIndex
                .Where(m => m.Title.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                .Take(30)
                .ToList();
            mSearchResults.Items.Clear();
            foreach (var m in matches)
            {
                string section = m.SubTab != null ? string.Format("{0} › {1}", m.MainTab.Text, m.SubTab.Text) : m.MainTab.Text;
                string label = m.Title.Length > 90 ? m.Title.Substring(0, 87) + "…" : m.Title;
                mSearchResults.Items.Add(new SearchResult { Control = m.Control, Display = string.Format("[{0}]  {1}", section, label) });
            }
            mSearchResults.DisplayMember = "Display";
            mSearchResults.Visible = matches.Count > 0;
            if (matches.Count > 0) mSearchResults.BringToFront();
        }

        private class SearchResult
        {
            public Control Control;
            public string Display;
            public override string ToString() => Display;
        }

        private void HideSearchResults()
        {
            mSearchResults.Visible = false;
            mSearchResults.Items.Clear();
        }

        private void JumpToSelectedSearchResult()
        {
            if (mSearchResults.SelectedItem is not SearchResult sr) return;
            Control target = sr.Control;
            // Find main + sub tab by walking up the parent chain.
            TabPage main = null, sub = null;
            for (var p = target.Parent; p != null; p = p.Parent)
            {
                if (p is TabPage tp)
                {
                    if (sub == null && p.Parent == packagingSubTabs) sub = tp;
                    else if (main == null) main = tp;
                }
            }
            if (main != null) tabControl1.SelectedTab = main;
            if (sub != null) packagingSubTabs.SelectedTab = sub;
            if (target.Parent is ScrollableControl s1) s1.ScrollControlIntoView(target);
            else
            {
                // Walk up to find a scrollable container.
                for (var p = target.Parent; p != null; p = p.Parent)
                    if (p is ScrollableControl sc) { sc.ScrollControlIntoView(target); break; }
            }
            FlashControl(target);
            HideSearchResults();
            mSearchBox.Clear();
        }

        // v2.64's `CompactifyPathPickersV264` + helpers were removed in v2.72 — superseded by
        // v2.65's RebuildPackagingV265 which uses proper layout containers instead of
        // pixel-shift compaction. `OpenPathInExplorer` is retained: still wired from the
        // `↗` button in v2.65's AddPath helper.
        private static void OpenPathInExplorer(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            string abs = PathUtils.GetAbsolutePath(path);
            try
            {
                if (Directory.Exists(abs))
                    Process.Start(new ProcessStartInfo("explorer.exe", "\"" + abs + "\"") { UseShellExecute = true });
                else if (File.Exists(abs))
                    Process.Start(new ProcessStartInfo("explorer.exe", "/select,\"" + abs + "\"") { UseShellExecute = true });
            }
            catch { /* user clicked an icon, no need to error-popup if Explorer is unhappy */ }
        }

        // ─────────────────────────────────────────────────────────────────────
        // v2.65 — full Packaging area rebuild.
        // The Designer's two sub-tabs (Sony / Nintendo) were absolute-positioned
        // walls of ~70 controls per tab, which v2.64's pixel-shift compactor
        // couldn't redo cleanly without overlaps. This pass throws away the
        // absolute layout: it rehomes every existing control into one of seven
        // new platform TabPages, each laid out via `FlowLayoutPanel +
        // GroupBox + TableLayoutPanel`. Controls keep their identity, so all the
        // event handlers wired in `InitializeComponent` and in the constructor
        // are still bound — only the parent chain changes.
        //
        // Side effects:
        //   • The 5 per-platform "Manage … Local PKG Folders…" buttons are
        //     hidden — the new root "Local Mirror Folders" node opens the
        //     unified `LocalPkgIndexerWindow` once for every platform.
        //   • `tabPackagingSony` / `tabPackagingNintendo` are removed from
        //     `packagingSubTabs` and replaced with seven `tabPlatform*` pages.
        //   • Treeview gets a new top-level "Local Mirror Folders" node (no
        //     tab1..5 index — `treeView1_AfterSelect` now reads `Tag` to find
        //     the target TabPage).
        // ─────────────────────────────────────────────────────────────────────

        private TabPage mTabLocalMirror;

        private void RebuildPackagingV265()
        {
            // 1. Hide the 5 legacy "Manage … Local PKG Folders…" buttons; we add a single
            //    top-level entry point instead.
            foreach (var b in new[] { ps3LocalPkgFoldersButton, pspLocalPkgFoldersButton, psvLocalPkgFoldersButton,
                                      wiiuLocalRomFoldersButton, ctr3dsLocalRomFoldersButton })
            {
                b.Visible = false;
                // Keep the Click handlers attached — they're harmless on a hidden button and
                // give us an easy backout if we ever re-show them.
            }

            // 2. Build the 7 platform pages.
            var ps3   = NewPlatformTab("PS3",   BuildPs3Section(), BuildPs3AutoUpdateSection(), BuildPs3IsoSection());
            var psp   = NewPlatformTab("PSP",   BuildPspSection(), BuildPspAutoUpdateSection());
            var psv   = NewPlatformTab("PSV",   BuildPsvSection(), BuildPsvAutoUpdateSection());
            var wii   = NewPlatformTab("Wii",   BuildWiiSection());
            var wiiu  = NewPlatformTab("Wii U", BuildWiiuSection());
            var ctr   = NewPlatformTab("3DS",   BuildCiaSection(), BuildCtr3dsDecryptSection());
            var dsi   = NewPlatformTab("DSi",   BuildDsiSection());

            // 3. Clear old sub-tabs and add the new ones.
            packagingSubTabs.SuspendLayout();
            packagingSubTabs.TabPages.Clear();
            packagingSubTabs.TabPages.AddRange(new[] { ps3, psp, psv, wii, wiiu, ctr, dsi });
            packagingSubTabs.ResumeLayout();

            // 4. Add the new top-level "Local Mirror Folders" TabPage + TreeView node.
            mTabLocalMirror = BuildLocalMirrorTab();
            tabControl1.TabPages.Add(mTabLocalMirror);
            var lmNode = new TreeNode("Local Mirror Folders") { Tag = mTabLocalMirror };
            treeView1.Nodes.Add(lmNode);

            // 5. Wire the Packaging sub-tree to the new platform tabs.
            if (treeView1.Nodes.Count >= 5)
            {
                var pkgRoot = treeView1.Nodes[4];
                pkgRoot.Nodes.Clear();
                pkgRoot.Nodes.Add(new TreeNode("PS3")   { Tag = new PackagingSubNav { SubTab = ps3,  Anchor = null } });
                pkgRoot.Nodes.Add(new TreeNode("PSP")   { Tag = new PackagingSubNav { SubTab = psp,  Anchor = null } });
                pkgRoot.Nodes.Add(new TreeNode("PSV")   { Tag = new PackagingSubNav { SubTab = psv,  Anchor = null } });
                pkgRoot.Nodes.Add(new TreeNode("Wii")   { Tag = new PackagingSubNav { SubTab = wii,  Anchor = null } });
                pkgRoot.Nodes.Add(new TreeNode("Wii U") { Tag = new PackagingSubNav { SubTab = wiiu, Anchor = null } });
                pkgRoot.Nodes.Add(new TreeNode("3DS")   { Tag = new PackagingSubNav { SubTab = ctr,  Anchor = null } });
                pkgRoot.Nodes.Add(new TreeNode("DSi")   { Tag = new PackagingSubNav { SubTab = dsi,  Anchor = null } });
                pkgRoot.Expand();
            }
        }

        // Tab layout: a 2-row TableLayoutPanel docked Fill — top row is the page banner
        // Label (51 px absolute), bottom row is a scrollable Panel that stacks GroupBoxes
        // with manual Y positions.
        //
        // Why TLP for the outer container (after rejecting it in v2.66): here the inner
        // GroupBoxes still use manual positioning, only the *outer split* uses a TLP. The
        // single-column "100%" works because the TLP itself is `Dock = Fill` inside a
        // TabPage that already has a real width by the time WinForms measures it — no
        // chicken-and-egg with AutoSize.
        private TabPage NewPlatformTab(string title, params GroupBox[] sections)
        {
            var tab = new TabPage(title) { BackColor = SystemColors.Control };

            var outerTlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
            };
            outerTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            outerTlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 51));
            outerTlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // Page banner — clones label1's styling from the Designer (Microsoft Sans Serif
            // 9.75 pt, centred, ~43 px tall). `UseMnemonic = false` so the `&` in the title
            // renders literally instead of being eaten as a keyboard-shortcut marker.
            var banner = new Label
            {
                Text = title + " — packaging & on-launch updates",
                Font = new Font("Microsoft Sans Serif", 9.75f, FontStyle.Regular, GraphicsUnit.Point, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Fill,
                Margin = new Padding(6, 6, 6, 0),
                UseMnemonic = false,
                // Designer banners (label1 / label2 / …) set BackColor = ControlLight; the
                // theme's Label branch then sees `label.BackColor != label.Parent.BackColor`
                // and bumps it to `backColor` (darker than the TabPage's `backColorContrast1`),
                // which is what gives the banner its slight contrast against the page.
                BackColor = SystemColors.ControlLight,
            };
            outerTlp.Controls.Add(banner, 0, 0);

            // Scroll content area with GroupBoxes positioned manually.
            var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            int padX = 8;
            int y = 8;
            foreach (var gb in sections)
            {
                gb.Location = new Point(padX, y);
                gb.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                gb.Width = scroll.ClientSize.Width - padX * 2;
                scroll.Controls.Add(gb);
                y += gb.Height + 8;
            }
            scroll.AutoScrollMinSize = new Size(0, y);
            outerTlp.Controls.Add(scroll, 0, 1);

            tab.Controls.Add(outerTlp);
            return tab;
        }

        private TabPage BuildLocalMirrorTab()
        {
            var tab = new TabPage("Local Mirror") { BackColor = SystemColors.Control };
            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false,
                Padding = new Padding(16),
            };

            var header = new Label
            {
                Text = "Local Mirror Folders",
                Font = new Font("Microsoft Sans Serif", 11f, FontStyle.Bold),
                AutoSize = true, Margin = new Padding(0, 0, 0, 6),
            };

            var intro = new Label
            {
                Text = "Index your offline mirror of PS3 / PSP / PSV / Wii U / 3DS update + DLC " +
                       "packages so launches can install from local files instead of querying Sony / Nintendo.\n\n" +
                       "Each platform keeps its own folder list. The button below opens the unified indexer " +
                       "with one tab per platform; the previous five \"Manage ... Local PKG Folders\" entries " +
                       "are gone.",
                AutoSize = true, MaximumSize = new Size(640, 0), Margin = new Padding(0, 0, 0, 12),
            };

            var openBtn = new Button
            {
                Text = "Open Local Mirror Indexer…",
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(12, 6, 12, 6), Margin = new Padding(0, 0, 0, 8),
            };
            openBtn.Click += (s, e) => OpenIndexerWindow(LocalPkgPlatform.Ps3);

            flow.Controls.Add(header);
            flow.Controls.Add(intro);
            flow.Controls.Add(openBtn);
            tab.Controls.Add(flow);
            return tab;
        }

        // ── Section builders ────────────────────────────────────────────────
        // Each Build*Section returns a GroupBox whose content is a TableLayoutPanel
        // hosting the existing controls (rehomed from tabPackagingSony / Nintendo).
        // Helpers:
        //   AddPath(tlp, label, txt, browse) → builds a row [label | textbox+browse+open]
        //   AddCheck(tlp, cb)                → adds a CheckBox spanning both columns
        //   AddCustom(tlp, label, content)   → arbitrary label + control row
        //   AddText(tlp, label, txt)         → label + textbox (no Browse button)
        //   AddPlatformList(tlp, lbl, clb)   → label + CheckedListBox (sized 280×120)

        // Layout-by-Y: each GroupBox tracks its `NextRowTop` in Tag and grows its Height as
        // rows are appended. Outer Panel anchors GroupBoxes Top|Left|Right so they stretch
        // with the form; inner content is positioned by hand but TextBoxes are
        // Top|Left|Right anchored and the right-edge buttons Top|Right, so the row stays
        // valid through resizes.
        private const int kSectionLabelWidth = 160;
        private const int kSectionPadX       = 12;
        private const int kSectionInitW      = 720;
        private const int kSectionHeaderTop  = 22;   // GroupBox internal Y where row 0 starts

        // The GroupBox itself gets a `9 pt Bold` font for its title — but children that don't
        // set Font explicitly inherit that, which made every Label / CheckBox / TextBox /
        // CheckedListBox in a section render bold (v2.70 bug). Each Add* helper now forces
        // this Regular font on every control it places, so only the section header stays bold.
        private static readonly Font kBodyFont = new Font("Microsoft Sans Serif", 8.25f, FontStyle.Regular);

        private GroupBox NewSection(string title)
        {
            var gb = new GroupBox
            {
                Text = title,
                Font = new Font("Microsoft Sans Serif", 9f, FontStyle.Bold),
                Width = kSectionInitW,
                Height = kSectionHeaderTop + 12,
                Tag = (int)kSectionHeaderTop,        // NextRowTop
            };
            return gb;
        }

        private void Bump(GroupBox gb, int rowHeight)
        {
            int next = (int)gb.Tag + rowHeight;
            gb.Tag = next;
            gb.Height = next + 12;
        }

        private void AddPath(GroupBox gb, string label, TextBox txt, Button browse)
        {
            int y = (int)gb.Tag;
            const int rowH = 28;
            const int browseW = 64;
            const int openW   = 28;
            const int gap     = 4;

            var lbl = new Label
            {
                Text = label,
                AutoSize = false,
                Location = new Point(kSectionPadX, y + 4),
                Size = new Size(kSectionLabelWidth, 20),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = kBodyFont,
            };
            gb.Controls.Add(lbl);

            int rightEdge = gb.Width - kSectionPadX;
            int openX     = rightEdge - openW;
            int browseX   = openX - gap - browseW;
            int txtLeft   = kSectionPadX + kSectionLabelWidth + 8;

            var openBtn = new Button
            {
                Text = "↗",
                Size = new Size(openW, 23),
                Location = new Point(openX, y + 3),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
            };
            var capTxt = txt;
            openBtn.Click += (s, e) => OpenPathInExplorer(capTxt.Text);
            gb.Controls.Add(openBtn);

            // Designer-time the button has `Image = folder_horizontal_open` +
            // `TextImageRelation = ImageBeforeText`; at the new 64-px width the folder icon
            // ate the "Browse…" text down to "Bro". Drop the icon and the explicit Size
            // fits the text fine.
            browse.AutoSize = false;
            browse.Image = null;
            browse.TextImageRelation = TextImageRelation.Overlay;
            browse.TextAlign = ContentAlignment.MiddleCenter;
            browse.Size = new Size(browseW, 23);
            browse.Location = new Point(browseX, y + 3);
            browse.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            browse.Text = "Browse…";
            browse.Font = kBodyFont;
            gb.Controls.Add(browse);

            openBtn.Font = kBodyFont;
            txt.Location = new Point(txtLeft, y + 4);
            txt.Size = new Size(browseX - gap - txtLeft, 20);
            txt.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txt.Font = kBodyFont;
            gb.Controls.Add(txt);

            Bump(gb, rowH);
        }

        private void AddText(GroupBox gb, string label, TextBox txt)
        {
            int y = (int)gb.Tag;
            const int rowH = 28;

            var lbl = new Label
            {
                Text = label,
                AutoSize = false,
                Location = new Point(kSectionPadX, y + 4),
                Size = new Size(kSectionLabelWidth, 20),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = kBodyFont,
            };
            gb.Controls.Add(lbl);

            int txtLeft = kSectionPadX + kSectionLabelWidth + 8;
            int rightEdge = gb.Width - kSectionPadX;
            txt.Location = new Point(txtLeft, y + 4);
            txt.Size = new Size(rightEdge - txtLeft, 20);
            txt.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txt.Font = kBodyFont;
            gb.Controls.Add(txt);

            Bump(gb, rowH);
        }

        private void AddCheck(GroupBox gb, CheckBox cb)
        {
            int y = (int)gb.Tag;
            const int rowH = 24;

            cb.Location = new Point(kSectionPadX, y + 2);
            cb.AutoSize = true;
            cb.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            cb.Font = kBodyFont;
            gb.Controls.Add(cb);

            Bump(gb, rowH);
        }

        /// <summary>
        /// Runtime-created CheckBoxes (used for v2.75+ Wii U / 3DS auto-install flags that don't
        /// have a Designer counterpart) defer their Config write until the OK button is clicked,
        /// matching the existing Designer flow: Cancel must throw away the dialog's edits, OK must
        /// flush them. Each `AddBoundCheck` registers an action here; `FlushBoundCheckWrites()` is
        /// called from `okButton_Click` right before `Config.Save()`.
        /// </summary>
        private readonly List<Action> mPendingConfigWrites = new List<Action>();

        private void AddBoundCheck(GroupBox gb, string text, Func<bool> getter, Action<bool> setter)
        {
            var cb = new CheckBox
            {
                Text = text,
                Checked = getter(),
                AutoSize = true,
            };
            mPendingConfigWrites.Add(() => setter(cb.Checked));
            AddCheck(gb, cb);
        }

        private void FlushBoundCheckWrites()
        {
            foreach (var w in mPendingConfigWrites) w();
        }

        private void AddPlatformList(GroupBox gb, string label, CheckedListBox clb)
        {
            int y = (int)gb.Tag;
            const int rowH = 130;
            const int clbHeight = 120;

            var lbl = new Label
            {
                Text = label,
                AutoSize = false,
                Location = new Point(kSectionPadX, y + 4),
                Size = new Size(kSectionLabelWidth, 20),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = kBodyFont,
            };
            gb.Controls.Add(lbl);

            int clbLeft = kSectionPadX + kSectionLabelWidth + 8;
            int rightEdge = gb.Width - kSectionPadX;
            clb.Location = new Point(clbLeft, y + 4);
            clb.Size = new Size(rightEdge - clbLeft, clbHeight);
            clb.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            clb.Font = kBodyFont;
            gb.Controls.Add(clb);

            Bump(gb, rowH);
        }

        // ── Concrete sections (one method per former section label) ──────────
        private GroupBox BuildWiiSection()
        {
            var gb = NewSection("Wii (WAD)  ·  Right-click \"Create Wii WAD…\" packager");
            AddPlatformList(gb, "Menu platforms:", wadPlatform);
            AddPath(gb, "Output folder:",       wadOutputPath,    wadOutputPathBrowseButton);
            AddPath(gb, "Ticket cache folder:", wadCetkCachePath, wadCetkCachePathBrowseButton);
            AddCheck(gb, wadAddToLibraryCheckBox);
            return gb;
        }

        private GroupBox BuildWiiuSection()
        {
            var gb = NewSection("Wii U  ·  Right-click \"Create Wii U Package…\" packager");
            AddPlatformList(gb, "Menu platforms:", wiiuPlatform);
            AddPath(gb, "Output folder:",       wiiuOutputPath,     wiiuOutputPathBrowseButton);
            AddText(gb, "Common Key:",          wiiuCommonKey);
            AddText(gb, "Title Key password:",  wiiuTitleKeyPassword);
            AddPath(gb, "Cemu keys path:",      wiiuCemuKeysPath,   wiiuCemuKeysPathBrowseButton);
            AddCheck(gb, wiiuAddToLibraryCheckBox);
            AddBoundCheck(gb, "On-launch: install updates from local mirror index → Cemu mlc01",
                () => Config.WiiuAutoInstallUpdates, v => Config.WiiuAutoInstallUpdates = v);
            AddBoundCheck(gb, "On-launch: install DLCs from local mirror index → Cemu mlc01",
                () => Config.WiiuAutoInstallDlcs,    v => Config.WiiuAutoInstallDlcs    = v);
            return gb;
        }

        private GroupBox BuildCiaSection()
        {
            var gb = NewSection("3DS (CIA)  ·  Right-click \"Create CIA Package…\" packager");
            AddPlatformList(gb, "Menu platforms:", ciaPlatform);
            AddPath(gb, "Output folder:",       ciaOutputPath,    ciaOutputPathBrowseButton);
            AddPath(gb, "Ticket cache folder:", ciaCetkCachePath, ciaCetkCachePathBrowseButton);
            AddCheck(gb, ciaAddToLibraryCheckBox);
            return gb;
        }

        private GroupBox BuildCtr3dsDecryptSection()
        {
            var gb = NewSection("3DS Decrypt  ·  Right-click \"Decrypt 3DS ROM…\" → plain .3ds");
            AddPlatformList(gb, "Menu platforms:", ctr3dsPlatform);
            AddPath(gb, "AES keys file:",       ctr3dsKeysPath,   ctr3dsKeysPathBrowseButton);
            AddPath(gb, "Seed DB file:",        ctr3dsSeedDbPath, ctr3dsSeedDbPathBrowseButton);
            AddPath(gb, "Output folder:",       ctr3dsOutputPath, ctr3dsOutputPathBrowseButton);
            AddCheck(gb, ctr3dsAddToLibraryCheckBox);
            return gb;
        }

        private GroupBox BuildDsiSection()
        {
            var gb = NewSection("DSi (TAD)  ·  Right-click \"Create TAD Package…\" packager");
            AddPlatformList(gb, "Menu platforms:", tadPlatform);
            AddPath(gb, "Output folder:",       tadOutputPath,    tadOutputPathBrowseButton);
            AddPath(gb, "Ticket cache folder:", tadCetkCachePath, tadCetkCachePathBrowseButton);
            AddCheck(gb, tadAddToLibraryCheckBox);
            return gb;
        }

        private GroupBox BuildPs3Section()
        {
            var gb = NewSection("PS3 (.pkg)  ·  On-launch packager (.pkg → RPCS3 folder, RAP → exdata)");
            AddPlatformList(gb, "Menu platforms:", ps3PkgPlatform);
            AddPath(gb, "Output folder:",       ps3PkgOutputPath,    ps3PkgOutputPathBrowseButton);
            AddPath(gb, "RPCS3 exdata path:",   ps3RpcsExdataPath,   ps3RpcsExdataPathBrowseButton);
            AddCheck(gb, ps3PkgAddToLibraryCheckBox);
            AddCheck(gb, ps3PkgAutoInstallToRpcs3CheckBox);
            return gb;
        }

        private GroupBox BuildPs3AutoUpdateSection()
        {
            var gb = NewSection("PS3 Auto-Update  ·  Pull patches/DLC from Sony PSN on launch");
            AddCheck(gb, ps3AutoInstallUpdatesCheckBox);
            AddCheck(gb, ps3AutoInstallDlcsCheckBox);
            AddPath(gb, "Update cache folder:", ps3UpdateCachePath, ps3UpdateCachePathBrowseButton);
            return gb;
        }

        private GroupBox BuildPs3IsoSection()
        {
            var gb = NewSection("PS3 ISO  ·  decrypted ISO launch via RPCS3");
            AddPath(gb, "PS3 .dkey folder:", ps3KeyPath, ps3KeyPathBrowseButton);
            AddCheck(gb, ps3UseIsoMountLauncherCheckBox);
            return gb;
        }

        private GroupBox BuildPspSection()
        {
            var gb = NewSection("PSP (.pkg)  ·  On-launch packager (.pkg → PPSSPP memstick, RAP → PSP/LICENSE)");
            AddPlatformList(gb, "Menu platforms:", pspPkgPlatform);
            AddPath(gb, "Output folder:",       pspPkgOutputPath,     pspPkgOutputPathBrowseButton);
            AddPath(gb, "PPSSPP LICENSE folder:", pspPpssppLicensePath, pspPpssppLicensePathBrowseButton);
            AddCheck(gb, pspPkgAddToLibraryCheckBox);
            return gb;
        }

        private GroupBox BuildPspAutoUpdateSection()
        {
            var gb = NewSection("PSP Auto-Update  ·  Pull patches/DLC from Sony PSN on launch");
            AddCheck(gb, pspAutoInstallUpdatesCheckBox);
            AddCheck(gb, pspAutoInstallDlcsCheckBox);
            AddPath(gb, "Update cache folder:", pspUpdateCachePath, pspUpdateCachePathBrowseButton);
            return gb;
        }

        private GroupBox BuildPsvSection()
        {
            var gb = NewSection("PSV (.pkg)  ·  On-launch packager (.pkg → Vita3K ux0:app + .vpk build)");
            AddPlatformList(gb, "Menu platforms:", psvPkgPlatform);
            AddPath(gb, "Output folder:",       psvPkgOutputPath,    psvPkgOutputPathBrowseButton);
            AddPath(gb, "Vita3K data folder:",  psvVita3kDataPath,   psvVita3kDataPathBrowseButton);
            AddPath(gb, "NPS DB file:",         npsDbPath,           npsDbPathBrowseButton);
            AddCheck(gb, psvPkgAddToLibraryCheckBox);
            AddCheck(gb, psvAutoInstallDlcsCheckBox);
            return gb;
        }

        private GroupBox BuildPsvAutoUpdateSection()
        {
            var gb = NewSection("PSV Auto-Update  ·  Pull patches from Sony PSN on launch");
            AddCheck(gb, psvAutoInstallUpdatesCheckBox);
            AddPath(gb, "Update cache folder:", psvUpdateCachePath, psvUpdateCachePathBrowseButton);
            AddCheck(gb, psvUpdateOfflineModeCheckBox);
            return gb;
        }

        // Briefly highlights a control's background to draw the eye after a jump.
        private void FlashControl(Control c)
        {
            if (c == null) return;
            if (mFlashTarget != null && mFlashOriginalBack.HasValue)
                mFlashTarget.BackColor = mFlashOriginalBack.Value;
            mFlashTarget = c;
            mFlashOriginalBack = c.BackColor;
            c.BackColor = Color.FromArgb(255, 250, 180);
            if (mFlashTimer == null)
            {
                mFlashTimer = new Timer { Interval = 1200 };
                mFlashTimer.Tick += (s, e) =>
                {
                    mFlashTimer.Stop();
                    if (mFlashTarget != null && mFlashOriginalBack.HasValue)
                    {
                        mFlashTarget.BackColor = mFlashOriginalBack.Value;
                        mFlashTarget = null;
                        mFlashOriginalBack = null;
                    }
                };
            }
            mFlashTimer.Stop();
            mFlashTimer.Start();
        }
    }
}
