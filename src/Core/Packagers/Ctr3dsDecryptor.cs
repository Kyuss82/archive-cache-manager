/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Top-level 3DS .3ds/.cci decryptor. Streams the source NCSD, decrypts every
 * encrypted NCCH partition section in place, and writes the result to a new
 * file with the NoCrypto flag set so Azahar / Citra / Lime3DS load it as
 * already-decrypted content.
 *
 * Per partition, the decryptor handles:
 *   • ExHeader (always primary key, 0x800 bytes)
 *   • ExeFS    (primary key for the .code-relative subset of file 0; secondary
 *               key for the rest — for our purposes we decrypt the entire
 *               ExeFS with the secondary key, which is what Decrypt9 + Citra do
 *               when crypto_method == 0 because primary == secondary)
 *   • RomFS    (secondary key — Secure2/3/4 for 7.x+ titles)
 *
 * For seed-crypto titles (flags[7] bit 5), the KeyY for ExeFS/RomFS is replaced
 * by SHA-256(KeyY || seed)[0..16] before scrambling.
 */
using System;
using System.IO;
using System.Security.Cryptography;

namespace ArchiveCacheManager
{
    public class Ctr3dsDecryptResult
    {
        public bool Success;
        public string ErrorMessage;
        public string OutputPath;
        public int PartitionsDecrypted;
        public int PartitionsSkipped;
    }

    public static class Ctr3dsDecryptor
    {
        private const int CopyBuffer = 64 * 1024;
        private const int ExHeaderOffset = 0x200;
        private const int ExHeaderSize = 0x800;

