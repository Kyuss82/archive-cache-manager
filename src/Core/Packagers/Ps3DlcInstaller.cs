/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * On-launch installer for DLC PKGs that live in the user's offline mirror
 * (indexed by LocalPkgIndexer). Sister to Ps3UpdateInstaller: same idea, just
 * targeting DLC content_type entries.
 *
 * For each DLC PKG referenced by the manifest for the given TITLE_ID:
 *   1. Decrypt + unpack into the same install root that received the base game
 *      (PS3) or under the same on-launch cache tree (PSP/PSV). DLCs ship the
 *      additional content under `USRDIR/<add-on dir>/` and overlay cleanly on
 *      top of the base install — RPCS3 / PPSSPP / Vita3K just find the extra
 *      files at runtime.
 *   2. Stage the companion .rap so the NPDRM licence resolves:
 *        • PS3 → copy to RPCS3's `dev_hdd0/home/00000001/exdata/<contentid>.rap`
 *        • PSP → copy to PPSSPP's `PSP/LICENSE/<contentid>.rap`
 *      The .rap is taken from the local sibling file if the indexer found one,
 *      otherwise we synthesise it from the NPS DB's `RAP` hex column.
 *
 * PSV DLC RAP staging is intentionally NOT implemented in this class — Vita3K
 * uses .rif files in `ux0/license/...`, not .rap files. PSV DLC rif handling
 * stays under PsvPkgStaging.TryInstallLicenseFromNps.
 */
using System;
using System.Collections.Generic;
using System.IO;

namespace ArchiveCacheManager
{
    public class Ps3DlcInstallResult
    {
        public string TitleId;
        public int    Installed;
        public int    Skipped;
        public int    Failed;
        public List<string> Messages = new List<string>();
    }

    public static class Ps3DlcInstaller
    {
        /// <summary>
        /// Apply every DLC PKG cached for <paramref name="titleId"/> to <paramref name="targetInstallDir"/>.
        /// `ctx.Platform` decides where the companion .rap goes (PS3 → exdata, PSP → PSP/LICENSE,
        /// PSV → ignored — Vita3K wants .rif, not .rap). No-op when the local manifest has nothing
        /// for the title.
        /// </summary>
        public static Ps3DlcInstallResult Run(string titleId, string targetInstallDir, SonyUpdateContext ctx,
                                              string exdataOrLicensePath, Action<string> progress = null)
        {
            var result = new Ps3DlcInstallResult { TitleId = titleId };
            if (string.IsNullOrWhiteSpace(titleId) || string.IsNullOrWhiteSpace(targetInstallDir))
                return result;

            LocalPkgPlatform platform;
            if      (string.Equals(ctx.Platform, "PSP", StringComparison.OrdinalIgnoreCase)) platform = LocalPkgPlatform.Psp;
            else if (string.Equals(ctx.Platform, "PSV", StringComparison.OrdinalIgnoreCase)) platform = LocalPkgPlatform.Psv;
            else                                                                              platform = LocalPkgPlatform.Ps3;

            var manifest = LocalPkgIndexer.Load(platform);
            if (manifest == null || manifest.Titles == null) return result;
            if (!manifest.Titles.TryGetValue(titleId, out var bucket) || bucket?.Dlcs == null || bucket.Dlcs.Count == 0)
                return result;

            Directory.CreateDirectory(targetInstallDir);

            foreach (var dlc in bucket.Dlcs)
            {
                string resolvedPkgPath; bool needsCleanup;
                try { resolvedPkgPath = LocalManifestResolver.ResolvePkgPath(dlc, out needsCleanup); }
                catch (Exception ex)
                {
                    result.Failed++;
                    result.Messages.Add(string.Format("DLC {0}: extract failed ({1}).", dlc.ContentId, ex.Message));
                    Logger.Log(string.Format("Ps3DlcInstaller: extract failed for {0}: {1}", dlc.ContentId, ex), Logger.LogLevel.Exception);
                    continue;
                }
                if (resolvedPkgPath == null)
                {
                    result.Failed++;
                    result.Messages.Add(string.Format("DLC {0}: PKG missing (path={1}, archive={2}).", dlc.ContentId, dlc.PkgPath, dlc.ArchivePath));
                    continue;
                }

                try
                {
                    progress?.Invoke(string.Format("DLC {0}: installing…", dlc.ContentId));
                    var parsed = PsPkgReader.Parse(resolvedPkgPath);
                    PsPkgUnpacker.Unpack(resolvedPkgPath, parsed, targetInstallDir, pathTransform: ctx.PathTransform);
                    result.Installed++;
                    Logger.Log(string.Format("Ps3DlcInstaller [{0}]: installed DLC {1} → {2}.", ctx.Platform, dlc.ContentId, targetInstallDir));

                    TryStageDlcRap(dlc, ctx, exdataOrLicensePath);
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    result.Messages.Add(string.Format("DLC {0}: install failed ({1}).", dlc.ContentId, ex.Message));
                    Logger.Log(string.Format("Ps3DlcInstaller: install failed for {0}: {1}", dlc.ContentId, ex), Logger.LogLevel.Exception);
                }
                finally
                {
                    if (needsCleanup) LocalManifestResolver.CleanupExtracted(resolvedPkgPath);
                }
            }
            return result;
        }

