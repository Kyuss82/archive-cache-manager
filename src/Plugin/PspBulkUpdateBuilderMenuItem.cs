/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Right-click "Build PSP Update DB..." — opens the bulk builder window with a
 * PSP context (Ps3BulkUpdateBuilderWindow's window class is platform-agnostic
 * via its ctx + platformGate parameters; only the title and config-binding
 * differ between PS3 and PSP). Supports both single-game (entire library) and
 * multi-selection (only the highlighted titles) right-click flows.
 */
using System.Drawing;
using System.Linq;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;

namespace ArchiveCacheManager
{
    class PspBulkUpdateBuilderMenuItem : IGameMenuItemPlugin
    {
        public bool SupportsMultipleGames => true;
        public string Caption => "Build PSP Update DB...";
        public Image IconImage => Resources.icon16x16;
        public bool ShowInLaunchBox => true;
        public bool ShowInBigBox => false;

        public bool GetIsValidForGame(IGame selectedGame) =>
            selectedGame != null && Config.MatchesPspPkgPlatform(selectedGame.Platform);

        public bool GetIsValidForGames(IGame[] selectedGames) =>
            selectedGames != null && selectedGames.Length > 0 &&
            selectedGames.All(g => g != null && Config.MatchesPspPkgPlatform(g.Platform));

        public void OnSelected(IGame[] selectedGames)
        {
            using (var window = new BulkOperationsWindow(BulkOperationTab.PspUpdate, selectedGames))
            {
                window.ShowDialog();
            }
        }

        public void OnSelected(IGame selectedGame)
        {
            using (var window = new BulkOperationsWindow(BulkOperationTab.PspUpdate))
            {
                window.ShowDialog();
            }
        }
    }
}
