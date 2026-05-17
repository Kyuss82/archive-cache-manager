/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Right-click "Index Local 3DS ROM Folders..." — opens `LocalPkgIndexerWindow`
 * scoped to 3DS. All wiring lives in `LocalIndexerMenuItemBase`. v2.72 switched
 * to `Config.MatchesCtr3dsPlatform()` from the hand-rolled `Platform.IndexOf("3DS")`
 * predicate the original file used, so platform mapping is centralised.
 */
namespace ArchiveCacheManager
{
    class Ctr3dsLocalRomIndexerMenuItem : LocalIndexerMenuItemBase
    {
        protected override LocalPkgPlatform Platform => LocalPkgPlatform.Ctr3ds;
        protected override string PlatformLabel => "3DS ROM";
        protected override bool IsValidPlatform(string platform) => Config.MatchesCtr3dsPlatform(platform);
    }
}
