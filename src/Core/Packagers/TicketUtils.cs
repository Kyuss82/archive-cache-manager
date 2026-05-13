using System;
using System.Buffers.Binary;
using System.IO;

namespace ArchiveCacheManager
{
    public static class TicketUtils
    {
        /// <summary>
        /// Returns the actual ticket body size for the supplied signed-blob bytes (cetk/title.tik).
        /// A raw cetk fetched from NUS contains the ticket followed by a trailing cert chain — this
        /// returns only the leading ticket portion size so callers can truncate before writing it
        /// into a CIA/WAD/TAD ticket section.
        /// </summary>
        public static int GetTicketBodySize(byte[] cetk)
        {
            if (cetk == null || cetk.Length < 4) throw new ArgumentException("Empty or invalid cetk.");
            uint sigType = BinaryPrimitives.ReadUInt32BigEndian(new ReadOnlySpan<byte>(cetk, 0, 4));
            switch (sigType)
            {
                case 0x00010001u: return 0x2A4; // Wii/DSi RSA-2048 SHA-1
                case 0x00010004u: return 0x350; // Wii U/3DS RSA-2048 SHA-256
                case 0x00010000u: return 0x3A4; // RSA-4096 SHA-1
                case 0x00010003u: return 0x450; // RSA-4096 SHA-256
                default:
                    throw new InvalidDataException(string.Format("Unsupported ticket signature type: 0x{0:X8}", sigType));
            }
        }

        public static byte[] TruncateToBody(byte[] cetk)
        {
            int size = GetTicketBodySize(cetk);
            if (size >= cetk.Length) return cetk;
            byte[] trimmed = new byte[size];
            Buffer.BlockCopy(cetk, 0, trimmed, 0, size);
            return trimmed;
        }
    }
}
