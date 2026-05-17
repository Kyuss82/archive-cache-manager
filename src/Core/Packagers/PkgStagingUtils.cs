/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Shared pure-utility helpers extracted from the three Sony PKG staging
 * pipelines (`Ps3PkgStaging`, `PspPkgStaging`, `PsvPkgStaging`). Each was
 * carrying its own private copy of these three byte-identical helpers, so a
 * fix in one didn't propagate to the others. The orchestration code itself
 * stays in each platform's own class — only these three pure functions move
 * here. The platform-specific bits (`PspPkgPathTransform`, exdata/memstick
 * resolution, zRIF→.rif conversion, install root layout) intentionally stay
 * in their respective files: they are NOT shared and were diverging in
 * load-bearing ways across platforms.
 */
using System;
using System.IO;

namespace ArchiveCacheManager
{
    public static class PkgStagingUtils
    {
        /// <summary>
        /// Parses an even-length hex string into a byte array. Returns null for any
        /// malformed input (null, whitespace, odd length, non-hex character). Used by
        /// the PS3/PSP staging classes when reading `.rap` hex from the NPS DB.
        /// </summary>
        public static byte[] HexToBytes(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) return null;
            hex = hex.Trim();
            if ((hex.Length & 1) != 0) return null;
            byte[] result = new byte[hex.Length / 2];
            for (int i = 0; i < result.Length; i++)
            {
                if (!byte.TryParse(hex.AsSpan(i * 2, 2), System.Globalization.NumberStyles.HexNumber,
                                   System.Globalization.CultureInfo.InvariantCulture, out byte b)) return null;
                result[i] = b;
            }
            return result;
        }

        /// <summary>Returns true iff `path` has a `.pkg` extension (case-insensitive).</summary>
        public static bool LooksLikePkg(string path)
        {
            return Path.GetExtension(path).Equals(".pkg", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Best-effort `Directory.Delete(path, recursive: true)` that swallows + logs any
        /// exception (file in use, AV scanner holding a handle, etc.). The `caller` tag
        /// goes into the log line so it's clear which staging pipeline emitted the warning.
        /// </summary>
        public static void TryDeleteDirectory(string path, string caller = "PkgStagingUtils")
        {
            try
            {
                if (Directory.Exists(path)) Directory.Delete(path, true);
            }
            catch (Exception ex)
            {
                Logger.Log(string.Format("{0}: failed to delete {1}: {2}", caller, path, ex.Message));
            }
        }
    }
}
