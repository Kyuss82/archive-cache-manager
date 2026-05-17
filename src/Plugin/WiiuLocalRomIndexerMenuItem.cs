/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Right-click "Index Local Wii U ROM Folders..." — opens `LocalPkgIndexerWindow`
 * scoped to Wii U. All wiring lives in `LocalIndexerMenuItemBase`. v2.72 switched
 * to `Config.MatchesWiiuPlatform()` from the hand-rolled `Platform.IndexOf("Wii U")`
 * predicate the original file used, so platform mapping is centralised.
 */
namespace ArchiveCacheManager
{
    class WiiuLocalRomIndexerMenuItem : LocalIndexerMenuItemBase
    {
        protected override LocalPkgPlatform Platform => LocalPkgPlatform.Wiiu;
        protected override string PlatformLabel => "Wii U ROM";
        protected override bool IsValidPlatform(string platform) => Config.MatchesWiiuPlatform(platform);
    }
}
