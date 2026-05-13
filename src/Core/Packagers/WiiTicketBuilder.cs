/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Part of the Wii U / 3DS / Wii / DSi on-launch packager additions to the
 * Archive Cache Manager plugin (original by fraganator, LGPL 2.1). This file
 * is licensed under LGPL 2.1 or (at your option) any later version.
 *
 * Forge a fake-signed Wii ticket from a title ID + encrypted title key.
 * Algorithm ported from the well-known wii.py reference tool, sections
 * Ticket.fakesign() and Ticket.fixpayload().
 *
 * Wii ticket layout (676 bytes, big-endian, per https://wiibrew.org/wiki/Ticket):
 *   0x000  4    signature type (0x00010001 = RSA-2048-SHA1)
 *   0x004  256  RSA signature  (zeroed for fakesign)
 *   0x104  60   padding between sig and issuer
 *   0x140  64   sig issuer: "Root-CA00000001-XS00000003" + NUL pad
 *   0x180  60   ECDH public key + 3-byte version/crl block
 *   0x1BF  16   encrypted title key
 *   0x1CF  1    reserved
 *   0x1D0  8    ticket ID
 *   0x1D8  4    console ID
 *   0x1DC  8    title ID
 *   0x1E4  2    title export public key type
 *   0x1E6  60   reserved
 *   0x222  64   content access mask  (one bit per content; set all for full access)
 *   0x262  2    fixpayload target (vary until SHA1[0] = 0x00)
 *   0x264  64   limits
 *
 * The "trujivuelta" string-compare bug on the Wii signature path treats a
 * SHA1 starting with 0x00 as a match against the zeroed signature, so an
 * unsigned ticket with the right SHA1 prefix passes verification on cIOS-patched
 * Wii systems and on Dolphin.
 */
using System;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace ArchiveCacheManager
{
    public static class WiiTicketBuilder
    {
        public const int TicketSize = 676;
        private const int SigTypeOffset       = 0x000;
        private const int SignatureOffset     = 0x004;
        private const int IssuerOffset        = 0x140;
        private const int TitleKeyOffset      = 0x1BF;
        private const int TicketIdOffset      = 0x1D0;
        private const int TitleIdOffset       = 0x1DC;
        private const int AccessMaskBlockOff  = 0x222;
        private const int AccessMaskBlockSize = 64;
        private const int FixpayloadOffset    = 0x262;

        private const uint SigTypeRsa2048Sha1 = 0x00010001;
        private static readonly byte[] IssuerString = Encoding.ASCII.GetBytes("Root-CA00000001-XS00000003");

        /// <summary>
        /// Returns a fakesigned 676-byte Wii ticket carrying the supplied title ID
        /// and AES-encrypted title key. Throws if no fixpayload value produces a
        /// SHA1 starting with 0x00 within the 2^16 search space (vanishingly unlikely
        /// — average expected iterations ≈ 256).
        /// </summary>
        public static byte[] Build(ulong titleId, byte[] encTitleKey)
        {
            if (encTitleKey == null || encTitleKey.Length != 16)
                throw new ArgumentException("Encrypted title key must be 16 bytes.");

            byte[] ticket = new byte[TicketSize];

            BinaryPrimitives.WriteUInt32BigEndian(ticket.AsSpan(SigTypeOffset, 4), SigTypeRsa2048Sha1);
            // signature stays all-zero (fakesign)
            IssuerString.CopyTo(ticket.AsSpan(IssuerOffset, IssuerString.Length));
            Buffer.BlockCopy(encTitleKey, 0, ticket, TitleKeyOffset, 16);
            BinaryPrimitives.WriteUInt64BigEndian(ticket.AsSpan(TicketIdOffset, 8), 0x0001000200030004UL);
            BinaryPrimitives.WriteUInt64BigEndian(ticket.AsSpan(TitleIdOffset, 8), titleId);

            // Grant access to every possible content (one bit per slot, 512 slots total).
            for (int i = 0; i < AccessMaskBlockSize; i++)
            {
                ticket[AccessMaskBlockOff + i] = 0xFF;
            }

            // Iterate the 2-byte "padding" field until SHA1(ticket)[0] == 0x00.
            using (var sha = SHA1.Create())
            {
                for (uint i = 0; i <= 0xFFFF; i++)
                {
                    BinaryPrimitives.WriteUInt16BigEndian(ticket.AsSpan(FixpayloadOffset, 2), (ushort)i);
                    if (sha.ComputeHash(ticket)[0] == 0x00) return ticket;
                }
            }

            throw new InvalidOperationException(
                "WiiTicketBuilder: exhausted 65 536 fixpayload candidates without finding a SHA1 starting with 0x00 (this should be statistically impossible).");
        }
    }
}
