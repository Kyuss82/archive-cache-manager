using System;
using System.Collections.Generic;
using System.IO;
using IniParser;
using IniParser.Model;

namespace ArchiveCacheManager
{
    public class Config
    {
        public enum Action
        {
            Extract,
            Copy,
            ExtractCopy
        };

        public enum LaunchPath
        {
            Default,
            Title,
            Platform,
            Emulator
        };

        public enum M3uName
        {
            GameId,
            TitleVersion,
            DiscOneFilename
        };

        private static readonly string configSection = "Archive Cache Manager";
        private static readonly string defaultCachePath = "ArchiveCache";
        private static readonly long defaultCacheSize = 20000;
        private static readonly long defaultMinArchiveSize = 100;
        private static readonly bool defaultMultiDisc = true;
        private static readonly bool defaultUseGameIdAsM3uFilename = true;
        private static readonly bool defaultSmartExtract = true;
        private static readonly string defaultStandaloneExtensions = "gb, gbc, gba, agb, nes, fds, smc, sfc, n64, z64, v64, ndd, md, smd, gen, iso, chd, gg, gcm, 32x, bin";
        private static readonly string defaultMetadataExtensions = "nfo, txt, dat, xml, json";
        private static readonly bool? defaultUpdateCheck = null;
        private static readonly string defaultSkipUpdate = null;
        private static readonly bool defaultBypassPathCheck = false;
        private static readonly string defaultEmulatorPlatform = @"All \ All";
        // Priorities determined by launching zip game from LaunchBox, where zip contains common rom and disc file types.
        // As matches were found, those file types were removed from the zip and the process repeated.
        // LaunchBox's priority list isn't documented anywhere, so this is a best guess. A more exhaustive list might look like:
        // cue, gdi, toc, nrg, ccd, mds, cdr, iso, eboot.bin, bin, img, mdf, chd, pbp
        // where disc metadata / table-of-contents types take priority over disc data types.
        private static readonly string defaultFilenamePriority = @"mds, gdi, cue, eboot.bin, eboot.pbp";

        private static readonly LaunchPath defaultLaunchPath = LaunchPath.Default;
        private static readonly Action defaultAction = Action.Extract;
        private static readonly M3uName defaultM3uName = M3uName.GameId;
        private static readonly bool defaultChdman = false;
        private static readonly bool defaultDolphinTool = false;
        private static readonly bool defaultExtractXiso = false;
        private static readonly bool defaultPS3dec = false;
        private static readonly string defaultPS3KeyPath = @"ThirdParty\PS3key";
        private static readonly bool defaultPs3UseIsoMountLauncher = false;
        // Default OFF: opt-in, because it adds a one-time robocopy mirror to
        // <RPCS3>/dev_hdd0/game/<TID>/ that doubles the on-disk footprint of every
        // PS3 PKG title. The reward is correct NPDRM behaviour for PSN releases
        // (RPCS3 only resolves rap files when the title is installed under its
        // dev_hdd0/game/<TID>/, so a cache-only extract fails with
        // "Cannot read SELF" for NPDRM eboots).
        private static readonly bool defaultPs3PkgAutoInstallToRpcs3 = false;

        // Pipe-separated list of folders that hold already-downloaded update / DLC PKGs
        // for each Sony platform (the user's own offline mirror — eXo updates pack, NPS
        // Browser exports, etc.). LocalPkgIndexer scans them recursively and produces a
        // JSON manifest in <Ps3UpdateCachePath>/local_pkg_index.json that the auto-install
        // path consults before hitting Sony.
        private static readonly string defaultPs3LocalPkgFolders = "";
        private static readonly string defaultPspLocalPkgFolders = "";
        private static readonly string defaultPsvLocalPkgFolders = "";
        private static readonly string defaultWiiuLocalRomFolders  = "";
        private static readonly string defaultCtr3dsLocalRomFolders = "";
        private static readonly string defaultWiiLocalRomFolders   = "";
        private static readonly bool defaultPs3AutoInstallDlcs   = false;
        private static readonly bool defaultWiiuAutoInstallUpdates = false;
        private static readonly bool defaultWiiuAutoInstallDlcs    = false;
        private static readonly bool defaultPspAutoInstallDlcs   = false;
        private static readonly bool defaultPsvAutoInstallDlcs   = false;
        private static readonly string defaultWadPlatform = "Nintendo Wii";
        private static readonly string defaultWadOutputPath = "";
        private static readonly string defaultWadCetkCachePath = "";
        private static readonly string defaultWadTitleKeysPath = "";
        private static readonly bool defaultWadAddToLibrary = false;
        private static readonly string defaultWiiuPlatform = "Nintendo Wii U";
        private static readonly string defaultWiiuOutputPath = "";
        private static readonly bool defaultWiiuAddToLibrary = false;
        private static readonly string defaultWiiuTitleKeyPassword = "nintendo";
        private static readonly string defaultWiiuCommonKey = "";
        private static readonly string defaultWiiuCemuKeysPath = "";
        private static readonly bool defaultWiiuPackAsWua = true;
        private static readonly string defaultCiaPlatform = "Nintendo 3DS";
        private static readonly string defaultCiaOutputPath = "";
        private static readonly string defaultCiaCetkCachePath = "";
        private static readonly string defaultCiaEncTitleKeysPath = "";
        private static readonly string defaultCiaCetkDonorPath = "";
        private static readonly bool defaultCiaAddToLibrary = false;
        private static readonly string defaultTadPlatform = "Nintendo DSi;Nintendo DSiWare";
        private static readonly string defaultTadOutputPath = "";
        private static readonly string defaultTadCetkCachePath = "";
        private static readonly bool defaultTadAddToLibrary = false;
        private static readonly bool defaultWiiuCacheOnLaunch = false;
        private static readonly bool defaultCiaCacheOnLaunch = false;
        private static readonly bool defaultWadCacheOnLaunch = false;
        private static readonly bool defaultTadCacheOnLaunch = false;
        private static readonly bool defaultPs3PkgCacheOnLaunch = false;
        private static readonly string defaultPs3PkgPlatform = "Sony Playstation 3";
        private static readonly string defaultPs3PkgOutputPath = "";
        private static readonly string defaultPs3RpcsExdataPath = "";
        private static readonly bool defaultPs3PkgAddToLibrary = false;
        private static readonly string defaultPs3UpdateOutputPath = "";
        private static readonly bool defaultPs3AutoInstallUpdates = false;
        private static readonly string defaultPs3UpdateCachePath = "";
        private static readonly bool defaultPs3UpdateOfflineMode = false;
        private static readonly string defaultPspUpdateOutputPath = "";
        private static readonly bool defaultPspAutoInstallUpdates = false;
        private static readonly string defaultPspUpdateCachePath = "";
        private static readonly bool defaultPspUpdateOfflineMode = false;
        private static readonly bool defaultCtr3dsCacheOnLaunch = false;
        private static readonly string defaultCtr3dsPlatform = "Nintendo 3DS";
        private static readonly string defaultCtr3dsKeysPath = "";
        private static readonly string defaultCtr3dsSeedDbPath = "";
        private static readonly string defaultCtr3dsOutputPath = "";
        private static readonly bool defaultCtr3dsAddToLibrary = false;
        private static readonly bool defaultPsvPkgCacheOnLaunch = false;
        private static readonly string defaultPsvPkgPlatform = "Sony Playstation Vita";
        private static readonly string defaultPsvPkgOutputPath = "";
        private static readonly bool defaultPsvPkgAddToLibrary = false;
        private static readonly string defaultPsvVita3kDataPath = "";
        private static readonly string defaultNpsDbPath = "";
        private static readonly bool defaultPsvAutoInstallUpdates = false;
        private static readonly string defaultPsvUpdateCachePath = "";
        private static readonly bool defaultPsvUpdateOfflineMode = false;
        private static readonly bool defaultPspPkgCacheOnLaunch = false;
        private static readonly string defaultPspPkgPlatform = "Sony PSP";
        private static readonly string defaultPspPkgOutputPath = "";
        private static readonly string defaultPspPpssppLicensePath = "";
        private static readonly bool defaultPspPkgAddToLibrary = false;

        public class EmulatorPlatformConfig
        {
            public string FilenamePriority;
            public Action Action;
            public LaunchPath LaunchPath;
            public bool MultiDisc;
            public M3uName M3uName;
            public bool SmartExtract;
            public bool Chdman;
            public bool DolphinTool;
            public bool ExtractXiso;
            public bool PS3dec;
            public bool WiiuCacheOnLaunch;
            public bool CiaCacheOnLaunch;
            public bool WadCacheOnLaunch;
            public bool TadCacheOnLaunch;
            public bool Ps3PkgCacheOnLaunch;
            public bool PspPkgCacheOnLaunch;
            public bool Ctr3dsCacheOnLaunch;
            public bool PsvPkgCacheOnLaunch;

            public EmulatorPlatformConfig()
            {
                FilenamePriority = defaultFilenamePriority;
                Action = defaultAction;
                LaunchPath = defaultLaunchPath;
                MultiDisc = defaultMultiDisc;
                M3uName = defaultM3uName;
                SmartExtract = defaultSmartExtract;
                Chdman = defaultChdman;
                DolphinTool = defaultDolphinTool;
                ExtractXiso = defaultExtractXiso;
                PS3dec = defaultPS3dec;
                WiiuCacheOnLaunch = defaultWiiuCacheOnLaunch;
                CiaCacheOnLaunch = defaultCiaCacheOnLaunch;
                WadCacheOnLaunch = defaultWadCacheOnLaunch;
                TadCacheOnLaunch = defaultTadCacheOnLaunch;
                Ps3PkgCacheOnLaunch = defaultPs3PkgCacheOnLaunch;
                PspPkgCacheOnLaunch = defaultPspPkgCacheOnLaunch;
                Ctr3dsCacheOnLaunch = defaultCtr3dsCacheOnLaunch;
                PsvPkgCacheOnLaunch = defaultPsvPkgCacheOnLaunch;
            }
        };

