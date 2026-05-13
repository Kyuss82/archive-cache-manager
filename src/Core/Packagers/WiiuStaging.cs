using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace ArchiveCacheManager
{
    public class WiiuBuildResult
    {
        public string OutputDir;
        public string ApplicationPath;
        public bool Success;
        public string ErrorMessage;
    }

    public static class WiiuStaging
    {
        private static readonly Regex ContentNameRegex = new Regex(@"^[0-9a-fA-F]{8}$", RegexOptions.Compiled);

        private const string ExpectedCertCrc32 = "0B80C239";
        private const string ExpectedCertMd5 = "420D5E6BB1BCB09B234F02CF6A6F4597";

        public static WiiuBuildResult BuildFromGameArchive(string archivePath, string outputBaseDir, string baseName)
        {
            var result = new WiiuBuildResult();

            if (!File.Exists(archivePath))
            {
                result.ErrorMessage = string.Format("Archive not found: {0}", archivePath);
                return result;
            }

            if (!CDecryptInvoker.IsAvailable())
            {
                result.ErrorMessage = string.Format("CDecrypt.exe not found in {0}", PathUtils.GetExtractorRootPath());
                return result;
            }

            if (string.IsNullOrWhiteSpace(Config.WiiuCommonKey) || Config.WiiuCommonKey.Trim().Length != 32)
            {
                result.ErrorMessage = "Wii U Common Key is not configured. Set 'WiiuCommonKey' in the plugin INI to a 32-character hex string.";
                return result;
            }

            string tempDir = Path.Combine(Path.GetTempPath(), "ACM_WiiuBuild_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                var extractor = new Zip();
                if (!extractor.Extract(archivePath, tempDir))
                {
                    result.ErrorMessage = string.Format("Failed to extract archive: {0}", archivePath);
                    return result;
                }

                string workDir = ResolveWorkDir(tempDir);

                string tmdPath = NormalizeTmd(workDir);
                if (tmdPath == null)
                {
                    result.ErrorMessage = "No TMD found in archive (expected 'tmd', 'tmd.<version>' or 'title.tmd').";
                    return result;
                }

                NormalizeContents(workDir);

                WiiuParsedTmd parsedTmd;
                try
                {
                    parsedTmd = WiiuTmd.Parse(tmdPath);
                }
                catch (Exception ex)
                {
                    result.ErrorMessage = string.Format("Failed to parse TMD: {0}", ex.Message);
                    return result;
                }

                string titleId = parsedTmd.TitleId.ToString("X16");
                Logger.Log(string.Format("Wii U build: title ID = {0}", titleId));

                string ticketPath = Path.Combine(workDir, "title.tik");
                if (!File.Exists(ticketPath))
                {
                    byte[] titleKey;
                    string chosenPassword;
                    try
                    {
                        titleKey = ResolveTitleKey(workDir, parsedTmd, out chosenPassword);
                    }
                    catch (Exception ex)
                    {
                        result.ErrorMessage = string.Format("Title key derivation failed: {0}", ex.Message);
                        return result;
                    }

                    if (titleKey == null)
                    {
                        result.ErrorMessage = string.Format(
                            "Could not derive a working title key for title {0}.\r\n\r\n" +
                            "The community title-key DB either has no entry for this title, or the entry is wrong (phunlabs is curated by hand and contains errors).\r\n\r\n" +
                            "To fix this, add the original 'cetk' file (also named 'title.tik' or 'ticket') inside the game's archive next to the tmd. " +
                            "The public NUS does not serve commercial titles, so the cetk must come from your original dump.\r\n\r\n" +
                            "If this is a homebrew / fake-signed title, set WiiuTitleKeyPassword in archive-cache-manager.ini to the correct password.",
                            titleId);
                        return result;
                    }

                    Logger.Log(string.Format("Wii U build: title key derived with password '{0}'", chosenPassword));

                    try
                    {
                        WiiuTicketBuilder.Build(titleId, titleKey, Config.WiiuCommonKey.Trim(), ticketPath);
                    }
                    catch (Exception ex)
                    {
                        result.ErrorMessage = string.Format("Failed to forge ticket: {0}", ex.Message);
                        return result;
                    }
                }

                string certPath = Path.Combine(workDir, "title.cert");
                if (!File.Exists(certPath))
                {
                    byte[] certBytes;
                    string source = LocateWiiuCertSource();
                    if (source != null)
                    {
                        certBytes = File.ReadAllBytes(source);
                        string actualCrc = ComputeCrc32Hex(certBytes);
                        string actualMd5 = ComputeMd5Hex(certBytes);
                        if (!string.Equals(actualCrc, ExpectedCertCrc32, StringComparison.OrdinalIgnoreCase) ||
                            !string.Equals(actualMd5, ExpectedCertMd5, StringComparison.OrdinalIgnoreCase))
                        {
                            result.ErrorMessage = string.Format(
                                "Wii U cert at '{0}' failed integrity check.\r\nExpected CRC32 {1} / MD5 {2}\r\nGot      CRC32 {3} / MD5 {4}",
                                source, ExpectedCertCrc32, ExpectedCertMd5, actualCrc, actualMd5);
                            return result;
                        }
                    }
                    else
                    {
                        certBytes = LoadEmbeddedCert();
                        if (certBytes == null)
                        {
                            result.ErrorMessage = "Wii U cert: embedded resource is missing and no override found in Extractors folder.";
                            return result;
                        }
                        Logger.Log("Wii U build: using embedded title.cert (no override in Extractors).");
                    }
                    File.WriteAllBytes(certPath, certBytes);
                }

                string outputDir = Path.Combine(outputBaseDir, baseName);
                if (Directory.Exists(outputDir))
                {
                    TryDeleteDirectory(outputDir);
                }
                Directory.CreateDirectory(outputDir);

                var (ok, stdout, stderr, exitCode) = CDecryptInvoker.Decrypt(workDir, outputDir);
                if (!ok)
                {
                    string detail = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;

                    // Preserve workDir on failure so the user can inspect intermediate files.
                    string preserved = Path.Combine(Path.GetTempPath(), "ACM_WiiuBuild_FAIL_" + Guid.NewGuid().ToString("N"));
                    try
                    {
                        Directory.Move(workDir, preserved);
                        Logger.Log(string.Format("CDecrypt failure: workDir preserved at {0}", preserved));
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(string.Format("CDecrypt failure: could not preserve workDir: {0}", ex.Message));
                        preserved = workDir;
                    }

                    result.ErrorMessage = string.Format(
                        "CDecrypt failed (exit {0}). {1}\r\n\r\n" +
                        "Files preserved at:\r\n{2}",
                        exitCode, detail, preserved);
                    return result;
                }

                result.OutputDir = outputDir;
                result.ApplicationPath = LocateRpx(outputDir) ?? outputDir;
                result.Success = true;

                if (Config.WiiuPackAsWua && ZArchiveInvoker.IsAvailable())
                {
                    string wuaPath = outputDir + ".wua";

                    // Cemu's .wua reader requires each title to live in a "<TITLEID>_v<VERSION>" folder
                    // at the archive root. CDecrypt outputs code/content/meta loose, so wrap them.
                    string wuaTitleFolder = string.Format("{0:X16}_v{1}", parsedTmd.TitleId, parsedTmd.TitleVersion);
                    string packStagingDir = outputDir + "_wuapack";
                    string innerDir = Path.Combine(packStagingDir, wuaTitleFolder);

                    bool stagingOk = false;
                    try
                    {
                        if (Directory.Exists(packStagingDir)) TryDeleteDirectory(packStagingDir);
                        Directory.CreateDirectory(packStagingDir);
                        Directory.Move(outputDir, innerDir);
                        stagingOk = true;
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(string.Format("Wii U build: failed to stage wua layout: {0}", ex.Message));
                    }

                    if (stagingOk)
                    {
                        var (wuaOk, wuaStdout, wuaStderr, wuaExit) = ZArchiveInvoker.Pack(packStagingDir, wuaPath);
                        if (wuaOk)
                        {
                            Logger.Log(string.Format("Wii U build: packed Loadiine folder into {0}", wuaPath));
                            TryDeleteDirectory(packStagingDir);
                            result.OutputDir = Path.GetDirectoryName(wuaPath);
                            result.ApplicationPath = wuaPath;
                        }
                        else
                        {
                            Logger.Log(string.Format("Wii U build: zarchive packing failed (exit {0}); keeping Loadiine folder. {1}",
                                wuaExit,
                                string.IsNullOrWhiteSpace(wuaStderr) ? wuaStdout : wuaStderr));
                            try
                            {
                                Directory.Move(innerDir, outputDir);
                                TryDeleteDirectory(packStagingDir);
                            }
                            catch (Exception ex)
                            {
                                Logger.Log(string.Format("Wii U build: could not restore Loadiine folder after pack failure: {0}", ex.Message));
                            }
                        }
                    }
                }
                else if (Config.WiiuPackAsWua)
                {
                    Logger.Log(string.Format("Wii U build: WUA packing requested but zarchive.exe not found in {0}. Keeping Loadiine folder.",
                        PathUtils.GetExtractorRootPath()));
                }

                return result;
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString(), Logger.LogLevel.Exception);
                result.ErrorMessage = ex.Message;
                return result;
            }
            finally
            {
                TryDeleteDirectory(tempDir);
            }
        }

        private static string ResolveWorkDir(string tempDir)
        {
            string[] entries = Directory.GetFileSystemEntries(tempDir);
            if (entries.Length == 1 && Directory.Exists(entries[0]))
            {
                return entries[0];
            }
            return tempDir;
        }

        private static string NormalizeTmd(string workDir)
        {
            string canonical = Path.Combine(workDir, "title.tmd");
            if (File.Exists(canonical)) return canonical;

            var candidates = Directory.GetFiles(workDir, "tmd*", SearchOption.TopDirectoryOnly)
                .Where(p =>
                {
                    string n = Path.GetFileName(p);
                    return string.Equals(n, "tmd", StringComparison.OrdinalIgnoreCase)
                        || n.StartsWith("tmd.", StringComparison.OrdinalIgnoreCase);
                })
                .OrderByDescending(p => ParseTmdVersion(Path.GetFileName(p)))
                .ToList();

            if (candidates.Count == 0) return null;

            File.Copy(candidates[0], canonical, true);
            return canonical;
        }

        private static void NormalizeContents(string workDir)
        {
            var contents = Directory.GetFiles(workDir, "*", SearchOption.TopDirectoryOnly)
                .Where(p => ContentNameRegex.IsMatch(Path.GetFileName(p)))
                .ToArray();

            foreach (var content in contents)
            {
                string appPath = content + ".app";
                if (!File.Exists(appPath))
                {
                    File.Move(content, appPath);
                }
            }

            string tik = Path.Combine(workDir, "title.tik");
            if (File.Exists(tik)) return;

            foreach (string candidate in new[] { "cetk", "tik", "ticket", "title.cetk", "ticket.bin", "cetk.bin" })
            {
                string p = Path.Combine(workDir, candidate);
                if (File.Exists(p))
                {
                    File.Copy(p, tik, true);
                    return;
                }
            }

            string highestCetk = Directory.GetFiles(workDir, "cetk.*", SearchOption.TopDirectoryOnly)
                .Where(p =>
                {
                    string n = Path.GetFileName(p);
                    return n.StartsWith("cetk.", StringComparison.OrdinalIgnoreCase);
                })
                .OrderByDescending(p => ParseTmdVersion(Path.GetFileName(p)))
                .FirstOrDefault();

            if (highestCetk != null)
            {
                File.Copy(highestCetk, tik, true);
                return;
            }

            string anyTik = Directory.GetFiles(workDir, "*.tik", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (anyTik != null)
            {
                File.Copy(anyTik, tik, true);
            }
        }

        private static int ParseTmdVersion(string fileName)
        {
            if (string.Equals(fileName, "tmd", StringComparison.OrdinalIgnoreCase)) return 0;
            int dot = fileName.IndexOf('.');
            if (dot < 0 || dot == fileName.Length - 1) return 0;
            return int.TryParse(fileName.Substring(dot + 1), out int v) ? v : 0;
        }

        private static byte[] DecryptEncTitleKey(byte[] encKey, string commonKeyHex, string titleIdHex)
        {
            if (encKey == null || encKey.Length != 16) throw new ArgumentException("Encrypted title key must be 16 bytes.");
            if (string.IsNullOrWhiteSpace(commonKeyHex) || commonKeyHex.Length != 32)
                throw new ArgumentException("Wii U Common Key must be 32 hex characters.");

            byte[] commonKey = new byte[16];
            for (int i = 0; i < 16; i++) commonKey[i] = Convert.ToByte(commonKeyHex.Substring(i * 2, 2), 16);

            byte[] iv = new byte[16];
            string tid = titleIdHex.PadLeft(16, '0');
            for (int i = 0; i < 8; i++) iv[i] = Convert.ToByte(tid.Substring(i * 2, 2), 16);

            using (var aes = Aes.Create())
            {
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.None;
                aes.Key = commonKey;
                aes.IV = iv;
                using (var dec = aes.CreateDecryptor())
                {
                    return dec.TransformFinalBlock(encKey, 0, encKey.Length);
                }
            }
        }

        private static readonly string[] WiiuPasswordCandidates = new[]
        {
            "mypass",
            "nintendo",
            "test",
            "1234567890",
            "Lucy131211",
            "fbf10",
            "5678",
            "1234",
            "",
            "MAGIC",
        };

        private static byte[] TryEncryptedKey(byte[] encKey, string sourceLabel, string titleIdHex, WiiuTmd.VerificationTarget target, out string chosenLabel)
        {
            chosenLabel = null;
            if (encKey == null) return null;

            byte[] plain;
            try
            {
                plain = DecryptEncTitleKey(encKey, Config.WiiuCommonKey.Trim(), titleIdHex);
            }
            catch (Exception ex)
            {
                Logger.Log(string.Format("Wii U build: failed to decrypt encrypted title key from {0}: {1}", sourceLabel, ex.Message));
                return null;
            }

            string encHex = BitConverter.ToString(encKey).Replace("-", "").ToLowerInvariant();
            string plainHex = BitConverter.ToString(plain).Replace("-", "").ToLowerInvariant();

            if (target == null)
            {
                Logger.Log(string.Format("Wii U build: {0} hit for {1} (enc={2} plain={3}); no verification target available, using as-is.", sourceLabel, titleIdHex, encHex, plainHex));
                chosenLabel = string.Format("<{0}, unverified>", sourceLabel);
                return plain;
            }

            if (WiiuTmd.VerifyTitleKey(target, plain))
            {
                Logger.Log(string.Format("Wii U build: {0} hit for {1} (enc={2} plain={3}); content-verified.", sourceLabel, titleIdHex, encHex, plainHex));
                chosenLabel = string.Format("<{0}>", sourceLabel);
                return plain;
            }

            Logger.Log(string.Format("Wii U build: key from {0} for {1} (enc={2}) FAILED content verification — entry may be wrong.", sourceLabel, titleIdHex, encHex));
            return null;
        }

        private static byte[] ResolveTitleKey(string workDir, WiiuParsedTmd tmd, out string chosenPassword)
        {
            string titleIdHex = tmd.TitleId.ToString("X16");
            var target = WiiuTmd.ChooseTarget(workDir, tmd);

            Logger.Log(string.Format("Wii U build: verification target = {0}",
                target == null ? "<none>" : string.Format("content {0:X8} (index={1}, size={2}, type=0x{3:X4}, hashed={4})",
                    target.Content.Id, target.Content.Index, target.Content.Size, target.Content.Type, target.H3Path != null)));

            // Encrypted title key lookup: Cemu keys.txt (local, offline, user-curated). The key is
            // AES-decrypted with the common key and verified against the TMD content hash.
            byte[] result = TryEncryptedKey(WiiuCemuKeys.Lookup(titleIdHex), "Cemu keys.txt", titleIdHex, target, out string label);
            if (result != null) { chosenPassword = label; return result; }

            Logger.Log(string.Format("Wii U build: title {0} not in Cemu keys.txt — falling back to passwords.", titleIdHex));

            var ordered = new List<string>();
            string configured = Config.WiiuTitleKeyPassword;
            if (!string.IsNullOrWhiteSpace(configured))
            {
                ordered.Add(configured);
            }
            foreach (var p in WiiuPasswordCandidates)
            {
                if (!ordered.Contains(p, StringComparer.Ordinal))
                {
                    ordered.Add(p);
                }
            }

            if (target == null)
            {
                chosenPassword = ordered[0];
                Logger.Log("Wii U build: no .app available for title-key verification, using first candidate password unverified.");
                return WiiuTitleKey.Derive(titleIdHex, chosenPassword);
            }

            foreach (string candidate in ordered)
            {
                byte[] key;
                try { key = WiiuTitleKey.Derive(titleIdHex, candidate); }
                catch (Exception ex)
                {
                    Logger.Log(string.Format("Wii U build: derive failed for password '{0}': {1}", candidate, ex.Message));
                    continue;
                }

                bool ok = WiiuTmd.VerifyTitleKey(target, key);
                Logger.Log(string.Format("Wii U build: password '{0}' -> key {1} -> {2}",
                    candidate,
                    BitConverter.ToString(key).Replace("-", "").ToLowerInvariant(),
                    ok ? "VERIFIED" : "fail"));
                if (ok)
                {
                    chosenPassword = candidate;
                    return key;
                }
            }

            chosenPassword = null;
            return null;
        }

        private const string EmbeddedCertResourceName = "ArchiveCacheManager.Packagers.title.cert";

        private static byte[] LoadEmbeddedCert()
        {
            var asm = typeof(WiiuStaging).Assembly;
            using (var stream = asm.GetManifestResourceStream(EmbeddedCertResourceName))
            {
                if (stream == null) return null;
                using (var ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    return ms.ToArray();
                }
            }
        }

        private static string LocateWiiuCertSource()
        {
            string extractorRoot = PathUtils.GetExtractorRootPath();
            foreach (string name in new[] { "title.cert", "wiiu-title.cert", "wiiu.cert" })
            {
                string p = Path.Combine(extractorRoot, name);
                if (File.Exists(p)) return p;
            }
            return null;
        }

        private static string LocateRpx(string outputDir)
        {
            try
            {
                string codeDir = Path.Combine(outputDir, "code");
                if (Directory.Exists(codeDir))
                {
                    var rpx = Directory.GetFiles(codeDir, "*.rpx", SearchOption.TopDirectoryOnly).FirstOrDefault();
                    if (rpx != null) return rpx;
                }
                return Directory.GetFiles(outputDir, "*.rpx", SearchOption.AllDirectories).FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        private static void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(string.Format("Failed to delete dir {0}: {1}", path, ex.Message));
            }
        }

        private static string ComputeMd5Hex(byte[] data)
        {
            using (var md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(data);
                return BitConverter.ToString(hash).Replace("-", string.Empty).ToUpperInvariant();
            }
        }

        private static readonly uint[] Crc32Table = BuildCrc32Table();

        private static uint[] BuildCrc32Table()
        {
            const uint poly = 0xEDB88320u;
            uint[] table = new uint[256];
            for (uint i = 0; i < 256; i++)
            {
                uint c = i;
                for (int k = 0; k < 8; k++)
                {
                    c = (c & 1) != 0 ? (poly ^ (c >> 1)) : (c >> 1);
                }
                table[i] = c;
            }
            return table;
        }

        private static string ComputeCrc32Hex(byte[] data)
        {
            uint crc = 0xFFFFFFFFu;
            for (int i = 0; i < data.Length; i++)
            {
                crc = Crc32Table[(crc ^ data[i]) & 0xFF] ^ (crc >> 8);
            }
            crc ^= 0xFFFFFFFFu;
            return crc.ToString("X8");
        }
    }
}
