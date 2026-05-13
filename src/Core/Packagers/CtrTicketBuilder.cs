/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Part of the Wii U / 3DS / Wii / DSi on-launch packager additions to the
 * Archive Cache Manager plugin (original by fraganator, LGPL 2.1). This file
 * is licensed under LGPL 2.1 or (at your option) any later version.
 *
 * 3DS cetk forger. Approach adapted from matiffeder/TikGenerator: reuse a real
 * Nintendo-signed cetk as a "donor" template and overwrite the title key,
 * title ID, and ticket version fields. The RSA signature remains valid because
 * Citra/Lime3DS and 3DS custom firmwares (FBI, Luma3DS) tolerate fake-signed
 * tickets — the original signature covers a body section that we leave intact
 * apart from the three replaced fields, which are not validated cryptographically.
 *
 * Cetk offsets per 3dbrew (https://www.3dbrew.org/wiki/Ticket):
 *   0x1BF — encrypted title key (16 bytes)
 *   0x1DC — title ID           ( 8 bytes, big-endian)
 *   0x1E6 — ticket version     ( 2 bytes, big-endian)
 */
using System;
using System.Buffers.Binary;
using System.IO;

namespace ArchiveCacheManager
{
    public static class CtrTicketBuilder
    {
        public const int CetkSize = 2640;
        private const int TitleKeyOffset    = 0x1BF;
        private const int TitleIdOffset     = 0x1DC;
        private const int TicketVersionOff  = 0x1E6;

        public static byte[] Build(byte[] donorCetk, ulong titleId, byte[] encTitleKey, ushort ticketVersion)
        {
            if (donorCetk == null || donorCetk.Length != CetkSize)
                throw new ArgumentException(string.Format("Donor cetk must be exactly {0} bytes (got {1}).",
                    CetkSize, donorCetk?.Length ?? 0));
            if (encTitleKey == null || encTitleKey.Length != 16)
                throw new ArgumentException("Encrypted title key must be 16 bytes.");

            byte[] cetk = new byte[CetkSize];
            Buffer.BlockCopy(donorCetk, 0, cetk, 0, CetkSize);

            Buffer.BlockCopy(encTitleKey, 0, cetk, TitleKeyOffset, 16);
            BinaryPrimitives.WriteUInt64BigEndian(new Span<byte>(cetk, TitleIdOffset, 8), titleId);
            BinaryPrimitives.WriteUInt16BigEndian(new Span<byte>(cetk, TicketVersionOff, 2), ticketVersion);

            return cetk;
        }

        /// <summary>
        /// Resolve the donor cetk path: explicit config first, otherwise auto-detect in Extractors/.
        /// </summary>
        public static byte[] TryLoadDonor()
        {
            string configured = Config.CiaCetkDonorPath;
            if (!string.IsNullOrWhiteSpace(configured))
            {
                string abs = PathUtils.GetAbsolutePath(configured);
                if (File.Exists(abs))
                {
                    try { return File.ReadAllBytes(abs); }
                    catch (Exception ex) { Logger.Log(string.Format("CTR cetk donor: read failed for {0}: {1}", abs, ex.Message)); }
                }
            }

            string extractors = PathUtils.GetExtractorRootPath();
            foreach (string name in new[] { "cetk-donor.bin", "cetk.donor", "ctr-cetk-donor.bin" })
            {
                string p = Path.Combine(extractors, name);
                if (!File.Exists(p)) continue;
                try { return File.ReadAllBytes(p); }
                catch (Exception ex) { Logger.Log(string.Format("CTR cetk donor: read failed for {0}: {1}", p, ex.Message)); }
            }

            return null;
        }
    }
}
