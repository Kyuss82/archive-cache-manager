using System;
using System.IO;

namespace ArchiveCacheManager
{
    public static class TmdReader
    {
        // Wii TMD: 0x140-byte signature header, then body. Title ID lives at 0x18C, big-endian.
        private const int TitleIdOffset = 0x18C;
        private const int TitleIdLength = 8;

        public static string ReadTitleId(string tmdPath)
        {
            using (var fs = new FileStream(tmdPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (fs.Length < TitleIdOffset + TitleIdLength)
                {
                    throw new InvalidDataException(string.Format("TMD file is too small to contain a title ID: {0}", tmdPath));
                }

                fs.Seek(TitleIdOffset, SeekOrigin.Begin);
                byte[] buf = new byte[TitleIdLength];
                int read = fs.Read(buf, 0, TitleIdLength);
                if (read != TitleIdLength)
                {
                    throw new InvalidDataException(string.Format("Failed to read title ID from TMD: {0}", tmdPath));
                }

                return BitConverter.ToString(buf).Replace("-", string.Empty).ToUpperInvariant();
            }
        }
    }
}
