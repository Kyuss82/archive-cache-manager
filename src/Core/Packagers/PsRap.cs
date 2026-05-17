/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * RAP → rifkey conversion, ported from make_npdata (RPCS3 import,
 * https://github.com/RPCS3/rpcs3/blob/master/rpcs3/Crypto/key_vault.cpp and
 * https://github.com/RPCS3/rpcs3/blob/master/rpcs3/Crypto/utils.cpp). The
 * permutation box, key tables and round structure are public algorithms
 * documented in numerous open-source PS3/PSP/PSV toolchains since 2010+.
 * The same rifkey derivation is used for RPCS3 (dev_hdd0/home/00000001/exdata/)
 * and for PPSSPP (PSP/LICENSE/), so this helper is platform-agnostic.
 *
 * Algorithm:
 *   1. AES-128-ECB-decrypt the 16-byte RAP with RAP_INITIAL_KEY → key
 *   2. Repeat 5 times:
 *        a. for i in 0..15: key[PBOX[i]] ^= E1[PBOX[i]]
 *        b. for i in 15..1:  key[PBOX[i]] ^= key[PBOX[i-1]]
 *        c. carry-aware subtract E2[PBOX[i]] from key[PBOX[i]] across all 16 bytes
 *   3. The resulting 16 bytes are the rifkey for that content.
 *
 * Note on RIF emission: RPCS3 will read a RAP placed in dev_hdd0/home/00000001/exdata/
 * directly and derive the rifkey internally (verify_npdrm_self_headers → get_npdrm_decryptor),
 * so the simplest pipeline is to copy <content_id>.rap into exdata/ and skip RIF generation.
 * We still expose BuildRif for callers that want to drop a fully-formed .rif as well.
 */
using System;
using System.Security.Cryptography;
using System.Text;

namespace ArchiveCacheManager
{
    public static class PsRap
    {
        public const int RapSize = 16;
        public const int RifSize = 0x98;          // 152 bytes — the v1 layout RPCS3 recognises
        public const int ContentIdSize = 48;

        /// <summary>
        /// Derive the per-content rifkey from a 16-byte RAP. The first 16 bytes of the RAP
        /// file are consumed; anything past that (some scene RAPs are padded to 152 or 1024
        /// bytes) is ignored.
        /// </summary>
        public static byte[] RapToRifKey(byte[] rap)
        {
            if (rap == null || rap.Length < RapSize)
                throw new ArgumentException("RAP must be at least 16 bytes.", nameof(rap));

            byte[] key = AesEcbDecryptBlock(PsPkgKeys.RapInitialKey, rap, 0);

            for (int round = 0; round < 5; round++)
            {
                for (int i = 0; i < 16; i++)
                {
                    int p = PsPkgKeys.RapPBox[i];
                    key[p] ^= PsPkgKeys.RapE1[p];
                }

                for (int i = 15; i >= 1; i--)
                {
                    int p  = PsPkgKeys.RapPBox[i];
                    int pp = PsPkgKeys.RapPBox[i - 1];
                    key[p] ^= key[pp];
                }

                int borrow = 0;
                for (int i = 0; i < 16; i++)
                {
                    int p = PsPkgKeys.RapPBox[i];
                    int kv = key[p] - PsPkgKeys.RapE2[p] - borrow;
                    if (kv < 0)
                    {
                        kv += 0x100;
                        borrow = 1;
                    }
                    else
                    {
                        borrow = 0;
                    }
                    key[p] = (byte)kv;
                }
            }

            return key;
        }

        /// <summary>
        /// Build a 152-byte "any-account, free-licence" RIF that carries the given content ID
        /// and the cleartext rifkey. This is the form RPCS3 accepts for non-network-tied content
        /// when no act.dat is present; for licences tied to a specific PSN account a full RIF
        /// signed against the user's idps would be required, which we cannot forge without
        /// per-console keys.
        /// </summary>
        public static byte[] BuildRif(string contentId, byte[] rifKey)
        {
            if (string.IsNullOrEmpty(contentId)) throw new ArgumentException("contentId is required.", nameof(contentId));
            if (rifKey == null || rifKey.Length != 16) throw new ArgumentException("rifKey must be 16 bytes.", nameof(rifKey));

            byte[] rif = new byte[RifSize];

            // version (BE uint32) = 1
            rif[0x00] = 0x00; rif[0x01] = 0x00; rif[0x02] = 0x00; rif[0x03] = 0x01;
            // licence type (BE uint32) = 0x00010002 — local, free
            rif[0x04] = 0x00; rif[0x05] = 0x01; rif[0x06] = 0x00; rif[0x07] = 0x02;
            // account_id (BE uint64) = 0 — "any account"
            // (already zeroed)

            byte[] cidBytes = Encoding.ASCII.GetBytes(contentId);
            Buffer.BlockCopy(cidBytes, 0, rif, 0x10, Math.Min(cidBytes.Length, ContentIdSize));

            // rifkey at 0x50 — stored cleartext for free / any-account licences.
            Buffer.BlockCopy(rifKey, 0, rif, 0x50, 16);

            return rif;
        }

        private static byte[] AesEcbDecryptBlock(byte[] key, byte[] data, int offset)
        {
            using (var aes = Aes.Create())
            {
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.None;
                aes.Key = key;
                using (var dec = aes.CreateDecryptor())
                {
                    byte[] outBuf = new byte[16];
                    dec.TransformBlock(data, offset, 16, outBuf, 0);
                    return outBuf;
                }
            }
        }
    }
}
