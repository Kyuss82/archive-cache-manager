using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;

namespace ArchiveCacheManager
{
    public class TadContentEntry
    {
        public ushort Index;
        public string AppPath;
        public ulong Size;
    }

    public static class TadBuilder
    {
        private const int HeaderSize = 0x20;
        private const uint WadType = 0x49730000; // "Is\0\0"
        private const int Alignment = 0x40;
        private const int CertChainSize = 0xA00;

        public static void Build(string outputPath, byte[] tmd, byte[] ticket, List<TadContentEntry> contents, byte[] footer)
        {
            byte[] certChain = LoadEmbeddedCert();
            if (certChain == null || certChain.Length != CertChainSize)
            {
                throw new InvalidOperationException("Embedded cert.tad is missing or has unexpected size.");
            }

            uint dataSize = 0;
            foreach (var c in contents)
            {
                long len = new FileInfo(c.AppPath).Length;
                dataSize += (uint)AlignUp(len, Alignment);
            }

            using (var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                WriteHeader(fs,
                    certSize: (uint)certChain.Length,
                    crlSize: 0,
                    ticketSize: (uint)ticket.Length,
                    tmdSize: (uint)tmd.Length,
                    dataSize: dataSize,
                    footerSize: (uint)(footer != null ? footer.Length : 0));
                AlignTo(fs, Alignment);

                fs.Write(certChain, 0, certChain.Length);
                AlignTo(fs, Alignment);

                fs.Write(ticket, 0, ticket.Length);
                AlignTo(fs, Alignment);

                fs.Write(tmd, 0, tmd.Length);
                AlignTo(fs, Alignment);

                foreach (var c in contents)
                {
                    using (var cf = File.OpenRead(c.AppPath))
                    {
                        cf.CopyTo(fs);
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

        private static void WriteHeader(Stream s, uint certSize, uint crlSize, uint ticketSize, uint tmdSize, uint dataSize, uint footerSize)
        {
            byte[] header = new byte[HeaderSize];
            WriteU32Be(header, 0x00, HeaderSize);
            WriteU32Be(header, 0x04, WadType);
            WriteU32Be(header, 0x08, certSize);
            WriteU32Be(header, 0x0C, crlSize);
            WriteU32Be(header, 0x10, ticketSize);
            WriteU32Be(header, 0x14, tmdSize);
            WriteU32Be(header, 0x18, dataSize);
            WriteU32Be(header, 0x1C, footerSize);
            s.Write(header, 0, header.Length);
        }

        private static void AlignTo(Stream s, int alignment)
        {
            long pos = s.Position;
            long aligned = AlignUp(pos, alignment);
            int padLen = (int)(aligned - pos);
            if (padLen > 0)
            {
                byte[] pad = new byte[padLen];
                s.Write(pad, 0, padLen);
            }
        }

        private static long AlignUp(long value, long alignment)
        {
            return (value + alignment - 1) & ~(alignment - 1);
        }

        private static byte[] LoadEmbeddedCert()
        {
            // Prefer a cert dropped into the Extractors folder by the user (public builds ship without).
            string extractorRoot = PathUtils.GetExtractorRootPath();
            foreach (string name in new[] { "cert.tad", "twl-cert.tad" })
            {
                string p = Path.Combine(extractorRoot, name);
                if (File.Exists(p))
                {
                    try { return File.ReadAllBytes(p); } catch { }
                }
            }

            // Legacy embedded resource — present only when the .csproj includes the cert as EmbeddedResource.
            var asm = typeof(TadBuilder).Assembly;
            using (var stream = asm.GetManifestResourceStream("ArchiveCacheManager.Packagers.cert.tad"))
            {
                if (stream == null) return null;
                using (var ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    return ms.ToArray();
                }
            }
        }

        private static void WriteU32Be(byte[] buf, int off, uint v)
        {
            BinaryPrimitives.WriteUInt32BigEndian(new Span<byte>(buf, off, 4), v);
        }
    }
}
