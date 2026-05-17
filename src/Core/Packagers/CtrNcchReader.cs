/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Parser for 3DS NCSD (top-level .3ds / .cci) + per-partition NCCH headers.
 * References:
 *   https://www.3dbrew.org/wiki/NCSD
 *   https://www.3dbrew.org/wiki/NCCH
 *
 * NCSD header (0x200 bytes, all little-endian unless noted):
 *   0x000 [256]  RSA-2048 signature
 *   0x100 [4]    magic "NCSD"
 *   0x104 [4]    media size (in 0x200-byte media units)
 *   0x108 [8]    media id
 *   0x110 [8]    partition fs types
 *   0x118 [8]    partition crypt types
 *   0x120 [64]   partition table — 8 entries × (uint32 offset, uint32 size), in media units
 *   …
 *
 * NCCH header (0x200 bytes, all little-endian unless noted):
 *   0x000 [256]  RSA-2048 signature  (first 16 bytes = KeyY for primary crypto)
 *   0x100 [4]    magic "NCCH"
 *   0x104 [4]    content size (media units)
 *   0x108 [8]    partition id (used as IV base for AES-CTR)
 *   0x118 [8]    program id
 *   0x150 [16]   product code
 *   0x180 [4]    extended header size
 *   0x188 [8]    flags
 *                 flags[3] = crypto method  (0x00=initial / 0x01=Secure2 / 0x0A=Secure3 / 0x0B=Secure4)
 *                 flags[5] = content type
 *                 flags[7] bits:
 *                   bit 0 (0x01) FixedCryptoKey
 *                   bit 2 (0x04) NoCrypto
 *                   bit 5 (0x20) SeedCrypto
 *   0x190 [4]    plain region offset (media units)
 *   0x194 [4]    plain region size
 *   0x1A0 [4]    ExeFS offset (media units, relative to NCCH start)
 *   0x1A4 [4]    ExeFS size
 *   0x1B0 [4]    RomFS offset
 *   0x1B4 [4]    RomFS size
 */
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;

namespace ArchiveCacheManager
{
    public class CtrNcsdPartition
    {
        public int Index;
        public long FileOffset;     // absolute byte offset in the .3ds file
        public long ByteSize;
        public CtrNcchHeader Ncch;  // null if partition empty / NCCH magic missing
    }

    public class CtrNcchHeader
    {
        public byte[] KeyY;                  // 16 bytes — first 16 of NCCH signature
        public ulong PartitionId;            // LE uint64 from header offset 0x108
        public ulong ProgramId;              // LE uint64 from header offset 0x118
        public byte CryptoMethod;            // flags[3]
        public byte Flags7;                  // flags[7]
        public uint ExHeaderSize;
        public uint ExeFsOffsetMediaUnits;
        public uint ExeFsSizeMediaUnits;
        public uint RomFsOffsetMediaUnits;
        public uint RomFsSizeMediaUnits;

        public bool IsNoCrypto    => (Flags7 & 0x04) != 0;
        public bool IsFixedKey    => (Flags7 & 0x01) != 0;
        public bool IsSeedCrypto  => (Flags7 & 0x20) != 0;

        /// <summary>Slot to look up KeyX for the secondary section (RomFS + ExeFS code parts).</summary>
        public int SecondaryKeyXSlot()
        {
            switch (CryptoMethod)
            {
                case 0x00: return 0x2C; // initial
                case 0x01: return 0x25; // Secure2 (7.x)
                case 0x0A: return 0x18; // Secure3 (9.3+)
                case 0x0B: return 0x1B; // Secure4 (9.6+)
                default:   return 0x2C;
            }
        }
    }

    public class CtrParsedNcsd
    {
        public List<CtrNcsdPartition> Partitions = new List<CtrNcsdPartition>();
        public long FileSize;
    }

    public static class CtrNcchReader
    {
        private const int MediaUnit = 0x200;
        private const int HeaderSize = 0x200;
        private static readonly byte[] NcsdMagic = { (byte)'N', (byte)'C', (byte)'S', (byte)'D' };
        private static readonly byte[] NcchMagic = { (byte)'N', (byte)'C', (byte)'C', (byte)'H' };

        public static CtrParsedNcsd Parse(string path)
        {
            using (var fs = File.OpenRead(path))
            {
                return Parse(fs);
            }
        }

