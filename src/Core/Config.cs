using System;
using System.Collections.Generic;
using System.IO;
using Salaros.Configuration;

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
            bool confignotMissing = false;

            if (File.Exists(PathUtils.GetPluginConfigPath()))
            {
                ConfigParser iniData = new ConfigParser(PathUtils.GetPluginConfigPath());

                try
                {
                    mEmulatorPlatformConfig.Clear();
                    foreach (var section in iniData.Sections)
                    {
                        if (section.SectionName == configSection)
                        {
                            mCachePath = iniData.GetValue(configSection, "CachePath");
                            mCacheSize = Convert.ToInt64(iniData.GetValue(configSection, "CacheSize"));
                            mMinArchiveSize = Convert.ToInt64(iniData.GetValue(configSection, "MinArchiveSize"));
                            mUpdateCheck = Convert.ToBoolean(iniData.GetValue(configSection, "UpdateCheck"));
                            mSkipUpdate = iniData.GetValue(configSection, "SkipUpdate");
                            mStandaloneExtensions = iniData.GetValue(configSection, "StandaloneExtensions");
                            mMetadataExtensions = iniData.GetValue(configSection, "MetadataExtensions");
                            mBypassPathCheck = Convert.ToBoolean(iniData.GetValue(configSection, "BypassPathCheck"));
                            mMultiDiscSupport = Convert.ToBoolean(iniData.GetValue(configSection, "MultiDiscSupport"));
                            mUseGameIdAsM3uFilename = Convert.ToBoolean(iniData.GetValue(configSection, "UseGameIdAsM3uFilename"));
                        }
                        else
                        {
                            // If this is the first time we've seen this section ("emulator \ platform" pair), create the EmulatorPlatformConfig object
                            if (!mEmulatorPlatformConfig.ContainsKey(section.SectionName))
                            {
                                mEmulatorPlatformConfig.Add(section.SectionName, new EmulatorPlatformConfig());
                            }

                            mEmulatorPlatformConfig[section.SectionName].FilenamePriority = iniData.GetValue(section.SectionName, "FilenamePriority");
                            Enum.TryParse(iniData.GetValue(section.SectionName, "Action"), out mEmulatorPlatformConfig[section.SectionName].Action);
                            Enum.TryParse(iniData.GetValue(section.SectionName, "LaunchPath"), out mEmulatorPlatformConfig[section.SectionName].LaunchPath);
                            mEmulatorPlatformConfig[section.SectionName].MultiDisc = Convert.ToBoolean(iniData.GetValue(section.SectionName, "MultiDisc"));
                            Enum.TryParse(iniData.GetValue(section.SectionName, "M3uName"), out mEmulatorPlatformConfig[section.SectionName].M3uName);
                            mEmulatorPlatformConfig[section.SectionName].SmartExtract = Convert.ToBoolean(iniData.GetValue(section.SectionName, "SmartExtract"));
                            mEmulatorPlatformConfig[section.SectionName].Chdman = Convert.ToBoolean(iniData.GetValue(section.SectionName, "Chdman"));
                            mEmulatorPlatformConfig[section.SectionName].DolphinTool = Convert.ToBoolean(iniData.GetValue(section.SectionName, "DolphinTool"));
                            mEmulatorPlatformConfig[section.SectionName].ExtractXiso = Convert.ToBoolean(iniData.GetValue(section.SectionName, "ExtractXiso"));
                            mEmulatorPlatformConfig[section.SectionName].PS3dec = Convert.ToBoolean(iniData.GetValue(section.SectionName, "PS3dec"));
                        }
                    }

                    // Check if the [All \ All] section exists.
                    foreach (var section in iniData.Sections)
                    {
                        if (section.SectionName == defaultEmulatorPlatform)
                        {
                            confignotMissing |= true;

                        }
                    }
                    if (!mEmulatorPlatformConfig.ContainsKey(defaultEmulatorPlatform))
                    {
                        mEmulatorPlatformConfig.Add(defaultEmulatorPlatform, new EmulatorPlatformConfig());
                    }

                }
                catch (Exception e)
                {
                    Logger.Log(string.Format("Error parsing config file from {0}. Using default config.", PathUtils.GetPluginConfigPath()));
                    Logger.Log(e.ToString(), Logger.LogLevel.Exception);
                    SetDefaultConfig();
                    confignotMissing |= false;
                }
                if (!PathUtils.IsPathSafe(mCachePath))
                {
                    Logger.Log(string.Format("Config CachePath can not be set to \"{0}\", using default ({1}).", mCachePath, defaultCachePath));
                    mCachePath = defaultCachePath;
                    confignotMissing |= false;
                }
                // CacheSize must be larger than 0
                if (mCacheSize <= 0)
                {
                    Logger.Log(string.Format("Config CacheSize can not be less than or equal 0, using default ({0:n0}).", defaultCacheSize));
                    mCacheSize = defaultCacheSize;
                    confignotMissing |= false;
                }
                // MinArchiveSize can be zero
                if (mMinArchiveSize < 0)
                {
                    Logger.Log(string.Format("Config MinArchiveSize can not be less than 0, using default ({0:n0}).", defaultMinArchiveSize));
                    mMinArchiveSize = defaultMinArchiveSize;
                    confignotMissing |= false;
                }

            }
            else
            {
                Logger.Log("Config file does not exist, using default config.");
                SetDefaultConfig();
                confignotMissing |= false;
            }

            if (!confignotMissing)
            {
                Save();
            }
        }

        /// <summary>
        /// Save current config to config file on disk.
        /// </summary>
        public static void Save()
        {
            ConfigParser iniData = new ConfigParser();

            iniData.SetValue(configSection, nameof(CachePath), mCachePath);
            iniData.SetValue(configSection, nameof(CacheSize), mCacheSize.ToString());
            iniData.SetValue(configSection, nameof(MinArchiveSize), mMinArchiveSize.ToString());
            if (mUpdateCheck != null)
            {
                iniData.SetValue(configSection, nameof(UpdateCheck), mUpdateCheck.ToString());
            }
            if (!string.IsNullOrEmpty(mSkipUpdate))
            {
                iniData.SetValue(configSection, nameof(SkipUpdate), mSkipUpdate);
            }
            iniData.SetValue(configSection, nameof(StandaloneExtensions), mStandaloneExtensions);
            iniData.SetValue(configSection, nameof(MetadataExtensions), mMetadataExtensions);
            iniData.SetValue(configSection, nameof(BypassPathCheck), mBypassPathCheck.ToString());

            foreach (KeyValuePair<string, EmulatorPlatformConfig> priority in mEmulatorPlatformConfig)
            {
                iniData.SetValue(priority.Key, nameof(EmulatorPlatformConfig.FilenamePriority), priority.Value.FilenamePriority);
                iniData.SetValue(priority.Key, nameof(EmulatorPlatformConfig.Action), priority.Value.Action.ToString());
                iniData.SetValue(priority.Key, nameof(EmulatorPlatformConfig.LaunchPath), priority.Value.LaunchPath.ToString());
                iniData.SetValue(priority.Key, nameof(EmulatorPlatformConfig.MultiDisc), priority.Value.MultiDisc.ToString());
                iniData.SetValue(priority.Key, nameof(EmulatorPlatformConfig.M3uName), priority.Value.M3uName.ToString());
                iniData.SetValue(priority.Key, nameof(EmulatorPlatformConfig.SmartExtract), priority.Value.SmartExtract.ToString());
                iniData.SetValue(priority.Key, nameof(EmulatorPlatformConfig.Chdman), priority.Value.Chdman.ToString());
                iniData.SetValue(priority.Key, nameof(EmulatorPlatformConfig.DolphinTool), priority.Value.DolphinTool.ToString());
                iniData.SetValue(priority.Key, nameof(EmulatorPlatformConfig.ExtractXiso), priority.Value.ExtractXiso.ToString());
                iniData.SetValue(priority.Key, nameof(EmulatorPlatformConfig.PS3dec), priority.Value.PS3dec.ToString());
            }

            try
            {
                iniData.Save(PathUtils.GetPluginConfigPath());
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

            mEmulatorPlatformConfig = new Dictionary<string, EmulatorPlatformConfig>();
            mEmulatorPlatformConfig.Add(defaultEmulatorPlatform, new EmulatorPlatformConfig());
            EmulatorPlatformConfig e = new EmulatorPlatformConfig();
            e.FilenamePriority = "bin, iso";
            mEmulatorPlatformConfig.Add(@"PCSX2 \ Sony Playstation 2", e);
        }
    }
}
