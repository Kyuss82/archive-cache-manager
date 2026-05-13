/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Part of the Wii U / 3DS / Wii / DSi on-launch packager additions to the
 * Archive Cache Manager plugin (original by fraganator, LGPL 2.1). This file
 * is licensed under LGPL 2.1 or (at your option) any later version.
 */
using System;
using System.IO;

namespace ArchiveCacheManager
{
    /// <summary>
    /// Local Wii title-key database. Format is identical to the 3DS one used by
    /// matiffeder/TikGenerator: pairs of (8-byte title ID, 16-byte AES-encrypted
    /// title key) embedded in a binary blob, possibly with framing bytes between
    /// entries. We just scan the blob for the title-ID byte sequence and return
    /// the 16 bytes that follow.
    /// </summary>
    public static class WiiTitleKeys
    {
        private static byte[] mDb;
        private static bool mLoadAttempted;
        private static readonly object SyncRoot = new object();

        /// <summary>
        /// Returns the *encrypted* title key (16 bytes) for the given Wii title ID,
        /// or null if not present. The caller forges a ticket via WiiTicketBuilder.
        /// </summary>
        public static byte[] Lookup(string titleIdHex)
        {
            if (string.IsNullOrWhiteSpace(titleIdHex)) return null;

            EnsureLoaded();
            if (mDb == null) return null;

            byte[] tid = HexToBytes(titleIdHex.PadLeft(16, '0'));
            int idx = IndexOf(mDb, tid, 0);
            if (idx < 0 || idx + 8 + 16 > mDb.Length) return null;

            byte[] key = new byte[16];
            Buffer.BlockCopy(mDb, idx + 8, key, 0, 16);
            return key;
        }

        private static void EnsureLoaded()
        {
            if (mLoadAttempted) return;
            lock (SyncRoot)
            {
                if (mLoadAttempted) return;
                mLoadAttempted = true;

                string path = ResolvePath();
                if (path == null)
                {
                    Logger.Log("Wii title keys: wii-titlekeys.bin not found (set WadTitleKeysPath or place it in Extractors/).");
                    return;
                }

                try
                {
                    mDb = File.ReadAllBytes(path);
                    Logger.Log(string.Format("Wii title keys: loaded {0} bytes from {1}.", mDb.Length, path));
                }
                catch (Exception ex)
                {
                    Logger.Log(string.Format("Wii title keys: read failed for {0}: {1}", path, ex.Message), Logger.LogLevel.Exception);
                    mDb = null;
                }
            }
        }

        private static string ResolvePath()
        {
            string configured = Config.WadTitleKeysPath;
            if (!string.IsNullOrWhiteSpace(configured))
            {
                string abs = PathUtils.GetAbsolutePath(configured);
                if (File.Exists(abs)) return abs;
            }

            string extractors = PathUtils.GetExtractorRootPath();
            foreach (string name in new[] { "wii-titlekeys.bin", "wii_titlekeys.bin", "titlekeys.bin" })
            {
                string p = Path.Combine(extractors, name);
                if (File.Exists(p)) return p;
            }
            return null;
        }

        private static int IndexOf(byte[] haystack, byte[] needle, int start)
        {
            int end = haystack.Length - needle.Length;
            for (int i = start; i <= end; i++)
            {
                bool match = true;
                for (int j = 0; j < needle.Length; j++)
                {
                    if (haystack[i + j] != needle[j]) { match = false; break; }
                }
                if (match) return i;
            }
            return -1;
        }

        private static byte[] HexToBytes(string hex)
        {
            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return bytes;
        }
    }
}