        public static CtrParsedNcsd Parse(Stream s)
        {
            if (s == null) throw new ArgumentNullException(nameof(s));
            if (!s.CanSeek) throw new ArgumentException("Stream must be seekable.", nameof(s));

            var result = new CtrParsedNcsd { FileSize = s.Length };
            byte[] ncsd = ReadExact(s, 0, HeaderSize);

            for (int i = 0; i < 4; i++)
            {
                if (ncsd[0x100 + i] != NcsdMagic[i])
                    throw new InvalidDataException("Not an NCSD container (bad magic at 0x100).");
            }

            // Partition table: 8 entries at 0x120, each (uint32 offset, uint32 size) in media units.
            for (int p = 0; p < 8; p++)
            {
                int off = 0x120 + p * 8;
                uint partOffUnits = BinaryPrimitives.ReadUInt32LittleEndian(ncsd.AsSpan(off, 4));
                uint partSizeUnits = BinaryPrimitives.ReadUInt32LittleEndian(ncsd.AsSpan(off + 4, 4));
                if (partOffUnits == 0 || partSizeUnits == 0) continue;

                long partOff = (long)partOffUnits * MediaUnit;
                long partSize = (long)partSizeUnits * MediaUnit;
                if (partOff + HeaderSize > s.Length) continue;

                var partition = new CtrNcsdPartition
                {
                    Index = p,
                    FileOffset = partOff,
                    ByteSize = partSize,
                };

                byte[] ncchHdr = ReadExact(s, partOff, HeaderSize);
                bool magicOk = true;
                for (int i = 0; i < 4; i++)
                {
                    if (ncchHdr[0x100 + i] != NcchMagic[i]) { magicOk = false; break; }
                }
                if (magicOk)
                {
                    partition.Ncch = ParseNcchHeader(ncchHdr);
                }
                result.Partitions.Add(partition);
            }

            if (result.Partitions.Count == 0)
                throw new InvalidDataException("NCSD has no readable partitions.");
            return result;
        }

        internal static CtrNcchHeader ParseNcchHeader(byte[] hdr)
        {
            var n = new CtrNcchHeader
            {
                KeyY = hdr.AsSpan(0x00, 16).ToArray(),
                PartitionId          = BinaryPrimitives.ReadUInt64LittleEndian(hdr.AsSpan(0x108, 8)),
                ProgramId            = BinaryPrimitives.ReadUInt64LittleEndian(hdr.AsSpan(0x118, 8)),
                ExHeaderSize         = BinaryPrimitives.ReadUInt32LittleEndian(hdr.AsSpan(0x180, 4)),
                ExeFsOffsetMediaUnits = BinaryPrimitives.ReadUInt32LittleEndian(hdr.AsSpan(0x1A0, 4)),
                ExeFsSizeMediaUnits   = BinaryPrimitives.ReadUInt32LittleEndian(hdr.AsSpan(0x1A4, 4)),
                RomFsOffsetMediaUnits = BinaryPrimitives.ReadUInt32LittleEndian(hdr.AsSpan(0x1B0, 4)),
                RomFsSizeMediaUnits   = BinaryPrimitives.ReadUInt32LittleEndian(hdr.AsSpan(0x1B4, 4)),
                CryptoMethod = hdr[0x188 + 3],
                Flags7       = hdr[0x188 + 7],
            };
            return n;
        }

        private static byte[] ReadExact(Stream s, long offset, int count)
        {
            s.Position = offset;
            byte[] buf = new byte[count];
            int read = 0;
            while (read < count)
            {
                int n = s.Read(buf, read, count - read);
                if (n <= 0) throw new EndOfStreamException(string.Format("Unexpected EOF reading {0} bytes at 0x{1:X}.", count, offset));
                read += n;
            }
            return buf;
        }

        /// <summary>Build the AES-CTR base IV for the given partition + section.</summary>
        public static byte[] BuildCounter(ulong partitionId, byte sectionId)
        {
            // Per 3dbrew NCCH page (version 2 — modern 3DS games):
            //   IV = partitionId (BE 8 bytes) || sectionId (1 byte) || 0 (7 bytes)
            byte[] iv = new byte[16];
            BinaryPrimitives.WriteUInt64BigEndian(iv.AsSpan(0, 8), partitionId);
            iv[8] = sectionId;
            return iv;
        }

        public const byte SectionExHeader = 0x01;
        public const byte SectionExeFs    = 0x02;
        public const byte SectionRomFs    = 0x03;
        public const int MediaUnitSize = MediaUnit;
    }
}