        private static string mCachePath = defaultCachePath;
        private static long mCacheSize = defaultCacheSize;
        private static long mMinArchiveSize = defaultMinArchiveSize;
        private static bool mMultiDiscSupport = defaultMultiDisc;
        private static bool mUseGameIdAsM3uFilename = defaultUseGameIdAsM3uFilename;
        private static bool? mUpdateCheck = defaultUpdateCheck;
        private static string mSkipUpdate = defaultSkipUpdate;
        private static string mStandaloneExtensions = defaultStandaloneExtensions;
        private static string mMetadataExtensions = defaultMetadataExtensions;
        private static bool mBypassPathCheck = defaultBypassPathCheck;
        private static string mPS3KeyPath = defaultPS3KeyPath;
        private static bool mPs3UseIsoMountLauncher = defaultPs3UseIsoMountLauncher;
        private static bool mPs3PkgAutoInstallToRpcs3 = defaultPs3PkgAutoInstallToRpcs3;
        private static string mPs3LocalPkgFolders = defaultPs3LocalPkgFolders;
        private static string mPspLocalPkgFolders = defaultPspLocalPkgFolders;
        private static string mPsvLocalPkgFolders = defaultPsvLocalPkgFolders;
        private static string mWiiuLocalRomFolders  = defaultWiiuLocalRomFolders;
        private static string mCtr3dsLocalRomFolders = defaultCtr3dsLocalRomFolders;
        private static string mWiiLocalRomFolders   = defaultWiiLocalRomFolders;
        private static bool mPs3AutoInstallDlcs = defaultPs3AutoInstallDlcs;
        private static bool mWiiuAutoInstallUpdates = defaultWiiuAutoInstallUpdates;
        private static bool mWiiuAutoInstallDlcs    = defaultWiiuAutoInstallDlcs;
        private static bool mPspAutoInstallDlcs = defaultPspAutoInstallDlcs;
        private static bool mPsvAutoInstallDlcs = defaultPsvAutoInstallDlcs;
        private static string mWadPlatform = defaultWadPlatform;
        private static string mWadOutputPath = defaultWadOutputPath;
        private static string mWadCetkCachePath = defaultWadCetkCachePath;
        private static string mWadTitleKeysPath = defaultWadTitleKeysPath;
        private static bool mWadAddToLibrary = defaultWadAddToLibrary;
        private static string mWiiuPlatform = defaultWiiuPlatform;
        private static string mWiiuOutputPath = defaultWiiuOutputPath;
        private static bool mWiiuAddToLibrary = defaultWiiuAddToLibrary;
        private static string mWiiuTitleKeyPassword = defaultWiiuTitleKeyPassword;
        private static string mWiiuCommonKey = defaultWiiuCommonKey;
        private static string mWiiuCemuKeysPath = defaultWiiuCemuKeysPath;
        private static bool mWiiuPackAsWua = defaultWiiuPackAsWua;
        private static string mCiaPlatform = defaultCiaPlatform;
        private static string mCiaOutputPath = defaultCiaOutputPath;
        private static string mCiaCetkCachePath = defaultCiaCetkCachePath;
        private static string mCiaEncTitleKeysPath = defaultCiaEncTitleKeysPath;
        private static string mCiaCetkDonorPath = defaultCiaCetkDonorPath;
        private static bool mCiaAddToLibrary = defaultCiaAddToLibrary;
        private static string mTadPlatform = defaultTadPlatform;
        private static string mTadOutputPath = defaultTadOutputPath;
        private static string mTadCetkCachePath = defaultTadCetkCachePath;
        private static bool mTadAddToLibrary = defaultTadAddToLibrary;
        private static string mPs3PkgPlatform = defaultPs3PkgPlatform;
        private static string mPs3PkgOutputPath = defaultPs3PkgOutputPath;
        private static string mPs3RpcsExdataPath = defaultPs3RpcsExdataPath;
        private static bool mPs3PkgAddToLibrary = defaultPs3PkgAddToLibrary;
        private static string mPs3UpdateOutputPath = defaultPs3UpdateOutputPath;
        private static bool mPs3AutoInstallUpdates = defaultPs3AutoInstallUpdates;
        private static string mPs3UpdateCachePath = defaultPs3UpdateCachePath;
        private static bool mPs3UpdateOfflineMode = defaultPs3UpdateOfflineMode;
        private static string mPspUpdateOutputPath = defaultPspUpdateOutputPath;
        private static bool mPspAutoInstallUpdates = defaultPspAutoInstallUpdates;
        private static string mPspUpdateCachePath = defaultPspUpdateCachePath;
        private static bool mPspUpdateOfflineMode = defaultPspUpdateOfflineMode;
        private static string mCtr3dsPlatform = defaultCtr3dsPlatform;
        private static string mCtr3dsKeysPath = defaultCtr3dsKeysPath;
        private static string mCtr3dsSeedDbPath = defaultCtr3dsSeedDbPath;
        private static string mCtr3dsOutputPath = defaultCtr3dsOutputPath;
        private static bool mCtr3dsAddToLibrary = defaultCtr3dsAddToLibrary;
        private static string mPsvPkgPlatform = defaultPsvPkgPlatform;
        private static string mPsvPkgOutputPath = defaultPsvPkgOutputPath;
        private static bool mPsvPkgAddToLibrary = defaultPsvPkgAddToLibrary;
        private static string mPsvVita3kDataPath = defaultPsvVita3kDataPath;
        private static string mNpsDbPath = defaultNpsDbPath;
        private static bool mPsvAutoInstallUpdates = defaultPsvAutoInstallUpdates;
        private static string mPsvUpdateCachePath = defaultPsvUpdateCachePath;
        private static bool mPsvUpdateOfflineMode = defaultPsvUpdateOfflineMode;
        private static string mPspPkgPlatform = defaultPspPkgPlatform;
        private static string mPspPkgOutputPath = defaultPspPkgOutputPath;
        private static string mPspPpssppLicensePath = defaultPspPpssppLicensePath;
        private static bool mPspPkgAddToLibrary = defaultPspPkgAddToLibrary;

        private static Dictionary<string, EmulatorPlatformConfig> mEmulatorPlatformConfig;

        /// <summary>
        /// Static constructor which loads config from disk into memory.
        /// </summary>
        static Config()
        {
            SetDefaultConfig();
            Load();
        }

        /// <summary>
        /// Configured cache path, relative to LaunchBox folder or absolute. Default is ArchiveCache.
        /// </summary>
        public static string CachePath
        {
            get => mCachePath;
            set => mCachePath = value;
        }

        /// <summary>
        /// Configured cache size in megabytes. Default is 20000.
        /// </summary>
        public static long CacheSize
        {
            get => mCacheSize;
            set => mCacheSize = value;
        }

        /// <summary>
        /// Configured minimum archive size in megabytes. Default is 100.
        /// </summary>
        public static long MinArchiveSize
        {
            get => mMinArchiveSize;
            set => mMinArchiveSize = value;
        }

        public static bool? UpdateCheck
        {
            get => mUpdateCheck;
            set => mUpdateCheck = value;
        }

        public static string SkipUpdate
        {
            get => mSkipUpdate;
            set => mSkipUpdate = value;
        }

        public static string StandaloneExtensions
        {
            get => mStandaloneExtensions;
            set => mStandaloneExtensions = value;
        }

        public static string MetadataExtensions
        {
            get => mMetadataExtensions;
            set => mMetadataExtensions = value;
        }

        public static bool BypassPathCheck
        {
            get => mBypassPathCheck;
            set => mBypassPathCheck = value;
        }

        public static string PS3KeyPath
        {
            get => mPS3KeyPath;
            set => mPS3KeyPath = value;
        }

        /// <summary>
        /// When true, PS3 ISO files launched with RPCS3 are routed through a PowerShell launcher that
        /// mounts the ISO as a virtual drive, runs RPCS3 on the EBOOT.BIN inside, and dismounts on exit.
        /// </summary>
        public static bool Ps3UseIsoMountLauncher
        {
            get => mPs3UseIsoMountLauncher;
            set => mPs3UseIsoMountLauncher = value;
        }

        public static bool Ps3PkgAutoInstallToRpcs3
        {
            get => mPs3PkgAutoInstallToRpcs3;
            set => mPs3PkgAutoInstallToRpcs3 = value;
        }

        public static string Ps3LocalPkgFolders { get => mPs3LocalPkgFolders; set => mPs3LocalPkgFolders = value ?? string.Empty; }
        public static string PspLocalPkgFolders { get => mPspLocalPkgFolders; set => mPspLocalPkgFolders = value ?? string.Empty; }
        public static string PsvLocalPkgFolders { get => mPsvLocalPkgFolders; set => mPsvLocalPkgFolders = value ?? string.Empty; }
        public static string WiiuLocalRomFolders  { get => mWiiuLocalRomFolders;  set => mWiiuLocalRomFolders  = value ?? string.Empty; }
        public static string Ctr3dsLocalRomFolders { get => mCtr3dsLocalRomFolders; set => mCtr3dsLocalRomFolders = value ?? string.Empty; }
        public static string WiiLocalRomFolders   { get => mWiiLocalRomFolders;   set => mWiiLocalRomFolders   = value ?? string.Empty; }
        public static bool   Ps3AutoInstallDlcs { get => mPs3AutoInstallDlcs; set => mPs3AutoInstallDlcs = value; }

        /// <summary>
        /// When true, every Wii U game launch consults the local mirror index
        /// (`WiiuLocalCache/local_rom_index.json`) and robocopies matching update entries into
        /// Cemu's `mlc01/usr/title/0005000E/&lt;TID-low&gt;/` before the emulator starts. Only
        /// Loadiine-layout sources (folder containing `title.tmd` + `code/content/meta/`) are
        /// auto-installable today; `.wua` / NUS-dump sources are skipped with a log line.
        /// </summary>
        public static bool   WiiuAutoInstallUpdates { get => mWiiuAutoInstallUpdates; set => mWiiuAutoInstallUpdates = value; }

        /// <summary>WiiU DLC counterpart of <see cref="WiiuAutoInstallUpdates"/>; targets `0005000C`.</summary>
        public static bool   WiiuAutoInstallDlcs    { get => mWiiuAutoInstallDlcs;    set => mWiiuAutoInstallDlcs    = value; }
        public static bool   PspAutoInstallDlcs { get => mPspAutoInstallDlcs; set => mPspAutoInstallDlcs = value; }
        public static bool   PsvAutoInstallDlcs { get => mPsvAutoInstallDlcs; set => mPsvAutoInstallDlcs = value; }

        public static string[] ParsePipeList(string csv)
        {
            if (string.IsNullOrWhiteSpace(csv)) return Array.Empty<string>();
            var parts = csv.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            var trimmed = new List<string>();
            foreach (var p in parts)
            {
                string t = p.Trim();
                if (t.Length > 0) trimmed.Add(t);
            }
            return trimmed.ToArray();
        }

        /// <summary>
        /// LaunchBox platform name that enables the "Create Wii WAD..." context menu item.
        /// </summary>
        public static string WadPlatform
        {
            get => mWadPlatform;
            set => mWadPlatform = value;
        }

        /// <summary>
        /// Output folder for generated WAD files. Empty = save next to the source game file.
        /// Relative paths are resolved against the LaunchBox root folder.
        /// </summary>
        public static string WadOutputPath
        {
            get => mWadOutputPath;
            set => mWadOutputPath = value;
        }

        /// <summary>
        /// Folder for caching tickets (cetk) downloaded from NUS, keyed by Title ID.
        /// Empty = no caching (NUS is hit on every build that lacks a ticket).
        /// </summary>
        public static string WadCetkCachePath
        {
            get => mWadCetkCachePath;
            set => mWadCetkCachePath = value;
        }

