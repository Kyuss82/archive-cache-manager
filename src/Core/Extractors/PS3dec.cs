using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace ArchiveCacheManager
{
    public class PS3dec : Extractor
    {
        string executablePath = Path.Combine(PathUtils.GetExtractorRootPath(), "ps3decrs.exe");

        public PS3dec()
        {

        }

        public static bool SupportedType(string archivePath)
        {
            return PathUtils.HasExtension(archivePath, new string[] { ".zip", ".iso", ".7z" });
        }

        public override bool Extract(string archivePath, string cachePath, string[] includeList = null, string[] excludeList = null)
        {
            string isoPath = string.Empty;
            string extension = Path.GetExtension(archivePath).ToLower();

            if (extension.Equals(".zip") || extension.Equals(".7z"))
            {
                Extractor zip = new Zip();
                if (zip.Extract(archivePath, cachePath, "*.iso".ToSingleArray(), excludeList))
                {
                    isoPath = Directory.GetFiles(cachePath, "*.iso")[0];
                }
            }
            else if (extension.Equals(".iso"))
            {
                Extractor robocopy = new Robocopy();
                if (robocopy.Extract(archivePath, cachePath, includeList, excludeList))
                {
                    isoPath = Directory.GetFiles(cachePath, "*.iso")[0];
                }
            }

            // Double-check isoPath is set, and not the original archive path, as it will be deleted.
            if (string.IsNullOrEmpty(isoPath) || string.Equals(isoPath, archivePath, StringComparison.InvariantCultureIgnoreCase))
            {
                Logger.Log($"PS3dec path check failed: {isoPath}");
                Environment.ExitCode = 1;
                return false;
            }
            //Read the key from the path
            string keypath = Path.Combine(PathUtils.GetPS3keyRootPath(), Path.ChangeExtension(Path.GetFileName(archivePath), ".dkey"));
            if (!File.Exists(keypath))
            {
                Logger.Log($"{keypath} not found");
                Environment.ExitCode = 1;
                return false;
            }
            string key = File.ReadLines(keypath).First();

            string temppath = Path.Combine(PathUtils.GetTempPath(), "temp.iso");

            // -k / --dk    decryption key (32 hex chars)
            // -t / --tc    thread count
            // -o / --output-dir   output directory
            // -n / --output-name  output filename without extension
            // -s / --skip  disables exit confirmation prompt
            // ISO path is a positional argument at the end
            string args = string.Format("--dk {0} --tc {1} --output-dir \"{2}\" --output-name \"temp\" --skip \"{3}\"", key, ThreadCount, PathUtils.GetTempPath(), isoPath);

            (string stdout, string stderr, int exitCode) = ProcessUtils.RunProcess(executablePath, args, false, null, false, ExtractionProgress);

            if (exitCode != 0)
            {
                Logger.Log($"PS3dec returned exit code {exitCode} with error output:\r\n{stderr}");
                Environment.ExitCode = exitCode;
            }
            File.Delete(isoPath);
            File.Move(temppath, isoPath);
            return exitCode == 0;
        }

        public override long GetSize(string archivePath, string fileInArchive = null)
        {
            string extension = Path.GetExtension(archivePath).ToLower();

            // If iso is still zipped, get its decompressed size. This will be (much) larger than the final xiso size.
            // This size will be used when calculating how much room to make in the cache, so no harm in over-estimating.
            // Actual converted xiso size will be checked and set in LaunchInfo.UpdateSizeFromCache() once extraction is complete.
            if (extension.Equals(".zip") || extension.Equals(".7z"))
            {
                Extractor zip = new Zip();
                return zip.GetSize(archivePath, fileInArchive);
            }

            // New PS3dec does not support a list/size command; read the file size directly.
            // Decryption does not change the ISO size, so this is accurate.
            return new FileInfo(archivePath).Length;
        }

        public override string[] List(string archivePath)
        {
            return string.Format("{0}.iso", Path.GetFileNameWithoutExtension(archivePath)).ToSingleArray();
        }

        public override string Name()
        {
            return "PS3dec";
        }

        public override string GetExtractorPath()
        {
            return executablePath;
        }
    }
}