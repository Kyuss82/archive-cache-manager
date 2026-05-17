/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Right-click "Index Local Wii ROM Folders..." — opens `LocalPkgIndexerWindow`
 * scoped to Wii. All wiring lives in `LocalIndexerMenuItemBase`. The visibility
 * gate uses `Config.MatchesWadPlatform()` (the same predicate the right-click
 * "Create Wii Package…" / on-launch WadExtractor use), since Wii in this fork
 * is identified by the WAD packager's platform config rather than a separate
 * "Wii game platform" string.
 */
namespace ArchiveCacheManager
{
    class WiiLocalRomIndexerMenuItem : LocalIndexerMenuItemBase
    {
        protected override LocalPkgPlatform Platform => LocalPkgPlatform.Wii;
        protected override string PlatformLabel => "Wii ROM";
        protected override bool IsValidPlatform(string platform) => Config.MatchesWadPlatform(platform);
    }
}
