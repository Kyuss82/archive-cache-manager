using System;
using System.IO;
using System.Security.Cryptography;

namespace ArchiveCacheManager
{
    /// <summary>
    /// Forges a minimal Wii U title.tik that CDecrypt can consume.
    /// CDecrypt only reads the encrypted title key (offset 0x1BF) and common key index (offset 0x1F1)
    /// from the ticket, then decrypts using the Wii U Common Key with IV = title_id || zeros.
    /// All other fields can be zero.
    /// </summary>
    public static class WiiuTicketBuilder
    {
        private const int TicketSize = 0x350;
        private const int VersionOffset = 0x1BC;
        private const int EncTitleKeyOffset = 0x1BF;
        private const int TicketTitleIdOffset = 0x1DC;
        private const int CommonKeyIndexOffset = 0x1F1;

        /// <param name="commonKeyHex">32 hex chars of the Wii U Common Key.</param>
        public static void Build(string titleIdHex, byte[] titleKey, string commonKeyHex, string outputPath)
        {
            if (titleKey == null || titleKey.Length != 16)
                throw new ArgumentException("Title key must be 16 bytes.", nameof(titleKey));
            if (string.IsNullOrWhiteSpace(commonKeyHex) || commonKeyHex.Length != 32)
                throw new ArgumentException("Common key must be 32 hex characters (16 bytes).", nameof(commonKeyHex));

            byte[] commonKey = HexToBytes(commonKeyHex);
            byte[] tidBytes = HexToBytes(titleIdHex.PadLeft(16, '0'));

            byte[] iv = new byte[16];
            Buffer.BlockCopy(tidBytes, 0, iv, 0, 8);

            byte[] encTitleKey = AesCbcEncrypt(commonKey, iv, titleKey);

            byte[] tik = new byte[TicketSize];
            // Sig type 0x00010004 (RSA-2048 + SHA-256)
            tik[0] = 0x00; tik[1] = 0x01; tik[2] = 0x00; tik[3] = 0x04;
            tik[VersionOffset] = 0x01;
            Buffer.BlockCopy(encTitleKey, 0, tik, EncTitleKeyOffset, 16);
            Buffer.BlockCopy(tidBytes, 0, tik, TicketTitleIdOffset, 8);
            tik[CommonKeyIndexOffset] = 0x01;

            File.WriteAllBytes(outputPath, tik);
        }

        private static byte[] AesCbcEncrypt(byte[] key, byte[] iv, byte[] data)
        {
            using (var aes = Aes.Create())
            {
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.None;
                aes.Key = key;
                aes.IV = iv;
                using (var enc = aes.CreateEncryptor())
                {
                    return enc.TransformFinalBlock(data, 0, data.Length);
                }
            }
        }

        private static byte[] HexToBytes(string hex)
        {
            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return bytes;
        }
    }
}
