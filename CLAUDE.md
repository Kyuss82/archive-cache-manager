# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Archive Cache Manager is a **LaunchBox plugin** (Windows only, .NET 9 / `net9.0-windows`) that intercepts LaunchBox's "Extract ROM archives before running" pipeline. It is also a fork of upstream `fraganator/archive-cache-manager` that adds on-launch builders for Nintendo native formats (Wii U `.wua`, 3DS `.cia`, Wii `.wad`, DSi `.tad`) and Sony PKG handling (PS3/PSP/PSV).

This is a single-developer fork; there is no team workflow, no test suite, and no PR review process. Releases are cut by hand with `scripts/release.sh`.

## Build / release

The repo lives at `<repo>/`; all C# sources live under `<repo>/src/`. The solution is `src/ArchiveCacheManager.sln`.

```
cd src && dotnet build ArchiveCacheManager.sln -c Release
```

There is no `Debug` configuration in the solution — the GUIDs map Debug→Release for every project. Always build `-c Release`.

Building **Plugin** requires `thirdparty/Unbroken.LaunchBox.Plugins/12.8/Unbroken.LaunchBox.Plugins.dll`, which is the LaunchBox plugin SDK and is **not redistributable**. The user copies it from a real LaunchBox install (`<LaunchBox>\Core\Unbroken.LaunchBox.Plugins.dll`). The target path is gitignored. If the DLL is missing, only **Core** will compile.

CI (`.github/workflows/`) only builds `Core` — that's intentional. The full plugin `.zip` is built locally and uploaded as a GitHub Release asset.

The `AfterBuild` target on `ArchiveCacheManager.csproj` assembles `release/ArchiveCacheManager.zip` from the build outputs, the bundled `7-Zip/` directory, the badges, and the readme/history. The 7z stock binaries get renamed to `7z.exe.original` / `7z.dll.original` so the plugin's own shim takes their place (see "Architecture: the 7z shim" below).

To cut a release:

```
scripts/release.sh v2.78.0
```

The script enforces a clean working tree, a matching `## v<major>.<minor>` header in `HISTORY.md`, that `gh` is authenticated, builds Release, creates an annotated tag, pushes it, and publishes a GitHub Release with `release/ArchiveCacheManager.zip` attached.

Version bumps live in **three** `<AssemblyVersion>` blocks (one per `.csproj`: `Core`, `Plugin`, `ArchiveCacheManager`). All three must be kept in lockstep — the release script does not enforce this, but mismatched versions confuse the runtime version check.

## Architecture

### Three projects

- **Core** (`src/Core/`) — pure logic, no LaunchBox SDK dependency. Cache management, path utilities, extractors, packagers (Wii U/3DS/Wii/DSi builders and Sony PKG infrastructure), config I/O. This is the only assembly CI builds, by design. Most non-trivial logic should live here so it remains testable / inspectable independent of the proprietary SDK.
- **Plugin** (`src/Plugin/`) — the LaunchBox-facing assembly. Implements LaunchBox plugin interfaces (`IGameLaunchingPlugin`, menu items, the `Tools → Archive Cache Manager` config window, badge support). References `Unbroken.LaunchBox.Plugins.dll`. Output name is `ArchiveCacheManager.Plugin.dll`.
- **ArchiveCacheManager** (`src/ArchiveCacheManager/`) — the `7z.exe` **shim executable**. This is the entry point LaunchBox actually launches at game-start time. See below.

### The 7z shim model

LaunchBox launches `ThirdParty\7-Zip\7z.exe` with one of:

- `7z.exe x <rom path> -o<output path> -y -aoa -bsp1` (extract before game launch)
- `7z.exe l <rom path> -slt` (list archive contents)

The plugin's installer renames the stock 7z binary to `7z.exe.original` and drops `ArchiveCacheManager.exe` (renamed to `7z.exe` in the release zip) into its place. `Program.Main` dispatches on `args[0]`:

- `x` → `CacheManager.ExtractArchive` (caches if applicable, else passes through to the renamed stock 7z)
- `l` → `CacheManager.ListArchive` (returns cached file list when relevant)
- `c` → `CacheManager.BatchCacheArchive` (custom verb for batch caching UI)
- anything else → `Zip.Call7z(args)` (transparent pass-through)

The shim only intercepts archive extraction when the output path is `...\7-Zip\Temp` (or a subfolder of it). Anything else falls through.

Game metadata gets passed from the Plugin assembly to the shim via a `game.ini` written to the plugin folder before launch (see `GameInfo.cs` / `LaunchInfo.cs`).

### Game launch flow (Plugin side)

`GameLaunching.cs` implements `IGameLaunchingPlugin`. `OnBeforeGameLaunching` decides whether to intervene based on the emulator+platform's *Extraction Settings* row (`Config.GetEmulatorPlatformConfig`) and the archive's file type. When intervening, it writes `game.ini`, sets up the launch path, and lets LaunchBox's extraction flow run — which then triggers the shim above. `OnAfterGameLaunched` restores any modified LaunchBox state.

