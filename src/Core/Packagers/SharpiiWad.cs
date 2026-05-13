using System.IO;

namespace ArchiveCacheManager
{
    public static class SharpiiWad
    {
        private const string ExecutableName = "Sharpii-NetCore.exe";

        public static string GetExecutablePath() =>
            Path.Combine(PathUtils.GetExtractorRootPath(), ExecutableName);

        public static bool IsAvailable() => File.Exists(GetExecutablePath());

        public static (bool ok, string stdout, string stderr, int exitCode) PackWad(string inputDir, string outputWadPath)
        {
            string exe = GetExecutablePath();
            string args = string.Format("WAD P \"{0}\" \"{1}\"",
                inputDir.TrimEnd(Path.DirectorySeparatorChar),
                outputWadPath);

            (string stdout, string stderr, int exitCode) = ProcessUtils.RunProcess(exe, args, redirectOutput: true, redirectError: true);

            if (exitCode != 0)
            {
                Logger.Log(string.Format("Sharpii WAD pack returned exit code {0}.\r\nstdout:\r\n{1}\r\nstderr:\r\n{2}", exitCode, stdout, stderr));
            }

            return (exitCode == 0 && File.Exists(outputWadPath), stdout, stderr, exitCode);
        }
    }
}