        /// <summary>
        /// Path to a Wii title-keys database (binary blob with 8-byte titleID + 16-byte
        /// encrypted title key pairs). Used as a last-resort source when the archive has
        /// no cetk and the NUS can't serve one. Empty = auto-detect Extractors/wii-titlekeys.bin.
        /// </summary>
        public static string WadTitleKeysPath
        {
            get => mWadTitleKeysPath;
            set => mWadTitleKeysPath = value;
        }

        /// <summary>
        /// When true, after a WAD is created it is also added as a new game in the LaunchBox library
        /// (Platform = WadPlatform), so the official Dolphin LaunchBox Integration plugin picks it up.
        /// </summary>
        public static bool WadAddToLibrary
        {
            get => mWadAddToLibrary;
            set => mWadAddToLibrary = value;
        }

        /// <summary>LaunchBox platform that enables the "Create Wii U Package..." menu item.</summary>
        public static string WiiuPlatform
        {
            get => mWiiuPlatform;
            set => mWiiuPlatform = value;
        }

        /// <summary>Output base folder for Wii U Loadiine packages. Empty = next to the source archive.</summary>
        public static string WiiuOutputPath
        {
            get => mWiiuOutputPath;
            set => mWiiuOutputPath = value;
        }

        /// <summary>If true, the produced Loadiine .rpx is added to the LaunchBox library.</summary>
        public static bool WiiuAddToLibrary
        {
            get => mWiiuAddToLibrary;
            set => mWiiuAddToLibrary = value;
        }

        /// <summary>Password used by the deterministic title key derivation for Wii U.</summary>
        public static string WiiuTitleKeyPassword
        {
            get => mWiiuTitleKeyPassword;
            set => mWiiuTitleKeyPassword = value;
        }

        /// <summary>Wii U Common Key as 32 hex characters. Required to forge tickets for CDecrypt.</summary>
        public static string WiiuCommonKey
        {
            get => mWiiuCommonKey;
            set => mWiiuCommonKey = value;
        }

        /// <summary>
        /// Path to Cemu's keys.txt. Used as the primary title-key source for the Wii U packager
        /// (no network, no extra files to install). Empty = auto-detect %APPDATA%\Cemu\keys.txt.
        /// </summary>
        public static string WiiuCemuKeysPath
        {
            get => mWiiuCemuKeysPath;
            set => mWiiuCemuKeysPath = value;
        }

        /// <summary>
        /// When true and zarchive.exe is in the Extractors folder, the decrypted Loadiine output is packed
        /// into a single .wua file (Cemu native archive). The intermediate folder is deleted.
        /// </summary>
        public static bool WiiuPackAsWua
        {
            get => mWiiuPackAsWua;
            set => mWiiuPackAsWua = value;
        }

        /// <summary>LaunchBox platform that enables the "Create CIA Package..." menu item.</summary>
        public static string CiaPlatform
        {
            get => mCiaPlatform;
            set => mCiaPlatform = value;
        }

        /// <summary>Output base folder for 3DS CIA packages. Empty = next to the source archive.</summary>
        public static string CiaOutputPath
        {
            get => mCiaOutputPath;
            set => mCiaOutputPath = value;
        }

        /// <summary>Optional folder used to cache cetk files downloaded from the 3DS NUS.</summary>
        public static string CiaCetkCachePath
        {
            get => mCiaCetkCachePath;
            set => mCiaCetkCachePath = value;
        }

        /// <summary>
        /// Path to a 3DS encTitleKeys.bin (binary DB of encrypted title keys, scene-distributed).
        /// When set, the CIA builder uses it as a last-resort source for the title key after NUS
        /// fetch fails. Empty = auto-detect Extractors/encTitleKeys.bin.
        /// </summary>
        public static string CiaEncTitleKeysPath
        {
            get => mCiaEncTitleKeysPath;
            set => mCiaEncTitleKeysPath = value;
        }

        /// <summary>
        /// Path to a "donor" cetk (any real Nintendo-signed 3DS cetk, 2640 bytes). Required to forge
        /// fake-signed tickets when only an encrypted title key is available. Empty = auto-detect
        /// Extractors/cetk-donor.bin.
        /// </summary>
        public static string CiaCetkDonorPath
        {
            get => mCiaCetkDonorPath;
            set => mCiaCetkDonorPath = value;
        }

        /// <summary>If true, the produced .cia is added to the LaunchBox library.</summary>
        public static bool CiaAddToLibrary
        {
            get => mCiaAddToLibrary;
            set => mCiaAddToLibrary = value;
        }

        /// <summary>True if the supplied LaunchBox platform name matches any entry in the Wii (WAD) platform list.</summary>
        public static bool MatchesWadPlatform(string platform) => MatchesPlatformCsv(WadPlatform, platform);

        /// <summary>True if the supplied LaunchBox platform name matches any entry in the Wii U platform list.</summary>
        public static bool MatchesWiiuPlatform(string platform) => MatchesPlatformCsv(WiiuPlatform, platform);

        /// <summary>True if the supplied LaunchBox platform name matches any entry in the 3DS CIA platform list.</summary>
        public static bool MatchesCiaPlatform(string platform) => MatchesPlatformCsv(CiaPlatform, platform);

        /// <summary>LaunchBox platform that enables the "Create TAD Package..." menu item.</summary>
        public static string TadPlatform
        {
            get => mTadPlatform;
            set => mTadPlatform = value;
        }

        /// <summary>Output base folder for DSi TAD packages. Empty = next to the source archive.</summary>
        public static string TadOutputPath
        {
            get => mTadOutputPath;
            set => mTadOutputPath = value;
        }

        /// <summary>Optional folder used to cache cetk files downloaded from the DSi NUS.</summary>
        public static string TadCetkCachePath
        {
            get => mTadCetkCachePath;
            set => mTadCetkCachePath = value;
        }

        /// <summary>If true, the produced .tad is added to the LaunchBox library.</summary>
        public static bool TadAddToLibrary
        {
            get => mTadAddToLibrary;
            set => mTadAddToLibrary = value;
        }

        /// <summary>True if the supplied LaunchBox platform name matches any entry in the DSi TAD platform list.</summary>
        public static bool MatchesTadPlatform(string platform) => MatchesPlatformCsv(TadPlatform, platform);

        /// <summary>LaunchBox platform that enables the PS3 PKG on-launch packager flow.</summary>
        public static string Ps3PkgPlatform
        {
            get => mPs3PkgPlatform;
            set => mPs3PkgPlatform = value;
        }

        /// <summary>Output base folder for staged PS3 PKG installs. Empty = next to the source archive.</summary>
        public static string Ps3PkgOutputPath
        {
            get => mPs3PkgOutputPath;
            set => mPs3PkgOutputPath = value;
        }

        /// <summary>
        /// Path to RPCS3's dev_hdd0/home/00000001/exdata/ folder. When set, PKG-side RAP licence
        /// files are copied here so RPCS3 picks them up automatically. Empty = leave RAPs only
        /// inside the plugin cache (user must copy them manually).
        /// </summary>
        public static string Ps3RpcsExdataPath
        {
            get => mPs3RpcsExdataPath;
            set => mPs3RpcsExdataPath = value;
        }

        /// <summary>If true, the staged install is also added as a new game in the LaunchBox library.</summary>
        public static bool Ps3PkgAddToLibrary
        {
            get => mPs3PkgAddToLibrary;
            set => mPs3PkgAddToLibrary = value;
        }

        /// <summary>True if the supplied LaunchBox platform name matches any entry in the PS3 PKG platform list.</summary>
        public static bool MatchesPs3PkgPlatform(string platform) => MatchesPlatformCsv(Ps3PkgPlatform, platform);

        /// <summary>Default download folder for the "Fetch PS3 Updates..." menu item. Empty = next to the source archive.</summary>
        public static string Ps3UpdateOutputPath
        {
            get => mPs3UpdateOutputPath;
            set => mPs3UpdateOutputPath = value;
        }

        /// <summary>
        /// When true, every PS3 game launch (PKG or decrypted-ISO flow) automatically queries
        /// Sony's update server, downloads any new patch / DLC PKGs into Ps3UpdateCachePath, and
        /// stages them on top of the base install before the emulator runs. Network failures are
        /// non-fatal — the game launches with whatever updates are already on disk.
        /// </summary>
        public static bool Ps3AutoInstallUpdates
        {
            get => mPs3AutoInstallUpdates;
            set => mPs3AutoInstallUpdates = value;
        }

        /// <summary>
        /// Persistent folder for the auto-installer's downloaded PKG cache. Empty falls back to
        /// &lt;Plugins\ArchiveCacheManager\Ps3UpdateCache&gt;. Files are keyed by TITLE_ID + version
        /// + sha1 so they're reused across launches. The same folder doubles as an offline DB:
        /// the bulk builder writes Sony's titlepatch XML alongside each title's PKGs, and the
        /// fetcher prefers that local copy over a network call.
        /// </summary>
        public static string Ps3UpdateCachePath
        {
            get => mPs3UpdateCachePath;
            set => mPs3UpdateCachePath = value;
        }

        /// <summary>
        /// When true, Ps3UpdateFetcher.Query never contacts Sony — only the locally-cached
        /// titlepatch XML under Ps3UpdateCachePath is consulted. Useful for LAN / offline setups
        /// where the user has pre-built the DB with the bulk builder.
        /// </summary>
        public static bool Ps3UpdateOfflineMode
        {
            get => mPs3UpdateOfflineMode;
            set => mPs3UpdateOfflineMode = value;
        }

        /// <summary>Default download folder for the "Fetch PSP Updates..." menu item. Empty = next to the source archive.</summary>
        public static string PspUpdateOutputPath
        {
            get => mPspUpdateOutputPath;
            set => mPspUpdateOutputPath = value;
        }

        /// <summary>
        /// PSP counterpart of Ps3AutoInstallUpdates — auto-fetches PSN update PKGs for the
        /// title being launched and stages them on top of the base install in PSP/GAME/&lt;TITLE_ID&gt;/.
        /// </summary>
        public static bool PspAutoInstallUpdates
        {
            get => mPspAutoInstallUpdates;
            set => mPspAutoInstallUpdates = value;
        }

        /// <summary>
        /// Persistent folder for the PSP auto-installer's downloaded PKG + manifest cache.
        /// Empty falls back to &lt;Plugins\ArchiveCacheManager\PspUpdateCache&gt;.
        /// </summary>
        public static string PspUpdateCachePath
        {
            get => mPspUpdateCachePath;
            set => mPspUpdateCachePath = value;
        }

        /// <summary>PSP counterpart of Ps3UpdateOfflineMode.</summary>
        public static bool PspUpdateOfflineMode
        {
            get => mPspUpdateOfflineMode;
            set => mPspUpdateOfflineMode = value;
        }

