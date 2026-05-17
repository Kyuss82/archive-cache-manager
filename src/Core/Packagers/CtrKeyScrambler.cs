/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * 3DS NCCH keyscrambler. Public algorithm, widely documented (Aurora Wright /
 * Decrypt9 / SciresM / Plailect's guide). Constant C is the leaked retail
 * scrambler constant; KeyX is supplied by the user via aes_keys.txt — we never
 * embed it.
 *
 *   NormalKey = rol128(  rol128(KeyX, 2)  XOR  KeyY  +  C , 87 )
 *
 *   C = 0x1FF9E9AAC5FE0408024591DC5D52768A   (16 bytes, big-endian)
 *   +  is 128-bit big-endian addition (mod 2^128)
 *   rol128(x, n)  rotates the 128-bit value left by n bits
 */
using System;

namespace ArchiveCacheManager
{
    internal static class CtrKeyScrambler
    {
        private static readonly byte[] ScramblerConst =
        {
            0x1F, 0xF9, 0xE9, 0xAA, 0xC5, 0xFE, 0x04, 0x08,
            0x02, 0x45, 0x91, 0xDC, 0x5D, 0x52, 0x76, 0x8A,
        };

        /// <summary>Compute NormalKey = rol((rol(KeyX, 2) ^ KeyY) + C, 87).</summary>
        public static byte[] Derive(byte[] keyX, byte[] keyY)
        {
            if (keyX == null || keyX.Length != 16) throw new ArgumentException("KeyX must be 16 bytes.", nameof(keyX));
            if (keyY == null || keyY.Length != 16) throw new ArgumentException("KeyY must be 16 bytes.", nameof(keyY));

            byte[] tmp = Rol128(keyX, 2);
            for (int i = 0; i < 16; i++) tmp[i] ^= keyY[i];
            Add128(tmp, ScramblerConst);
            return Rol128(tmp, 87);
        }

        /// <summary>Rotate <paramref name="src"/> left by <paramref name="n"/> bits as a 128-bit BE value.</summary>
        public static byte[] Rol128(byte[] src, int n)
        {
            n &= 127;
            byte[] result = new byte[16];
            int byteShift = n / 8;
            int bitShift = n % 8;
            for (int i = 0; i < 16; i++)
            {
                int from = (i + byteShift) & 0xF;
                int next = (from + 1) & 0xF;
                int hi = (src[from] << bitShift) & 0xFF;
                int lo = bitShift == 0 ? 0 : (src[next] >> (8 - bitShift));
                result[i] = (byte)(hi | lo);
            }
            return result;
        }

        /// <summary>In-place: dst[0..16] += src[0..16] as a 128-bit BE integer (wrap mod 2^128).</summary>
        private static void Add128(byte[] dst, byte[] src)
        {
            int carry = 0;
            for (int i = 15; i >= 0; i--)
            {
                int sum = dst[i] + src[i] + carry;
                dst[i] = (byte)sum;
                carry = sum >> 8;
            }
        }
    }
}
