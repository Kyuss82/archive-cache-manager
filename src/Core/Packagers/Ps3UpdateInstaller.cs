/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * On-launch auto-install of PS3 updates / DLC from Sony's public update server.
 * Composes Ps3UpdateFetcher (HTTPS query + download with SHA1 verify) with the
 * shared PsPkgReader / PsPkgUnpacker pipeline to stage patches and DLC on top of
 * a base game install — works for both the PKG flow (install root in plugin
 * cache) and the ISO flow (install root in RPCS3's dev_hdd0/game/&lt;TITLE_ID&gt;/).
 *
 * Persistent caching: downloaded PKG files are saved under
 *   <Config.Ps3UpdateCachePath>/<TITLE_ID>/<TITLE_ID>-v<version>.pkg
 * and reused across launches when their SHA1 matches Sony's manifest. Network
 * failures are non-fatal — the game launches with whatever updates are already
 * present on disk.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace ArchiveCacheManager
{
    public class Ps3UpdateInstallResult
    {
        public string TitleId;
        public int Queried;
        public int Downloaded;
        public int Reused;
        public int Installed;
        public int Failed;
        public List<string> Messages = new List<string>();
        public bool NetworkError;
    }

    public static class Ps3UpdateInstaller
    {
        /// <summary>
        /// Pull every published patch / DLC PKG for <paramref name="titleId"/> from Sony and
        /// unpack them into <paramref name="targetInstallDir"/>. Idempotent — repeat calls only
        /// re-download or re-unpack what's missing. Pass <paramref name="ctx"/> to switch the
        /// cache root / offline-mode / path-transform (PSP needs USRDIR/CONTENT stripping).
        /// </summary>
        public static Ps3UpdateInstallResult Run(string titleId, string targetInstallDir, Action<string> progress = null, SonyUpdateContext ctx = null)
        {
            return RunAsync(titleId, targetInstallDir, progress, CancellationToken.None, ctx).GetAwaiter().GetResult();
        }

        public static Task<Ps3UpdateInstallResult> RunAsync(string titleId, string targetInstallDir, Action<string> progress, CancellationToken ct)
        {
            return RunAsync(titleId, targetInstallDir, progress, ct, null);
        }

        public static async Task<Ps3UpdateInstallResult> RunAsync(string titleId, string targetInstallDir, Action<string> progress, CancellationToken ct, SonyUpdateContext ctx)
        {
            ctx = ctx ?? SonyUpdateContext.ForPs3();
            var result = new Ps3UpdateInstallResult { TitleId = titleId };
            if (string.IsNullOrWhiteSpace(titleId))
            {
                result.Messages.Add("No TITLE_ID — skipping update fetch.");
                return result;
            }
            if (string.IsNullOrWhiteSpace(targetInstallDir))
            {
                result.Messages.Add("No target install dir — skipping update fetch.");
                return result;
            }

            Directory.CreateDirectory(targetInstallDir);

            // Local-mirror first: if LocalPkgIndexer has any update PKGs cached for this TID,
            // install them straight from disk and skip the Sony query entirely. The user's
            // offline pack always wins over the network — no version check, no SHA1 dance,
            // we trust whatever the user has already curated.
            var localApplied = TryInstallFromLocalManifest(titleId, targetInstallDir, ctx, result, progress, ct);
            if (localApplied > 0)
            {
                result.Messages.Add(string.Format("Installed {0} update(s) from local PKG index.", localApplied));
                return result;
            }

            List<Ps3UpdateInfo> updates;
            try
            {
                progress?.Invoke(string.Format("Querying Sony for {0} updates ({1})…", ctx.Platform, titleId));
                updates = await Ps3UpdateFetcher.QueryAsync(titleId, ct, ctx).ConfigureAwait(false);
                result.Queried = updates.Count;
            }
            catch (Exception ex)
            {
                result.NetworkError = true;
                result.Messages.Add(string.Format("Update query failed: {0}", ex.Message));
                Logger.Log(string.Format("Ps3UpdateInstaller: query failed for {0}: {1}", titleId, ex), Logger.LogLevel.Exception);
                return result;
            }

            if (updates.Count == 0)
            {
                result.Messages.Add("No updates published for this title.");
                return result;
            }

            string cacheRoot = Ps3UpdateFetcher.ResolveCacheRoot(ctx);
            string updateCacheDir = Path.Combine(cacheRoot, titleId);
            Directory.CreateDirectory(updateCacheDir);

            foreach (var info in updates)
            {
                if (ct.IsCancellationRequested) break;
                string pkgFileName = string.Format("{0}-v{1}.pkg", titleId, SafeForFilename(info.Version));
                string pkgPath = Path.Combine(updateCacheDir, pkgFileName);

                bool haveValid = File.Exists(pkgPath) && (string.IsNullOrEmpty(info.Sha1Sum) || FileSha1Matches(pkgPath, info.Sha1Sum));
                if (haveValid)
                {
                    result.Reused++;
                    progress?.Invoke(string.Format("v{0}: cached.", info.Version));
                }
                else
                {
                    try
                    {
                        progress?.Invoke(string.Format("v{0}: downloading…", info.Version));
                        await Ps3UpdateFetcher.DownloadAsync(info, pkgPath, null, ct).ConfigureAwait(false);
                        result.Downloaded++;
                    }
                    catch (Exception ex)
                    {
                        result.Failed++;
                        result.NetworkError = true;
                        result.Messages.Add(string.Format("v{0}: download failed ({1}).", info.Version, ex.Message));
                        Logger.Log(string.Format("Ps3UpdateInstaller: download failed for {0} v{1}: {2}", titleId, info.Version, ex), Logger.LogLevel.Exception);
                        continue;
                    }
                }

                try
                {
                    progress?.Invoke(string.Format("v{0}: installing into {1}…", info.Version, targetInstallDir));
                    var parsed = PsPkgReader.Parse(pkgPath);
                    // For PS3 ctx accept type=0x0001; for PSP/PSV accept type=0x0002 retail.
                    bool typeOk = string.Equals(ctx.Platform, "PSP", StringComparison.OrdinalIgnoreCase)
                               || string.Equals(ctx.Platform, "PSV", StringComparison.OrdinalIgnoreCase)
                        ? parsed.Header.IsRetailPspOrPsv
                        : parsed.Header.IsRetailPs3;
                    if (!typeOk)
                    {
                        result.Failed++;
                        result.Messages.Add(string.Format("v{0}: cached PKG is not a {1} retail package, skipped.", info.Version, ctx.Platform));
                        continue;
                    }
                    PsPkgUnpacker.Unpack(pkgPath, parsed, targetInstallDir, pathTransform: ctx.PathTransform);
                    result.Installed++;
                    Logger.Log(string.Format("Ps3UpdateInstaller [{0}]: installed {1} v{2} into {3}.", ctx.Platform, titleId, info.Version, targetInstallDir));
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    result.Messages.Add(string.Format("v{0}: install failed ({1}).", info.Version, ex.Message));
                    Logger.Log(string.Format("Ps3UpdateInstaller: install failed for {0} v{1}: {2}", titleId, info.Version, ex), Logger.LogLevel.Exception);
                }
            }

            return result;
        }

        /// <summary>
        /// Apply update PKGs listed for <paramref name="titleId"/> in the platform's
        /// LocalPkgIndexer manifest (built from `Ps3LocalPkgFolders` / `Psp...` / `Psv...`).
        /// Returns the count actually installed. 0 means "no local entry — fall through to Sony".
        /// </summary>
        private static int TryInstallFromLocalManifest(string titleId, string targetInstallDir,
            SonyUpdateContext ctx, Ps3UpdateInstallResult result,
            Action<string> progress, System.Threading.CancellationToken ct)
        {
            LocalPkgPlatform platform;
            if      (string.Equals(ctx.Platform, "PSP", StringComparison.OrdinalIgnoreCase)) platform = LocalPkgPlatform.Psp;
            else if (string.Equals(ctx.Platform, "PSV", StringComparison.OrdinalIgnoreCase)) platform = LocalPkgPlatform.Psv;
            else                                                                              platform = LocalPkgPlatform.Ps3;

            var manifest = LocalPkgIndexer.Load(platform);
            if (manifest == null || manifest.Titles == null) return 0;
            if (!manifest.Titles.TryGetValue(titleId, out var bucket) || bucket?.Updates == null || bucket.Updates.Count == 0)
                return 0;

            int applied = 0;
            foreach (var u in bucket.Updates)
            {
                if (ct.IsCancellationRequested) break;

                string resolvedPkgPath; bool needsCleanup;
                try { resolvedPkgPath = LocalManifestResolver.ResolvePkgPath(u, out needsCleanup); }
                catch (Exception ex)
                {
                    result.Failed++;
                    result.Messages.Add(string.Format("local update {0}: extract failed ({1}).", u.ContentId, ex.Message));
                    Logger.Log(string.Format("Ps3UpdateInstaller: local extract failed for {0}: {1}", u.ContentId, ex), Logger.LogLevel.Exception);
                    continue;
                }
                if (resolvedPkgPath == null)
                {
                    result.Failed++;
                    result.Messages.Add(string.Format("local update {0}: PKG missing (path={1}, archive={2}).", u.ContentId, u.PkgPath, u.ArchivePath));
                    continue;
                }

                try
                {
                    progress?.Invoke(string.Format("local update {0}: installing…", u.ContentId));
                    var parsed = PsPkgReader.Parse(resolvedPkgPath);
                    PsPkgUnpacker.Unpack(resolvedPkgPath, parsed, targetInstallDir, pathTransform: ctx.PathTransform);
                    applied++;
                    result.Installed++;
                    Logger.Log(string.Format("Ps3UpdateInstaller [{0}]: installed local update {1} → {2}.", ctx.Platform, u.ContentId, targetInstallDir));
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    result.Messages.Add(string.Format("local update {0}: install failed ({1}).", u.ContentId, ex.Message));
                    Logger.Log(string.Format("Ps3UpdateInstaller: local install failed for {0}: {1}", u.ContentId, ex), Logger.LogLevel.Exception);
                }
                finally
                {
                    if (needsCleanup) LocalManifestResolver.CleanupExtracted(resolvedPkgPath);
                }
            }
            result.Reused += applied;   // surfaced as "reused" since we didn't download anything
            return applied;
        }

        /// <summary>
        /// PS3-disc-ISO entry point: read PARAM.SFO out of the decrypted ISO at
        /// <paramref name="decryptedIsoPath"/>, derive the TITLE_ID, and install every published
        /// patch / DLC PKG into RPCS3's dev_hdd0/game/&lt;TITLE_ID&gt;/. RPCS3 must be portable for
        /// the install path to be derivable from <paramref name="emulatorPath"/>.
        /// </summary>
        public static void TryInstallForDecryptedIso(string decryptedIsoPath, string emulatorPath)
        {
            if (string.IsNullOrWhiteSpace(decryptedIsoPath) || !File.Exists(decryptedIsoPath)) return;
            if (string.IsNullOrWhiteSpace(emulatorPath)) return;

            string titleId = ExtractTitleIdFromIso(decryptedIsoPath);
            if (string.IsNullOrWhiteSpace(titleId))
            {
                Logger.Log(string.Format("Ps3UpdateInstaller: could not read TITLE_ID from {0}; skipping auto-update.", decryptedIsoPath));
                return;
            }

            string emuDir = Path.GetDirectoryName(emulatorPath);
            if (string.IsNullOrWhiteSpace(emuDir))
            {
                Logger.Log("Ps3UpdateInstaller: empty emulator dir; skipping auto-update.");
                return;
            }
            string installDir = Path.Combine(emuDir, "dev_hdd0", "game", titleId);

            try
            {
                var result = Run(titleId, installDir, msg => Logger.Log("Ps3UpdateInstaller: " + msg));
                Logger.Log(string.Format(
                    "Ps3UpdateInstaller: ISO-flow auto-update for {0} → {1} — {2} queried, {3} downloaded, {4} reused, {5} installed, {6} failed.",
                    titleId, installDir, result.Queried, result.Downloaded, result.Reused, result.Installed, result.Failed));
            }
            catch (Exception ex)
            {
                Logger.Log(string.Format("Ps3UpdateInstaller: ISO-flow auto-update threw for {0}: {1}", titleId, ex), Logger.LogLevel.Exception);
            }
        }

        private static string ExtractTitleIdFromIso(string isoPath)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "ACM_Ps3IsoSfo_" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(tempDir);
                var zip = new Zip();
                // 7-Zip handles PS3 ISO (UDF + ISO9660) natively; ask for PS3_GAME/PARAM.SFO only.
                if (!zip.Extract(isoPath, tempDir, new[] { "PS3_GAME/PARAM.SFO" }))
                {
                    return null;
                }
                string sfoPath = Directory.GetFiles(tempDir, "PARAM.SFO", SearchOption.AllDirectories).FirstOrDefault();
                return sfoPath != null ? Sfo.ReadTitleId(sfoPath) : null;
            }
            catch (Exception ex)
            {
                Logger.Log(string.Format("Ps3UpdateInstaller: ISO PARAM.SFO read failed for {0}: {1}", isoPath, ex.Message));
                return null;
            }
            finally
            {
                try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); } catch { }
            }
        }

        private static bool FileSha1Matches(string path, string expectedHexLower)
        {
            try
            {
                using (var fs = File.OpenRead(path))
                using (var sha = SHA1.Create())
                {
                    byte[] hash = sha.ComputeHash(fs);
                    string got = BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
                    return string.Equals(got, expectedHexLower, StringComparison.OrdinalIgnoreCase);
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static string SafeForFilename(string s)
        {
            if (string.IsNullOrEmpty(s)) return "x";
            foreach (char c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return s;
        }
    }
}
