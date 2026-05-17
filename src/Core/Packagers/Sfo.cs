/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Minimal Sony PARAM.SFO parser — returns string entries by key name.
 * PARAM.SFO is the canonical metadata file shipped in every PS3/PSP/PSV game
 * (under PS3_GAME/, PSP_GAME/, ux0:app/<TITLE_ID>/sce_sys/ respectively).
 *
 * Format (all multi-byte fields little-endian):
 *   0x00 [4]   magic "\x00PSF"
 *   0x04 [4]   version              (0x00000101 = v1.1)
 *   0x08 [4]   key_table_offset     (relative to file start)
 *   0x0C [4]   data_table_offset    (relative to file start)
 *   0x10 [4]   num_entries
 *   0x14 ...   entry table — 16 bytes per entry:
 *                [2] key_offset       (relative to key_table_offset)
 *                [1] alignment        (always 0x04)
 *                [1] data_type        (0x00=binary, 0x02=string-utf8, 0x04=int32)
 *                [4] data_len         (actual byte length)
 *                [4] data_max_len     (allocated byte length)
 *                [4] data_offset      (relative to data_table_offset)
 *              followed by NUL-padded ASCII keys at key_table_offset,
 *              then per-entry data at data_table_offset + data_offset.
 */
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ArchiveCacheManager
{
    public static class Sfo
    {
        private static readonly byte[] Magic = { 0x00, (byte)'P', (byte)'S', (byte)'F' };

        /// <summary>
        /// Parse PARAM.SFO from <paramref name="path"/> and return a string→string map of
        /// every entry. String entries (data_type=2) are trimmed of trailing NULs. Integer
        /// entries (data_type=4) are returned as the LE uint32 decimal representation.
        /// Binary entries (data_type=0) are returned as lowercase hex.
        /// </summary>
        public static Dictionary<string, string> Parse(string path)
        {
            byte[] data = File.ReadAllBytes(path);
            return Parse(data);
        }

        public static Dictionary<string, string> Parse(byte[] data)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (data == null || data.Length < 20) return map;
            for (int i = 0; i < 4; i++)
            {
                if (data[i] != Magic[i]) return map;
            }

            uint keyTableOff  = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(0x08, 4));
            uint dataTableOff = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(0x0C, 4));
            uint numEntries   = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(0x10, 4));

            if (keyTableOff >= data.Length || dataTableOff >= data.Length || numEntries == 0) return map;

            for (uint i = 0; i < numEntries; i++)
            {
                int entryOff = 0x14 + (int)i * 16;
                if (entryOff + 16 > data.Length) break;

                ushort keyOff   = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(entryOff, 2));
                byte dataType   = data[entryOff + 3];
                uint dataLen    = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(entryOff + 4, 4));
                uint dataOff    = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(entryOff + 12, 4));

                int keyStart = (int)keyTableOff + keyOff;
                if (keyStart >= data.Length) continue;
                int keyEnd = keyStart;
                while (keyEnd < data.Length && data[keyEnd] != 0) keyEnd++;
                string key = Encoding.ASCII.GetString(data, keyStart, keyEnd - keyStart);
                if (key.Length == 0) continue;

                int valStart = (int)dataTableOff + (int)dataOff;
                if (valStart < 0 || valStart >= data.Length) continue;
                int len = (int)Math.Min(dataLen, (uint)(data.Length - valStart));

                string value;
                switch (dataType)
                {
                    case 0x02: // UTF-8 string, NUL-padded
                        int strLen = len;
                        while (strLen > 0 && data[valStart + strLen - 1] == 0) strLen--;
                        value = Encoding.UTF8.GetString(data, valStart, strLen);
                        break;
                    case 0x04: // uint32 LE
                        value = len >= 4
                            ? BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(valStart, 4)).ToString()
                            : string.Empty;
                        break;
                    default:   // binary
                        value = BitConverter.ToString(data, valStart, len).Replace("-", string.Empty).ToLowerInvariant();
                        break;
                }
                map[key] = value;
            }
            return map;
        }

        /// <summary>Shorthand: extract TITLE_ID from a PARAM.SFO file path, or null if absent.</summary>
        public static string ReadTitleId(string path)
        {
            try
            {
                var sfo = Parse(path);
                return sfo.TryGetValue("TITLE_ID", out string id) ? id.Trim() : null;
            }
            catch (Exception ex)
            {
                Logger.Log(string.Format("Sfo.ReadTitleId failed for {0}: {1}", path, ex.Message));
                return null;
            }
        }
    }
}
