/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * PS3 software-update fetcher. Queries Sony's PSN update server for a given
 * TITLE_ID and returns the publicly-listed patch/update PKG files, then can
 * download them with SHA1 verification.
 *
 * Endpoint and XML schema mirror rusty-psn (RainbowCookie32/rusty-psn, GPL-3.0):
 *   https://a0.ww.np.dl.playstation.net/tpl/np/{TITLE_ID}/{TITLE_ID}-ver.xml
 *   <titlepatch status="OK">
 *     <tag name="..." popup="false">
 *       <package version="01.01" size="69123456" sha1sum="aabbcc…" url="https://…/EP….pkg" />
 *       <package version="01.02" ... />
 *     </tag>
 *   </titlepatch>
 *
 * Note on TLS: the .np.dl.playstation.net cert chain is sometimes flagged by
 * .NET's default validator (older intermediates). We accept the cert without
 * chain validation, but only when the request target host ends with
 * .playstation.net — this avoids the "danger_accept_invalid_certs" anti-pattern
 * leaking into other code paths.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ArchiveCacheManager
{
    public class Ps3UpdateInfo
    {
        public string Version;
        public ulong Size;
        public string Sha1Sum;
        public string Url;
        public string SystemVer;
    }

    public static class Ps3UpdateFetcher
    {
        // PS3 + PSP share the unsigned endpoint. PSV requires a per-title HMAC-SHA256 of
        // the literal "np_<TITLE_ID>" using a public 32-byte key — same algorithm rusty-psn /
        // pkg2zip use.
        private const string Ps3PspUrlTemplate = "https://a0.ww.np.dl.playstation.net/tpl/np/{0}/{0}-ver.xml";
        private const string PsvUrlTemplate   = "https://gs-sec.ww.np.dl.playstation.net/pl/np/{0}/{1}/{0}-ver.xml";
        private static readonly byte[] PsvHmacKey =
        {
            0xE5, 0xE2, 0x78, 0xAA, 0x1E, 0xE3, 0x40, 0x82, 0xA0, 0x88, 0x27, 0x9C, 0x83, 0xF9, 0xBB, 0xC8,
            0x06, 0x82, 0x1C, 0x52, 0xF2, 0xAB, 0x5D, 0x2B, 0x4A, 0xBD, 0x99, 0x54, 0x50, 0x35, 0x51, 0x14,
        };

        private static readonly Lazy<HttpClient> mClient = new Lazy<HttpClient>(BuildClient);

        private static HttpClient BuildClient()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (req, cert, chain, errors) =>
                {
                    string host = req?.RequestUri?.Host ?? string.Empty;
                    return host.EndsWith(".playstation.net", StringComparison.OrdinalIgnoreCase)
                        || host.Equals("playstation.net", StringComparison.OrdinalIgnoreCase);
                }
            };
            var client = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(2) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (PLAYSTATION 3)");
            return client;
        }

        /// <summary>
        /// Query Sony's update server for the supplied title ID (e.g. "BLES01047", "NPEB00321").
        /// Returns an empty list if the server has no patches for this title.
        /// </summary>
        public static List<Ps3UpdateInfo> Query(string titleId)
        {
            return QueryAsync(titleId, CancellationToken.None).GetAwaiter().GetResult();
        }

        public static Task<List<Ps3UpdateInfo>> QueryAsync(string titleId, CancellationToken ct)
        {
            return QueryAsync(titleId, ct, null);
        }

        /// <summary>
        /// Query overload with a platform context (PS3 / PSP). Pass null to keep the PS3
        /// defaults (Config.Ps3UpdateCachePath + Ps3UpdateOfflineMode). PSP callers should
        /// pass SonyUpdateContext.ForPsp() so the cache root + offline flag are read from
        /// the PSP-specific Config fields.
        /// </summary>
        public static async Task<List<Ps3UpdateInfo>> QueryAsync(string titleId, CancellationToken ct, SonyUpdateContext ctx)
        {
            if (string.IsNullOrWhiteSpace(titleId))
                throw new ArgumentException("titleId is required.", nameof(titleId));

            ctx = ctx ?? SonyUpdateContext.ForPs3();
            string normalised = titleId.Trim().ToUpperInvariant();

            // Cache-first: parse the local copy of Sony's titlepatch XML if we already have one.
            // In offline mode, this is the only allowed source — Sony's server is never touched.
            string cachedManifest = ResolveCachedManifestPath(normalised, ctx);
            if (!string.IsNullOrEmpty(cachedManifest) && File.Exists(cachedManifest))
            {
                try
                {
                    string cachedXml = File.ReadAllText(cachedManifest);
                    if (!string.IsNullOrWhiteSpace(cachedXml))
                    {
                        Logger.Log(string.Format("Ps3UpdateFetcher [{0}]: parsed cached manifest for {1} ({2}).", ctx.Platform, normalised, cachedManifest));
                        return ParseUpdateXml(cachedXml);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(string.Format("Ps3UpdateFetcher [{0}]: cached manifest read failed for {1}: {2} — falling back to network.", ctx.Platform, normalised, ex.Message));
                }
            }
            if (ctx.OfflineMode)
            {
                var fromNps = TryNpsFallback(normalised, ctx, "offline mode, no cached manifest");
                if (fromNps.Count > 0) return fromNps;
                Logger.Log(string.Format("Ps3UpdateFetcher [{0}]: offline mode and no cached manifest for {1} — returning empty list.", ctx.Platform, normalised));
                return new List<Ps3UpdateInfo>();
            }

            string url = BuildUrl(normalised, ctx);
            try
            {
                using (var response = await mClient.Value.GetAsync(url, ct).ConfigureAwait(false))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        var nps404 = TryNpsFallback(normalised, ctx, "Sony returned 404");
                        if (nps404.Count > 0) return nps404;
                        return new List<Ps3UpdateInfo>();
                    }
                    response.EnsureSuccessStatusCode();
                    string xml = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    // Sony returns an empty body for some titles instead of 404.
                    if (string.IsNullOrWhiteSpace(xml))
                    {
                        var npsEmpty = TryNpsFallback(normalised, ctx, "Sony returned empty body");
                        if (npsEmpty.Count > 0) return npsEmpty;
                        return new List<Ps3UpdateInfo>();
                    }

                    TryWriteCachedManifest(normalised, xml, ctx);
                    var parsed = ParseUpdateXml(xml);
                    if (parsed.Count == 0)
                    {
                        var npsAug = TryNpsFallback(normalised, ctx, "Sony manifest had no <package> rows");
                        if (npsAug.Count > 0) return npsAug;
                    }
                    return parsed;
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                var npsErr = TryNpsFallback(normalised, ctx, "Sony query threw: " + ex.GetType().Name);
                if (npsErr.Count > 0) return npsErr;
                throw;
            }
        }

        /// <summary>
        /// Convert NoPayStation update rows for the title (if any) into Ps3UpdateInfo. Sony's
        /// titlepatch XML is preferred — this is the fallback for offline / Sony-not-listing
        /// scenarios. NPS publishes SHA256 (not SHA1) so the resulting infos have empty Sha1Sum
        /// and Ps3UpdateFetcher.Download will skip post-download hash verification for them.
        /// </summary>
        private static List<Ps3UpdateInfo> TryNpsFallback(string titleId, SonyUpdateContext ctx, string reason)
        {
            var rows = NpsDb.LookupUpdatesByTitleId(titleId);
            if (rows == null || rows.Count == 0) return new List<Ps3UpdateInfo>();

            var list = new List<Ps3UpdateInfo>(rows.Count);
            foreach (var u in rows)
            {
                if (string.IsNullOrWhiteSpace(u.PkgUrl)) continue;
                list.Add(new Ps3UpdateInfo
                {
                    Version   = string.IsNullOrEmpty(u.UpdateVersion) ? "?" : u.UpdateVersion,
                    Sha1Sum   = string.Empty,
                    Size      = u.SizeBytes > 0 ? (ulong)u.SizeBytes : 0UL,
                    Url       = u.PkgUrl,
                    SystemVer = u.RequiredFw,
                });
            }
            // NPS rows are pre-sorted by version when the TSV is — preserve as-is, miniz / Sony
            // apply oldest → newest in the installer anyway.
            Logger.Log(string.Format("Ps3UpdateFetcher [{0}]: NPS fallback ({1}) returned {2} update(s) for {3}.",
                ctx.Platform, reason, list.Count, titleId));
            return list;
        }

        private static string BuildUrl(string titleId, SonyUpdateContext ctx)
        {
            if (string.Equals(ctx.Platform, "PSV", StringComparison.OrdinalIgnoreCase))
            {
                using (var hmac = new System.Security.Cryptography.HMACSHA256(PsvHmacKey))
                {
                    byte[] msg = System.Text.Encoding.ASCII.GetBytes("np_" + titleId);
                    byte[] hash = hmac.ComputeHash(msg);
                    string hex = BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
                    return string.Format(PsvUrlTemplate, titleId, hex);
                }
            }
            return string.Format(Ps3PspUrlTemplate, titleId);
        }

        /// <summary>Absolute path where the configured cache root stores the cached titlepatch XML for a title.</summary>
        public static string ResolveCachedManifestPath(string titleId, SonyUpdateContext ctx = null)
        {
            ctx = ctx ?? SonyUpdateContext.ForPs3();
            string root = ResolveCacheRoot(ctx);
            if (string.IsNullOrEmpty(root)) return null;
            return Path.Combine(root, titleId, titleId + "-ver.xml");
        }

        public static string ResolveCacheRoot(SonyUpdateContext ctx = null)
        {
            ctx = ctx ?? SonyUpdateContext.ForPs3();
            string configured = ctx.CacheRoot;
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return PathUtils.GetAbsolutePath(configured);
            }
            string defaultFolder;
            if (string.Equals(ctx.Platform, "PSP", StringComparison.OrdinalIgnoreCase)) defaultFolder = "PspUpdateCache";
            else if (string.Equals(ctx.Platform, "PSV", StringComparison.OrdinalIgnoreCase)) defaultFolder = "PsvUpdateCache";
            else defaultFolder = "Ps3UpdateCache";
            try { return Path.Combine(PathUtils.GetPluginRootPath(), defaultFolder); }
            catch { return null; }
        }

        private static void TryWriteCachedManifest(string titleId, string xml, SonyUpdateContext ctx)
        {
            try
            {
                string path = ResolveCachedManifestPath(titleId, ctx);
                if (string.IsNullOrEmpty(path)) return;
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, xml);
            }
            catch (Exception ex)
            {
                Logger.Log(string.Format("Ps3UpdateFetcher: failed to cache manifest for {0}: {1}", titleId, ex.Message));
            }
        }

        public static List<Ps3UpdateInfo> ParseUpdateXml(string xml)
        {
            var list = new List<Ps3UpdateInfo>();
            XDocument doc;
            try
            {
                doc = XDocument.Parse(xml);
            }
            catch (Exception ex)
            {
                Logger.Log(string.Format("Ps3UpdateFetcher: XML parse failed: {0}", ex.Message), Logger.LogLevel.Exception);
                return list;
            }

            // Sony's `status` attribute observed values: "alive" (title has active patches),
            // "deleted" (title removed from PSN), and historically "OK". Treat anything other
            // than an explicit "deleted" as parseable — the <package> list is the source of truth.
            string status = doc.Root?.Attribute("status")?.Value ?? string.Empty;
            if (string.Equals(status, "deleted", StringComparison.OrdinalIgnoreCase))
            {
                Logger.Log(string.Format("Ps3UpdateFetcher: titlepatch status=\"deleted\" — title retired from PSN."));
                return list;
            }

            foreach (var pkg in doc.Descendants("package"))
            {
                string url = (string)pkg.Attribute("url");
                if (string.IsNullOrWhiteSpace(url)) continue;

                var info = new Ps3UpdateInfo
                {
                    Version    = (string)pkg.Attribute("version") ?? "?",
                    Sha1Sum    = ((string)pkg.Attribute("sha1sum") ?? string.Empty).ToLowerInvariant(),
                    Url        = url,
                    SystemVer  = (string)pkg.Attribute("ps3_system_ver") ?? (string)pkg.Attribute("system_ver"),
                };
                if (ulong.TryParse((string)pkg.Attribute("size"), out ulong size))
                {
                    info.Size = size;
                }
                list.Add(info);
            }

            // Sony lists oldest → newest within a tag; preserve that order.
            Logger.Log(string.Format("Ps3UpdateFetcher: parsed {0} update(s) from titlepatch status=\"{1}\".", list.Count, status));
            return list;
        }

        /// <summary>
        /// Download a single update PKG to <paramref name="outputPath"/>, streaming with progress
        /// reporting in bytes. After the download completes the file's SHA1 is verified against
        /// the manifest's sha1sum; on mismatch the partial file is deleted and the method throws.
        /// </summary>
        public static void Download(Ps3UpdateInfo info, string outputPath, IProgress<long> progress = null)
        {
            DownloadAsync(info, outputPath, progress, CancellationToken.None).GetAwaiter().GetResult();
        }

        public static async Task DownloadAsync(Ps3UpdateInfo info, string outputPath, IProgress<long> progress, CancellationToken ct)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            if (string.IsNullOrWhiteSpace(info.Url)) throw new ArgumentException("info.Url is empty.", nameof(info));
            if (string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentException("outputPath is required.", nameof(outputPath));

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            using (var response = await mClient.Value.GetAsync(info.Url, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                using (var src = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (var dst = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, FileOptions.SequentialScan))
                using (var sha = SHA1.Create())
                {
                    byte[] buf = new byte[81920];
                    long total = 0;
                    int read;
                    while ((read = await src.ReadAsync(buf, 0, buf.Length, ct).ConfigureAwait(false)) > 0)
                    {
                        sha.TransformBlock(buf, 0, read, null, 0);
                        await dst.WriteAsync(buf, 0, read, ct).ConfigureAwait(false);
                        total += read;
                        progress?.Report(total);
                    }
                    sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

                    if (!string.IsNullOrWhiteSpace(info.Sha1Sum))
                    {
                        string got = BitConverterToLowerHex(sha.Hash);
                        if (!string.Equals(got, info.Sha1Sum, StringComparison.OrdinalIgnoreCase))
                        {
                            dst.Dispose();
                            try { File.Delete(outputPath); } catch { }
                            throw new InvalidDataException(string.Format(
                                "SHA1 mismatch for {0}: expected {1}, got {2}.",
                                Path.GetFileName(outputPath), info.Sha1Sum, got));
                        }
                    }
                }
            }
        }

        private static string BitConverterToLowerHex(byte[] hash)
        {
            return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}
