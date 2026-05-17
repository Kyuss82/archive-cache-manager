/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Right-click "Purge Update/DLC Library Entries..." entry. Visible on any game
 * whose platform matches one of the packaging platforms (Sony PS3/PSP/PSV or
 * Nintendo Wii U/3DS) — opens LibraryPurgeWindow, which itself operates on the
 * entire LaunchBox library, not just the right-clicked game.
 */
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Interop;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;

namespace ArchiveCacheManager
{
    class PurgeUpdateDlcMenuItem : IGameMenuItemPlugin
    {
        public bool SupportsMultipleGames => false;
        public string Caption => "Purge Update/DLC Library Entries...";
        public Image IconImage => Resources.icon16x16;
        public bool ShowInLaunchBox => true;
        public bool ShowInBigBox => false;

        public bool GetIsValidForGame(IGame selectedGame)
        {
            if (selectedGame == null || string.IsNullOrWhiteSpace(selectedGame.Platform)) return false;
            string p = selectedGame.Platform;
            return Config.MatchesPs3PkgPlatform(p)
                || Config.MatchesPspPkgPlatform(p)
                || Config.MatchesPsvPkgPlatform(p)
                || Config.MatchesWiiuPlatform(p)
                || Config.MatchesCiaPlatform(p)
                || Config.MatchesCtr3dsPlatform(p);
        }

        public bool GetIsValidForGames(IGame[] selectedGames) => false;

        public void OnSelected(IGame selectedGame)
        {
            try
            {
                using (var window = new LibraryPurgeWindow())
                {
                    var parent = new NativeWindow();
                    parent.AssignHandle(new WindowInteropHelper(System.Windows.Application.Current.MainWindow).Handle);
                    window.ShowDialog(parent);

                    if (window.RefreshLaunchBox && !PluginHelper.StateManager.IsBigBox)
                        PluginHelper.LaunchBoxMainViewModel.RefreshData();
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString(), Logger.LogLevel.Exception);
                UserInterface.ErrorDialog("Unexpected error opening the library purge window. See log for details.");
            }
        }

        public void OnSelected(IGame[] selectedGames) { }
    }
}
