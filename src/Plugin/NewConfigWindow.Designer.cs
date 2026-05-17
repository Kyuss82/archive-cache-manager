
namespace ArchiveCacheManager
{
    partial class NewConfigWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.TreeNode treeNode1 = new System.Windows.Forms.TreeNode("Cache Settings");
            System.Windows.Forms.TreeNode treeNode2 = new System.Windows.Forms.TreeNode("Extraction Settings");
            System.Windows.Forms.TreeNode treeNode3 = new System.Windows.Forms.TreeNode("Smart Extract Settings");
            System.Windows.Forms.TreeNode treeNode4 = new System.Windows.Forms.TreeNode("Plugin Settings");
            System.Windows.Forms.TreeNode treeNode5 = new System.Windows.Forms.TreeNode("Packaging");
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NewConfigWindow));
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.openInExplorerButton = new System.Windows.Forms.Button();
            this.configureCacheButton = new System.Windows.Forms.Button();
            this.deleteAllButton = new System.Windows.Forms.Button();
            this.refreshButton = new System.Windows.Forms.Button();
            this.deleteSelectedButton = new System.Windows.Forms.Button();
            this.versionLabel = new System.Windows.Forms.Label();
            this.forumLink = new System.Windows.Forms.LinkLabel();
            this.sourceLink = new System.Windows.Forms.LinkLabel();
            this.pluginLink = new System.Windows.Forms.LinkLabel();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.treeView1 = new System.Windows.Forms.TreeView();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.tabControl1 = new ArchiveCacheManager.StackPanel();
            this.tab1CacheSettings = new System.Windows.Forms.TabPage();
            this.label1 = new System.Windows.Forms.Label();
            this.cacheSummaryTextBox = new System.Windows.Forms.RichTextBox();
            this.cacheDataGridView = new System.Windows.Forms.DataGridView();
            this.ArchivePath = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Archive = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ArchivePlatform = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ArchiveSize = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Keep = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.tab2ExtractionSettings = new System.Windows.Forms.TabPage();
            this.label9 = new System.Windows.Forms.Label();
            this.extractionSettingsTipLabel = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.deletePriorityButton = new System.Windows.Forms.Button();
            this.emulatorPlatformConfigDataGridView = new System.Windows.Forms.DataGridView();
            this.addPriorityButton = new System.Windows.Forms.Button();
            this.tab3SmartExtractSettings = new System.Windows.Forms.TabPage();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.metadataExtensions = new System.Windows.Forms.TextBox();
            this.cachePathLabel = new System.Windows.Forms.Label();
            this.standaloneExtensions = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.tab4PluginSettings = new System.Windows.Forms.TabPage();
            this.tab5PackagingSettings = new System.Windows.Forms.TabPage();
            this.packagingSubTabs = new System.Windows.Forms.TabControl();
            this.tabPackagingNintendo = new System.Windows.Forms.TabPage();
            this.tabPackagingSony = new System.Windows.Forms.TabPage();
            this.packagingWiiSectionLabel = new System.Windows.Forms.Label();
            this.wadPlatformLabel = new System.Windows.Forms.Label();
            this.wadPlatform = new System.Windows.Forms.CheckedListBox();
            this.wadOutputPathLabel = new System.Windows.Forms.Label();
            this.wadOutputPath = new System.Windows.Forms.TextBox();
            this.wadOutputPathBrowseButton = new System.Windows.Forms.Button();
            this.wadCetkCachePathLabel = new System.Windows.Forms.Label();
            this.wadCetkCachePath = new System.Windows.Forms.TextBox();
            this.wadCetkCachePathBrowseButton = new System.Windows.Forms.Button();
            this.wadAddToLibraryCheckBox = new System.Windows.Forms.CheckBox();
            this.packagingWiiuSectionLabel = new System.Windows.Forms.Label();
            this.wiiuPlatformLabel = new System.Windows.Forms.Label();
            this.wiiuPlatform = new System.Windows.Forms.CheckedListBox();
            this.wiiuOutputPathLabel = new System.Windows.Forms.Label();
            this.wiiuOutputPath = new System.Windows.Forms.TextBox();
            this.wiiuOutputPathBrowseButton = new System.Windows.Forms.Button();
            this.wiiuCommonKeyLabel = new System.Windows.Forms.Label();
            this.wiiuCommonKey = new System.Windows.Forms.TextBox();
            this.wiiuTitleKeyPasswordLabel = new System.Windows.Forms.Label();
            this.wiiuTitleKeyPassword = new System.Windows.Forms.TextBox();
            this.wiiuAddToLibraryCheckBox = new System.Windows.Forms.CheckBox();
            this.wiiuCemuKeysPathLabel = new System.Windows.Forms.Label();
            this.wiiuCemuKeysPath = new System.Windows.Forms.TextBox();
            this.wiiuCemuKeysPathBrowseButton = new System.Windows.Forms.Button();
            this.packagingCiaSectionLabel = new System.Windows.Forms.Label();
            this.ciaPlatformLabel = new System.Windows.Forms.Label();
            this.ciaPlatform = new System.Windows.Forms.CheckedListBox();
            this.ciaOutputPathLabel = new System.Windows.Forms.Label();
            this.ciaOutputPath = new System.Windows.Forms.TextBox();
            this.ciaOutputPathBrowseButton = new System.Windows.Forms.Button();
            this.ciaCetkCachePathLabel = new System.Windows.Forms.Label();
            this.ciaCetkCachePath = new System.Windows.Forms.TextBox();
            this.ciaCetkCachePathBrowseButton = new System.Windows.Forms.Button();
            this.ciaAddToLibraryCheckBox = new System.Windows.Forms.CheckBox();
            this.packagingTadSectionLabel = new System.Windows.Forms.Label();
            this.tadPlatformLabel = new System.Windows.Forms.Label();
            this.tadPlatform = new System.Windows.Forms.CheckedListBox();
            this.tadOutputPathLabel = new System.Windows.Forms.Label();
            this.tadOutputPath = new System.Windows.Forms.TextBox();
            this.tadOutputPathBrowseButton = new System.Windows.Forms.Button();
            this.tadCetkCachePathLabel = new System.Windows.Forms.Label();
            this.tadCetkCachePath = new System.Windows.Forms.TextBox();
            this.tadCetkCachePathBrowseButton = new System.Windows.Forms.Button();
            this.tadAddToLibraryCheckBox = new System.Windows.Forms.CheckBox();
            this.packagingPs3PkgSectionLabel = new System.Windows.Forms.Label();
            this.ps3PkgPlatformLabel = new System.Windows.Forms.Label();
            this.ps3PkgPlatform = new System.Windows.Forms.CheckedListBox();
            this.ps3PkgOutputPathLabel = new System.Windows.Forms.Label();
            this.ps3PkgOutputPath = new System.Windows.Forms.TextBox();
            this.ps3PkgOutputPathBrowseButton = new System.Windows.Forms.Button();
            this.ps3RpcsExdataPathLabel = new System.Windows.Forms.Label();
            this.ps3RpcsExdataPath = new System.Windows.Forms.TextBox();
            this.ps3RpcsExdataPathBrowseButton = new System.Windows.Forms.Button();
            this.ps3PkgAddToLibraryCheckBox = new System.Windows.Forms.CheckBox();
            this.ps3PkgAutoInstallToRpcs3CheckBox = new System.Windows.Forms.CheckBox();
            this.ps3AutoInstallDlcsCheckBox = new System.Windows.Forms.CheckBox();
            this.pspAutoInstallDlcsCheckBox = new System.Windows.Forms.CheckBox();
            this.psvAutoInstallDlcsCheckBox = new System.Windows.Forms.CheckBox();
            this.ps3LocalPkgFoldersButton = new System.Windows.Forms.Button();
            this.pspLocalPkgFoldersButton = new System.Windows.Forms.Button();
            this.psvLocalPkgFoldersButton = new System.Windows.Forms.Button();
            this.wiiuLocalRomFoldersButton = new System.Windows.Forms.Button();
            this.ctr3dsLocalRomFoldersButton = new System.Windows.Forms.Button();
            this.packagingPspPkgSectionLabel = new System.Windows.Forms.Label();
            this.pspPkgPlatformLabel = new System.Windows.Forms.Label();
            this.pspPkgPlatform = new System.Windows.Forms.CheckedListBox();
            this.pspPkgOutputPathLabel = new System.Windows.Forms.Label();
            this.pspPkgOutputPath = new System.Windows.Forms.TextBox();
            this.pspPkgOutputPathBrowseButton = new System.Windows.Forms.Button();
            this.pspPpssppLicensePathLabel = new System.Windows.Forms.Label();
            this.pspPpssppLicensePath = new System.Windows.Forms.TextBox();
            this.pspPpssppLicensePathBrowseButton = new System.Windows.Forms.Button();
            this.pspPkgAddToLibraryCheckBox = new System.Windows.Forms.CheckBox();
            this.packagingPs3AutoUpdateSectionLabel = new System.Windows.Forms.Label();
            this.ps3AutoInstallUpdatesCheckBox = new System.Windows.Forms.CheckBox();
            this.ps3UpdateCachePathLabel = new System.Windows.Forms.Label();
            this.ps3UpdateCachePath = new System.Windows.Forms.TextBox();
            this.ps3UpdateCachePathBrowseButton = new System.Windows.Forms.Button();
            this.ps3UpdateOfflineModeCheckBox = new System.Windows.Forms.CheckBox();
            this.packagingPspAutoUpdateSectionLabel = new System.Windows.Forms.Label();
            this.pspAutoInstallUpdatesCheckBox = new System.Windows.Forms.CheckBox();
            this.pspUpdateCachePathLabel = new System.Windows.Forms.Label();
            this.pspUpdateCachePath = new System.Windows.Forms.TextBox();
            this.pspUpdateCachePathBrowseButton = new System.Windows.Forms.Button();
            this.pspUpdateOfflineModeCheckBox = new System.Windows.Forms.CheckBox();
            this.packagingCtr3dsSectionLabel = new System.Windows.Forms.Label();
            this.ctr3dsPlatformLabel = new System.Windows.Forms.Label();
            this.ctr3dsPlatform = new System.Windows.Forms.CheckedListBox();
            this.ctr3dsKeysPathLabel = new System.Windows.Forms.Label();
            this.ctr3dsKeysPath = new System.Windows.Forms.TextBox();
            this.ctr3dsKeysPathBrowseButton = new System.Windows.Forms.Button();
            this.ctr3dsSeedDbPathLabel = new System.Windows.Forms.Label();
            this.ctr3dsSeedDbPath = new System.Windows.Forms.TextBox();
            this.ctr3dsSeedDbPathBrowseButton = new System.Windows.Forms.Button();
            this.ctr3dsOutputPathLabel = new System.Windows.Forms.Label();
            this.ctr3dsOutputPath = new System.Windows.Forms.TextBox();
            this.ctr3dsOutputPathBrowseButton = new System.Windows.Forms.Button();
            this.ctr3dsAddToLibraryCheckBox = new System.Windows.Forms.CheckBox();
            this.packagingPsvPkgSectionLabel = new System.Windows.Forms.Label();
            this.psvPkgPlatformLabel = new System.Windows.Forms.Label();
            this.psvPkgPlatform = new System.Windows.Forms.CheckedListBox();
            this.psvPkgOutputPathLabel = new System.Windows.Forms.Label();
            this.psvPkgOutputPath = new System.Windows.Forms.TextBox();
            this.psvPkgOutputPathBrowseButton = new System.Windows.Forms.Button();
            this.psvPkgAddToLibraryCheckBox = new System.Windows.Forms.CheckBox();
            this.psvVita3kDataPathLabel = new System.Windows.Forms.Label();
            this.psvVita3kDataPath = new System.Windows.Forms.TextBox();
            this.psvVita3kDataPathBrowseButton = new System.Windows.Forms.Button();
            this.npsDbPathLabel = new System.Windows.Forms.Label();
            this.npsDbPath = new System.Windows.Forms.TextBox();
            this.npsDbPathBrowseButton = new System.Windows.Forms.Button();
            this.packagingPsvAutoUpdateSectionLabel = new System.Windows.Forms.Label();
            this.psvAutoInstallUpdatesCheckBox = new System.Windows.Forms.CheckBox();
            this.psvUpdateCachePathLabel = new System.Windows.Forms.Label();
            this.psvUpdateCachePath = new System.Windows.Forms.TextBox();
            this.psvUpdateCachePathBrowseButton = new System.Windows.Forms.Button();
            this.psvUpdateOfflineModeCheckBox = new System.Windows.Forms.CheckBox();
            this.label8 = new System.Windows.Forms.Label();
            this.bypassPathCheckCheckBox = new System.Windows.Forms.CheckBox();
            this.packagingPs3IsoSectionLabel = new System.Windows.Forms.Label();
            this.ps3KeyPathLabel = new System.Windows.Forms.Label();
            this.ps3KeyPath = new System.Windows.Forms.TextBox();
            this.ps3KeyPathBrowseButton = new System.Windows.Forms.Button();
            this.ps3UseIsoMountLauncherCheckBox = new System.Windows.Forms.CheckBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.updateCheckCheckBox = new System.Windows.Forms.CheckBox();
            this.Emulator = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Platform = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Priority = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Action = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.LaunchPath = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.MultiDisc = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.M3uName = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.SmartExtract = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.Chdman = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.DolphinTool = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.ExtractXiso = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.PS3dec = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.WiiuCacheOnLaunch = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.CiaCacheOnLaunch = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.WadCacheOnLaunch = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.TadCacheOnLaunch = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.Ps3PkgCacheOnLaunch = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.PspPkgCacheOnLaunch = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.Ctr3dsCacheOnLaunch = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.PsvPkgCacheOnLaunch = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.flowLayoutPanel1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tab1CacheSettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.cacheDataGridView)).BeginInit();
            this.tab2ExtractionSettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.emulatorPlatformConfigDataGridView)).BeginInit();
            this.tab3SmartExtractSettings.SuspendLayout();
            this.tab4PluginSettings.SuspendLayout();
            this.tab5PackagingSettings.SuspendLayout();
            this.packagingSubTabs.SuspendLayout();
            this.tabPackagingNintendo.SuspendLayout();
            this.tabPackagingSony.SuspendLayout();
            this.SuspendLayout();
            // 
            // openInExplorerButton
            // 
            this.openInExplorerButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.openInExplorerButton.Image = global::ArchiveCacheManager.Resources.folder_horizontal_open;
            this.openInExplorerButton.Location = new System.Drawing.Point(576, 92);
            this.openInExplorerButton.Name = "openInExplorerButton";
            this.openInExplorerButton.Size = new System.Drawing.Size(156, 28);
            this.openInExplorerButton.TabIndex = 6;
            this.openInExplorerButton.Text = "Open In Explorer";
            this.openInExplorerButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.openInExplorerButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.toolTip.SetToolTip(this.openInExplorerButton, "Opens the confgured cache path in Windows Explorer.");
            this.openInExplorerButton.UseVisualStyleBackColor = true;
            this.openInExplorerButton.Click += new System.EventHandler(this.openInExplorerButton_Click);
            // 
            // configureCacheButton
            // 
            this.configureCacheButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.configureCacheButton.Image = global::ArchiveCacheManager.Resources.gear;
            this.configureCacheButton.Location = new System.Drawing.Point(576, 58);
            this.configureCacheButton.Name = "configureCacheButton";
            this.configureCacheButton.Size = new System.Drawing.Size(156, 28);
            this.configureCacheButton.TabIndex = 5;
            this.configureCacheButton.Text = "Configure Cache...";
            this.configureCacheButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.configureCacheButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.toolTip.SetToolTip(this.configureCacheButton, "Edit the cache configuration.");
            this.configureCacheButton.UseVisualStyleBackColor = true;
            this.configureCacheButton.Click += new System.EventHandler(this.configureCacheButton_Click);
            // 
            // deleteAllButton
            // 
            this.deleteAllButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.deleteAllButton.BackColor = System.Drawing.SystemColors.ControlLight;
            this.deleteAllButton.Image = global::ArchiveCacheManager.Resources.broom;
            this.deleteAllButton.Location = new System.Drawing.Point(616, 488);
            this.deleteAllButton.Name = "deleteAllButton";
            this.deleteAllButton.Size = new System.Drawing.Size(116, 28);
            this.deleteAllButton.TabIndex = 10;
            this.deleteAllButton.Text = "Delete All";
            this.deleteAllButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.deleteAllButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.toolTip.SetToolTip(this.deleteAllButton, "Delete all items from the cache. The cache folder is not removed.");
            this.deleteAllButton.UseVisualStyleBackColor = true;
            this.deleteAllButton.Click += new System.EventHandler(this.deleteAllButton_Click);
            // 
            // refreshButton
            // 
            this.refreshButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.refreshButton.Image = global::ArchiveCacheManager.Resources.arrow_circle_double;
            this.refreshButton.Location = new System.Drawing.Point(6, 488);
            this.refreshButton.Name = "refreshButton";
            this.refreshButton.Size = new System.Drawing.Size(80, 28);
            this.refreshButton.TabIndex = 8;
            this.refreshButton.Text = "Refresh";
            this.refreshButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.refreshButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.toolTip.SetToolTip(this.refreshButton, "Refresh the cache details.");
            this.refreshButton.UseVisualStyleBackColor = true;
            this.refreshButton.Click += new System.EventHandler(this.refreshButton_Click);
            // 
            // deleteSelectedButton
            // 
            this.deleteSelectedButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.deleteSelectedButton.BackColor = System.Drawing.SystemColors.ControlLight;
            this.deleteSelectedButton.Image = global::ArchiveCacheManager.Resources.cross_script;
            this.deleteSelectedButton.Location = new System.Drawing.Point(494, 488);
            this.deleteSelectedButton.Name = "deleteSelectedButton";
            this.deleteSelectedButton.Size = new System.Drawing.Size(116, 28);
            this.deleteSelectedButton.TabIndex = 9;
            this.deleteSelectedButton.Text = "Delete";
            this.deleteSelectedButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.deleteSelectedButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.toolTip.SetToolTip(this.deleteSelectedButton, "Delete the selected items from the cache.");
            this.deleteSelectedButton.UseVisualStyleBackColor = true;
            this.deleteSelectedButton.Click += new System.EventHandler(this.deleteSelectedButton_Click);
            // 
            // versionLabel
            // 
            this.versionLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.versionLabel.Location = new System.Drawing.Point(832, 580);
            this.versionLabel.Name = "versionLabel";
            this.versionLabel.Size = new System.Drawing.Size(100, 19);
            this.versionLabel.TabIndex = 8;
            this.versionLabel.Text = "v0.0.0";
            this.versionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // forumLink
            // 
            this.forumLink.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.forumLink.AutoSize = true;
            this.forumLink.Location = new System.Drawing.Point(100, 0);
            this.forumLink.Name = "forumLink";
            this.forumLink.Size = new System.Drawing.Size(76, 13);
            this.forumLink.TabIndex = 31;
            this.forumLink.TabStop = true;
            this.forumLink.Text = "Forum Support";
            this.forumLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.forumLink_LinkClicked);
            // 
            // sourceLink
            // 
            this.sourceLink.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.sourceLink.AutoSize = true;
            this.sourceLink.Location = new System.Drawing.Point(182, 0);
            this.sourceLink.Name = "sourceLink";
            this.sourceLink.Size = new System.Drawing.Size(93, 13);
            this.sourceLink.TabIndex = 32;
            this.sourceLink.TabStop = true;
            this.sourceLink.Text = "GitHub Repository";
            this.sourceLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.sourceLink_LinkClicked);
            // 
            // pluginLink
            // 
            this.pluginLink.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.pluginLink.AutoSize = true;
            this.pluginLink.Location = new System.Drawing.Point(3, 0);
            this.pluginLink.Name = "pluginLink";
            this.pluginLink.Size = new System.Drawing.Size(91, 13);
            this.pluginLink.TabIndex = 30;
            this.pluginLink.TabStop = true;
            this.pluginLink.Text = "Plugin Homepage";
            this.pluginLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.pluginLink_LinkClicked);
            // 
            // okButton
            // 
            this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.okButton.Image = global::ArchiveCacheManager.Resources.tick;
            this.okButton.Location = new System.Drawing.Point(12, 578);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(80, 28);
            this.okButton.TabIndex = 0;
            this.okButton.Text = "OK";
            this.okButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.okButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Image = global::ArchiveCacheManager.Resources.cross_script;
            this.cancelButton.Location = new System.Drawing.Point(98, 578);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(80, 28);
            this.cancelButton.TabIndex = 1;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.cancelButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // treeView1
            // 
            this.treeView1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.treeView1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.treeView1.FullRowSelect = true;
            this.treeView1.HideSelection = false;
            this.treeView1.ItemHeight = 32;
            this.treeView1.Location = new System.Drawing.Point(12, 12);
            this.treeView1.Name = "treeView1";
            treeNode1.Name = "CacheSettings";
            treeNode1.Text = "Cache Settings";
            treeNode2.Name = "ExtractionSettings";
            treeNode2.Text = "Extraction Settings";
            treeNode3.Name = "SmartExtractSettings";
            treeNode3.Text = "Smart Extract Settings";
            treeNode4.Name = "PluginSettings";
            treeNode4.Text = "Plugin Settings";
            treeNode5.Name = "Packaging";
            treeNode5.Text = "Packaging";
            this.treeView1.Nodes.AddRange(new System.Windows.Forms.TreeNode[] {
            treeNode1,
            treeNode2,
            treeNode3,
            treeNode4,
            treeNode5});
            this.treeView1.ShowLines = false;
            this.treeView1.Size = new System.Drawing.Size(166, 550);
            this.treeView1.TabIndex = 3;
            this.treeView1.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeView1_AfterSelect);
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.flowLayoutPanel1.Controls.Add(this.pluginLink);
            this.flowLayoutPanel1.Controls.Add(this.forumLink);
            this.flowLayoutPanel1.Controls.Add(this.sourceLink);
            this.flowLayoutPanel1.Location = new System.Drawing.Point(522, 583);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(320, 26);
            this.flowLayoutPanel1.TabIndex = 33;
            this.flowLayoutPanel1.WrapContents = false;
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tab1CacheSettings);
            this.tabControl1.Controls.Add(this.tab2ExtractionSettings);
            this.tabControl1.Controls.Add(this.tab3SmartExtractSettings);
            this.tabControl1.Controls.Add(this.tab4PluginSettings);
            this.tabControl1.Controls.Add(this.tab5PackagingSettings);
            this.tabControl1.Location = new System.Drawing.Point(184, 12);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(748, 550);
            this.tabControl1.TabIndex = 4;
            this.tabControl1.TabStop = false;
            // 
            // tab1CacheSettings
            // 
            this.tab1CacheSettings.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tab1CacheSettings.Controls.Add(this.label1);
            this.tab1CacheSettings.Controls.Add(this.openInExplorerButton);
            this.tab1CacheSettings.Controls.Add(this.configureCacheButton);
            this.tab1CacheSettings.Controls.Add(this.deleteAllButton);
            this.tab1CacheSettings.Controls.Add(this.cacheSummaryTextBox);
            this.tab1CacheSettings.Controls.Add(this.refreshButton);
            this.tab1CacheSettings.Controls.Add(this.deleteSelectedButton);
            this.tab1CacheSettings.Controls.Add(this.cacheDataGridView);
            this.tab1CacheSettings.Location = new System.Drawing.Point(4, 22);
            this.tab1CacheSettings.Name = "tab1CacheSettings";
            this.tab1CacheSettings.Padding = new System.Windows.Forms.Padding(3);
            this.tab1CacheSettings.Size = new System.Drawing.Size(740, 524);
            this.tab1CacheSettings.TabIndex = 0;
            this.tab1CacheSettings.Text = "Cache Settings";
            this.tab1CacheSettings.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.BackColor = System.Drawing.SystemColors.ControlLight;
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(6, 7);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(726, 43);
            this.label1.TabIndex = 99;
            this.label1.Text = "Cache Settings";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // cacheSummaryTextBox
            // 
            this.cacheSummaryTextBox.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.cacheSummaryTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.cacheSummaryTextBox.Cursor = System.Windows.Forms.Cursors.Default;
            this.cacheSummaryTextBox.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cacheSummaryTextBox.Location = new System.Drawing.Point(9, 62);
            this.cacheSummaryTextBox.Name = "cacheSummaryTextBox";
            this.cacheSummaryTextBox.ReadOnly = true;
            this.cacheSummaryTextBox.Size = new System.Drawing.Size(561, 62);
            this.cacheSummaryTextBox.TabIndex = 99;
            this.cacheSummaryTextBox.TabStop = false;
            this.cacheSummaryTextBox.Text = "";
            // 
            // cacheDataGridView
            // 
            this.cacheDataGridView.AllowUserToAddRows = false;
            this.cacheDataGridView.AllowUserToDeleteRows = false;
            this.cacheDataGridView.AllowUserToResizeRows = false;
            this.cacheDataGridView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cacheDataGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.cacheDataGridView.BackgroundColor = System.Drawing.SystemColors.ControlLightLight;
            this.cacheDataGridView.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.Padding = new System.Windows.Forms.Padding(0, 3, 0, 3);
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.cacheDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.cacheDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.cacheDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ArchivePath,
            this.Archive,
            this.ArchivePlatform,
            this.ArchiveSize,
            this.Keep});
            this.cacheDataGridView.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
            this.cacheDataGridView.Location = new System.Drawing.Point(6, 130);
            this.cacheDataGridView.Name = "cacheDataGridView";
            this.cacheDataGridView.RowHeadersVisible = false;
            this.cacheDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.cacheDataGridView.Size = new System.Drawing.Size(726, 352);
            this.cacheDataGridView.StandardTab = true;
            this.cacheDataGridView.TabIndex = 7;
            this.cacheDataGridView.CellPainting += new System.Windows.Forms.DataGridViewCellPaintingEventHandler(this.cacheDataGridView_CellPainting);
            this.cacheDataGridView.CurrentCellDirtyStateChanged += new System.EventHandler(this.cacheDataGridView_CurrentCellDirtyStateChanged);
            // 
            // ArchivePath
            // 
            this.ArchivePath.HeaderText = "ArchivePath";
            this.ArchivePath.Name = "ArchivePath";
            this.ArchivePath.ReadOnly = true;
            this.ArchivePath.Visible = false;
            // 
            // Archive
            // 
            dataGridViewCellStyle2.Padding = new System.Windows.Forms.Padding(24, 0, 0, 0);
            this.Archive.DefaultCellStyle = dataGridViewCellStyle2;
            this.Archive.HeaderText = "Archive";
            this.Archive.Name = "Archive";
            this.Archive.ReadOnly = true;
            // 
            // ArchivePlatform
            // 
            this.ArchivePlatform.FillWeight = 60F;
            this.ArchivePlatform.HeaderText = "Platform";
            this.ArchivePlatform.Name = "ArchivePlatform";
            this.ArchivePlatform.ReadOnly = true;
            // 
            // ArchiveSize
            // 
            this.ArchiveSize.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle3.Format = "N1";
            this.ArchiveSize.DefaultCellStyle = dataGridViewCellStyle3;
            this.ArchiveSize.FillWeight = 25F;
            this.ArchiveSize.HeaderText = "Size (MB)";
            this.ArchiveSize.Name = "ArchiveSize";
            this.ArchiveSize.ReadOnly = true;
            this.ArchiveSize.Width = 77;
            // 
            // Keep
            // 
            this.Keep.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.Keep.FillWeight = 12F;
            this.Keep.HeaderText = "Keep";
            this.Keep.Name = "Keep";
            this.Keep.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.Keep.Width = 57;
            // 
            // tab2ExtractionSettings
            // 
            this.tab2ExtractionSettings.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tab2ExtractionSettings.Controls.Add(this.label9);
            this.tab2ExtractionSettings.Controls.Add(this.extractionSettingsTipLabel);
            this.tab2ExtractionSettings.Controls.Add(this.label2);
            this.tab2ExtractionSettings.Controls.Add(this.deletePriorityButton);
            this.tab2ExtractionSettings.Controls.Add(this.emulatorPlatformConfigDataGridView);
            this.tab2ExtractionSettings.Controls.Add(this.addPriorityButton);
            this.tab2ExtractionSettings.Location = new System.Drawing.Point(4, 22);
            this.tab2ExtractionSettings.Name = "tab2ExtractionSettings";
            this.tab2ExtractionSettings.Padding = new System.Windows.Forms.Padding(3);
            this.tab2ExtractionSettings.Size = new System.Drawing.Size(740, 524);
            this.tab2ExtractionSettings.TabIndex = 1;
            this.tab2ExtractionSettings.Text = "Extraction Settings";
            this.tab2ExtractionSettings.UseVisualStyleBackColor = true;
            // 
            // label9
            // 
            this.label9.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(6, 451);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(516, 26);
            this.label9.TabIndex = 99;
            this.label9.Text = resources.GetString("label9.Text");
            // 
            // extractionSettingsTipLabel
            // 
            this.extractionSettingsTipLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.extractionSettingsTipLabel.AutoSize = true;
            this.extractionSettingsTipLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.extractionSettingsTipLabel.Location = new System.Drawing.Point(6, 496);
            this.extractionSettingsTipLabel.Name = "extractionSettingsTipLabel";
            this.extractionSettingsTipLabel.Size = new System.Drawing.Size(392, 13);
            this.extractionSettingsTipLabel.TabIndex = 99;
            this.extractionSettingsTipLabel.Text = "Tip: Hover the mouse cursor over a column header for a description of the setting" +
    ".";
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.BackColor = System.Drawing.SystemColors.ControlLight;
            this.label2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(6, 7);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(726, 43);
            this.label2.TabIndex = 99;
            this.label2.Text = "Extraction Settings";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // deletePriorityButton
            // 
            this.deletePriorityButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.deletePriorityButton.BackColor = System.Drawing.SystemColors.ControlLight;
            this.deletePriorityButton.Image = global::ArchiveCacheManager.Resources.cross_script;
            this.deletePriorityButton.Location = new System.Drawing.Point(616, 488);
            this.deletePriorityButton.Name = "deletePriorityButton";
            this.deletePriorityButton.Size = new System.Drawing.Size(116, 28);
            this.deletePriorityButton.TabIndex = 13;
            this.deletePriorityButton.Text = "Delete";
            this.deletePriorityButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.deletePriorityButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.deletePriorityButton.UseVisualStyleBackColor = true;
            this.deletePriorityButton.Click += new System.EventHandler(this.deletePriorityButton_Click);
            // 
            // emulatorPlatformConfigDataGridView
            // 
            this.emulatorPlatformConfigDataGridView.AllowUserToAddRows = false;
            this.emulatorPlatformConfigDataGridView.AllowUserToDeleteRows = false;
            this.emulatorPlatformConfigDataGridView.AllowUserToResizeRows = false;
            this.emulatorPlatformConfigDataGridView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.emulatorPlatformConfigDataGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.emulatorPlatformConfigDataGridView.BackgroundColor = System.Drawing.SystemColors.ControlLightLight;
            this.emulatorPlatformConfigDataGridView.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle4.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle4.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle4.Padding = new System.Windows.Forms.Padding(0, 3, 0, 3);
            dataGridViewCellStyle4.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle4.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.emulatorPlatformConfigDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle4;
            this.emulatorPlatformConfigDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.emulatorPlatformConfigDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Emulator,
            this.Platform,
            this.Priority,
            this.Action,
            this.LaunchPath,
            this.MultiDisc,
            this.M3uName,
            this.SmartExtract,
            this.Chdman,
            this.DolphinTool,
            this.ExtractXiso,
            this.PS3dec,
            this.WiiuCacheOnLaunch,
            this.CiaCacheOnLaunch,
            this.WadCacheOnLaunch,
            this.TadCacheOnLaunch,
            this.Ps3PkgCacheOnLaunch,
            this.PspPkgCacheOnLaunch,
            this.Ctr3dsCacheOnLaunch,
            this.PsvPkgCacheOnLaunch});
            this.emulatorPlatformConfigDataGridView.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
            this.emulatorPlatformConfigDataGridView.Location = new System.Drawing.Point(6, 62);
            this.emulatorPlatformConfigDataGridView.MultiSelect = false;
            this.emulatorPlatformConfigDataGridView.Name = "emulatorPlatformConfigDataGridView";
            this.emulatorPlatformConfigDataGridView.RowHeadersVisible = false;
            this.emulatorPlatformConfigDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.emulatorPlatformConfigDataGridView.Size = new System.Drawing.Size(726, 375);
            this.emulatorPlatformConfigDataGridView.StandardTab = true;
            this.emulatorPlatformConfigDataGridView.TabIndex = 11;
            this.emulatorPlatformConfigDataGridView.CellMouseEnter += new System.Windows.Forms.DataGridViewCellEventHandler(this.emulatorPlatformConfigDataGridView_CellMouseEnter);
            this.emulatorPlatformConfigDataGridView.CellMouseLeave += new System.Windows.Forms.DataGridViewCellEventHandler(this.emulatorPlatformConfigDataGridView_CellMouseLeave);
            this.emulatorPlatformConfigDataGridView.CellPainting += new System.Windows.Forms.DataGridViewCellPaintingEventHandler(this.emulatorPlatformConfigDataGridView_CellPainting);
            this.emulatorPlatformConfigDataGridView.SelectionChanged += new System.EventHandler(this.extensionPriorityDataGridView_SelectionChanged);
            // 
            // addPriorityButton
            // 
            this.addPriorityButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.addPriorityButton.Image = global::ArchiveCacheManager.Resources.plus;
            this.addPriorityButton.Location = new System.Drawing.Point(494, 488);
            this.addPriorityButton.Name = "addPriorityButton";
            this.addPriorityButton.Size = new System.Drawing.Size(116, 28);
            this.addPriorityButton.TabIndex = 12;
            this.addPriorityButton.Text = "Add...";
            this.addPriorityButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.addPriorityButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.addPriorityButton.UseVisualStyleBackColor = true;
            this.addPriorityButton.Click += new System.EventHandler(this.addPriorityButton_Click);
            // 
            // tab3SmartExtractSettings
            // 
            this.tab3SmartExtractSettings.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tab3SmartExtractSettings.Controls.Add(this.label6);
            this.tab3SmartExtractSettings.Controls.Add(this.label5);
            this.tab3SmartExtractSettings.Controls.Add(this.metadataExtensions);
            this.tab3SmartExtractSettings.Controls.Add(this.cachePathLabel);
            this.tab3SmartExtractSettings.Controls.Add(this.standaloneExtensions);
            this.tab3SmartExtractSettings.Controls.Add(this.label4);
            this.tab3SmartExtractSettings.Location = new System.Drawing.Point(4, 22);
            this.tab3SmartExtractSettings.Name = "tab3SmartExtractSettings";
            this.tab3SmartExtractSettings.Padding = new System.Windows.Forms.Padding(3);
            this.tab3SmartExtractSettings.Size = new System.Drawing.Size(740, 524);
            this.tab3SmartExtractSettings.TabIndex = 3;
            this.tab3SmartExtractSettings.Text = "Smart Extract Settings";
            this.tab3SmartExtractSettings.UseVisualStyleBackColor = true;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(7, 174);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(471, 130);
            this.label6.TabIndex = 99;
            this.label6.Text = resources.GetString("label6.Text");
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(7, 117);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(109, 13);
            this.label5.TabIndex = 99;
            this.label5.Text = "Metadata Extensions:";
            // 
            // metadataExtensions
            // 
            this.metadataExtensions.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.metadataExtensions.Location = new System.Drawing.Point(10, 133);
            this.metadataExtensions.MaxLength = 260;
            this.metadataExtensions.Name = "metadataExtensions";
            this.metadataExtensions.Size = new System.Drawing.Size(721, 20);
            this.metadataExtensions.TabIndex = 15;
            // 
            // cachePathLabel
            // 
            this.cachePathLabel.AutoSize = true;
            this.cachePathLabel.Location = new System.Drawing.Point(7, 60);
            this.cachePathLabel.Name = "cachePathLabel";
            this.cachePathLabel.Size = new System.Drawing.Size(149, 13);
            this.cachePathLabel.TabIndex = 99;
            this.cachePathLabel.Text = "Stand-alone ROM Extensions:";
            // 
            // standaloneExtensions
            // 
            this.standaloneExtensions.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.standaloneExtensions.Location = new System.Drawing.Point(10, 76);
            this.standaloneExtensions.MaxLength = 260;
            this.standaloneExtensions.Name = "standaloneExtensions";
            this.standaloneExtensions.Size = new System.Drawing.Size(721, 20);
            this.standaloneExtensions.TabIndex = 14;
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label4.BackColor = System.Drawing.SystemColors.ControlLight;
            this.label4.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(6, 7);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(726, 43);
            this.label4.TabIndex = 99;
            this.label4.Text = "Smart Extract Settings";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // tab4PluginSettings
            // 
            this.tab4PluginSettings.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tab4PluginSettings.Controls.Add(this.label8);
            this.tab4PluginSettings.Controls.Add(this.bypassPathCheckCheckBox);
            this.tabPackagingSony.Controls.Add(this.packagingPs3IsoSectionLabel);
            this.tabPackagingSony.Controls.Add(this.ps3KeyPathLabel);
            this.tabPackagingSony.Controls.Add(this.ps3KeyPath);
            this.tabPackagingSony.Controls.Add(this.ps3KeyPathBrowseButton);
            this.tabPackagingSony.Controls.Add(this.ps3UseIsoMountLauncherCheckBox);
            this.tab4PluginSettings.Controls.Add(this.label7);
            this.tab4PluginSettings.Controls.Add(this.label3);
            this.tab4PluginSettings.Controls.Add(this.updateCheckCheckBox);
            this.tab4PluginSettings.Location = new System.Drawing.Point(4, 22);
            this.tab4PluginSettings.Name = "tab4PluginSettings";
            this.tab4PluginSettings.Padding = new System.Windows.Forms.Padding(3);
            this.tab4PluginSettings.Size = new System.Drawing.Size(740, 524);
            this.tab4PluginSettings.TabIndex = 2;
            this.tab4PluginSettings.Text = "Plugin Settings";
            this.tab4PluginSettings.UseVisualStyleBackColor = true;
            //
            // tab5PackagingSettings
            //
            this.tab5PackagingSettings.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tabPackagingNintendo.Controls.Add(this.wiiuLocalRomFoldersButton);
            this.tabPackagingNintendo.Controls.Add(this.ctr3dsLocalRomFoldersButton);
            this.tabPackagingNintendo.Controls.Add(this.packagingWiiSectionLabel);
            this.tabPackagingNintendo.Controls.Add(this.wadPlatformLabel);
            this.tabPackagingNintendo.Controls.Add(this.wadPlatform);
            this.tabPackagingNintendo.Controls.Add(this.wadOutputPathLabel);
            this.tabPackagingNintendo.Controls.Add(this.wadOutputPath);
            this.tabPackagingNintendo.Controls.Add(this.wadOutputPathBrowseButton);
            this.tabPackagingNintendo.Controls.Add(this.wadCetkCachePathLabel);
            this.tabPackagingNintendo.Controls.Add(this.wadCetkCachePath);
            this.tabPackagingNintendo.Controls.Add(this.wadCetkCachePathBrowseButton);
            this.tabPackagingNintendo.Controls.Add(this.wadAddToLibraryCheckBox);
            this.tabPackagingNintendo.Controls.Add(this.packagingWiiuSectionLabel);
            this.tabPackagingNintendo.Controls.Add(this.wiiuPlatformLabel);
            this.tabPackagingNintendo.Controls.Add(this.wiiuPlatform);
            this.tabPackagingNintendo.Controls.Add(this.wiiuOutputPathLabel);
            this.tabPackagingNintendo.Controls.Add(this.wiiuOutputPath);
            this.tabPackagingNintendo.Controls.Add(this.wiiuOutputPathBrowseButton);
            this.tabPackagingNintendo.Controls.Add(this.wiiuCommonKeyLabel);
            this.tabPackagingNintendo.Controls.Add(this.wiiuCommonKey);
            this.tabPackagingNintendo.Controls.Add(this.wiiuTitleKeyPasswordLabel);
            this.tabPackagingNintendo.Controls.Add(this.wiiuTitleKeyPassword);
            this.tabPackagingNintendo.Controls.Add(this.wiiuAddToLibraryCheckBox);
            this.tabPackagingNintendo.Controls.Add(this.wiiuCemuKeysPathLabel);
            this.tabPackagingNintendo.Controls.Add(this.wiiuCemuKeysPath);
            this.tabPackagingNintendo.Controls.Add(this.wiiuCemuKeysPathBrowseButton);
            this.tabPackagingNintendo.Controls.Add(this.packagingCiaSectionLabel);
            this.tabPackagingNintendo.Controls.Add(this.ciaPlatformLabel);
            this.tabPackagingNintendo.Controls.Add(this.ciaPlatform);
            this.tabPackagingNintendo.Controls.Add(this.ciaOutputPathLabel);
            this.tabPackagingNintendo.Controls.Add(this.ciaOutputPath);
            this.tabPackagingNintendo.Controls.Add(this.ciaOutputPathBrowseButton);
            this.tabPackagingNintendo.Controls.Add(this.ciaCetkCachePathLabel);
            this.tabPackagingNintendo.Controls.Add(this.ciaCetkCachePath);
            this.tabPackagingNintendo.Controls.Add(this.ciaCetkCachePathBrowseButton);
            this.tabPackagingNintendo.Controls.Add(this.ciaAddToLibraryCheckBox);
            this.tabPackagingNintendo.Controls.Add(this.packagingTadSectionLabel);
            this.tabPackagingNintendo.Controls.Add(this.tadPlatformLabel);
            this.tabPackagingNintendo.Controls.Add(this.tadPlatform);
            this.tabPackagingNintendo.Controls.Add(this.tadOutputPathLabel);
            this.tabPackagingNintendo.Controls.Add(this.tadOutputPath);
            this.tabPackagingNintendo.Controls.Add(this.tadOutputPathBrowseButton);
            this.tabPackagingNintendo.Controls.Add(this.tadCetkCachePathLabel);
            this.tabPackagingNintendo.Controls.Add(this.tadCetkCachePath);
            this.tabPackagingNintendo.Controls.Add(this.tadCetkCachePathBrowseButton);
            this.tabPackagingNintendo.Controls.Add(this.tadAddToLibraryCheckBox);
            this.tabPackagingSony.Controls.Add(this.packagingPs3PkgSectionLabel);
            this.tabPackagingSony.Controls.Add(this.ps3PkgPlatformLabel);
            this.tabPackagingSony.Controls.Add(this.ps3PkgPlatform);
            this.tabPackagingSony.Controls.Add(this.ps3PkgOutputPathLabel);
            this.tabPackagingSony.Controls.Add(this.ps3PkgOutputPath);
            this.tabPackagingSony.Controls.Add(this.ps3PkgOutputPathBrowseButton);
            this.tabPackagingSony.Controls.Add(this.ps3RpcsExdataPathLabel);
            this.tabPackagingSony.Controls.Add(this.ps3RpcsExdataPath);
            this.tabPackagingSony.Controls.Add(this.ps3RpcsExdataPathBrowseButton);
            this.tabPackagingSony.Controls.Add(this.ps3PkgAddToLibraryCheckBox);
            this.tabPackagingSony.Controls.Add(this.ps3PkgAutoInstallToRpcs3CheckBox);
            this.tabPackagingSony.Controls.Add(this.ps3AutoInstallDlcsCheckBox);
            this.tabPackagingSony.Controls.Add(this.pspAutoInstallDlcsCheckBox);
            this.tabPackagingSony.Controls.Add(this.psvAutoInstallDlcsCheckBox);
            this.tabPackagingSony.Controls.Add(this.ps3LocalPkgFoldersButton);
            this.tabPackagingSony.Controls.Add(this.pspLocalPkgFoldersButton);
            this.tabPackagingSony.Controls.Add(this.psvLocalPkgFoldersButton);
            this.tabPackagingSony.Controls.Add(this.packagingPspPkgSectionLabel);
            this.tabPackagingSony.Controls.Add(this.pspPkgPlatformLabel);
            this.tabPackagingSony.Controls.Add(this.pspPkgPlatform);
            this.tabPackagingSony.Controls.Add(this.pspPkgOutputPathLabel);
            this.tabPackagingSony.Controls.Add(this.pspPkgOutputPath);
            this.tabPackagingSony.Controls.Add(this.pspPkgOutputPathBrowseButton);
            this.tabPackagingSony.Controls.Add(this.pspPpssppLicensePathLabel);
            this.tabPackagingSony.Controls.Add(this.pspPpssppLicensePath);
            this.tabPackagingSony.Controls.Add(this.pspPpssppLicensePathBrowseButton);
            this.tabPackagingSony.Controls.Add(this.pspPkgAddToLibraryCheckBox);
            this.tabPackagingSony.Controls.Add(this.packagingPs3AutoUpdateSectionLabel);
            this.tabPackagingSony.Controls.Add(this.ps3AutoInstallUpdatesCheckBox);
            this.tabPackagingSony.Controls.Add(this.ps3UpdateCachePathLabel);
            this.tabPackagingSony.Controls.Add(this.ps3UpdateCachePath);
            this.tabPackagingSony.Controls.Add(this.ps3UpdateCachePathBrowseButton);
            this.tabPackagingSony.Controls.Add(this.ps3UpdateOfflineModeCheckBox);
            this.tabPackagingSony.Controls.Add(this.packagingPspAutoUpdateSectionLabel);
            this.tabPackagingSony.Controls.Add(this.pspAutoInstallUpdatesCheckBox);
            this.tabPackagingSony.Controls.Add(this.pspUpdateCachePathLabel);
            this.tabPackagingSony.Controls.Add(this.pspUpdateCachePath);
            this.tabPackagingSony.Controls.Add(this.pspUpdateCachePathBrowseButton);
            this.tabPackagingSony.Controls.Add(this.pspUpdateOfflineModeCheckBox);
            this.tabPackagingNintendo.Controls.Add(this.packagingCtr3dsSectionLabel);
            this.tabPackagingNintendo.Controls.Add(this.ctr3dsPlatformLabel);
            this.tabPackagingNintendo.Controls.Add(this.ctr3dsPlatform);
            this.tabPackagingNintendo.Controls.Add(this.ctr3dsKeysPathLabel);
            this.tabPackagingNintendo.Controls.Add(this.ctr3dsKeysPath);
            this.tabPackagingNintendo.Controls.Add(this.ctr3dsKeysPathBrowseButton);
            this.tabPackagingNintendo.Controls.Add(this.ctr3dsSeedDbPathLabel);
            this.tabPackagingNintendo.Controls.Add(this.ctr3dsSeedDbPath);
            this.tabPackagingNintendo.Controls.Add(this.ctr3dsSeedDbPathBrowseButton);
            this.tabPackagingNintendo.Controls.Add(this.ctr3dsOutputPathLabel);
            this.tabPackagingNintendo.Controls.Add(this.ctr3dsOutputPath);
            this.tabPackagingNintendo.Controls.Add(this.ctr3dsOutputPathBrowseButton);
            this.tabPackagingNintendo.Controls.Add(this.ctr3dsAddToLibraryCheckBox);
            this.tabPackagingSony.Controls.Add(this.packagingPsvPkgSectionLabel);
            this.tabPackagingSony.Controls.Add(this.psvPkgPlatformLabel);
            this.tabPackagingSony.Controls.Add(this.psvPkgPlatform);
            this.tabPackagingSony.Controls.Add(this.psvPkgOutputPathLabel);
            this.tabPackagingSony.Controls.Add(this.psvPkgOutputPath);
            this.tabPackagingSony.Controls.Add(this.psvPkgOutputPathBrowseButton);
            this.tabPackagingSony.Controls.Add(this.psvPkgAddToLibraryCheckBox);
            this.tabPackagingSony.Controls.Add(this.psvVita3kDataPathLabel);
            this.tabPackagingSony.Controls.Add(this.psvVita3kDataPath);
            this.tabPackagingSony.Controls.Add(this.psvVita3kDataPathBrowseButton);
            this.tabPackagingSony.Controls.Add(this.npsDbPathLabel);
            this.tabPackagingSony.Controls.Add(this.npsDbPath);
            this.tabPackagingSony.Controls.Add(this.npsDbPathBrowseButton);
            this.tabPackagingSony.Controls.Add(this.packagingPsvAutoUpdateSectionLabel);
            this.tabPackagingSony.Controls.Add(this.psvAutoInstallUpdatesCheckBox);
            this.tabPackagingSony.Controls.Add(this.psvUpdateCachePathLabel);
            this.tabPackagingSony.Controls.Add(this.psvUpdateCachePath);
            this.tabPackagingSony.Controls.Add(this.psvUpdateCachePathBrowseButton);
            this.tabPackagingSony.Controls.Add(this.psvUpdateOfflineModeCheckBox);
            this.tab5PackagingSettings.Controls.Add(this.packagingSubTabs);
            this.tab5PackagingSettings.AutoScroll = false;
            this.tab5PackagingSettings.Location = new System.Drawing.Point(4, 22);
            this.tab5PackagingSettings.Name = "tab5PackagingSettings";
            this.tab5PackagingSettings.Padding = new System.Windows.Forms.Padding(3);
            this.tab5PackagingSettings.Size = new System.Drawing.Size(740, 524);
            this.tab5PackagingSettings.TabIndex = 4;
            this.tab5PackagingSettings.Text = "Packaging";
            this.tab5PackagingSettings.UseVisualStyleBackColor = true;
            //
            // packagingSubTabs
            //
            this.packagingSubTabs.Controls.Add(this.tabPackagingNintendo);
            this.packagingSubTabs.Controls.Add(this.tabPackagingSony);
            this.packagingSubTabs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.packagingSubTabs.Location = new System.Drawing.Point(3, 3);
            this.packagingSubTabs.Name = "packagingSubTabs";
            this.packagingSubTabs.SelectedIndex = 0;
            this.packagingSubTabs.Size = new System.Drawing.Size(734, 518);
            this.packagingSubTabs.TabIndex = 0;
            //
            // tabPackagingNintendo
            //
            this.tabPackagingNintendo.AutoScroll = true;
            this.tabPackagingNintendo.BackColor = System.Drawing.SystemColors.Control;
            this.tabPackagingNintendo.Location = new System.Drawing.Point(4, 22);
            this.tabPackagingNintendo.Name = "tabPackagingNintendo";
            this.tabPackagingNintendo.Padding = new System.Windows.Forms.Padding(3);
            this.tabPackagingNintendo.Size = new System.Drawing.Size(726, 492);
            this.tabPackagingNintendo.TabIndex = 0;
            this.tabPackagingNintendo.Text = "Nintendo (Wii / Wii U / 3DS / DSi)";
            //
            // tabPackagingSony
            //
            this.tabPackagingSony.AutoScroll = true;
            this.tabPackagingSony.BackColor = System.Drawing.SystemColors.Control;
            this.tabPackagingSony.Location = new System.Drawing.Point(4, 22);
            this.tabPackagingSony.Name = "tabPackagingSony";
            this.tabPackagingSony.Padding = new System.Windows.Forms.Padding(3);
            this.tabPackagingSony.Size = new System.Drawing.Size(726, 492);
            this.tabPackagingSony.TabIndex = 1;
            this.tabPackagingSony.Text = "Sony (PS3 / PSP / PSV) + NoPayStation";
            //
            // packagingWiiSectionLabel
            //
            this.packagingWiiSectionLabel.AutoSize = true;
            this.packagingWiiSectionLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold);
            this.packagingWiiSectionLabel.Location = new System.Drawing.Point(9, 12);
            this.packagingWiiSectionLabel.Name = "packagingWiiSectionLabel";
            this.packagingWiiSectionLabel.Size = new System.Drawing.Size(86, 15);
            this.packagingWiiSectionLabel.TabIndex = 100;
            this.packagingWiiSectionLabel.Text = "Wii (WAD)  ·  Right-click 'Create Wii WAD...' menu + shared ticket cache";
            //
            // wadPlatformLabel
            //
            this.wadPlatformLabel.AutoSize = true;
            this.wadPlatformLabel.Location = new System.Drawing.Point(9, 36);
            this.wadPlatformLabel.Name = "wadPlatformLabel";
            this.wadPlatformLabel.Size = new System.Drawing.Size(180, 13);
            this.wadPlatformLabel.TabIndex = 101;
            this.wadPlatformLabel.Text = "Menu platforms — show 'Create Wii WAD...' on these (multi-select):";
            //
            // wadPlatform
            //
            this.wadPlatform.CheckOnClick = true;
            this.wadPlatform.IntegralHeight = false;
            this.wadPlatform.Location = new System.Drawing.Point(12, 52);
            this.wadPlatform.Name = "wadPlatform";
            this.wadPlatform.Size = new System.Drawing.Size(297, 120);
            this.wadPlatform.TabIndex = 1;
            //
            // wadOutputPathLabel
            //
            this.wadOutputPathLabel.AutoSize = true;
            this.wadOutputPathLabel.Location = new System.Drawing.Point(9, 180);
            this.wadOutputPathLabel.Name = "wadOutputPathLabel";
            this.wadOutputPathLabel.Size = new System.Drawing.Size(220, 13);
            this.wadOutputPathLabel.TabIndex = 102;
            this.wadOutputPathLabel.Text = "Menu output folder (empty = next to source):";
            //
            // wadOutputPath
            //
            this.wadOutputPath.Location = new System.Drawing.Point(12, 196);
            this.wadOutputPath.MaxLength = 260;
            this.wadOutputPath.Name = "wadOutputPath";
            this.wadOutputPath.Size = new System.Drawing.Size(497, 20);
            this.wadOutputPath.TabIndex = 2;
            //
            // wadOutputPathBrowseButton
            //
            this.wadOutputPathBrowseButton.Image = global::ArchiveCacheManager.Resources.folder_horizontal_open;
            this.wadOutputPathBrowseButton.Location = new System.Drawing.Point(515, 193);
            this.wadOutputPathBrowseButton.Name = "wadOutputPathBrowseButton";
            this.wadOutputPathBrowseButton.Size = new System.Drawing.Size(97, 28);
            this.wadOutputPathBrowseButton.TabIndex = 3;
            this.wadOutputPathBrowseButton.Text = "Browse...";
            this.wadOutputPathBrowseButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.wadOutputPathBrowseButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.wadOutputPathBrowseButton.UseVisualStyleBackColor = true;
            this.wadOutputPathBrowseButton.Click += new System.EventHandler(this.wadOutputPathBrowseButton_Click);
            //
            // wadCetkCachePathLabel
            //
            this.wadCetkCachePathLabel.AutoSize = true;
            this.wadCetkCachePathLabel.Location = new System.Drawing.Point(9, 228);
            this.wadCetkCachePathLabel.Name = "wadCetkCachePathLabel";
            this.wadCetkCachePathLabel.Size = new System.Drawing.Size(220, 13);
            this.wadCetkCachePathLabel.TabIndex = 103;
            this.wadCetkCachePathLabel.Text = "Ticket cache folder — shared with auto-cache (empty = no cache):";
            //
            // wadCetkCachePath
            //
            this.wadCetkCachePath.Location = new System.Drawing.Point(12, 244);
            this.wadCetkCachePath.MaxLength = 260;
            this.wadCetkCachePath.Name = "wadCetkCachePath";
            this.wadCetkCachePath.Size = new System.Drawing.Size(497, 20);
            this.wadCetkCachePath.TabIndex = 4;
            //
            // wadCetkCachePathBrowseButton
            //
            this.wadCetkCachePathBrowseButton.Image = global::ArchiveCacheManager.Resources.folder_horizontal_open;
            this.wadCetkCachePathBrowseButton.Location = new System.Drawing.Point(515, 241);
            this.wadCetkCachePathBrowseButton.Name = "wadCetkCachePathBrowseButton";
            this.wadCetkCachePathBrowseButton.Size = new System.Drawing.Size(97, 28);
            this.wadCetkCachePathBrowseButton.TabIndex = 5;
            this.wadCetkCachePathBrowseButton.Text = "Browse...";
            this.wadCetkCachePathBrowseButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.wadCetkCachePathBrowseButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.wadCetkCachePathBrowseButton.UseVisualStyleBackColor = true;
            this.wadCetkCachePathBrowseButton.Click += new System.EventHandler(this.wadCetkCachePathBrowseButton_Click);
            //
            // wadAddToLibraryCheckBox
            //
            this.wadAddToLibraryCheckBox.AutoSize = true;
            this.wadAddToLibraryCheckBox.Location = new System.Drawing.Point(12, 276);
            this.wadAddToLibraryCheckBox.Name = "wadAddToLibraryCheckBox";
            this.wadAddToLibraryCheckBox.Size = new System.Drawing.Size(280, 17);
            this.wadAddToLibraryCheckBox.TabIndex = 6;
            this.wadAddToLibraryCheckBox.Text = "Menu: add created WAD to LaunchBox library";
            this.wadAddToLibraryCheckBox.UseVisualStyleBackColor = true;
            //
            // packagingWiiuSectionLabel
            //
            this.packagingWiiuSectionLabel.AutoSize = true;
            this.packagingWiiuSectionLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold);
            this.packagingWiiuSectionLabel.Location = new System.Drawing.Point(9, 310);
            this.packagingWiiuSectionLabel.Name = "packagingWiiuSectionLabel";
            this.packagingWiiuSectionLabel.Size = new System.Drawing.Size(40, 15);
            this.packagingWiiuSectionLabel.TabIndex = 104;
            this.packagingWiiuSectionLabel.Text = "Wii U  ·  Right-click 'Create Wii U Package...' menu + shared keys/password";
            //
            // wiiuPlatformLabel
            //
            this.wiiuPlatformLabel.AutoSize = true;
            this.wiiuPlatformLabel.Location = new System.Drawing.Point(9, 334);
            this.wiiuPlatformLabel.Name = "wiiuPlatformLabel";
            this.wiiuPlatformLabel.Size = new System.Drawing.Size(180, 13);
            this.wiiuPlatformLabel.TabIndex = 105;
            this.wiiuPlatformLabel.Text = "Menu platforms — show 'Create Wii U Package...' on these (multi-select):";
            //
            // wiiuPlatform
            //
            this.wiiuPlatform.CheckOnClick = true;
            this.wiiuPlatform.IntegralHeight = false;
            this.wiiuPlatform.Location = new System.Drawing.Point(12, 350);
            this.wiiuPlatform.Name = "wiiuPlatform";
            this.wiiuPlatform.Size = new System.Drawing.Size(297, 120);
            this.wiiuPlatform.TabIndex = 7;
            //
            // wiiuOutputPathLabel
            //
            this.wiiuOutputPathLabel.AutoSize = true;
            this.wiiuOutputPathLabel.Location = new System.Drawing.Point(9, 478);
            this.wiiuOutputPathLabel.Name = "wiiuOutputPathLabel";
            this.wiiuOutputPathLabel.Size = new System.Drawing.Size(220, 13);
            this.wiiuOutputPathLabel.TabIndex = 106;
            this.wiiuOutputPathLabel.Text = "Menu output folder (empty = next to source):";
            //
            // wiiuOutputPath
            //
            this.wiiuOutputPath.Location = new System.Drawing.Point(12, 494);
            this.wiiuOutputPath.MaxLength = 260;
            this.wiiuOutputPath.Name = "wiiuOutputPath";
            this.wiiuOutputPath.Size = new System.Drawing.Size(497, 20);
            this.wiiuOutputPath.TabIndex = 8;
            //
            // wiiuOutputPathBrowseButton
            //
            this.wiiuOutputPathBrowseButton.Image = global::ArchiveCacheManager.Resources.folder_horizontal_open;
            this.wiiuOutputPathBrowseButton.Location = new System.Drawing.Point(515, 491);
            this.wiiuOutputPathBrowseButton.Name = "wiiuOutputPathBrowseButton";
            this.wiiuOutputPathBrowseButton.Size = new System.Drawing.Size(97, 28);
            this.wiiuOutputPathBrowseButton.TabIndex = 9;
            this.wiiuOutputPathBrowseButton.Text = "Browse...";
            this.wiiuOutputPathBrowseButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.wiiuOutputPathBrowseButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.wiiuOutputPathBrowseButton.UseVisualStyleBackColor = true;
            this.wiiuOutputPathBrowseButton.Click += new System.EventHandler(this.wiiuOutputPathBrowseButton_Click);
            //
            // wiiuCommonKeyLabel
            //
            this.wiiuCommonKeyLabel.AutoSize = true;
            this.wiiuCommonKeyLabel.Location = new System.Drawing.Point(9, 526);
            this.wiiuCommonKeyLabel.Name = "wiiuCommonKeyLabel";
            this.wiiuCommonKeyLabel.Size = new System.Drawing.Size(220, 13);
            this.wiiuCommonKeyLabel.TabIndex = 107;
            this.wiiuCommonKeyLabel.Text = "Common Key — shared with auto-cache (32 hex characters):";
            //
            // wiiuCommonKey
            //
            this.wiiuCommonKey.Location = new System.Drawing.Point(12, 542);
            this.wiiuCommonKey.MaxLength = 32;
            this.wiiuCommonKey.Name = "wiiuCommonKey";
            this.wiiuCommonKey.Size = new System.Drawing.Size(297, 20);
            this.wiiuCommonKey.TabIndex = 10;
            this.wiiuCommonKey.UseSystemPasswordChar = true;
            //
            // wiiuTitleKeyPasswordLabel
            //
            this.wiiuTitleKeyPasswordLabel.AutoSize = true;
            this.wiiuTitleKeyPasswordLabel.Location = new System.Drawing.Point(9, 574);
            this.wiiuTitleKeyPasswordLabel.Name = "wiiuTitleKeyPasswordLabel";
            this.wiiuTitleKeyPasswordLabel.Size = new System.Drawing.Size(180, 13);
            this.wiiuTitleKeyPasswordLabel.TabIndex = 108;
            this.wiiuTitleKeyPasswordLabel.Text = "Title Key Password — shared with auto-cache:";
            //
            // wiiuTitleKeyPassword
            //
            this.wiiuTitleKeyPassword.Location = new System.Drawing.Point(12, 590);
            this.wiiuTitleKeyPassword.MaxLength = 64;
            this.wiiuTitleKeyPassword.Name = "wiiuTitleKeyPassword";
            this.wiiuTitleKeyPassword.Size = new System.Drawing.Size(297, 20);
            this.wiiuTitleKeyPassword.TabIndex = 11;
            //
            // wiiuCemuKeysPathLabel
            //
            this.wiiuCemuKeysPathLabel.AutoSize = true;
            this.wiiuCemuKeysPathLabel.Location = new System.Drawing.Point(9, 614);
            this.wiiuCemuKeysPathLabel.Name = "wiiuCemuKeysPathLabel";
            this.wiiuCemuKeysPathLabel.Size = new System.Drawing.Size(420, 13);
            this.wiiuCemuKeysPathLabel.TabIndex = 130;
            this.wiiuCemuKeysPathLabel.Text = "Cemu keys.txt path — shared with auto-cache (empty = auto-detect %APPDATA%\\Cemu\\keys.txt):";
            //
            // wiiuCemuKeysPath
            //
            this.wiiuCemuKeysPath.Location = new System.Drawing.Point(12, 630);
            this.wiiuCemuKeysPath.MaxLength = 260;
            this.wiiuCemuKeysPath.Name = "wiiuCemuKeysPath";
            this.wiiuCemuKeysPath.Size = new System.Drawing.Size(497, 20);
            this.wiiuCemuKeysPath.TabIndex = 131;
            //
            // wiiuCemuKeysPathBrowseButton
            //
            this.wiiuCemuKeysPathBrowseButton.Image = global::ArchiveCacheManager.Resources.folder_horizontal_open;
            this.wiiuCemuKeysPathBrowseButton.Location = new System.Drawing.Point(515, 627);
            this.wiiuCemuKeysPathBrowseButton.Name = "wiiuCemuKeysPathBrowseButton";
            this.wiiuCemuKeysPathBrowseButton.Size = new System.Drawing.Size(97, 28);
            this.wiiuCemuKeysPathBrowseButton.TabIndex = 132;
            this.wiiuCemuKeysPathBrowseButton.Text = "Browse...";
            this.wiiuCemuKeysPathBrowseButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.wiiuCemuKeysPathBrowseButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.wiiuCemuKeysPathBrowseButton.UseVisualStyleBackColor = true;
            this.wiiuCemuKeysPathBrowseButton.Click += new System.EventHandler(this.wiiuCemuKeysPathBrowseButton_Click);
            //
            // wiiuAddToLibraryCheckBox
            //
            this.wiiuAddToLibraryCheckBox.AutoSize = true;
            this.wiiuAddToLibraryCheckBox.Location = new System.Drawing.Point(12, 668);
            this.wiiuAddToLibraryCheckBox.Name = "wiiuAddToLibraryCheckBox";
            this.wiiuAddToLibraryCheckBox.Size = new System.Drawing.Size(290, 17);
            this.wiiuAddToLibraryCheckBox.TabIndex = 12;
            this.wiiuAddToLibraryCheckBox.Text = "Menu: add created Wii U package to LaunchBox library";
            this.wiiuAddToLibraryCheckBox.UseVisualStyleBackColor = true;
            //
            // packagingCiaSectionLabel
            //
            this.packagingCiaSectionLabel.AutoSize = true;
            this.packagingCiaSectionLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold);
            this.packagingCiaSectionLabel.Location = new System.Drawing.Point(9, 708);
            this.packagingCiaSectionLabel.Name = "packagingCiaSectionLabel";
            this.packagingCiaSectionLabel.Size = new System.Drawing.Size(80, 15);
            this.packagingCiaSectionLabel.TabIndex = 109;
            this.packagingCiaSectionLabel.Text = "3DS (CIA)  ·  Right-click 'Create CIA Package...' menu + shared ticket cache";
            //
            // ciaPlatformLabel
            //
            this.ciaPlatformLabel.AutoSize = true;
            this.ciaPlatformLabel.Location = new System.Drawing.Point(9, 732);
            this.ciaPlatformLabel.Name = "ciaPlatformLabel";
            this.ciaPlatformLabel.Size = new System.Drawing.Size(180, 13);
            this.ciaPlatformLabel.TabIndex = 110;
            this.ciaPlatformLabel.Text = "Menu platforms — show 'Create CIA Package...' on these (multi-select):";
            //
            // ciaPlatform
            //
            this.ciaPlatform.CheckOnClick = true;
            this.ciaPlatform.IntegralHeight = false;
            this.ciaPlatform.Location = new System.Drawing.Point(12, 748);
            this.ciaPlatform.Name = "ciaPlatform";
            this.ciaPlatform.Size = new System.Drawing.Size(297, 120);
            this.ciaPlatform.TabIndex = 13;
            //
            // ciaOutputPathLabel
            //
            this.ciaOutputPathLabel.AutoSize = true;
            this.ciaOutputPathLabel.Location = new System.Drawing.Point(9, 972);
            this.ciaOutputPathLabel.Name = "ciaOutputPathLabel";
            this.ciaOutputPathLabel.Size = new System.Drawing.Size(220, 13);
            this.ciaOutputPathLabel.TabIndex = 111;
            this.ciaOutputPathLabel.Text = "Menu output folder (empty = next to source):";
            //
            // ciaOutputPath
            //
            this.ciaOutputPath.Location = new System.Drawing.Point(12, 940);
            this.ciaOutputPath.MaxLength = 260;
            this.ciaOutputPath.Name = "ciaOutputPath";
            this.ciaOutputPath.Size = new System.Drawing.Size(497, 20);
            this.ciaOutputPath.TabIndex = 14;
            //
            // ciaOutputPathBrowseButton
            //
            this.ciaOutputPathBrowseButton.Image = global::ArchiveCacheManager.Resources.folder_horizontal_open;
            this.ciaOutputPathBrowseButton.Location = new System.Drawing.Point(515, 937);
            this.ciaOutputPathBrowseButton.Name = "ciaOutputPathBrowseButton";
            this.ciaOutputPathBrowseButton.Size = new System.Drawing.Size(97, 28);
            this.ciaOutputPathBrowseButton.TabIndex = 15;
            this.ciaOutputPathBrowseButton.Text = "Browse...";
            this.ciaOutputPathBrowseButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.ciaOutputPathBrowseButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.ciaOutputPathBrowseButton.UseVisualStyleBackColor = true;
            this.ciaOutputPathBrowseButton.Click += new System.EventHandler(this.ciaOutputPathBrowseButton_Click);
            //
            // ciaCetkCachePathLabel
            //
            this.ciaCetkCachePathLabel.AutoSize = true;
            this.ciaCetkCachePathLabel.Location = new System.Drawing.Point(9, 972);
            this.ciaCetkCachePathLabel.Name = "ciaCetkCachePathLabel";
            this.ciaCetkCachePathLabel.Size = new System.Drawing.Size(220, 13);
            this.ciaCetkCachePathLabel.TabIndex = 112;
            this.ciaCetkCachePathLabel.Text = "Ticket cache folder — shared with auto-cache (empty = no cache):";
            //
            // ciaCetkCachePath
            //
            this.ciaCetkCachePath.Location = new System.Drawing.Point(12, 940);
            this.ciaCetkCachePath.MaxLength = 260;
            this.ciaCetkCachePath.Name = "ciaCetkCachePath";
            this.ciaCetkCachePath.Size = new System.Drawing.Size(497, 20);
            this.ciaCetkCachePath.TabIndex = 16;
            //
            // ciaCetkCachePathBrowseButton
            //
            this.ciaCetkCachePathBrowseButton.Image = global::ArchiveCacheManager.Resources.folder_horizontal_open;
            this.ciaCetkCachePathBrowseButton.Location = new System.Drawing.Point(515, 937);
            this.ciaCetkCachePathBrowseButton.Name = "ciaCetkCachePathBrowseButton";
            this.ciaCetkCachePathBrowseButton.Size = new System.Drawing.Size(97, 28);
            this.ciaCetkCachePathBrowseButton.TabIndex = 17;
            this.ciaCetkCachePathBrowseButton.Text = "Browse...";
            this.ciaCetkCachePathBrowseButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.ciaCetkCachePathBrowseButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.ciaCetkCachePathBrowseButton.UseVisualStyleBackColor = true;
            this.ciaCetkCachePathBrowseButton.Click += new System.EventHandler(this.ciaCetkCachePathBrowseButton_Click);
            //
            // ciaAddToLibraryCheckBox
            //
            this.ciaAddToLibraryCheckBox.AutoSize = true;
            this.ciaAddToLibraryCheckBox.Location = new System.Drawing.Point(12, 972);
            this.ciaAddToLibraryCheckBox.Name = "ciaAddToLibraryCheckBox";
            this.ciaAddToLibraryCheckBox.Size = new System.Drawing.Size(280, 17);
            this.ciaAddToLibraryCheckBox.TabIndex = 18;
            this.ciaAddToLibraryCheckBox.Text = "Menu: add created CIA to LaunchBox library";
            this.ciaAddToLibraryCheckBox.UseVisualStyleBackColor = true;
            //
            // packagingTadSectionLabel
            //
            this.packagingTadSectionLabel.AutoSize = true;
            this.packagingTadSectionLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold);
            this.packagingTadSectionLabel.Location = new System.Drawing.Point(9, 1006);
            this.packagingTadSectionLabel.Name = "packagingTadSectionLabel";
            this.packagingTadSectionLabel.Size = new System.Drawing.Size(80, 15);
            this.packagingTadSectionLabel.TabIndex = 113;
            this.packagingTadSectionLabel.Text = "DSi (TAD)  ·  Right-click 'Create TAD Package...' menu + shared ticket cache";
            //
            // tadPlatformLabel
            //
            this.tadPlatformLabel.AutoSize = true;
            this.tadPlatformLabel.Location = new System.Drawing.Point(9, 1030);
            this.tadPlatformLabel.Name = "tadPlatformLabel";
            this.tadPlatformLabel.Size = new System.Drawing.Size(180, 13);
            this.tadPlatformLabel.TabIndex = 114;
            this.tadPlatformLabel.Text = "Menu platforms — show 'Create TAD Package...' on these (multi-select):";
            //
            // tadPlatform
            //
            this.tadPlatform.CheckOnClick = true;
            this.tadPlatform.IntegralHeight = false;
            this.tadPlatform.Location = new System.Drawing.Point(12, 1046);
            this.tadPlatform.Name = "tadPlatform";
            this.tadPlatform.Size = new System.Drawing.Size(297, 120);
            this.tadPlatform.TabIndex = 19;
            //
            // tadOutputPathLabel
            //
            this.tadOutputPathLabel.AutoSize = true;
            this.tadOutputPathLabel.Location = new System.Drawing.Point(9, 1270);
            this.tadOutputPathLabel.Name = "tadOutputPathLabel";
            this.tadOutputPathLabel.Size = new System.Drawing.Size(220, 13);
            this.tadOutputPathLabel.TabIndex = 115;
            this.tadOutputPathLabel.Text = "Menu output folder (empty = next to source):";
            //
            // tadOutputPath
            //
            this.tadOutputPath.Location = new System.Drawing.Point(12, 1238);
            this.tadOutputPath.MaxLength = 260;
            this.tadOutputPath.Name = "tadOutputPath";
            this.tadOutputPath.Size = new System.Drawing.Size(497, 20);
            this.tadOutputPath.TabIndex = 20;
            //
            // tadOutputPathBrowseButton
            //
            this.tadOutputPathBrowseButton.Image = global::ArchiveCacheManager.Resources.folder_horizontal_open;
            this.tadOutputPathBrowseButton.Location = new System.Drawing.Point(515, 1235);
            this.tadOutputPathBrowseButton.Name = "tadOutputPathBrowseButton";
            this.tadOutputPathBrowseButton.Size = new System.Drawing.Size(97, 28);
            this.tadOutputPathBrowseButton.TabIndex = 21;
            this.tadOutputPathBrowseButton.Text = "Browse...";
            this.tadOutputPathBrowseButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.tadOutputPathBrowseButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.tadOutputPathBrowseButton.UseVisualStyleBackColor = true;
            this.tadOutputPathBrowseButton.Click += new System.EventHandler(this.tadOutputPathBrowseButton_Click);
            //
            // tadCetkCachePathLabel
            //
            this.tadCetkCachePathLabel.AutoSize = true;
            this.tadCetkCachePathLabel.Location = new System.Drawing.Point(9, 1270);
            this.tadCetkCachePathLabel.Name = "tadCetkCachePathLabel";
            this.tadCetkCachePathLabel.Size = new System.Drawing.Size(220, 13);
            this.tadCetkCachePathLabel.TabIndex = 116;
            this.tadCetkCachePathLabel.Text = "Ticket cache folder — shared with auto-cache (empty = no cache):";
            //
            // tadCetkCachePath
            //
            this.tadCetkCachePath.Location = new System.Drawing.Point(12, 1238);
            this.tadCetkCachePath.MaxLength = 260;
            this.tadCetkCachePath.Name = "tadCetkCachePath";
            this.tadCetkCachePath.Size = new System.Drawing.Size(497, 20);
            this.tadCetkCachePath.TabIndex = 22;
            //
            // tadCetkCachePathBrowseButton
            //
            this.tadCetkCachePathBrowseButton.Image = global::ArchiveCacheManager.Resources.folder_horizontal_open;
            this.tadCetkCachePathBrowseButton.Location = new System.Drawing.Point(515, 1235);
            this.tadCetkCachePathBrowseButton.Name = "tadCetkCachePathBrowseButton";
            this.tadCetkCachePathBrowseButton.Size = new System.Drawing.Size(97, 28);
            this.tadCetkCachePathBrowseButton.TabIndex = 23;
            this.tadCetkCachePathBrowseButton.Text = "Browse...";
            this.tadCetkCachePathBrowseButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.tadCetkCachePathBrowseButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.tadCetkCachePathBrowseButton.UseVisualStyleBackColor = true;
            this.tadCetkCachePathBrowseButton.Click += new System.EventHandler(this.tadCetkCachePathBrowseButton_Click);
            //
            // tadAddToLibraryCheckBox
            //
            this.tadAddToLibraryCheckBox.AutoSize = true;
            this.tadAddToLibraryCheckBox.Location = new System.Drawing.Point(12, 1270);
            this.tadAddToLibraryCheckBox.Name = "tadAddToLibraryCheckBox";
            this.tadAddToLibraryCheckBox.Size = new System.Drawing.Size(280, 17);
            this.tadAddToLibraryCheckBox.TabIndex = 24;
            this.tadAddToLibraryCheckBox.Text = "Menu: add created TAD to LaunchBox library";
            this.tadAddToLibraryCheckBox.UseVisualStyleBackColor = true;
            //
            // packagingPs3PkgSectionLabel
            //
            this.packagingPs3PkgSectionLabel.AutoSize = true;
            this.packagingPs3PkgSectionLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold);
            this.packagingPs3PkgSectionLabel.Location = new System.Drawing.Point(9, 142);
            this.packagingPs3PkgSectionLabel.Name = "packagingPs3PkgSectionLabel";
            this.packagingPs3PkgSectionLabel.Size = new System.Drawing.Size(80, 15);
            this.packagingPs3PkgSectionLabel.TabIndex = 117;
            this.packagingPs3PkgSectionLabel.Text = "PS3 (.pkg)  ·  On-launch packager (.pkg → RPCS3-bootable folder, RAP → exdata)";
            //
            // ps3PkgPlatformLabel
            //
            this.ps3PkgPlatformLabel.AutoSize = true;
            this.ps3PkgPlatformLabel.Location = new System.Drawing.Point(9, 166);
            this.ps3PkgPlatformLabel.Name = "ps3PkgPlatformLabel";
            this.ps3PkgPlatformLabel.Size = new System.Drawing.Size(220, 13);
            this.ps3PkgPlatformLabel.TabIndex = 118;
            this.ps3PkgPlatformLabel.Text = "Menu platforms — show 'Create PS3 Package...' / 'Fetch PS3 Updates...' on these (multi-select):";
            //
            // ps3PkgPlatform
            //
            this.ps3PkgPlatform.CheckOnClick = true;
            this.ps3PkgPlatform.IntegralHeight = false;
            this.ps3PkgPlatform.Location = new System.Drawing.Point(12, 182);
            this.ps3PkgPlatform.Name = "ps3PkgPlatform";
            this.ps3PkgPlatform.Size = new System.Drawing.Size(297, 120);
            this.ps3PkgPlatform.TabIndex = 25;
            //
            // ps3PkgOutputPathLabel
            //
            this.ps3PkgOutputPathLabel.AutoSize = true;
            this.ps3PkgOutputPathLabel.Location = new System.Drawing.Point(9, 310);
            this.ps3PkgOutputPathLabel.Name = "ps3PkgOutputPathLabel";
            this.ps3PkgOutputPathLabel.Size = new System.Drawing.Size(220, 13);
            this.ps3PkgOutputPathLabel.TabIndex = 119;
            this.ps3PkgOutputPathLabel.Text = "Menu output folder (empty = next to source):";
            //
            // ps3PkgOutputPath
            //
            this.ps3PkgOutputPath.Location = new System.Drawing.Point(12, 326);
            this.ps3PkgOutputPath.MaxLength = 260;
            this.ps3PkgOutputPath.Name = "ps3PkgOutputPath";
            this.ps3PkgOutputPath.Size = new System.Drawing.Size(497, 20);
            this.ps3PkgOutputPath.TabIndex = 26;
            //
            // ps3PkgOutputPathBrowseButton
            //
            this.ps3PkgOutputPathBrowseButton.Image = global::ArchiveCacheManager.Resources.folder_horizontal_open;
            this.ps3PkgOutputPathBrowseButton.Location = new System.Drawing.Point(515, 323);
            this.ps3PkgOutputPathBrowseButton.Name = "ps3PkgOutputPathBrowseButton";
            this.ps3PkgOutputPathBrowseButton.Size = new System.Drawing.Size(97, 28);
            this.ps3PkgOutputPathBrowseButton.TabIndex = 27;
            this.ps3PkgOutputPathBrowseButton.Text = "Browse...";
            this.ps3PkgOutputPathBrowseButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.ps3PkgOutputPathBrowseButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.ps3PkgOutputPathBrowseButton.UseVisualStyleBackColor = true;
            this.ps3PkgOutputPathBrowseButton.Click += new System.EventHandler(this.ps3PkgOutputPathBrowseButton_Click);
            //
            // ps3RpcsExdataPathLabel
            //
            this.ps3RpcsExdataPathLabel.AutoSize = true;
            this.ps3RpcsExdataPathLabel.Location = new System.Drawing.Point(9, 358);
            this.ps3RpcsExdataPathLabel.Name = "ps3RpcsExdataPathLabel";
            this.ps3RpcsExdataPathLabel.Size = new System.Drawing.Size(220, 13);
            this.ps3RpcsExdataPathLabel.TabIndex = 120;
            this.ps3RpcsExdataPathLabel.Text = "RPCS3 exdata folder for RAP licences (dev_hdd0/home/00000001/exdata/) — empty = cache-only:";
            //
            // ps3RpcsExdataPath
            //
            this.ps3RpcsExdataPath.Location = new System.Drawing.Point(12, 374);
            this.ps3RpcsExdataPath.MaxLength = 260;
            this.ps3RpcsExdataPath.Name = "ps3RpcsExdataPath";
            this.ps3RpcsExdataPath.Size = new System.Drawing.Size(497, 20);
            this.ps3RpcsExdataPath.TabIndex = 28;
            //
            // ps3RpcsExdataPathBrowseButton
            //
            this.ps3RpcsExdataPathBrowseButton.Image = global::ArchiveCacheManager.Resources.folder_horizontal_open;
            this.ps3RpcsExdataPathBrowseButton.Location = new System.Drawing.Point(515, 371);
            this.ps3RpcsExdataPathBrowseButton.Name = "ps3RpcsExdataPathBrowseButton";
            this.ps3RpcsExdataPathBrowseButton.Size = new System.Drawing.Size(97, 28);
            this.ps3RpcsExdataPathBrowseButton.TabIndex = 29;
            this.ps3RpcsExdataPathBrowseButton.Text = "Browse...";
            this.ps3RpcsExdataPathBrowseButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.ps3RpcsExdataPathBrowseButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.ps3RpcsExdataPathBrowseButton.UseVisualStyleBackColor = true;
            this.ps3RpcsExdataPathBrowseButton.Click += new System.EventHandler(this.ps3RpcsExdataPathBrowseButton_Click);
            //
            // ps3PkgAddToLibraryCheckBox
            //
            this.ps3PkgAddToLibraryCheckBox.AutoSize = true;
            this.ps3PkgAddToLibraryCheckBox.Location = new System.Drawing.Point(12, 406);
            this.ps3PkgAddToLibraryCheckBox.Name = "ps3PkgAddToLibraryCheckBox";
            this.ps3PkgAddToLibraryCheckBox.Size = new System.Drawing.Size(280, 17);
            this.ps3PkgAddToLibraryCheckBox.TabIndex = 30;
            this.ps3PkgAddToLibraryCheckBox.Text = "Menu: add staged PS3 install to LaunchBox library";
            this.ps3PkgAddToLibraryCheckBox.UseVisualStyleBackColor = true;
            //
            // ps3PkgAutoInstallToRpcs3CheckBox
            //
            this.ps3PkgAutoInstallToRpcs3CheckBox.AutoSize = true;
            this.ps3PkgAutoInstallToRpcs3CheckBox.Location = new System.Drawing.Point(12, 427);
            this.ps3PkgAutoInstallToRpcs3CheckBox.Name = "ps3PkgAutoInstallToRpcs3CheckBox";
            this.ps3PkgAutoInstallToRpcs3CheckBox.Size = new System.Drawing.Size(420, 17);
            this.ps3PkgAutoInstallToRpcs3CheckBox.TabIndex = 31;
            this.ps3PkgAutoInstallToRpcs3CheckBox.Text = "On-launch: also robocopy cache → RPCS3 dev_hdd0/game/<TID>/ so NPDRM eboots find their rap";
            this.ps3PkgAutoInstallToRpcs3CheckBox.UseVisualStyleBackColor = true;
            //
            // ps3AutoInstallDlcsCheckBox
            //
            this.ps3AutoInstallDlcsCheckBox.AutoSize = true;
            this.ps3AutoInstallDlcsCheckBox.Location = new System.Drawing.Point(12, 448);
            this.ps3AutoInstallDlcsCheckBox.Name = "ps3AutoInstallDlcsCheckBox";
            this.ps3AutoInstallDlcsCheckBox.Size = new System.Drawing.Size(420, 17);
            this.ps3AutoInstallDlcsCheckBox.TabIndex = 32;
            this.ps3AutoInstallDlcsCheckBox.Text = "On-launch: install DLCs from local PKG index (see right-click \"Index Local PS3 PKG Folders…\")";
            this.ps3AutoInstallDlcsCheckBox.UseVisualStyleBackColor = true;
            //
            // pspAutoInstallDlcsCheckBox
            //
            this.pspAutoInstallDlcsCheckBox.AutoSize = true;
            this.pspAutoInstallDlcsCheckBox.Location = new System.Drawing.Point(12, 877);
            this.pspAutoInstallDlcsCheckBox.Name = "pspAutoInstallDlcsCheckBox";
            this.pspAutoInstallDlcsCheckBox.Size = new System.Drawing.Size(420, 17);
            this.pspAutoInstallDlcsCheckBox.TabIndex = 50;
            this.pspAutoInstallDlcsCheckBox.Text = "On-launch: install DLCs from local PKG index (see right-click \"Index Local PSP PKG Folders…\")";
            this.pspAutoInstallDlcsCheckBox.UseVisualStyleBackColor = true;
            //
            // psvAutoInstallDlcsCheckBox
            //
            this.psvAutoInstallDlcsCheckBox.AutoSize = true;
            this.psvAutoInstallDlcsCheckBox.Location = new System.Drawing.Point(12, 1279);
            this.psvAutoInstallDlcsCheckBox.Name = "psvAutoInstallDlcsCheckBox";
            this.psvAutoInstallDlcsCheckBox.Size = new System.Drawing.Size(420, 17);
            this.psvAutoInstallDlcsCheckBox.TabIndex = 70;
            this.psvAutoInstallDlcsCheckBox.Text = "On-launch: install DLCs from local PKG index (see right-click \"Index Local PSV PKG Folders…\")";
            this.psvAutoInstallDlcsCheckBox.UseVisualStyleBackColor = true;
            //
            // ps3LocalPkgFoldersButton
            //
            this.ps3LocalPkgFoldersButton.Location = new System.Drawing.Point(440, 444);
            this.ps3LocalPkgFoldersButton.Name = "ps3LocalPkgFoldersButton";
            this.ps3LocalPkgFoldersButton.Size = new System.Drawing.Size(290, 25);
            this.ps3LocalPkgFoldersButton.TabIndex = 33;
            this.ps3LocalPkgFoldersButton.Text = "Manage PS3 Local PKG Folders…";
            this.ps3LocalPkgFoldersButton.UseVisualStyleBackColor = true;
            //
            // pspLocalPkgFoldersButton
            //
            this.pspLocalPkgFoldersButton.Location = new System.Drawing.Point(440, 873);
            this.pspLocalPkgFoldersButton.Name = "pspLocalPkgFoldersButton";
            this.pspLocalPkgFoldersButton.Size = new System.Drawing.Size(290, 25);
            this.pspLocalPkgFoldersButton.TabIndex = 51;
            this.pspLocalPkgFoldersButton.Text = "Manage PSP Local PKG Folders…";
            this.pspLocalPkgFoldersButton.UseVisualStyleBackColor = true;
            //
            // psvLocalPkgFoldersButton
            //
            this.psvLocalPkgFoldersButton.Location = new System.Drawing.Point(440, 1275);
            this.psvLocalPkgFoldersButton.Name = "psvLocalPkgFoldersButton";
            this.psvLocalPkgFoldersButton.Size = new System.Drawing.Size(290, 25);
            this.psvLocalPkgFoldersButton.TabIndex = 71;
            this.psvLocalPkgFoldersButton.Text = "Manage PSV Local PKG Folders…";
            this.psvLocalPkgFoldersButton.UseVisualStyleBackColor = true;
            //
            // wiiuLocalRomFoldersButton
            //
            this.wiiuLocalRomFoldersButton.Location = new System.Drawing.Point(440, 250);
            this.wiiuLocalRomFoldersButton.Name = "wiiuLocalRomFoldersButton";
            this.wiiuLocalRomFoldersButton.Size = new System.Drawing.Size(290, 25);
            this.wiiuLocalRomFoldersButton.TabIndex = 200;
            this.wiiuLocalRomFoldersButton.Text = "Manage Wii U Local ROM Folders…";
            this.wiiuLocalRomFoldersButton.UseVisualStyleBackColor = true;
            //
            // ctr3dsLocalRomFoldersButton
            //
            this.ctr3dsLocalRomFoldersButton.Location = new System.Drawing.Point(440, 1618);
            this.ctr3dsLocalRomFoldersButton.Name = "ctr3dsLocalRomFoldersButton";
            this.ctr3dsLocalRomFoldersButton.Size = new System.Drawing.Size(290, 25);
            this.ctr3dsLocalRomFoldersButton.TabIndex = 201;
            this.ctr3dsLocalRomFoldersButton.Text = "Manage 3DS Local ROM Folders…";
            this.ctr3dsLocalRomFoldersButton.UseVisualStyleBackColor = true;
            //
            // packagingPspPkgSectionLabel
            //
            this.packagingPspPkgSectionLabel.AutoSize = true;
            this.packagingPspPkgSectionLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold);
            this.packagingPspPkgSectionLabel.Location = new System.Drawing.Point(9, 590);
            this.packagingPspPkgSectionLabel.Name = "packagingPspPkgSectionLabel";
            this.packagingPspPkgSectionLabel.Size = new System.Drawing.Size(80, 15);
            this.packagingPspPkgSectionLabel.TabIndex = 121;
            this.packagingPspPkgSectionLabel.Text = "PSP (.pkg)  ·  On-launch packager (.pkg → PPSSPP memstick layout, RAP → PSP/LICENSE)";
            //
            // pspPkgPlatformLabel
            //
            this.pspPkgPlatformLabel.AutoSize = true;
            this.pspPkgPlatformLabel.Location = new System.Drawing.Point(9, 614);
            this.pspPkgPlatformLabel.Name = "pspPkgPlatformLabel";
            this.pspPkgPlatformLabel.Size = new System.Drawing.Size(220, 13);
            this.pspPkgPlatformLabel.TabIndex = 122;
            this.pspPkgPlatformLabel.Text = "Menu platforms — show 'Create PSP Package...' on these (multi-select):";
            //
            // pspPkgPlatform
            //
            this.pspPkgPlatform.CheckOnClick = true;
            this.pspPkgPlatform.IntegralHeight = false;
            this.pspPkgPlatform.Location = new System.Drawing.Point(12, 630);
            this.pspPkgPlatform.Name = "pspPkgPlatform";
            this.pspPkgPlatform.Size = new System.Drawing.Size(297, 120);
            this.pspPkgPlatform.TabIndex = 31;
            //
            // pspPkgOutputPathLabel
            //
            this.pspPkgOutputPathLabel.AutoSize = true;
            this.pspPkgOutputPathLabel.Location = new System.Drawing.Point(9, 758);
            this.pspPkgOutputPathLabel.Name = "pspPkgOutputPathLabel";
            this.pspPkgOutputPathLabel.Size = new System.Drawing.Size(220, 13);
            this.pspPkgOutputPathLabel.TabIndex = 123;
            this.pspPkgOutputPathLabel.Text = "Menu output folder (empty = next to source):";
            //
            // pspPkgOutputPath
            //
            this.pspPkgOutputPath.Location = new System.Drawing.Point(12, 774);
            this.pspPkgOutputPath.MaxLength = 260;
            this.pspPkgOutputPath.Name = "pspPkgOutputPath";
            this.pspPkgOutputPath.Size = new System.Drawing.Size(497, 20);
            this.pspPkgOutputPath.TabIndex = 32;
            //
            // pspPkgOutputPathBrowseButton
            //
            this.pspPkgOutputPathBrowseButton.Image = global::ArchiveCacheManager.Resources.folder_horizontal_open;
            this.pspPkgOutputPathBrowseButton.Location = new System.Drawing.Point(515, 771);
            this.pspPkgOutputPathBrowseButton.Name = "pspPkgOutputPathBrowseButton";
            this.pspPkgOutputPathBrowseButton.Size = new System.Drawing.Size(97, 28);
            this.pspPkgOutputPathBrowseButton.TabIndex = 33;
            this.pspPkgOutputPathBrowseButton.Text = "Browse...";
            this.pspPkgOutputPathBrowseButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.pspPkgOutputPathBrowseButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.pspPkgOutputPathBrowseButton.UseVisualStyleBackColor = true;
            this.pspPkgOutputPathBrowseButton.Click += new System.EventHandler(this.pspPkgOutputPathBrowseButton_Click);
            //
            // pspPpssppLicensePathLabel
            //
            this.pspPpssppLicensePathLabel.AutoSize = true;
            this.pspPpssppLicensePathLabel.Location = new System.Drawing.Point(9, 806);
            this.pspPpssppLicensePathLabel.Name = "pspPpssppLicensePathLabel";
            this.pspPpssppLicensePathLabel.Size = new System.Drawing.Size(220, 13);
            this.pspPpssppLicensePathLabel.TabIndex = 124;
            this.pspPpssppLicensePathLabel.Text = "PPSSPP memstick LICENSE folder (PSP/LICENSE/) — empty = cache-only:";
            //
            // pspPpssppLicensePath
            //
            this.pspPpssppLicensePath.Location = new System.Drawing.Point(12, 822);
            this.pspPpssppLicensePath.MaxLength = 260;
            this.pspPpssppLicensePath.Name = "pspPpssppLicensePath";
            this.pspPpssppLicensePath.Size = new System.Drawing.Size(497, 20);
            this.pspPpssppLicensePath.TabIndex = 34;
            //
            // pspPpssppLicensePathBrowseButton
            //
            this.pspPpssppLicensePathBrowseButton.Image = global::ArchiveCacheManager.Resources.folder_horizontal_open;
            this.pspPpssppLicensePathBrowseButton.Location = new System.Drawing.Point(515, 819);
            this.pspPpssppLicensePathBrowseButton.Name = "pspPpssppLicensePathBrowseButton";
            this.pspPpssppLicensePathBrowseButton.Size = new System.Drawing.Size(97, 28);
            this.pspPpssppLicensePathBrowseButton.TabIndex = 35;
            this.pspPpssppLicensePathBrowseButton.Text = "Browse...";
            this.pspPpssppLicensePathBrowseButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.pspPpssppLicensePathBrowseButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.pspPpssppLicensePathBrowseButton.UseVisualStyleBackColor = true;
            this.pspPpssppLicensePathBrowseButton.Click += new System.EventHandler(this.pspPpssppLicensePathBrowseButton_Click);
            //
            // pspPkgAddToLibraryCheckBox
            //
            this.pspPkgAddToLibraryCheckBox.AutoSize = true;
            this.pspPkgAddToLibraryCheckBox.Location = new System.Drawing.Point(12, 854);
            this.pspPkgAddToLibraryCheckBox.Name = "pspPkgAddToLibraryCheckBox";
            this.pspPkgAddToLibraryCheckBox.Size = new System.Drawing.Size(280, 17);
            this.pspPkgAddToLibraryCheckBox.TabIndex = 36;
            this.pspPkgAddToLibraryCheckBox.Text = "Menu: add staged PSP install to LaunchBox library";
            this.pspPkgAddToLibraryCheckBox.UseVisualStyleBackColor = true;
            //
            // packagingPs3AutoUpdateSectionLabel
            //
            this.packagingPs3AutoUpdateSectionLabel.AutoSize = true;
            this.packagingPs3AutoUpdateSectionLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold);
            this.packagingPs3AutoUpdateSectionLabel.Location = new System.Drawing.Point(9, 450);
            this.packagingPs3AutoUpdateSectionLabel.Name = "packagingPs3AutoUpdateSectionLabel";
            this.packagingPs3AutoUpdateSectionLabel.Size = new System.Drawing.Size(80, 15);
            this.packagingPs3AutoUpdateSectionLabel.TabIndex = 125;
            this.packagingPs3AutoUpdateSectionLabel.Text = "PS3 Auto-Update  ·  Pull patches/DLC from Sony PSN on launch (covers both PKG and ISO flows)";
            //
            // ps3AutoInstallUpdatesCheckBox
            //
            this.ps3AutoInstallUpdatesCheckBox.AutoSize = true;
            this.ps3AutoInstallUpdatesCheckBox.Location = new System.Drawing.Point(12, 475);
            this.ps3AutoInstallUpdatesCheckBox.Name = "ps3AutoInstallUpdatesCheckBox";
            this.ps3AutoInstallUpdatesCheckBox.Size = new System.Drawing.Size(280, 17);
            this.ps3AutoInstallUpdatesCheckBox.TabIndex = 37;
            this.ps3AutoInstallUpdatesCheckBox.Text = "Auto-install updates / DLC from Sony on launch (PS3 PKG + decrypted ISO)";
            this.ps3AutoInstallUpdatesCheckBox.UseVisualStyleBackColor = true;
            //
            // ps3UpdateCachePathLabel
            //
            this.ps3UpdateCachePathLabel.AutoSize = true;
            this.ps3UpdateCachePathLabel.Location = new System.Drawing.Point(9, 503);
            this.ps3UpdateCachePathLabel.Name = "ps3UpdateCachePathLabel";
            this.ps3UpdateCachePathLabel.Size = new System.Drawing.Size(220, 13);
            this.ps3UpdateCachePathLabel.TabIndex = 126;
            this.ps3UpdateCachePathLabel.Text = "Update PKG cache folder (persistent, reused across launches) — empty = Plugins\\ArchiveCacheManager\\Ps3UpdateCache:";
            //
            // ps3UpdateCachePath
            //
            this.ps3UpdateCachePath.Location = new System.Drawing.Point(12, 519);
            this.ps3UpdateCachePath.MaxLength = 260;
            this.ps3UpdateCachePath.Name = "ps3UpdateCachePath";
            this.ps3UpdateCachePath.Size = new System.Drawing.Size(497, 20);
            this.ps3UpdateCachePath.TabIndex = 38;
            //
            // ps3UpdateCachePathBrowseButton
            //
            this.ps3UpdateCachePathBrowseButton.Image = global::ArchiveCacheManager.Resources.folder_horizontal_open;
            this.ps3UpdateCachePathBrowseButton.Location = new System.Drawing.Point(515, 516);
            this.ps3UpdateCachePathBrowseButton.Name = "ps3UpdateCachePathBrowseButton";
            this.ps3UpdateCachePathBrowseButton.Size = new System.Drawing.Size(97, 28);
            this.ps3UpdateCachePathBrowseButton.TabIndex = 39;
            this.ps3UpdateCachePathBrowseButton.Text = "Browse...";
            this.ps3UpdateCachePathBrowseButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.ps3UpdateCachePathBrowseButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.ps3UpdateCachePathBrowseButton.UseVisualStyleBackColor = true;
            this.ps3UpdateCachePathBrowseButton.Click += new System.EventHandler(this.ps3UpdateCachePathBrowseButton_Click);
            //
            // ps3UpdateOfflineModeCheckBox
            //
            this.ps3UpdateOfflineModeCheckBox.AutoSize = true;
            this.ps3UpdateOfflineModeCheckBox.Location = new System.Drawing.Point(12, 550);
            this.ps3UpdateOfflineModeCheckBox.Name = "ps3UpdateOfflineModeCheckBox";
            this.ps3UpdateOfflineModeCheckBox.Size = new System.Drawing.Size(280, 17);
            this.ps3UpdateOfflineModeCheckBox.TabIndex = 40;
            this.ps3UpdateOfflineModeCheckBox.Text = "Offline mode — read only from local cache, never query Sony (pre-build the DB with right-click → \"Build PS3 Update DB...\")";
            this.ps3UpdateOfflineModeCheckBox.UseVisualStyleBackColor = true;
            //
            // packagingPspAutoUpdateSectionLabel
            //
            this.packagingPspAutoUpdateSectionLabel.AutoSize = true;
            this.packagingPspAutoUpdateSectionLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold);
            this.packagingPspAutoUpdateSectionLabel.Location = new System.Drawing.Point(9, 900);
            this.packagingPspAutoUpdateSectionLabel.Name = "packagingPspAutoUpdateSectionLabel";
            this.packagingPspAutoUpdateSectionLabel.Size = new System.Drawing.Size(80, 15);
            this.packagingPspAutoUpdateSectionLabel.TabIndex = 127;
            this.packagingPspAutoUpdateSectionLabel.Text = "PSP Auto-Update  ·  Pull patches/DLC from Sony PSN on launch (PSP PKG flow)";
            //
            // pspAutoInstallUpdatesCheckBox
            //
            this.pspAutoInstallUpdatesCheckBox.AutoSize = true;
            this.pspAutoInstallUpdatesCheckBox.Location = new System.Drawing.Point(12, 925);
            this.pspAutoInstallUpdatesCheckBox.Name = "pspAutoInstallUpdatesCheckBox";
            this.pspAutoInstallUpdatesCheckBox.Size = new System.Drawing.Size(280, 17);
            this.pspAutoInstallUpdatesCheckBox.TabIndex = 41;
            this.pspAutoInstallUpdatesCheckBox.Text = "Auto-install updates / DLC from Sony on launch (PSP PKG)";
            this.pspAutoInstallUpdatesCheckBox.UseVisualStyleBackColor = true;
            //
            // pspUpdateCachePathLabel
            //
            this.pspUpdateCachePathLabel.AutoSize = true;
            this.pspUpdateCachePathLabel.Location = new System.Drawing.Point(9, 953);
            this.pspUpdateCachePathLabel.Name = "pspUpdateCachePathLabel";
            this.pspUpdateCachePathLabel.Size = new System.Drawing.Size(220, 13);
            this.pspUpdateCachePathLabel.TabIndex = 128;
            this.pspUpdateCachePathLabel.Text = "Update PKG cache folder (persistent, reused across launches) — empty = Plugins\\ArchiveCacheManager\\PspUpdateCache:";
            //
            // pspUpdateCachePath
            //
            this.pspUpdateCachePath.Location = new System.Drawing.Point(12, 969);
            this.pspUpdateCachePath.MaxLength = 260;
            this.pspUpdateCachePath.Name = "pspUpdateCachePath";
            this.pspUpdateCachePath.Size = new System.Drawing.Size(497, 20);
            this.pspUpdateCachePath.TabIndex = 42;
            //
            // pspUpdateCachePathBrowseButton
            //
            this.pspUpdateCachePathBrowseButton.Image = global::ArchiveCacheManager.Resources.folder_horizontal_open;
            this.pspUpdateCachePathBrowseButton.Location = new System.Drawing.Point(515, 966);
            this.pspUpdateCachePathBrowseButton.Name = "pspUpdateCachePathBrowseButton";
            this.pspUpdateCachePathBrowseButton.Size = new System.Drawing.Size(97, 28);
            this.pspUpdateCachePathBrowseButton.TabIndex = 43;
            this.pspUpdateCachePathBrowseButton.Text = "Browse...";
            this.pspUpdateCachePathBrowseButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.pspUpdateCachePathBrowseButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.pspUpdateCachePathBrowseButton.UseVisualStyleBackColor = true;
            this.pspUpdateCachePathBrowseButton.Click += new System.EventHandler(this.pspUpdateCachePathBrowseButton_Click);
            //
            // pspUpdateOfflineModeCheckBox
            //
            this.pspUpdateOfflineModeCheckBox.AutoSize = true;
            this.pspUpdateOfflineModeCheckBox.Location = new System.Drawing.Point(12, 1000);
            this.pspUpdateOfflineModeCheckBox.Name = "pspUpdateOfflineModeCheckBox";
            this.pspUpdateOfflineModeCheckBox.Size = new System.Drawing.Size(280, 17);
            this.pspUpdateOfflineModeCheckBox.TabIndex = 44;
            this.pspUpdateOfflineModeCheckBox.Text = "Offline mode — read only from local cache, never query Sony (pre-build the DB with right-click → \"Build PSP Update DB...\")";
            this.pspUpdateOfflineModeCheckBox.UseVisualStyleBackColor = true;
            //
            // packagingCtr3dsSectionLabel
            //
            this.packagingCtr3dsSectionLabel.AutoSize = true;
            this.packagingCtr3dsSectionLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold);
            this.packagingCtr3dsSectionLabel.Location = new System.Drawing.Point(9, 1310);
            this.packagingCtr3dsSectionLabel.Name = "packagingCtr3dsSectionLabel";
            this.packagingCtr3dsSectionLabel.Size = new System.Drawing.Size(80, 15);
            this.packagingCtr3dsSectionLabel.TabIndex = 130;
            this.packagingCtr3dsSectionLabel.Text = "3DS (.3ds / .cci)  ·  In-process NCCH decryption (needs user-supplied aes_keys.txt)";
            //
            // ctr3dsPlatformLabel
            //
            this.ctr3dsPlatformLabel.AutoSize = true;
            this.ctr3dsPlatformLabel.Location = new System.Drawing.Point(9, 1334);
            this.ctr3dsPlatformLabel.Name = "ctr3dsPlatformLabel";
            this.ctr3dsPlatformLabel.Size = new System.Drawing.Size(220, 13);
            this.ctr3dsPlatformLabel.TabIndex = 131;
            this.ctr3dsPlatformLabel.Text = "Menu platforms — show 'Decrypt 3DS ROM...' on these (multi-select):";
            //
            // ctr3dsPlatform
            //
            this.ctr3dsPlatform.CheckOnClick = true;
            this.ctr3dsPlatform.IntegralHeight = false;
            this.ctr3dsPlatform.Location = new System.Drawing.Point(12, 1350);
            this.ctr3dsPlatform.Name = "ctr3dsPlatform";
            this.ctr3dsPlatform.Size = new System.Drawing.Size(297, 120);
            this.ctr3dsPlatform.TabIndex = 45;
            //
            // ctr3dsKeysPathLabel
            //
            this.ctr3dsKeysPathLabel.AutoSize = true;
            this.ctr3dsKeysPathLabel.Location = new System.Drawing.Point(9, 1478);
            this.ctr3dsKeysPathLabel.Name = "ctr3dsKeysPathLabel";
            this.ctr3dsKeysPathLabel.Size = new System.Drawing.Size(220, 13);
            this.ctr3dsKeysPathLabel.TabIndex = 132;
            this.ctr3dsKeysPathLabel.Text = "Path to aes_keys.txt (slot0x2CKeyX + optional Secure2/3/4 KeyX) — empty = Extractors\\aes_keys.txt:";
            //
            // ctr3dsKeysPath
            //
            this.ctr3dsKeysPath.Location = new System.Drawing.Point(12, 1494);
            this.ctr3dsKeysPath.MaxLength = 260;
            this.ctr3dsKeysPath.Name = "ctr3dsKeysPath";
            this.ctr3dsKeysPath.Size = new System.Drawing.Size(497, 20);
            this.ctr3dsKeysPath.TabIndex = 46;
            //
            // ctr3dsKeysPathBrowseButton
            //
            this.ctr3dsKeysPathBrowseButton.Image = global::ArchiveCacheManager.Resources.folder_horizontal_open;
            this.ctr3dsKeysPathBrowseButton.Location = new System.Drawing.Point(515, 1491);
            this.ctr3dsKeysPathBrowseButton.Name = "ctr3dsKeysPathBrowseButton";
            this.ctr3dsKeysPathBrowseButton.Size = new System.Drawing.Size(97, 28);
            this.ctr3dsKeysPathBrowseButton.TabIndex = 47;
            this.ctr3dsKeysPathBrowseButton.Text = "Browse...";
            this.ctr3dsKeysPathBrowseButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.ctr3dsKeysPathBrowseButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.ctr3dsKeysPathBrowseButton.UseVisualStyleBackColor = true;
            this.ctr3dsKeysPathBrowseButton.Click += new System.EventHandler(this.ctr3dsKeysPathBrowseButton_Click);
            //
            // ctr3dsSeedDbPathLabel
            //
            this.ctr3dsSeedDbPathLabel.AutoSize = true;
            this.ctr3dsSeedDbPathLabel.Location = new System.Drawing.Point(9, 1526);
            this.ctr3dsSeedDbPathLabel.Name = "ctr3dsSeedDbPathLabel";
            this.ctr3dsSeedDbPathLabel.Size = new System.Drawing.Size(220, 13);
            this.ctr3dsSeedDbPathLabel.TabIndex = 133;
            this.ctr3dsSeedDbPathLabel.Text = "Path to seeddb.bin (per-TitleID seeds for 7.x+ seed-crypto titles) — empty = Extractors\\seeddb.bin:";
            //
            // ctr3dsSeedDbPath
            //
            this.ctr3dsSeedDbPath.Location = new System.Drawing.Point(12, 1542);
            this.ctr3dsSeedDbPath.MaxLength = 260;
            this.ctr3dsSeedDbPath.Name = "ctr3dsSeedDbPath";
            this.ctr3dsSeedDbPath.Size = new System.Drawing.Size(497, 20);
            this.ctr3dsSeedDbPath.TabIndex = 48;
            //
            // ctr3dsSeedDbPathBrowseButton
            //
            this.ctr3dsSeedDbPathBrowseButton.Image = global::ArchiveCacheManager.Resources.folder_horizontal_open;
            this.ctr3dsSeedDbPathBrowseButton.Location = new System.Drawing.Point(515, 1539);
            this.ctr3dsSeedDbPathBrowseButton.Name = "ctr3dsSeedDbPathBrowseButton";
            this.ctr3dsSeedDbPathBrowseButton.Size = new System.Drawing.Size(97, 28);
            this.ctr3dsSeedDbPathBrowseButton.TabIndex = 49;
            this.ctr3dsSeedDbPathBrowseButton.Text = "Browse...";
            this.ctr3dsSeedDbPathBrowseButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.ctr3dsSeedDbPathBrowseButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.ctr3dsSeedDbPathBrowseButton.UseVisualStyleBackColor = true;
            this.ctr3dsSeedDbPathBrowseButton.Click += new System.EventHandler(this.ctr3dsSeedDbPathBrowseButton_Click);
            //
            // ctr3dsOutputPathLabel
            //
            this.ctr3dsOutputPathLabel.AutoSize = true;
            this.ctr3dsOutputPathLabel.Location = new System.Drawing.Point(9, 1574);
            this.ctr3dsOutputPathLabel.Name = "ctr3dsOutputPathLabel";
            this.ctr3dsOutputPathLabel.Size = new System.Drawing.Size(220, 13);
            this.ctr3dsOutputPathLabel.TabIndex = 134;
            this.ctr3dsOutputPathLabel.Text = "Menu output folder (empty = next to source):";
            //
            // ctr3dsOutputPath
            //
            this.ctr3dsOutputPath.Location = new System.Drawing.Point(12, 1590);
            this.ctr3dsOutputPath.MaxLength = 260;
            this.ctr3dsOutputPath.Name = "ctr3dsOutputPath";
            this.ctr3dsOutputPath.Size = new System.Drawing.Size(497, 20);
            this.ctr3dsOutputPath.TabIndex = 50;
            //
            // ctr3dsOutputPathBrowseButton
            //
            this.ctr3dsOutputPathBrowseButton.Image = global::ArchiveCacheManager.Resources.folder_horizontal_open;
            this.ctr3dsOutputPathBrowseButton.Location = new System.Drawing.Point(515, 1587);
            this.ctr3dsOutputPathBrowseButton.Name = "ctr3dsOutputPathBrowseButton";
            this.ctr3dsOutputPathBrowseButton.Size = new System.Drawing.Size(97, 28);
            this.ctr3dsOutputPathBrowseButton.TabIndex = 51;
            this.ctr3dsOutputPathBrowseButton.Text = "Browse...";
            this.ctr3dsOutputPathBrowseButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.ctr3dsOutputPathBrowseButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.ctr3dsOutputPathBrowseButton.UseVisualStyleBackColor = true;
            this.ctr3dsOutputPathBrowseButton.Click += new System.EventHandler(this.ctr3dsOutputPathBrowseButton_Click);
            //
            // ctr3dsAddToLibraryCheckBox
            //
            this.ctr3dsAddToLibraryCheckBox.AutoSize = true;
            this.ctr3dsAddToLibraryCheckBox.Location = new System.Drawing.Point(12, 1622);
            this.ctr3dsAddToLibraryCheckBox.Name = "ctr3dsAddToLibraryCheckBox";
            this.ctr3dsAddToLibraryCheckBox.Size = new System.Drawing.Size(280, 17);
            this.ctr3dsAddToLibraryCheckBox.TabIndex = 52;
            this.ctr3dsAddToLibraryCheckBox.Text = "Menu: add decrypted .3ds to LaunchBox library";
            this.ctr3dsAddToLibraryCheckBox.UseVisualStyleBackColor = true;
            //
            // packagingPsvPkgSectionLabel
            //
            this.packagingPsvPkgSectionLabel.AutoSize = true;
            this.packagingPsvPkgSectionLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold);
            this.packagingPsvPkgSectionLabel.Location = new System.Drawing.Point(9, 1040);
            this.packagingPsvPkgSectionLabel.Name = "packagingPsvPkgSectionLabel";
            this.packagingPsvPkgSectionLabel.Size = new System.Drawing.Size(80, 15);
            this.packagingPsvPkgSectionLabel.TabIndex = 135;
            this.packagingPsvPkgSectionLabel.Text = "PSV (.pkg)  ·  On-launch decrypt (.pkg → Vita3K ux0:app layout) + .vpk packaging";
            //
            // psvPkgPlatformLabel
            //
            this.psvPkgPlatformLabel.AutoSize = true;
            this.psvPkgPlatformLabel.Location = new System.Drawing.Point(9, 1064);
            this.psvPkgPlatformLabel.Name = "psvPkgPlatformLabel";
            this.psvPkgPlatformLabel.Size = new System.Drawing.Size(220, 13);
            this.psvPkgPlatformLabel.TabIndex = 136;
            this.psvPkgPlatformLabel.Text = "Menu platforms — show 'Create PSV VPK...' on these (multi-select):";
            //
            // psvPkgPlatform
            //
            this.psvPkgPlatform.CheckOnClick = true;
            this.psvPkgPlatform.IntegralHeight = false;
            this.psvPkgPlatform.Location = new System.Drawing.Point(12, 1080);
            this.psvPkgPlatform.Name = "psvPkgPlatform";
            this.psvPkgPlatform.Size = new System.Drawing.Size(297, 120);
            this.psvPkgPlatform.TabIndex = 53;
            //
            // psvPkgOutputPathLabel
            //
            this.psvPkgOutputPathLabel.AutoSize = true;
            this.psvPkgOutputPathLabel.Location = new System.Drawing.Point(9, 1208);
            this.psvPkgOutputPathLabel.Name = "psvPkgOutputPathLabel";
            this.psvPkgOutputPathLabel.Size = new System.Drawing.Size(220, 13);
            this.psvPkgOutputPathLabel.TabIndex = 137;
            this.psvPkgOutputPathLabel.Text = "Menu output folder (empty = next to source):";
            //
            // psvPkgOutputPath
            //
            this.psvPkgOutputPath.Location = new System.Drawing.Point(12, 1224);
            this.psvPkgOutputPath.MaxLength = 260;
            this.psvPkgOutputPath.Name = "psvPkgOutputPath";
            this.psvPkgOutputPath.Size = new System.Drawing.Size(497, 20);
            this.psvPkgOutputPath.TabIndex = 54;
            //
            // psvPkgOutputPathBrowseButton
            //
            this.psvPkgOutputPathBrowseButton.Image = global::ArchiveCacheManager.Resources.folder_horizontal_open;
            this.psvPkgOutputPathBrowseButton.Location = new System.Drawing.Point(515, 1221);
            this.psvPkgOutputPathBrowseButton.Name = "psvPkgOutputPathBrowseButton";
            this.psvPkgOutputPathBrowseButton.Size = new System.Drawing.Size(97, 28);
            this.psvPkgOutputPathBrowseButton.TabIndex = 55;
            this.psvPkgOutputPathBrowseButton.Text = "Browse...";
            this.psvPkgOutputPathBrowseButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.psvPkgOutputPathBrowseButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.psvPkgOutputPathBrowseButton.UseVisualStyleBackColor = true;
            this.psvPkgOutputPathBrowseButton.Click += new System.EventHandler(this.psvPkgOutputPathBrowseButton_Click);
            //
            // psvPkgAddToLibraryCheckBox
            //
            this.psvPkgAddToLibraryCheckBox.AutoSize = true;
            this.psvPkgAddToLibraryCheckBox.Location = new System.Drawing.Point(12, 1256);
            this.psvPkgAddToLibraryCheckBox.Name = "psvPkgAddToLibraryCheckBox";
            this.psvPkgAddToLibraryCheckBox.Size = new System.Drawing.Size(280, 17);
            this.psvPkgAddToLibraryCheckBox.TabIndex = 56;
            this.psvPkgAddToLibraryCheckBox.Text = "Menu: add created .vpk to LaunchBox library";
            this.psvPkgAddToLibraryCheckBox.UseVisualStyleBackColor = true;
            //
            // psvVita3kDataPathLabel
            //
            this.psvVita3kDataPathLabel.AutoSize = true;
            this.psvVita3kDataPathLabel.Location = new System.Drawing.Point(9, 1284);
            this.psvVita3kDataPathLabel.Name = "psvVita3kDataPathLabel";
            this.psvVita3kDataPathLabel.Size = new System.Drawing.Size(220, 13);
            this.psvVita3kDataPathLabel.TabIndex = 138;
            this.psvVita3kDataPathLabel.Text = "Vita3K data folder (contains ux0/, ur0/) — empty = %APPDATA%\\Vita3K\\Vita3K. Used to drop auto-decoded .rif licenses.";
            //
            // psvVita3kDataPath
            //
            this.psvVita3kDataPath.Location = new System.Drawing.Point(12, 1300);
            this.psvVita3kDataPath.MaxLength = 260;
            this.psvVita3kDataPath.Name = "psvVita3kDataPath";
            this.psvVita3kDataPath.Size = new System.Drawing.Size(497, 20);
            this.psvVita3kDataPath.TabIndex = 57;
            //
            // psvVita3kDataPathBrowseButton
            //
            this.psvVita3kDataPathBrowseButton.Image = global::ArchiveCacheManager.Resources.folder_horizontal_open;
            this.psvVita3kDataPathBrowseButton.Location = new System.Drawing.Point(515, 1297);
            this.psvVita3kDataPathBrowseButton.Name = "psvVita3kDataPathBrowseButton";
            this.psvVita3kDataPathBrowseButton.Size = new System.Drawing.Size(97, 28);
            this.psvVita3kDataPathBrowseButton.TabIndex = 58;
            this.psvVita3kDataPathBrowseButton.Text = "Browse...";
            this.psvVita3kDataPathBrowseButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.psvVita3kDataPathBrowseButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.psvVita3kDataPathBrowseButton.UseVisualStyleBackColor = true;
            this.psvVita3kDataPathBrowseButton.Click += new System.EventHandler(this.psvVita3kDataPathBrowseButton_Click);
            //
            // npsDbPathLabel
            //
            this.npsDbPathLabel.AutoSize = true;
            this.npsDbPathLabel.Location = new System.Drawing.Point(9, 1332);
            this.npsDbPathLabel.Name = "npsDbPathLabel";
            this.npsDbPathLabel.Size = new System.Drawing.Size(220, 13);
            this.npsDbPathLabel.TabIndex = 139;
            this.npsDbPathLabel.Text = "NoPayStation TSV folder (or single .tsv) — used by PS3 / PSP / PSV PKG flows to auto-stage missing RAP / zRIF licenses:";
            //
            // npsDbPath
            //
            this.npsDbPath.Location = new System.Drawing.Point(12, 1348);
            this.npsDbPath.MaxLength = 260;
            this.npsDbPath.Name = "npsDbPath";
            this.npsDbPath.Size = new System.Drawing.Size(497, 20);
            this.npsDbPath.TabIndex = 59;
            //
            // npsDbPathBrowseButton
            //
            this.npsDbPathBrowseButton.Image = global::ArchiveCacheManager.Resources.folder_horizontal_open;
            this.npsDbPathBrowseButton.Location = new System.Drawing.Point(515, 1345);
            this.npsDbPathBrowseButton.Name = "npsDbPathBrowseButton";
            this.npsDbPathBrowseButton.Size = new System.Drawing.Size(97, 28);
            this.npsDbPathBrowseButton.TabIndex = 60;
            this.npsDbPathBrowseButton.Text = "Browse...";
            this.npsDbPathBrowseButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.npsDbPathBrowseButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.npsDbPathBrowseButton.UseVisualStyleBackColor = true;
            this.npsDbPathBrowseButton.Click += new System.EventHandler(this.npsDbPathBrowseButton_Click);
            //
            // packagingPsvAutoUpdateSectionLabel
            //
            this.packagingPsvAutoUpdateSectionLabel.AutoSize = true;
            this.packagingPsvAutoUpdateSectionLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold);
            this.packagingPsvAutoUpdateSectionLabel.Location = new System.Drawing.Point(9, 1400);
            this.packagingPsvAutoUpdateSectionLabel.Name = "packagingPsvAutoUpdateSectionLabel";
            this.packagingPsvAutoUpdateSectionLabel.Size = new System.Drawing.Size(80, 15);
            this.packagingPsvAutoUpdateSectionLabel.TabIndex = 140;
            this.packagingPsvAutoUpdateSectionLabel.Text = "PSV Auto-Update  ·  Pull patches from Sony PSN on launch (HMAC-signed endpoint, stages into ux0/patch/<TID>/)";
            //
            // psvAutoInstallUpdatesCheckBox
            //
            this.psvAutoInstallUpdatesCheckBox.AutoSize = true;
            this.psvAutoInstallUpdatesCheckBox.Location = new System.Drawing.Point(12, 1425);
            this.psvAutoInstallUpdatesCheckBox.Name = "psvAutoInstallUpdatesCheckBox";
            this.psvAutoInstallUpdatesCheckBox.Size = new System.Drawing.Size(280, 17);
            this.psvAutoInstallUpdatesCheckBox.TabIndex = 61;
            this.psvAutoInstallUpdatesCheckBox.Text = "Auto-install patches from Sony on launch (PSV PKG)";
            this.psvAutoInstallUpdatesCheckBox.UseVisualStyleBackColor = true;
            //
            // psvUpdateCachePathLabel
            //
            this.psvUpdateCachePathLabel.AutoSize = true;
            this.psvUpdateCachePathLabel.Location = new System.Drawing.Point(9, 1453);
            this.psvUpdateCachePathLabel.Name = "psvUpdateCachePathLabel";
            this.psvUpdateCachePathLabel.Size = new System.Drawing.Size(220, 13);
            this.psvUpdateCachePathLabel.TabIndex = 141;
            this.psvUpdateCachePathLabel.Text = "Update PKG cache folder (persistent) — empty = Plugins\\ArchiveCacheManager\\PsvUpdateCache:";
            //
            // psvUpdateCachePath
            //
            this.psvUpdateCachePath.Location = new System.Drawing.Point(12, 1469);
            this.psvUpdateCachePath.MaxLength = 260;
            this.psvUpdateCachePath.Name = "psvUpdateCachePath";
            this.psvUpdateCachePath.Size = new System.Drawing.Size(497, 20);
            this.psvUpdateCachePath.TabIndex = 62;
            //
            // psvUpdateCachePathBrowseButton
            //
            this.psvUpdateCachePathBrowseButton.Image = global::ArchiveCacheManager.Resources.folder_horizontal_open;
            this.psvUpdateCachePathBrowseButton.Location = new System.Drawing.Point(515, 1466);
            this.psvUpdateCachePathBrowseButton.Name = "psvUpdateCachePathBrowseButton";
            this.psvUpdateCachePathBrowseButton.Size = new System.Drawing.Size(97, 28);
            this.psvUpdateCachePathBrowseButton.TabIndex = 63;
            this.psvUpdateCachePathBrowseButton.Text = "Browse...";
            this.psvUpdateCachePathBrowseButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.psvUpdateCachePathBrowseButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.psvUpdateCachePathBrowseButton.UseVisualStyleBackColor = true;
            this.psvUpdateCachePathBrowseButton.Click += new System.EventHandler(this.psvUpdateCachePathBrowseButton_Click);
            //
            // psvUpdateOfflineModeCheckBox
            //
            this.psvUpdateOfflineModeCheckBox.AutoSize = true;
            this.psvUpdateOfflineModeCheckBox.Location = new System.Drawing.Point(12, 1500);
            this.psvUpdateOfflineModeCheckBox.Name = "psvUpdateOfflineModeCheckBox";
            this.psvUpdateOfflineModeCheckBox.Size = new System.Drawing.Size(280, 17);
            this.psvUpdateOfflineModeCheckBox.TabIndex = 64;
            this.psvUpdateOfflineModeCheckBox.Text = "Offline mode — read only from local cache, never query Sony";
            this.psvUpdateOfflineModeCheckBox.UseVisualStyleBackColor = true;
            //
            // label8
            //
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(6, 91);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(522, 52);
            this.label8.TabIndex = 99;
            this.label8.Text = resources.GetString("label8.Text");
            // 
            // bypassPathCheckCheckBox
            // 
            this.bypassPathCheckCheckBox.AutoSize = true;
            this.bypassPathCheckCheckBox.Location = new System.Drawing.Point(7, 62);
            this.bypassPathCheckCheckBox.Name = "bypassPathCheckCheckBox";
            this.bypassPathCheckCheckBox.Size = new System.Drawing.Size(212, 17);
            this.bypassPathCheckCheckBox.TabIndex = 16;
            this.bypassPathCheckCheckBox.Text = "Always Bypass LaunchBox Path Check";
            this.bypassPathCheckCheckBox.UseVisualStyleBackColor = true;
            //
            //
            // packagingPs3IsoSectionLabel
            //
            this.packagingPs3IsoSectionLabel.AutoSize = true;
            this.packagingPs3IsoSectionLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold);
            this.packagingPs3IsoSectionLabel.Location = new System.Drawing.Point(9, 12);
            this.packagingPs3IsoSectionLabel.Name = "packagingPs3IsoSectionLabel";
            this.packagingPs3IsoSectionLabel.Size = new System.Drawing.Size(80, 15);
            this.packagingPs3IsoSectionLabel.TabIndex = 200;
            this.packagingPs3IsoSectionLabel.Text = "PS3 (.iso)  ·  Per-game .dkey lookup for PS3Dec extractor + optional RPCS3 ISO-mount launcher";
            //
            // ps3KeyPathLabel
            //
            this.ps3KeyPathLabel.AutoSize = true;
            this.ps3KeyPathLabel.Location = new System.Drawing.Point(6, 36);
            this.ps3KeyPathLabel.Name = "ps3KeyPathLabel";
            this.ps3KeyPathLabel.Size = new System.Drawing.Size(78, 13);
            this.ps3KeyPathLabel.TabIndex = 20;
            this.ps3KeyPathLabel.Text = "PS3 .dkey folder:";
            //
            // ps3KeyPath
            //
            this.ps3KeyPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ps3KeyPath.Location = new System.Drawing.Point(9, 52);
            this.ps3KeyPath.MaxLength = 260;
            this.ps3KeyPath.Name = "ps3KeyPath";
            this.ps3KeyPath.Size = new System.Drawing.Size(618, 20);
            this.ps3KeyPath.TabIndex = 21;
            //
            // ps3KeyPathBrowseButton
            //
            this.ps3KeyPathBrowseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ps3KeyPathBrowseButton.Image = global::ArchiveCacheManager.Resources.folder_horizontal_open;
            this.ps3KeyPathBrowseButton.Location = new System.Drawing.Point(633, 49);
            this.ps3KeyPathBrowseButton.Name = "ps3KeyPathBrowseButton";
            this.ps3KeyPathBrowseButton.Size = new System.Drawing.Size(97, 28);
            this.ps3KeyPathBrowseButton.TabIndex = 22;
            this.ps3KeyPathBrowseButton.Text = "Browse...";
            this.ps3KeyPathBrowseButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.ps3KeyPathBrowseButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.ps3KeyPathBrowseButton.UseVisualStyleBackColor = true;
            this.ps3KeyPathBrowseButton.Click += new System.EventHandler(this.ps3KeyPathBrowseButton_Click);
            //
            // ps3UseIsoMountLauncherCheckBox
            //
            this.ps3UseIsoMountLauncherCheckBox.AutoSize = true;
            this.ps3UseIsoMountLauncherCheckBox.Location = new System.Drawing.Point(7, 86);
            this.ps3UseIsoMountLauncherCheckBox.Name = "ps3UseIsoMountLauncherCheckBox";
            this.ps3UseIsoMountLauncherCheckBox.Size = new System.Drawing.Size(360, 17);
            this.ps3UseIsoMountLauncherCheckBox.TabIndex = 23;
            this.ps3UseIsoMountLauncherCheckBox.Text = "Mount decrypted PS3 ISOs at launch and run RPCS3 on EBOOT.BIN";
            this.ps3UseIsoMountLauncherCheckBox.UseVisualStyleBackColor = true;
            //
            // label7
            //
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(6, 244);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(489, 13);
            this.label7.TabIndex = 99;
            this.label7.Text = "Be notified of plugin updates when LaunchBox starts. Nothing is automatically dow" +
    "nloaded or installed.";
            //
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.BackColor = System.Drawing.SystemColors.ControlLight;
            this.label3.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(6, 7);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(726, 43);
            this.label3.TabIndex = 99;
            this.label3.Text = "Plugin Settings";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // updateCheckCheckBox
            // 
            this.updateCheckCheckBox.AutoSize = true;
            this.updateCheckCheckBox.Location = new System.Drawing.Point(7, 215);
            this.updateCheckCheckBox.Name = "updateCheckCheckBox";
            this.updateCheckCheckBox.Size = new System.Drawing.Size(172, 17);
            this.updateCheckCheckBox.TabIndex = 17;
            this.updateCheckCheckBox.Text = "Check For Updates On Startup";
            this.updateCheckCheckBox.UseVisualStyleBackColor = true;
            this.updateCheckCheckBox.CheckedChanged += new System.EventHandler(this.multiDiscSupportCheckBox_CheckedChanged);
            // 
            // Emulator
            // 
            dataGridViewCellStyle5.Padding = new System.Windows.Forms.Padding(24, 0, 0, 0);
            this.Emulator.DefaultCellStyle = dataGridViewCellStyle5;
            this.Emulator.Frozen = true;
            this.Emulator.HeaderText = "Emulator";
            this.Emulator.MinimumWidth = 150;
            this.Emulator.Name = "Emulator";
            this.Emulator.ReadOnly = true;
            this.Emulator.Width = 150;
            // 
            // Platform
            // 
            this.Platform.HeaderText = "Platform";
            this.Platform.MinimumWidth = 150;
            this.Platform.Name = "Platform";
            this.Platform.ReadOnly = true;
            this.Platform.Width = 150;
            // 
            // Priority
            // 
            this.Priority.HeaderText = "Priority";
            this.Priority.MinimumWidth = 150;
            this.Priority.Name = "Priority";
            this.Priority.ToolTipText = "Filename \\ extension priority within an archive.";
            this.Priority.Width = 150;
            // 
            // Action
            // 
            this.Action.FillWeight = 50F;
            this.Action.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.Action.HeaderText = "Action";
            this.Action.Items.AddRange(new object[] {
            "Extract",
            "Copy",
            "Extract or Copy"});
            this.Action.Name = "Action";
            this.Action.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.Action.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.Action.ToolTipText = "Extract archive files to the cache, extract archives or copy non-archive files to" +
    " the cache, or just copy files to the cache (even if they\'re archives).";
            this.Action.Width = 62;
            // 
            // LaunchPath
            // 
            this.LaunchPath.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.LaunchPath.HeaderText = "Launch Path";
            this.LaunchPath.Items.AddRange(new object[] {
            "Default",
            "Title",
            "Platform",
            "Emulator"});
            this.LaunchPath.Name = "LaunchPath";
            this.LaunchPath.ToolTipText = "Launch games from a common path within the cache. Useful for RetroArch common set" +
    "tings.";
            this.LaunchPath.Width = 74;
            // 
            // MultiDisc
            // 
            this.MultiDisc.FillWeight = 50F;
            this.MultiDisc.HeaderText = "Multi-Disc";
            this.MultiDisc.Name = "MultiDisc";
            this.MultiDisc.ToolTipText = "Cache all discs in a multi-disc game. Generates and launches an M3U file if suppo" +
    "rted by the emulator.";
            this.MultiDisc.Width = 59;
            // 
            // M3uName
            // 
            this.M3uName.FillWeight = 50F;
            this.M3uName.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.M3uName.HeaderText = "M3U Name";
            this.M3uName.Items.AddRange(new object[] {
            "Game ID",
            "Title + Version",
            "Disc 1 Filename"});
            this.M3uName.MinimumWidth = 100;
            this.M3uName.Name = "M3uName";
            this.M3uName.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.M3uName.ToolTipText = "Name of the M3U file to save. Game ID is LaunchBox\'s default.";
            // 
            // SmartExtract
            // 
            this.SmartExtract.FillWeight = 50F;
            this.SmartExtract.HeaderText = "Smart Extract";
            this.SmartExtract.Name = "SmartExtract";
            this.SmartExtract.ToolTipText = "Only extract a single ROM from an archive if certain conditions are met.";
            this.SmartExtract.Width = 76;
            // 
            // Chdman
            // 
            this.Chdman.HeaderText = "chdman";
            this.Chdman.Name = "Chdman";
            this.Chdman.ToolTipText = "Extract CHD files to CUE+BIN files.";
            this.Chdman.Width = 51;
            // 
            // DolphinTool
            // 
            this.DolphinTool.HeaderText = "DolphinTool";
            this.DolphinTool.Name = "DolphinTool";
            this.DolphinTool.ToolTipText = "Extract RVZ, WIA, and GCZ files to ISO files.";
            this.DolphinTool.Width = 70;
            // 
            // ExtractXiso
            // 
            this.ExtractXiso.HeaderText = "extract-xiso";
            this.ExtractXiso.Name = "ExtractXiso";
            this.ExtractXiso.ToolTipText = "Extract ZIP and ISO files in xiso format.";
            this.ExtractXiso.Width = 66;
            //
            // PS3dec
            //
            this.PS3dec.HeaderText = "PS3Dec";
            this.PS3dec.Name = "PS3dec";
            this.PS3dec.ToolTipText = "Decrypt PS3 ISO files using a .dkey file.";
            this.PS3dec.Width = 55;
            //
            // WiiuCacheOnLaunch
            //
            this.WiiuCacheOnLaunch.HeaderText = "Wii U .wua";
            this.WiiuCacheOnLaunch.Name = "WiiuCacheOnLaunch";
            this.WiiuCacheOnLaunch.ToolTipText = "Build a .wua from a Wii U CDN-dump archive on launch (CDecrypt + zarchive).";
            this.WiiuCacheOnLaunch.Width = 68;
            //
            // CiaCacheOnLaunch
            //
            this.CiaCacheOnLaunch.HeaderText = "3DS .cia";
            this.CiaCacheOnLaunch.Name = "CiaCacheOnLaunch";
            this.CiaCacheOnLaunch.ToolTipText = "Build a .cia from a 3DS CDN-dump archive on launch.";
            this.CiaCacheOnLaunch.Width = 58;
            //
            // WadCacheOnLaunch
            //
            this.WadCacheOnLaunch.HeaderText = "Wii .wad";
            this.WadCacheOnLaunch.Name = "WadCacheOnLaunch";
            this.WadCacheOnLaunch.ToolTipText = "Build .wad file(s) from a Wii CDN-dump archive on launch (Sharpii).";
            this.WadCacheOnLaunch.Width = 60;
            //
            // TadCacheOnLaunch
            //
            this.TadCacheOnLaunch.HeaderText = "DSi .tad";
            this.TadCacheOnLaunch.Name = "TadCacheOnLaunch";
            this.TadCacheOnLaunch.ToolTipText = "Build a .tad from a DSi CDN-dump archive on launch.";
            this.TadCacheOnLaunch.Width = 58;
            //
            // Ps3PkgCacheOnLaunch
            //
            this.Ps3PkgCacheOnLaunch.HeaderText = "PS3 .pkg";
            this.Ps3PkgCacheOnLaunch.Name = "Ps3PkgCacheOnLaunch";
            this.Ps3PkgCacheOnLaunch.ToolTipText = "Decrypt PS3 .pkg files to an RPCS3-bootable folder on launch (in-process AES-CTR).";
            this.Ps3PkgCacheOnLaunch.Width = 62;
            //
            // PspPkgCacheOnLaunch
            //
            this.PspPkgCacheOnLaunch.HeaderText = "PSP .pkg";
            this.PspPkgCacheOnLaunch.Name = "PspPkgCacheOnLaunch";
            this.PspPkgCacheOnLaunch.ToolTipText = "Decrypt PSP .pkg files to a PPSSPP-bootable memstick folder on launch (shared in-process AES-CTR).";
            this.PspPkgCacheOnLaunch.Width = 62;
            //
            // Ctr3dsCacheOnLaunch
            //
            this.Ctr3dsCacheOnLaunch.HeaderText = "3DS .3ds";
            this.Ctr3dsCacheOnLaunch.Name = "Ctr3dsCacheOnLaunch";
            this.Ctr3dsCacheOnLaunch.ToolTipText = "Decrypt 3DS .3ds/.cci files on launch (in-process NCCH decryption, needs aes_keys.txt in Extractors/).";
            this.Ctr3dsCacheOnLaunch.Width = 60;
            //
            // PsvPkgCacheOnLaunch
            //
            this.PsvPkgCacheOnLaunch.HeaderText = "PSV .pkg";
            this.PsvPkgCacheOnLaunch.Name = "PsvPkgCacheOnLaunch";
            this.PsvPkgCacheOnLaunch.ToolTipText = "Decrypt PS Vita .pkg files into a Vita3K-installable folder tree on launch (in-process AES-CTR with derived key).";
            this.PsvPkgCacheOnLaunch.Width = 62;
            //
            // NewConfigWindow
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(944, 613);
            this.Controls.Add(this.flowLayoutPanel1);
            this.Controls.Add(this.treeView1);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.versionLabel);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(960, 480);
            this.Name = "NewConfigWindow";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Archive Cache Manager";
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.tab1CacheSettings.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.cacheDataGridView)).EndInit();
            this.tab2ExtractionSettings.ResumeLayout(false);
            this.tab2ExtractionSettings.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.emulatorPlatformConfigDataGridView)).EndInit();
            this.tab3SmartExtractSettings.ResumeLayout(false);
            this.tab3SmartExtractSettings.PerformLayout();
            this.tab4PluginSettings.ResumeLayout(false);
            this.tabPackagingNintendo.ResumeLayout(false);
            this.tabPackagingNintendo.PerformLayout();
            this.tabPackagingSony.ResumeLayout(false);
            this.tabPackagingSony.PerformLayout();
            this.packagingSubTabs.ResumeLayout(false);
            this.tab5PackagingSettings.ResumeLayout(false);
            this.tab5PackagingSettings.PerformLayout();
            this.tab4PluginSettings.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.Label versionLabel;
        private System.Windows.Forms.Button deletePriorityButton;
        private System.Windows.Forms.Button addPriorityButton;
        private System.Windows.Forms.LinkLabel forumLink;
        private System.Windows.Forms.LinkLabel sourceLink;
        private System.Windows.Forms.LinkLabel pluginLink;
        private System.Windows.Forms.DataGridView emulatorPlatformConfigDataGridView;
        private System.Windows.Forms.DataGridView cacheDataGridView;
        private System.Windows.Forms.Button configureCacheButton;
        private System.Windows.Forms.Button deleteSelectedButton;
        private System.Windows.Forms.Button openInExplorerButton;
        private System.Windows.Forms.Button refreshButton;
        private System.Windows.Forms.RichTextBox cacheSummaryTextBox;
        private System.Windows.Forms.Button deleteAllButton;
        private System.Windows.Forms.CheckBox updateCheckCheckBox;
        private StackPanel tabControl1;
        private System.Windows.Forms.TabPage tab1CacheSettings;
        private System.Windows.Forms.TabPage tab2ExtractionSettings;
        private System.Windows.Forms.TabPage tab4PluginSettings;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TreeView treeView1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label extractionSettingsTipLabel;
        private System.Windows.Forms.TabPage tab3SmartExtractSettings;
        private System.Windows.Forms.TabPage tab5PackagingSettings;
        private System.Windows.Forms.TabControl packagingSubTabs;
        private System.Windows.Forms.TabPage tabPackagingNintendo;
        private System.Windows.Forms.TabPage tabPackagingSony;
        private System.Windows.Forms.Label packagingWiiSectionLabel;
        private System.Windows.Forms.Label wadPlatformLabel;
        private System.Windows.Forms.CheckedListBox wadPlatform;
        private System.Windows.Forms.Label wadOutputPathLabel;
        private System.Windows.Forms.TextBox wadOutputPath;
        private System.Windows.Forms.Button wadOutputPathBrowseButton;
        private System.Windows.Forms.Label wadCetkCachePathLabel;
        private System.Windows.Forms.TextBox wadCetkCachePath;
        private System.Windows.Forms.Button wadCetkCachePathBrowseButton;
        private System.Windows.Forms.CheckBox wadAddToLibraryCheckBox;
        private System.Windows.Forms.Label packagingWiiuSectionLabel;
        private System.Windows.Forms.Label wiiuPlatformLabel;
        private System.Windows.Forms.CheckedListBox wiiuPlatform;
        private System.Windows.Forms.Label wiiuOutputPathLabel;
        private System.Windows.Forms.TextBox wiiuOutputPath;
        private System.Windows.Forms.Button wiiuOutputPathBrowseButton;
        private System.Windows.Forms.Label wiiuCommonKeyLabel;
        private System.Windows.Forms.TextBox wiiuCommonKey;
        private System.Windows.Forms.Label wiiuTitleKeyPasswordLabel;
        private System.Windows.Forms.TextBox wiiuTitleKeyPassword;
        private System.Windows.Forms.CheckBox wiiuAddToLibraryCheckBox;
        private System.Windows.Forms.Label packagingCiaSectionLabel;
        private System.Windows.Forms.Label ciaPlatformLabel;
        private System.Windows.Forms.CheckedListBox ciaPlatform;
        private System.Windows.Forms.Label ciaOutputPathLabel;
        private System.Windows.Forms.TextBox ciaOutputPath;
        private System.Windows.Forms.Button ciaOutputPathBrowseButton;
        private System.Windows.Forms.Label ciaCetkCachePathLabel;
        private System.Windows.Forms.TextBox ciaCetkCachePath;
        private System.Windows.Forms.Button ciaCetkCachePathBrowseButton;
        private System.Windows.Forms.CheckBox ciaAddToLibraryCheckBox;
        private System.Windows.Forms.Label packagingTadSectionLabel;
        private System.Windows.Forms.Label tadPlatformLabel;
        private System.Windows.Forms.CheckedListBox tadPlatform;
        private System.Windows.Forms.Label tadOutputPathLabel;
        private System.Windows.Forms.TextBox tadOutputPath;
        private System.Windows.Forms.Button tadOutputPathBrowseButton;
        private System.Windows.Forms.Label tadCetkCachePathLabel;
        private System.Windows.Forms.TextBox tadCetkCachePath;
        private System.Windows.Forms.Button tadCetkCachePathBrowseButton;
        private System.Windows.Forms.CheckBox tadAddToLibraryCheckBox;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox standaloneExtensions;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox metadataExtensions;
        private System.Windows.Forms.Label cachePathLabel;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.CheckBox bypassPathCheckCheckBox;
        private System.Windows.Forms.Label ps3KeyPathLabel;
        private System.Windows.Forms.TextBox ps3KeyPath;
        private System.Windows.Forms.Button ps3KeyPathBrowseButton;
        private System.Windows.Forms.CheckBox ps3UseIsoMountLauncherCheckBox;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.DataGridViewTextBoxColumn ArchivePath;
        private System.Windows.Forms.DataGridViewTextBoxColumn Archive;
        private System.Windows.Forms.DataGridViewTextBoxColumn ArchivePlatform;
        private System.Windows.Forms.DataGridViewTextBoxColumn ArchiveSize;
        private System.Windows.Forms.DataGridViewCheckBoxColumn Keep;
        private System.Windows.Forms.DataGridViewTextBoxColumn Emulator;
        private System.Windows.Forms.DataGridViewTextBoxColumn Platform;
        private System.Windows.Forms.DataGridViewTextBoxColumn Priority;
        private System.Windows.Forms.DataGridViewComboBoxColumn Action;
        private System.Windows.Forms.DataGridViewComboBoxColumn LaunchPath;
        private System.Windows.Forms.DataGridViewCheckBoxColumn MultiDisc;
        private System.Windows.Forms.DataGridViewComboBoxColumn M3uName;
        private System.Windows.Forms.DataGridViewCheckBoxColumn SmartExtract;
        private System.Windows.Forms.DataGridViewCheckBoxColumn Chdman;
        private System.Windows.Forms.DataGridViewCheckBoxColumn DolphinTool;
        private System.Windows.Forms.DataGridViewCheckBoxColumn ExtractXiso;
        private System.Windows.Forms.DataGridViewCheckBoxColumn PS3dec;
        private System.Windows.Forms.DataGridViewCheckBoxColumn WiiuCacheOnLaunch;
        private System.Windows.Forms.DataGridViewCheckBoxColumn CiaCacheOnLaunch;
        private System.Windows.Forms.DataGridViewCheckBoxColumn WadCacheOnLaunch;
        private System.Windows.Forms.DataGridViewCheckBoxColumn TadCacheOnLaunch;
        private System.Windows.Forms.DataGridViewCheckBoxColumn Ps3PkgCacheOnLaunch;
        private System.Windows.Forms.Label packagingPs3PkgSectionLabel;
        private System.Windows.Forms.Label packagingPs3IsoSectionLabel;
        private System.Windows.Forms.Label ps3PkgPlatformLabel;
        private System.Windows.Forms.CheckedListBox ps3PkgPlatform;
        private System.Windows.Forms.Label ps3PkgOutputPathLabel;
        private System.Windows.Forms.TextBox ps3PkgOutputPath;
        private System.Windows.Forms.Button ps3PkgOutputPathBrowseButton;
        private System.Windows.Forms.Label ps3RpcsExdataPathLabel;
        private System.Windows.Forms.TextBox ps3RpcsExdataPath;
        private System.Windows.Forms.Button ps3RpcsExdataPathBrowseButton;
        private System.Windows.Forms.CheckBox ps3PkgAddToLibraryCheckBox;
        private System.Windows.Forms.CheckBox ps3PkgAutoInstallToRpcs3CheckBox;
        private System.Windows.Forms.CheckBox ps3AutoInstallDlcsCheckBox;
        private System.Windows.Forms.CheckBox pspAutoInstallDlcsCheckBox;
        private System.Windows.Forms.CheckBox psvAutoInstallDlcsCheckBox;
        private System.Windows.Forms.Button ps3LocalPkgFoldersButton;
        private System.Windows.Forms.Button pspLocalPkgFoldersButton;
        private System.Windows.Forms.Button psvLocalPkgFoldersButton;
        private System.Windows.Forms.Button wiiuLocalRomFoldersButton;
        private System.Windows.Forms.Button ctr3dsLocalRomFoldersButton;
        private System.Windows.Forms.DataGridViewCheckBoxColumn PspPkgCacheOnLaunch;
        private System.Windows.Forms.Label packagingPspPkgSectionLabel;
        private System.Windows.Forms.Label pspPkgPlatformLabel;
        private System.Windows.Forms.CheckedListBox pspPkgPlatform;
        private System.Windows.Forms.Label pspPkgOutputPathLabel;
        private System.Windows.Forms.TextBox pspPkgOutputPath;
        private System.Windows.Forms.Button pspPkgOutputPathBrowseButton;
        private System.Windows.Forms.Label pspPpssppLicensePathLabel;
        private System.Windows.Forms.TextBox pspPpssppLicensePath;
        private System.Windows.Forms.Button pspPpssppLicensePathBrowseButton;
        private System.Windows.Forms.CheckBox pspPkgAddToLibraryCheckBox;
        private System.Windows.Forms.Label packagingPs3AutoUpdateSectionLabel;
        private System.Windows.Forms.CheckBox ps3AutoInstallUpdatesCheckBox;
        private System.Windows.Forms.Label ps3UpdateCachePathLabel;
        private System.Windows.Forms.TextBox ps3UpdateCachePath;
        private System.Windows.Forms.Button ps3UpdateCachePathBrowseButton;
        private System.Windows.Forms.CheckBox ps3UpdateOfflineModeCheckBox;
        private System.Windows.Forms.Label packagingPspAutoUpdateSectionLabel;
        private System.Windows.Forms.CheckBox pspAutoInstallUpdatesCheckBox;
        private System.Windows.Forms.Label pspUpdateCachePathLabel;
        private System.Windows.Forms.TextBox pspUpdateCachePath;
        private System.Windows.Forms.Button pspUpdateCachePathBrowseButton;
        private System.Windows.Forms.CheckBox pspUpdateOfflineModeCheckBox;
        private System.Windows.Forms.DataGridViewCheckBoxColumn Ctr3dsCacheOnLaunch;
        private System.Windows.Forms.Label packagingCtr3dsSectionLabel;
        private System.Windows.Forms.Label ctr3dsPlatformLabel;
        private System.Windows.Forms.CheckedListBox ctr3dsPlatform;
        private System.Windows.Forms.Label ctr3dsKeysPathLabel;
        private System.Windows.Forms.TextBox ctr3dsKeysPath;
        private System.Windows.Forms.Button ctr3dsKeysPathBrowseButton;
        private System.Windows.Forms.Label ctr3dsSeedDbPathLabel;
        private System.Windows.Forms.TextBox ctr3dsSeedDbPath;
        private System.Windows.Forms.Button ctr3dsSeedDbPathBrowseButton;
        private System.Windows.Forms.Label ctr3dsOutputPathLabel;
        private System.Windows.Forms.TextBox ctr3dsOutputPath;
        private System.Windows.Forms.Button ctr3dsOutputPathBrowseButton;
        private System.Windows.Forms.CheckBox ctr3dsAddToLibraryCheckBox;
        private System.Windows.Forms.DataGridViewCheckBoxColumn PsvPkgCacheOnLaunch;
        private System.Windows.Forms.Label packagingPsvPkgSectionLabel;
        private System.Windows.Forms.Label psvPkgPlatformLabel;
        private System.Windows.Forms.CheckedListBox psvPkgPlatform;
        private System.Windows.Forms.Label psvPkgOutputPathLabel;
        private System.Windows.Forms.TextBox psvPkgOutputPath;
        private System.Windows.Forms.Button psvPkgOutputPathBrowseButton;
        private System.Windows.Forms.CheckBox psvPkgAddToLibraryCheckBox;
        private System.Windows.Forms.Label psvVita3kDataPathLabel;
        private System.Windows.Forms.TextBox psvVita3kDataPath;
        private System.Windows.Forms.Button psvVita3kDataPathBrowseButton;
        private System.Windows.Forms.Label npsDbPathLabel;
        private System.Windows.Forms.TextBox npsDbPath;
        private System.Windows.Forms.Button npsDbPathBrowseButton;
        private System.Windows.Forms.Label packagingPsvAutoUpdateSectionLabel;
        private System.Windows.Forms.CheckBox psvAutoInstallUpdatesCheckBox;
        private System.Windows.Forms.Label psvUpdateCachePathLabel;
        private System.Windows.Forms.TextBox psvUpdateCachePath;
        private System.Windows.Forms.Button psvUpdateCachePathBrowseButton;
        private System.Windows.Forms.CheckBox psvUpdateOfflineModeCheckBox;
        private System.Windows.Forms.Label wiiuCemuKeysPathLabel;
        private System.Windows.Forms.TextBox wiiuCemuKeysPath;
        private System.Windows.Forms.Button wiiuCemuKeysPathBrowseButton;
    }
}