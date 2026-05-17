/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Right-click "Install 3DS Updates + DLCs..." — opens `Ctr3dsInstallDialog` listing
 * matches from `<plugin>/Ctr3dsLocalCache/local_rom_index.json` for the selected
 * game and letting the user install them via Citra/Azahar/Lime3DS.
 *
 * Unlike the Sony / Wii U side, 3DS auto-install at game launch is impractical
 * because Citra stores titles encrypted with a per-instance key — pre-cooking the
 * NAND tree isn't an option. The user-driven menu is the realistic UX for 3DS.
 */
using System;
using System.Drawing;
using System.Windows.Forms;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;

namespace ArchiveCacheManager
{
    class Ctr3dsInstallUpdatesMenuItem : IGameMenuItemPlugin
    {
        public bool SupportsMultipleGames => false;
        public string Caption => "Install 3DS Updates + DLCs...";
        public Image IconImage => Resources.icon16x16;
        public bool ShowInLaunchBox => true;
        public bool ShowInBigBox => false;

        public bool GetIsValidForGame(IGame selectedGame) =>
            selectedGame != null && !string.IsNullOrEmpty(selectedGame.Platform) &&
            Config.MatchesCtr3dsPlatform(selectedGame.Platform);

        public bool GetIsValidForGames(IGame[] selectedGames) => false;

        public void OnSelected(IGame[] selectedGames) { }

        public void OnSelected(IGame selectedGame)
        {
            try
            {
                using (var window = new Ctr3dsInstallDialog(selectedGame)) window.ShowDialog();
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString(), Logger.LogLevel.Exception);
                UserInterface.ErrorDialog("Unexpected error opening the 3DS install dialog. See log for details.");
            }
        }
    }
}
