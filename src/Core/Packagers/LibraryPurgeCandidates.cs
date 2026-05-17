/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Enumerates non-base file paths from every persisted LocalPkgIndexer manifest
 * (PS3 / PSP / PSV / Wii U / 3DS) so the Plugin layer can match them against
 * LaunchBox library entries and offer to purge those entries from the library
 * (without touching the files on disk).
 *
 * v2.79 — typed categories. Originally this only surfaced Updates and DLCs; the
 * scanner now also emits Theme / SystemTitle / Demo / Other, plus a manifest-
 * level Orphans bucket for entries with no resolvable parent title. Each
 * candidate carries a `Kind` string that the UI uses both as a display value
 * and to drive default checkbox state (Update + DLC pre-checked; everything
 * else surfaced but opt-in, since these are less-obvious purge targets).
 *
 * Why only `PkgPath` and not `ArchivePath`:
 *   A `.zip` wrapper indexed by `LocalPkgScanner` can hold updates AND DLCs AND
 *   the base game in the same archive (NPS-style scene packs). The manifest only
 *   records the update/DLC entries it found inside, but the LaunchBox library
 *   entry for that `.zip` typically corresponds to the BASE game. Purging the
 *   wrapper path would delete the base library entry — wrong. So we deliberately
 *   skip wrapper-only entries here. Standalone `.pkg` / `.cia` / `.wua` files on
 *   disk are by construction not the base game (the indexer skips base titles),
 *   so they're safe to surface as purge candidates.
 */
using System;
using System.Collections.Generic;
using System.IO;

namespace ArchiveCacheManager
{
    /// <summary>
    /// Stable string identifiers for candidate categories. Used both as the
    /// JSON-friendly display value in the purge UI and to drive default
    /// checkbox state. Update / Dlc pre-checked; others surfaced but opt-in.
    /// </summary>
    public static class LibraryPurgeKind
    {
        public const string Update = "Update";
        public const string Dlc    = "DLC";
        public const string Theme  = "Theme";
        public const string System = "System";
        public const string Demo   = "Demo";
        public const string Other  = "Other";

        /// <summary>Categories that the UI ticks by default (the original v2.78 scope).</summary>
        public static bool IsDefaultChecked(string kind) =>
            kind == Update || kind == Dlc;
    }

    public class LibraryPurgeCandidate
    {
        /// <summary>Normalized absolute path on disk. Lower-cased for case-insensitive lookup.</summary>
        public string PathKey;
        /// <summary>Original path as written in the manifest (not normalized).</summary>
        public string OriginalPath;
        public string TitleId;
        public string ContentId;
        public LocalPkgPlatform Platform;
        /// <summary>One of <see cref="LibraryPurgeKind"/>'s constants.</summary>
        public string Kind;
    }

    public static class LibraryPurgeCandidates
    {
        /// <summary>
        /// Loads every available platform manifest and yields one candidate per
        /// standalone non-base entry across all typed buckets (Updates, Dlcs,
        /// Themes, SystemTitles, Demos, Other) plus the manifest-level Orphans
        /// list. Caller is responsible for matching the returned `PathKey`
        /// against normalized library application paths.
        /// </summary>
        public static IEnumerable<LibraryPurgeCandidate> EnumerateAll()
        {
            foreach (LocalPkgPlatform platform in Enum.GetValues(typeof(LocalPkgPlatform)))
            {
                LocalPkgManifest manifest = null;
                try { manifest = LocalPkgIndexer.Load(platform); }
                catch (Exception ex)
                {
                    Logger.Log(string.Format("LibraryPurgeCandidates: failed to load {0} manifest: {1}", platform, ex.Message));
                }
                if (manifest == null) continue;

                foreach (var kv in manifest.Titles)
                {
                    string tid = kv.Key;
                    var t = kv.Value;
                    if (t == null) continue;
                    foreach (var c in YieldFromList(t.Updates,      tid, platform, LibraryPurgeKind.Update)) yield return c;
                    foreach (var c in YieldFromList(t.Dlcs,         tid, platform, LibraryPurgeKind.Dlc))    yield return c;
                    foreach (var c in YieldFromList(t.Themes,       tid, platform, LibraryPurgeKind.Theme))  yield return c;
                    foreach (var c in YieldFromList(t.SystemTitles, tid, platform, LibraryPurgeKind.System)) yield return c;
                    foreach (var c in YieldFromList(t.Demos,        tid, platform, LibraryPurgeKind.Demo))   yield return c;
                    foreach (var c in YieldFromList(t.Other,        tid, platform, LibraryPurgeKind.Other))  yield return c;
                }

                if (manifest.Orphans != null)
                {
                    // Orphans don't have a parent title id; fall back to the entry's own ContentId-derived TID if present.
                    foreach (var c in YieldFromList(manifest.Orphans, parentTid: null, platform, LibraryPurgeKind.Other)) yield return c;
                }
            }
        }

        private static IEnumerable<LibraryPurgeCandidate> YieldFromList(List<LocalPkgEntry> list, string parentTid, LocalPkgPlatform platform, string kind)
        {
            if (list == null) yield break;
            foreach (var e in list)
                if (TryMake(e, parentTid ?? e?.TitleId, platform, kind, out var c)) yield return c;
        }

        private static bool TryMake(LocalPkgEntry e, string titleId, LocalPkgPlatform platform, string kind, out LibraryPurgeCandidate candidate)
        {
            candidate = null;
            if (e == null) return false;
            // Skip entries that only exist inside a zip/7z/rar wrapper — see file header.
            if (!string.IsNullOrEmpty(e.ArchivePath)) return false;
            if (string.IsNullOrWhiteSpace(e.PkgPath)) return false;

            string key = NormalizeForLookup(e.PkgPath);
            if (string.IsNullOrEmpty(key)) return false;

            candidate = new LibraryPurgeCandidate
            {
                PathKey      = key,
                OriginalPath = e.PkgPath,
                TitleId      = titleId,
                ContentId    = e.ContentId,
                Platform     = platform,
                Kind         = kind,
            };
            return true;
        }

        /// <summary>
        /// Canonical key for path comparison. Resolves to an absolute path (manifests
        /// always store absolutes, but be defensive), trims trailing separators, and
        /// lower-cases for case-insensitive Windows matching.
        /// </summary>
        public static string NormalizeForLookup(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            try
            {
                string full = Path.IsPathRooted(path) ? Path.GetFullPath(path) : PathUtils.GetAbsolutePath(path);
                return full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).ToLowerInvariant();
            }
            catch
            {
                return path.Trim().ToLowerInvariant();
            }
        }
    }
}
