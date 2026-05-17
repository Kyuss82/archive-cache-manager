/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Wii U on-launch auto-installer. Reads `<plugin>/WiiuLocalCache/local_rom_index.json`
 * (built by `LocalPkgIndexer` / `LocalPkgScanner`) and copies matching update + DLC
 * entries into Cemu's `mlc01/usr/title/<high>/<low>/` so the next launch of `cemu.exe`
 * sees them. PS3/PSP/PSV equivalent is `Ps3UpdateInstaller`/`Ps3DlcInstaller`.
 *
 * Source-format support today:
 *   • Loadiine layout — folder containing `title.tmd` + `code/`, `content/`, `meta/`
 *     subdirs. The parent folder is what Cemu expects under `mlc01/usr/title/<TID>/`,
 *     so we just robocopy that tree across. This is the native Cemu format and the
 *     common case for users who ran `cdecrypt` once over their NUS dumps.
 *
 * Not yet supported (logged and skipped):
 *   • `.wua` archives — would need `zarchive.exe -x` to unpack into Loadiine layout
 *     before the copy. v2.76 candidate.
 *   • NUS dumps — `tmd.NN` + `cetk` + `NNNNNNNN.app` requires `cdecrypt.exe` to
 *     produce Loadiine layout. Out of scope for auto-install (run cdecrypt yourself
 *     and re-index, the indexer picks up the Loadiine result).
 *   • `.zip` wrappers — would need ad-hoc unzip + classify-inner-layout + install.
 *
 * Title-ID discovery for the launching game:
 *   1. If `game.ApplicationPath` matches `<TID16>_v<ver>.wua` → take TID from filename.
 *   2. Otherwise walk the archive's parent folder for a TMD file (`title.tmd` /
 *      `tmd.NN` / bare `tmd`) and parse with `WiiuTmd.Parse`.
 *   3. If we still can't get a TID, log and skip.
 *
 * Cemu MLC resolution:
 *   • If `<emulator>/mlc01/` exists, that's the target (Cemu portable, default).
 *   • Otherwise log a warning; user must set up a portable Cemu, or move their mlc01
 *     next to `cemu.exe`. (A `WiiuCemuMlcPath` config override is a future addition
 *     if anyone reports needing it.)
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace ArchiveCacheManager
{
    public class WiiuAutoInstallResult
    {
        public bool   Ran;
        public string TitleId;            // 16-hex string of the base game's TID, if resolved
        public string MlcRoot;            // resolved Cemu mlc01 absolute path
        public int    UpdatesInstalled;
        public int    DlcsInstalled;
        public int    Skipped;
        public int    Failed;
        public List<string> Messages = new List<string>();
    }

    public static class WiiuUpdateInstaller
    {
        private static readonly Regex WuaFilenameRegex =
            new Regex(@"^(?<tid>[0-9a-fA-F]{16})(?:_v\d+)?\.wua$", RegexOptions.Compiled);

        /// <summary>
        /// Entry point — call from `OnBeforeGameLaunching` for Wii U games. No-ops cleanly if the
        /// manifest is missing, the title ID can't be resolved, or Cemu's mlc01 isn't findable.
        /// </summary>
        public static WiiuAutoInstallResult RunAtLaunch(string gameArchivePath, string emulatorPath, bool wantUpdates, bool wantDlcs)
        {
            var result = new WiiuAutoInstallResult();
            if (!wantUpdates && !wantDlcs) return result;

            string mlcRoot = ResolveMlcRoot(emulatorPath);
            if (mlcRoot == null)
            {
                Logger.Log("WiiuUpdateInstaller: cannot locate Cemu mlc01/ next to emulator; skipping auto-install.");
                result.Messages.Add("Cemu mlc01/ not found next to the emulator executable.");
                return result;
            }
            result.MlcRoot = mlcRoot;

            ulong? tid = ResolveGameTitleId(gameArchivePath);
            if (tid == null)
            {
                Logger.Log(string.Format("WiiuUpdateInstaller: cannot resolve title_id from {0}; skipping auto-install.", gameArchivePath));
                result.Messages.Add("Title ID could not be resolved from the game archive.");
                return result;
            }
            string titleIdHex = tid.Value.ToString("X16");
            result.TitleId = titleIdHex;
            // The manifest keys updates/DLCs under the BASE title id (00050000XXXXXXXX). Mask the
            // category bits in case the resolved TID happens to be already an update (0005000E)
            // by mistake.
            uint low = (uint)(tid.Value & 0xFFFFFFFF);
            string baseTid = "00050000" + low.ToString("X8");

            var manifest = LocalPkgIndexer.Load(LocalPkgPlatform.Wiiu);
            if (manifest == null || manifest.Titles == null)
            {
                Logger.Log("WiiuUpdateInstaller: no Wii U mirror manifest loaded (run the indexer first).");
                return result;
            }
            if (!manifest.Titles.TryGetValue(baseTid, out var bucket) || bucket == null)
            {
                Logger.Log(string.Format("WiiuUpdateInstaller: no manifest entry for base TID {0}; nothing to install.", baseTid));
                return result;
            }

            result.Ran = true;
            Logger.Log(string.Format("WiiuUpdateInstaller: game TID={0}, base TID={1}, mlc01={2}", titleIdHex, baseTid, mlcRoot));

            if (wantUpdates && bucket.Updates != null)
            {
                foreach (var u in bucket.Updates) InstallEntry(u, mlcRoot, result, isUpdate: true);
            }
            if (wantDlcs && bucket.Dlcs != null)
            {
                foreach (var d in bucket.Dlcs) InstallEntry(d, mlcRoot, result, isUpdate: false);
            }

            Logger.Log(string.Format(
                "WiiuUpdateInstaller: done — {0} updates, {1} DLCs installed; {2} skipped, {3} failed.",
                result.UpdatesInstalled, result.DlcsInstalled, result.Skipped, result.Failed));
            return result;
        }

        private static void InstallEntry(LocalPkgEntry entry, string mlcRoot, WiiuAutoInstallResult result, bool isUpdate)
        {
            // entry.ContentType holds the title_id_high (0x0005000E for update, 0x0005000C for DLC).
            // entry.TitleId is the 16-hex full TID; the lower 8 hex chars are the low TID under the
            // category directory in mlc01.
            if (string.IsNullOrEmpty(entry?.TitleId) || entry.TitleId.Length != 16)
            {
                result.Skipped++;
                result.Messages.Add(string.Format("[skip] {0}: TitleId missing or malformed in manifest.", entry?.PkgPath ?? "<null>"));
                return;
            }
            string highHex = entry.TitleId.Substring(0, 8);
            string lowHex  = entry.TitleId.Substring(8, 8);
            string target  = Path.Combine(mlcRoot, "usr", "title", highHex, lowHex);

            // Resolve the source — either a Loadiine folder on disk we can copy as-is, or a .wua
            // archive that we need to extract first into a temp dir. `tempDirToCleanup` is set
            // when extraction happened so we can rm it after the copy.
            string sourceDir = ResolveLoadiineSource(entry, out string tempDirToCleanup);
            if (sourceDir == null)
            {
                result.Skipped++;
                Logger.Log(string.Format("WiiuUpdateInstaller: [skip] {0} — not a supported source (zip wrapper / NUS dump / .wua extraction failed).", entry.PkgPath));
                result.Messages.Add(string.Format("[skip] {0}: source format not supported by auto-install today.", entry.PkgPath));
                return;
            }

            try
            {
                Directory.CreateDirectory(target);
                int rc = RobocopyMir(sourceDir, target);
                // robocopy: 0..7 are success-ish exit codes (0 = no change, 1 = files copied, etc.).
                // 8+ are failures.
                if (rc < 8)
                {
                    if (isUpdate) result.UpdatesInstalled++; else result.DlcsInstalled++;
                    Logger.Log(string.Format("WiiuUpdateInstaller: [idx] {0} → {1} ({2}). robocopy rc={3}.",
                        sourceDir, target, isUpdate ? "UPDATE" : "DLC", rc));
                }
                else
                {
                    result.Failed++;
                    Logger.Log(string.Format("WiiuUpdateInstaller: [error] robocopy rc={0} for {1} → {2}.", rc, sourceDir, target),
                        Logger.LogLevel.Exception);
                    result.Messages.Add(string.Format("[error] robocopy {0} rc={1}.", sourceDir, rc));
                }
            }
            catch (Exception ex)
            {
                result.Failed++;
                Logger.Log(string.Format("WiiuUpdateInstaller: install failed for {0}: {1}", entry.PkgPath, ex), Logger.LogLevel.Exception);
                result.Messages.Add(string.Format("[error] {0}: {1}", entry.PkgPath, ex.Message));
            }
            finally
            {
                if (!string.IsNullOrEmpty(tempDirToCleanup) && Directory.Exists(tempDirToCleanup))
                {
                    try { Directory.Delete(tempDirToCleanup, true); }
                    catch (Exception ex)
                    {
                        Logger.Log(string.Format("WiiuUpdateInstaller: failed to clean up {0}: {1}", tempDirToCleanup, ex.Message));
                    }
                }
            }
        }

        /// <summary>
        /// Resolves a manifest entry into a directory tree this method can robocopy. Two paths:
        ///   1. PkgPath points at a TMD inside a Loadiine folder → return that folder, no cleanup.
        ///   2. PkgPath is a `.wua` file and `zarchive.exe` is in Extractors/ → extract to a
        ///      temp dir, return it, caller cleans up.
        /// Returns (null, null) for unsupported sources (zip wrappers, NUS dumps without
        /// cdecrypt pre-pass, .wua without zarchive available).
        /// </summary>
        private static string ResolveLoadiineSource(LocalPkgEntry entry, out string tempDirToCleanup)
        {
            tempDirToCleanup = null;
            if (string.IsNullOrEmpty(entry?.PkgPath)) return null;
            if (entry.PkgPath.IndexOf('!') >= 0) return null;   // zip-wrapper entry
            if (!File.Exists(entry.PkgPath)) return null;

            // .wua → extract via zarchive into a temp dir, return that dir for the robocopy step.
            if (entry.PkgPath.EndsWith(".wua", StringComparison.OrdinalIgnoreCase) ||
                entry.PkgPath.EndsWith(".zar", StringComparison.OrdinalIgnoreCase))
            {
                if (!ZArchiveInvoker.IsAvailable())
                {
                    Logger.Log(string.Format("WiiuUpdateInstaller: [skip] {0} — zarchive.exe missing in Extractors/, can't unpack .wua.", entry.PkgPath));
                    return null;
                }
                string tmp = Path.Combine(Path.GetTempPath(), "ACM_WiiuWuaUnpack_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tmp);
                var (ok, _, _, rc) = ZArchiveInvoker.Extract(entry.PkgPath, tmp);
                if (!ok)
                {
                    Logger.Log(string.Format("WiiuUpdateInstaller: [error] zarchive extract failed (rc={0}) for {1}.", rc, entry.PkgPath),
                        Logger.LogLevel.Exception);
                    try { Directory.Delete(tmp, true); } catch { }
                    return null;
                }
                tempDirToCleanup = tmp;
                // zarchive extracts the Loadiine root straight into the output dir, so it IS the
                // copy source. If the archive contained an extra wrapping dir (some packs have
                // `<TID16>/code/...`), step into the single child dir.
                string sourceDir = MaybeUnwrapSingleChildDir(tmp);
                return sourceDir;
            }

            // Direct Loadiine folder (TMD on disk + code/content/meta siblings).
            string dir = Path.GetDirectoryName(entry.PkgPath);
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return null;

            bool hasTitleTmd = string.Equals(Path.GetFileName(entry.PkgPath), "title.tmd", StringComparison.OrdinalIgnoreCase);
            bool hasLoadiineDirs =
                Directory.Exists(Path.Combine(dir, "code")) ||
                Directory.Exists(Path.Combine(dir, "content")) ||
                Directory.Exists(Path.Combine(dir, "meta"));
            if (!hasTitleTmd && !hasLoadiineDirs)
            {
                // NUS dump (tmd.NN + numbered .app) — needs cdecrypt, out of scope.
                return null;
            }
            return dir;
        }

        /// <summary>
        /// When zarchive extracts to a folder that contains exactly one subfolder (some `.wua`
        /// packs wrap the Loadiine layout under a `<TitleID16>/` parent), peel that one layer so
        /// the robocopy source is the Loadiine root rather than its parent. Otherwise return the
        /// dir unchanged.
        /// </summary>
        private static string MaybeUnwrapSingleChildDir(string dir)
        {
            var entries = Directory.GetFileSystemEntries(dir);
            if (entries.Length == 1 && Directory.Exists(entries[0]))
            {
                string only = entries[0];
                if (Directory.Exists(Path.Combine(only, "code")) ||
                    Directory.Exists(Path.Combine(only, "content")) ||
                    Directory.Exists(Path.Combine(only, "meta")))
                {
                    return only;
                }
            }
            return dir;
        }

        /// <summary>
        /// Best-effort title_id derivation from the launching game's archive:
        ///   1. Filename `<TID16>_v<ver>.wua` → parse TID directly.
        ///   2. Sibling `title.tmd` / `tmd.NN` / bare `tmd` → WiiuTmd.Parse.
        ///   3. Inside a folder that IS a Loadiine layout (`code/title.tmd`) → parse that.
        /// Returns null if none of the above resolve.
        /// </summary>
        private static ulong? ResolveGameTitleId(string gameArchivePath)
        {
            if (string.IsNullOrWhiteSpace(gameArchivePath)) return null;
            try
            {
                string name = Path.GetFileName(gameArchivePath);
                var m = WuaFilenameRegex.Match(name);
                if (m.Success && ulong.TryParse(m.Groups["tid"].Value, System.Globalization.NumberStyles.HexNumber,
                    System.Globalization.CultureInfo.InvariantCulture, out ulong tidFromName))
                    return tidFromName;

                if (File.Exists(gameArchivePath))
                {
                    string dir = Path.GetDirectoryName(gameArchivePath);
                    if (!string.IsNullOrEmpty(dir))
                    {
                        string tmd = FindTmdIn(dir);
                        if (tmd != null) return WiiuTmd.Parse(tmd).TitleId;
                    }
                }
                else if (Directory.Exists(gameArchivePath))
                {
                    string tmd = FindTmdIn(gameArchivePath) ?? FindTmdIn(Path.Combine(gameArchivePath, "code"));
                    if (tmd != null) return WiiuTmd.Parse(tmd).TitleId;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(string.Format("WiiuUpdateInstaller: title_id resolve failed for {0}: {1}", gameArchivePath, ex.Message));
            }
            return null;
        }

        private static string FindTmdIn(string dir)
        {
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return null;
            foreach (string p in Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly))
            {
                string name = Path.GetFileName(p).ToLowerInvariant();
                if (name == "title.tmd" || name == "tmd" || name.StartsWith("tmd.")) return p;
                if (name.EndsWith(".tmd")) return p;
            }
            return null;
        }

        /// <summary>
        /// Resolves Cemu's mlc01 root. Standard portable layout puts it next to cemu.exe.
        /// </summary>
        private static string ResolveMlcRoot(string emulatorPath)
        {
            if (string.IsNullOrWhiteSpace(emulatorPath)) return null;
            string abs = PathUtils.GetAbsolutePath(emulatorPath);
            string exeDir = Path.GetDirectoryName(abs);
            if (string.IsNullOrEmpty(exeDir)) return null;
            string candidate = Path.Combine(exeDir, "mlc01");
            return Directory.Exists(candidate) ? candidate : null;
        }

        /// <summary>
        /// Mirror-copy `source` → `target` via robocopy. /MIR mirrors structure, /NFL /NDL /NJH /NJS
        /// keep the log quiet, /R:1 /W:1 keep retries low. Returns the robocopy exit code.
        /// </summary>
        private static int RobocopyMir(string source, string target)
        {
            var p = new System.Diagnostics.Process();
            p.StartInfo.FileName  = "robocopy";
            p.StartInfo.Arguments = string.Format("\"{0}\" \"{1}\" /MIR /NFL /NDL /NJH /NJS /R:1 /W:1", source, target);
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow  = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError  = true;
            p.Start();
            p.WaitForExit();
            return p.ExitCode;
        }
    }
}
