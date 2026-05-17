# `src/Core/Packagers/` — file map

Platform-specific staging, decryption, key management, and update/DLC
installation. 44 files, ~12k LOC. This README is a map for orientation —
read the per-file headers for actual semantics.

## Naming conventions

| Prefix       | Scope                                                              |
|--------------|--------------------------------------------------------------------|
| `Ps3*`       | Sony PlayStation 3 (PKG + RAP + RPCS3 layout)                      |
| `Psp*`       | Sony PSP (PKG + RAP + PPSSPP memstick layout)                      |
| `Psv*`       | Sony PS Vita (PKG + zRIF licence + Vita3K layout, optional .vpk)   |
| `Ps*`        | Sony PKG primitives shared across PS3/PSP/PSV (reader, AES, keys)  |
| `Wii*`       | Nintendo Wii (WAD format, Wii-specific ticket + title-key crypto)  |
| `Wiiu*`      | Nintendo Wii U (WUA archives, Cemu keys, NUS/Loadiine TMDs)        |
| `Ctr*`       | 3DS crypto primitives (AES key tables, key scrambler, NCCH reader) |
| `Ctr3ds*`    | 3DS pipeline classes (decryptor, raw .3ds staging)                 |
| `Cia*`       | 3DS CIA installer-package format (build + staging)                 |
| `Tad*`       | DSi TAD format (build + staging)                                   |
| `Local*`     | Offline mirror indexer for PS3/PSP/PSV/Wii U/3DS updates + DLC     |
| `Nps*`       | NoPayStation TSV database lookup                                   |
| `Nus*`       | Nintendo Update Server (CDN fetch for Wii / Wii U updates)         |
| `Z*`         | zlib-flavoured (`Zrif` = PSV licence text codec) / 7zip wrappers   |

## Sony PKG infrastructure (shared by PS3, PSP, PSV)

| File                          | LOC  | Role                                                       |
|-------------------------------|------|------------------------------------------------------------|
| `PsPkgReader.cs`              |  417 | Parses PKG headers + metadata block (shared format)        |
| `PsPkgUnpacker.cs`            |  261 | Decrypts AES-CTR body + extracts files; takes optional `pathTransform` |
| `PsPkgKeys.cs`                |   89 | PKG AES-ECB key tables (PS3 / PSP / PSV)                    |
| `PsPkgAesCtr.cs`              |   96 | AES-CTR cipher wrapper                                      |
| `PsRap.cs`                    |  131 | `.rap` NPDRM licence file handling (16-byte binary)         |
| `Zrif.cs`                     |  190 | PSV zRIF text codec ↔ `.rif` binary (uses preset zlib dict) |
| `PkgStagingUtils.cs`          |   62 | Shared pure utilities: HexToBytes, LooksLikePkg, TryDeleteDirectory |

## PS3-specific

| File                          | LOC  | Role                                                       |
|-------------------------------|------|------------------------------------------------------------|
| `Ps3PkgStaging.cs`            |  448 | Archive → RPCS3 install root (base + update + DLC merge)   |
| `Ps3UpdateFetcher.cs`         |  362 | Sony CDN fetch for PS3 updates                              |
| `Ps3UpdateInstaller.cs`       |  323 | Install fetched PKGs to `dev_hdd0/game/<TID>/`              |
| `Ps3DlcInstaller.cs`          |  160 | DLC-only installer variant (PS3 / PSP / PSV via context)    |
| `Ps3PkgRawExporter.cs`        |  236 | Raw PKG → folder export (NPS-Browser style)                 |
| `SonyUpdateContext.cs`        |   58 | Shared state for the PS3/PSP/PSV update fetch flow          |

## PSP/PSV-specific

| File                          | LOC  | Role                                                       |
|-------------------------------|------|------------------------------------------------------------|
| `PspPkgStaging.cs`            |  504 | Archive → `PSP/GAME/<TITLE_ID>/` layout; threads pathTransform |
| `PsvPkgStaging.cs`            |  315 | Archive → Vita3K `ux0:app/<TID>/`; optional `.vpk` wrapping |

PSV update / DLC paths reuse `Ps3UpdateInstaller` / `Ps3DlcInstaller` with
`SonyUpdateContext.ForPsv()`. PSP same with `ForPsp()`. There's no
PspUpdateFetcher / PsvUpdateFetcher — they delegate.

## Nintendo Wii / Wii U crypto + staging

| File                          | LOC  | Role                                                       |
|-------------------------------|------|------------------------------------------------------------|
| `WiiTicketBuilder.cs`         |   97 | Wii ticket file generation                                   |
| `WiiTitleKeys.cs`             |  117 | Wii title key DB lookup                                      |
| `WiiuTicketBuilder.cs`        |   73 | Wii U ticket file generation                                 |
| `WiiuTitleKeys.cs`            |   69 | Wii U deterministic title-key derivation (PHP port)         |
| `WiiuCemuKeys.cs`             |  140 | Cemu per-game key storage                                   |
| `WiiuTmd.cs`                  |  268 | TMD (Title Metadata) parser — shared format with 3DS        |
| `TmdReader.cs`                |   33 | TMD file reader (shared)                                    |
| `WadStaging.cs`               |  382 | WAD → folder extraction (Wii classic)                       |
| `WadBuilder.cs`               |  137 | `.wad` creation from extracted contents                     |
| `WiiuStaging.cs`              |  589 | WUA → Cemu cache layout + ticket+TMD handling               |
| `ZArchiveInvoker.cs`          |   37 | `zarchive.exe` CLI wrapper (.wua extraction)                |
| `CDecryptInvoker.cs`          |   32 | `cdecrypt.exe` CLI wrapper (Loadiine layout)                |

