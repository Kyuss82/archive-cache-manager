/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Part of the Sony PKG on-launch packager additions (PS3 + PSP/PSV). Same
 * license as the rest of the fork additions (LGPL 2.1 or, at your option, any
 * later version).
 *
 * Constants for retail Sony .pkg decryption (PS3, PSP and PSV variants) and
 * RAP→rifkey derivation. Sources: pkg2zip (mmozeiko, MIT) and the make_npdata
 * import in RPCS3. Both keys and the RAP key schedule are well-known and have
 * been published in numerous open-source PSx tools since 2010+; no proprietary
 * data here.
 *
 * Key selection (see PsPkgReader.SelectAesKey):
 *   pkg_type 0x0001 (PS3)        → Ps3AesKey                  — single retail key
 *   pkg_type 0x0002 (PSP / PSV)  → drm_type from metadata 0x01:
 *                                    2 → PsVita2Key, 3 → PsVita3Key, 4 → PsVita4Key
 *                                    anything else (1, 0xD, 0xE, 0xF, …) → PspKey
 */
using System;

namespace ArchiveCacheManager
{
    internal static class PsPkgKeys
    {
        /// <summary>AES-128-CTR key for retail PS3 .pkg files (pkg_type = 0x0001).</summary>
        public static readonly byte[] Ps3AesKey =
        {
            0x2E, 0x7B, 0x71, 0xD7, 0xC9, 0xC9, 0xA1, 0x4E,
            0xA3, 0x22, 0x1F, 0x18, 0x88, 0x28, 0xB8, 0xF8
        };

        /// <summary>AES-128-CTR key for retail PSP .pkg files (PSP minis, PSN demos, classic PS1 EBOOTs).</summary>
        public static readonly byte[] PspKey =
        {
            0x07, 0xF2, 0xC6, 0x82, 0x90, 0xB5, 0x0D, 0x2C,
            0x33, 0x81, 0x8D, 0x70, 0x9B, 0x60, 0xE6, 0x2B
        };

        /// <summary>PS Vita PKG key variant 2 (drm_type = 2).</summary>
        public static readonly byte[] PsVita2Key =
        {
            0xE3, 0x1A, 0x70, 0xC9, 0xCE, 0x1D, 0xD7, 0x2B,
            0xF3, 0xC0, 0x62, 0x29, 0x63, 0xF2, 0xEC, 0xCB
        };

        /// <summary>PS Vita PKG key variant 3 (drm_type = 3).</summary>
        public static readonly byte[] PsVita3Key =
        {
            0x42, 0x3A, 0xCA, 0x3A, 0x2B, 0xD5, 0x64, 0x9F,
            0x96, 0x86, 0xAB, 0xAD, 0x6F, 0xD8, 0x80, 0x1F
        };

        /// <summary>PS Vita PKG key variant 4 (drm_type = 4).</summary>
        public static readonly byte[] PsVita4Key =
        {
            0xAF, 0x07, 0xFD, 0x59, 0x65, 0x25, 0x27, 0xBA,
            0xF1, 0x33, 0x89, 0x66, 0x8B, 0x17, 0xD9, 0xEA
        };

        /// <summary>AES-128-ECB key used as the first step of rap→rifkey.</summary>
        public static readonly byte[] RapInitialKey =
        {
            0x86, 0x9F, 0x77, 0x45, 0xC1, 0x3F, 0xD8, 0x90,
            0xCC, 0xF2, 0x91, 0x88, 0xE3, 0xCC, 0x3E, 0xDF
        };

        /// <summary>Permutation box for rap→rifkey post-AES rounds.</summary>
        public static readonly byte[] RapPBox =
        {
            0x0C, 0x03, 0x06, 0x04, 0x01, 0x0B, 0x0F, 0x08,
            0x02, 0x07, 0x00, 0x05, 0x0A, 0x0E, 0x0D, 0x09
        };

        /// <summary>XOR table applied per byte during each rap→rifkey round.</summary>
        public static readonly byte[] RapE1 =
        {
            0xA9, 0x3E, 0x1F, 0xD6, 0x7C, 0x55, 0xA3, 0x29,
            0xB7, 0x5F, 0xDD, 0xA6, 0x2A, 0xA3, 0x5A, 0xA1
        };

        /// <summary>Subtraction table (mod 256, with borrow) applied per byte during each rap→rifkey round.</summary>
        public static readonly byte[] RapE2 =
        {
            0x67, 0xD4, 0x5D, 0xA3, 0x29, 0x6D, 0x00, 0x6A,
            0x4E, 0x7C, 0x53, 0x7B, 0x86, 0x95, 0xB1, 0xC0
        };
    }
}
