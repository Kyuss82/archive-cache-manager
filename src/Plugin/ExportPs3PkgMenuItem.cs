/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Right-click "Export PS3 PKG..." menu — pulls the raw .pkg (+ matching .rap)
 * out of the source archive without decrypting anything, ready for the user to
 * import via RPCS3 → File → Install Packages/Raps. This is the right workflow
 * for NPDRM titles, where our existing decrypted-folder flow doesn't satisfy
 * RPCS3's klicensee lookup (which only kicks in for titles installed under
 * `dev_hdd0/game/<TID>/`). The companion bulk version is `Ps3BulkPkgExportWindow`.
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
    class ExportPs3PkgMenuItem : IGameMenuItemPlugin
    {
        public bool SupportsMultipleGames => false;
        public string Caption => "Export PS3 PKG (for RPCS3 install)...";
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

            string outputDir = !string.IsNullOrWhiteSpace(Config.Ps3PkgOutputPath)
                ? PathUtils.GetAbsolutePath(Config.Ps3PkgOutputPath)
                : Path.GetDirectoryName(archivePath);

            try
            {
                Cursor.Current = Cursors.WaitCursor;
                var result = Ps3PkgRawExporter.ExportFromGameArchive(archivePath, outputDir);
                Cursor.Current = Cursors.Default;

                ReportResult(result);
            }
            catch (Exception ex)
            {
                Cursor.Current = Cursors.Default;
                Logger.Log(ex.ToString(), Logger.LogLevel.Exception);
                UserInterface.ErrorDialog("Unexpected error exporting PS3 PKG. See log for details.");
            }
        }

        private static void ReportResult(Ps3PkgRawExportResult result)
        {
            if (!result.Success)
            {
                UserInterface.ErrorDialog(string.Format("PS3 PKG export failed.\r\n\r\n{0}",
                    result.ErrorMessage ?? "<no detail>"));
                return;
            }

            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(result.TitleId))   sb.AppendLine(string.Format("Title ID:   {0}", result.TitleId));
            if (!string.IsNullOrEmpty(result.ContentId)) sb.AppendLine(string.Format("Content ID: {0}", result.ContentId));
            sb.AppendLine();
            sb.AppendLine(string.Format("PKG file(s) ({0}):", result.PkgPaths.Count));
            foreach (string p in result.PkgPaths) sb.AppendLine("  " + p);
            sb.AppendLine();
            if (result.RapPaths.Count > 0)
            {
                sb.AppendLine(string.Format("RAP file(s) ({0}, source: {1}):", result.RapPaths.Count, result.RapSource ?? "?"));
                foreach (string r in result.RapPaths) sb.AppendLine("  " + r);
            }
            else
            {
                sb.AppendLine("RAP: none — RPCS3 will boot non-NPDRM titles but NPDRM eboots will fail.");
            }
            sb.AppendLine();
            sb.AppendLine("Next step: open RPCS3 → File → Install Packages/Raps and select the .pkg(s) above.");

            MessageBox.Show(sb.ToString().TrimEnd(),
                "Export PS3 PKG",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
