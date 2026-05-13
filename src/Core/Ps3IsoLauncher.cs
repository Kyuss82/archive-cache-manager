using System;
using System.IO;

namespace ArchiveCacheManager
{
    /// <summary>
    /// Helper that materialises the PS3 ISO mount launcher PowerShell script next to the plugin
    /// and reports its on-disk path. Lazily created on first request.
    ///
    /// Approach adapted from ptmorris1/RPCS3-ISOLauncher-Launchbox
    /// (https://github.com/ptmorris1/RPCS3-ISOLauncher-Launchbox). The script below is parameterised
    /// (-RPCS3path / -ISOpath) so the launcher does not need to live next to rpcs3.exe, and the
    /// fixed 2s post-mount sleep is replaced with a bounded poll on Get-Volume to handle slow
    /// optical-style virtual drives.
    /// </summary>
    public static class Ps3IsoLauncher
    {
        private const string ScriptFileName = "ps3-iso-launcher.ps1";

        // Improved over the original drag-and-drop variant: accepts -RPCS3path as a parameter so the
        // launcher does not need to live next to rpcs3.exe, and replaces the fixed 2s sleep with a
        // bounded poll on Get-Volume so it works on slow drives too.
        private const string ScriptBody =
@"param (
    [Parameter(Mandatory=$true)][string]$ISOpath,
    [Parameter(Mandatory=$true)][string]$RPCS3path
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $ISOpath))   { throw ""ISO not found: $ISOpath"" }
if (-not (Test-Path -LiteralPath $RPCS3path)) { throw ""RPCS3 not found: $RPCS3path"" }

$vol = Mount-DiskImage -ImagePath $ISOpath -PassThru

# Wait up to ~10s for the volume drive letter to appear.
$driveLetter = $null
for ($i = 0; $i -lt 50; $i++) {
    $volInfo = $vol | Get-Volume -ErrorAction SilentlyContinue
    if ($volInfo -and $volInfo.DriveLetter) { $driveLetter = $volInfo.DriveLetter; break }
    Start-Sleep -Milliseconds 200
}
if (-not $driveLetter) {
    Dismount-DiskImage -ImagePath $ISOpath | Out-Null
    throw ""Mounted ISO did not get a drive letter within 10s.""
}

$eboot = ""${driveLetter}:\PS3_GAME\USRDIR\EBOOT.BIN""
if (-not (Test-Path -LiteralPath $eboot)) {
    Dismount-DiskImage -ImagePath $ISOpath | Out-Null
    throw ""EBOOT.BIN not found on mounted drive: $eboot""
}

try {
    & $RPCS3path $eboot
    Start-Sleep -Seconds 2
    Wait-Process -Name (Split-Path -LeafBase $RPCS3path) -ErrorAction SilentlyContinue
}
finally {
    Dismount-DiskImage -ImagePath $ISOpath | Out-Null
}
";

        /// <summary>Returns the absolute path to the script on disk, writing it if missing or stale.</summary>
        public static string GetScriptPath()
        {
            string baseDir = Path.GetDirectoryName(typeof(Ps3IsoLauncher).Assembly.Location);
            string path = Path.Combine(baseDir, ScriptFileName);

            try
            {
                string existing = File.Exists(path) ? File.ReadAllText(path) : null;
                if (existing != ScriptBody)
                {
                    File.WriteAllText(path, ScriptBody);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(string.Format("Failed to write PS3 ISO launcher script: {0}", ex.Message));
            }
            return path;
        }

        public static string PowerShellExePath
        {
            get
            {
                string sys = Environment.GetFolderPath(Environment.SpecialFolder.System);
                return Path.Combine(sys, @"WindowsPowerShell\v1.0\powershell.exe");
            }
        }

        public static bool LooksLikeRpcs3(string applicationPath) =>
            !string.IsNullOrWhiteSpace(applicationPath) &&
            string.Equals(Path.GetFileNameWithoutExtension(applicationPath), "rpcs3", StringComparison.OrdinalIgnoreCase);
    }
}
