using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;

namespace ArchiveCacheManager
{
    class CreateWiiuPackageMenuItem : IGameMenuItemPlugin
    {
        public bool SupportsMultipleGames => false;
        public string Caption => "Create Wii U Package...";
        public Image IconImage => Resources.icon16x16;
        public bool ShowInLaunchBox => true;
        public bool ShowInBigBox => false;

        public bool GetIsValidForGame(IGame selectedGame) =>
            selectedGame != null && Config.MatchesWiiuPlatform(selectedGame.Platform);

        public bool GetIsValidForGames(IGame[] selectedGames) => false;

        public void OnSelected(IGame[] selectedGames) { }

        public void OnSelected(IGame selectedGame)
        {
            if (!CDecryptInvoker.IsAvailable())
            {
                UserInterface.ErrorDialog(
                    string.Format("CDecrypt.exe not found.\r\n\r\nPlace it in:\r\n{0}",
                        PathUtils.GetExtractorRootPath()));
                return;
            }

            string archivePath = !string.IsNullOrWhiteSpace(selectedGame.ApplicationPath)
                ? PathUtils.GetAbsolutePath(selectedGame.ApplicationPath)
                : null;

            if (string.IsNullOrWhiteSpace(archivePath) || !File.Exists(archivePath))
            {
                UserInterface.ErrorDialog("The selected game has no valid Application Path or the archive file is missing.");
                return;
            }

            string outputBaseDir = ResolveOutputBaseDir(archivePath);
            string baseName = !string.IsNullOrWhiteSpace(selectedGame.ApplicationPath)
                ? Path.GetFileNameWithoutExtension(selectedGame.ApplicationPath)
                : PathUtils.GetValidFilename(selectedGame.Title, "output");

            try
            {
                Cursor.Current = Cursors.WaitCursor;
                var result = WiiuStaging.BuildFromGameArchive(archivePath, outputBaseDir, baseName);

                if (result.Success && Config.WiiuAddToLibrary)
                {
                    AddToLibrary(selectedGame, result);
                }

                Cursor.Current = Cursors.Default;

                if (result.Success)
                {
                    MessageBox.Show(
                        string.Format("Wii U package created:\r\n\r\n{0}", result.OutputDir),
                        "Create Wii U Package",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    UserInterface.ErrorDialog(result.ErrorMessage ?? "Unknown error.");
                }
            }
            catch (Exception ex)
            {
                Cursor.Current = Cursors.Default;
                Logger.Log(ex.ToString(), Logger.LogLevel.Exception);
                UserInterface.ErrorDialog("Unexpected error creating Wii U package. See log for details.");
            }
        }

        private static string ResolveOutputBaseDir(string archivePath)
        {
            string configured = Config.WiiuOutputPath;
            if (!string.IsNullOrWhiteSpace(configured))
            {
                string abs = PathUtils.GetAbsolutePath(configured);
                Directory.CreateDirectory(abs);
                return abs;
            }
            return Path.GetDirectoryName(archivePath);
        }

        private static void AddToLibrary(IGame sourceGame, WiiuBuildResult result)
        {
            if (string.IsNullOrWhiteSpace(result.ApplicationPath) || !File.Exists(result.ApplicationPath))
            {
                Logger.Log(string.Format("Skipping library add: no .rpx located in {0}", result.OutputDir));
                return;
            }

            try
            {
                var newGame = PluginHelper.DataManager.AddNewGame(sourceGame.Title);
                newGame.ApplicationPath = result.ApplicationPath;
                newGame.Platform = sourceGame.Platform;
                newGame.SortTitle = sourceGame.Title;
                newGame.Source = "Archive Cache Manager (Wii U)";

                PluginHelper.DataManager.Save();
                if (!PluginHelper.StateManager.IsBigBox)
                {
                    PluginHelper.LaunchBoxMainViewModel.RefreshData();
                }
            }
            catch (Exception ex)
            {
                Logger.Log(string.Format("Failed to add Wii U package to library: {0}", ex.Message), Logger.LogLevel.Exception);
            }
        }
    }
}
