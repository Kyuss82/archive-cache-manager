using System.IO;

namespace ArchiveCacheManager
{
    /// <summary>
    /// Wrapper for Exzap's zarchive.exe, which packs a decrypted Wii U Loadiine folder into a .wua / .zar archive.
    /// Download: https://github.com/Exzap/ZArchive/releases
    /// Place zarchive.exe in the Extractors folder.
    /// </summary>
    public static class ZArchiveInvoker
    {
        private const string ExecutableName = "zarchive.exe";

        public static string GetExecutablePath() =>
            Path.Combine(PathUtils.GetExtractorRootPath(), ExecutableName);

        public static bool IsAvailable() => File.Exists(GetExecutablePath());

        public static (bool ok, string stdout, string stderr, int exitCode) Pack(string inputDir, string outputFile)
        {
            string exe = GetExecutablePath();
            string args = string.Format("\"{0}\" \"{1}\"",
                inputDir.TrimEnd(Path.DirectorySeparatorChar),
                outputFile);

            (string stdout, string stderr, int exitCode) = ProcessUtils.RunProcess(exe, args, redirectOutput: true, redirectError: true);

            if (exitCode != 0)
            {
                Logger.Log(string.Format("zarchive returned exit {0}.\r\nstdout:\r\n{1}\r\nstderr:\r\n{2}", exitCode, stdout, stderr));
            }

            bool produced = File.Exists(outputFile) && new FileInfo(outputFile).Length > 0;
            return (exitCode == 0 && produced, stdout, stderr, exitCode);
        }
    }
}
