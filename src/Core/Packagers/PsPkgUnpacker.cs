/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Streaming decrypt + file-tree writer for a parsed Sony .pkg (PS3, PSP or PSV).
 * Walks the item list produced by PsPkgReader and writes every regular file
 * under <outputRoot>, preserving the in-PKG path. AES-CTR keystream is reset
 * per item so each entry decrypts independently; the per-item starting counter
 * is derived from the item's data_offset (within the encrypted region) divided
 * by 16.
 *
 * Directory entries (flag low byte = 0x04) become mkdir calls. Path separators
 * inside item names are normalised to the host OS convention; any ".." segments
 * are rejected as a zip-slip / pkg-slip defence.
 */
using System;
using System.Collections.Generic;
using System.IO;

namespace ArchiveCacheManager
{
    public class PsPkgUnpackResult
    {
        public int FilesWritten;
        public int DirectoriesCreated;
        public int Skipped;
        /// <summary>Relative path of the first EBOOT-style file found (EBOOT.BIN for PS3, EBOOT.PBP for PSP), host separators; null if none.</summary>
        public string EbootRelativePath;
    }

    public static class PsPkgUnpacker
    {
        private const int BlockSize = 64 * 1024;   // 4096 AES-128 blocks per chunk

        /// <summary>
        /// Decrypt + write every regular file in a parsed PKG under <paramref name="outputRoot"/>.
        /// <paramref name="pathTransform"/> rewrites each PKG-internal path before sanitisation;
        /// return null from it to drop the entry entirely (useful for stripping platform-specific
        /// prefixes like PSP's "USRDIR/CONTENT/" or for filtering out auxiliary entries).
        /// </summary>
        public static PsPkgUnpackResult Unpack(string pkgPath, PsParsedPkg parsed, string outputRoot,
                                               Action<string> progress = null,
                                               Func<string, string> pathTransform = null)
        {
            if (parsed == null) throw new ArgumentNullException(nameof(parsed));
            if (parsed.AesKey == null) throw new ArgumentException("Parsed PKG has no resolved AES key.", nameof(parsed));
            if (string.IsNullOrEmpty(outputRoot)) throw new ArgumentException("outputRoot is required.", nameof(outputRoot));

            Directory.CreateDirectory(outputRoot);

            var result = new PsPkgUnpackResult();

            // Pre-pass: build the set of names that appear as a path prefix of some other entry.
            // PSV PKGs commonly list 'sce_pfs' as a *file* entry alongside 'sce_pfs/files.db',
            // 'sce_pfs/unicv.db', 'sce_pfs/pflist' as file entries — the bare 'sce_pfs' name is
            // a PKG-format placeholder (not destined to live on disk as a file). If we write it
            // as a file first, the three real children fail to extract because their parent path
            // already exists as a file. Identifying parent names up-front lets us skip the
            // placeholder cleanly and let the children's CreateDirectory(parent) call succeed.
            var pathsWithChildren = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var it in parsed.Items)
            {
                string n = it.Name;
                if (string.IsNullOrEmpty(n)) continue;
                n = n.Replace('\\', '/');
                int slash = n.IndexOf('/');
                while (slash >= 0)
                {
                    pathsWithChildren.Add(n.Substring(0, slash));
                    slash = n.IndexOf('/', slash + 1);
                }
            }

