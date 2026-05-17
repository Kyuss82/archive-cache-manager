/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Right-click "Create PSV VPK..." menu — one-off conversion of an encrypted PSV
 * .pkg (or .zip/.7z/.rar containing one) into a .vpk archive (zip of the
 * decrypted file tree) that Vita3K can install via "Install firmware/.vpk".
 * Gated by Config.PsvPkgPlatform.
 */
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;

namespace ArchiveCacheManager
{
    class CreatePsvVpkMenuItem : IGameMenuItemPlugin
    {
        public bool SupportsMultipleGames => false;
        public string Caption => "Create PSV VPK...";
        public Image IconImage => Resources.icon16x16;
        public bool ShowInLaunchBox => true;
        public bool ShowInBigBox => false;

        public bool GetIsValidForGame(IGame selectedGame) =>
            selectedGame != null && Config.MatchesPsvPkgPlatform(selectedGame.Platform);

        public bool GetIsValidForGames(IGame[] selectedGames) => false;

        public void OnSelected(IGame[] selectedGames) { }

        public void OnSelected(IGame selectedGame)
        {
            string archivePath = !string.IsNullOrWhiteSpace(selectedGame.ApplicationPath)
                ? PathUtils.GetAbsolutePath(selectedGame.ApplicationPath)
                : null;

            if (string.IsNullOrWhiteSpace(archivePath) || !File.Exists(archivePath))
            {
                UserInterface.ErrorDialog("The selected game has no valid Application Path or the archive file is missing.");
                return;
            }

            string outputDir = !string.IsNullOrWhiteSpace(Config.PsvPkgOutputPath)
                ? PathUtils.GetAbsolutePath(Config.PsvPkgOutputPath)
                : Path.GetDirectoryName(archivePath);
            string baseName = Path.GetFileNameWithoutExtension(archivePath);

            try
            {
                Cursor.Current = Cursors.WaitCursor;
                var result = PsvPkgStaging.BuildFromGameArchive(archivePath, outputDir, baseName, packageAsVpk: true);
                Cursor.Current = Cursors.Default;

                if (result.Success && Config.PsvPkgAddToLibrary && !string.IsNullOrEmpty(result.VpkPath))
                {
                    AddVpkToLibrary(selectedGame, result.VpkPath);
                }

                if (result.Success)
                {
                    MessageBox.Show(
                        string.Format("PSV VPK created.\r\n\r\nVPK: {0}\r\nTitle ID: {1}\r\nFiles: {2}, dirs: {3}",
                            result.VpkPath, result.TitleId, result.FilesWritten, result.DirectoriesCreated),
                        "Create PSV VPK",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    UserInterface.ErrorDialog(string.Format("PSV VPK creation failed.\r\n\r\n{0}", result.ErrorMessage ?? "<no detail>"));
                }
            }
            catch (Exception ex)
            {
                Cursor.Current = Cursors.Default;
                Logger.Log(ex.ToString(), Logger.LogLevel.Exception);
                UserInterface.ErrorDialog("Unexpected error during PSV VPK creation. See log for details.");
            }
        }

        private static void AddVpkToLibrary(IGame sourceGame, string vpkPath)
        {
            try
            {
                var newGame = PluginHelper.DataManager.AddNewGame(sourceGame.Title);
                newGame.ApplicationPath = vpkPath;
                newGame.Platform = sourceGame.Platform;
                newGame.SortTitle = sourceGame.Title;
                newGame.Source = "Archive Cache Manager (PSV VPK)";
                PluginHelper.DataManager.Save();
                if (!PluginHelper.StateManager.IsBigBox)
                {
                    PluginHelper.LaunchBoxMainViewModel.RefreshData();
                }
            }
            catch (Exception ex)
            {
                Logger.Log(string.Format("CreatePsvVpkMenuItem: AddToLibrary failed: {0}", ex.Message));
            }
        }
    }
}
