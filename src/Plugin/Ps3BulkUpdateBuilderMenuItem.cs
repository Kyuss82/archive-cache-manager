/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Right-click "Build PS3 Update DB..." entry — opens the bulk builder window
 * which queries Sony's titlepatch XMLs (and, optionally, the PKG files themselves)
 * into Ps3UpdateCachePath. Two modes, picked by what the user right-clicked on:
 *   • Single PS3 game → window enumerates the whole library and processes
 *     every PS3 title.
 *   • Multiple highlighted PS3 games → window processes only those, no library
 *     scan, no platform gate (the user already chose).
 */
using System.Drawing;
using System.Linq;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;

namespace ArchiveCacheManager
{
    class Ps3BulkUpdateBuilderMenuItem : IGameMenuItemPlugin
    {
        public bool SupportsMultipleGames => true;
        public string Caption => "Build PS3 Update DB...";
        public Image IconImage => Resources.icon16x16;
        public bool ShowInLaunchBox => true;
        public bool ShowInBigBox => false;

        public bool GetIsValidForGame(IGame selectedGame) =>
            selectedGame != null && Config.MatchesPs3PkgPlatform(selectedGame.Platform);

        public bool GetIsValidForGames(IGame[] selectedGames) =>
            selectedGames != null && selectedGames.Length > 0 &&
            selectedGames.All(g => g != null && Config.MatchesPs3PkgPlatform(g.Platform));

        public void OnSelected(IGame[] selectedGames)
        {
            using (var window = new BulkOperationsWindow(BulkOperationTab.Ps3Update, selectedGames))
            {
                window.ShowDialog();
            }
        }

        public void OnSelected(IGame selectedGame)
        {
            using (var window = new BulkOperationsWindow(BulkOperationTab.Ps3Update))
            {
                window.ShowDialog();
            }
        }
    }
}
