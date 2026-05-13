using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;

namespace ArchiveCacheManager
{
    class CreateCiaMenuItem : IGameMenuItemPlugin
    {
        public bool SupportsMultipleGames => false;
        public string Caption => "Create CIA Package...";
        public Image IconImage => Resources.icon16x16;
        public bool ShowInLaunchBox => true;
        public bool ShowInBigBox => false;

        public bool GetIsValidForGame(IGame selectedGame) =>
            selectedGame != null && Config.MatchesCiaPlatform(selectedGame.Platform);

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

            string outputBaseDir = ResolveOutputBaseDir(archivePath);
            string baseName = !string.IsNullOrWhiteSpace(selectedGame.ApplicationPath)
                ? Path.GetFileNameWithoutExtension(selectedGame.ApplicationPath)
                : PathUtils.GetValidFilename(selectedGame.Title, "output");

            try
            {
                Cursor.Current = Cursors.WaitCursor;
                var results = CiaStaging.BuildFromGameArchive(archivePath, outputBaseDir, baseName);

                if (Config.CiaAddToLibrary)
                {
                    AddCiasToLibrary(selectedGame, results);
                }

                Cursor.Current = Cursors.Default;

                ReportResults(results);
            }
            catch (Exception ex)
            {
                Cursor.Current = Cursors.Default;
                Logger.Log(ex.ToString(), Logger.LogLevel.Exception);
                UserInterface.ErrorDialog("Unexpected error creating CIA. See log for details.");
            }
        }

        private static void AddCiasToLibrary(IGame sourceGame, List<CiaBuildResult> results)
        {
            var successes = results.Where(r => r.Success && !string.IsNullOrWhiteSpace(r.OutputPath) && File.Exists(r.OutputPath)).ToList();
            if (successes.Count == 0) return;

            bool multiVersion = successes.Count > 1;
            int added = 0;

            foreach (var r in successes)
            {
                try
                {
                    string title = multiVersion
                        ? string.Format("{0} (v{1})", sourceGame.Title, r.TmdVersion)
                        : sourceGame.Title;
                    var newGame = PluginHelper.DataManager.AddNewGame(title);
                    newGame.ApplicationPath = r.OutputPath;
                    newGame.Platform = sourceGame.Platform;
                    newGame.SortTitle = title;
                    newGame.Source = "Archive Cache Manager (CIA)";
                    added++;
                }
                catch (Exception ex)
                {
                    Logger.Log(string.Format("Failed to add CIA to library ({0}): {1}", r.OutputPath, ex.Message), Logger.LogLevel.Exception);
                }
            }

            if (added == 0) return;

            try
            {
                PluginHelper.DataManager.Save();
                if (!PluginHelper.StateManager.IsBigBox)
                {
                    PluginHelper.LaunchBoxMainViewModel.RefreshData();
                }
            }
            catch (Exception ex)
            {
                Logger.Log(string.Format("Failed to save library after adding CIAs: {0}", ex.Message), Logger.LogLevel.Exception);
            }
        }

        private static string ResolveOutputBaseDir(string archivePath)
        {
            string configured = Config.CiaOutputPath;
            if (!string.IsNullOrWhiteSpace(configured))
            {
                string abs = PathUtils.GetAbsolutePath(configured);
                Directory.CreateDirectory(abs);
                return abs;
            }
            return Path.GetDirectoryName(archivePath);
        }

        private static void ReportResults(List<CiaBuildResult> results)
        {
            int successCount = results.Count(r => r.Success);
            int failureCount = results.Count - successCount;

            var sb = new StringBuilder();
            foreach (var r in results)
            {
                if (r.Success)
                {
                    sb.AppendLine(string.Format("OK   {0}", r.OutputPath));
                }
                else
                {
                    string label = string.IsNullOrEmpty(r.TmdSourceName)
                        ? "FAIL"
                        : string.Format("FAIL ({0})", r.TmdSourceName);
                    sb.AppendLine(string.Format("{0}: {1}", label, r.ErrorMessage));
                }
            }

            string message = sb.ToString().TrimEnd();

            if (failureCount == 0)
            {
                MessageBox.Show(
                    string.Format("CIA creation completed.\r\n\r\n{0}", message),
                    "Create CIA Package",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                UserInterface.ErrorDialog(
                    string.Format("CIA creation finished with {0} success(es) and {1} failure(s).\r\n\r\n{2}",
                        successCount, failureCount, message));
            }
        }
    }
}