## 3DS / DSi

| File                          | LOC  | Role                                                       |
|-------------------------------|------|------------------------------------------------------------|
| `Ctr3dsStaging.cs`            |   66 | Raw `.3ds` ROM decryption staging                            |
| `Ctr3dsDecryptor.cs`          |  228 | 3DS ROM AES-CTR decryption logic                             |
| `CtrAesKeys.cs`               |  179 | 3DS AES key tables + derivation                              |
| `CtrKeyScrambler.cs`          |   69 | 3DS key XOR/scrambling                                       |
| `CtrNcchReader.cs`            |  207 | NCCH (3DS content container) header parser                   |
| `CtrTicketBuilder.cs`         |   79 | 3DS ticket creation                                          |
| `CtrTitleKeys.cs`             |  116 | 3DS title key DB lookup                                      |
| `CiaStaging.cs`               |  356 | CIA installer → decrypted folder                             |
| `CiaBuilder.cs`               |  156 | Rebuild `.cia` from decrypted content                        |
| `TadStaging.cs`               |  315 | TAD (DSi) → folder extraction                                |
| `TadBuilder.cs`               |  136 | TAD archive creation                                         |
| `TicketUtils.cs`              |   39 | Ticket encryption helpers (shared 3DS/Wii)                   |

## Local mirror indexing & manifests

| File                          | LOC  | Role                                                       |
|-------------------------------|------|------------------------------------------------------------|
| `LocalPkgManifest.cs`         |   83 | Data model (DTOs + `LocalPkgPlatform` enum) — JSON schema   |
| `LocalPkgIndexer.cs`          |  247 | Orchestrator: BuildIndex, CountFiles, Save, Load, FoldersForPlatform |
| `LocalPkgScanner.cs`          |  783 | Scanning + parsing (Wii U TMDs, 3DS CIAs/TMDs, Sony PKGs in zips) |
| `LocalManifestResolver.cs`    |  128 | Consults JSON manifest when installers need a local PKG      |
| `NpsDb.cs`                    |  286 | NoPayStation TSV parser (PSN game DB)                        |
| `NusFetcher.cs`               |   70 | Nintendo Update Server CDN fetch (Wii/Wii U)                 |

`LocalPkgIndexer` is a `public static partial class` split across
`LocalPkgIndexer.cs` (orchestration) and `LocalPkgScanner.cs` (scanning).
Private members are visible across both files.

## Public API entry points (called from `Plugin/`)

```
Ps3PkgStaging::BuildFromGameArchive()         — game launch pipeline (PS3)
PspPkgStaging::BuildFromGameArchive()         — game launch pipeline (PSP)
PsvPkgStaging::BuildFromGameArchive()         — game launch pipeline (PSV)
WadStaging::BuildFromGameArchive()            — Wii
WadBuilder::BuildWad()                        — right-click "Create WAD..."
WiiuStaging::BuildFromGameArchive()           — Wii U
CiaStaging::BuildFromGameArchive()            — 3DS CIA
CiaBuilder::BuildCia()                        — right-click "Create CIA..."
Ctr3dsStaging::DecryptFromGameArchive()       — 3DS .3ds raw
TadStaging::BuildFromGameArchive()            — DSi
TadBuilder::BuildTad()                        — right-click "Create TAD..."

Ps3PkgRawExporter::ExportFromGameArchive()    — bulk raw export (NPS style)
Ps3UpdateFetcher::FetchUpdates()              — Sony CDN: PS3/PSP/PSV via context
Ps3UpdateInstaller::InstallUpdates()          — on-launch install
Ps3DlcInstaller::InstallDlcs()                — on-launch DLC install

LocalPkgIndexer::BuildIndex()                 — Local Mirror Indexer hub
LocalPkgIndexer::Load()                       — installers read at boot
LocalManifestResolver::Resolve*()             — installers materialise PKGs from .zip wrappers
```

## Why some classes are *not* abstracted onto a base

The 7 `*Staging` classes (Ps3 / Psp / Psv / Wad / Wiiu / Cia / Ctr3ds)
share the *shape* `extract archive → enumerate contents → parse → run
unpacker → write platform layout`, but **not the body**:
- PSP threads a `PspPkgPathTransform` callback through `PsPkgUnpacker.Unpack`
  that PS3/PSV don't need.
- PS3 unpacks N PKGs into a shared install root; PSP into
  `PSP/GAME/<TITLE_ID>/`; PSV expects exactly one PKG.
- Licensing diverges fundamentally: PS3/PSP stage `.rap` files; PSV decodes
  zRIF into `.rif` and injects `work.bin` into `sce_sys/package/`.
- Wii uses ticket + title-key derivation; 3DS uses NCCH + AES-CTR; TAD has
  its own format.

A `<TParser, TWriter>` base would require ~5–6 virtuals to express these
differences, which is more indirection than the duplication costs. v2.73
extracted only the byte-identical utilities into `PkgStagingUtils`; the
orchestrators stay independent on purpose.
