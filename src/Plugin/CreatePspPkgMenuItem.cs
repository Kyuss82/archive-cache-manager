/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Right-click "Create PSP Package..." menu item — one-off conversion of a
 * .pkg-bearing archive (or a bare .pkg) into a decrypted, PPSSPP-bootable
 * memstick folder outside the plugin cache. Mirrors CreatePs3PkgMenuItem;
 * visibility gated by Config.PspPkgPlatform.
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
    class CreatePspPkgMenuItem : IGameMenuItemPlugin
    {
        public bool SupportsMultipleGames => false;
        public string Caption => "Create PSP Package...";
        public Image IconImage => Resources.icon16x16;
        public bool ShowInLaunchBox => true;
        public bool ShowInBigBox => false;

        public bool GetIsValidForGame(IGame selectedGame) =>
            selectedGame != null && Config.MatchesPspPkgPlatform(selectedGame.Platform);

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
                var result = PspPkgStaging.BuildFromGameArchive(archivePath, outputBaseDir, baseName);

                if (result.Success && Config.PspPkgAddToLibrary)
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
                UserInterface.ErrorDialog("Unexpected error creating PSP package. See log for details.");
            }
        }

        private static void AddInstallToLibrary(IGame sourceGame, PspPkgBuildResult result)
        {
            if (string.IsNullOrWhiteSpace(result.EbootPath) || !File.Exists(result.EbootPath))
            {
                Logger.Log("CreatePspPkg: AddToLibrary skipped — no EBOOT.PBP was located in the staged install.");
                return;
            }

            try
            {
                var newGame = PluginHelper.DataManager.AddNewGame(sourceGame.Title);
                newGame.ApplicationPath = result.EbootPath;
                newGame.Platform = sourceGame.Platform;
                newGame.SortTitle = sourceGame.Title;
                newGame.Source = "Archive Cache Manager (PSP PKG)";

                PluginHelper.DataManager.Save();
                if (!PluginHelper.StateManager.IsBigBox)
                {
                    PluginHelper.LaunchBoxMainViewModel.RefreshData();
                }
            }
            catch (Exception ex)
            {
                Logger.Log(string.Format("Failed to add PSP install to library ({0}): {1}", result.EbootPath, ex.Message), Logger.LogLevel.Exception);
            }
        }

        private static string ResolveOutputBaseDir(string archivePath)
        {
            string configured = Config.PspPkgOutputPath;
            if (!string.IsNullOrWhiteSpace(configured))
            {
                string abs = PathUtils.GetAbsolutePath(configured);
                Directory.CreateDirectory(abs);
                return abs;
            }
            return Path.GetDirectoryName(archivePath);
        }

        private static void ReportResult(PspPkgBuildResult result)
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
                sb.AppendLine(string.Format("EBOOT.PBP: {0}", result.EbootPath));
            }

            if (result.Steps != null && result.Steps.Count > 0)
            {
                sb.AppendLine();
                foreach (var step in result.Steps)
                {
                    if (step.Success)
                    {
                        sb.AppendLine(string.Format("OK   {0} (content_type {1}, drm {2}) — {3} files, {4} dirs",
                            step.SourcePkg, step.ContentType, step.DrmType, step.FilesWritten, step.DirectoriesCreated));
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
                    string where = rap.MemstickCopyPath != null
                        ? string.Format("staged + copied to {0}", rap.MemstickCopyPath)
                        : string.Format("staged at {0}", rap.StagedRapPath);
                    sb.AppendLine(string.Format("RAP  {0} — {1}", rap.ContentId, where));
                }
            }

            string message = sb.ToString().TrimEnd();

            if (result.Success)
            {
                MessageBox.Show(
                    string.Format("PSP package created.\r\n\r\n{0}", message),
                    "Create PSP Package",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                UserInterface.ErrorDialog(
                    string.Format("PSP package creation failed.\r\n\r\n{0}\r\n\r\n{1}",
                        result.ErrorMessage ?? "<no detail>", message));
            }
        }
    }
}