        /// <summary>LaunchBox platform that enables the 3DS .3ds/.cci on-launch decryption flow.</summary>
        public static string Ctr3dsPlatform
        {
            get => mCtr3dsPlatform;
            set => mCtr3dsPlatform = value;
        }

        /// <summary>Path to aes_keys.txt (slot0x2CKeyX + Secure2/3/4 KeyX). Empty = Extractors/aes_keys.txt.</summary>
        public static string Ctr3dsKeysPath
        {
            get => mCtr3dsKeysPath;
            set => mCtr3dsKeysPath = value;
        }

        /// <summary>Path to seeddb.bin (per-TitleID seeds for 7.x+ seed-crypto titles). Empty = Extractors/seeddb.bin.</summary>
        public static string Ctr3dsSeedDbPath
        {
            get => mCtr3dsSeedDbPath;
            set => mCtr3dsSeedDbPath = value;
        }

        /// <summary>Output folder for the "Decrypt 3DS ROM..." menu item. Empty = next to the source archive.</summary>
        public static string Ctr3dsOutputPath
        {
            get => mCtr3dsOutputPath;
            set => mCtr3dsOutputPath = value;
        }

        /// <summary>If true, the decrypted .3ds from the right-click menu is added as a new game in the LaunchBox library.</summary>
        public static bool Ctr3dsAddToLibrary
        {
            get => mCtr3dsAddToLibrary;
            set => mCtr3dsAddToLibrary = value;
        }

        /// <summary>True if the supplied LaunchBox platform matches the 3DS decrypt platform list.</summary>
        public static bool MatchesCtr3dsPlatform(string platform) => MatchesPlatformCsv(Ctr3dsPlatform, platform);

        public static bool GetCtr3dsCacheOnLaunch(string key) => GetEmulatorPlatformFlag(key, c => c.Ctr3dsCacheOnLaunch, defaultCtr3dsCacheOnLaunch);
        public static bool GetPsvPkgCacheOnLaunch(string key) => GetEmulatorPlatformFlag(key, c => c.PsvPkgCacheOnLaunch, defaultPsvPkgCacheOnLaunch);

        /// <summary>LaunchBox platform(s) for which the PS Vita PKG flow is offered.</summary>
        public static string PsvPkgPlatform { get => mPsvPkgPlatform; set => mPsvPkgPlatform = value; }
        /// <summary>Output base folder for menu-created PSV VPK files. Empty = next to the source archive.</summary>
        public static string PsvPkgOutputPath { get => mPsvPkgOutputPath; set => mPsvPkgOutputPath = value; }
        /// <summary>If true, the produced .vpk is also added to LaunchBox.</summary>
        public static bool PsvPkgAddToLibrary { get => mPsvPkgAddToLibrary; set => mPsvPkgAddToLibrary = value; }
        public static bool MatchesPsvPkgPlatform(string platform) => MatchesPlatformCsv(PsvPkgPlatform, platform);

        /// <summary>
        /// Vita3K user-data folder (the one that contains ux0/, ur0/, vs0/, …). Default falls back
        /// to %APPDATA%\Vita3K\Vita3K. Used by the PSV pipeline to drop auto-decoded .rif licenses
        /// at &lt;data&gt;/ux0/license/app/&lt;TITLE_ID&gt;/&lt;contentid&gt;.rif.
        /// </summary>
        public static string PsvVita3kDataPath { get => mPsvVita3kDataPath; set => mPsvVita3kDataPath = value; }

        /// <summary>
        /// Path to a NoPayStation TSV file or a folder of TSV files (PS3_GAMES.tsv,
        /// PS3_DLC.tsv, PSP_GAMES.tsv, PSV_GAMES.tsv, etc.). Used by the Sony PKG flows to
        /// auto-stage missing zRIF (PSV) / RAP (PS3+PSP) licenses when the source archive
        /// doesn't carry them.
        /// </summary>
        public static string NpsDbPath { get => mNpsDbPath; set => mNpsDbPath = value; }

        /// <summary>PSV equivalent of Ps3AutoInstallUpdates — auto-fetches Vita patches from Sony at launch and stages them in &lt;Vita3K&gt;/ux0/patch/&lt;TID&gt;/.</summary>
        public static bool PsvAutoInstallUpdates { get => mPsvAutoInstallUpdates; set => mPsvAutoInstallUpdates = value; }
        public static string PsvUpdateCachePath { get => mPsvUpdateCachePath; set => mPsvUpdateCachePath = value; }
        public static bool PsvUpdateOfflineMode { get => mPsvUpdateOfflineMode; set => mPsvUpdateOfflineMode = value; }

        /// <summary>LaunchBox platform that enables the PSP PKG on-launch packager flow.</summary>
        public static string PspPkgPlatform
        {
            get => mPspPkgPlatform;
            set => mPspPkgPlatform = value;
        }

        /// <summary>Output base folder for staged PSP PKG installs. Empty = next to the source archive.</summary>
        public static string PspPkgOutputPath
        {
            get => mPspPkgOutputPath;
            set => mPspPkgOutputPath = value;
        }

        /// <summary>
        /// Path to PPSSPP's memstick PSP/LICENSE/ folder. When set, RAP licence files from
        /// the source archive are copied here so PPSSPP picks them up automatically. Empty
        /// = leave RAPs only inside the plugin cache (user must copy them manually).
        /// </summary>
        public static string PspPpssppLicensePath
        {
            get => mPspPpssppLicensePath;
            set => mPspPpssppLicensePath = value;
        }

        /// <summary>If true, the staged install is also added as a new game in the LaunchBox library.</summary>
        public static bool PspPkgAddToLibrary
        {
            get => mPspPkgAddToLibrary;
            set => mPspPkgAddToLibrary = value;
        }

        /// <summary>True if the supplied LaunchBox platform name matches any entry in the PSP PKG platform list.</summary>
        public static bool MatchesPspPkgPlatform(string platform) => MatchesPlatformCsv(PspPkgPlatform, platform);

        public static bool GetWiiuCacheOnLaunch(string key) => GetEmulatorPlatformFlag(key, c => c.WiiuCacheOnLaunch, defaultWiiuCacheOnLaunch);
        public static bool GetCiaCacheOnLaunch(string key) => GetEmulatorPlatformFlag(key, c => c.CiaCacheOnLaunch, defaultCiaCacheOnLaunch);
        public static bool GetWadCacheOnLaunch(string key) => GetEmulatorPlatformFlag(key, c => c.WadCacheOnLaunch, defaultWadCacheOnLaunch);
        public static bool GetTadCacheOnLaunch(string key) => GetEmulatorPlatformFlag(key, c => c.TadCacheOnLaunch, defaultTadCacheOnLaunch);
        public static bool GetPs3PkgCacheOnLaunch(string key) => GetEmulatorPlatformFlag(key, c => c.Ps3PkgCacheOnLaunch, defaultPs3PkgCacheOnLaunch);
        public static bool GetPspPkgCacheOnLaunch(string key) => GetEmulatorPlatformFlag(key, c => c.PspPkgCacheOnLaunch, defaultPspPkgCacheOnLaunch);

        private static bool GetEmulatorPlatformFlag(string key, Func<EmulatorPlatformConfig, bool> selector, bool fallback)
        {
            try
            {
                return selector(mEmulatorPlatformConfig[key]);
            }
            catch (KeyNotFoundException) { }

            try
            {
                return selector(mEmulatorPlatformConfig[defaultEmulatorPlatform]);
            }
            catch (KeyNotFoundException) { }

            return fallback;
        }

        private static bool MatchesPlatformCsv(string csv, string platform)
        {
            if (string.IsNullOrWhiteSpace(csv) || string.IsNullOrWhiteSpace(platform)) return false;
            foreach (string entry in csv.Split(';'))
            {
                string trimmed = entry.Trim();
                if (trimmed.Length == 0) continue;
                if (string.Equals(trimmed, platform, StringComparison.InvariantCultureIgnoreCase)) return true;
            }
            return false;
        }

        public static Dictionary<string, EmulatorPlatformConfig> GetAllEmulatorPlatformConfig()
        {
            return mEmulatorPlatformConfig;
        }

        public static ref Dictionary<string, EmulatorPlatformConfig> GetAllEmulatorPlatformConfigByRef()
        {
            return ref mEmulatorPlatformConfig;
        }

        public static EmulatorPlatformConfig GetEmulatorPlatformConfig(string key)
        {
            try
            {
                return mEmulatorPlatformConfig[key];
            }
            catch (KeyNotFoundException) { }

            try
            {
                return mEmulatorPlatformConfig[defaultEmulatorPlatform];
            }
            catch (KeyNotFoundException) { }

            return new EmulatorPlatformConfig();
        }

        public static string GetFilenamePriority(string key)
        {
            try
            {
                return mEmulatorPlatformConfig[key].FilenamePriority;
            }
            catch (KeyNotFoundException) { }

            try
            {
                return mEmulatorPlatformConfig[defaultEmulatorPlatform].FilenamePriority;
            }
            catch (KeyNotFoundException) { }

            return defaultFilenamePriority;
        }

        public static Action GetAction(string key)
        {
            try
            {
                return mEmulatorPlatformConfig[key].Action;
            }
            catch (KeyNotFoundException) { }

            try
            {
                return mEmulatorPlatformConfig[defaultEmulatorPlatform].Action;
            }
            catch (KeyNotFoundException) { }

            return defaultAction;
        }

        public static LaunchPath GetLaunchPath(string key)
        {
            try
            {
                return mEmulatorPlatformConfig[key].LaunchPath;
            }
            catch (KeyNotFoundException) { }

            try
            {
                return mEmulatorPlatformConfig[defaultEmulatorPlatform].LaunchPath;
            }
            catch (KeyNotFoundException) { }

            return defaultLaunchPath;
        }

        public static bool GetMultiDisc(string key)
        {
            try
            {
                return mEmulatorPlatformConfig[key].MultiDisc;
            }
            catch (KeyNotFoundException) { }

            try
            {
                return mEmulatorPlatformConfig[defaultEmulatorPlatform].MultiDisc;
            }
            catch (KeyNotFoundException) { }

            return defaultMultiDisc;
        }

        public static M3uName GetM3uName(string key)
        {
            try
            {
                return mEmulatorPlatformConfig[key].M3uName;
            }
            catch (KeyNotFoundException) { }

            try
            {
                return mEmulatorPlatformConfig[defaultEmulatorPlatform].M3uName;
            }
            catch (KeyNotFoundException) { }

            return defaultM3uName;
        }

        public static bool GetSmartExtract(string key)
        {
            try
            {
                return mEmulatorPlatformConfig[key].SmartExtract;
            }
            catch (KeyNotFoundException) { }

            try
            {
                return mEmulatorPlatformConfig[defaultEmulatorPlatform].SmartExtract;
            }
            catch (KeyNotFoundException) { }

            return defaultSmartExtract;
        }

