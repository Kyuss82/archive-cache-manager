/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Parse the header + metadata block of a Sony .pkg file (PS3 retail, PSP retail
 * or PSV retail) and decrypt-in-RAM the 32-byte item-record table that sits at
 * the start of the encrypted data region. Layout reference: PSDevWiki
 * (https://www.psdevwiki.com/ps3/PKG_files,
 *  https://www.psdevwiki.com/ps3/PKG_files_(PSP_and_PSVita)) and the canonical
 * pkg2zip reader (mmozeiko, MIT).
 *
 * Per-PKG AES key selection happens in SelectAesKey, based on pkg_type (0x0001
 * = PS3, 0x0002 = PSP/PSV) and, for type 0x0002, on the drm_type field read from
 * metadata record 0x01. The 0x8000 bit of pkg_revision is the "finalized" flag —
 * every retail/PSN PKG sets it; non-finalized dev/internal packages clear it but
 * still decrypt with the same key, so we don't filter on it.
 *
 * Fixed PKG header layout (192 bytes, all multi-byte fields big-endian):
 *   0x00 [4]   magic "\x7FPKG"
 *   0x04 [2]   pkg_revision           (high bit = finalized; low bits = internal version)
 *   0x06 [2]   pkg_type               (0x0001 = PS3, 0x0002 = PSP/PSV)
 *   0x08 [4]   pkg_metadata_offset
 *   0x0C [4]   pkg_metadata_count
 *   0x10 [4]   pkg_metadata_size
 *   0x14 [4]   item_count
 *   0x18 [8]   total_size
 *   0x20 [8]   data_offset            (start of encrypted region)
 *   0x28 [8]   data_size
 *   0x30 [48]  content_id             (ASCII, NUL-padded)
 *   0x60 [16]  digest
 *   0x70 [16]  pkg_data_riv           (IV for AES-CTR keystream)
 *   0x80 [64]  pkg_header_digest
 *
 * Metadata block (plaintext, at pkg_metadata_offset, pkg_metadata_count records):
 *   uint32_be record_id
 *   uint32_be record_size
 *   uint8     record_data[record_size]
 * Records of interest for our pipeline:
 *   0x01 drm_type (uint32_be)
 *   0x02 content_type (uint32_be) — 4=PS3 game, 5/6=patch, 7=theme, 9=licence, A=app, B=DLC, …
 *   0x06 title_id (12 ASCII chars)
 *
 * Item record (32 bytes, encrypted; offsets are within the encrypted region):
 *   uint32_be filename_offset
 *   uint32_be filename_size
 *   uint64_be data_offset
 *   uint64_be data_size
 *   uint32_be flags          (low byte = entry type; 0x04 = directory)
 *   uint32_be padding
 */
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ArchiveCacheManager
{
    public class PsPkgHeader
    {
        public ushort PkgRevision;
        public ushort PkgType;
        public uint MetadataOffset;
        public uint MetadataCount;
        public uint MetadataSize;
        public uint ItemCount;
        public ulong TotalSize;
        public ulong DataOffset;
        public ulong DataSize;
        public string ContentId;
        public byte[] DataRiv;

        /// <summary>True for PS3 packages (retail or non-finalized — both decrypt with the same key).</summary>
        public bool IsRetailPs3 => PkgType == 0x0001;

        /// <summary>True for PSP / PS Vita packages (retail or non-finalized).</summary>
        public bool IsRetailPspOrPsv => PkgType == 0x0002;

        /// <summary>True for any package the reader can handle (PS3 or PSP/PSV).</summary>
        public bool IsRetail => PkgType == 0x0001 || PkgType == 0x0002;

        /// <summary>True if the high bit of pkg_revision is set ("FINALIZED" / signed-for-release).</summary>
        public bool IsFinalized => (PkgRevision & 0x8000) != 0;
    }

    public class PsPkgItem
    {
        public string Name;
        public ulong DataOffset;   // relative to header.DataOffset
        public ulong DataSize;
        public uint Flags;
        public byte EntryType;     // low byte of Flags

        public bool IsDirectory => EntryType == 0x04;
    }

    public class PsParsedPkg
    {
        public PsPkgHeader Header;
        public List<PsPkgItem> Items;
        public uint DrmType;        // 0 if record absent
        public uint ContentType;    // 0 if record absent
        public string TitleId;      // up to 12 chars (e.g. "UCES01285" / "NPEB00000"), null if absent
        public byte[] AesKey;       // resolved from pkg_type + key_type
        public int  KeyType;        // pkg_header[0xE7] & 7 — pkg2zip's key dispatcher: 1=PSP key, 2/3/4=PSV-N wrap
    }

    public static class PsPkgReader
    {
        public const int PkgHeaderSize = 0xC0;
        public const int PkgExtHeaderSize = 0x40;   // optional 64-byte extension that follows the main header on PSP/PSV PKGs
        public const int ItemRecordSize = 32;
        private static readonly byte[] PkgMagic = { 0x7F, (byte)'P', (byte)'K', (byte)'G' };

        public static PsParsedPkg Parse(string pkgPath)
        {
            using (var fs = File.OpenRead(pkgPath))
            {
                return Parse(fs);
            }
        }

        public static PsParsedPkg Parse(Stream pkg) => Parse(pkg, skipItemTable: false);

        /// <summary>
        /// Parses header + metadata block. With <paramref name="skipItemTable"/>=true the encrypted
        /// item table is not decoded — useful for offline scanners that only need title id /
        /// content id / content type / key type to categorise a PKG (no per-item decryption).
        /// </summary>
        public static PsParsedPkg Parse(Stream pkg, bool skipItemTable)
        {
            if (pkg == null) throw new ArgumentNullException(nameof(pkg));
            if (!pkg.CanSeek) throw new ArgumentException("PKG stream must be seekable.", nameof(pkg));

            pkg.Position = 0;
            // Read main header + (optional) ext_header in one go. pkg2zip's `key_type` lives at
            // offset 0xE7 of the file, which is inside the ext_header (= ext_header[0x27]).
            byte[] headerBytes = ReadExact(pkg, PkgHeaderSize + PkgExtHeaderSize);

            if (headerBytes[0] != PkgMagic[0] || headerBytes[1] != PkgMagic[1] ||
                headerBytes[2] != PkgMagic[2] || headerBytes[3] != PkgMagic[3])
            {
                throw new InvalidDataException("Not a PKG file (bad magic).");
            }

            var hdr = new PsPkgHeader
            {
                PkgRevision    = BinaryPrimitives.ReadUInt16BigEndian(headerBytes.AsSpan(0x04, 2)),
                PkgType        = BinaryPrimitives.ReadUInt16BigEndian(headerBytes.AsSpan(0x06, 2)),
                MetadataOffset = BinaryPrimitives.ReadUInt32BigEndian(headerBytes.AsSpan(0x08, 4)),
                MetadataCount  = BinaryPrimitives.ReadUInt32BigEndian(headerBytes.AsSpan(0x0C, 4)),
                MetadataSize   = BinaryPrimitives.ReadUInt32BigEndian(headerBytes.AsSpan(0x10, 4)),
                ItemCount      = BinaryPrimitives.ReadUInt32BigEndian(headerBytes.AsSpan(0x14, 4)),
                TotalSize      = BinaryPrimitives.ReadUInt64BigEndian(headerBytes.AsSpan(0x18, 8)),
                DataOffset     = BinaryPrimitives.ReadUInt64BigEndian(headerBytes.AsSpan(0x20, 8)),
                DataSize       = BinaryPrimitives.ReadUInt64BigEndian(headerBytes.AsSpan(0x28, 8)),
                ContentId      = ReadCString(headerBytes, 0x30, 48),
                DataRiv        = headerBytes.AsSpan(0x70, 16).ToArray(),
            };

            if (!hdr.IsRetail)
            {
                throw new NotSupportedException(string.Format(
                    "PKG is not a supported package type (revision=0x{0:X4}, type=0x{1:X4}). " +
                    "This reader handles PS3 (type 0x0001) and PSP/PSV (type 0x0002) only.",
                    hdr.PkgRevision, hdr.PkgType));
            }

            var parsed = new PsParsedPkg { Header = hdr };
            ParseMetadata(pkg, hdr, parsed);

            // pkg2zip's key dispatcher for type-0x0002 PKGs is `pkg_header[0xE7] & 7`:
            //   1 = PSP key (pkg_psp_key, no wrap)
            //   2/3/4 = PSV variants — the published vita_N key is a *wrap*, the real
            //           AES-CTR data key is `AES-ECB-encrypt(vita_N, pkg_data_riv)`.
            // PSP "PSN-Encrypted" titles in particular often carry content_type=0x07 but
            // key_type=2, so routing the key on content_type alone (what we used to do)
            // produced garbage filenames after decrypt. Routing on key_type matches pkg2zip
            // exactly and works for every PSP/PSV PKG distribution out there.
            parsed.KeyType = headerBytes[0xE7] & 7;
            parsed.AesKey = SelectAesKey(hdr.PkgType, parsed.KeyType, hdr.DataRiv, out string keyDesc);

            Logger.Log(string.Format(
                "PsPkgReader: parsed PKG content={0} type=0x{1:X4} drm={2} content_type={3} key_type={4} → key={5}",
                hdr.ContentId, hdr.PkgType, parsed.DrmType, parsed.ContentType, parsed.KeyType, keyDesc));
            if (!skipItemTable)
            {
                parsed.Items = ReadItemTable(pkg, hdr, parsed.AesKey);
            }
            return parsed;
        }

        private static byte[] DerivePsvMainKey(byte[] baseKey, byte[] iv)
        {
            using (var aes = System.Security.Cryptography.Aes.Create())
            {
                aes.Mode = System.Security.Cryptography.CipherMode.ECB;
                aes.Padding = System.Security.Cryptography.PaddingMode.None;
                aes.Key = baseKey;
                using (var enc = aes.CreateEncryptor())
                {
                    byte[] derived = new byte[16];
                    enc.TransformBlock(iv, 0, 16, derived, 0);
                    return derived;
                }
            }
        }

        /// <summary>
        /// Resolve the AES-128 key for the encrypted data region. Routing matches pkg2zip
        /// (mmozeiko/pkg2zip) exactly:
        ///   pkg_type 0x0001            → PS3 key (no wrap).
        ///   pkg_type 0x0002, key_type 1 → PSP key (no wrap).
        ///   pkg_type 0x0002, key_type 2 → AES-ECB-encrypt(PSV-2 wrap key, pkg_data_riv).
        ///   pkg_type 0x0002, key_type 3 → AES-ECB-encrypt(PSV-3 wrap key, pkg_data_riv).
        ///   pkg_type 0x0002, key_type 4 → AES-ECB-encrypt(PSV-4 wrap key, pkg_data_riv).
        /// key_type lives at `pkg_header[0xE7] & 7`.
        /// </summary>
        public static byte[] SelectAesKey(ushort pkgType, int keyType, byte[] dataRiv, out string describe)
        {
            if (pkgType == 0x0001)
            {
                describe = "PS3";
                return PsPkgKeys.Ps3AesKey;
            }
            if (pkgType == 0x0002)
            {
                switch (keyType)
                {
                    case 1:
                        describe = "PSP";
                        return PsPkgKeys.PspKey;
                    case 2:
                        describe = "PSV-2→derived";
                        return DerivePsvMainKey(PsPkgKeys.PsVita2Key, dataRiv);
                    case 3:
                        describe = "PSV-3→derived";
                        return DerivePsvMainKey(PsPkgKeys.PsVita3Key, dataRiv);
                    case 4:
                        describe = "PSV-4→derived";
                        return DerivePsvMainKey(PsPkgKeys.PsVita4Key, dataRiv);
                    default:
                        // pkg2zip falls back to PSP key for unknown variants (very rare).
                        describe = string.Format("PSP (fallback for key_type={0})", keyType);
                        return PsPkgKeys.PspKey;
                }
            }
            throw new NotSupportedException(string.Format("Unsupported PKG type 0x{0:X4}.", pkgType));
        }

        private static void ParseMetadata(Stream pkg, PsPkgHeader hdr, PsParsedPkg parsed)
        {
            if (hdr.MetadataSize == 0 || hdr.MetadataCount == 0) return;

            pkg.Position = hdr.MetadataOffset;
            byte[] block = ReadExact(pkg, (int)hdr.MetadataSize);

            int pos = 0;
            for (uint i = 0; i < hdr.MetadataCount && pos + 8 <= block.Length; i++)
            {
                uint recordId   = BinaryPrimitives.ReadUInt32BigEndian(block.AsSpan(pos, 4));
                uint recordSize = BinaryPrimitives.ReadUInt32BigEndian(block.AsSpan(pos + 4, 4));
                pos += 8;
                if (pos + recordSize > block.Length) break;

                switch (recordId)
                {
                    case 0x01: // drm_type
                        if (recordSize >= 4)
                            parsed.DrmType = BinaryPrimitives.ReadUInt32BigEndian(block.AsSpan(pos, 4));
                        break;
                    case 0x02: // content_type
                        if (recordSize >= 4)
                            parsed.ContentType = BinaryPrimitives.ReadUInt32BigEndian(block.AsSpan(pos, 4));
                        break;
                    case 0x06: // title_id
                        parsed.TitleId = ReadCString(block, pos, (int)recordSize).Trim();
                        break;
                }

                pos += (int)recordSize;
            }
        }

        private static List<PsPkgItem> ReadItemTable(Stream pkg, PsPkgHeader hdr, byte[] aesKey)
        {
            var items = new List<PsPkgItem>((int)hdr.ItemCount);
            if (hdr.ItemCount == 0) return items;

            long tableBytes = (long)hdr.ItemCount * ItemRecordSize;
            if (tableBytes > int.MaxValue || (ulong)tableBytes > hdr.DataSize)
            {
                throw new InvalidDataException("PKG item table is larger than the encrypted data region.");
            }

            byte[] table = new byte[tableBytes];
            pkg.Position = (long)hdr.DataOffset;
            if (pkg.Read(table, 0, table.Length) != table.Length)
            {
                throw new EndOfStreamException("PKG item table truncated.");
            }

            using (var ctr = new PsPkgAesCtr(aesKey, hdr.DataRiv))
            {
                ctr.Decrypt(0, table, 0, table.Length);
            }

            // Diagnostic: dump the first decrypted item record so we can sanity-check key + IV.
            if (table.Length >= ItemRecordSize)
            {
                Logger.Log(string.Format(
                    "PsPkgReader: first decrypted item record (32 bytes): {0}",
                    BitConverter.ToString(table, 0, ItemRecordSize)));
            }

            var nameRanges = new SortedSet<(uint Offset, uint Size)>(Comparer<(uint, uint)>.Create((a, b) =>
            {
                int c = a.Item1.CompareTo(b.Item1);
                return c != 0 ? c : a.Item2.CompareTo(b.Item2);
            }));

            for (uint i = 0; i < hdr.ItemCount; i++)
            {
                int off = (int)(i * ItemRecordSize);
                uint filenameOffset = BinaryPrimitives.ReadUInt32BigEndian(table.AsSpan(off, 4));
                uint filenameSize   = BinaryPrimitives.ReadUInt32BigEndian(table.AsSpan(off + 4, 4));
                ulong dataOffset    = BinaryPrimitives.ReadUInt64BigEndian(table.AsSpan(off + 8, 8));
                ulong dataSize      = BinaryPrimitives.ReadUInt64BigEndian(table.AsSpan(off + 16, 8));
                uint flags          = BinaryPrimitives.ReadUInt32BigEndian(table.AsSpan(off + 24, 4));

                if (filenameSize > 0)
                    nameRanges.Add((filenameOffset, filenameSize));

                items.Add(new PsPkgItem
                {
                    DataOffset = dataOffset,
                    DataSize   = dataSize,
                    Flags      = flags,
                    EntryType  = (byte)(flags & 0xFF),
                });
            }

            using (var ctr = new PsPkgAesCtr(aesKey, hdr.DataRiv))
            {
                foreach (var range in nameRanges)
                {
                    long abs = (long)hdr.DataOffset + range.Offset;
                    if (abs + range.Size > (long)(hdr.DataOffset + hdr.DataSize))
                    {
                        throw new InvalidDataException("PKG filename runs past the encrypted data region.");
                    }
                    byte[] nameBuf = new byte[range.Size];
                    pkg.Position = abs;
                    if (pkg.Read(nameBuf, 0, nameBuf.Length) != nameBuf.Length)
                    {
                        throw new EndOfStreamException("PKG filename truncated.");
                    }
                    ctr.Decrypt(range.Offset, nameBuf, 0, nameBuf.Length);

                    string name = TrimAsciiName(nameBuf);

                    // Diagnostic: log the raw decrypted bytes of the first few filenames so we
                    // can verify key + IV by eye (PSP/PSV titles typically start with EBOOT/PARAM/ICON).
                    if (nameRanges.Count <= 4 || string.IsNullOrEmpty(name) || name.Length < 3)
                    {
                        int dumpLen = Math.Min(nameBuf.Length, 32);
                        Logger.Log(string.Format(
                            "PsPkgReader: filename @ off=0x{0:X} size={1}: hex={2} ascii=\"{3}\"",
                            range.Offset, range.Size,
                            BitConverter.ToString(nameBuf, 0, dumpLen),
                            name));
                    }

                    for (int k = 0; k < items.Count; k++)
                    {
                        int recOff = k * ItemRecordSize;
                        uint fOff  = BinaryPrimitives.ReadUInt32BigEndian(table.AsSpan(recOff, 4));
                        uint fSize = BinaryPrimitives.ReadUInt32BigEndian(table.AsSpan(recOff + 4, 4));
                        if (fOff == range.Offset && fSize == range.Size)
                        {
                            items[k].Name = name;
                        }
                    }
                }
            }

            return items;
        }

        private static string TrimAsciiName(byte[] buf)
        {
            int end = buf.Length;
            while (end > 0 && buf[end - 1] == 0) end--;
            return Encoding.ASCII.GetString(buf, 0, end);
        }

        private static string ReadCString(byte[] buf, int offset, int maxLength)
        {
            int end = offset;
            int limit = Math.Min(offset + maxLength, buf.Length);
            while (end < limit && buf[end] != 0) end++;
            return Encoding.ASCII.GetString(buf, offset, end - offset);
        }

        private static byte[] ReadExact(Stream s, int count)
        {
            byte[] buf = new byte[count];
            int read = 0;
            while (read < count)
            {
                int n = s.Read(buf, read, count - read);
                if (n <= 0) throw new EndOfStreamException("Unexpected end of PKG stream.");
                read += n;
            }
            return buf;
        }
    }
}
