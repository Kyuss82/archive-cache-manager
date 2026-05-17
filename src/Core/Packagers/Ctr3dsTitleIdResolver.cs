/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Best-effort 3DS title-id resolver for the launching game. The v2.76 "Install 3DS
 * Updates + DLCs" menu uses this to match the game's TID against the local mirror
 * manifest. Supports:
 *   • `.cia` files — read TMD title_id at the standard CIA structure offsets.
 *   • `.3ds`/`.cci` carts — read MediaID at NCSD header offset 0x108 (8 bytes LE).
 *   • Folder containing a `.cia` → first .cia found, recursed up to depth 2.
 */
using System;
using System.IO;

namespace ArchiveCacheManager
{
    public static class Ctr3dsTitleIdResolver
    {
        /// <summary>
        /// Returns the title_id (64-bit) for the file at <paramref name="path"/>, or null if it
        /// can't be parsed or the format isn't recognized.
        /// </summary>
        public static ulong? Resolve(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            try
            {
                if (File.Exists(path))
                {
                    string ext = Path.GetExtension(path).ToLowerInvariant();
                    if (ext == ".cia") return ResolveCia(path);
                    if (ext == ".3ds" || ext == ".cci") return ResolveNcsd(path);
                }
                else if (Directory.Exists(path))
                {
                    foreach (string cia in Directory.EnumerateFiles(path, "*.cia", SearchOption.AllDirectories))
                    {
                        var tid = ResolveCia(cia);
                        if (tid.HasValue) return tid;
                    }
                    foreach (string nds in Directory.EnumerateFiles(path, "*.3ds", SearchOption.AllDirectories))
                    {
                        var tid = ResolveNcsd(nds);
                        if (tid.HasValue) return tid;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(string.Format("Ctr3dsTitleIdResolver: resolve failed for {0}: {1}", path, ex.Message));
            }
            return null;
        }

        private static ulong? ResolveCia(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                // Same math as LocalPkgScanner.ReadCiaTitleId: walk the CIA's section sizes to
                // locate the TMD, then read offset 0x18C (title_id) of the TMD blob.
                byte[] header = new byte[0x20];
                if (fs.Read(header, 0, header.Length) != header.Length) return null;
                uint archiveHeaderSize = BitConverter.ToUInt32(header, 0x00);
                uint certSize          = BitConverter.ToUInt32(header, 0x08);
                uint ticketSize        = BitConverter.ToUInt32(header, 0x0C);
                uint tmdSize           = BitConverter.ToUInt32(header, 0x10);
                if (archiveHeaderSize == 0 || tmdSize == 0) return null;

                long Align64(long pos) => (pos + 63) & ~63L;
                long pos = Align64(archiveHeaderSize);
                pos = Align64(pos + certSize);
                pos = Align64(pos + ticketSize);
                if (tmdSize < 0x1E0) return null;
                fs.Position = pos + 0x18C;
                byte[] tidBe = new byte[8];
                if (fs.Read(tidBe, 0, 8) != 8) return null;
                Array.Reverse(tidBe);
                return BitConverter.ToUInt64(tidBe, 0);
            }
        }

        private static ulong? ResolveNcsd(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                // 3DS cart NCSD header layout (3dbrew):
                //   0x000: Signature (RSA-2048, 0x100 bytes)
                //   0x100: Magic "NCSD"
                //   0x104: Media size in units of 0x200
                //   0x108: Media ID (== title_id for partition 0, 8 bytes LE)
                byte[] header = new byte[0x120];
                if (fs.Read(header, 0, header.Length) != header.Length) return null;
                if (!(header[0x100] == 'N' && header[0x101] == 'C' && header[0x102] == 'S' && header[0x103] == 'D'))
                    return null;
                return BitConverter.ToUInt64(header, 0x108);
            }
        }
    }
}