            using (var fs = new FileStream(pkgPath, FileMode.Open, FileAccess.Read, FileShare.Read, BlockSize, FileOptions.SequentialScan))
            using (var ctr = new PsPkgAesCtr(parsed.AesKey, parsed.Header.DataRiv))
            {
                byte[] buf = new byte[BlockSize];

                for (int i = 0; i < parsed.Items.Count; i++)
                {
                    var item = parsed.Items[i];
                    string relName = item.Name ?? string.Empty;

                    // Drop directory-placeholder entries up front: anything whose name appears as
                    // the prefix of another entry's name is a PKG-internal marker, not a real file.
                    if (!string.IsNullOrEmpty(relName)
                        && pathsWithChildren.Contains(relName.Replace('\\', '/'))
                        && !item.IsDirectory)
                    {
                        Logger.Log(string.Format(
                            "PsPkgUnpacker: dropping directory-placeholder entry '{0}' (other entries live under it).",
                            relName));
                        result.Skipped++;
                        continue;
                    }
                    if (relName.Length == 0)
                    {
                        result.Skipped++;
                        continue;
                    }

                    if (pathTransform != null)
                    {
                        relName = pathTransform(relName);
                        if (string.IsNullOrEmpty(relName))
                        {
                            result.Skipped++;
                            continue;
                        }
                    }

                    string safeRel = SanitiseRelativePath(relName);
                    if (safeRel == null)
                    {
                        Logger.Log(string.Format("PsPkgUnpacker: refused unsafe item name (raw bytes after decrypt: '{0}').", DescribeBytes(relName)));
                        result.Skipped++;
                        continue;
                    }

                    string fullPath = Path.Combine(outputRoot, safeRel);

                    if (item.IsDirectory)
                    {
                        // PSV PKGs sometimes list 'sce_pfs' as a directory AND as a file in the
                        // same item table — skip the directory mkdir if a file with that name was
                        // already written, leaving the file in place (Vita3K reads sce_pfs/ contents
                        // from the install location regardless).
                        if (File.Exists(fullPath))
                        {
                            Logger.Log(string.Format(
                                "PsPkgUnpacker: skipping directory entry '{0}' — a file with that name already exists.",
                                relName));
                            result.Skipped++;
                            continue;
                        }
                        Directory.CreateDirectory(fullPath);
                        result.DirectoriesCreated++;
                        continue;
                    }

                    // Symmetric defensive check: if a *directory* already exists at this path
                    // (because earlier items lived under it), we can't write a regular file here.
                    // Skip with a warning rather than crashing the whole extract.
                    if (Directory.Exists(fullPath))
                    {
                        Logger.Log(string.Format(
                            "PsPkgUnpacker: skipping file entry '{0}' — a directory with that name already exists.",
                            relName));
                        result.Skipped++;
                        continue;
                    }

                    string parent = Path.GetDirectoryName(fullPath);
                    if (!string.IsNullOrEmpty(parent))
                    {
                        // The parent path may already exist as a FILE (PSV PFS standalone entries).
                        // Skip with a warning rather than crashing the whole extract.
                        if (File.Exists(parent))
                        {
                            Logger.Log(string.Format(
                                "PsPkgUnpacker: cannot create parent '{0}' for '{1}' (collides with existing file). Skipping.",
                                parent, relName));
                            result.Skipped++;
                            continue;
                        }
                        Directory.CreateDirectory(parent);
                    }

                    progress?.Invoke(string.Format("{0,3}% - extracting {1}",
                        (int)((i + 1) * 100L / Math.Max(1, parsed.Items.Count)),
                        relName));

                    WriteEntry(fs, ctr, parsed.Header.DataOffset, item, fullPath, buf);
                    result.FilesWritten++;

                    if (result.EbootRelativePath == null && IsEbootName(safeRel))
                    {
                        result.EbootRelativePath = safeRel;
                    }
                }
            }

            return result;
        }

        private static void WriteEntry(Stream pkg, PsPkgAesCtr ctr, ulong pkgDataOffset,
                                       PsPkgItem item, string outPath, byte[] buf)
        {
            ulong remaining = item.DataSize;
            ulong itemOff   = item.DataOffset;

            pkg.Position = (long)(pkgDataOffset + itemOff);

            using (var outFs = new FileStream(outPath, FileMode.Create, FileAccess.Write, FileShare.None, BlockSize, FileOptions.SequentialScan))
            {
                while (remaining > 0)
                {
                    int chunk = (int)Math.Min((ulong)buf.Length, remaining);
                    int read = pkg.Read(buf, 0, chunk);
                    if (read <= 0) throw new EndOfStreamException("PKG payload truncated.");

                    ctr.Decrypt(itemOff, buf, 0, read);
                    outFs.Write(buf, 0, read);

                    itemOff   += (ulong)read;
                    remaining -= (ulong)read;
                }
            }
        }

        private static string SanitiseRelativePath(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            string normalised = name.Replace('\\', '/');

            if (normalised.StartsWith("/")) return null;

            // Reject random-byte "filenames" that PSP PKG stub records sometimes carry — they
            // decrypt to bytes that pass the AES integrity but don't form a real file name.
            // Anything outside printable 7-bit ASCII (sans control chars) plus the Windows-invalid
            // set is treated as a stub and silently dropped.
            foreach (char c in normalised)
            {
                if (c < 0x20 || c == 0x7F) return null;                              // control
                if (c > 0x7E) return null;                                           // high-bit / encrypted noise
                if (c == '<' || c == '>' || c == '"' || c == '|' || c == '*' || c == '?') return null;
            }

            var parts = normalised.Split('/');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length == 0) continue;
                if (parts[i] == "." || parts[i] == "..") return null;
                if (parts[i].Contains(":")) return null;
            }

            return string.Join(Path.DirectorySeparatorChar.ToString(),
                               Array.FindAll(parts, p => p.Length > 0));
        }

        private static string DescribeBytes(string name)
        {
            if (string.IsNullOrEmpty(name)) return "<empty>";
            var sb = new System.Text.StringBuilder();
            foreach (char c in name)
            {
                if (c >= 0x20 && c < 0x7F) sb.Append(c);
                else sb.AppendFormat("\\x{0:X2}", (int)c);
                if (sb.Length > 64) { sb.Append('…'); break; }
            }
            return sb.ToString();
        }

        /// <summary>True for EBOOT.BIN (PS3) and EBOOT.PBP (PSP) — both are the launcher target for their emulator.</summary>
        private static bool IsEbootName(string rel)
        {
            string n = Path.GetFileName(rel);
            return string.Equals(n, "EBOOT.BIN", StringComparison.OrdinalIgnoreCase)
                || string.Equals(n, "EBOOT.PBP", StringComparison.OrdinalIgnoreCase);
        }
    }
}
