using System.IO;

namespace ArchiveCacheManager
{
    public static class CDecryptInvoker
    {
        private const string ExecutableName = "CDecrypt.exe";

        public static string GetExecutablePath() =>
            Path.Combine(PathUtils.GetExtractorRootPath(), ExecutableName);

        public static bool IsAvailable() => File.Exists(GetExecutablePath());

        public static (bool ok, string stdout, string stderr, int exitCode) Decrypt(string inputDir, string outputDir)
        {
            string exe = GetExecutablePath();
            string args = string.Format("\"{0}\" \"{1}\"",
                inputDir.TrimEnd(Path.DirectorySeparatorChar),
                outputDir.TrimEnd(Path.DirectorySeparatorChar));

            (string stdout, string stderr, int exitCode) = ProcessUtils.RunProcess(exe, args, redirectOutput: true, redirectError: true);

            if (exitCode != 0)
            {
                Logger.Log(string.Format("CDecrypt returned exit code {0}.\r\nstdout:\r\n{1}\r\nstderr:\r\n{2}", exitCode, stdout, stderr));
            }

            bool produced = Directory.Exists(outputDir) && Directory.GetFileSystemEntries(outputDir).Length > 0;
            return (exitCode == 0 && produced, stdout, stderr, exitCode);
        }
    }
}
