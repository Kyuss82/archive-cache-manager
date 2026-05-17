/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * NoPayStation TSV database — reads one or more .tsv files (typically named
 * PS3_GAMES.tsv, PS3_DLC.tsv, PSP_GAMES.tsv, PSV_GAMES.tsv, etc.) into a unified
 * in-memory lookup, keyed by Title ID and Content ID. Used to auto-populate
 * licenses for the Sony PKG flows:
 *   • PSV: zRIF column → decoded RIF → ux0:license/app/&lt;TITLE_ID&gt;/&lt;contentid&gt;.rif
 *   • PS3: RAP column   → 16-byte binary → dev_hdd0/home/00000001/exdata/&lt;contentid&gt;.rap
 *   • PSP: RAP column   → 16-byte binary → PSP/LICENSE/&lt;contentid&gt;.rap
 *
 * TSV schema (NPS canonical, 12 columns):
 *   1. Title ID         e.g. PCSB00963 / BLES01047 / ULES01513
 *   2. Region           e.g. EU / US / JP
 *   3. Name
 *   4. PKG direct link  (https://zeus.dl.playstation.net/...)
 *   5. zRIF             (PSV TSVs) OR RAP (PS3/PSP TSVs) — column header determines which
 *   6. Content ID       e.g. EP4395-PCSB00963_00-G000000000000885
 *   7. Last Modification Date
 *   8. Original Name
 *   9. File Size
 *   10. SHA256
 *   11. Required FW
 *   12. App Version
 *
 * Missing / unknown values are commonly the literal string "MISSING" — we treat
 * any empty-or-MISSING zRIF/RAP as absent.
 */
using System;
using System.Collections.Generic;
using System.IO;

namespace ArchiveCacheManager
{
    public class NpsEntry
    {
        public string TitleId;
        public string ContentId;
        public string Region;
        public string Name;
        public string PkgUrl;
        public string Zrif;       // PSV only — base64+deflate-encoded RIF
        public string RapHex;     // PS3/PSP only — 32 hex chars
        public string Sha256;
        public long SizeBytes;
        public string RequiredFw;
        public string AppVersion;
        public string SourceFile;
    }

    /// <summary>
    /// Row from a *_UPDATES.tsv (NPS): a single signed Sony update PKG, no Content ID.
    /// One Title ID can have several entries (e.g. v1.01 and v1.02). These are NOT licenses
    /// — they're patches that get staged under &lt;Vita3K&gt;/ux0/patch/&lt;TID&gt;/ on install.
    /// </summary>
    public class NpsUpdateEntry
    {
        public string TitleId;
        public string Region;
        public string Name;
        public string UpdateVersion;
        public string RequiredFw;
        public string PkgUrl;
        public string Sha256;
        public long SizeBytes;
        public string SourceFile;
    }

    public static class NpsDb
    {
        private static readonly Dictionary<string, NpsEntry> mByTitleId   = new Dictionary<string, NpsEntry>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, NpsEntry> mByContentId = new Dictionary<string, NpsEntry>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, List<NpsUpdateEntry>> mUpdatesByTitleId = new Dictionary<string, List<NpsUpdateEntry>>(StringComparer.OrdinalIgnoreCase);
        private static bool mLoaded;
        private static readonly object SyncRoot = new object();

        public static NpsEntry LookupByTitleId(string titleId)
        {
            EnsureLoaded();
            return !string.IsNullOrWhiteSpace(titleId) && mByTitleId.TryGetValue(titleId.Trim(), out NpsEntry e) ? e : null;
        }

        public static NpsEntry LookupByContentId(string contentId)
        {
            EnsureLoaded();
            return !string.IsNullOrWhiteSpace(contentId) && mByContentId.TryGetValue(contentId.Trim(), out NpsEntry e) ? e : null;
        }

        /// <summary>Returns the list of update PKGs NPS knows for the given title, or null if none.</summary>
        public static IReadOnlyList<NpsUpdateEntry> LookupUpdatesByTitleId(string titleId)
        {
            EnsureLoaded();
            if (string.IsNullOrWhiteSpace(titleId)) return null;
            return mUpdatesByTitleId.TryGetValue(titleId.Trim(), out var list) ? list : null;
        }

        public static int LoadedEntryCount
        {
            get { EnsureLoaded(); return mByTitleId.Count; }
        }

        public static int LoadedUpdateRowCount
        {
            get { EnsureLoaded(); int n = 0; foreach (var kv in mUpdatesByTitleId) n += kv.Value.Count; return n; }
        }

        /// <summary>Force a re-scan on next lookup. Call after the user changes NpsDbPath.</summary>
        public static void Invalidate()
        {
            lock (SyncRoot)
            {
                mByTitleId.Clear();
                mByContentId.Clear();
                mUpdatesByTitleId.Clear();
                mLoaded = false;
            }
        }

        private static void EnsureLoaded()
        {
            if (mLoaded) return;
            lock (SyncRoot)
            {
                if (mLoaded) return;
                mLoaded = true;

                string path = Config.NpsDbPath;
                if (string.IsNullOrWhiteSpace(path))
                {
                    Logger.Log("NpsDb: NpsDbPath not configured — skipping NoPayStation lookups.");
                    return;
                }
                string resolved = PathUtils.GetAbsolutePath(path);
                if (Directory.Exists(resolved))
                {
                    foreach (string tsv in Directory.GetFiles(resolved, "*.tsv", SearchOption.AllDirectories))
                    {
                        TryLoadTsv(tsv);
                    }
                }
                else if (File.Exists(resolved))
                {
                    TryLoadTsv(resolved);
                }
                else
                {
                    Logger.Log(string.Format("NpsDb: configured path {0} does not exist — skipping.", resolved));
                    return;
                }
                Logger.Log(string.Format("NpsDb: loaded {0} unique TITLE_ID entries and {1} update rows from NoPayStation TSV(s).",
                    mByTitleId.Count, LoadedUpdateRowCount));
            }
        }

        private static void TryLoadTsv(string path)
        {
            try
            {
                using (var reader = new StreamReader(path))
                {
                    string headerLine = reader.ReadLine();
                    if (string.IsNullOrEmpty(headerLine)) return;

                    string[] headers = headerLine.Split('\t');
                    int idxTitle    = IndexOf(headers, "Title ID");
                    int idxRegion   = IndexOf(headers, "Region");
                    int idxName     = IndexOf(headers, "Name");
                    int idxPkgUrl   = IndexOf(headers, "PKG direct link");
                    int idxZrif     = IndexOf(headers, "zRIF");
                    int idxRap      = IndexOf(headers, "RAP");
                    int idxContent  = IndexOf(headers, "Content ID");
                    int idxSize     = IndexOf(headers, "File Size");
                    int idxSha256   = IndexOf(headers, "SHA256");
                    int idxFwLong   = IndexOf(headers, "Required FW VERSION");
                    int idxFw       = idxFwLong >= 0 ? idxFwLong : IndexOf(headers, "Required FW");
                    int idxApp      = IndexOf(headers, "App Version");
                    int idxUpdateVer = IndexOf(headers, "Update Version");

                    if (idxTitle < 0)
                    {
                        Logger.Log(string.Format("NpsDb: {0} missing Title ID column — skipped.", path));
                        return;
                    }

                    // Schema discriminator: NPS *_UPDATES.tsv files have "Update Version" but no "Content ID"
                    // (the PKG is a signed patch, not a per-license content). Route those to mUpdatesByTitleId
                    // instead of trying to make them masquerade as license entries (which would clobber the
                    // base-game zRIF row for the same Title ID due to last-write-wins).
                    bool isUpdateTsv = idxContent < 0 && idxUpdateVer >= 0;

                    if (!isUpdateTsv && idxContent < 0)
                    {
                        Logger.Log(string.Format("NpsDb: {0} missing Content ID column and no Update Version either — skipped.", path));
                        return;
                    }

                    int rowCount = 0;
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Length == 0) continue;
                        string[] cols = line.Split('\t');
                        if (cols.Length <= idxTitle) continue;

                        string titleId = cols[idxTitle].Trim();
                        if (string.IsNullOrEmpty(titleId)) continue;

                        if (isUpdateTsv)
                        {
                            string pkgUrl = NormaliseValue(SafeColumn(cols, idxPkgUrl));
                            if (string.IsNullOrEmpty(pkgUrl)) continue;
                            var u = new NpsUpdateEntry
                            {
                                TitleId       = titleId,
                                Region        = SafeColumn(cols, idxRegion),
                                Name          = SafeColumn(cols, idxName),
                                UpdateVersion = SafeColumn(cols, idxUpdateVer),
                                RequiredFw    = SafeColumn(cols, idxFw),
                                PkgUrl        = pkgUrl,
                                Sha256        = NormaliseValue(SafeColumn(cols, idxSha256)),
                                SourceFile    = Path.GetFileName(path),
                            };
                            if (long.TryParse(SafeColumn(cols, idxSize), out long usz)) u.SizeBytes = usz;
                            if (!mUpdatesByTitleId.TryGetValue(titleId, out var list))
                            {
                                list = new List<NpsUpdateEntry>();
                                mUpdatesByTitleId[titleId] = list;
                            }
                            list.Add(u);
                            rowCount++;
                            continue;
                        }

                        if (cols.Length <= idxContent) continue;

                        var entry = new NpsEntry
                        {
                            TitleId    = titleId,
                            ContentId  = SafeColumn(cols, idxContent),
                            Region     = SafeColumn(cols, idxRegion),
                            Name       = SafeColumn(cols, idxName),
                            PkgUrl     = NormaliseValue(SafeColumn(cols, idxPkgUrl)),
                            Zrif       = NormaliseValue(SafeColumn(cols, idxZrif)),
                            RapHex     = NormaliseValue(SafeColumn(cols, idxRap)),
                            Sha256     = NormaliseValue(SafeColumn(cols, idxSha256)),
                            RequiredFw = SafeColumn(cols, idxFw),
                            AppVersion = SafeColumn(cols, idxApp),
                            SourceFile = Path.GetFileName(path),
                        };
                        if (long.TryParse(SafeColumn(cols, idxSize), out long sz)) entry.SizeBytes = sz;

                        // Last-write-wins so users can override with a custom TSV listed alphabetically later.
                        mByTitleId[titleId] = entry;
                        if (!string.IsNullOrEmpty(entry.ContentId))
                        {
                            mByContentId[entry.ContentId] = entry;
                        }
                        rowCount++;
                    }
                    Logger.Log(string.Format("NpsDb: {0} → {1} rows{2}.", Path.GetFileName(path), rowCount,
                        isUpdateTsv ? " (update PKGs)" : string.Empty));
                }
            }
            catch (Exception ex)
            {
                Logger.Log(string.Format("NpsDb: failed to parse {0}: {1}", path, ex.Message), Logger.LogLevel.Exception);
            }
        }

        private static int IndexOf(string[] headers, string name)
        {
            for (int i = 0; i < headers.Length; i++)
            {
                if (string.Equals(headers[i].Trim(), name, StringComparison.OrdinalIgnoreCase)) return i;
            }
            return -1;
        }

        private static string SafeColumn(string[] cols, int idx) =>
            idx >= 0 && idx < cols.Length ? cols[idx].Trim() : null;

        /// <summary>NPS uses "MISSING" as a placeholder for unknown values — strip those.</summary>
        private static string NormaliseValue(string v) =>
            !string.IsNullOrWhiteSpace(v) && !string.Equals(v, "MISSING", StringComparison.OrdinalIgnoreCase) ? v : null;
    }
}
