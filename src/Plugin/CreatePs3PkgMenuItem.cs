/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Right-click "Create PS3 Package..." menu item — one-off conversion of a
 * .pkg-bearing archive (or a bare .pkg) into a decrypted, RPCS3-bootable folder
 * outside the plugin cache. Mirrors CreateWadMenuItem / CreateCiaMenuItem /
 * CreateTadMenuItem; visibility gated by Config.Ps3PkgPlatform.
 */
using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;

namespace ArchiveCacheManager
{
    class CreatePs3PkgMenuItem : IGameMenuItemPlugin
    {
        public bool SupportsMultipleGames => false;
        public string Caption => "Create PS3 Package...";
        public Image IconImage => Resources.icon16x16;
        public bool ShowInLaunchBox => true;
        public bool ShowInBigBox => false;

        public bool GetIsValidForGame(IGame selectedGame) =>
            selectedGame != null && Config.MatchesPs3PkgPlatform(selectedGame.Platform);

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
                var result = Ps3PkgStaging.BuildFromGameArchive(archivePath, outputBaseDir, baseName);

                if (result.Success && Config.Ps3PkgAddToLibrary)
                {
                    AddInstallToLibrary(selectedGame, result);
                }

                Cursor.Current = Cursors.Default;

                ReportResult(result);
            }
            catch (Exception ex)
            {
                Cursor.Current = Cursors.Default;
                Logger.Log(ex.ToString(), Logger.LogLevel.Exception);
                UserInterface.ErrorDialog("Unexpected error creating PS3 package. See log for details.");
            }
        }

        private static void AddInstallToLibrary(IGame sourceGame, Ps3PkgBuildResult result)
        {
            if (string.IsNullOrWhiteSpace(result.EbootPath) || !File.Exists(result.EbootPath))
            {
                Logger.Log("CreatePs3Pkg: AddToLibrary skipped — no EBOOT.BIN was located in the staged install.");
                return;
            }

            try
            {
                var newGame = PluginHelper.DataManager.AddNewGame(sourceGame.Title);
                newGame.ApplicationPath = result.EbootPath;
                newGame.Platform = sourceGame.Platform;
                newGame.SortTitle = sourceGame.Title;
                newGame.Source = "Archive Cache Manager (PS3 PKG)";

                PluginHelper.DataManager.Save();
                if (!PluginHelper.StateManager.IsBigBox)
                {
                    PluginHelper.LaunchBoxMainViewModel.RefreshData();
                }
            }
            catch (Exception ex)
            {
                Logger.Log(string.Format("Failed to add PS3 install to library ({0}): {1}", result.EbootPath, ex.Message), Logger.LogLevel.Exception);
            }
        }

        private static string ResolveOutputBaseDir(string archivePath)
        {
            string configured = Config.Ps3PkgOutputPath;
            if (!string.IsNullOrWhiteSpace(configured))
            {
                string abs = PathUtils.GetAbsolutePath(configured);
                Directory.CreateDirectory(abs);
                return abs;
            }
            return Path.GetDirectoryName(archivePath);
        }

        private static void ReportResult(Ps3PkgBuildResult result)
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrEmpty(result.InstallRoot))
            {
                sb.AppendLine(string.Format("Install root: {0}", result.InstallRoot));
            }
            if (!string.IsNullOrEmpty(result.TitleId))
            {
                sb.AppendLine(string.Format("Title ID: {0}", result.TitleId));
            }
            if (!string.IsNullOrEmpty(result.EbootPath))
            {
                sb.AppendLine(string.Format("EBOOT.BIN: {0}", result.EbootPath));
            }

            if (result.Steps != null && result.Steps.Count > 0)
            {
                sb.AppendLine();
                foreach (var step in result.Steps)
                {
                    if (step.Success)
                    {
                        sb.AppendLine(string.Format("OK   {0} (content_type {1}) — {2} files, {3} dirs",
                            step.SourcePkg, step.ContentType, step.FilesWritten, step.DirectoriesCreated));
                    }
                    else
                    {
                        sb.AppendLine(string.Format("FAIL {0}: {1}", step.SourcePkg, step.ErrorMessage));
                    }
                }
            }

            if (result.Raps != null && result.Raps.Count > 0)
            {
                sb.AppendLine();
                foreach (var rap in result.Raps)
                {
                    string where = rap.ExdataCopyPath != null
                        ? string.Format("staged + copied to {0}", rap.ExdataCopyPath)
                        : string.Format("staged at {0}", rap.StagedRapPath);
                    sb.AppendLine(string.Format("RAP  {0} — {1}", rap.ContentId, where));
                }
            }

            string message = sb.ToString().TrimEnd();

            if (result.Success)
            {
                MessageBox.Show(
                    string.Format("PS3 package created.\r\n\r\n{0}", message),
                    "Create PS3 Package",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                UserInterface.ErrorDialog(
                    string.Format("PS3 package creation failed.\r\n\r\n{0}\r\n\r\n{1}",
                        result.ErrorMessage ?? "<no detail>", message));
            }
        }
    }
}
