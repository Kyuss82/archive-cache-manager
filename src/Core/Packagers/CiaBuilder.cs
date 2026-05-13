using System;
using System.Collections.Generic;
using System.IO;

namespace ArchiveCacheManager
{
    public class CiaContentEntry
    {
        public ushort Index;
        public string AppPath;
    }

    public static class CiaBuilder
    {
        private const int HeaderSize = 0x2020;
        private const int ContentIndexBytes = 0x2000;
        private const int Alignment = 0x40;
        private const int CertChainSize = 0xA00;

        public static void Build(string outputPath, byte[] tmd, byte[] ticket, List<CiaContentEntry> contents)
        {
            byte[] certChain = BuildCertChain();

            byte[] contentIndex = new byte[ContentIndexBytes];
            ulong contentSize = 0;
            foreach (var c in contents)
            {
                int byteIdx = c.Index / 8;
                int bitIdx = 7 - (c.Index % 8);
                contentIndex[byteIdx] |= (byte)(1 << bitIdx);
                contentSize += (ulong)new FileInfo(c.AppPath).Length;
            }

            using (var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                WriteHeader(fs, (uint)certChain.Length, (uint)ticket.Length, (uint)tmd.Length, 0u, contentSize, contentIndex);
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
            }
        }

        private static void WriteHeader(Stream s, uint certSize, uint tikSize, uint tmdSize, uint metaSize, ulong contentSize, byte[] contentIndex)
        {
            byte[] header = new byte[HeaderSize];
            WriteU32Le(header, 0x00, HeaderSize);
            WriteU16Le(header, 0x04, 0);
            WriteU16Le(header, 0x06, 0);
            WriteU32Le(header, 0x08, certSize);
            WriteU32Le(header, 0x0C, tikSize);
            WriteU32Le(header, 0x10, tmdSize);
            WriteU32Le(header, 0x14, metaSize);
            WriteU64Le(header, 0x18, contentSize);
            Buffer.BlockCopy(contentIndex, 0, header, 0x20, ContentIndexBytes);
            s.Write(header, 0, header.Length);
        }

        private static void AlignTo(Stream s, int alignment)
        {
            long pos = s.Position;
            long aligned = (pos + alignment - 1) & ~((long)alignment - 1);
            int padLen = (int)(aligned - pos);
            if (padLen > 0)
            {
                byte[] pad = new byte[padLen];
                s.Write(pad, 0, padLen);
            }
        }

        private static byte[] BuildCertChain()
        {
            byte[] titleCert = LoadEmbeddedCert();
            if (titleCert == null || titleCert.Length != 0xA00)
            {
                throw new InvalidOperationException("Embedded title.cert is missing or has unexpected size.");
            }

            byte[] cp = new byte[0x300];
            byte[] ca = new byte[0x400];
            byte[] xs = new byte[0x300];
            Buffer.BlockCopy(titleCert, 0x000, cp, 0, 0x300);
            Buffer.BlockCopy(titleCert, 0x300, ca, 0, 0x400);
            Buffer.BlockCopy(titleCert, 0x700, xs, 0, 0x300);

            byte[] chain = new byte[CertChainSize];
            Buffer.BlockCopy(ca, 0, chain, 0x000, 0x400);
            Buffer.BlockCopy(cp, 0, chain, 0x400, 0x300);
            Buffer.BlockCopy(xs, 0, chain, 0x700, 0x300);
            return chain;
        }

        private static byte[] LoadEmbeddedCert()
        {
            // Prefer a cert dropped into the Extractors folder by the user (public builds ship without).
            string extractorRoot = PathUtils.GetExtractorRootPath();
            foreach (string name in new[] { "title.cert", "ctr-title.cert" })
            {
                string p = Path.Combine(extractorRoot, name);
                if (File.Exists(p))
                {
                    try { return File.ReadAllBytes(p); } catch { }
                }
            }

            // Legacy embedded resource — present only when the .csproj includes the cert as EmbeddedResource.
            var asm = typeof(CiaBuilder).Assembly;
            using (var stream = asm.GetManifestResourceStream("ArchiveCacheManager.Packagers.title.cert"))
            {
                if (stream == null) return null;
                using (var ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    return ms.ToArray();
                }
            }
        }

        private static void WriteU16Le(byte[] buf, int off, ushort v)
        {
            buf[off] = (byte)(v & 0xFF);
            buf[off + 1] = (byte)((v >> 8) & 0xFF);
        }

        private static void WriteU32Le(byte[] buf, int off, uint v)
        {
            buf[off] = (byte)(v & 0xFF);
            buf[off + 1] = (byte)((v >> 8) & 0xFF);
            buf[off + 2] = (byte)((v >> 16) & 0xFF);
            buf[off + 3] = (byte)((v >> 24) & 0xFF);
        }

        private static void WriteU64Le(byte[] buf, int off, ulong v)
        {
            for (int i = 0; i < 8; i++)
            {
                buf[off + i] = (byte)((v >> (8 * i)) & 0xFF);
            }
        }
    }
}
