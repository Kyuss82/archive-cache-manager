/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Per-platform parameters for the Sony update fetcher / installer. Sony's PSN
 * update endpoint (a0.ww.np.dl.playstation.net/tpl/np/<TITLE_ID>/<TITLE_ID>-ver.xml)
 * is identical for PS3 and PSP — what differs is the cache folder, the offline
 * flag, and the path-transform applied when staging extracted files. Bundle
 * those three knobs into a struct so callers don't have to construct one when
 * the defaults (PS3 config) are what they want.
 */
using System;

namespace ArchiveCacheManager
{
    public sealed class SonyUpdateContext
    {
        public string Platform;     // "PS3" / "PSP" — informational, used in logs and UI labels
        public string CacheRoot;    // null = fall back to default
        public bool OfflineMode;
        public Func<string, string> PathTransform;  // null = pass paths through unchanged

        /// <summary>Defaults to the PS3 flow — reads Config.Ps3UpdateCachePath / Ps3UpdateOfflineMode.</summary>
        public static SonyUpdateContext ForPs3()
        {
            return new SonyUpdateContext
            {
                Platform = "PS3",
                CacheRoot = Config.Ps3UpdateCachePath,
                OfflineMode = Config.Ps3UpdateOfflineMode,
                PathTransform = null,
            };
        }

        /// <summary>PSP variant — reads Config.PspUpdate* and strips USRDIR/CONTENT/ on extract.</summary>
        public static SonyUpdateContext ForPsp()
        {
            return new SonyUpdateContext
            {
                Platform = "PSP",
                CacheRoot = Config.PspUpdateCachePath,
                OfflineMode = Config.PspUpdateOfflineMode,
                PathTransform = PspPkgStaging.PspPkgPathTransform,
            };
        }

        /// <summary>PSV variant — reads Config.PsvUpdate*, no path transform (patches mirror app layout).</summary>
        public static SonyUpdateContext ForPsv()
        {
            return new SonyUpdateContext
            {
                Platform = "PSV",
                CacheRoot = Config.PsvUpdateCachePath,
                OfflineMode = Config.PsvUpdateOfflineMode,
                PathTransform = null,
            };
        }
    }
}
