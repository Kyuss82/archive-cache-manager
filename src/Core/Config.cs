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
        private static readonly string defaultFilenamePriority = @"mds, gdi, cue, eboot.bin";

        private static readonly LaunchPath defaultLaunchPath = LaunchPath.Default;
        private static readonly Action defaultAction = Action.Extract;
        private static readonly M3uName defaultM3uName = M3uName.GameId;
        private static readonly bool defaultChdman = false;
        private static readonly bool defaultDolphinTool = false;
        private static readonly bool defaultExtractXiso = false;
        private static readonly bool defaultPS3dec = false;
        private static readonly string defaultPS3KeyPath = @"ThirdParty\PS3key";
        private static readonly bool defaultPs3UseIsoMountLauncher = false;
        private static readonly string defaultWadPlatform = "Nintendo Wii";
        private static readonly string defaultWadOutputPath = "";
        private static readonly string defaultWadCetkCachePath = "";
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
        private static readonly bool defaultCiaAddToLibrary = false;
        private static readonly string defaultTadPlatform = "Nintendo DSi;Nintendo DSiWare";
        private static readonly string defaultTadOutputPath = "";
        private static readonly string defaultTadCetkCachePath = "";
        private static readonly bool defaultTadAddToLibrary = false;
        private static readonly bool defaultWiiuCacheOnLaunch = false;
        private static readonly bool defaultCiaCacheOnLaunch = false;
        private static readonly bool defaultWadCacheOnLaunch = false;
        private static readonly bool defaultTadCacheOnLaunch = false;

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
        private static string mWadPlatform = defaultWadPlatform;
        private static string mWadOutputPath = defaultWadOutputPath;
        private static string mWadCetkCachePath = defaultWadCetkCachePath;
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
        private static bool mCiaAddToLibrary = defaultCiaAddToLibrary;
        private static string mTadPlatform = defaultTadPlatform;
        private static string mTadOutputPath = defaultTadOutputPath;
        private static string mTadCetkCachePath = defaultTadCetkCachePath;
        private static bool mTadAddToLibrary = defaultTadAddToLibrary;

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

        public static bool GetWiiuCacheOnLaunch(string key) => GetEmulatorPlatformFlag(key, c => c.WiiuCacheOnLaunch, defaultWiiuCacheOnLaunch);
        public static bool GetCiaCacheOnLaunch(string key) => GetEmulatorPlatformFlag(key, c => c.CiaCacheOnLaunch, defaultCiaCacheOnLaunch);
        public static bool GetWadCacheOnLaunch(string key) => GetEmulatorPlatformFlag(key, c => c.WadCacheOnLaunch, defaultWadCacheOnLaunch);
        public static bool GetTadCacheOnLaunch(string key) => GetEmulatorPlatformFlag(key, c => c.TadCacheOnLaunch, defaultTadCacheOnLaunch);

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
            iniData[configSection][nameof(WadPlatform)] = mWadPlatform;
            iniData[configSection][nameof(WadOutputPath)] = mWadOutputPath;
            iniData[configSection][nameof(WadCetkCachePath)] = mWadCetkCachePath;
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
            iniData[configSection][nameof(CiaAddToLibrary)] = mCiaAddToLibrary.ToString();
            iniData[configSection][nameof(TadPlatform)] = mTadPlatform;
            iniData[configSection][nameof(TadOutputPath)] = mTadOutputPath;
            iniData[configSection][nameof(TadCetkCachePath)] = mTadCetkCachePath;
            iniData[configSection][nameof(TadAddToLibrary)] = mTadAddToLibrary.ToString();

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
            mWadPlatform = defaultWadPlatform;
            mWadOutputPath = defaultWadOutputPath;
            mWadCetkCachePath = defaultWadCetkCachePath;
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
            mCiaAddToLibrary = defaultCiaAddToLibrary;
            mTadPlatform = defaultTadPlatform;
            mTadOutputPath = defaultTadOutputPath;
            mTadCetkCachePath = defaultTadCetkCachePath;
            mTadAddToLibrary = defaultTadAddToLibrary;

            mEmulatorPlatformConfig = new Dictionary<string, EmulatorPlatformConfig>();
            mEmulatorPlatformConfig.Add(defaultEmulatorPlatform, new EmulatorPlatformConfig());
            EmulatorPlatformConfig e = new EmulatorPlatformConfig();
            e.FilenamePriority = "bin, iso";
            mEmulatorPlatformConfig.Add(@"PCSX2 \ Sony Playstation 2", e);
        }
    }
}
