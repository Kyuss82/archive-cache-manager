/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * AES-128 in counter mode with a big-endian 128-bit counter, sized for arbitrary
 * random-access decryption of a Sony .pkg's encrypted region (PS3, PSP and PSV
 * all share this construction; only the AES key differs). .NET's Aes class
 * doesn't ship a CTR mode, so the keystream is produced manually by encrypting
 * (iv + block_index) under ECB and XORing the buffer in place.
 *
 * Layout note: the caller passes <em>offset within the encrypted region</em> — i.e.
 * the byte at the start of pkg.data_offset corresponds to ctr offset 0. The 128-bit
 * counter is interpreted big-endian; carry propagates from byte 15 toward byte 0.
 */
using System;
using System.Security.Cryptography;

namespace ArchiveCacheManager
{
    internal sealed class PsPkgAesCtr : IDisposable
    {
        private readonly Aes _aes;
        private readonly ICryptoTransform _encryptor;
        private readonly byte[] _iv;
        private readonly byte[] _ctr = new byte[16];
        private readonly byte[] _keystream = new byte[16];

        public PsPkgAesCtr(byte[] key, byte[] iv)
        {
            if (key == null || key.Length != 16) throw new ArgumentException("AES key must be 16 bytes.");
            if (iv == null || iv.Length != 16) throw new ArgumentException("IV must be 16 bytes.");

            _aes = Aes.Create();
            _aes.Mode = CipherMode.ECB;
            _aes.Padding = PaddingMode.None;
            _aes.Key = key;
            _encryptor = _aes.CreateEncryptor();
            _iv = (byte[])iv.Clone();
        }

        /// <summary>
        /// XORs the keystream into <paramref name="buffer"/>[<paramref name="bufOffset"/>..+<paramref name="count"/>],
        /// where the first byte sits at absolute offset <paramref name="dataOffset"/> inside the encrypted region.
        /// Caller must have already loaded the ciphertext into the buffer.
        /// </summary>
        public void Decrypt(ulong dataOffset, byte[] buffer, int bufOffset, int count)
        {
            ulong blockIndex = dataOffset / 16;
            int blockSubOffset = (int)(dataOffset % 16);

            while (count > 0)
            {
                SetCounter(blockIndex);
                _encryptor.TransformBlock(_ctr, 0, 16, _keystream, 0);

                int toCopy = Math.Min(16 - blockSubOffset, count);
                for (int i = 0; i < toCopy; i++)
                {
                    buffer[bufOffset + i] ^= _keystream[blockSubOffset + i];
                }

                bufOffset += toCopy;
                count -= toCopy;
                blockIndex++;
                blockSubOffset = 0;
            }
        }

        private void SetCounter(ulong blockIndex)
        {
            Buffer.BlockCopy(_iv, 0, _ctr, 0, 16);

            int carry = 0;
            // Bottom 64 bits: add blockIndex byte-by-byte from LSB to MSB.
            for (int i = 0; i < 8; i++)
            {
                int byteIdx = 15 - i;
                int add = _ctr[byteIdx] + (int)((blockIndex >> (i * 8)) & 0xFF) + carry;
                _ctr[byteIdx] = (byte)add;
                carry = add >> 8;
            }
            // Propagate carry into the top 64 bits.
            for (int i = 7; i >= 0 && carry > 0; i--)
            {
                int add = _ctr[i] + carry;
                _ctr[i] = (byte)add;
                carry = add >> 8;
            }
        }

        public void Dispose()
        {
            _encryptor.Dispose();
            _aes.Dispose();
        }
    }
}
