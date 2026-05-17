/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * On-launch wrapper for PS3 NPDRM PKG titles. Sister to Ps3IsoLauncher.
 *
 * The existing PS3 PKG on-launch flow extracts the decrypted file tree into
 * `<cache>/<baseName>/`, then LaunchBox launches rpcs3 with the cache-path
 * EBOOT.BIN as the positional argument. That works for non-NPDRM titles, but
 * NPDRM titles (Amy, most PSN releases) fail with "Game data corrupted / Cannot
 * read SELF" because RPCS3 only consults `dev_hdd0/home/00000001/exdata/` for
 * `<contentid>.rap` when the title is sitting under `dev_hdd0/game/<TID>/` —
 * not for an eboot booted from an arbitrary path.
 *
 * The wrapper script below sidesteps that:
 *   1. Reads TITLE_ID out of the PARAM.SFO that sits next to the cache eboot.
 *   2. robocopy /MIR's the cache install tree into
 *      `<RpcsGameDir>/<TID>/`. robocopy is idempotent and skips files whose
 *      timestamps + sizes match, so the second launch onwards costs almost
 *      nothing.
 *   3. Launches rpcs3 with `<RpcsGameDir>/<TID>/USRDIR/EBOOT.BIN` — which
 *      satisfies the klicensee lookup that resolves the rap from
 *      `dev_hdd0/home/00000001/exdata/<contentid>.rap` (the rap itself is
 *      already staged there by Ps3PkgStaging).
 *
 * Same hijack technique as Ps3IsoLauncher: `GameLaunching` swaps
 * emulator.ApplicationPath to powershell.exe + this script in
 * OnBeforeGameLaunching and restores both in OnAfterGameLaunched.
 */
using System;
using System.IO;

namespace ArchiveCacheManager
{
    public static class Ps3PkgRpcs3Launcher
    {
        private const string ScriptFileName = "ps3-pkg-rpcs3-launcher.ps1";

        // Inlines a small PSF parser so we don't need to ship a separate helper. The PSF format
        // is well-documented (psdevwiki) — magic \0PSF, then 16-byte index entries pointing into
        // a key table and a value table.
        private const string ScriptBody =
@"param (
    [Parameter(Mandatory=$true)][string]$Rpcs3Path,
    [Parameter(Mandatory=$true)][string]$RpcsGameDir,
    [Parameter(Mandatory=$true)][string]$EbootInCache
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $Rpcs3Path))    { throw ""rpcs3 not found: $Rpcs3Path"" }
if (-not (Test-Path -LiteralPath $EbootInCache)) { throw ""eboot not found: $EbootInCache"" }

# Parse TITLE_ID from PARAM.SFO that sits one level above USRDIR/EBOOT.BIN.
$gameRoot   = Split-Path -Parent (Split-Path -Parent $EbootInCache)
$paramSfo   = Join-Path $gameRoot 'PARAM.SFO'
if (-not (Test-Path -LiteralPath $paramSfo)) {
    throw ""PARAM.SFO not found next to eboot at: $paramSfo""
}

function Read-PsfValue([string]$path, [string]$key) {
    $bytes = [System.IO.File]::ReadAllBytes($path)
    if ($bytes.Length -lt 20 -or [System.BitConverter]::ToUInt32($bytes, 0) -ne 0x46535000) {
        throw ""Not a PSF file: $path""
    }
    $keyTable  = [System.BitConverter]::ToUInt32($bytes, 8)
    $dataTable = [System.BitConverter]::ToUInt32($bytes, 12)
    $count     = [System.BitConverter]::ToUInt32($bytes, 16)
    for ($i = 0; $i -lt $count; $i++) {
        $entry  = 20 + $i * 16
        $keyOff = [System.BitConverter]::ToUInt16($bytes, $entry)
        $dataLen= [System.BitConverter]::ToUInt32($bytes, $entry + 4)
        $dataOff= [System.BitConverter]::ToUInt32($bytes, $entry + 12)
        $kStart = $keyTable + $keyOff
        $kEnd   = $kStart
        while ($kEnd -lt $bytes.Length -and $bytes[$kEnd] -ne 0) { $kEnd++ }
        $kName  = [System.Text.Encoding]::ASCII.GetString($bytes, $kStart, $kEnd - $kStart)
        if ($kName -eq $key) {
            $vStart = $dataTable + $dataOff
            $val    = [System.Text.Encoding]::UTF8.GetString($bytes, $vStart, $dataLen)
            return $val.TrimEnd([char]0)
        }
    }
    return $null
}

$tid = Read-PsfValue $paramSfo 'TITLE_ID'
if ([string]::IsNullOrWhiteSpace($tid)) {
    throw ""Could not read TITLE_ID from $paramSfo""
}
Write-Host ""[ACM] PS3 NPDRM wrapper: TITLE_ID = $tid""

$targetGameDir = Join-Path $RpcsGameDir $tid
$targetEboot   = Join-Path $targetGameDir 'USRDIR\EBOOT.BIN'

# robocopy /MIR creates the target tree on first run; on later runs it skips files whose size +
# timestamp already match, so this is almost free past the first launch. /NJH /NJS keep the
# output quiet, /R:0 /W:0 means 'don't retry on transient failures'. /XO 'exclude older' could
# be added if you ever want to keep target-side edits, but for cache-driven content it's better
# to mirror.
Write-Host ""[ACM] robocopy ""${gameRoot}"" -> ""${targetGameDir}""""
$rcArgs = @($gameRoot, $targetGameDir, '/MIR', '/COPY:DAT', '/DCOPY:DAT', '/R:0', '/W:0', '/NFL', '/NDL', '/NP', '/NJH', '/NJS')
$proc = Start-Process -FilePath 'robocopy.exe' -ArgumentList $rcArgs -NoNewWindow -Wait -PassThru
# robocopy exit codes 0..7 are success-ish; 8+ is a real failure.
if ($proc.ExitCode -ge 8) {
    throw ""robocopy failed with exit code $($proc.ExitCode)""
}
if (-not (Test-Path -LiteralPath $targetEboot)) {
    throw ""Mirror finished but target eboot is missing: $targetEboot""
}

Write-Host ""[ACM] launching rpcs3 with $targetEboot""
& $Rpcs3Path $targetEboot
";

        /// <summary>Returns the absolute path to the script on disk, writing it if missing or stale.</summary>
        public static string GetScriptPath()
        {
            string baseDir = Path.GetDirectoryName(typeof(Ps3PkgRpcs3Launcher).Assembly.Location);
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
                Logger.Log(string.Format("Failed to write PS3 PKG RPCS3 launcher script: {0}", ex.Message));
            }
            return path;
        }

        /// <summary>Resolves the dev_hdd0/game directory next to the given rpcs3.exe.</summary>
        public static string ResolveRpcs3GameDir(string rpcs3Path)
        {
            if (string.IsNullOrWhiteSpace(rpcs3Path)) return null;
            string abs = PathUtils.GetAbsolutePath(rpcs3Path);
            string exeDir = Path.GetDirectoryName(abs);
            if (string.IsNullOrWhiteSpace(exeDir)) return null;
            return Path.Combine(exeDir, "dev_hdd0", "game");
        }
    }
}
