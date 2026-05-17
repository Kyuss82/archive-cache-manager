/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Scanning + per-file parsing half of `LocalPkgIndexer`, split off into a
 * partial-class file in v2.74. The orchestration (BuildIndex / CountFiles /
 * Save / Load / ResolveManifestPath / FoldersForPlatform / the `Reporter`
 * delegate) stays in `LocalPkgIndexer.cs`; everything below is the per-
 * platform scanning logic + helpers it calls.
 *
 * Membership of this file (all `private static` on the partial class):
 *   • Wii U scan: IsWiiuTmdFile, ScanWiiu, AddWiiuEntry
 *   • 3DS scan: ScanCtr3ds, ReadCiaTitleId, AddCtr3dsEntry
 *   • Sony PKG scan: SonyUpdateFileRegex, Categorise, Matches, ExtractTitleId,
 *     TryLocateSiblingRap, EnumeratePkgs, EnumerateZipWrappers,
 *     PsxHeaderSnapshotBytes, ProcessZipWrapper, FindRapEntryFor, ProcessOnePsxPkg
 *   • Shared helpers: EntryRole enum, LabelFor, EmitLog, SafeFileLength, ReadExactly
 *
 * Everything is `private` on `LocalPkgIndexer`, accessible from the orchestrator
 * file because they're both `public static partial class LocalPkgIndexer`.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;

namespace ArchiveCacheManager
{
    public static partial class LocalPkgIndexer
    {
        // ─── Wii U scanning ──────────────────────────────────────────────────────
        // Two shapes are supported per folder root:
        //   (a) Loadiine layout — one subfolder per title containing a `.tmd` + `.app` files.
        //       We point WiiuTmd.Parse at the .tmd and read title_id (8 bytes BE) directly.
        //   (b) `.wua` files — zarchive archives. We do NOT extract them here (would need
        //       zarchive.exe and a full unpack pass per scan); we just record them keyed
        //       by `_v<version>` filename when it follows the conventional naming.
        // title_id_high (high 32 bits):
        //       0x00050000 → base (skipped)
        //       0x0005000C → DLC
        //       0x0005000E → update
        //       0x00050002 → demo (skipped)
        // Wii U dumps come in several naming flavours:
        //   • Loadiine layout : files end in `.tmd` (e.g. `title.tmd`)
        //   • NUS / wud-dump  : files named `tmd.16`, `tmd.32`, etc. — region code after the dot
        //   • Cdecrypt output : just `tmd`
        // Match all three shapes via filename rules instead of a glob, so we don't miss any.
        private static bool IsWiiuTmdFile(string fullPath)
        {
            string name = Path.GetFileName(fullPath);
            if (string.IsNullOrEmpty(name)) return false;
            string lc = name.ToLowerInvariant();
            if (lc.EndsWith(".tmd"))    return true;       // title.tmd, foo.tmd
            if (lc == "tmd")            return true;       // bare 'tmd'
            if (lc.StartsWith("tmd."))  return true;       // tmd.16, tmd.32, tmd.eu
            return false;
        }

        private static void ScanWiiu(string folder, LocalPkgManifest manifest, LocalPkgScanProgress progress, Reporter onProgress, CancellationToken cancellationToken = default)
        {
            EnumerationOptions opts = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true };

