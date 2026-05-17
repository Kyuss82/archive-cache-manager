/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Offline mirror indexer. Scans a user-supplied list of folders for *.pkg
 * files, parses each header to figure out what it is (PS3/PSP/PSV game, update
 * or DLC), and writes a JSON manifest that the auto-update / auto-DLC paths
 * consult before falling back to Sony's CDN.
 *
 * The manifest layout (one per platform, stored under each platform's update
 * cache root):
 *
 *   {
 *     "platform": "PS3",
 *     "generated_at": "2026-05-16T18:00:00Z",
 *     "titles": {
 *       "NPEB00768": {
 *         "updates": [
 *           { "content_id":"EP4295-NPEB00768_00-…", "pkg_path":"D:\\PS3\\Updates\\…pkg",
 *             "size":169123456, "sha1":"" }
 *         ],
 *         "dlcs": [
 *           { "content_id":"EP4295-NPEB00768_00-AMYDLC01000001",
 *             "pkg_path":"D:\\PS3\\DLC\\…pkg",
 *             "rap_path":"D:\\PS3\\DLC\\…rap"  // sibling .rap, NPS-style export
 *           }
 *         ]
 *       },
 *       ...
 *     }
 *   }
 *
 * Categorisation rules:
 *   • content_type 0x04                          → PS3 update
 *   • content_type 0x05                          → PS3 base game (skipped — already in library)
 *   • content_type 0x07                          → PSP base game (skipped — base PKG only, no patch concept for PSP via this stream)
 *   • content_type 0x0F                          → PS3 DLC
 *   • content_type 0x14, 0x15                    → PSV app/game (base, skipped unless it's a 'PATCH'-style title)
 *   • content_type 0x16                          → PSV DLC
 *   • PSV/PSP "update" entries → fall back to content_id heuristic: a title id seen with
 *     multiple distinct entries gets the lowest-version one tagged as base, others as
 *     updates. (PSP and PSV don't ship a clean update content_type — versioning lives
 *     inside the content id.)
 *
 * Sibling .rap pickup: for any .pkg processed we look for a `<contentid>.rap` (or
 * any *.rap whose filename without extension matches the content id case-insensitively)
 * sitting in the same folder, and record its path on the entry.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace ArchiveCacheManager
{
    // Model classes (LocalPkgPlatform / LocalPkgEntry / LocalPkgTitle / LocalPkgManifest /
    // LocalPkgScanProgress) moved to LocalPkgManifest.cs in v2.72.
    public static partial class LocalPkgIndexer
    {
        public delegate void Reporter(LocalPkgScanProgress p);

        /// <summary>
        /// Recursively scan <paramref name="folders"/> for .pkg files, build a manifest,
        /// and persist it next to the platform's update cache root.
        /// </summary>
        public static LocalPkgManifest BuildIndex(LocalPkgPlatform platform, IEnumerable<string> folders, Reporter onProgress = null, CancellationToken cancellationToken = default)
        {
            var manifest = new LocalPkgManifest
            {
                Platform    = platform.ToString().ToUpperInvariant(),
                GeneratedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            };
            var progress = new LocalPkgScanProgress();

            foreach (string folder in (folders ?? Array.Empty<string>()))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                {
                    Logger.Log(string.Format("LocalPkgIndexer: folder missing or empty — {0}", folder));
                    continue;
                }

                if (platform == LocalPkgPlatform.Wiiu)
                {
                    ScanWiiu(folder, manifest, progress, onProgress, cancellationToken);
                    continue;
                }
                if (platform == LocalPkgPlatform.Ctr3ds)
                {
                    ScanCtr3ds(folder, manifest, progress, onProgress, cancellationToken);
                    continue;
                }
                if (platform == LocalPkgPlatform.Wii)
                {
                    ScanWii(folder, manifest, progress, onProgress, cancellationToken);
                    continue;
                }

                // Bare .pkg files in the folder tree
                foreach (string pkgPath in EnumeratePkgs(folder))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    progress.FilesSeen++;
                    progress.CurrentFile = pkgPath;
                    onProgress?.Invoke(progress);

                    try
                    {
                        using (var fs = new FileStream(pkgPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            ProcessOnePsxPkg(platform, fs, pkgPath, archivePath: null, archiveEntry: null,
                                rapArchiveEntry: null,
                                rapPath: TryLocateSiblingRap(pkgPath, null),
                                fileLength: SafeFileLength(pkgPath),
                                manifest, progress, onProgress);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(string.Format("LocalPkgIndexer: parse failed for {0}: {1}", pkgPath, ex.Message));
                        progress.Errors++;
                    }
                }

                // .zip / .7z / .rar wrappers — common when the user mirrors NPS Browser scene packs
                foreach (string archivePath in EnumerateZipWrappers(folder))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    progress.FilesSeen++;
                    progress.CurrentFile = archivePath;
                    onProgress?.Invoke(progress);
                    try
                    {
                        ProcessZipWrapper(platform, archivePath, manifest, progress, onProgress);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(string.Format("LocalPkgIndexer: zip scan failed for {0}: {1}", archivePath, ex.Message));
                        EmitLog(progress, onProgress, string.Format("[error] {0}: {1}", archivePath, ex.Message));
                        progress.Errors++;
                    }
                }
            }

            onProgress?.Invoke(progress);
            return manifest;
        }

        /// <summary>
        /// Quick pre-pass that counts the files BuildIndex would visit, without parsing any of
        /// them. Used by the UI to drive a real (non-marquee) progress bar — the cost of two
        /// recursive walks is negligible compared to the parse phase.
        /// </summary>
        public static int CountFiles(LocalPkgPlatform platform, IEnumerable<string> folders)
        {
            int total = 0;
            foreach (string folder in (folders ?? Array.Empty<string>()))
            {
                if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder)) continue;
                EnumerationOptions opts = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true };

                if (platform == LocalPkgPlatform.Wiiu || platform == LocalPkgPlatform.Ctr3ds)
                {
                    // raw TMD-like files
                    try { foreach (var f in Directory.EnumerateFiles(folder, "*", opts)) if (IsWiiuTmdFile(f)) total++; } catch { }
                    // .cia (3DS only) and .wua (Wii U only) and .zip (both)
                    if (platform == LocalPkgPlatform.Ctr3ds)
                        try { foreach (var _ in Directory.EnumerateFiles(folder, "*.cia", opts)) total++; } catch { }
                    if (platform == LocalPkgPlatform.Wiiu)
                        try { foreach (var _ in Directory.EnumerateFiles(folder, "*.wua", opts)) total++; } catch { }
                    try { foreach (var _ in Directory.EnumerateFiles(folder, "*.zip", opts)) total++; } catch { }
                }
                else if (platform == LocalPkgPlatform.Wii)
                {
                    // Wii indexer covers .wad downloadables (WiiWare/VC/channels). Disc games (.iso/.wbfs/.rvz)
                    // are deliberately not in scope here — they're the base library entries, not the clutter
                    // that this indexer is for.
                    try { foreach (var _ in Directory.EnumerateFiles(folder, "*.wad", opts)) total++; } catch { }
                }
                else
                {
                    try { foreach (var _ in Directory.EnumerateFiles(folder, "*.pkg", opts)) total++; } catch { }
                    foreach (var ext in new[] { "*.zip", "*.7z", "*.rar" })
                        try { foreach (var _ in Directory.EnumerateFiles(folder, ext, opts)) total++; } catch { }
                }
            }
            return total;
        }

        public static string Save(LocalPkgManifest manifest, LocalPkgPlatform platform)
        {
            string path = ResolveManifestPath(platform);
            if (string.IsNullOrEmpty(path)) return null;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
            Logger.Log(string.Format("LocalPkgIndexer: wrote {0} ({1} titles, {2:N0} bytes).",
                path, manifest.Titles.Count, json.Length));
            return path;
        }

        /// <summary>Loads the persisted manifest for a platform, or null if missing/corrupt.</summary>
        public static LocalPkgManifest Load(LocalPkgPlatform platform)
        {
            string path = ResolveManifestPath(platform);
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            try
            {
                return JsonSerializer.Deserialize<LocalPkgManifest>(File.ReadAllText(path));
            }
            catch (Exception ex)
            {
                Logger.Log(string.Format("LocalPkgIndexer: failed to load {0}: {1}", path, ex.Message));
                return null;
            }
        }

        public static string ResolveManifestPath(LocalPkgPlatform platform)
        {
            string cacheRoot;
            switch (platform)
            {
                case LocalPkgPlatform.Ps3: cacheRoot = Ps3UpdateFetcher.ResolveCacheRoot(SonyUpdateContext.ForPs3()); break;
                case LocalPkgPlatform.Psp: cacheRoot = Ps3UpdateFetcher.ResolveCacheRoot(SonyUpdateContext.ForPsp()); break;
                case LocalPkgPlatform.Psv: cacheRoot = Ps3UpdateFetcher.ResolveCacheRoot(SonyUpdateContext.ForPsv()); break;
                case LocalPkgPlatform.Wiiu:
                    cacheRoot = Path.Combine(PathUtils.GetPluginRootPath(), "WiiuLocalCache");
                    return Path.Combine(cacheRoot, "local_rom_index.json");
                case LocalPkgPlatform.Ctr3ds:
                    cacheRoot = Path.Combine(PathUtils.GetPluginRootPath(), "Ctr3dsLocalCache");
                    return Path.Combine(cacheRoot, "local_rom_index.json");
                case LocalPkgPlatform.Wii:
                    cacheRoot = Path.Combine(PathUtils.GetPluginRootPath(), "WiiLocalCache");
                    return Path.Combine(cacheRoot, "local_rom_index.json");
                default: return null;
            }
            return string.IsNullOrEmpty(cacheRoot) ? null : Path.Combine(cacheRoot, "local_pkg_index.json");
        }

        public static string[] FoldersForPlatform(LocalPkgPlatform platform)
        {
            string csv;
            switch (platform)
            {
                case LocalPkgPlatform.Ps3:    csv = Config.Ps3LocalPkgFolders;    break;
                case LocalPkgPlatform.Psp:    csv = Config.PspLocalPkgFolders;    break;
                case LocalPkgPlatform.Psv:    csv = Config.PsvLocalPkgFolders;    break;
                case LocalPkgPlatform.Wiiu:   csv = Config.WiiuLocalRomFolders;   break;
                case LocalPkgPlatform.Ctr3ds: csv = Config.Ctr3dsLocalRomFolders; break;
                case LocalPkgPlatform.Wii:    csv = Config.WiiLocalRomFolders;    break;
                default: return Array.Empty<string>();
            }
            var raw = Config.ParsePipeList(csv);
            for (int i = 0; i < raw.Length; i++) raw[i] = PathUtils.GetAbsolutePath(raw[i]);
            return raw;
        }

    }
}