### The extractor abstraction

`Core/Extractors/Extractor.cs` is the abstract base. Concrete extractors (`Zip`, `Chdman`, `DolphinTool`, `ExtractXiso`, `PS3dec`, `Robocopy`, plus the fork-added `WiiuWuaExtractor`, `CiaExtractor`, `WadExtractor`, `TadExtractor`, `Ps3PkgExtractor`, `PspPkgExtractor`, `PsvPkgExtractor`, `Ctr3dsExtractor`) each:

- declare a static `SupportedType(archivePath)` (used by `GameLaunching.GetExtractorExists` and `UseArchiveCacheManager`),
- override `Name`, `GetSize`, `Extract`, `List`, `GetExtractorPath`,
- optionally override `AlwaysCache` (skip the MinArchiveSize threshold — used for builders whose cost is decrypt+repack, not source size).

**Adding a new extractor** is a multi-file change with no central registry — at minimum: a new `Extractors/<X>.cs`, a `Config.Get<X>CacheOnLaunch(key)` flag (in `Config.cs` and the `EmulatorPlatformConfig` class), wiring in `GameLaunching.GetExtractor` and `GameLaunching.UseArchiveCacheManager`, and a checkbox column in `NewConfigWindow`'s *Extraction Settings* grid.

### Packagers vs extractors

Anything named `*Extractor` is the launch-time hook. The heavier per-platform logic lives in `src/Core/Packagers/` and is invoked **from** the extractors. The `Packagers/` directory has its own [`README.md`](src/Core/Packagers/README.md) which is the authoritative map for the 44 files there (naming conventions, public API entry points, why `*Staging` classes were not abstracted onto a base). Read it before touching anything under `Packagers/`.

Key idea for the fork-added formats: a *packager* takes a CDN dump (`tmd` + `.app` blobs + cert chain) and rebuilds the native emulator-installable format (`.cia`, `.wad`, `.wua`, `.tad`). The encrypted title key for Wii U comes from Cemu's `keys.txt`; Nintendo cert chains are user-supplied (`title.cert`, `cert.sys`, `cert.tad`) and live in `<LaunchBox>/Plugins/ArchiveCacheManager/Extractors/`.

### Config

`Config.cs` is large (~90KB) because every emulator/platform pair has its own row of toggles. There is a global `[Archive Cache Manager]` section plus one `[<Emulator> \ <Platform>]` section per row. The fallback section is `[All \ All]`. Use `Config.GetEmulatorPlatformConfig(key)` to read a row, where `key = Config.EmulatorPlatformKey(emulatorTitle, platform)`.

### Runtime layout

Once installed, the plugin lives at `<LaunchBox>\Plugins\ArchiveCacheManager\`:

- `7z.exe` (the shim), `*.dll`, `*.runtimeconfig.json`
- `Extractors/` — user drops `chdman.exe`, `DolphinTool.exe`, `extract-xiso.exe`, `ps3decrs.exe`, `CDecrypt.exe`, `zarchive.exe`, `Sharpii-NetCore.exe`, `title.cert`, `cert.sys`, `cert.tad` here
- `7-Zip/7z.exe.original` + `7z.dll.original` — the stock 7z that the shim falls through to
- `Logs/events-YYYY-MM-DD.log` — per-day log files (`Logger.cs`)
- `config.ini`, `game.ini`, `game-index.ini` — runtime state

## Legal posture (important for changes touching keys / certs / proprietary blobs)

The plugin **invokes** third-party tools (CDecrypt, ZArchive, Sharpii-NetCore) and **does not redistribute** them. It does not bundle:

- Nintendo cert blobs (`cert.sys`, `title.cert`, `cert.tad`)
- The Wii U common key
- Any community title-key databases

When adding new platform support, preserve this posture: invoke user-supplied tools, read user-supplied keys, do not embed Nintendo/Sony cryptographic material as resources. `Core.csproj` has a comment block explaining how a private build could re-enable embedded certs as `EmbeddedResource` — do not do this in the public build.

## Conventions

- Target framework: `net9.0-windows` (WinForms + WPF + unsafe blocks enabled in Plugin).
- The repo uses LGPL-2.1 (inherited from upstream).
- `HISTORY.md` is the changelog; one `## v<major>.<minor>` heading per release. The release script greps for the heading matching the tag and uses everything below it (until the next `## `) as the GitHub Release notes — so write the entry before tagging.
- The shim catches all exceptions in `Main`, logs them, and sets `ExitCode = 1` rather than crashing — preserve that behaviour, because a crashing shim breaks LaunchBox's launch flow.
- `.cs` files use CRLF line endings (Windows source). `.gitattributes` enforces this.
- This is a WSL development environment with a Windows checkout (`/mnt/c/...`); paths in tooling are usually Windows-style.
