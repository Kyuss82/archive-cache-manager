/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Right-click "Index Local PSP PKG Folders..." — opens `LocalPkgIndexerWindow`
 * scoped to PSP. All wiring lives in `LocalIndexerMenuItemBase`.
 */
namespace ArchiveCacheManager
{
    class PspLocalPkgIndexerMenuItem : LocalIndexerMenuItemBase
    {
        protected override LocalPkgPlatform Platform => LocalPkgPlatform.Psp;
        protected override string PlatformLabel => "PSP PKG";
        protected override bool IsValidPlatform(string platform) => Config.MatchesPspPkgPlatform(platform);
    }
}
