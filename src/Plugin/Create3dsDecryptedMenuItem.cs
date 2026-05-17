/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Right-click "Decrypt 3DS ROM..." menu — one-off conversion of an encrypted
 * .3ds / .cci (or a .zip/.7z/.rar containing one) into a decrypted .3ds saved
 * to Ctr3dsOutputPath (or next to the source archive when empty). Gated by
 * Config.Ctr3dsPlatform.
 */
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;

namespace ArchiveCacheManager
{
    class Create3dsDecryptedMenuItem : IGameMenuItemPlugin
    {
        public bool SupportsMultipleGames => false;
        public string Caption => "Decrypt 3DS ROM...";
        public Image IconImage => Resources.icon16x16;
        public bool ShowInLaunchBox => true;
        public bool ShowInBigBox => false;

        public bool GetIsValidForGame(IGame selectedGame) =>
            selectedGame != null && Config.MatchesCtr3dsPlatform(selectedGame.Platform);

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

            string outputDir = !string.IsNullOrWhiteSpace(Config.Ctr3dsOutputPath)
                ? PathUtils.GetAbsolutePath(Config.Ctr3dsOutputPath)
                : Path.GetDirectoryName(archivePath);
            string baseName = Path.GetFileNameWithoutExtension(archivePath);

            try
            {
                Cursor.Current = Cursors.WaitCursor;
                var result = Ctr3dsStaging.DecryptFromGameArchive(archivePath, outputDir, baseName);
                Cursor.Current = Cursors.Default;

                if (result.Success && Config.Ctr3dsAddToLibrary)
                {
                    AddToLibrary(selectedGame, result.OutputPath);
                }

                if (result.Success)
                {
                    MessageBox.Show(
                        string.Format("Decrypted 3DS ROM created.\r\n\r\n{0}\r\n\r\n{1} partition(s) decrypted, {2} skipped.",
                            result.OutputPath, result.PartitionsDecrypted, result.PartitionsSkipped),
                        "Decrypt 3DS ROM",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    UserInterface.ErrorDialog(string.Format("3DS decryption failed.\r\n\r\n{0}", result.ErrorMessage ?? "<no detail>"));
                }
            }
            catch (Exception ex)
            {
                Cursor.Current = Cursors.Default;
                Logger.Log(ex.ToString(), Logger.LogLevel.Exception);
                UserInterface.ErrorDialog("Unexpected error during 3DS decryption. See log for details.");
            }
        }

        private static void AddToLibrary(IGame sourceGame, string outputPath)
        {
            try
            {
                var newGame = PluginHelper.DataManager.AddNewGame(sourceGame.Title);
                newGame.ApplicationPath = outputPath;
                newGame.Platform = sourceGame.Platform;
                newGame.SortTitle = sourceGame.Title;
                newGame.Source = "Archive Cache Manager (3DS Decrypt)";

                PluginHelper.DataManager.Save();
                if (!PluginHelper.StateManager.IsBigBox)
                {
                    PluginHelper.LaunchBoxMainViewModel.RefreshData();
                }
            }
            catch (Exception ex)
            {
                Logger.Log(string.Format("Create3dsDecryptedMenuItem: AddToLibrary failed: {0}", ex.Message));
            }
        }
    }
}