        public static bool GetChdman(string key)
        {
            try
            {
                return mEmulatorPlatformConfig[key].Chdman;
            }
            catch (KeyNotFoundException) { }

            try
            {
                return mEmulatorPlatformConfig[defaultEmulatorPlatform].Chdman;
            }
            catch (KeyNotFoundException) { }

            return defaultChdman;
        }

        public static bool GetDolphinTool(string key)
        {
            try
            {
                return mEmulatorPlatformConfig[key].DolphinTool;
            }
            catch (KeyNotFoundException) { }

            try
            {
                return mEmulatorPlatformConfig[defaultEmulatorPlatform].DolphinTool;
            }
            catch (KeyNotFoundException) { }

            return defaultDolphinTool;
        }

        public static bool GetExtractXiso(string key)
        {
            try
            {
                return mEmulatorPlatformConfig[key].ExtractXiso;
            }
            catch (KeyNotFoundException) { }

            try
            {
                return mEmulatorPlatformConfig[defaultEmulatorPlatform].ExtractXiso;
            }
            catch (KeyNotFoundException) { }

            return defaultExtractXiso;
        }

        public static bool GetPS3dec(string key)
        {
            try
            {
                return mEmulatorPlatformConfig[key].PS3dec;
            }
            catch (KeyNotFoundException) { }

            try
            {
                return mEmulatorPlatformConfig[defaultEmulatorPlatform].PS3dec;
            }
            catch (KeyNotFoundException) { }

            return defaultPS3dec;
        }

        public static string EmulatorPlatformKey(string emulator, string platform) => string.Format(@"{0} \ {1}", emulator, platform);