        public static Ctr3dsDecryptResult Decrypt(string sourcePath, string outputPath, Action<string> progress = null)
        {
            var result = new Ctr3dsDecryptResult { OutputPath = outputPath };
            if (!File.Exists(sourcePath))
            {
                result.ErrorMessage = string.Format("Source not found: {0}", sourcePath);
                return result;
            }
            if (!CtrAesKeys.IsAvailable)
            {
                result.ErrorMessage = "aes_keys.txt is missing or has no slot0x2CKeyX — drop it into Extractors/ or set Ctr3dsKeysPath.";
                return result;
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

                // First: byte-for-byte copy source → output. We'll then overwrite the encrypted
                // ranges in place with decrypted bytes. This preserves the NCSD layout perfectly,
                // including padding between partitions and the trailing block, without us having
                // to re-emit the container structure manually.
                File.Copy(sourcePath, outputPath, true);
                try { File.SetAttributes(outputPath, FileAttributes.Normal); } catch { }

                CtrParsedNcsd parsed;
                using (var src = File.OpenRead(sourcePath))
                {
                    parsed = CtrNcchReader.Parse(src);
                }

                using (var fs = new FileStream(outputPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None, CopyBuffer, FileOptions.RandomAccess))
                {
                    foreach (var partition in parsed.Partitions)
                    {
                        if (partition.Ncch == null) { result.PartitionsSkipped++; continue; }
                        if (partition.Ncch.IsNoCrypto)
                        {
                            progress?.Invoke(string.Format("Partition {0}: already plain — skipped.", partition.Index));
                            result.PartitionsSkipped++;
                            continue;
                        }
                        if (partition.Ncch.IsFixedKey)
                        {
                            // FixedKey games (typically system titles or homebrew with the all-zero key).
                            // Out of scope for the retail .3ds flow — refuse loudly.
                            progress?.Invoke(string.Format("Partition {0}: fixed-key NCCH — not supported, skipping.", partition.Index));
                            result.PartitionsSkipped++;
                            continue;
                        }

                        progress?.Invoke(string.Format(
                            "Partition {0}: decrypting (pid=0x{1:X16}, prog=0x{2:X16}, crypto={3:X2}{4})…",
                            partition.Index, partition.Ncch.PartitionId, partition.Ncch.ProgramId,
                            partition.Ncch.CryptoMethod,
                            partition.Ncch.IsSeedCrypto ? ", seed" : ""));

                        DecryptPartition(fs, partition);

                        // Clear encryption flags on the now-decrypted partition header so emulators
                        // treat it as plain content. Flags live at offset 0x188 of the NCCH header.
                        long flagsAbs = partition.FileOffset + 0x188;
                        fs.Position = flagsAbs;
                        byte[] flagsBuf = new byte[8];
                        if (fs.Read(flagsBuf, 0, 8) == 8)
                        {
                            flagsBuf[3] = 0x00;                  // crypto_method
                            flagsBuf[7] = (byte)((flagsBuf[7] | 0x04) & ~0x20); // set NoCrypto, clear SeedCrypto
                            fs.Position = flagsAbs;
                            fs.Write(flagsBuf, 0, 8);
                        }

                        result.PartitionsDecrypted++;
                    }
                }

                result.Success = result.PartitionsDecrypted > 0;
                if (!result.Success && string.IsNullOrEmpty(result.ErrorMessage))
                {
                    result.ErrorMessage = "No partitions required decryption (file may already be plain).";
                }
                return result;
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString(), Logger.LogLevel.Exception);
                try { if (File.Exists(outputPath)) File.Delete(outputPath); } catch { }
                result.Success = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        private static void DecryptPartition(FileStream fs, CtrNcsdPartition partition)
        {
            var ncch = partition.Ncch;
            byte[] keyXPrimary   = CtrAesKeys.GetKeyX(0x2C)
                ?? throw new InvalidDataException("aes_keys.txt is missing slot0x2CKeyX (the retail NCCH KeyX).");
            byte[] keyXSecondary = CtrAesKeys.GetKeyX(ncch.SecondaryKeyXSlot());
            if (keyXSecondary == null && ncch.SecondaryKeyXSlot() != 0x2C)
            {
                throw new InvalidDataException(string.Format(
                    "aes_keys.txt is missing slot0x{0:X2}KeyX (needed for crypto_method 0x{1:X2}).",
                    ncch.SecondaryKeyXSlot(), ncch.CryptoMethod));
            }
            if (keyXSecondary == null) keyXSecondary = keyXPrimary;

            byte[] keyYPrimary = ncch.KeyY;
            byte[] keyYSecondary = keyYPrimary;
            if (ncch.IsSeedCrypto)
            {
                byte[] seed = CtrAesKeys.LookupSeed(ncch.ProgramId);
                if (seed == null)
                {
                    throw new InvalidDataException(string.Format(
                        "Seed-crypto NCCH (program 0x{0:X16}) but no matching seed in seeddb.bin. " +
                        "Populate Extractors/seeddb.bin or Ctr3dsSeedDbPath.",
                        ncch.ProgramId));
                }
                using (var sha = SHA256.Create())
                {
                    byte[] input = new byte[16 + 16];
                    Buffer.BlockCopy(keyYPrimary, 0, input, 0, 16);
                    Buffer.BlockCopy(seed,        0, input, 16, 16);
                    byte[] hash = sha.ComputeHash(input);
                    keyYSecondary = new byte[16];
                    Buffer.BlockCopy(hash, 0, keyYSecondary, 0, 16);
                }
            }

            byte[] normalKeyPrimary   = CtrKeyScrambler.Derive(keyXPrimary,   keyYPrimary);
            byte[] normalKeySecondary = CtrKeyScrambler.Derive(keyXSecondary, keyYSecondary);

            // ExHeader — primary key, always at NCCH offset 0x200, size 0x800.
            if (ncch.ExHeaderSize > 0)
            {
                int exhSize = (int)Math.Min(ExHeaderSize, ncch.ExHeaderSize);
                long abs = partition.FileOffset + ExHeaderOffset;
                DecryptRegion(fs, abs, exhSize,
                    normalKeyPrimary,
                    CtrNcchReader.BuildCounter(ncch.PartitionId, CtrNcchReader.SectionExHeader));
            }

            // ExeFS — primary key. Offset/size in NCCH header are in media units (0x200) relative to NCCH start.
            if (ncch.ExeFsSizeMediaUnits > 0)
            {
                long abs = partition.FileOffset + (long)ncch.ExeFsOffsetMediaUnits * CtrNcchReader.MediaUnitSize;
                long size = (long)ncch.ExeFsSizeMediaUnits * CtrNcchReader.MediaUnitSize;
                DecryptRegion(fs, abs, size,
                    normalKeyPrimary,
                    CtrNcchReader.BuildCounter(ncch.PartitionId, CtrNcchReader.SectionExeFs));
            }

            // RomFS — secondary key (Secure2/3/4 only swap this).
            if (ncch.RomFsSizeMediaUnits > 0)
            {
                long abs = partition.FileOffset + (long)ncch.RomFsOffsetMediaUnits * CtrNcchReader.MediaUnitSize;
                long size = (long)ncch.RomFsSizeMediaUnits * CtrNcchReader.MediaUnitSize;
                DecryptRegion(fs, abs, size,
                    normalKeySecondary,
                    CtrNcchReader.BuildCounter(ncch.PartitionId, CtrNcchReader.SectionRomFs));
            }
        }

        private static void DecryptRegion(FileStream fs, long absoluteOffset, long size, byte[] key, byte[] iv)
        {
            using (var ctr = new PsPkgAesCtr(key, iv))
            {
                byte[] buf = new byte[CopyBuffer];
                long remaining = size;
                ulong relOffset = 0;
                fs.Position = absoluteOffset;
                while (remaining > 0)
                {
                    int chunk = (int)Math.Min((long)buf.Length, remaining);
                    long blockStart = fs.Position;
                    int read = fs.Read(buf, 0, chunk);
                    if (read <= 0) throw new EndOfStreamException("Truncated NCCH region.");
                    ctr.Decrypt(relOffset, buf, 0, read);
                    fs.Position = blockStart;
                    fs.Write(buf, 0, read);
                    relOffset  += (ulong)read;
                    remaining  -= read;
                }
            }
        }
    }
}