        private static void TryStageDlcRap(LocalPkgEntry dlc, SonyUpdateContext ctx, string exdataOrLicensePath)
        {
            if (string.IsNullOrWhiteSpace(dlc.ContentId)) return;
            if (string.IsNullOrWhiteSpace(exdataOrLicensePath))
            {
                Logger.Log(string.Format("Ps3DlcInstaller: no exdata/LICENSE folder configured — DLC {0} will be unlicensed.", dlc.ContentId));
                return;
            }
            // PSV uses .rif licenses (Vita3K reads them from a different folder), not .rap.
            // We let PsvPkgStaging.TryInstallLicenseFromNps handle the PSV case via zRIF.
            if (string.Equals(ctx.Platform, "PSV", StringComparison.OrdinalIgnoreCase)) return;

            // 1. sibling .rap from the user's offline mirror (file or zip entry)
            byte[] rapBytes = LocalManifestResolver.ResolveRapBytes(dlc);
            // 2. NPS DB hex column
            if (rapBytes == null)
            {
                var nps = NpsDb.LookupByContentId(dlc.ContentId);
                if (nps != null && !string.IsNullOrWhiteSpace(nps.RapHex))
                {
                    try { rapBytes = HexToBytes(nps.RapHex); }
                    catch (Exception ex) { Logger.Log(string.Format("Ps3DlcInstaller: NPS RAP hex decode failed for {0}: {1}", dlc.ContentId, ex.Message)); }
                }
            }

            if (rapBytes == null || rapBytes.Length != 16)
            {
                Logger.Log(string.Format("Ps3DlcInstaller: no RAP for DLC {0} — RPCS3/PPSSPP may treat it as unlicensed.", dlc.ContentId));
                return;
            }
            try
            {
                Directory.CreateDirectory(exdataOrLicensePath);
                string outPath = Path.Combine(exdataOrLicensePath, dlc.ContentId + ".rap");
                File.WriteAllBytes(outPath, rapBytes);
                Logger.Log(string.Format("Ps3DlcInstaller: RAP for DLC {0} → {1}.", dlc.ContentId, outPath));
            }
            catch (Exception ex)
            {
                Logger.Log(string.Format("Ps3DlcInstaller: failed to write DLC RAP for {0}: {1}", dlc.ContentId, ex.Message));
            }
        }

        private static byte[] HexToBytes(string hex)
        {
            hex = hex.Trim();
            byte[] result = new byte[hex.Length / 2];
            for (int i = 0; i < result.Length; i++)
                result[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            return result;
        }
    }
}