        /// <summary>
        /// Load the config into memory from the config file on disk. Will save new config file to disk if there was a error loading the config.
        /// </summary>
        public static void Load()
        {
            bool configMissing = false;

            if (File.Exists(PathUtils.GetPluginConfigPath()))
            {
                var parser = new FileIniDataParser();
                IniData iniData = new IniData();

                try
                {
                    iniData = parser.ReadFile(PathUtils.GetPluginConfigPath());

                    mEmulatorPlatformConfig.Clear();
                    foreach (SectionData section in iniData.Sections)
                    {
                        if (section.SectionName == configSection)
                        {
                            if (section.Keys.ContainsKey(nameof(CachePath)))
                            {
                                mCachePath = section.Keys[nameof(CachePath)];
                            }
                            // Older config file version used lower case first letter
                            else if (section.Keys.ContainsKey("cachePath"))
                            {
                                mCachePath = section.Keys["cachePath"];
                            }

                            if (section.Keys.ContainsKey(nameof(CacheSize)))
                            {
                                mCacheSize = Convert.ToInt64(section.Keys[nameof(CacheSize)]);
                            }
                            // Older config file version used lower case first letter
                            else if (section.Keys.ContainsKey("cacheSize"))
                            {
                                mCacheSize = Convert.ToInt64(section.Keys["cacheSize"]);
                            }

                            if (section.Keys.ContainsKey(nameof(MinArchiveSize)))
                            {
                                mMinArchiveSize = Convert.ToInt64(section.Keys[nameof(MinArchiveSize)]);
                            }
                            // Older config file version used lower case first letter
                            else if (section.Keys.ContainsKey("minArchiveSize"))
                            {
                                mMinArchiveSize = Convert.ToInt64(section.Keys["minArchiveSize"]);
                            }

                            if (section.Keys.ContainsKey(nameof(UpdateCheck)))
                            {
                                mUpdateCheck = Convert.ToBoolean(section.Keys[nameof(UpdateCheck)]);
                            }
                            else
                            {
                                // Set this null to indicate the option has never been set.
                                mUpdateCheck = null;
                            }

                            if (section.Keys.ContainsKey(nameof(SkipUpdate)))
                            {
                                mSkipUpdate = section.Keys[nameof(SkipUpdate)];
                            }

                            if (section.Keys.ContainsKey(nameof(StandaloneExtensions)))
                            {
                                mStandaloneExtensions = section.Keys[nameof(StandaloneExtensions)];
                            }

                            if (section.Keys.ContainsKey(nameof(MetadataExtensions)))
                            {
                                mMetadataExtensions = section.Keys[nameof(MetadataExtensions)];
                            }

                            if (section.Keys.ContainsKey(nameof(BypassPathCheck)))
                            {
                                mBypassPathCheck = Convert.ToBoolean(section.Keys[nameof(BypassPathCheck)]);
                            }

                            if (section.Keys.ContainsKey(nameof(PS3KeyPath)))
                            {
                                mPS3KeyPath = section.Keys[nameof(PS3KeyPath)];
                            }

                            if (section.Keys.ContainsKey(nameof(Ps3PkgAutoInstallToRpcs3)))
                            {
                                mPs3PkgAutoInstallToRpcs3 = Convert.ToBoolean(section.Keys[nameof(Ps3PkgAutoInstallToRpcs3)]);
                            }
                            if (section.Keys.ContainsKey(nameof(Ps3LocalPkgFolders))) mPs3LocalPkgFolders = section.Keys[nameof(Ps3LocalPkgFolders)];
                            if (section.Keys.ContainsKey(nameof(PspLocalPkgFolders))) mPspLocalPkgFolders = section.Keys[nameof(PspLocalPkgFolders)];
                            if (section.Keys.ContainsKey(nameof(PsvLocalPkgFolders))) mPsvLocalPkgFolders = section.Keys[nameof(PsvLocalPkgFolders)];
                            if (section.Keys.ContainsKey(nameof(WiiuLocalRomFolders))) mWiiuLocalRomFolders = section.Keys[nameof(WiiuLocalRomFolders)];
                            if (section.Keys.ContainsKey(nameof(Ctr3dsLocalRomFolders))) mCtr3dsLocalRomFolders = section.Keys[nameof(Ctr3dsLocalRomFolders)];
                            if (section.Keys.ContainsKey(nameof(WiiLocalRomFolders))) mWiiLocalRomFolders = section.Keys[nameof(WiiLocalRomFolders)];
                            if (section.Keys.ContainsKey(nameof(Ps3AutoInstallDlcs))) mPs3AutoInstallDlcs = Convert.ToBoolean(section.Keys[nameof(Ps3AutoInstallDlcs)]);
                            if (section.Keys.ContainsKey(nameof(WiiuAutoInstallUpdates))) mWiiuAutoInstallUpdates = Convert.ToBoolean(section.Keys[nameof(WiiuAutoInstallUpdates)]);
                            if (section.Keys.ContainsKey(nameof(WiiuAutoInstallDlcs)))    mWiiuAutoInstallDlcs    = Convert.ToBoolean(section.Keys[nameof(WiiuAutoInstallDlcs)]);
                            if (section.Keys.ContainsKey(nameof(PspAutoInstallDlcs))) mPspAutoInstallDlcs = Convert.ToBoolean(section.Keys[nameof(PspAutoInstallDlcs)]);
                            if (section.Keys.ContainsKey(nameof(PsvAutoInstallDlcs))) mPsvAutoInstallDlcs = Convert.ToBoolean(section.Keys[nameof(PsvAutoInstallDlcs)]);
                            if (section.Keys.ContainsKey(nameof(Ps3UseIsoMountLauncher)))
                            {
                                mPs3UseIsoMountLauncher = Convert.ToBoolean(section.Keys[nameof(Ps3UseIsoMountLauncher)]);
                            }

                            if (section.Keys.ContainsKey(nameof(WadPlatform)))
                            {
                                mWadPlatform = section.Keys[nameof(WadPlatform)];
                            }

                            if (section.Keys.ContainsKey(nameof(WadOutputPath)))
                            {
                                mWadOutputPath = section.Keys[nameof(WadOutputPath)];
                            }

                            if (section.Keys.ContainsKey(nameof(WadCetkCachePath)))
                            {
                                mWadCetkCachePath = section.Keys[nameof(WadCetkCachePath)];
                            }

                            if (section.Keys.ContainsKey(nameof(WadTitleKeysPath)))
                            {
                                mWadTitleKeysPath = section.Keys[nameof(WadTitleKeysPath)];
                            }

                            if (section.Keys.ContainsKey(nameof(WadAddToLibrary)))
                            {
                                mWadAddToLibrary = Convert.ToBoolean(section.Keys[nameof(WadAddToLibrary)]);
                            }

                            if (section.Keys.ContainsKey(nameof(WiiuPlatform)))
                            {
                                mWiiuPlatform = section.Keys[nameof(WiiuPlatform)];
                            }

                            if (section.Keys.ContainsKey(nameof(WiiuOutputPath)))
                            {
                                mWiiuOutputPath = section.Keys[nameof(WiiuOutputPath)];
                            }

                            if (section.Keys.ContainsKey(nameof(WiiuAddToLibrary)))
                            {
                                mWiiuAddToLibrary = Convert.ToBoolean(section.Keys[nameof(WiiuAddToLibrary)]);
                            }

                            if (section.Keys.ContainsKey(nameof(WiiuTitleKeyPassword)))
                            {
                                mWiiuTitleKeyPassword = section.Keys[nameof(WiiuTitleKeyPassword)];
                            }

                            if (section.Keys.ContainsKey(nameof(WiiuCommonKey)))
                            {
                                mWiiuCommonKey = section.Keys[nameof(WiiuCommonKey)];
                            }

                            if (section.Keys.ContainsKey(nameof(WiiuCemuKeysPath)))
                            {
                                mWiiuCemuKeysPath = section.Keys[nameof(WiiuCemuKeysPath)];
                            }

                            if (section.Keys.ContainsKey(nameof(WiiuPackAsWua)))
                            {
                                mWiiuPackAsWua = Convert.ToBoolean(section.Keys[nameof(WiiuPackAsWua)]);
                            }

                            if (section.Keys.ContainsKey(nameof(CiaPlatform)))
                            {
                                mCiaPlatform = section.Keys[nameof(CiaPlatform)];
                            }

                            if (section.Keys.ContainsKey(nameof(CiaOutputPath)))
                            {
                                mCiaOutputPath = section.Keys[nameof(CiaOutputPath)];
                            }

                            if (section.Keys.ContainsKey(nameof(CiaCetkCachePath)))
                            {
                                mCiaCetkCachePath = section.Keys[nameof(CiaCetkCachePath)];
                            }

                            if (section.Keys.ContainsKey(nameof(CiaEncTitleKeysPath)))
                            {
                                mCiaEncTitleKeysPath = section.Keys[nameof(CiaEncTitleKeysPath)];
                            }

                            if (section.Keys.ContainsKey(nameof(CiaCetkDonorPath)))
                            {
                                mCiaCetkDonorPath = section.Keys[nameof(CiaCetkDonorPath)];
                            }

                            if (section.Keys.ContainsKey(nameof(CiaAddToLibrary)))
                            {
                                mCiaAddToLibrary = Convert.ToBoolean(section.Keys[nameof(CiaAddToLibrary)]);
                            }

                            if (section.Keys.ContainsKey(nameof(TadPlatform)))
                            {
                                mTadPlatform = section.Keys[nameof(TadPlatform)];
                            }

                            if (section.Keys.ContainsKey(nameof(TadOutputPath)))
                            {
                                mTadOutputPath = section.Keys[nameof(TadOutputPath)];
                            }

                            if (section.Keys.ContainsKey(nameof(TadCetkCachePath)))
                            {
                                mTadCetkCachePath = section.Keys[nameof(TadCetkCachePath)];
                            }

                            if (section.Keys.ContainsKey(nameof(TadAddToLibrary)))
                            {
                                mTadAddToLibrary = Convert.ToBoolean(section.Keys[nameof(TadAddToLibrary)]);
                            }

                            if (section.Keys.ContainsKey(nameof(Ps3PkgPlatform)))
                            {
                                mPs3PkgPlatform = section.Keys[nameof(Ps3PkgPlatform)];
                            }

                            if (section.Keys.ContainsKey(nameof(Ps3PkgOutputPath)))
                            {
                                mPs3PkgOutputPath = section.Keys[nameof(Ps3PkgOutputPath)];
                            }

                            if (section.Keys.ContainsKey(nameof(Ps3RpcsExdataPath)))
                            {
                                mPs3RpcsExdataPath = section.Keys[nameof(Ps3RpcsExdataPath)];
                            }

                            if (section.Keys.ContainsKey(nameof(Ps3PkgAddToLibrary)))
                            {
                                mPs3PkgAddToLibrary = Convert.ToBoolean(section.Keys[nameof(Ps3PkgAddToLibrary)]);
                            }

                            if (section.Keys.ContainsKey(nameof(Ps3UpdateOutputPath)))
                            {
                                mPs3UpdateOutputPath = section.Keys[nameof(Ps3UpdateOutputPath)];
                            }

                            if (section.Keys.ContainsKey(nameof(Ps3AutoInstallUpdates)))
                            {
                                mPs3AutoInstallUpdates = Convert.ToBoolean(section.Keys[nameof(Ps3AutoInstallUpdates)]);
                            }

                            if (section.Keys.ContainsKey(nameof(Ps3UpdateCachePath)))
                            {
                                mPs3UpdateCachePath = section.Keys[nameof(Ps3UpdateCachePath)];
                            }

                            if (section.Keys.ContainsKey(nameof(Ps3UpdateOfflineMode)))
                            {
                                mPs3UpdateOfflineMode = Convert.ToBoolean(section.Keys[nameof(Ps3UpdateOfflineMode)]);
                            }

                            if (section.Keys.ContainsKey(nameof(PspUpdateOutputPath)))
                            {
                                mPspUpdateOutputPath = section.Keys[nameof(PspUpdateOutputPath)];
                            }
                            if (section.Keys.ContainsKey(nameof(PspAutoInstallUpdates)))
                            {
                                mPspAutoInstallUpdates = Convert.ToBoolean(section.Keys[nameof(PspAutoInstallUpdates)]);
                            }
                            if (section.Keys.ContainsKey(nameof(PspUpdateCachePath)))
                            {
                                mPspUpdateCachePath = section.Keys[nameof(PspUpdateCachePath)];
                            }
                            if (section.Keys.ContainsKey(nameof(PspUpdateOfflineMode)))
                            {
                                mPspUpdateOfflineMode = Convert.ToBoolean(section.Keys[nameof(PspUpdateOfflineMode)]);
                            }

                            if (section.Keys.ContainsKey(nameof(Ctr3dsPlatform))) mCtr3dsPlatform = section.Keys[nameof(Ctr3dsPlatform)];
                            if (section.Keys.ContainsKey(nameof(Ctr3dsKeysPath))) mCtr3dsKeysPath = section.Keys[nameof(Ctr3dsKeysPath)];
                            if (section.Keys.ContainsKey(nameof(Ctr3dsSeedDbPath))) mCtr3dsSeedDbPath = section.Keys[nameof(Ctr3dsSeedDbPath)];
                            if (section.Keys.ContainsKey(nameof(Ctr3dsOutputPath))) mCtr3dsOutputPath = section.Keys[nameof(Ctr3dsOutputPath)];
                            if (section.Keys.ContainsKey(nameof(Ctr3dsAddToLibrary))) mCtr3dsAddToLibrary = Convert.ToBoolean(section.Keys[nameof(Ctr3dsAddToLibrary)]);

                            if (section.Keys.ContainsKey(nameof(PsvPkgPlatform))) mPsvPkgPlatform = section.Keys[nameof(PsvPkgPlatform)];
                            if (section.Keys.ContainsKey(nameof(PsvPkgOutputPath))) mPsvPkgOutputPath = section.Keys[nameof(PsvPkgOutputPath)];
                            if (section.Keys.ContainsKey(nameof(PsvPkgAddToLibrary))) mPsvPkgAddToLibrary = Convert.ToBoolean(section.Keys[nameof(PsvPkgAddToLibrary)]);
                            if (section.Keys.ContainsKey(nameof(PsvVita3kDataPath))) mPsvVita3kDataPath = section.Keys[nameof(PsvVita3kDataPath)];
                            if (section.Keys.ContainsKey(nameof(NpsDbPath))) mNpsDbPath = section.Keys[nameof(NpsDbPath)];
                            if (section.Keys.ContainsKey(nameof(PsvAutoInstallUpdates))) mPsvAutoInstallUpdates = Convert.ToBoolean(section.Keys[nameof(PsvAutoInstallUpdates)]);
                            if (section.Keys.ContainsKey(nameof(PsvUpdateCachePath))) mPsvUpdateCachePath = section.Keys[nameof(PsvUpdateCachePath)];
                            if (section.Keys.ContainsKey(nameof(PsvUpdateOfflineMode))) mPsvUpdateOfflineMode = Convert.ToBoolean(section.Keys[nameof(PsvUpdateOfflineMode)]);

                            if (section.Keys.ContainsKey(nameof(PspPkgPlatform)))
                            {
                                mPspPkgPlatform = section.Keys[nameof(PspPkgPlatform)];
                            }

                            if (section.Keys.ContainsKey(nameof(PspPkgOutputPath)))
                            {
                                mPspPkgOutputPath = section.Keys[nameof(PspPkgOutputPath)];
                            }

                            if (section.Keys.ContainsKey(nameof(PspPpssppLicensePath)))
                            {
                                mPspPpssppLicensePath = section.Keys[nameof(PspPpssppLicensePath)];
                            }

                            if (section.Keys.ContainsKey(nameof(PspPkgAddToLibrary)))
                            {
                                mPspPkgAddToLibrary = Convert.ToBoolean(section.Keys[nameof(PspPkgAddToLibrary)]);
                            }


                            if (section.Keys.ContainsKey("MultiDiscSupport"))
                            {
                                mMultiDiscSupport = Convert.ToBoolean(section.Keys["MultiDiscSupport"]);
                            }

                            if (section.Keys.ContainsKey("UseGameIdAsM3uFilename"))
                            {
                                mUseGameIdAsM3uFilename = Convert.ToBoolean(section.Keys["UseGameIdAsM3uFilename"]);
                            }
                        }
                        else
                        {
                            // If this is the first time we've seen this section ("emulator \ platform" pair), create the EmulatorPlatformConfig object
                            if (!mEmulatorPlatformConfig.ContainsKey(section.SectionName))
                            {
                                mEmulatorPlatformConfig.Add(section.SectionName, new EmulatorPlatformConfig());
                            }

                            if (section.Keys.ContainsKey(nameof(EmulatorPlatformConfig.FilenamePriority)))
                            {
                                mEmulatorPlatformConfig[section.SectionName].FilenamePriority = section.Keys[nameof(EmulatorPlatformConfig.FilenamePriority)];
                            }
                            else if (section.Keys.ContainsKey("ExtensionPriority"))
                            {
                                mEmulatorPlatformConfig[section.SectionName].FilenamePriority = section.Keys["ExtensionPriority"];
                            }
                            else if (section.Keys.ContainsKey("extensionPriority"))
                            {
                                mEmulatorPlatformConfig[section.SectionName].FilenamePriority = section.Keys["extensionPriority"];
                            }

                            if (section.Keys.ContainsKey(nameof(EmulatorPlatformConfig.Action)))
                            {
                                Enum.TryParse(section.Keys[nameof(EmulatorPlatformConfig.Action)], out mEmulatorPlatformConfig[section.SectionName].Action);
                            }

                            if (section.Keys.ContainsKey(nameof(EmulatorPlatformConfig.LaunchPath)))
                            {
                                Enum.TryParse(section.Keys[nameof(EmulatorPlatformConfig.LaunchPath)], out mEmulatorPlatformConfig[section.SectionName].LaunchPath);
                            }

                            if (section.Keys.ContainsKey(nameof(EmulatorPlatformConfig.MultiDisc)))
                            {
                                mEmulatorPlatformConfig[section.SectionName].MultiDisc = Convert.ToBoolean(section.Keys[nameof(EmulatorPlatformConfig.MultiDisc)]);
                            }
                            else
                            {
                                mEmulatorPlatformConfig[section.SectionName].MultiDisc = mMultiDiscSupport;
                            }

                            if (section.Keys.ContainsKey(nameof(EmulatorPlatformConfig.M3uName)))
                            {
                                Enum.TryParse(section.Keys[nameof(EmulatorPlatformConfig.M3uName)], out mEmulatorPlatformConfig[section.SectionName].M3uName);
                            }
                            else
                            {
                                mEmulatorPlatformConfig[section.SectionName].M3uName = mUseGameIdAsM3uFilename ? M3uName.GameId : M3uName.TitleVersion;
                            }

                            if (section.Keys.ContainsKey(nameof(EmulatorPlatformConfig.SmartExtract)))
                            {
                                mEmulatorPlatformConfig[section.SectionName].SmartExtract = Convert.ToBoolean(section.Keys[nameof(EmulatorPlatformConfig.SmartExtract)]);
                            }

                            if (section.Keys.ContainsKey(nameof(EmulatorPlatformConfig.Chdman)))
                            {
                                mEmulatorPlatformConfig[section.SectionName].Chdman = Convert.ToBoolean(section.Keys[nameof(EmulatorPlatformConfig.Chdman)]);
                            }

                            if (section.Keys.ContainsKey(nameof(EmulatorPlatformConfig.DolphinTool)))
                            {
                                mEmulatorPlatformConfig[section.SectionName].DolphinTool = Convert.ToBoolean(section.Keys[nameof(EmulatorPlatformConfig.DolphinTool)]);
                            }

                            if (section.Keys.ContainsKey(nameof(EmulatorPlatformConfig.ExtractXiso)))
                            {
                                mEmulatorPlatformConfig[section.SectionName].ExtractXiso = Convert.ToBoolean(section.Keys[nameof(EmulatorPlatformConfig.ExtractXiso)]);
                            }
                            if (section.Keys.ContainsKey(nameof(EmulatorPlatformConfig.PS3dec)))
                            {
                                mEmulatorPlatformConfig[section.SectionName].PS3dec = Convert.ToBoolean(section.Keys[nameof(EmulatorPlatformConfig.PS3dec)]);
                            }

                            if (section.Keys.ContainsKey(nameof(EmulatorPlatformConfig.WiiuCacheOnLaunch)))
                            {
                                mEmulatorPlatformConfig[section.SectionName].WiiuCacheOnLaunch = Convert.ToBoolean(section.Keys[nameof(EmulatorPlatformConfig.WiiuCacheOnLaunch)]);
                            }

                            if (section.Keys.ContainsKey(nameof(EmulatorPlatformConfig.CiaCacheOnLaunch)))
                            {
                                mEmulatorPlatformConfig[section.SectionName].CiaCacheOnLaunch = Convert.ToBoolean(section.Keys[nameof(EmulatorPlatformConfig.CiaCacheOnLaunch)]);
                            }

                            if (section.Keys.ContainsKey(nameof(EmulatorPlatformConfig.WadCacheOnLaunch)))
                            {
                                mEmulatorPlatformConfig[section.SectionName].WadCacheOnLaunch = Convert.ToBoolean(section.Keys[nameof(EmulatorPlatformConfig.WadCacheOnLaunch)]);
                            }

                            if (section.Keys.ContainsKey(nameof(EmulatorPlatformConfig.TadCacheOnLaunch)))
                            {
                                mEmulatorPlatformConfig[section.SectionName].TadCacheOnLaunch = Convert.ToBoolean(section.Keys[nameof(EmulatorPlatformConfig.TadCacheOnLaunch)]);
                            }

                            if (section.Keys.ContainsKey(nameof(EmulatorPlatformConfig.Ps3PkgCacheOnLaunch)))
                            {
                                mEmulatorPlatformConfig[section.SectionName].Ps3PkgCacheOnLaunch = Convert.ToBoolean(section.Keys[nameof(EmulatorPlatformConfig.Ps3PkgCacheOnLaunch)]);
                            }

                            if (section.Keys.ContainsKey(nameof(EmulatorPlatformConfig.PspPkgCacheOnLaunch)))
                            {
                                mEmulatorPlatformConfig[section.SectionName].PspPkgCacheOnLaunch = Convert.ToBoolean(section.Keys[nameof(EmulatorPlatformConfig.PspPkgCacheOnLaunch)]);
                            }

                            if (section.Keys.ContainsKey(nameof(EmulatorPlatformConfig.Ctr3dsCacheOnLaunch)))
                            {
                                mEmulatorPlatformConfig[section.SectionName].Ctr3dsCacheOnLaunch = Convert.ToBoolean(section.Keys[nameof(EmulatorPlatformConfig.Ctr3dsCacheOnLaunch)]);
                            }

                            if (section.Keys.ContainsKey(nameof(EmulatorPlatformConfig.PsvPkgCacheOnLaunch)))
                            {
                                mEmulatorPlatformConfig[section.SectionName].PsvPkgCacheOnLaunch = Convert.ToBoolean(section.Keys[nameof(EmulatorPlatformConfig.PsvPkgCacheOnLaunch)]);
                            }
                        }
                    }

                    // Check if the [All \ All] section exists.
                    if (!iniData.Sections.ContainsSection(defaultEmulatorPlatform))
                    {
                        if (!mEmulatorPlatformConfig.ContainsKey(defaultEmulatorPlatform))
                        {
                            mEmulatorPlatformConfig.Add(defaultEmulatorPlatform, new EmulatorPlatformConfig());
                        }
                        configMissing |= true;
                    }
                }
                catch (Exception e)
                {
                    Logger.Log(string.Format("Error parsing config file from {0}. Using default config.", PathUtils.GetPluginConfigPath()));
                    Logger.Log(e.ToString(), Logger.LogLevel.Exception);
                    SetDefaultConfig();
                    configMissing |= true;
                }

                if (!PathUtils.IsPathSafe(mCachePath))
                {
                    Logger.Log(string.Format("Config CachePath can not be set to \"{0}\", using default ({1}).", mCachePath, defaultCachePath));
                    mCachePath = defaultCachePath;
                    configMissing |= true;
                }
                // CacheSize must be larger than 0
                if (mCacheSize <= 0)
                {
                    Logger.Log(string.Format("Config CacheSize can not be less than or equal 0, using default ({0:n0}).", defaultCacheSize));
                    mCacheSize = defaultCacheSize;
                    configMissing |= true;
                }
                // MinArchiveSize can be zero
                if (mMinArchiveSize < 0)
                {
                    Logger.Log(string.Format("Config MinArchiveSize can not be less than 0, using default ({0:n0}).", defaultMinArchiveSize));
                    mMinArchiveSize = defaultMinArchiveSize;
                    configMissing |= true;
                }

            }
            else
            {
                Logger.Log("Config file does not exist, using default config.");
                SetDefaultConfig();
                configMissing |= true;
            }

            if (configMissing)
            {
                Save();
            }
        }

