/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Local 3DS AES key + seed database. Format matches Decrypt9 / Citra / Lime3DS /
 * Azahar's `aes_keys.txt` (one `slot0xNNKeyX=hex_value` per line, plus optional
 * `common0..common5`), and the community `seeddb.bin` (per-TitleID 16-byte seeds
 * for 7.x+ seed-crypto titles).
 *
 * Both files are USER-SUPPLIED in <LaunchBox>\Plugins\ArchiveCacheManager\Extractors\
 * (or paths overridden via Config.Ctr3dsKeysPath / Ctr3dsSeedDbPath). The keys
 * themselves are publicly documented but cannot be redistributed by us — same
 * pattern as wii-titlekeys.bin / encTitleKeys.bin in the WAD/CIA packagers.
 *
 * Slots we care about for NCCH decryption:
 *   0x2C — primary KeyX for retail NCCH (every game)
 *   0x25 — Secure2 KeyX (firmware 7.x+ titles, crypto_method=0x01)
 *   0x18 — Secure3 KeyX (firmware 9.3+ titles, crypto_method=0x0A)
 *   0x1B — Secure4 KeyX (firmware 9.6+ titles, crypto_method=0x0B)
 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace ArchiveCacheManager
{
    public static class CtrAesKeys
    {
        private static readonly Dictionary<string, byte[]> mKeys = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        private static byte[] mSeedDb;
        private static bool mLoaded;
        private static readonly object SyncRoot = new object();

        /// <summary>Returns the 16-byte KeyX for the requested NCCH crypto slot (0x2C / 0x25 / 0x18 / 0x1B), or null if absent.</summary>
        public static byte[] GetKeyX(int slot)
        {
            EnsureLoaded();
            return mKeys.TryGetValue(KeyXName(slot), out byte[] k) ? k : null;
        }

        /// <summary>Look up a 16-byte seed for the supplied 64-bit program/title ID, or null if not in seeddb.bin.</summary>
        public static byte[] LookupSeed(ulong programId)
        {
            EnsureLoaded();
            if (mSeedDb == null || mSeedDb.Length < 0x10) return null;

            // seeddb.bin layout: uint32 count, uint32 padding, then count×(uint64 program_id LE, 16-byte seed, 8-byte padding).
            int count = BitConverter.ToInt32(mSeedDb, 0);
            int pos = 0x10;
            byte[] needle = BitConverter.GetBytes(programId);
            for (int i = 0; i < count && pos + 32 <= mSeedDb.Length; i++)
            {
                bool match = true;
                for (int b = 0; b < 8; b++)
                {
                    if (mSeedDb[pos + b] != needle[b]) { match = false; break; }
                }
                if (match)
                {
                    byte[] seed = new byte[16];
                    Buffer.BlockCopy(mSeedDb, pos + 8, seed, 0, 16);
                    return seed;
                }
                pos += 32;
            }
            return null;
        }

        public static bool IsAvailable
        {
            get
            {
                EnsureLoaded();
                return mKeys.ContainsKey(KeyXName(0x2C));
            }
        }

        private static string KeyXName(int slot) => string.Format("slot0x{0:X2}KeyX", slot);

        private static void EnsureLoaded()
        {
            if (mLoaded) return;
            lock (SyncRoot)
            {
                if (mLoaded) return;
                mLoaded = true;

                string aesPath = ResolveAesKeysPath();
                if (aesPath != null && File.Exists(aesPath))
                {
                    try
                    {
                        foreach (string raw in File.ReadAllLines(aesPath))
                        {
                            string line = raw.Trim();
                            if (line.Length == 0 || line.StartsWith("#") || line.StartsWith("//")) continue;
                            int eq = line.IndexOf('=');
                            if (eq <= 0 || eq == line.Length - 1) continue;
                            string name = line.Substring(0, eq).Trim();
                            string value = line.Substring(eq + 1).Trim();
                            if (value.Length != 32) continue;
                            byte[] bytes = HexToBytes(value);
                            if (bytes != null) mKeys[name] = bytes;
                        }
                        Logger.Log(string.Format("CtrAesKeys: loaded {0} key(s) from {1}.", mKeys.Count, aesPath));
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(string.Format("CtrAesKeys: read failed for {0}: {1}", aesPath, ex.Message), Logger.LogLevel.Exception);
                    }
                }
                else
                {
                    Logger.Log("CtrAesKeys: aes_keys.txt not found (set Ctr3dsKeysPath or place it in Extractors/aes_keys.txt). NCCH decryption disabled.");
                }

                string seedPath = ResolveSeedDbPath();
                if (seedPath != null && File.Exists(seedPath))
                {
                    try
                    {
                        mSeedDb = File.ReadAllBytes(seedPath);
                        Logger.Log(string.Format("CtrAesKeys: loaded seeddb ({0} bytes) from {1}.", mSeedDb.Length, seedPath));
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(string.Format("CtrAesKeys: seeddb read failed for {0}: {1}", seedPath, ex.Message));
                    }
                }
            }
        }

        private static string ResolveAesKeysPath()
        {
            string configured = Config.Ctr3dsKeysPath;
            if (!string.IsNullOrWhiteSpace(configured))
            {
                string abs = PathUtils.GetAbsolutePath(configured);
                if (File.Exists(abs)) return abs;
            }
            string extractors = PathUtils.GetExtractorRootPath();
            foreach (string name in new[] { "aes_keys.txt", "aes_keys.txt.txt" })
            {
                string p = Path.Combine(extractors, name);
                if (File.Exists(p)) return p;
            }
            return null;
        }

        private static string ResolveSeedDbPath()
        {
            string configured = Config.Ctr3dsSeedDbPath;
            if (!string.IsNullOrWhiteSpace(configured))
            {
                string abs = PathUtils.GetAbsolutePath(configured);
                if (File.Exists(abs)) return abs;
            }
            string extractors = PathUtils.GetExtractorRootPath();
            foreach (string name in new[] { "seeddb.bin" })
            {
                string p = Path.Combine(extractors, name);
                if (File.Exists(p)) return p;
            }
            return null;
        }

        private static byte[] HexToBytes(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex) || (hex.Length & 1) != 0) return null;
            byte[] result = new byte[hex.Length / 2];
            for (int i = 0; i < result.Length; i++)
            {
                if (!byte.TryParse(hex.AsSpan(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte b)) return null;
                result[i] = b;
            }
            return result;
        }
    }
}
