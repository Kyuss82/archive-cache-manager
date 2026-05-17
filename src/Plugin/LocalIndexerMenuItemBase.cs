/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Shared base class for the five right-click "Index Local … Folders…" menu
 * items (PS3 / PSP / PSV / Wii U / 3DS). Each subclass declares only the
 * three things that actually vary between platforms:
 *   • Caption          — the menu label LaunchBox displays
 *   • Platform         — `LocalPkgPlatform.*` used when opening the indexer
 *   • IsValidPlatform  — Config.Matches* predicate gating visibility
 *
 * Before v2.72 this was 5 separate ~50-line files containing identical
 * IGameMenuItemPlugin scaffolding plus a one-line difference. The Wii U /
 * 3DS variants additionally used hardcoded `g.Platform.IndexOf("Wii U")`
 * predicates instead of the Config.MatchesXxxPlatform pattern used by the
 * Sony three — that inconsistency is fixed here.
 */
using System;
using System.Drawing;
using System.Windows.Forms;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;

namespace ArchiveCacheManager
{
    internal abstract class LocalIndexerMenuItemBase : IGameMenuItemPlugin
    {
        protected abstract LocalPkgPlatform Platform { get; }
        protected abstract bool IsValidPlatform(string platform);
        protected abstract string PlatformLabel { get; }   // "PS3 PKG", "Wii U ROM", …

        public bool SupportsMultipleGames => false;
        public string Caption => string.Format("Index Local {0} Folders...", PlatformLabel);
        public Image IconImage => Resources.icon16x16;
        public bool ShowInLaunchBox => true;
        public bool ShowInBigBox => false;

        public bool GetIsValidForGame(IGame selectedGame) =>
            selectedGame != null && !string.IsNullOrEmpty(selectedGame.Platform) && IsValidPlatform(selectedGame.Platform);

        public bool GetIsValidForGames(IGame[] selectedGames) => false;

        public void OnSelected(IGame[] selectedGames) { }

        public void OnSelected(IGame selectedGame)
        {
            try
            {
                using (var window = new LocalPkgIndexerWindow(Platform)) window.ShowDialog();
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString(), Logger.LogLevel.Exception);
                UserInterface.ErrorDialog(string.Format("Unexpected error opening the local {0} indexer. See log for details.", PlatformLabel));
            }
        }
    }
}
