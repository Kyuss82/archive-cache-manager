/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Part of the Wii U / 3DS / Wii / DSi on-launch packager additions to the
 * Archive Cache Manager plugin (original by fraganator, LGPL 2.1). This file
 * is licensed under LGPL 2.1 or (at your option) any later version.
 *
 * Pure-C# Wii .wad packer. Logic ported from the well-known wii.py reference
 * tool (grp68 and friends, public domain), section: packdir(directory).
 *
 * The .wad container layout (big-endian fixed-width fields, 64-byte-aligned
 * sections) is documented at https://wiibrew.org/wiki/WAD_files :
 *
 *   header (0x20 bytes)  → pad to 0x40
 *   cert chain           → pad to next 0x40 boundary
 *   ticket (0x2A4 bytes) → pad to next 0x40 boundary
 *   TMD (variable)       → pad to next 0x40 boundary
 *   each content (.app)  → pad each to next 0x40 boundary
 *   footer (optional)    → pad to next 0x40 boundary
 *
 * Pass-through model: .app inputs are written verbatim. CDN-style dumps already
 * carry AES-CBC-encrypted content keyed with the title key, which is exactly what
 * the WAD format stores — so no re-encryption is needed.
 */
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;

namespace ArchiveCacheManager
{
    public static class WadBuilder
    {
        private const int HeaderSize       = 0x20;
        private const int Alignment        = 0x40;
        private const int TmdContentTblOff = 484;
        private const int TmdContnumOff    = 0x1DE;
        private const int TmdContentStride = 36;
        private static readonly byte[] WadType = new byte[] { (byte)'I', (byte)'s', 0x00, 0x00 };

        /// <summary>
        /// Write a .wad file at <paramref name="outputPath"/>. Inputs:
        ///   <paramref name="cert"/>   raw cert chain bytes
        ///   <paramref name="ticket"/> raw ticket bytes (0x2A4 = 676 expected)
        ///   <paramref name="tmd"/>    raw TMD bytes (header + content table)
        ///   <paramref name="contentPaths"/> content-ID → .app file path map
        ///   <paramref name="footer"/> optional WAD footer (timestamp etc.); null/empty = none
        /// </summary>
        public static void Build(string outputPath, byte[] cert, byte[] ticket, byte[] tmd,
                                 IDictionary<uint, string> contentPaths, byte[] footer = null)
        {
            if (cert == null || cert.Length == 0)   throw new ArgumentException("cert is empty");
            if (ticket == null || ticket.Length == 0) throw new ArgumentException("ticket is empty");
            if (tmd == null || tmd.Length < TmdContentTblOff + TmdContentStride)
                throw new ArgumentException("tmd too small or null");

            int contNum = BinaryPrimitives.ReadUInt16BigEndian(new ReadOnlySpan<byte>(tmd, TmdContnumOff, 2));
            if (contNum <= 0) throw new InvalidOperationException("TMD declares 0 contents");
            if (tmd.Length < TmdContentTblOff + contNum * TmdContentStride)
                throw new InvalidOperationException("TMD content table truncated");

            // Resolve every TMD content to a file on disk in TMD order, compute aligned data size.
            var ordered = new List<(uint cid, string path, long size)>(contNum);
            long dataSize = 0;
            for (int i = 0; i < contNum; i++)
            {
                int off = TmdContentTblOff + i * TmdContentStride;
                uint cid = BinaryPrimitives.ReadUInt32BigEndian(new ReadOnlySpan<byte>(tmd, off, 4));

                if (!contentPaths.TryGetValue(cid, out string path) || !File.Exists(path))
                    throw new InvalidOperationException(
                        string.Format("WadBuilder: no .app file found for content ID 0x{0:X8}", cid));

                long size = new FileInfo(path).Length;
                ordered.Add((cid, path, size));
                dataSize += AlignUp(size, Alignment);
            }

            uint footerSize = (uint)(footer != null ? footer.Length : 0);

            using (var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                WriteHeader(fs,
                    certSize:   (uint)cert.Length,
                    tikSize:    (uint)ticket.Length,
                    tmdSize:    (uint)tmd.Length,
                    dataSize:   (uint)dataSize,
                    footerSize: footerSize);
                AlignTo(fs, Alignment);

                fs.Write(cert,   0, cert.Length);   AlignTo(fs, Alignment);
                fs.Write(ticket, 0, ticket.Length); AlignTo(fs, Alignment);
                fs.Write(tmd,    0, tmd.Length);    AlignTo(fs, Alignment);

                foreach (var (_, path, _) in ordered)
                {
                    using (var af = File.OpenRead(path))
                    {
                        af.CopyTo(fs);
                    }
                    AlignTo(fs, Alignment);
                }

                if (footer != null && footer.Length > 0)
                {
                    fs.Write(footer, 0, footer.Length);
                    AlignTo(fs, Alignment);
                }
            }
        }

        private static void WriteHeader(Stream s, uint certSize, uint tikSize, uint tmdSize, uint dataSize, uint footerSize)
        {
            byte[] header = new byte[HeaderSize];
            BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(0x00, 4), HeaderSize);
            WadType.CopyTo(header.AsSpan(0x04, 4));
            BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(0x08, 4), certSize);
            BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(0x0C, 4), 0);
            BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(0x10, 4), tikSize);
            BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(0x14, 4), tmdSize);
            BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(0x18, 4), dataSize);
            BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(0x1C, 4), footerSize);
            s.Write(header, 0, header.Length);
        }

        private static long AlignUp(long value, long alignment) =>
            (value + alignment - 1) & ~(alignment - 1);

        private static void AlignTo(Stream s, int alignment)
        {
            long pos = s.Position;
            long aligned = AlignUp(pos, alignment);
            int pad = (int)(aligned - pos);
            if (pad > 0) s.Write(new byte[pad], 0, pad);
        }
    }
}
