using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace ArchiveCacheManager
{
    public class WiiuTmdContent
    {
        public uint Id;
        public ushort Index;
        public ushort Type;
        public ulong Size;
        public byte[] Hash;
    }

    public class WiiuParsedTmd
    {
        public ulong TitleId;
        public byte Version;
        public ushort TitleVersion;
        public List<WiiuTmdContent> Contents;
        /// <summary>Offset of the first byte after the body+content_table (i.e., size of TMD without trailing cert chain).</summary>
        public int BodySize;
    }

    public static class WiiuTmd
    {
        private const byte TmdVersionWii = 0x00;
        private const byte TmdVersionWiiu = 0x01;
        private const int ContentTypeHashed = 0x02;
        private const int BlockSizeHashed = 0x10000;
        private const int HashBlockSize = 0xFC00;
        private const int HashesSize = 0x0400;
        private const int HashEntrySize = 0x14;

        public static WiiuParsedTmd Parse(string tmdPath)
        {
            byte[] data = File.ReadAllBytes(tmdPath);
            if (data.Length < 0x1E0) throw new InvalidDataException("TMD too small");

            byte version = data[0x180];
            int contentStart, stride, hashSize;
            if (version == TmdVersionWii)
            {
                contentStart = 0x1E4; stride = 0x24; hashSize = 0x14;
            }
            else if (version == TmdVersionWiiu)
            {
                contentStart = 0xB04; stride = 0x30; hashSize = 0x20;
            }
            else
            {
                throw new InvalidDataException(string.Format("Unknown TMD version: {0}", version));
            }

            ulong titleId = BinaryPrimitives.ReadUInt64BigEndian(new ReadOnlySpan<byte>(data, 0x18C, 8));
            ushort titleVersion = BinaryPrimitives.ReadUInt16BigEndian(new ReadOnlySpan<byte>(data, 0x1DC, 2));
            ushort contentCount = BinaryPrimitives.ReadUInt16BigEndian(new ReadOnlySpan<byte>(data, 0x1DE, 2));

            var contents = new List<WiiuTmdContent>(contentCount);
            for (int i = 0; i < contentCount; i++)
            {
                int off = contentStart + stride * i;
                if (off + 16 + hashSize > data.Length)
                    throw new InvalidDataException("TMD content table truncated");

                var c = new WiiuTmdContent
                {
                    Id = BinaryPrimitives.ReadUInt32BigEndian(new ReadOnlySpan<byte>(data, off, 4)),
                    Index = BinaryPrimitives.ReadUInt16BigEndian(new ReadOnlySpan<byte>(data, off + 4, 2)),
                    Type = BinaryPrimitives.ReadUInt16BigEndian(new ReadOnlySpan<byte>(data, off + 6, 2)),
                    Size = BinaryPrimitives.ReadUInt64BigEndian(new ReadOnlySpan<byte>(data, off + 8, 8)),
                    Hash = new byte[hashSize]
                };
                Buffer.BlockCopy(data, off + 16, c.Hash, 0, hashSize);
                contents.Add(c);
            }

            return new WiiuParsedTmd
            {
                TitleId = titleId,
                Version = version,
                TitleVersion = titleVersion,
                Contents = contents,
                BodySize = contentStart + stride * contentCount
            };
        }

        public class VerificationTarget
        {
            public WiiuTmdContent Content;
            public string AppPath;
            public string H3Path; // null if non-hashed
        }

        public static VerificationTarget ChooseTarget(string workDir, WiiuParsedTmd tmd)
        {
            // Pick the target with the lowest "verification cost":
            //   non-hashed: cost = full content size (we read & decrypt all of it for SHA1)
            //   hashed:     cost = min(size, 0x10000) (only first block is needed for H0 check)
            // At equal cost, prefer non-hashed: H0 hashes are sometimes stale on re-encrypted archives,
            // whereas the SHA1 stored in TMD for non-hashed content is the ground truth.
            VerificationTarget best = null;
            long bestCost = long.MaxValue;
            int bestTypePriority = int.MaxValue; // lower = better (non-hashed=0, hashed=1)

            foreach (var content in tmd.Contents)
            {
                string appPath = FindFileCaseInsensitive(workDir, string.Format("{0:X8}.app", content.Id));
                if (appPath == null) continue;

                bool isHashed = (content.Type & ContentTypeHashed) != 0;
                string h3 = null;
                if (isHashed)
                {
                    h3 = FindFileCaseInsensitive(workDir, Path.GetFileNameWithoutExtension(appPath) + ".h3");
                    if (h3 == null) continue;
                }

                long cost = isHashed
                    ? Math.Min((long)content.Size, BlockSizeHashed)
                    : (long)content.Size;
                int typePriority = isHashed ? 1 : 0;

                if (cost < bestCost || (cost == bestCost && typePriority < bestTypePriority))
                {
                    best = new VerificationTarget { Content = content, AppPath = appPath, H3Path = h3 };
                    bestCost = cost;
                    bestTypePriority = typePriority;
                }
            }

            return best;
        }

        public static bool VerifyTitleKey(VerificationTarget target, byte[] titleKey)
        {
            if (target == null || titleKey == null || titleKey.Length != 16) return false;
            return target.H3Path != null
                ? VerifyHashed(target.AppPath, target.H3Path, target.Content, titleKey)
                : VerifyNonHashed(target.AppPath, target.Content, titleKey);
        }

        private static bool VerifyHashed(string appPath, string h3Path, WiiuTmdContent content, byte[] titleKey)
        {
            byte[] h3Data;
            try { h3Data = File.ReadAllBytes(h3Path); }
            catch { return false; }

            using (var sha = SHA1.Create())
            {
                byte[] h3Hash = sha.ComputeHash(h3Data);
                if (!FirstBytesEqual(h3Hash, content.Hash, 20)) return false;
            }

            byte[] block = new byte[BlockSizeHashed];
            using (var fs = File.OpenRead(appPath))
            {
                int read = fs.Read(block, 0, BlockSizeHashed);
                if (read < HashesSize + 16 || (read % 16) != 0) return false;
            }

            byte[] hashesEnc = new byte[HashesSize];
            byte[] bodyEnc = new byte[BlockSizeHashed - HashesSize];
            Buffer.BlockCopy(block, 0, hashesEnc, 0, HashesSize);
            Buffer.BlockCopy(block, HashesSize, bodyEnc, 0, bodyEnc.Length);

            byte[] zeroIv = new byte[16];
            byte[] hashes = AesCbcDecrypt(titleKey, zeroIv, hashesEnc);

            byte[] h0Hash = new byte[HashEntrySize];
            Buffer.BlockCopy(hashes, 0, h0Hash, 0, HashEntrySize);

            byte[] ivBlock = new byte[16];
            Buffer.BlockCopy(hashes, 0, ivBlock, 0, 16);
            ivBlock[1] ^= (byte)(content.Index & 0xFF);

            byte[] bodyDec = AesCbcDecrypt(titleKey, ivBlock, bodyEnc);

            using (var sha = SHA1.Create())
            {
                byte[] calc = sha.ComputeHash(bodyDec, 0, HashBlockSize);
                calc[1] ^= (byte)(content.Index & 0xFF);
                return FirstBytesEqual(calc, h0Hash, 20);
            }
        }

        private static bool VerifyNonHashed(string appPath, WiiuTmdContent content, byte[] titleKey)
        {
            byte[] iv = new byte[16];
            iv[0] = (byte)((content.Index >> 8) & 0xFF);
            iv[1] = (byte)(content.Index & 0xFF);

            using (var aes = Aes.Create())
            {
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.None;
                aes.Key = titleKey;
                aes.IV = iv;
                using (var decryptor = aes.CreateDecryptor())
                using (var sha = SHA1.Create())
                using (var fs = File.OpenRead(appPath))
                {
                    long leftPlain = (long)content.Size;
                    const int chunkPlainMax = 8 * 1024 * 1024;
                    byte[] cipherBuf = new byte[chunkPlainMax];
                    byte[] plainBuf = new byte[chunkPlainMax];

                    while (leftPlain > 0)
                    {
                        int toPlain = (int)Math.Min(chunkPlainMax, leftPlain);
                        int toRead = (toPlain + 15) & ~15;
                        int got = fs.Read(cipherBuf, 0, toRead);
                        if (got != toRead) return false;

                        int produced = decryptor.TransformBlock(cipherBuf, 0, toRead, plainBuf, 0);
                        sha.TransformBlock(plainBuf, 0, toPlain, null, 0);
                        leftPlain -= toPlain;
                    }

                    sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                    return FirstBytesEqual(sha.Hash, content.Hash, 20);
                }
            }
        }

        private static byte[] AesCbcDecrypt(byte[] key, byte[] iv, byte[] data)
        {
            using (var aes = Aes.Create())
            {
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.None;
                aes.Key = key;
                aes.IV = iv;
                using (var dec = aes.CreateDecryptor())
                {
                    return dec.TransformFinalBlock(data, 0, data.Length);
                }
            }
        }

        private static bool FirstBytesEqual(byte[] a, byte[] b, int n)
        {
            if (a == null || b == null || a.Length < n || b.Length < n) return false;
            for (int i = 0; i < n; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }

        private static string FindFileCaseInsensitive(string dir, string name)
        {
            try
            {
                foreach (string p in Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly))
                {
                    if (string.Equals(Path.GetFileName(p), name, StringComparison.OrdinalIgnoreCase))
                        return p;
                }
            }
            catch { }
            return null;
        }
    }
}
