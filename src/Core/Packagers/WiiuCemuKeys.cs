/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Part of the Wii U / 3DS / Wii / DSi on-launch packager additions to the
 * Archive Cache Manager plugin (original by fraganator, LGPL 2.1). This file
 * is licensed under LGPL 2.1 or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace ArchiveCacheManager
{
    /// <summary>
    /// Local title-key source backed by Cemu's keys.txt. Each line of interest has the form
    /// "&lt;16 hex TID&gt;  &lt;32 hex encrypted title key&gt;  # comment" (tabs or spaces, '#' may be glued
    /// to the key with no separating space). Loaded once per session and cached in memory.
    /// </summary>
    public static class WiiuCemuKeys
    {
        private static Dictionary<string, byte[]> mCache;
        private static bool mLoadAttempted;
        private static readonly object SyncRoot = new object();

        private static readonly Regex EntryRegex = new Regex(
            @"^\s*([0-9a-fA-F]{16})\s+([0-9a-fA-F]{32})",
            RegexOptions.Compiled);

        /// <summary>
        /// Returns the *encrypted* title key (16 bytes) for the given title ID, or null if absent.
        /// Caller decrypts with the Wii U common key.
        /// </summary>
        public static byte[] Lookup(string titleIdHex)
        {
            if (string.IsNullOrWhiteSpace(titleIdHex)) return null;

            EnsureLoaded();
            if (mCache == null) return null;

            string key = titleIdHex.ToLowerInvariant().PadLeft(16, '0');
            return mCache.TryGetValue(key, out byte[] bytes) ? bytes : null;
        }

        private static void EnsureLoaded()
        {
            if (mLoadAttempted) return;
            lock (SyncRoot)
            {
                if (mLoadAttempted) return;
                mLoadAttempted = true;

                string path = ResolveKeysPath();
                if (path == null)
                {
                    Logger.Log("Wii U Cemu keys: path not configured and auto-detect found nothing.");
                    return;
                }

                if (!File.Exists(path))
                {
                    Logger.Log(string.Format("Wii U Cemu keys: file not found at {0}.", path));
                    return;
                }

                try
                {
                    mCache = Parse(File.ReadAllLines(path));
                    Logger.Log(string.Format("Wii U Cemu keys: loaded {0} title keys from {1}.", mCache.Count, path));
                }
                catch (Exception ex)
                {
                    Logger.Log(string.Format("Wii U Cemu keys: parse failed for {0}: {1}", path, ex.Message), Logger.LogLevel.Exception);
                    mCache = null;
                }
            }
        }

        private static string ResolveKeysPath()
        {
            string configured = Config.WiiuCemuKeysPath;
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return PathUtils.GetAbsolutePath(configured);
            }

            // Auto-detect standard Cemu install location on Windows.
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                if (!string.IsNullOrEmpty(appData))
                {
                    string candidate = Path.Combine(appData, "Cemu", "keys.txt");
                    if (File.Exists(candidate)) return candidate;
                }
            }
            catch { }

            return null;
        }

        private static Dictionary<string, byte[]> Parse(string[] lines)
        {
            var map = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            foreach (string raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;

                // Trim comment (anything after '#', whether glued to a key or not).
                int hash = raw.IndexOf('#');
                string body = hash >= 0 ? raw.Substring(0, hash) : raw;

                var m = EntryRegex.Match(body);
                if (!m.Success) continue;

                string tid = m.Groups[1].Value.ToLowerInvariant();
                string keyHex = m.Groups[2].Value;
                try
                {
                    map[tid] = HexToBytes(keyHex);
                }
                catch
                {
                    // Malformed hex — skip silently.
                }
            }
            return map;
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
