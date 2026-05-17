/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Right-click "Index Local PS3 PKG Folders..." — opens `LocalPkgIndexerWindow`
 * scoped to PS3. All wiring lives in `LocalIndexerMenuItemBase`.
 */
namespace ArchiveCacheManager
{
    class Ps3LocalPkgIndexerMenuItem : LocalIndexerMenuItemBase
    {
        protected override LocalPkgPlatform Platform => LocalPkgPlatform.Ps3;
        protected override string PlatformLabel => "PS3 PKG";
        protected override bool IsValidPlatform(string platform) => Config.MatchesPs3PkgPlatform(platform);
    }
}
