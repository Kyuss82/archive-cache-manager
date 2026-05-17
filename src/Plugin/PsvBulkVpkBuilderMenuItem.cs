/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Right-click "Build PSV VPKs..." — entry point to PsvBulkVpkBuilderWindow.
 * Supports both single-game right-click (window enumerates the full library)
 * and multi-selection right-click (window processes only the highlighted titles).
 */
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;

namespace ArchiveCacheManager
{
    class PsvBulkVpkBuilderMenuItem : IGameMenuItemPlugin
    {
        public bool SupportsMultipleGames => true;
        public string Caption => "Build PSV VPKs...";
        public Image IconImage => Resources.icon16x16;
        public bool ShowInLaunchBox => true;
        public bool ShowInBigBox => false;

        public bool GetIsValidForGame(IGame selectedGame) =>
            selectedGame != null && Config.MatchesPsvPkgPlatform(selectedGame.Platform);

        public bool GetIsValidForGames(IGame[] selectedGames) =>
            selectedGames != null && selectedGames.Length > 0 &&
            selectedGames.All(g => g != null && Config.MatchesPsvPkgPlatform(g.Platform));

        public void OnSelected(IGame[] selectedGames)
        {
            try
            {
                using (var window = new BulkOperationsWindow(BulkOperationTab.PsvVpk, selectedGames))
                {
                    window.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString(), Logger.LogLevel.Exception);
                UserInterface.ErrorDialog("Unexpected error opening the PSV VPK builder. See log for details.");
            }
        }

        public void OnSelected(IGame selectedGame)
        {
            try
            {
                using (var window = new BulkOperationsWindow(BulkOperationTab.PsvVpk))
                {
                    window.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString(), Logger.LogLevel.Exception);
                UserInterface.ErrorDialog("Unexpected error opening the PSV VPK builder. See log for details.");
            }
        }
    }
}
