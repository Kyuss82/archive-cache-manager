/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Right-click "Index Local PSV PKG Folders..." — opens `LocalPkgIndexerWindow`
 * scoped to PSV. All wiring lives in `LocalIndexerMenuItemBase`.
 */
namespace ArchiveCacheManager
{
    class PsvLocalPkgIndexerMenuItem : LocalIndexerMenuItemBase
    {
        protected override LocalPkgPlatform Platform => LocalPkgPlatform.Psv;
        protected override string PlatformLabel => "PSV PKG";
        protected override bool IsValidPlatform(string platform) => Config.MatchesPsvPkgPlatform(platform);
    }
}
