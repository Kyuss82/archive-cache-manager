/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Enumerates update/DLC file paths from every persisted LocalPkgIndexer manifest
 * (PS3 / PSP / PSV / Wii U / 3DS) so the Plugin layer can match them against
 * LaunchBox library entries and offer to purge those entries from the library
 * (without touching the files on disk).
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
    public class LibraryPurgeCandidate
    {
        /// <summary>Normalized absolute path on disk. Lower-cased for case-insensitive lookup.</summary>
        public string PathKey;
        /// <summary>Original path as written in the manifest (not normalized).</summary>
        public string OriginalPath;
        public string TitleId;
        public string ContentId;
        public LocalPkgPlatform Platform;
        public bool IsDlc;
    }

    public static class LibraryPurgeCandidates
    {
        /// <summary>
        /// Loads every available platform manifest and yields one candidate per
        /// standalone update/DLC entry. Caller is responsible for matching the
        /// returned `PathKey` against normalized library application paths.
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
                    if (kv.Value == null) continue;
                    if (kv.Value.Updates != null)
                        foreach (var e in kv.Value.Updates)
                            if (TryMake(e, tid, platform, isDlc: false, out var c)) yield return c;
                    if (kv.Value.Dlcs != null)
                        foreach (var e in kv.Value.Dlcs)
                            if (TryMake(e, tid, platform, isDlc: true, out var c)) yield return c;
                }
            }
        }

        private static bool TryMake(LocalPkgEntry e, string titleId, LocalPkgPlatform platform, bool isDlc, out LibraryPurgeCandidate candidate)
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
                IsDlc        = isDlc,
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