        /// <summary>
        /// Save current config to config file on disk.
        /// </summary>
        public static void Save()
        {
            var parser = new FileIniDataParser();
            IniData iniData = new IniData();

            iniData[configSection][nameof(CachePath)] = mCachePath;
            iniData[configSection][nameof(CacheSize)] = mCacheSize.ToString();
            iniData[configSection][nameof(MinArchiveSize)] = mMinArchiveSize.ToString();
            if (mUpdateCheck != null)
            {
                iniData[configSection][nameof(UpdateCheck)] = mUpdateCheck.ToString();
            }
            if (!string.IsNullOrEmpty(mSkipUpdate))
            {
                iniData[configSection][nameof(SkipUpdate)] = mSkipUpdate;
            }
            iniData[configSection][nameof(StandaloneExtensions)] = mStandaloneExtensions;
            iniData[configSection][nameof(MetadataExtensions)] = mMetadataExtensions;
            iniData[configSection][nameof(BypassPathCheck)] = mBypassPathCheck.ToString();
            iniData[configSection][nameof(PS3KeyPath)] = mPS3KeyPath;
            iniData[configSection][nameof(Ps3UseIsoMountLauncher)] = mPs3UseIsoMountLauncher.ToString();
            iniData[configSection][nameof(Ps3PkgAutoInstallToRpcs3)] = mPs3PkgAutoInstallToRpcs3.ToString();
            iniData[configSection][nameof(Ps3LocalPkgFolders)] = mPs3LocalPkgFolders;
            iniData[configSection][nameof(PspLocalPkgFolders)] = mPspLocalPkgFolders;
            iniData[configSection][nameof(PsvLocalPkgFolders)] = mPsvLocalPkgFolders;
            iniData[configSection][nameof(WiiuLocalRomFolders)] = mWiiuLocalRomFolders;
            iniData[configSection][nameof(Ctr3dsLocalRomFolders)] = mCtr3dsLocalRomFolders;
            iniData[configSection][nameof(WiiLocalRomFolders)] = mWiiLocalRomFolders;
            iniData[configSection][nameof(Ps3AutoInstallDlcs)] = mPs3AutoInstallDlcs.ToString();
            iniData[configSection][nameof(WiiuAutoInstallUpdates)] = mWiiuAutoInstallUpdates.ToString();
            iniData[configSection][nameof(WiiuAutoInstallDlcs)]    = mWiiuAutoInstallDlcs.ToString();
            iniData[configSection][nameof(PspAutoInstallDlcs)] = mPspAutoInstallDlcs.ToString();
            iniData[configSection][nameof(PsvAutoInstallDlcs)] = mPsvAutoInstallDlcs.ToString();
            iniData[configSection][nameof(WadPlatform)] = mWadPlatform;
            iniData[configSection][nameof(WadOutputPath)] = mWadOutputPath;
            iniData[configSection][nameof(WadCetkCachePath)] = mWadCetkCachePath;
            iniData[configSection][nameof(WadTitleKeysPath)] = mWadTitleKeysPath;
            iniData[configSection][nameof(WadAddToLibrary)] = mWadAddToLibrary.ToString();
            iniData[configSection][nameof(WiiuPlatform)] = mWiiuPlatform;
            iniData[configSection][nameof(WiiuOutputPath)] = mWiiuOutputPath;
            iniData[configSection][nameof(WiiuAddToLibrary)] = mWiiuAddToLibrary.ToString();
            iniData[configSection][nameof(WiiuTitleKeyPassword)] = mWiiuTitleKeyPassword;
            iniData[configSection][nameof(WiiuCommonKey)] = mWiiuCommonKey;
            iniData[configSection][nameof(WiiuCemuKeysPath)] = mWiiuCemuKeysPath;
            iniData[configSection][nameof(WiiuPackAsWua)] = mWiiuPackAsWua.ToString();
            iniData[configSection][nameof(CiaPlatform)] = mCiaPlatform;
            iniData[configSection][nameof(CiaOutputPath)] = mCiaOutputPath;
            iniData[configSection][nameof(CiaCetkCachePath)] = mCiaCetkCachePath;
            iniData[configSection][nameof(CiaEncTitleKeysPath)] = mCiaEncTitleKeysPath;
            iniData[configSection][nameof(CiaCetkDonorPath)] = mCiaCetkDonorPath;
            iniData[configSection][nameof(CiaAddToLibrary)] = mCiaAddToLibrary.ToString();
            iniData[configSection][nameof(TadPlatform)] = mTadPlatform;
            iniData[configSection][nameof(TadOutputPath)] = mTadOutputPath;
            iniData[configSection][nameof(TadCetkCachePath)] = mTadCetkCachePath;
            iniData[configSection][nameof(TadAddToLibrary)] = mTadAddToLibrary.ToString();
            iniData[configSection][nameof(Ps3PkgPlatform)] = mPs3PkgPlatform;
            iniData[configSection][nameof(Ps3PkgOutputPath)] = mPs3PkgOutputPath;
            iniData[configSection][nameof(Ps3RpcsExdataPath)] = mPs3RpcsExdataPath;
            iniData[configSection][nameof(Ps3PkgAddToLibrary)] = mPs3PkgAddToLibrary.ToString();
            iniData[configSection][nameof(Ps3UpdateOutputPath)] = mPs3UpdateOutputPath;
            iniData[configSection][nameof(Ps3AutoInstallUpdates)] = mPs3AutoInstallUpdates.ToString();
            iniData[configSection][nameof(Ps3UpdateCachePath)] = mPs3UpdateCachePath;
            iniData[configSection][nameof(Ps3UpdateOfflineMode)] = mPs3UpdateOfflineMode.ToString();
            iniData[configSection][nameof(PspUpdateOutputPath)] = mPspUpdateOutputPath;
            iniData[configSection][nameof(PspAutoInstallUpdates)] = mPspAutoInstallUpdates.ToString();
            iniData[configSection][nameof(PspUpdateCachePath)] = mPspUpdateCachePath;
            iniData[configSection][nameof(PspUpdateOfflineMode)] = mPspUpdateOfflineMode.ToString();
            iniData[configSection][nameof(Ctr3dsPlatform)] = mCtr3dsPlatform;
            iniData[configSection][nameof(Ctr3dsKeysPath)] = mCtr3dsKeysPath;
            iniData[configSection][nameof(Ctr3dsSeedDbPath)] = mCtr3dsSeedDbPath;
            iniData[configSection][nameof(Ctr3dsOutputPath)] = mCtr3dsOutputPath;
            iniData[configSection][nameof(Ctr3dsAddToLibrary)] = mCtr3dsAddToLibrary.ToString();
            iniData[configSection][nameof(PsvPkgPlatform)] = mPsvPkgPlatform;
            iniData[configSection][nameof(PsvPkgOutputPath)] = mPsvPkgOutputPath;
            iniData[configSection][nameof(PsvPkgAddToLibrary)] = mPsvPkgAddToLibrary.ToString();
            iniData[configSection][nameof(PsvVita3kDataPath)] = mPsvVita3kDataPath;
            iniData[configSection][nameof(NpsDbPath)] = mNpsDbPath;
            iniData[configSection][nameof(PsvAutoInstallUpdates)] = mPsvAutoInstallUpdates.ToString();
            iniData[configSection][nameof(PsvUpdateCachePath)] = mPsvUpdateCachePath;
            iniData[configSection][nameof(PsvUpdateOfflineMode)] = mPsvUpdateOfflineMode.ToString();
            iniData[configSection][nameof(PspPkgPlatform)] = mPspPkgPlatform;
            iniData[configSection][nameof(PspPkgOutputPath)] = mPspPkgOutputPath;
            iniData[configSection][nameof(PspPpssppLicensePath)] = mPspPpssppLicensePath;
            iniData[configSection][nameof(PspPkgAddToLibrary)] = mPspPkgAddToLibrary.ToString();

            foreach (KeyValuePair<string, EmulatorPlatformConfig> priority in mEmulatorPlatformConfig)
            {
                iniData[priority.Key][nameof(EmulatorPlatformConfig.FilenamePriority)] = priority.Value.FilenamePriority;
                iniData[priority.Key][nameof(EmulatorPlatformConfig.Action)] = priority.Value.Action.ToString();
                iniData[priority.Key][nameof(EmulatorPlatformConfig.LaunchPath)] = priority.Value.LaunchPath.ToString();
                iniData[priority.Key][nameof(EmulatorPlatformConfig.MultiDisc)] = priority.Value.MultiDisc.ToString();
                iniData[priority.Key][nameof(EmulatorPlatformConfig.M3uName)] = priority.Value.M3uName.ToString();
                iniData[priority.Key][nameof(EmulatorPlatformConfig.SmartExtract)] = priority.Value.SmartExtract.ToString();
                iniData[priority.Key][nameof(EmulatorPlatformConfig.Chdman)] = priority.Value.Chdman.ToString();
                iniData[priority.Key][nameof(EmulatorPlatformConfig.DolphinTool)] = priority.Value.DolphinTool.ToString();
                iniData[priority.Key][nameof(EmulatorPlatformConfig.ExtractXiso)] = priority.Value.ExtractXiso.ToString();
                iniData[priority.Key][nameof(EmulatorPlatformConfig.PS3dec)] = priority.Value.PS3dec.ToString();
                iniData[priority.Key][nameof(EmulatorPlatformConfig.WiiuCacheOnLaunch)] = priority.Value.WiiuCacheOnLaunch.ToString();
                iniData[priority.Key][nameof(EmulatorPlatformConfig.CiaCacheOnLaunch)] = priority.Value.CiaCacheOnLaunch.ToString();
                iniData[priority.Key][nameof(EmulatorPlatformConfig.WadCacheOnLaunch)] = priority.Value.WadCacheOnLaunch.ToString();
                iniData[priority.Key][nameof(EmulatorPlatformConfig.TadCacheOnLaunch)] = priority.Value.TadCacheOnLaunch.ToString();
                iniData[priority.Key][nameof(EmulatorPlatformConfig.Ps3PkgCacheOnLaunch)] = priority.Value.Ps3PkgCacheOnLaunch.ToString();
                iniData[priority.Key][nameof(EmulatorPlatformConfig.PspPkgCacheOnLaunch)] = priority.Value.PspPkgCacheOnLaunch.ToString();
                iniData[priority.Key][nameof(EmulatorPlatformConfig.Ctr3dsCacheOnLaunch)] = priority.Value.Ctr3dsCacheOnLaunch.ToString();
                iniData[priority.Key][nameof(EmulatorPlatformConfig.PsvPkgCacheOnLaunch)] = priority.Value.PsvPkgCacheOnLaunch.ToString();
            }

            try
            {
                parser.WriteFile(PathUtils.GetPluginConfigPath(), iniData);
            }
            catch (Exception e)
            {
                Logger.Log(string.Format("Error saving config file to {0}.", PathUtils.GetPluginConfigPath()));
                Logger.Log(e.ToString(), Logger.LogLevel.Exception);
            }
        }

