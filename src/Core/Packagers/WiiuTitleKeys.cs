using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ArchiveCacheManager
{
    /// <summary>
    /// Deterministic Wii U title key derivation, ported from a community PHP implementation.
    /// salt = MD5(secret(-3, 10) || mungedTitleId); titleKey = PBKDF2-HMAC-SHA1(password, salt, 20 iter, 16 bytes).
    /// </summary>
    public static class WiiuTitleKeys
    {
        public static byte[] Derive(string titleIdHex, string password)
        {
            byte[] secret = BuildSecret(-3, 10);
            byte[] mungedTid = MungeTitleId(titleIdHex);
            byte[] saltInput = secret.Concat(mungedTid).ToArray();

            byte[] salt;
            using (var md5 = MD5.Create())
            {
                salt = md5.ComputeHash(saltInput);
            }

            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 20, HashAlgorithmName.SHA1))
            {
                return pbkdf2.GetBytes(16);
            }
        }

        private static byte[] BuildSecret(int start, int len)
        {
            byte[] result = new byte[len];
            int add = start + len;
            int cur = start;
            for (int i = 0; i < len; i++)
            {
                result[i] = (byte)(cur & 0xFF);
                int next = cur + add;
                add = cur;
                cur = next;
            }
            return result;
        }

        private static byte[] MungeTitleId(string titleIdHex)
        {
            string s = titleIdHex ?? string.Empty;
            while (s.Length >= 2 && s.Substring(0, 2) == "00")
            {
                s = s.Substring(2);
            }
            if (string.IsNullOrEmpty(s)) s = "00";
            return HexToBytes(s);
        }

        private static byte[] HexToBytes(string hex)
        {
            if (hex.Length % 2 != 0) hex = "0" + hex;
            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return bytes;
        }
    }
}