            // TMD files in the source tree (Loadiine + NUS-dump + bare).
            foreach (string tmdPath in Directory.EnumerateFiles(folder, "*", opts))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!IsWiiuTmdFile(tmdPath)) continue;
                // Some network shares / shell virtual filesystems surface zip entries as
                // pseudo-paths (e.g. `<zip>\tmd.32`) that match IsWiiuTmdFile but aren't real
                // files. Skip those — the explicit `.zip` wrapper scanner below will handle the
                // archive itself.
                if (!File.Exists(tmdPath))
                {
                    EmitLog(progress, onProgress, string.Format("[skip] {0} — virtual / inaccessible path (zip scanner will handle the wrapper)", tmdPath));
                    progress.Skipped++;
                    continue;
                }
                progress.FilesSeen++;
                progress.CurrentFile = tmdPath;
                onProgress?.Invoke(progress);
                try
                {
                    var tmd = WiiuTmd.Parse(tmdPath);
                    AddWiiuEntry(manifest, progress, tmdPath, tmd.TitleId, (int)tmd.TitleVersion, SafeFileLength(tmdPath), onProgress);
                }
                catch (Exception ex)
                {
                    Logger.Log(string.Format("LocalPkgIndexer/wiiu: TMD parse failed for {0}: {1}", tmdPath, ex.Message));
                    EmitLog(progress, onProgress, string.Format("[error] {0}: {1}", tmdPath, ex.Message));
                    progress.Errors++;
                }
            }

            // .zip wrappers — read the embedded .tmd in-stream and parse it.
            foreach (string zipPath in Directory.EnumerateFiles(folder, "*.zip", opts))
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress.FilesSeen++;
                progress.CurrentFile = zipPath;
                onProgress?.Invoke(progress);
                try
                {
                    using (var fs = new FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (var zip = new ZipArchive(fs, ZipArchiveMode.Read))
                    {
                        bool sawAnyTmd = false;
                        foreach (var e in zip.Entries)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            if (!IsWiiuTmdFile(e.FullName)) continue;
                            sawAnyTmd = true;
                            string tmpDir = Path.Combine(Path.GetTempPath(), "ACM_WiiuTmd_" + Guid.NewGuid().ToString("N"));
                            Directory.CreateDirectory(tmpDir);
                            string tmpTmd = Path.Combine(tmpDir, Path.GetFileName(e.FullName));
                            try
                            {
                                e.ExtractToFile(tmpTmd, overwrite: true);
                                var tmd = WiiuTmd.Parse(tmpTmd);
                                AddWiiuEntry(manifest, progress, zipPath + "!" + e.FullName, tmd.TitleId, (int)tmd.TitleVersion, e.Length, onProgress);
                            }
                            catch (Exception exInner)
                            {
                                Logger.Log(string.Format("LocalPkgIndexer/wiiu: zip TMD parse failed for {0}!{1}: {2}", zipPath, e.FullName, exInner.Message));
                                EmitLog(progress, onProgress, string.Format("[error] {0}!{1}: {2}", zipPath, e.FullName, exInner.Message));
                                progress.Errors++;
                            }
                            finally
                            {
                                try { Directory.Delete(tmpDir, true); } catch { }
                            }
                        }
                        if (!sawAnyTmd)
                        {
                            EmitLog(progress, onProgress, string.Format("[skip] {0} — no TMD entry inside (.tmd / tmd.* / 'tmd')", zipPath));
                            progress.Skipped++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(string.Format("LocalPkgIndexer/wiiu: zip scan failed for {0}: {1}", zipPath, ex.Message));
                    progress.Errors++;
                }
            }

            // .wua archives — we don't have a quick TMD-only reader for them, so we fall back
            // to a filename heuristic: zarchive's convention is "<TitleId16hex>_v<ver>.wua".
            foreach (string wua in Directory.EnumerateFiles(folder, "*.wua", opts))
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress.FilesSeen++;
                progress.CurrentFile = wua;
                onProgress?.Invoke(progress);
                try
                {
                    string name = Path.GetFileNameWithoutExtension(wua);
                    int v = name.IndexOf("_v", StringComparison.OrdinalIgnoreCase);
                    string tidHex = v >= 0 ? name.Substring(0, v) : name;
                    if (tidHex.Length != 16 || !ulong.TryParse(tidHex, System.Globalization.NumberStyles.HexNumber, null, out ulong tid))
                    {
                        progress.Skipped++;
                        Logger.Log(string.Format("LocalPkgIndexer/wiiu: skipping {0} — name not in <TID16>_v<ver>.wua form.", wua));
                        continue;
                    }
                    int version = 0;
                    if (v >= 0) int.TryParse(name.Substring(v + 2), out version);
                    AddWiiuEntry(manifest, progress, wua, tid, version, SafeFileLength(wua), onProgress);
                }
                catch (Exception ex)
                {
                    Logger.Log(string.Format("LocalPkgIndexer/wiiu: wua scan failed for {0}: {1}", wua, ex.Message));
                    progress.Errors++;
                }
            }
        }

        private static void AddWiiuEntry(LocalPkgManifest manifest, LocalPkgScanProgress progress, string sourcePath, ulong titleId, int titleVersion, long sizeBytes, Reporter onProgress = null)
        {
            uint high = (uint)(titleId >> 32);
            uint low  = (uint)(titleId & 0xFFFFFFFF);
            EntryRole role;
            string skipReason = null;
            switch (high)
            {
                case 0x00050000: role = EntryRole.Skip;   skipReason = "base game (already in library)";   break;   // retail base
                case 0x0005000E: role = EntryRole.Update; break;
                case 0x0005000C: role = EntryRole.Dlc;    break;
                case 0x00050002: role = EntryRole.Skip;   skipReason = "demo";                              break;
                case 0x00050010: role = EntryRole.Skip;   skipReason = "system title";                     break;
                default:         role = EntryRole.Skip;   skipReason = "unknown title_id_high";            break;
            }
            if (role == EntryRole.Skip)
            {
                EmitLog(progress, onProgress, string.Format("[skip] {0} — Wii U title_id=0x{1:X16} ({2})", sourcePath, titleId, skipReason));
                progress.Skipped++;
                return;
            }
            string lowKey = low.ToString("X8");
            string baseTid = "00050000" + lowKey;     // the matching base title id (what's installed in the LB library)
            if (!manifest.Titles.TryGetValue(baseTid, out var bucket))
            {
                bucket = new LocalPkgTitle();
                manifest.Titles[baseTid] = bucket;
            }
            var entry = new LocalPkgEntry
            {
                ContentId   = string.Format("{0:X16}_v{1}", titleId, titleVersion),
                TitleId     = titleId.ToString("X16"),
                PkgPath     = sourcePath,
                Size        = sizeBytes,
                ContentType = high,
            };
            if (role == EntryRole.Update) { bucket.Updates.Add(entry); progress.Updates++; }
            else                          { bucket.Dlcs.Add(entry);    progress.Dlcs++;    }
            progress.Indexed++;
            EmitLog(progress, onProgress, string.Format("[idx]  {0} — Wii U title_id=0x{1:X16} → base 0x{2} {3}",
                sourcePath, titleId, baseTid, role == EntryRole.Update ? "UPDATE" : "DLC"));
        }

        // ─── 3DS scanning ───────────────────────────────────────────────────────
        // .cia layout (psdevwiki):
        //   0x00..0x20    : header  { archive_header_size_le32, type_be16, version_be16,
        //                              cert_size_le32, ticket_size_le32, tmd_size_le32,
        //                              meta_size_le32, content_size_le64, content_index[0x2000] }
        //   align(64)     : cert chain (cert_size bytes)
        //   align(64)     : ticket    (ticket_size bytes)
        //   align(64)     : tmd       (tmd_size bytes)  ← title_id at offset 0x18C of the TMD
        //   align(64)     : content
        //   align(64)     : meta (optional)
        // 3DS title_id_high categories (per 3dbrew.org/wiki/Titles):
        //       0x00040000 → Application (base game)               — SKIPPED, already in LB lib
        //       0x00040001 → System Application                    — SKIPPED
        //       0x00040002 → System Data Archive (CTR/TWL)         — SKIPPED
        //       0x00040003 → System Module                         — SKIPPED
        //       0x00040010 → System Applet                         — SKIPPED
        //       0x0004000E → Game Update (CIA patch for retail)    — UPDATE (what we want)
        //       0x00040020 → AutoUpdateContent (system CDN auto-update) — SKIPPED (not a game patch)
        //       0x0004008C → DLC                                    — DLC
        // Note: prior to v2.60 this mistakenly mapped 0x00040020 to UPDATE; that's the
        // *system* CDN auto-update bucket. Retail 3DS game updates are 0x0004000E.
        private static void ScanCtr3ds(string folder, LocalPkgManifest manifest, LocalPkgScanProgress progress, Reporter onProgress, CancellationToken cancellationToken = default)
        {
            EnumerationOptions opts = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true };

            // Raw TMD files (NUS dump layout — same family as Wii U, the TMD on-disk format is
            // identical between Wii U and 3DS modulo the title id namespace).
            foreach (string anyPath in Directory.EnumerateFiles(folder, "*", opts))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!IsWiiuTmdFile(anyPath)) continue;
                if (!File.Exists(anyPath))
                {
                    // Virtual / shell-view path that pretended to be a file — skip silently
                    // (the zip wrapper scanner below handles real archives).
                    EmitLog(progress, onProgress, string.Format("[skip] {0} — virtual / inaccessible path (zip scanner will handle the wrapper)", anyPath));
                    progress.Skipped++;
                    continue;
                }
                progress.FilesSeen++;
                progress.CurrentFile = anyPath;
                onProgress?.Invoke(progress);
                try
                {
                    var tmd = WiiuTmd.Parse(anyPath);
                    AddCtr3dsEntry(manifest, progress, anyPath, tmd.TitleId, (int)tmd.TitleVersion, SafeFileLength(anyPath), onProgress);
                }
                catch (Exception ex)
                {
                    Logger.Log(string.Format("LocalPkgIndexer/3ds: TMD parse failed for {0}: {1}", anyPath, ex.Message));
                    EmitLog(progress, onProgress, string.Format("[error] {0}: {1}", anyPath, ex.Message));
                    progress.Errors++;
                }
            }

            // Bare .cia files
            foreach (string ciaPath in Directory.EnumerateFiles(folder, "*.cia", opts))
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress.FilesSeen++;
                progress.CurrentFile = ciaPath;
                onProgress?.Invoke(progress);

                try
                {
                    using (var fs = new FileStream(ciaPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        var (tid, tver) = ReadCiaTitleId(fs);
                        if (tid == 0) { progress.Skipped++; continue; }
                        AddCtr3dsEntry(manifest, progress, ciaPath, tid, tver, SafeFileLength(ciaPath), onProgress);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(string.Format("LocalPkgIndexer/3ds: CIA parse failed for {0}: {1}", ciaPath, ex.Message));
                    progress.Errors++;
                }
            }

            // .zip wrappers carrying .cia inside
            foreach (string zipPath in Directory.EnumerateFiles(folder, "*.zip", opts))
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress.FilesSeen++;
                progress.CurrentFile = zipPath;
                onProgress?.Invoke(progress);
                try
                {
                    using (var fs = new FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (var zip = new ZipArchive(fs, ZipArchiveMode.Read))
                    {
                        bool sawAny = false;
                        foreach (var e in zip.Entries)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            bool isCia = e.FullName.EndsWith(".cia", StringComparison.OrdinalIgnoreCase);
                            bool isTmd = IsWiiuTmdFile(e.FullName);    // NUS-dump style TMD inside the zip
                            if (!isCia && !isTmd) continue;
                            sawAny = true;
                            try
                            {
                                if (isCia)
                                {
                                    // Snapshot first ~256 KB — covers CIA header + cert + ticket + TMD comfortably.
                                    byte[] snapshot;
                                    using (var es = e.Open())
                                    using (var ms = new MemoryStream())
                                    {
                                        int snapshotLen = (int)Math.Min(e.Length, 256 * 1024);
                                        byte[] buf = new byte[81920];
                                        int total = 0, read;
                                        while (total < snapshotLen && (read = es.Read(buf, 0, Math.Min(buf.Length, snapshotLen - total))) > 0)
                                        {
                                            ms.Write(buf, 0, read);
                                            total += read;
                                        }
                                        snapshot = ms.ToArray();
                                    }
                                    using (var snapStream = new MemoryStream(snapshot))
                                    {
                                        var (tid, tver) = ReadCiaTitleId(snapStream);
                                        if (tid == 0) { progress.Skipped++; continue; }
                                        AddCtr3dsEntry(manifest, progress, zipPath + "!" + e.FullName, tid, tver, e.Length, onProgress);
                                    }
                                }
                                else
                                {
                                    // Raw TMD inside the wrapper — extract to a temp file (TMDs are tiny) and parse.
                                    string tmpDir = Path.Combine(Path.GetTempPath(), "ACM_Ctr3dsTmd_" + Guid.NewGuid().ToString("N"));
                                    Directory.CreateDirectory(tmpDir);
                                    string tmpTmd = Path.Combine(tmpDir, Path.GetFileName(e.FullName));
                                    try
                                    {
                                        e.ExtractToFile(tmpTmd, overwrite: true);
                                        var tmd = WiiuTmd.Parse(tmpTmd);
                                        AddCtr3dsEntry(manifest, progress, zipPath + "!" + e.FullName, tmd.TitleId, (int)tmd.TitleVersion, e.Length, onProgress);
                                    }
                                    finally
                                    {
                                        try { Directory.Delete(tmpDir, true); } catch { }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Log(string.Format("LocalPkgIndexer/3ds: zip-entry parse failed for {0}!{1}: {2}", zipPath, e.FullName, ex.Message));
                                EmitLog(progress, onProgress, string.Format("[error] {0}!{1}: {2}", zipPath, e.FullName, ex.Message));
                                progress.Errors++;
                            }
                        }
                        if (!sawAny)
                        {
                            EmitLog(progress, onProgress, string.Format("[skip] {0} — no .cia or TMD entry inside", zipPath));
                            progress.Skipped++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(string.Format("LocalPkgIndexer/3ds: zip scan failed for {0}: {1}", zipPath, ex.Message));
                    progress.Errors++;
                }
            }
        }

        private static (ulong titleId, int version) ReadCiaTitleId(Stream fs)
        {
            byte[] header = new byte[0x20];
            if (fs.Read(header, 0, header.Length) != header.Length) return (0, 0);
            uint archiveHeaderSize = BitConverter.ToUInt32(header, 0x00);
            uint certSize          = BitConverter.ToUInt32(header, 0x08);
            uint ticketSize        = BitConverter.ToUInt32(header, 0x0C);
            uint tmdSize           = BitConverter.ToUInt32(header, 0x10);
            if (archiveHeaderSize == 0 || tmdSize == 0) return (0, 0);

            long Align64(long pos) => (pos + 63) & ~63L;
            long pos = Align64(archiveHeaderSize);
            pos = Align64(pos + certSize);
            pos = Align64(pos + ticketSize);
            // Now pointing at the TMD blob. CTR TMD has the same layout family as Wii U (title_id @ 0x18C).
            if (tmdSize < 0x1E0) return (0, 0);
            fs.Position = pos + 0x18C;
            byte[] tidBe = new byte[8];
            if (!ReadExactly(fs, tidBe, 0, 8)) return (0, 0);
            Array.Reverse(tidBe);
            ulong tid = BitConverter.ToUInt64(tidBe, 0);

            fs.Position = pos + 0x1DC;
            byte[] verBe = new byte[2];
            if (!ReadExactly(fs, verBe, 0, 2)) return (0, 0);
            int version = (verBe[0] << 8) | verBe[1];

            return (tid, version);
        }

        private static void AddCtr3dsEntry(LocalPkgManifest manifest, LocalPkgScanProgress progress, string sourcePath, ulong titleId, int version, long sizeBytes, Reporter onProgress = null)
        {
            uint high = (uint)(titleId >> 32);
            uint low  = (uint)(titleId & 0xFFFFFFFF);
            EntryRole role;
            string skipReason = null;
            switch (high)
            {
                case 0x00040000: role = EntryRole.Skip;   skipReason = "base game (already in library)"; break;
                case 0x0004000E: role = EntryRole.Update; break;
                case 0x0004008C: role = EntryRole.Dlc;    break;
                case 0x00040001: role = EntryRole.Skip;   skipReason = "system application";       break;
                case 0x00040002: role = EntryRole.Skip;   skipReason = "system data archive";      break;
                case 0x00040003: role = EntryRole.Skip;   skipReason = "system module";            break;
                case 0x00040010: role = EntryRole.Skip;   skipReason = "system applet";            break;
                case 0x00040020: role = EntryRole.Skip;   skipReason = "system auto-update content (not a game patch)"; break;
                default:         role = EntryRole.Skip;   skipReason = "unknown title_id_high";    break;
            }
            if (role == EntryRole.Skip)
            {
                EmitLog(progress, onProgress, string.Format("[skip] {0} — 3DS title_id=0x{1:X16} ({2})", sourcePath, titleId, skipReason));
                progress.Skipped++;
                return;
            }
            string lowKey = low.ToString("X8");
            string baseTid = "00040000" + lowKey;
            if (!manifest.Titles.TryGetValue(baseTid, out var bucket))
            {
                bucket = new LocalPkgTitle();
                manifest.Titles[baseTid] = bucket;
            }
            var entry = new LocalPkgEntry
            {
                ContentId   = string.Format("{0:X16}_v{1}", titleId, version),
                TitleId     = titleId.ToString("X16"),
                PkgPath     = sourcePath,
                Size        = sizeBytes,
                ContentType = high,
            };
            if (role == EntryRole.Update) { bucket.Updates.Add(entry); progress.Updates++; }
            else                          { bucket.Dlcs.Add(entry);    progress.Dlcs++;    }
            progress.Indexed++;
            EmitLog(progress, onProgress, string.Format("[idx]  {0} — 3DS title_id=0x{1:X16} → base 0x{2} {3}",
                sourcePath, titleId, baseTid, role == EntryRole.Update ? "UPDATE" : "DLC"));
        }

        private enum EntryRole { Skip, Update, Dlc }

        // Sony's CDN delivers update PKGs under filenames shaped like:
        //   PS3: <content_id>-A<aaaa>-V<vvvv>-PE.pkg
        //   PSV: <content_id>-A<aaaa>-V<vvvv>-<sha1>-PE.pkg     (40-char hex SHA1 in the middle)
        // (-A is the app version, -V is the SDK version, -PE the suffix). For PSV the SHA1
        // body chunk is optional in the regex so both shapes are caught. This is the only
        // reliable update-vs-base discriminator on Sony's side — there is no dedicated
        // content_type for update PKGs.
        private static readonly System.Text.RegularExpressions.Regex SonyUpdateFileRegex =
            new System.Text.RegularExpressions.Regex(@"-A\d{4}-V\d{4}(-[0-9a-fA-F]{40})?-PE\.pkg$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

        private static EntryRole Categorise(LocalPkgPlatform platform, uint contentType, string contentId, string filename)
        {
            string cid = contentId ?? string.Empty;
            string fn  = filename ?? string.Empty;
            bool looksLikePatch = cid.IndexOf("PATCH", StringComparison.OrdinalIgnoreCase) >= 0
                                  || SonyUpdateFileRegex.IsMatch(fn);

            if (platform == LocalPkgPlatform.Ps3)
            {
                // Update PKGs are identified by *filename* (Sony's CDN naming
                // `<content_id>-A<aver>-V<sdkver>-PE.pkg`) or by the `PATCH` keyword in the
                // content id. They have no dedicated content_type — Sony ships PS3 updates with
                // the *same* content_type as the game they patch (so an update for a 0x05 game
                // is also 0x05). Filename always wins over content_type for this discriminator.
                if (looksLikePatch) return EntryRole.Update;

                switch (contentType)
                {
                    // 0x04 = PS3 add-on content (DLC) — NoPayStation distributes these under
                    // PS3_DLCS.tsv. 0x0F is the legacy PSN DLC marker (same role, older releases).
                    case 0x04: return EntryRole.Dlc;
                    case 0x0F: return EntryRole.Dlc;
                    // 0x05 = base PS3 game (NPDRM), 0x09 = theme, 0x0A = avatar.
                    case 0x05: return EntryRole.Skip;
                    case 0x09: return EntryRole.Skip;
                    case 0x0A: return EntryRole.Skip;
                    default:   return EntryRole.Skip;
                }
            }
            if (platform == LocalPkgPlatform.Psp)
            {
                // PSP doesn't have a dedicated update content_type — fall back to content_id / filename pattern.
                if (looksLikePatch) return EntryRole.Update;
                if (cid.IndexOf("DLC", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    cid.IndexOf("ADDCONT", StringComparison.OrdinalIgnoreCase) >= 0) return EntryRole.Dlc;
                return EntryRole.Skip;
            }
            if (platform == LocalPkgPlatform.Psv)
            {
                if (contentType == 0x16) return EntryRole.Dlc;            // PSV DLC content_type
                if (looksLikePatch)      return EntryRole.Update;          // PATCH-style content id or Sony naming
                return EntryRole.Skip;
            }
            return EntryRole.Skip;
        }

        private static bool Matches(LocalPkgPlatform platform, ushort pkgType)
        {
            if (platform == LocalPkgPlatform.Ps3) return pkgType == 0x0001;
            return pkgType == 0x0002;   // PSP + PSV both live under type 0x0002
        }

        private static string ExtractTitleId(PsParsedPkg pkg)
        {
            if (!string.IsNullOrWhiteSpace(pkg.TitleId)) return pkg.TitleId.Trim();
            string cid = pkg.Header.ContentId ?? string.Empty;
            int dash = cid.IndexOf('-');
            int under = dash >= 0 ? cid.IndexOf('_', dash + 1) : -1;
            return (dash >= 0 && under > dash + 1) ? cid.Substring(dash + 1, under - dash - 1) : null;
        }

        private static string TryLocateSiblingRap(string pkgPath, string contentId)
        {
            string dir = Path.GetDirectoryName(pkgPath);
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir) || string.IsNullOrEmpty(contentId)) return null;
            string exact = Path.Combine(dir, contentId + ".rap");
            if (File.Exists(exact)) return exact;
            // Fallback: case-insensitive scan (some scene packs use different case).
            foreach (string r in Directory.EnumerateFiles(dir, "*.rap"))
            {
                if (string.Equals(Path.GetFileNameWithoutExtension(r), contentId, StringComparison.OrdinalIgnoreCase))
                    return r;
            }
            return null;
        }

        private static IEnumerable<string> EnumeratePkgs(string root)
        {
            EnumerationOptions opts = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true };
            return Directory.EnumerateFiles(root, "*.pkg", opts);
        }

        private static IEnumerable<string> EnumerateZipWrappers(string root)
        {
            EnumerationOptions opts = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true };
            // .NET's ZipArchive only handles .zip natively; we still emit .7z/.rar so the loop can
            // at least log "wrapper found but format unsupported here" and move on. In practice the
            // user's offline mirror is the NPS Browser scene-style .zip, so .zip is the case that
            // matters.
            foreach (var ext in new[] { "*.zip", "*.7z", "*.rar" })
            {
                foreach (string p in Directory.EnumerateFiles(root, ext, opts))
                    yield return p;
            }
        }

        // Parsing the PKG header + metadata block requires a seekable stream and only the first
        // few hundred KB of the file — definitely fits within this snapshot. Big enough to cover
        // every PSx PKG metadata block we've seen in practice.
        private const int PsxHeaderSnapshotBytes = 256 * 1024;

        private static void ProcessZipWrapper(LocalPkgPlatform platform, string archivePath, LocalPkgManifest manifest, LocalPkgScanProgress progress, Reporter onProgress = null)
        {
            string ext = Path.GetExtension(archivePath);
            if (!string.Equals(ext, ".zip", StringComparison.OrdinalIgnoreCase))
            {
                Logger.Log(string.Format("LocalPkgIndexer: skipping {0} — wrapper format {1} is not handled (extract manually or repack as .zip).", archivePath, ext));
                EmitLog(progress, onProgress, string.Format("[skip] {0} — {1} wrapper not supported (only .zip)", archivePath, ext));
                progress.Skipped++;
                return;
            }

            // Use Update-mode-less Read to keep the stream forward-only friendly. .NET ZipArchive
            // can list the central directory cheaply and stream each entry on demand.
            using (var fs = new FileStream(archivePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var zip = new ZipArchive(fs, ZipArchiveMode.Read))
            {
                // Collect .pkg + .rap entries so the rap can be associated with the matching pkg by content id.
                var pkgEntries = new List<ZipArchiveEntry>();
                var rapEntries = new List<ZipArchiveEntry>();
                foreach (var e in zip.Entries)
                {
                    if (e.FullName.EndsWith(".pkg", StringComparison.OrdinalIgnoreCase)) pkgEntries.Add(e);
                    else if (e.FullName.EndsWith(".rap", StringComparison.OrdinalIgnoreCase)) rapEntries.Add(e);
                }

                if (pkgEntries.Count == 0)
                {
                    EmitLog(progress, onProgress, string.Format("[skip] {0} — no .pkg entries inside", archivePath));
                    progress.Skipped++;
                    return;
                }

                foreach (var pkgEntry in pkgEntries)
                {
                    try
                    {
                        // Snapshot the first N bytes of the entry into memory so we have a seekable
                        // stream for PsPkgReader.Parse(skipItemTable: true).
                        byte[] snapshot;
                        using (var es = pkgEntry.Open())
                        using (var ms = new MemoryStream())
                        {
                            int snapshotLen = (int)Math.Min(pkgEntry.Length, PsxHeaderSnapshotBytes);
                            byte[] buf = new byte[81920];
                            int total = 0, read;
                            while (total < snapshotLen && (read = es.Read(buf, 0, Math.Min(buf.Length, snapshotLen - total))) > 0)
                            {
                                ms.Write(buf, 0, read);
                                total += read;
                            }
                            snapshot = ms.ToArray();
                        }
                        using (var snapStream = new MemoryStream(snapshot))
                        {
                            string rapEntryName = FindRapEntryFor(rapEntries, snapStream);
                            snapStream.Position = 0;
                            ProcessOnePsxPkg(platform, snapStream,
                                pkgPath: null,
                                archivePath: archivePath,
                                archiveEntry: pkgEntry.FullName,
                                rapArchiveEntry: rapEntryName,
                                rapPath: null,
                                fileLength: pkgEntry.Length,
                                manifest, progress, onProgress);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(string.Format("LocalPkgIndexer: zip-entry parse failed for {0}!{1}: {2}", archivePath, pkgEntry.FullName, ex.Message));
                        EmitLog(progress, onProgress, string.Format("[error] {0}!{1}: {2}", archivePath, pkgEntry.FullName, ex.Message));
                        progress.Errors++;
                    }
                }
            }
        }

        /// <summary>
        /// Match a .rap entry inside the wrapper to the .pkg snapshot by reading the PKG content id.
        /// </summary>
        private static string FindRapEntryFor(List<ZipArchiveEntry> rapEntries, MemoryStream pkgSnapshot)
        {
            if (rapEntries == null || rapEntries.Count == 0) return null;
            // content_id sits at offset 0x30 of the PKG header (PsPkgReader.PkgHeaderSize precondition).
            if (pkgSnapshot.Length < 0x70) return null;
            byte[] cidBytes = new byte[48];
            pkgSnapshot.Position = 0x30;
            pkgSnapshot.Read(cidBytes, 0, 48);
            int nul = Array.IndexOf(cidBytes, (byte)0);
            string cid = Encoding.ASCII.GetString(cidBytes, 0, nul < 0 ? cidBytes.Length : nul).Trim();
            if (string.IsNullOrEmpty(cid)) return null;

            foreach (var r in rapEntries)
            {
                string name = Path.GetFileNameWithoutExtension(r.FullName);
                if (string.Equals(name, cid, StringComparison.OrdinalIgnoreCase)) return r.FullName;
            }
            return null;
        }

        private static void ProcessOnePsxPkg(LocalPkgPlatform platform, Stream pkgStream,
            string pkgPath, string archivePath, string archiveEntry, string rapArchiveEntry,
            string rapPath, long fileLength,
            LocalPkgManifest manifest, LocalPkgScanProgress progress, Reporter onProgress = null)
        {
            string label = LabelFor(pkgPath, archivePath, archiveEntry);

            var parsed = PsPkgReader.Parse(pkgStream, skipItemTable: true);
            if (!Matches(platform, parsed.Header.PkgType))
            {
                progress.Skipped++;
                EmitLog(progress, onProgress, string.Format("[skip] {0} — pkg_type=0x{1:X4} doesn't match platform {2}",
                    label, parsed.Header.PkgType, platform));
                return;
            }

            string tid = ExtractTitleId(parsed);
            if (string.IsNullOrWhiteSpace(tid))
            {
                progress.Skipped++;
                EmitLog(progress, onProgress, string.Format("[skip] {0} — couldn't derive TITLE_ID from content_id '{1}'",
                    label, parsed.Header.ContentId ?? "<null>"));
                return;
            }

            // Use the entry filename (zip case) or pkg file basename (bare case) for the heuristic.
            string sourceFile = archiveEntry ?? (pkgPath != null ? Path.GetFileName(pkgPath) : null);
            var role = Categorise(platform, parsed.ContentType, parsed.Header.ContentId, sourceFile);
            if (role == EntryRole.Skip)
            {
                progress.Skipped++;
                EmitLog(progress, onProgress, string.Format("[skip] {0} — content_type=0x{1:X2}, content_id='{2}', file='{3}' → categorised as base/theme/avatar (not update or DLC)",
                    label, parsed.ContentType, parsed.Header.ContentId ?? "<null>", sourceFile ?? "<null>"));
                return;
            }

            var entry = new LocalPkgEntry
            {
                ContentId       = parsed.Header.ContentId,
                TitleId         = tid,
                PkgPath         = pkgPath,
                ArchivePath     = archivePath,
                ArchiveEntry    = archiveEntry,
                RapPath         = rapPath,
                RapArchiveEntry = rapArchiveEntry,
                Size            = fileLength,
                ContentType     = parsed.ContentType,
            };

            if (!manifest.Titles.TryGetValue(tid, out var bucket))
            {
                bucket = new LocalPkgTitle();
                manifest.Titles[tid] = bucket;
            }
            string rapTag = entry.RapPath != null || entry.RapArchiveEntry != null ? " +rap" : "";
            if (role == EntryRole.Update)
            {
                bucket.Updates.Add(entry);
                progress.Updates++;
                EmitLog(progress, onProgress, string.Format("[idx]  {0} → {1} UPDATE (content_type=0x{2:X2}{3})", label, tid, parsed.ContentType, rapTag));
            }
            else if (role == EntryRole.Dlc)
            {
                bucket.Dlcs.Add(entry);
                progress.Dlcs++;
                EmitLog(progress, onProgress, string.Format("[idx]  {0} → {1} DLC    (content_type=0x{2:X2}{3})", label, tid, parsed.ContentType, rapTag));
            }
            progress.Indexed++;
        }

        private static string LabelFor(string pkgPath, string archivePath, string archiveEntry)
        {
            if (!string.IsNullOrEmpty(pkgPath)) return pkgPath;
            if (!string.IsNullOrEmpty(archivePath)) return archivePath + "!" + (archiveEntry ?? "<entry>");
            return "<unknown>";
        }

        private static void EmitLog(LocalPkgScanProgress progress, Reporter onProgress, string line)
        {
            if (progress == null) return;
            progress.LogLine = line;
            onProgress?.Invoke(progress);
            progress.LogLine = null;
        }

        // FileInfo.Length throws FileNotFoundException for shell-virtual / network-share
        // synthetic paths (e.g. `<zip>!entry`) that some Explorer extensions surface via
        // Directory.EnumerateFiles. Wrap the lookup so a missing/inaccessible path falls back
        // to 0 instead of taking the whole zip-wrapper scan down.
        private static long SafeFileLength(string path)
        {
            try { return new FileInfo(path).Length; }
            catch { return 0L; }
        }

        private static bool ReadExactly(Stream s, byte[] buf, int offset, int count)
        {
            int read = 0;
            while (read < count)
            {
                int n = s.Read(buf, offset + read, count - read);
                if (n <= 0) return false;
                read += n;
            }
            return true;
        }
    }
}