        /// <summary>
        /// Initialise internal variables to defaults.
        /// </summary>
        private static void SetDefaultConfig()
        {
            mCachePath = defaultCachePath;
            mCacheSize = defaultCacheSize;
            mMinArchiveSize = defaultMinArchiveSize;
            mStandaloneExtensions = defaultStandaloneExtensions;
            mMetadataExtensions = defaultMetadataExtensions;
            mBypassPathCheck = defaultBypassPathCheck;
            mPS3KeyPath = defaultPS3KeyPath;
            mPs3UseIsoMountLauncher = defaultPs3UseIsoMountLauncher;
            mPs3PkgAutoInstallToRpcs3 = defaultPs3PkgAutoInstallToRpcs3;
            mPs3LocalPkgFolders = defaultPs3LocalPkgFolders;
            mPspLocalPkgFolders = defaultPspLocalPkgFolders;
            mPsvLocalPkgFolders = defaultPsvLocalPkgFolders;
            mWiiuLocalRomFolders = defaultWiiuLocalRomFolders;
            mCtr3dsLocalRomFolders = defaultCtr3dsLocalRomFolders;
            mWiiLocalRomFolders = defaultWiiLocalRomFolders;
            mPs3AutoInstallDlcs = defaultPs3AutoInstallDlcs;
            mWiiuAutoInstallUpdates = defaultWiiuAutoInstallUpdates;
            mWiiuAutoInstallDlcs    = defaultWiiuAutoInstallDlcs;
            mPspAutoInstallDlcs = defaultPspAutoInstallDlcs;
            mPsvAutoInstallDlcs = defaultPsvAutoInstallDlcs;
            mWadPlatform = defaultWadPlatform;
            mWadOutputPath = defaultWadOutputPath;
            mWadCetkCachePath = defaultWadCetkCachePath;
            mWadTitleKeysPath = defaultWadTitleKeysPath;
            mWadAddToLibrary = defaultWadAddToLibrary;
            mWiiuPlatform = defaultWiiuPlatform;
            mWiiuOutputPath = defaultWiiuOutputPath;
            mWiiuAddToLibrary = defaultWiiuAddToLibrary;
            mWiiuTitleKeyPassword = defaultWiiuTitleKeyPassword;
            mWiiuCommonKey = defaultWiiuCommonKey;
            mWiiuCemuKeysPath = defaultWiiuCemuKeysPath;
            mWiiuPackAsWua = defaultWiiuPackAsWua;
            mCiaPlatform = defaultCiaPlatform;
            mCiaOutputPath = defaultCiaOutputPath;
            mCiaCetkCachePath = defaultCiaCetkCachePath;
            mCiaEncTitleKeysPath = defaultCiaEncTitleKeysPath;
            mCiaCetkDonorPath = defaultCiaCetkDonorPath;
            mCiaAddToLibrary = defaultCiaAddToLibrary;
            mTadPlatform = defaultTadPlatform;
            mTadOutputPath = defaultTadOutputPath;
            mTadCetkCachePath = defaultTadCetkCachePath;
            mTadAddToLibrary = defaultTadAddToLibrary;
            mPs3PkgPlatform = defaultPs3PkgPlatform;
            mPs3PkgOutputPath = defaultPs3PkgOutputPath;
            mPs3RpcsExdataPath = defaultPs3RpcsExdataPath;
            mPs3PkgAddToLibrary = defaultPs3PkgAddToLibrary;
            mPs3UpdateOutputPath = defaultPs3UpdateOutputPath;
            mPs3AutoInstallUpdates = defaultPs3AutoInstallUpdates;
            mPs3UpdateCachePath = defaultPs3UpdateCachePath;
            mPs3UpdateOfflineMode = defaultPs3UpdateOfflineMode;
            mPspUpdateOutputPath = defaultPspUpdateOutputPath;
            mPspAutoInstallUpdates = defaultPspAutoInstallUpdates;
            mPspUpdateCachePath = defaultPspUpdateCachePath;
            mPspUpdateOfflineMode = defaultPspUpdateOfflineMode;
            mCtr3dsPlatform = defaultCtr3dsPlatform;
            mCtr3dsKeysPath = defaultCtr3dsKeysPath;
            mCtr3dsSeedDbPath = defaultCtr3dsSeedDbPath;
            mCtr3dsOutputPath = defaultCtr3dsOutputPath;
            mCtr3dsAddToLibrary = defaultCtr3dsAddToLibrary;
            mPsvPkgPlatform = defaultPsvPkgPlatform;
            mPsvPkgOutputPath = defaultPsvPkgOutputPath;
            mPsvPkgAddToLibrary = defaultPsvPkgAddToLibrary;
            mPsvVita3kDataPath = defaultPsvVita3kDataPath;
            mNpsDbPath = defaultNpsDbPath;
            mPsvAutoInstallUpdates = defaultPsvAutoInstallUpdates;
            mPsvUpdateCachePath = defaultPsvUpdateCachePath;
            mPsvUpdateOfflineMode = defaultPsvUpdateOfflineMode;
            mPspPkgPlatform = defaultPspPkgPlatform;
            mPspPkgOutputPath = defaultPspPkgOutputPath;
            mPspPpssppLicensePath = defaultPspPpssppLicensePath;
            mPspPkgAddToLibrary = defaultPspPkgAddToLibrary;

            mEmulatorPlatformConfig = new Dictionary<string, EmulatorPlatformConfig>();
            mEmulatorPlatformConfig.Add(defaultEmulatorPlatform, new EmulatorPlatformConfig());
            EmulatorPlatformConfig e = new EmulatorPlatformConfig();
            e.FilenamePriority = "bin, iso";
            mEmulatorPlatformConfig.Add(@"PCSX2 \ Sony Playstation 2", e);
        }
    }
}
