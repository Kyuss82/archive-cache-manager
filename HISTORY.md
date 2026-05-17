# Archive Cache Manager change history
## v2.79 (Kyuss82 fork)
* **Library-purge feature gets typed categories.** v2.78 surfaced only Update + DLC matches; v2.79 expands the scope to themes, system titles, demos, and an explicit "Other" catch-all so users can also purge XMB themes, applets, Wii U system content, etc. when those entries clutter the library. Same right-click menu, same window, broader scope.
* **Scanner refactor: typed `EntryRole`.** `LocalPkgScanner.EntryRole` grows from `{Skip, Update, Dlc}` to `{Skip, Update, Dlc, Theme, SystemTitle, Demo, Other}`. Cases previously dropped silently as "Skip" with a reason string are now promoted to their typed role:
  * **PS3 `Categorise()`**: `0x09` (XMB theme) → `Theme`; `0x0A` (avatar) → `Other`; unknown `content_type` → `Other` instead of swallowed `Skip`. Base game (`0x05`) and Patch-by-filename detection unchanged.
  * **PSP `Categorise()`**: known base content types (`0x06` PSP game, `0x07` theme-on-PSP, `0x18` mini) stay `Skip`; everything else that's not a Patch-by-name or DLC-by-id falls through as `Other` for review.
  * **PSV `Categorise()`**: `0x17` → `Theme`; `0x14`/`0x15` (base app/game) stay `Skip`; unknown → `Other`. DLC (`0x16`) and Patch (filename) unchanged.
  * **Wii U `AddWiiuEntry`**: `0x00050002` → `Demo` (was Skip "demo"); `0x00050010` → `SystemTitle` (was Skip "system title"). Base (`0x00050000`) and unknown still Skip.
  * **3DS `AddCtr3dsEntry`**: `0x00040001` / `0x00040002` / `0x00040003` / `0x00040010` / `0x00040020` all → `SystemTitle` (were five different Skip-with-reason cases). Base (`0x00040000`) and unknown still Skip.
* **Schema additions (additive — old manifests stay readable).** `LocalPkgTitle` grows four new lists alongside `Updates` / `Dlcs`: `Themes` (`themes`), `SystemTitles` (`system_titles`), `Demos` (`demos`), `Other` (`other`). `LocalPkgManifest` grows a top-level `Orphans` (`orphans`) list for non-base entries that have no resolvable parent title id — e.g. a PS3 theme whose content id doesn't decode into a base TID. Missing fields in old JSON files deserialize to empty lists, so existing manifests load without re-indexing (they just won't yield new categories until the user re-runs the indexer).
* **Bucket routing centralised** in a new `AddToBucket(LocalPkgTitle, LocalPkgEntry, EntryRole, LocalPkgScanProgress)` helper that returns the role label used in `[idx]` log lines. The previous if/else chains in `AddWiiuEntry` / `AddCtr3dsEntry` / `ProcessOnePsxPkg` are replaced with a single call, so all three platforms share routing and log-formatting.
* **Orphan handling for Sony PKGs.** `ProcessOnePsxPkg` used to skip any entry where `ExtractTitleId` returned null. v2.79 keeps that behaviour only for Update/DLC (the installers consume `Updates` / `Dlcs` grouped by TID and have no concept of an orphan), but routes Theme / System / Demo / Other entries without a parent TID to `manifest.Orphans` so they still surface in the purge UI.
* **`LibraryPurgeCandidate` switches from `bool IsDlc` to `string Kind`.** Kind values are stable constants on a new static class `LibraryPurgeKind` (`Update`, `DLC`, `Theme`, `System`, `Demo`, `Other`). `LibraryPurgeKind.IsDefaultChecked(kind)` drives the UI's default-tick rule: only `Update` and `DLC` are pre-checked, everything else is shown but opt-in (these are less-obvious purge targets — a user may have intentionally added a theme as a library entry).
* **`LibraryPurgeCandidates.EnumerateAll()` now iterates all six per-title lists plus `manifest.Orphans` per platform**, yielding one `LibraryPurgeCandidate` per non-base entry across PS3/PSP/PSV/Wii U/3DS. Zip-wrapped entries are still filtered (same rationale as v2.78 — a wrapper might also contain the base game).
* **`LibraryPurgeWindow` UI changes:**
  * Kind column now shows the typed value (`Theme` / `System` / `Demo` / `Other`) instead of just `Update` / `DLC`.
  * Default tick rule per row: `LibraryPurgeKind.IsDefaultChecked(candidate.Kind)` — Update + DLC pre-checked, others surfaced but unchecked.
  * New preset button **"Check Update+DLC"** sets the safe-default tick pattern in one click (handy after exploring the broader categories and wanting to reset).
  * Existing **"Check All"** / **"Uncheck All"** / **"Refresh"** kept (only renamed — were "Select All" / "Select None").
  * Summary line below the grid now breaks down by category: `Scanned N entries — M matched (X Update, Y DLC, Z Theme, ...)`.
  * Confirmation dialog before purge lists per-kind counts: `• Update: 4  • DLC: 12  • Theme: 1` so the user sees exactly what's about to be removed.
  * Window title and right-click caption updated to `Purge Library Entries (Update / DLC / Theme / System / Demo / Other)…` to reflect the broader scope.
* **Re-indexing required** to populate the new categories from existing libraries — old manifests don't have the new fields, and the scanner only fills them on a fresh `BuildIndex` run. Users who only want the v2.78 Update+DLC purge can keep their existing manifests; the new categories show up after a re-index.
* **Files touched:** `Core/Packagers/LocalPkgManifest.cs` (schema), `Core/Packagers/LocalPkgScanner.cs` (`EntryRole` + `AddToBucket` + per-platform routing), `Core/Packagers/LibraryPurgeCandidates.cs` (`Kind` field + `LibraryPurgeKind` constants + all-list enumeration), `Plugin/LibraryPurgeWindow.cs` (UI + safe-default presets + per-kind summary), `Plugin/PurgeUpdateDlcMenuItem.cs` (caption).

## v2.78 (Kyuss82 fork)
* **Library-wide purge of Update / DLC entries via right-click → "Purge Update/DLC Library Entries…".** Cleans up the LaunchBox library when the user has imported every individual update and DLC `.pkg` / `.cia` / `.wua` alongside the base titles. After running it, only base games stay in the library; updates and DLCs continue to be applied automatically by the plugin's existing update-manager pipeline at launch (since the source files remain on disk and stay indexed in the manifests).
* **Source of truth is the existing `LocalPkgIndexer` JSON manifests.** No re-parsing of files, no filename heuristics — `LibraryPurgeCandidates.EnumerateAll()` walks every persisted `local_pkg_index.json` (PS3/PSP/PSV under each platform's update cache root) and `local_rom_index.json` (Wii U at `WiiuLocalCache/`, 3DS at `Ctr3dsLocalCache/`) and yields one candidate per entry that already lives in the `Updates` or `Dlcs` arrays. Since the indexer never records base titles in those arrays, anything yielded is by construction a non-base file and safe to surface.
* **Path matching is exact, not heuristic.** The window builds a `Dictionary<normalizedAbsolutePath, candidate>` from the manifests, then walks `PluginHelper.DataManager.GetAllGames()` once and keeps only entries whose `ApplicationPath` (resolved via `PathUtils.GetAbsolutePath` + lowercased) hits the dictionary. False-positive rate is effectively zero — a library entry only matches if its file is the same file the indexer classified as an update or DLC.
* **Zip-wrapped manifest entries are deliberately skipped.** When the indexer found a `.pkg` inside a `.zip` / `.7z` / `.rar` (NPS-style scene packs), the manifest sets `ArchivePath` to the wrapper. But a wrapper can also contain the base game; the manifest doesn't record that. Surfacing the wrapper path would risk purging the base library entry, so `LibraryPurgeCandidates.TryMake` rejects any entry with a non-empty `ArchivePath`. Only standalone files on disk (`PkgPath` set, `ArchivePath` empty) become purge candidates. Documented in the file header.
* **Files on disk are never touched.** Purge calls `IDataManager.TryRemoveGame(IGame)` per selected row and `IDataManager.Save(true)` once at the end — that's library-state-only. The actual `.pkg` / `.cia` / `.wua` files stay where they are, which is the point: the update manager pipeline still finds them next time the user launches the related base game.
* **UX:**
  * Right-click any game on a packaging-eligible platform (PS3 / PSP / PSV / Wii U / 3DS) → *Purge Update/DLC Library Entries…*.
  * Window shows a DataGridView (`LibraryPurgeWindow`) with columns: Select (checkbox, all pre-checked), Title, Kind (Update / DLC), Platform (PS3 / PSP / PSV / Wiiu / Ctr3ds — from the manifest), LB Platform (raw value from the library entry, for sanity-checking mismatches), Title ID, Application Path.
  * Buttons: *Select All* / *Select None* / *Refresh* / *Purge Selected* / *Close*. Confirmation dialog before destructive action spells out "files on disk are NOT touched" so the user doesn't think they're deleting the source PKGs.
  * Summary line at the bottom reports `Scanned N library entries — M matched (X updates, Y DLCs)` after the scan and `Removed N entries from library, F failed.` after purging.
  * On success, sets `RefreshLaunchBox = true` so the LaunchBox main view re-loads after the dialog closes (same convention as `BatchCacheWindow` / `NewConfigWindow`).
* **Visibility gate** matches the rest of the packaging menu items: only shown in LaunchBox (not BigBox), only valid on games whose platform matches one of `Config.MatchesPs3PkgPlatform` / `MatchesPspPkgPlatform` / `MatchesPsvPkgPlatform` / `MatchesWiiuPlatform` / `MatchesCiaPlatform` / `MatchesCtr3dsPlatform`. The window itself operates on the full library though — the right-clicked game is only used to gate menu visibility.
* **Failure modes handled explicitly:**
  * No manifests indexed yet → "No Local Mirror manifests found. Run the Local PKG Indexer first." (purge button disabled).
  * Manifest read fails → logged + "Failed to read manifests — see log."
  * `TryRemoveGame` returns false for a row → logged + counted as failed, the rest of the batch continues.
* **Wii (`.wad`) is intentionally out of scope** — the `LocalPkgPlatform` enum has no `Wii` entry; only Wii U is indexed today. If we add a Wii indexer later, the purge picks it up for free (it iterates every value of `LocalPkgPlatform`).
* **Files added:** `Core/Packagers/LibraryPurgeCandidates.cs` (Core-side enumerator + path normalizer), `Plugin/LibraryPurgeWindow.cs` (Form), `Plugin/PurgeUpdateDlcMenuItem.cs` (`IGameMenuItemPlugin` entry point). No changes to existing files — the feature is additive.

## v2.77 (Kyuss82 fork)
* **`WiiuUpdateInstaller` now handles `.wua` sources via on-the-fly `zarchive.exe` extraction.** v2.75 skipped any manifest entry whose `PkgPath` was a `.wua` archive ("Loadiine-folder only"); v2.77 extends the install pipeline to extract them transparently:
  * **`ZArchiveInvoker.Extract(inputWua, outputDir)`** — new sibling of the existing `Pack` method. `zarchive.exe` auto-detects pack-vs-extract from the input arg's type (file → extract, directory → pack), so the CLI shape is identical: `"<input.wua>" "<output dir>"`. Logs stdout/stderr on nonzero exit and verifies the output dir actually got populated before returning success.
  * **`ResolveLoadiineSource` rewrite** — returns `(sourceDir, tempDirToCleanup)`. For a `.wua` entry: makes a `Path.GetTempPath()/ACM_WiiuWuaUnpack_<guid>/` dir, calls `ZArchiveInvoker.Extract`, returns the temp dir as the robocopy source and itself as the cleanup target. For Loadiine folders (the v2.75 path): returns the existing folder, no cleanup. For everything else (`<zip>!entry`, NUS dumps without cdecrypt, `.wua` with zarchive missing from Extractors/): returns `(null, null)` and the entry is skipped with a clear log line.
  * **`MaybeUnwrapSingleChildDir`** — some `.wua` packs wrap the Loadiine root under a `<TID16>/code/...` parent dir. After extract, if the temp output contains exactly one subfolder and that subfolder has the Loadiine signature (`code/` / `content/` / `meta/`), the robocopy source steps into it. Otherwise the temp dir itself is used as-is.
  * **Cleanup**: the install loop's `finally` deletes `tempDirToCleanup` after the robocopy step (success or failure). Errors during cleanup are logged but don't surface as install failures.
* **Requirements**: `zarchive.exe` must be present at `<plugin>/Extractors/zarchive.exe` (the same binary the existing "Create Wii U Package…" pack-as-WUA feature uses). The installer logs `[skip] … zarchive.exe missing` and continues with the next manifest entry if it's not there.
* **Still skipped today** — `<zip>!entry` wrappers (would need a separate unzip + classify-inner-layout pass) and raw NUS dumps (`tmd.NN` + `cetk` + `NNNNNNNN.app`, which need cdecrypt). Users with NUS sources continue to run `cdecrypt` themselves and re-index. `.zip` wrapper support is a v2.78 candidate if anyone reports needing it.

## v2.76 (Kyuss82 fork)
* **3DS install-on-demand via right-click menu.** The Wii U auto-install model (robocopy decrypted folders into Cemu's `mlc01`) doesn't translate to 3DS because Citra/Azahar/Lime3DS store titles encrypted with a per-instance key — pre-cooking the NAND tree isn't an option, every CIA must go through the emulator's installer. So instead of an at-launch auto-install, v2.76 adds a user-driven flow.
* **New right-click menu `Ctr3dsInstallUpdatesMenuItem`** on 3DS games: opens `Ctr3dsInstallDialog` listing the manifest's updates + DLCs that match the launching title's TID. The user ticks which to install and clicks *Install*; ACM invokes the configured emulator (`citra-qt.exe` / `azahar.exe` / `lime3ds.exe`) once per selected CIA with the CIA path as a positional arg — that's the documented install-trigger for all three forks. We wait for each emulator process to exit so installs serialize cleanly.
* **Title-ID resolution** for the launching game lives in new `Ctr3dsTitleIdResolver` (Core/Packagers/):
  * `.cia` → walk the CIA header sections to the TMD blob, read title_id at offset 0x18C (same math as `LocalPkgScanner.ReadCiaTitleId`).
  * `.3ds` / `.cci` → read MediaID at NCSD header offset 0x108 (8 bytes LE) — this equals the title_id for retail cart images.
  * Folder source → recurse for the first `.cia` or `.3ds`.
* **Manifest matching**: `baseTid = "00040000" + (resolvedTid & 0xFFFFFFFF).ToString("X8")` indexes into the JSON manifest's `Titles` dictionary, identical to the WiiU pattern. Only entries whose source is an existing `.cia` on disk are shown as installable (raw TMDs from NUS dumps and entries inside `.zip` wrappers are filtered — those would need a separate cdecrypt / unzip pre-pass).
* **Emulator path discovery**: the dialog pre-fills the executable path from the game's configured LB emulator (`PluginHelper.DataManager.GetEmulatorById(game.EmulatorId).ApplicationPath`). User can edit / browse if it's wrong.
* **Why not auto-install at launch like Wii U?** Citra/Azahar's `--install` semantics typically pop a GUI install dialog and write to a NAND only that emulator instance can decrypt. Running it transparently at every launch would (a) spam install dialogs the user has to dismiss, (b) re-install on every launch since we can't reliably introspect Citra's NAND from outside. On-demand from a menu was the cleanest UX trade-off after listing the three options.

## v2.75 (Kyuss82 fork)
* **Wii U auto-install at game launch.** Promised in v2.49 ("v2.50 will pick that up"), arrived in v2.75. When `WiiuAutoInstallUpdates` / `WiiuAutoInstallDlcs` is on and a Wii U game is launching, ACM consults `<plugin>/WiiuLocalCache/local_rom_index.json` (built by `LocalPkgIndexer`) and robocopies matching entries into Cemu's `mlc01/usr/title/0005000E/<TID>/` (updates) or `0005000C/<TID>/` (DLCs) before the emulator starts. PS3/PSP/PSV equivalent has lived inside `Ps3UpdateInstaller`/`Ps3DlcInstaller` since v2.48; Wii U was the missing peer.
* **New `WiiuUpdateInstaller`** in `src/Core/Packagers/`:
  * Title-ID resolution for the launching game: filename `<TID16>_v<ver>.wua` first, sibling TMD (`title.tmd` / `tmd.NN` / bare `tmd`) second, Loadiine `code/title.tmd` third. Skips cleanly if none of those resolve.
  * MLC root: standard portable layout (`<emulator>/mlc01/`) is auto-detected; non-portable Cemu installs are out of scope today.
  * Manifest lookup: derives `baseTid = "00050000" + lowHex` from the resolved TID and indexes into `manifest.Titles`.
  * Installer: `robocopy <source> <target> /MIR /R:1 /W:1` for each manifest entry. Source is the parent folder of the TMD; target is `<mlc01>/usr/title/<entry.ContentType:X8>/<entry.TitleId.lower>/`.
* **Source-format support today: Loadiine-folder only.** Loadiine means a folder containing `title.tmd` plus `code/` / `content/` / `meta/` subdirs — the native Cemu install layout. The indexer also catches `.wua` archives and NUS dumps (`tmd.NN` + `cetk` + `NNNNNNNN.app`); both require external decryption before they're installable, so they're skipped with `[skip] … not a Loadiine folder` rather than attempted. Users with NUS dumps run `cdecrypt` once over their mirror, re-scan, and the resulting Loadiine folders auto-install. `.wua` extraction via `zarchive.exe` is a candidate for v2.76.
* **Hook location**: new `#region Wii U auto-install` block in `GameLaunching.cs OnBeforeGameLaunching`, gated on `Config.MatchesWiiuPlatform(game.Platform)` + at least one of the two flags being on. Runs synchronously before the cache extraction starts; failures are logged but don't abort the launch.
* **Settings**: two new checkboxes in *Packaging → Wii U* — "On-launch: install updates / DLCs from local mirror index → Cemu mlc01". Built via a new `AddBoundCheck` helper that creates a runtime `CheckBox` and defers the Config write to the OK button (matches the existing Designer flow: Cancel discards edits).
* Two new Config properties (`WiiuAutoInstallUpdates`, `WiiuAutoInstallDlcs`) wired through the INI load/save/reset pipeline alongside the existing PS3/PSP/PSV ones.
* **3DS counterpart pending v2.76** — Citra/Azahar NAND layout (`nand:/title/<high>/<low>/`) is conceptually similar but the path-resolution from the Citra executable is messier (multiple Citra forks, sdmc vs nand, per-user vs portable). Splitting Wii U and 3DS keeps each tractable.

## v2.74 (Kyuss82 fork)
* **`Packagers/` orientation README + `LocalPkgIndexer` partial-class split.** Polishing pass — zero user-visible change, makes the `src/Core/Packagers/` directory readable for someone arriving cold.
  * **`src/Core/Packagers/README.md`**: file-by-file map of the 44 .cs files in `Packagers/`, organised by platform (Sony PKG infra, PS3-specific, PSP/PSV, Wii/Wii U crypto+staging, 3DS/DSi, Local mirror indexing). Documents the naming conventions (`Ps3`/`Psp`/`Psv` = Sony PKG; `Wii`/`Wiiu`; `Ctr` = 3DS crypto primitives vs `Ctr3ds` = 3DS workflow; `Local*` = offline indexer). Lists the public API entry points the Plugin layer calls. Explains why the staging classes are not abstracted onto a base.
  * **`LocalPkgIndexer` → split into `LocalPkgIndexer.cs` (orchestrator, 247 LOC) + `LocalPkgScanner.cs` (scanning, 783 LOC)** via `public static partial class`. The orchestrator file now reads cleanly as "BuildIndex / CountFiles / Save / Load / ResolveManifestPath / FoldersForPlatform" — no more 1,000-line monolith. Private members are visible across the partial halves so zero API surface changes.
* **Logger-prefix convention pass: skipped on purpose.** Audit found ~30 `Logger.Log("…")` calls without a class prefix, all in upstream ACM code (`CacheManager.cs`, `Config.cs`, etc.) — not fork additions. The fork additions (LocalPkgIndexer, PkgStagingUtils, Wiiu/Ctr3ds staging, NewConfigWindow rework) already follow `"<Class>: <message>"`. A partial pass over the upstream files would make the convention murkier, not cleaner. Leaving upstream untouched is the right call.

## v2.73 (Kyuss82 fork)
* **Pkg staging utilities consolidated into `PkgStagingUtils`** — `Ps3PkgStaging`, `PspPkgStaging`, and `PsvPkgStaging` each carried their own private copy of `HexToBytes` / `LooksLikePkg` / `TryDeleteDirectory`. v2.73 lifts these three pure-utility functions into a new `PkgStagingUtils` static class and rewires the call sites. No behaviour change, no per-platform branching needed — these are byte-identical helpers (modulo the `TryDeleteDirectory` log prefix, which is now an optional `caller` parameter).
* **Why this is *not* the full `ArchiveStaging<TParser, TWriter>` base-class refactor.** I dug into the three Sony staging classes hoping to consolidate the pipeline (extract → enumerate → parse → unpack → write) onto a shared base. Audit conclusion: **don't**. The three classes share a *shape*, not a *body*. Specifically:
  * PSP threads a `PspPkgPathTransform` callback through `PsPkgUnpacker.Unpack` to strip `USRDIR/CONTENT/` prefixes — PS3/PSV don't. A naive base template would have to expose a virtual on every call.
  * PS3 unpacks N PKGs (base + update + DLC) into a single shared install root; PSP unpacks them into `PSP/GAME/<TITLE_ID>/`; PSV unpacks exactly one PKG and fails otherwise. The "install root" is platform-specific in load-bearing ways.
  * Licensing is fundamentally different: PS3/PSP stage `.rap` files and copy them to the emulator's exdata/license folder; PSV decodes zRIF into `.rif` and injects `work.bin` into `sce_sys/package/`. Merging this under a "StageLicense" virtual would be contorted.
  * Update/DLC paths are computed per-platform and passed to different installers with different argument sets.
* **Decision: keep the orchestrators independent.** A `<TParser, TWriter>` base class would be premature: two examples is a coincidence, three is a pattern emerging, you really want a fourth before committing to inheritance. The orchestration code is ~1,280 LOC total across the three Sony classes; the genuinely-shared utility code is ~50 LOC — that 50 LOC is now in `PkgStagingUtils` and that's the *right* size of shared surface. Further candidates (RAP staging loop, temp-dir creation, NPS DB fallback) showed enough subtle divergence on close reading that pulling them into a helper would have masked platform-specific behaviour rather than simplified it.

## v2.72 (Kyuss82 fork)
* **Code reorganisation pass — naming uniformity, dead code removal, menu-item consolidation, manifest split.** No user-visible behaviour change; the dialog still looks exactly like v2.71. The aim was to make the codebase navigable enough that the next refactor doesn't hit "where is this defined" friction.
* **`WiiuTitleKey` → `WiiuTitleKeys`** (plural, to match `WiiTitleKeys`). File renamed via `git mv`; 2 call-sites in `WiiuStaging.cs` updated.
* **Five `*LocalPkgIndexerMenuItem` / `*LocalRomIndexerMenuItem` files collapsed onto a shared abstract base.** New `LocalIndexerMenuItemBase` in `Plugin/` holds the entire `IGameMenuItemPlugin` scaffolding (Caption / IconImage / SupportsMultipleGames / GetIsValidForGame / OnSelected / try-catch around the window open). Each concrete `Ps3LocalPkgIndexerMenuItem`, `PspLocalPkgIndexerMenuItem`, `PsvLocalPkgIndexerMenuItem`, `WiiuLocalRomIndexerMenuItem`, `Ctr3dsLocalRomIndexerMenuItem` is now ~10 lines declaring only its `Platform`, `PlatformLabel`, and `IsValidPlatform` predicate. Saves ~200 LOC of identical scaffolding and means a bug found in one of them is fixed in all five at once.
* **Wii U / 3DS menu items: `Config.MatchesWiiuPlatform()` / `Config.MatchesCtr3dsPlatform()` instead of hand-rolled `g.Platform.IndexOf("Wii U")` / `IndexOf("3DS")`.** The Sony three already used the Config predicates; the Nintendo two were the outliers. Now all five route platform-matching through Config so the user's CSV platform-list takes effect everywhere.
* **Dead code removal in `NewConfigWindow.cs` (~200 LOC dropped).** Three orphaned methods from prior refactor passes were sitting around:
  * `AddPackagingSubNodes()` — v2.62 version of the Packaging tree, superseded by v2.65 which builds its own sub-nodes in `RebuildPackagingV265`.
  * `CompactifyPathPickersV264` + `CompactifyPathPickersIn` + `PathTrio` struct + `ShortenPathLabel` — v2.64's pixel-shift compactor, never called since v2.65 introduced proper layout containers.
  * `mPathTip` + `GetOrCreateToolTip()` — used only by `ShortenPathLabel`, gone with it.
  * `OpenPathInExplorer` retained: still wired from the `↗` button in v2.65's `AddPath` helper.
* **`LocalPkgIndexer.cs` split: model classes extracted to `LocalPkgManifest.cs`.** The 1,040-line monolith was doing data-model declaration + directory scanning + PKG header parsing + JSON I/O + manifest orchestration in one file. v2.72 lifts the four DTOs (`LocalPkgEntry`, `LocalPkgTitle`, `LocalPkgManifest`, `LocalPkgScanProgress`) and the `LocalPkgPlatform` enum into their own file alongside the JSON schema documentation. `LocalPkgIndexer.cs` shrinks by ~80 LOC and now reads as orchestration-only.
* **Deferred to a dedicated future version:** the `ArchiveStaging<TParser, TWriter>` base-class refactor that would consolidate Ps3PkgStaging / PspPkgStaging / PsvPkgStaging / WadStaging / WiiuStaging / CiaStaging / Ctr3dsStaging (~2,700 LOC) onto a shared pipeline. It's a high-payoff cleanup but a regression there means games stop launching on the affected platform, so it warrants per-platform manual testing rather than being bundled into a "cleanup" version.

## v2.71 (Kyuss82 fork)
* **Packaging tabs: body text is no longer bold; banner gets the contrast colour.** Two issues from screen.jpg vs screen2.jpg:
  * **All body text rendered bold** — labels, checkboxes, textbox content, the platform `CheckedListBox` items, everything. Cause: `NewSection` creates the `GroupBox` with `Font = "Microsoft Sans Serif", 9pt, Bold` (to make the title bold like the Designer-positioned `packagingXxxSectionLabel`s). Any child Label / CheckBox / TextBox / CheckedListBox that doesn't set Font *itself* inherits the Bold from its parent. My new Labels didn't set Font; the rehomed Designer CheckBoxes / TextBoxes / CheckedListBoxes likewise never set Font in the Designer — they used to inherit from `tabPackagingSony` (Regular), but after the v2.65 rehome their parent became a Bold GroupBox.
  * **Fix:** added `kBodyFont = "Microsoft Sans Serif" 8.25pt Regular` and every `Add*` helper now sets `control.Font = kBodyFont` on every Label it creates *and* on every rehomed child (TextBox, CheckBox, CheckedListBox, Browse Button, `↗` Button). Only the GroupBox header text stays bold.
  * **Banner colour was wrong** — looked like the rest of the page, but the Designer's `label1` ("Cache Settings" banner) has `BackColor = SystemColors.ControlLight`, which is then bumped to `backColor` (darker than the TabPage's `backColorContrast1`) by `ApplyTheme`'s Label branch (`label.BackColor != label.Parent.BackColor` triggers the swap). My banner had the default BackColor → matched the parent → ApplyTheme didn't bump it.
  * **Fix:** the banner Label now explicitly sets `BackColor = SystemColors.ControlLight` at construction. ApplyTheme then promotes it to `backColor`, giving the same darker-than-page contrast as the rest of the tab banners.

## v2.70 (Kyuss82 fork)
* **Packaging tabs: banner is centred + reads the `&` literal.** v2.69 added the page banner but it landed misaligned right of centre and the `&` in *"packaging & on-launch updates"* disappeared. Two bugs from how I'd set the Label up:
  * `Anchor = Top | Left | Right` with `Width = 720 − 16` (set at construction) computed a *negative* right-margin against the still-tiny parent Panel (`Panel.ClientSize.Width` is ~200 px before layout), so when the form grew the Label kept that negative margin and extended past the right edge — `TextAlign.MiddleCenter` then centred the text at the off-screen midpoint.
  * `Label.Text = "PSP — packaging & on-launch updates"` interpreted the `&` as a mnemonic marker and ate it. `&&` would have escaped, but `UseMnemonic = false` is cleaner — disables the feature entirely on that one Label.
* **Fix: 2-row `TableLayoutPanel` for the outer tab layout.** The Designer's `tabPlatform*` content now holds a single `TableLayoutPanel { Dock = Fill, RowCount = 2 }` with row 0 fixed at 51 px (banner row) and row 1 at `Percent 100` (scrollable content). The banner Label dock-fills its row (so it always matches the parent width without needing `Anchor` math). The GroupBox stack lives inside a `Panel { Dock = Fill, AutoScroll = true }` in row 1. Outer-TLP was rejected for *inner* layout in v2.66 because of the chicken-and-egg with AutoSize + Percent; here it's safe because the TLP is `Fill` inside a TabPage that already has a real width when WinForms measures.

## v2.69 (Kyuss82 fork)
* **Packaging tabs: page-banner header + font inheritance.** screen.jpg vs screen2.jpg comparison: the Cache / Extraction / Smart Extract / Plugin Settings tabs all open with a centred banner Label at the top ("Cache Settings" / "Extraction Settings" / …) — the Designer's `label1` / `label2` / `label3` / `label4` (Microsoft Sans Serif 9.75 pt, centred, ~43 px tall, `FlatStyle.Flat`, `BackColor = ControlLight` re-themed by `ApplyTheme`). The Packaging sub-tabs jumped straight into GroupBoxes, looking off-style from the rest of the dialog.
  * `NewPlatformTab` now prepends a banner Label cloning that styling (`"<platform> — packaging & on-launch updates"`), then stacks the GroupBoxes below it.
  * `AddPath` / `AddText` / `AddPlatformList` no longer set `Font = "Microsoft Sans Serif", 8.25f` explicitly on the row Labels — they inherit `Font` from the GroupBox, which inherits from the Form. This makes the metrics match the rest of the dialog (Form's default font is the Designer's `Form.Font` which falls through Windows' standard cascade) and also follows DPI scaling correctly.
* Why the rest of the dialog looked subtly different from screen.jpg even though both used "Microsoft Sans Serif": explicit `new Font(...)` calls create a Font scoped to the local control and don't pick up the parent's DPI-scaled metrics. Letting inheritance work means the same Font instance ripples down the visual tree.

## v2.68 (Kyuss82 fork)
* **Packaging area: theming holes filled + Browse buttons read "Browse…" again.** v2.67 fixed the *missing text*; v2.68 fixes the *wrong colours* (the Packaging pages came out with white GroupBoxes and white CheckedListBox backgrounds — completely off-style from Plugin Settings / Cache Settings).
  * **`UserInterface.ApplyTheme` did not handle `GroupBox`, `CheckBox`, or `CheckedListBox`.** Default `BackColor = SystemColors.Control` rendered them white inside the dark-themed dialog. Added three new branches: `GroupBox` inherits `Parent.BackColor` (so the box blends into the page), `CheckBox` same, `CheckedListBox` gets `ForeColor + GetBackgroundColor(this)` and `BorderStyle.FixedSingle`. Note: `CheckedListBox.DrawMode` is locked at `Normal` (throws if set to `OwnerDraw`) — that's why the existing `ListBox` branch couldn't be reused as-is.
  * **Browse buttons rendered as "Bro" because the Designer set an `Image = folder_horizontal_open` + `TextImageRelation = ImageBeforeText`.** The folder icon is ~28 px wide and at the new 64-px button width the text was clipped to two-three letters. `AddPath` now sets `browse.Image = null` and `browse.TextImageRelation = Overlay`, so the explicit `Size(64, 23)` fits "Browse…" cleanly. The OK/Cancel buttons at the bottom of the form keep their icons (separate Designer-positioned buttons, not touched by this code path).

## v2.67 (Kyuss82 fork)
* **Packaging area: labels visible + Browse buttons full-width.** v2.66 fixed the GroupBox rendering but two issues remained, both reported via screenshot:
  * **All label text was invisible** ("Menu platforms:", "Output folder:", "RPCS3 exdata:", "PS3 .dkey folder:"). Reason: `UserInterface.ApplyTheme(this)` runs at the top of the constructor, but `RebuildPackagingV265()` (which creates new `Label` instances) runs after it. New Labels keep WinForms' default `ForeColor = SystemColors.ControlText` (black) on a dark theme background → invisible. Same applied to the *rehomed* `CheckBox` controls: pulling them out of the original `tabPackagingSony` parent didn't strip their themed ForeColor, but the BackColor inheritance through the new GroupBox parent confused them in some cases.
  * **`Browse…` buttons rendered as "Bro"** (~30 px wide instead of 64 px). The Designer-generated `Button` instances have `AutoSize = true`, which silently overrides any later `Size` assignment and re-sizes the button to fit its current text — and at theme-default font metrics that comes out to ~30 px.
* **Fix is two extra lines.** `UserInterface.ApplyTheme(this)` is now re-invoked after `RebuildPackagingV265()` so the new Labels / Buttons / rehomed CheckBoxes get themed colors. `browse.AutoSize = false` is set *before* `browse.Size = new Size(64, 23)` inside `AddPath` so the explicit size sticks. Layout itself unchanged from v2.66.

## v2.66 (Kyuss82 fork)
* **Packaging tabs now actually render (fixes v2.65).** User reported the Packaging area was empty — only narrow ~10 px columns of vertical title characters were visible (`P\nS\n3\n(\n.\np\nk\ng\n)`). Root cause: the combination of `FlowLayoutPanel` + `GroupBox.AutoSize = GrowAndShrink` + inner `TableLayoutPanel` with a `ColumnStyle(SizeType.Percent, 100f)` second column tries to measure during the first layout pass, when `FlowLayoutPanel.ClientSize.Width` is still 0; the Percent column collapses to 0, GroupBox shrinks to fit, content disappears. By the time the form is shown and the parent has a real width, the layout has already committed to the tiny size.
* **Fix: replace the layout containers with manual-Y placement + `Anchor` flags.** Each platform tab is now a `Panel { AutoScroll = true, Dock = Fill }` and GroupBoxes are added with explicit `Location` (sequential Y) and `Anchor = Top | Left | Right`, with an explicit initial `Width = 720`. Inside each GroupBox, rows are added by hand at known offsets — labels at `(12, y+4)` size `(160, 20)`, text-box content stretches between label and the right-edge buttons via `Anchor = Top | Left | Right`, `Browse…` and `↗` anchor `Top | Right` so they hug the right edge through resize. GroupBox `Height` grows as rows are appended (`Tag` carries `NextRowTop`). No `AutoSize`, no `Percent` column, nothing that needs the parent to have a real width before measuring.
* **Same layout shape as v2.65**, just a deterministic rendering pipeline:
  ```
  ┌─ PS3 (.pkg) ──────────────────────────────────────────┐
  │ Menu platforms:    [☑ PS3  ☐ …]                       │
  │ Output folder:     [D:\PS3-out      ] [Browse…] [↗]   │
  │ RPCS3 exdata:      [D:\rpcs3\…      ] [Browse…] [↗]   │
  │ ☐ Add staged PS3 install to LaunchBox library         │
  │ ☐ Auto-install to RPCS3                               │
  └───────────────────────────────────────────────────────┘
  ```

## v2.65 (Kyuss82 fork)
* **Packaging area: full rewrite with `FlowLayoutPanel` + `GroupBox` per section.** v2.64's pixel-shift compactor sometimes left controls overlapping when a tab had heterogeneous spacing between section headers, multi-select boxes and path trios — section labels stacked on top of path rows, checkboxes leaked into the row above, the new `↗` buttons overlapped the `Browse…` buttons. v2.65 throws the absolute-positioned Sony / Nintendo sub-tabs away and rebuilds the area from scratch:
  * 7 new platform sub-tabs (PS3, PSP, PSV, Wii, Wii U, 3DS, DSi) replace the 2 region sub-tabs.
  * Each platform sub-tab is a `FlowLayoutPanel` (TopDown, no wrap, AutoScroll) stacking one `GroupBox` per logical section: PS3 has packager + auto-update + ISO, PSP/PSV have packager + auto-update, 3DS has CIA + Decrypt, the rest have one section each.
  * Each `GroupBox` hosts a `TableLayoutPanel` with two columns (`170 px` label + percent-fill content). Path rows nest a `Panel` with `↗`/`Browse…` docked right and the path `TextBox` filling the rest, so no more pixel math.
  * `Resize` handler on each `FlowLayoutPanel` widens every child `GroupBox` to fit the current width minus the vertical scrollbar.
* **Controls are rehomed, not duplicated.** Every existing `TextBox` / `CheckBox` / `Button` / `CheckedListBox` declared in the Designer file gets `Parent = newCell` (`Anchor` / `Location` cleared, `Dock` set) — all the event handlers wired in `InitializeComponent` and in the constructor stay bound, all `Config.*` bindings keep working unchanged. The Designer file itself is untouched.
* **"Local Mirror Folders" is now a top-level TreeView node + its own tab.** The five per-platform "Manage … Local PKG Folders…" buttons that used to be scattered across Sony/Nintendo are hidden. The new node opens a tab with an intro paragraph and a single "Open Local Mirror Indexer…" button that launches the unified hub (which has had its own per-platform tabs since v2.61). `treeView1_AfterSelect` now reads `e.Node.Tag` to find the target `TabPage` for top-level nodes that aren't backed by a `tabControl1` index — same plumbing used for the Packaging children, which switched from "scroll to section anchor inside a giant sub-tab" to "select the platform's own sub-tab" (`Anchor = null` is fine — `ScrollControlIntoView` is skipped when the anchor is missing).
* **`CompactifyPathPickersV264` is no longer called.** The new layout uses proper layout containers, so the pixel-shift compactor is superseded. The method is kept in the source as dead code for one release in case we need to roll back.

## v2.64 (Kyuss82 fork)
* **Path-picker compaction in `NewConfigWindow`.** Each path setting in the Sony / Nintendo packaging tabs used to be a three-control trio rendered across two rows: an `AutoSize` `Label` above, a wide `TextBox` below and a `Browse…` button to the right (~46 px tall). With 22 trios across the two tabs, that was ~500 px of vertical real estate just for the path widgets. `CompactifyPathPickersV264` collapses each trio onto a single row (~22 px) and shifts every control below upward by the cumulative saved height so the page actually gets shorter.
* **How it works.** Naming convention `<prefix>BrowseButton` / `<prefix>Path` / `<prefix>PathLabel` is exploited: each `Button` in the tab whose name ends in `BrowseButton` is paired with its sibling `TextBox` and `Label` to form a `PathTrio`. Trios are sorted by `OrigTop`, new `Top` values are computed by accumulating saved heights, every non-trio control gets `c.Top -= sum-of-prior-savings`, and the trio itself is rebuilt inline: label (with short text + tooltip for the long original), text box, `Browse…` button (now `64 px` wide), and a new `↗` button that opens the current path in Explorer (Directory → open, File → `/select,`).
* **Labels get terser.** `ShortenPathLabel` cuts at the first ` — ` or ` (` (so "*Menu output folder (empty = next to source):*" → "*Output folder:*", "*Common Key — shared with auto-cache (32 hex characters):*" → "*Common Key:*"). The full original phrasing is preserved as a `ToolTip` on the label, so hovering still tells you the caveats.
* **Designer.cs left untouched.** All the rework is in `NewConfigWindow.cs` (the partial-class file); the auto-generated Designer keeps round-tripping cleanly. Any future control added with the same naming convention is automatically picked up by the next run of `CompactifyPathPickersV264`.

## v2.63 (Kyuss82 fork)
* **Unified Bulk Operations hub.** Until v2.62 there were four right-click menu items that each opened a separate Form: *PS3 Update Builder*, *PSP Update Builder*, *PS3 Raw PKG Export*, *PSV VPK Builder*. The first two were even the same class (`Ps3BulkUpdateBuilderWindow`) instantiated with a different `SonyUpdateContext`. The four windows all looked subtly different and switching from "build PS3 updates" to "raw export the same selection" meant closing one Form, finding the other menu item, and re-selecting the games. New `BulkOperationsWindow` is a single Form with one `TabControl` and four tabs.
* **Implementation note: no logic was duplicated.** The hub embeds each existing Form as a `TopLevel = false` child inside its TabPage (`FormBorderStyle = None`, `Dock = Fill`, then `Show()`). The four backends — `Ps3BulkUpdateBuilderWindow`, `Ps3BulkUpdateBuilderWindow(SonyUpdateContext.ForPsp(), …)`, `Ps3BulkPkgExportWindow`, `PsvBulkVpkBuilderWindow` — are kept verbatim; the hub is ~80 lines of plumbing. Each child's existing [Close] button still works: we listen to `FormClosed` on every embedded child and `Close()` the hub in response, so the user sees the familiar Close → window goes away. On hub close we dispose every embedded child so their `CancellationTokenSource`s and any in-flight tasks get cleaned up.
* **Menu items rewired.** The four entry points (`Ps3BulkUpdateBuilderMenuItem`, `PspBulkUpdateBuilderMenuItem`, `Ps3BulkPkgExportMenuItem`, `PsvBulkVpkBuilderMenuItem`) now construct `BulkOperationsWindow(<initialTab>, selectedGames)` instead of their respective per-platform Form. The initial tab argument decides which TabPage is selected when the hub opens, so right-clicking *Build PS3 Updates…* still lands on the PS3-Update tab — just with the other three workflows one click away.

## v2.62 (Kyuss82 fork)
* **Settings dialog navigation rework + cross-page search.** Two issues with the old `NewConfigWindow`: the left-side `TreeView` and the top `TabControl` showed the *same five* sections (Cache / Extraction / Smart / Plugin / Packaging) at the same time, and the *Packaging* tab buried 12 platform sections (Wii / Wii U / 3DS CIA / 3DS Decrypt / DSi / PS3 / PS3 Auto-Update / PS3 ISO / PSP / PSP Auto-Update / PSV / PSV Auto-Update) under two further sub-tabs that you had to scroll through.
  * The main `tabControl1` and `packagingSubTabs` headers are now hidden (`TabAppearance.FlatButtons` + `ItemSize = (0,1)`). The `TreeView` is the only navigator.
  * The Packaging node now has 12 children — each child has a `Tag = PackagingSubNav { SubTab, Anchor }`. Selecting a child node switches the main tab to Packaging, sets the right sub-tab (Sony / Nintendo), and calls `ScrollControlIntoView(Anchor)` on the matching section label so the section header lands at the top. A 1.2 s yellow flash on the anchor draws the eye after the jump.
  * `treeView1_AfterSelect` was extended to distinguish the three cases: top-level node (old `tabControl1.SelectTab(node.Index)` path), Packaging child (sub-nav lookup), anything else (no-op).
* **Search box above the navigator.** `BuildSearchIndex` walks every `Label / CheckBox / RadioButton / GroupBox / Button` inside every tab page (recursively, including the Sony/Nintendo sub-tabs) and records `(Control, Text, MainTab, SubTab)`. Typing in the box filters the index case-insensitively and shows up to 30 hits in a floating `ListBox`; each row is formatted as `[Tab › SubTab] <label text>`. Clicking (or pressing Enter on) a hit jumps to that control: walks up the parent chain to identify the right tab + sub-tab, switches both, calls `ScrollControlIntoView`, and flashes the control yellow for 1.2 s. `Esc` clears the search; `Down` from the search box moves focus to the results list.
* **Designer file untouched.** All changes are in `NewConfigWindow.cs` (the partial-class `.cs`, not the auto-generated `.Designer.cs`) so the IDE WinForms designer keeps round-tripping cleanly and a future Designer regeneration won't undo the rework.

## v2.61 (Kyuss82 fork)
* **Local Mirror Indexer GUI rewrite — one window, five tabs, real progress, stop button, log filter + colours, manifest browser, drag-drop on the folders list.** Previously there were five separate windows (one per platform) opened from the right-click menus and Settings buttons — managing the mirror across PS3/PSP/PSV/WiiU/3DS meant juggling five Forms. The new `LocalPkgIndexerWindow` is a single hub with one `TabPage` per platform; each tab keeps its own folder list, log, manifest viewer and scan lifecycle. The old constructor `new LocalPkgIndexerWindow(LocalPkgPlatform.Ps3)` still works — it just selects that tab. The five right-click menu items and the five "Manage…" buttons in Settings all open the same hub with the matching tab active.
* **Folders are now a `ListView`** with columns `Path / Status / Files` (live file count via `LocalPkgIndexer.CountFiles`), drag-drop from Explorer (drop a folder onto the list — no need to click *Add…*), a new *Save list* button so you can update the list without re-running the scan, and an *Open in Explorer* button to inspect what's actually in there. Rows for missing folders show `missing` in red.
* **Real progress bar.** `LocalPkgIndexer.CountFiles(platform, folders)` now does a fast pre-pass (just `Directory.EnumerateFiles` with no parsing) and returns the file count. The window uses that as `mProgress.Maximum`, and the per-tick callback computes `(seen / total) * 100`. Marquee-only behaviour replaced. Pre-count runs on a worker so the UI doesn't freeze when scanning a slow network share.
* **Stop button.** `LocalPkgIndexer.BuildIndex` now takes `CancellationToken cancellationToken = default` and throws at the next iteration of any inner loop when cancelled. The new *Stop* button in the window cancels the active token; the window catches `OperationCanceledException`, logs `[error] cancelled by user`, and re-enables the controls. Closing the form also cancels any running scans.
* **Log is a colour-coded `ListView`** with `Type / Source / Detail` columns. Four filter checkboxes (`idx` / `skip` / `error` / `seen`) toggle visibility of each kind in real time (the underlying `mLogStore` keeps everything; `RefreshLogView` rebuilds the visible rows from the filter set). Right-click any row → *Copy line* or *Reveal in Explorer* (parses `<zip>!<entry>` syntax and selects the wrapper file). Colours: idx green, skip grey, error red, seen dim blue.
* **Manifest viewer.** A `SplitContainer` puts the log on the left and a `TreeView` on the right — each platform tab lazy-loads its persisted JSON when first activated and shows one root node per title id, expandable to `Update — <file> [size] <content_id>` / `DLC — …` children. Double-clicking an entry reveals the source file/zip in Explorer. Re-runs after a scan rebuild the tree from the new manifest.
* **`Scan ALL platforms`** button at the bottom of the window runs each tab's scan in sequence so you can refresh the entire offline mirror in one click. Stop cancels the current platform and breaks the sequence.

## v2.60 (Kyuss82 fork)
* **3DS update categoriser fixed — `0x0004000E` is the update bucket, not `0x00040020`** — the v2.49 switch in `AddCtr3dsEntry` mapped `0x00040020` to `Update`, but per [3dbrew](https://www.3dbrew.org/wiki/Titles) `0x00040020` is *SystemAutoUpdateContent* (Nintendo's CDN system-update bucket). Retail 3DS game updates (the CIA patches No-Intro mirrors under `(Update).zip`) all use `title_id_high = 0x0004000E`. The user's Z:\emulator\No-Intro\up Culdcept / OlliOlli / Q / Terraria all parsed fine but `AddCtr3dsEntry` rejected them silently via the `default → Skip` branch. New switch covers `0x0004000E` (Update), `0x0004008C` (DLC), and explicitly enumerates `0x00040000/0x00040001/0x00040002/0x00040003/0x00040010/0x00040020` → Skip with a descriptive reason. Wii U switch likewise extended with `0x00050002` (demo) and `0x00050010` (system).
* **`AddWiiuEntry` / `AddCtr3dsEntry` now emit `[skip]` / `[idx]` per decision** — previously both methods just bumped counters and returned, so a scan that skipped everything looked like `[done] 0 titles indexed` with no clue why. They now take the `Reporter` callback and emit:
  * `[idx]  <path> — <plat> title_id=0xNNNNNNNN_NNNNNNNN → base 0xXXXXXXXXXXXXXXXX UPDATE|DLC`
  * `[skip] <path> — <plat> title_id=0xNNNNNNNN_NNNNNNNN (<reason>)`
* The same diagnostic gap was responsible for misclassified entries vanishing without a trace in prior versions — now any title_id_high outside the recognised set gets `unknown title_id_high` in the log so it can be triaged. Re-scan Z:\ and Culdcept / OlliOlli / Q / Terraria should appear as `[idx] … UPDATE`.

## v2.59 (Kyuss82 fork)
* **The Wii U / 3DS zip-wrapper scan finally stops printing `[error] <zip>!tmd.NN: Could not find file '<zip>!tmd.NN'`** — the real bug was *not* the shell-virtual-path theory chased in v2.58. It was inside `AddWiiuEntry` / `AddCtr3dsEntry`: those build a `LocalPkgEntry` and set `Size = new FileInfo(sourcePath).Length`. For zip-wrapped entries we pass `sourcePath = <zip>!<entry>` (a synthetic identifier, **not** a real filesystem path), so `FileInfo.Length` throws `FileNotFoundException("Could not find file '<zip>!tmd.16'")` and the catch upstream prints the noisy `[error]`. The .tmd parse itself succeeded — every Wii U update zip would have been indexed correctly if not for the size lookup that came right after.
* New `SafeFileLength` wrapper plus an explicit `sizeBytes` parameter on both `Add*Entry` methods. Bare-file callers compute it via `SafeFileLength` (falls back to 0 on any I/O exception); zip-wrapper callers pass `ZipArchiveEntry.Length` directly, which is the uncompressed entry size from the central directory — no filesystem touch needed.
* Confirmed by re-reading the user's v2.58 scan log: 62 .zip wrappers were enumerated, all 62 produced `[error] <zip>!tmd.NN`, none reached `[idx]`. The v2.58 `File.Exists` gate on the bare-TMD loop was a no-op because that loop never picked up the synthetic paths in the first place (`Path.GetFileName("<zip>!tmd.16") == "<zip-name>.zip!tmd.16"`, which doesn't match `IsWiiuTmdFile`'s `tmd.*` rule).

## v2.58 (Kyuss82 fork)
* **`File.Exists` gate in front of `WiiuTmd.Parse`** — some shell virtual filesystems (and certain network shares with custom Explorer extensions) expose `.zip` contents as pseudo-paths like `<zip>\tmd.32` or `<zip>!tmd.32`. `Directory.EnumerateFiles` returns those as if they were real files; `IsWiiuTmdFile` then accepts them (the filename `tmd.32` matches the `tmd.*` rule) and `WiiuTmd.Parse` → `File.ReadAllBytes` ends up with `Could not find file '...'` because the file system can't actually open them. Now the Wii U + 3DS folder loops call `File.Exists(path)` first and skip with a clear `[skip] … virtual / inaccessible path` line otherwise, leaving the explicit `.zip` wrapper scanner below to do its job.
* Fixes the *Disney Infinity (Europe) (Update).zip!tmd.32* false error.

## v2.57 (Kyuss82 fork)
* **3DS scan accepts raw TMD (NUS-dump layout) in addition to `.cia`** — the v2.49 scanner only walked `.cia` files for 3DS. Now `ScanCtr3ds` also picks up `tmd` / `tmd.<region>` / `<name>.tmd` files (same `IsWiiuTmdFile` rule the Wii U scanner uses — TMD on-disk format is identical across Wii U and 3DS, only the `title_id_high` namespace differs).
* The 3DS `.zip` wrapper scanner now also walks for raw TMD entries inside (Wii U-style NUS dump packed as a zip) and parses them with `WiiuTmd.Parse`. Wrappers that contain neither a `.cia` nor a TMD entry now emit `[skip] <archive> — no .cia or TMD entry inside`.

## v2.56 (Kyuss82 fork)
* **Wii U TMD detection now accepts NUS-dump naming** — the previous scan filtered files via the `*.tmd` glob, which catches Loadiine layouts (`title.tmd`) but **not** NUS dumps where the TMD is named `tmd.16` / `tmd.32` / etc. (the suffix is the region/locale code). Wii U scene `.zip` packs that ship as NUS dumps (file numbered contents `00000000`, `00000001`, … plus `tmd.16`, `tmd.32`, `cetk.16`, `cetk.32`) were therefore silently skipped. New `IsWiiuTmdFile` accepts:
  * `<anything>.tmd` (Loadiine)
  * `tmd.<anything>` (NUS dump — `tmd.16`, `tmd.32`, `tmd.eu`, …)
  * bare `tmd` (cdecrypt output)
* Both the folder walker and the `.zip` wrapper scanner now use this rule. `.zip` wrappers that contain no TMD entry emit a clear `[skip]` log line so it's obvious why they were ignored.
* Fixes the *3Souls (Europe) (En,De,Es) (Update).zip* misclassification.

## v2.55 (Kyuss82 fork)
* **`SonyUpdateFileRegex` now matches PSV update PKGs** — PSV update filenames embed a 40-char hex SHA1 between the SDK version and the `-PE` suffix:
  * PS3: `<contentid>-A0102-V0101-PE.pkg`
  * PSV: `<contentid>-A0102-V0101-c95f72f920ed6b2e3c02f88a9a8fa3020f8adc44-PE.pkg`
  The previous regex `-A\d{4}-V\d{4}-PE\.pkg$` only caught the PS3 shape so PSV updates fell through `Categorise` and got skipped. Updated to `-A\d{4}-V\d{4}(-[0-9a-fA-F]{40})?-PE\.pkg$` which catches both. Fixes the *1001 Spikes (USA) (v1.02) (Update).zip* misclassification.

## v2.54 (Kyuss82 fork)
* **PS3 categoriser swap: `content_type=0x04` is DLC, not Update** — v2.48 mistakenly mapped `0x04 → Update`, but on PS3 that value means "add-on content" (= DLC) on Sony's side (NoPayStation distributes all `0x04` entries under `PS3_DLCS.tsv`). The actual update-vs-base discriminator is the **filename / content_id** pattern: an update PKG either contains `PATCH` in the content id or matches the Sony CDN naming `<contentid>-A<aver>-V<sdkver>-PE.pkg`. Updates inherit the game's content_type (so a patch for a 0x05 base game is also 0x05) — there is no dedicated content_type for them. New logic:
  * Filename `-A####-V####-PE.pkg` or content_id contains `PATCH` → **UPDATE** (regardless of content_type).
  * Otherwise: `0x04` or `0x0F` → **DLC**, `0x05`/`0x09`/`0x0A` (base/theme/avatar) → **SKIP**, anything else → **SKIP**.
* Fixes the *Agarest - Generations of War - Ellis Set (Europe) (DLC).zip* misclassification: that PKG is content_type=0x04 with a scrambled NPS CDN filename and no `PATCH` marker — now correctly tagged as DLC.

## v2.53 (Kyuss82 fork)
* **Sony PS3 update PKGs with `content_type=0x05` now classified as updates** — Sony's PSN ships some PS3 updates under content_type 0x05 (= "PS3 game") with no `PATCH` keyword in the content id, distinguishable from a base game only by the `-A<aaaa>-V<vvvv>-PE.pkg` filename suffix (app-version + SDK-version, e.g. `JP0082-NPJB00665_00-CODAW00000000000-A0120-V0100-PE.pkg`). `LocalPkgIndexer.Categorise` now consults the filename via `Regex(@"-A\d{4}-V\d{4}-PE\.pkg$")` as a fallback whenever `content_type=0x05`, and the same heuristic is applied to the PSP / PSV "PATCH content-id" path too.
* `[skip]` log lines now include the content_id and the source filename when the classifier rejects an entry, so it's obvious at a glance what failed the heuristic.

## v2.52 (Kyuss82 fork)
* **Indexer scan log now reports each decision per-file** — `[seen]` is emitted once per visited source, then for every `.pkg` inside (or the bare `.pkg`) the indexer prints exactly one of:
  * `[idx]   <label> → <TID> UPDATE (content_type=0xNN+rap)`
  * `[idx]   <label> → <TID> DLC    (content_type=0xNN+rap)`
  * `[skip]  <label> — pkg_type=… doesn't match platform`
  * `[skip]  <label> — couldn't derive TITLE_ID from content_id …`
  * `[skip]  <label> — content_type=… categorised as base/theme/avatar`
  * `[skip]  <archive> — .7z/.rar wrapper not supported (only .zip)`
  * `[skip]  <archive> — no .pkg entries inside`
  * `[error] <label>: <exception message>`
* `LocalPkgScanProgress.LogLine` is the channel — the worker thread sets it before calling the UI reporter callback; `LocalPkgIndexerWindow.DoScan` snapshots the value before the BeginInvoke marshals to the UI thread (the `prog` instance is reused on every tick).

## v2.51 (Kyuss82 fork)
* **`LocalPkgIndexer` now reads `.zip` wrappers** — the user's offline mirror typically ships PS3/PSP/PSV update + DLC as `<scene-name>.zip` containing one `.pkg` (and optional `.rap` for DLC). v2.48..v2.50 only looked for bare `.pkg` so those packs were silently invisible. Now:
  * Every `.zip` in the configured folders is opened with `System.IO.Compression.ZipArchive` (no extraction to disk during scan), each `.pkg` entry is streamed into a 256 KB memory snapshot, and `PsPkgReader.Parse(skipItemTable: true)` reads enough header+metadata to categorise it.
  * Sibling `.rap` entries inside the same zip are matched to their `.pkg` by content-id and recorded on the manifest entry (`RapArchiveEntry`).
  * The manifest stores `ArchivePath` + `ArchiveEntry` + `RapArchiveEntry` alongside the existing `PkgPath` / `RapPath`. New shared helper `LocalManifestResolver` materialises a real `.pkg` file from either source on demand for the installers.
* **Wii U / 3DS scanners** also pick up `.zip` wrappers (looking for `.tmd` inside for Wii U Loadiine dumps, `.cia` for 3DS).
* **`PsPkgReader.Parse`** gained an optional `skipItemTable` overload so the indexer doesn't pay the AES-CTR cost on the item table when it only needs the header — important when many .pkg files are visited per scan.
* **`Ps3UpdateInstaller` + `Ps3DlcInstaller`** now route through `LocalManifestResolver.ResolvePkgPath`, transparently extracting the .pkg from a wrapper zip to a temp folder at install time and cleaning up after. RAP staging similarly reads from the zip via `LocalManifestResolver.ResolveRapBytes` when no bare `.rap` is present.
* `.7z` / `.rar` wrappers are detected but skipped with a log line — `System.IO.Compression` doesn't speak those formats; repack as `.zip` or extract first.

## v2.50 (Kyuss82 fork)
* **Local mirror folder management surfaced in *Packaging → Sony* and *Packaging → Nintendo*** — the v2.48/v2.49 right-click "Index Local … Folders..." menus were the only place to configure the offline mirror paths, so users opening *Tools → Manage → ACM Settings* found no UI hook at all. Added five buttons that open the same `LocalPkgIndexerWindow` with the matching platform pre-selected:
  * Sony sub-tab: "Manage PS3 / PSP / PSV Local PKG Folders…" next to each platform's existing config block.
  * Nintendo sub-tab: "Manage Wii U / 3DS Local ROM Folders…" placed alongside the Wii U and 3DS sections respectively.
* The right-click context menus are unchanged — both entry points share the same Add / Remove / Scan + Save UI inside `LocalPkgIndexerWindow`.

## v2.49 (Kyuss82 fork)
* **Local ROM indexer extended to Wii U + 3DS** — `LocalPkgIndexer` now understands two more platforms beyond the PSx family:
  * **Wii U** (`LocalPkgPlatform.Wiiu`, config `WiiuLocalRomFolders`) — scans the configured folders for `.tmd` files (Loadiine layout) and `.wua` archives (Cemu zarchive format, name parsed for `<TitleId16>_v<ver>.wua`). `WiiuTmd.Parse` extracts the title id; `title_id_high` decides the category:
    * `0x00050000` → base game (skipped — already in the LB library)
    * `0x0005000E` → update
    * `0x0005000C` → DLC
  * **3DS** (`LocalPkgPlatform.Ctr3ds`, config `Ctr3dsLocalRomFolders`) — scans the configured folders for `.cia` files. The CIA header (archive_header / cert / ticket sizes, 64-byte aligned) is walked in-stream to locate the TMD section; title id and version are read from TMD offsets `0x18C` / `0x1DC`. `title_id_high` decides the category:
    * `0x00040000` → base (skipped)
    * `0x00040020` → update
    * `0x0004008C` → DLC
* Manifests are bucketed by the **base** title id so the auto-install path (next iteration) can pick them up off a single library entry. Wii U entries get the standard 16-hex-digit form (e.g. `0005000E10112E00` saved under base `0005000010112E00`); 3DS entries follow the same convention.
* Two new right-click menu items — **"Index Local Wii U ROM Folders..."** and **"Index Local 3DS ROM Folders..."** — both opening the same `LocalPkgIndexerWindow` with the matching platform pre-selected. Folder lists are persisted via `Config.WiiuLocalRomFolders` / `Config.Ctr3dsLocalRomFolders`.
* Manifests are written to `<plugin>/WiiuLocalCache/local_rom_index.json` and `<plugin>/Ctr3dsLocalCache/local_rom_index.json` respectively.
* Auto-install at game launch for Wii U / 3DS is **not** wired up yet — the emulator-side storage layouts (Cemu's `mlc01/usr/title/<high>/<low>/` vs Citra/Azahar's `nand:/title/<high>/<low>/`) and the install-CIA-via-emulator vs install-on-disk choices warrant a dedicated pass. v2.50 will pick that up; today's index is enough to point users at the right files for manual install via the emulator's "Install Title" UI.

## v2.48 (Kyuss82 fork)
* **Offline local PKG index for updates + DLCs (PS3 / PSP / PSV)** — when the user maintains their own offline mirror of Sony's update + DLC catalogue, ACM can ingest that mirror as a JSON manifest and apply patches/DLC straight from disk instead of hitting Sony's CDN.
  * **`LocalPkgIndexer`** (`src/Core/Packagers/LocalPkgIndexer.cs`) recursively walks the folders in `Ps3LocalPkgFolders` / `PspLocalPkgFolders` / `PsvLocalPkgFolders` (pipe-separated, multiple folders per platform), parses each `.pkg`, categorises by content_type (`0x04`=PS3 update, `0x0F`=PS3 DLC, `0x16`=PSV DLC) with `PATCH` content-id fallbacks for PSP/PSV, and persists `<update cache>/local_pkg_index.json` keyed by Title ID. Sibling `.rap` files (NPS Browser export style) are picked up automatically.
  * **Right-click "Index Local PS3 / PSP / PSV PKG Folders..."** (3 new menu items) opens a single `LocalPkgIndexerWindow` with inline folder-list editing (Add / Remove), live per-file progress, and a "Scan + Save" button. Folder lists are persisted to `Config.{Ps3,Psp,Psv}LocalPkgFolders` on each scan.
  * **`Ps3UpdateInstaller.RunAsync`** now consults the local manifest *before* querying Sony. When the manifest has updates for the requested Title ID, ACM installs straight from disk and skips the Sony query entirely — useful for offline / LAN setups, and for users whose offline mirror is curated differently from Sony's published patch list.
  * **New `Ps3DlcInstaller`** runs after update install during the on-launch flow when `Ps3AutoInstallDlcs` / `PspAutoInstallDlcs` / `PsvAutoInstallDlcs` is on. For each DLC PKG in the manifest it decrypts + unpacks the additional content onto the install root, and stages the companion `.rap` (PS3 → `dev_hdd0/home/00000001/exdata/`, PSP → `PSP/LICENSE/`) sourced from the sibling file first, NPS DB `RapHex` column second. PSV DLC is handled by Vita3K's app-level license so no extra rap staging is needed.
  * Three new checkboxes in *Packaging → Sony* (under each platform's PKG section): "On-launch: install DLCs from local PKG index". Off by default.

## v2.47 (Kyuss82 fork)
* **Multi-select support on every bulk menu item** — right-clicking on N highlighted titles in LaunchBox now scopes the bulk action to exactly those titles, instead of always sweeping the entire library. Affected menus:
  * "Build PS3 Update DB..." (`Ps3BulkUpdateBuilderMenuItem`)
  * "Build PSP Update DB..." (`PspBulkUpdateBuilderMenuItem`)
  * "Build PSV VPKs..." (`PsvBulkVpkBuilderMenuItem`)
  * "Export PS3 PKGs (bulk)..." (`Ps3BulkPkgExportMenuItem`)
* All four `IGameMenuItemPlugin` types now declare `SupportsMultipleGames = true` and validate the selection with `GetIsValidForGames` (every highlighted title must match the platform gate). Single-game right-click still pops the same window with the full-library scan, so the existing entry point keeps working.
* Each bulk window constructor gained an optional `IGame[] preselected` parameter — when non-empty the window honours that list verbatim (no platform gate, no library scan), so the user can mix-and-match titles, queue an arbitrary subset, etc.

## v2.46 (Kyuss82 fork)
* **New on-launch fix for PS3 NPDRM: auto-mirror cache → RPCS3 `dev_hdd0/game/<TID>/`** — opt-in via new `Ps3PkgAutoInstallToRpcs3` checkbox in the *Packaging → Sony* sub-tab. When enabled, `GameLaunching.cs` hijacks `emulator.ApplicationPath` → `powershell.exe` + a new `ps3-pkg-rpcs3-launcher.ps1` wrapper script (sister to the existing PS3 ISO mount launcher). The script:
  1. Reads `TITLE_ID` out of `PARAM.SFO` next to the cache eboot (inline PSF parser, no external tools).
  2. `robocopy /MIR`s `<cache>/<baseName>/` → `<RPCS3>/dev_hdd0/game/<TID>/`. First launch costs a full copy (~30s for ~1.6 GB); subsequent launches skip files whose size + timestamp already match, so the overhead drops to near zero.
  3. Launches `rpcs3.exe <RPCS3>/dev_hdd0/game/<TID>/USRDIR/EBOOT.BIN` — the path that satisfies RPCS3's `exdata/<contentid>.rap` lookup.
* Default off because the robocopy doubles each title's on-disk footprint. For users who want zero ACM-side magic the v2.45 "Export PS3 PKG (for RPCS3 install)" menus are still the simplest path.

## v2.45 (Kyuss82 fork)
* **New "Export PS3 PKG..." workflow (NoPayStation-style)** — for NPDRM PS3 titles, RPCS3 only does the NPDRM klicensee dance when the title is installed under `dev_hdd0/game/<TID>/`. Our existing "decrypt to cache, boot eboot from there" flow produces a byte-identical eboot (md5 matches what `RPCS3 → File → Install Packages` puts on disk), but RPCS3 launched against the cache path doesn't link it to the rap in `exdata/`, and the title fails with `Game data corrupted / Cannot read SELF` (e.g. *Amy*, EP4295-NPEB00768). New flow mirrors NoPayStation Browser:
  * **Right-click "Export PS3 PKG (for RPCS3 install)..."** — `Ps3PkgRawExporter` pulls the bare `.pkg` (Sony-signed, undecrypted) and its companion `.rap` (from the source archive, or synthesised from the `NpsDb.RapHex` column if the archive has none) into `Ps3PkgOutputPath` (or the source archive's folder). User then opens RPCS3 → File → Install Packages/Raps and points it at the resulting files; RPCS3 handles NPDRM correctly.
  * **Right-click "Export PS3 PKGs (bulk)..."** — `Ps3BulkPkgExportWindow` (companion to the existing PSV bulk builder) iterates every PS3 PKG title in the library and runs the same exporter, with a "skip if .pkg already in output folder" toggle.
  * The existing "Create PS3 Package..." (decrypted-folder export) and the on-launch `Ps3PkgCacheOnLaunch` flow are left in place for non-NPDRM titles where they still work.

## v2.44 (Kyuss82 fork)
* **PKG key dispatch fix for PSP PSN-Encrypted titles** — pkg2zip routes the AES-128 data key for type-0x0002 PKGs on `pkg_header[0xE7] & 7` (the "key_type" field inside the ext_header), not on `content_type`. PSP PSN-Encrypted titles like *2010 FIFA World Cup: South Africa* (`EP0006-ULES01412`) carry `content_type=0x07` (= PSP game) but `key_type=2` (= PSV-2 wrap-and-derive). The old routing keyed on `content_type` alone, so these PKGs got the bare `pkg_psp_key` and the AES-CTR decrypt produced garbage filenames — only the rare item whose name decrypted to printable ASCII by chance got through.
* `PsPkgReader.Parse` now reads `PkgHeaderSize + PkgExtHeaderSize` (= 256 bytes) and extracts `keyType = headerBytes[0xE7] & 7`. `SelectAesKey` is rewritten to switch on `keyType`: 1 → PSP key (no wrap), 2/3/4 → PSV-N wrap-and-derive (`AES-ECB-encrypt(vita_N, pkg_data_riv)`). Bug-for-bug parity with pkg2zip's `pkg2zip.c:426..449`.

## v2.43 (Kyuss82 fork)
* **New right-click "Build PSV VPKs..." menu (bulk builder)** — analog of the existing "Build PS3/PSP Update DB..." menus. Opens `PsvBulkVpkBuilderWindow` which enumerates every game in the library whose platform matches `PsvPkgPlatform`, then for each one runs the same `PsvPkgStaging.BuildFromGameArchive` pipeline that powers the one-shot "Create PSV VPK..." menu, writing a `<baseName>.vpk` per title to the chosen output folder (default = `PsvPkgOutputPath`, or next to each source archive when empty). Cancellable, skips already-built .vpks by default, optional "add to library" toggle.
* **Cleanup** — removed `PsvUseVita3kLauncher` config flag, the `psvUseVita3kLauncherCheckBox` UI control, the `PSV Vita3K wrapper` block in `GameLaunching.cs`, and the `PsvVita3kLauncher.cs` source file. The PowerShell `Push-Location`-then-launch trick was a v2.36 dead end — Vita3K v0.2.x reads the NPDRM klicensee from `ux0/app/<TID>/sce_sys/package/work.bin` (now staged inside the .vpk in v2.42) and from `ux0/license/<TID>/<contentid>.rif` (staged in v2.41), so once the .vpk is drag-dropped onto Vita3K the title boots without any wrapper.
* **Removed `AssemblyResolver.cs`** (v2.34) — was a safety net for `SharpZipLib` transitive resolution, but v2.35 dropped the SharpZipLib dependency entirely (the zRIF inflate now uses stock `System.IO.Compression.DeflateStream` with a stored-block dictionary prepend trick), so the resolver had no remaining purpose.

## v2.42 (Kyuss82 fork)
* **NPDRM klicensee now embedded inside the `.vpk` as `sce_sys/package/work.bin`** — comparison against an NPS Browser extraction of *2013: Infected Wars* showed that pkg2zip writes the decoded RIF into `sce_sys/package/work.bin` inside the app folder (bit-identical to a standalone `<contentid>.rif`, MD5 verified). Vita3K reads the klicensee from this file at app-boot to decrypt the NPDRM `eboot.bin`. ACM was decoding the RIF and writing it to `ux0/license/<TID>/` but **not** staging `work.bin` inside the cache tree, so Vita3K's load_module → decrypt_fself ended in `Invalid SELF: still encrypted (unsupported)`. Now we write the RIF to both locations:
  * `<installRoot>/sce_sys/package/work.bin` — ends up inside the .vpk, copied by Vita3K into `ux0/app/<TID>/sce_sys/package/work.bin` on install. This is the canonical NPDRM key location.
  * `<Vita3K data>/ux0/license/<TID>/<contentid>.rif` (and the legacy `…/app/<TID>/` mirror) — kept as a fallback for builds that read from the license folder.
* `TryInstallLicenseFromNps` now runs **before** `ZipFile.CreateFromDirectory` so `work.bin` is included in the generated `.vpk`.

## v2.41 (Kyuss82 fork)
* **PSV license `.rif` path fix** — was being written to `ux0/license/app/<TID>/<contentid>.rif` (the pkg2zip output convention), but Vita3K ≥ v0.2.x looks for it in `ux0/license/<TID>/<contentid>.rif` (without the `app/` middle directory). Symptom in Vita3K log: `[W] [get_license]: License file is corrupted or missing at: "…/ux0/license/PCSB00842/…rif", using default value.` followed by `[E] [decrypt_fself]: Invalid SELF: file is either not a SELF or is still encrypted (unsupported).` — the eboot was NPDRM-locked and Vita3K had no license to decrypt it with. Now writes to the modern path; the legacy `app/` mirror is also written so older Vita3K builds keep working.

## v2.40 (Kyuss82 fork)
* **Fix `Installation failed` in Vita3K on the generated `.vpk`** — PSV PKGs list `sce_pfs` as a *file* entry alongside `sce_pfs/files.db`, `sce_pfs/unicv.db`, `sce_pfs/pflist` as separate file entries. The bare `sce_pfs` is a PKG-format directory placeholder, not a real file. Old `PsPkgUnpacker` wrote it as a file first; the three real children then failed to extract because their parent path already existed as a file (the old log line was *"cannot create parent 'sce_pfs' for 'sce_pfs/files.db' (collides with existing file). Skipping."*). The resulting `.vpk` carried a `sce_pfs` dummy file plus missing PFS metadata, and Vita3K rejected the install with a generic `Installation failed`.
* New pre-pass in `PsPkgUnpacker.Unpack` builds the set of all path prefixes that appear before a `/` in some other entry's name. Entries whose own name lands in that set are dropped as directory placeholders, so the three `sce_pfs/*` children's `Directory.CreateDirectory(parent)` succeeds and they get extracted normally. Bug-for-bug compatible with the pkg2zip / rusty-psn handling.

## v2.39 (Kyuss82 fork)
* **PSV PKG on-launch flow reverted to producing a `.vpk`** — v2.30 switched to a decrypted folder tree + `SelectedFile = <baseName>\eboot.bin` and v2.36..v2.38 piled on increasingly elaborate machinery (powershell wrapper, restore-delay tuning, absolute-path resolution) trying to get Vita3K to auto-launch the eboot from the cache. End result on the user's machine: still no end-to-end auto-launch — Vita3K simply has no `--vpk install-and-run` CLI flag, and `-r` wants an already-installed app. New contract:
  * `PsvPkgExtractor` builds a clean `<baseName>.vpk` in the cache and points `SelectedFile` at it.
  * NPDRM license is auto-staged in `<Vita3K>/ux0/license/app/<TITLE_ID>/<contentid>.rif` so the eboot decrypts cleanly on first launch.
  * The user drag-drops the .vpk onto Vita3K once; Vita3K installs the app and the license is already there, so the title boots immediately and on subsequent launches it's already in the Vita3K library.
* `Config.PsvUseVita3kLauncher` default flipped to `false`. The powershell wrapper code stays in the codebase for users who've wired up an alternative Vita3K config and want the CWD push-location, but it's no longer the default path.

## v2.38 (Kyuss82 fork)
* **Fix v2.36/v2.37 wrapper getting reverted mid-extraction** — `OnBeforeGameLaunching` calls `RestoreAllSettingsDelay(5000)` as a safety net for the case where the game never actually starts (so the user's emulator settings aren't left in the hijacked state forever). 5 seconds is too short for the PSV/PS3 PKG flows: archive extraction alone runs ~10s on a ~800 MB PSV PKG, so the timer fired *before* LaunchBox got to launch the emulator and reverted `ApplicationPath` back to `Vita3K.exe` — the powershell wrapper was effectively never used. Bumped the safety-net delay to 120s; the happy path is still `OnAfterGameLaunched` calling `RestoreAllSettings` synchronously the instant LaunchBox does start the emulator, so this only affects the failure case.
* **Resolve emulator path to absolute before hijack** — LaunchBox stores emulator paths relative to its own root (e.g. `..\emulator\vita3k\Vita3K.exe`). The wrapper script was receiving that relative form, then failing `Test-Path` from PowerShell's own CWD. Now resolves via `PathUtils.GetAbsolutePath` first.

## v2.37 (Kyuss82 fork)
* **PSV Vita3K wrapper is now a user-visible option** — v2.36 hardcoded the wrapper; this exposes it as `PsvUseVita3kLauncher` (default `true`) with a checkbox in the *Packaging → Sony* sub-tab, right under the PSV Auto-Update section: *"Wrap Vita3K so config.yml resolves (Push-Location into its install folder before launching)"*. Same UI pattern as the existing `Ps3UseIsoMountLauncher` option. `GameLaunching.cs` only redirects through the powershell wrapper when the flag is on; turning it off restores the vanilla `vita3k.exe -F -r <eboot>` invocation for users who already solved CWD some other way (Symbolic Links, batch launcher, etc.).

## v2.36 (Kyuss82 fork)
* **PSV Vita3K wrapper launcher** — Vita3K reads `config.yml` from the current working directory and aborts with `[E] [main]: Failed to initialise config` if it's missing. LaunchBox launches emulators with its own CWD, so even with `-F -r` configured and a valid NPDRM license in `ux0/license/app/`, Vita3K vanishes silently the moment ACM hands it the cached eboot. Symptom: emulator window never appears, game log ends at "Game started" but nothing happens on screen.
* Same hijack technique used by `Ps3IsoLauncher`: in `OnGameLaunching`, when (a) the title's emulator is Vita3K, (b) the archive matches the PSV PKG flow, and (c) `PsvPkgCacheOnLaunch` is on for that emulator/platform, ACM temporarily swaps `emulator.ApplicationPath` to `powershell.exe` and `emulator.CommandLine` to a generated `psv-vita3k-launcher.ps1` that `Push-Location`s into Vita3K's install folder before invoking the original binary with `-F -r <eboot>`. Both settings are restored after the game launches.

## v2.35 (Kyuss82 fork)
* **zRIF decoder no longer needs SharpZipLib** — v2.33/v2.34 used `SharpZipLib.Inflater.SetDictionary` for the preset-dictionary inflate, but framework-dependent .NET 9 deploys couldn't resolve the transitive package DLL outside the NuGet cache (even with `<Reference HintPath>` + flat `.deps.json` + `AssemblyLoadContext.Default.Resolving` fallback). Rewrote `Zrif.Decode` with a dictionary-replay trick that works on stock `System.IO.Compression.DeflateStream`:
  1. Strip the zlib envelope (CMF/FLG + DICTID + trailing Adler32) → raw deflate bitstream.
  2. Prepend a *stored block* containing the 1024-byte preset dictionary verbatim.
  3. Inflate that wrapped stream — the inflater sees the dict as already-emitted output and resolves back-references inside the RIF bitstream against it exactly like a real preset-dict decoder would.
  4. Slice off the first 1024 bytes — what remains is the RIF.
* Verified against the NPS zRIF for *2013: Infected Wars* (PCSB00842): inflates to 1536 bytes, slices to the expected 512-byte RIF with the content id at offset 23.
* Removed SharpZipLib `<Reference>` from `Core.csproj` and the bundled `thirdparty/SharpZipLib/ICSharpCode.SharpZipLib.dll`. `AssemblyResolver` from v2.34 stays in place as a safety net for any future transitive package.

## v2.34 (Kyuss82 fork)
* **Transitive-package assemblies now load from the app folder** — `dotnet build` produces a `.deps.json` whose `runtime` paths reference the NuGet cache layout (`lib/net6.0/…`). On a machine without that package in `%USERPROFILE%\.nuget\packages`, the .NET host throws `Could not load file or assembly 'ICSharpCode.SharpZipLib, Version=1.4.2.13, …'` even though the DLL sits next to ACM's .exe. New `AssemblyResolver` in Core wires `AssemblyLoadContext.Default.Resolving` from a `[ModuleInitializer]` so any unresolved transitive dependency falls back to `AppContext.BaseDirectory\<name>.dll`. Fixes the v2.33 zRIF decode regression on user machines that don't have SharpZipLib in their NuGet cache.

## v2.33 (Kyuss82 fork)
* **zRIF decoder now uses the pkg2zip preset dictionary** — the previous v2.30 "ZLibStream first, raw deflate fallback" attempt could not work because zRIF blobs declare a 1024-byte preset dictionary (zlib FDICT bit = 1, DICTID = 0x627D1D5D). `System.IO.Compression` does not expose `inflateSetDictionary`, which is why every real-world entry surfaced as "archive entry was compressed using an unsupported compression method". Replaced with SharpZipLib's `Inflater.SetDictionary` and the verbatim 1024-byte `zrif_dict` from pkg2zip (`mmozeiko/pkg2zip`, MIT). Flow now matches pkg2zip / rusty-psn bit-for-bit:
  1. base64-decode the zRIF.
  2. Validate the zlib CMF/FLG bytes (CM=8 deflate, FDICT=1).
  3. Read the 4-byte DICTID, verify it's `0x627D1D5D`.
  4. Feed the remaining bytes (raw deflate + 4-byte trailing Adler32) to a header-less Inflater.
  5. Honour `Inflater.IsNeedingDictionary` by calling `SetDictionary(mZrifDict)` and resuming.
* New PackageReference on SharpZipLib 1.4.2 in `Core.csproj` to get `ICSharpCode.SharpZipLib.Zip.Compression.Inflater`. Adds ~250 KB to the deploy.

## v2.32 (Kyuss82 fork)
* **NPS DB as an offline fallback for `Ps3UpdateFetcher`** — when Sony's titlepatch endpoint can't (or won't) serve a manifest for a Title ID, the fetcher now consults `NpsDb.LookupUpdatesByTitleId`. Triggered in four scenarios:
  * `OfflineMode` is on and no cached manifest exists for the title.
  * Sony returns `404 Not Found`.
  * Sony returns an empty body.
  * Sony's manifest parses but contains zero `<package>` rows.
  * Sony query throws (DNS / TLS / timeout).
* Returned `Ps3UpdateInfo` rows have empty `Sha1Sum` (NPS publishes SHA256, not SHA1) so `Ps3UpdateFetcher.Download` skips the post-download hash check for NPS-sourced updates. Reused as-is across PS3, PSP, and PSV — for PSV this means a working auto-update path even when the Sony HMAC endpoint is unreachable from the user's network.

## v2.31 (Kyuss82 fork)
* **NpsDb now ingests `*_UPDATES.tsv` files** — NPS publishes signed Sony update PKGs in `PSV_UPDATES.tsv` / `PSP_UPDATES.tsv`, with schema `Title ID | Region | Name | Update Version | Required FW VERSION | PKG direct link | Last Modification Date | File Size | SHA256` (no Content ID, no zRIF/RAP). Earlier `NpsDb.TryLoadTsv` rejected those files outright because Content ID was mandatory. New behaviour:
  * Schema discriminator: when Content ID is absent but `Update Version` is present, the file is treated as an update database and rows are stored in `mUpdatesByTitleId : Dictionary<TitleId, List<NpsUpdateEntry>>` (one Title ID can have several update PKGs at different versions).
  * Stays out of the license dictionaries (`mByTitleId` / `mByContentId`), so the base-game zRIF row from `PSV_GAMES.tsv` is not clobbered by a same-Title-Id update row (last-write-wins).
  * Exposes `NpsDb.LookupUpdatesByTitleId(string titleId)` returning the list of `NpsUpdateEntry` so the update installers can use the NPS DB as an offline fallback to the Sony HMAC endpoint.

## v2.30 (Kyuss82 fork)
* **PSV on-launch flow now produces a Vita3K-runnable cache** — previous behaviour packed the decrypted PKG into a `.vpk` in cache and pointed `SelectedFile` at it. Vita3K's CLI rejects a positional `.vpk` argument (its `--vpk` flag doesn't exist; install is GUI-only), so LaunchBox would launch Vita3K but Vita3K had nothing to do — emulator opened to its picker. New flow:
  * `PsvPkgExtractor` unpacks into `<cache>/<baseName>/` (sce_sys/, module/, eboot.bin, …) and sets `SelectedFile = <baseName>/eboot.bin`.
  * Right-click "Create PSV VPK..." (CreatePsvVpkMenuItem) is unchanged — it still produces a standalone `.vpk` for manual installs.
  * **Configuration required in LaunchBox**: set Vita3K's "Default Command-Line Parameters" to `-F -r` (or similar), so the per-game positional path becomes `vita3k.exe -F -r "<cache>\<baseName>\eboot.bin"`.
* **zRIF decoder accepts zlib-wrapped streams** — NoPayStation's zRIF column uses miniz's default `mz_inflateInit` which emits a zlib header (0x78 0x??). `Zrif.Decode` was calling `DeflateStream` directly (raw-deflate only) → "unsupported compression method" on real-world DB entries. Now tries `ZLibStream` first (handles the 0x78 prefix) and falls back to `DeflateStream` for the rare raw-deflate variant.

## v2.29 (Kyuss82 fork)
* **PS3 ISO Decrypt section moved into Sony sub-tab** — `PS3 .dkey folder` (used by the PS3Dec extractor for per-game ISO decryption) and `Mount decrypted PS3 ISOs at launch and run RPCS3 on EBOOT.BIN` were previously orphaned in the *Plugin Settings* tab. They now sit at the top of the *Packaging → Sony* sub-tab as a new "PS3 (.iso)" section, next to the other PS3 families (.pkg, Auto-Update). No behavioural change — pure UI consolidation.

## v2.28 (Kyuss82 fork)
* **Packaging tab restructured** — sezioni accorpate in due sotto-tab "Nintendo (Wii / Wii U / 3DS / DSi)" e "Sony (PS3 / PSP / PSV) + NoPayStation". Scroll interno per sub-tab invece di un'unica colonna alta 2900px; auto-update + license-path stanno ora subito sotto la rispettiva sezione PKG.
* **PSV Auto-Update** — Sony pubblica gli update Vita su un endpoint HMAC-firmato `https://gs-sec.ww.np.dl.playstation.net/pl/np/<TID>/<HMAC>/<TID>-ver.xml`, dove HMAC è `HMAC-SHA256("np_<TID>", key=E5E278AA…550355114)`. `Ps3UpdateFetcher.BuildUrl` ora seleziona il template URL in base a `SonyUpdateContext.Platform`. `PsvPkgStaging` chiama `Ps3UpdateInstaller.Run` al termine dell'estrazione, scaricando le patch in `Ps3UpdateInstaller` cache + unpacking in `<Vita3K>/ux0/patch/<TITLE_ID>/`. Vita3K poi auto-fonde patch+base ad ogni boot.
* New `SonyUpdateContext.ForPsv()` + config flags `PsvAutoInstallUpdates` / `PsvUpdateCachePath` / `PsvUpdateOfflineMode` + UI section "PSV Auto-Update" sotto la sezione PSV (.pkg) nel sub-tab Sony.

## v2.27 (Kyuss82 fork)
* **NoPayStation TSV integration** for PS3 / PSP / PSV PKG flows. New `NpsDbPath` (folder or single .tsv) → ACM scans the user-supplied TSVs on first lookup, builds a TITLE_ID + Content ID map, and uses it to auto-stage missing licenses:
  * **PSV**: `Zrif` column decoded (base64 + raw deflate) into the binary `.rif` file and dropped at `<PsvVita3kDataPath>/ux0/license/app/<TITLE_ID>/<contentid>.rif`. Vita3K boots NPDRM-locked eboot.bin transparently from the next launch onwards. New `PsvVita3kDataPath` config (auto-detects `%APPDATA%\Vita3K\Vita3K` if empty).
  * **PS3**: when the source `.zip` has no `.rap` next to the `.pkg`, ACM falls back to the NPS RAP hex column and synthesises a 16-byte `<contentid>.rap` in both the staging `_rap/` folder and `Ps3RpcsExdataPath`.
  * **PSP**: same RAP fallback for PPSSPP's `PSP/LICENSE/`.
* New `NpsDb` + `Zrif` helpers in Core; new `Ps3RapStaged` / `PspRapStaged` `SourcePath = "<NoPayStation TSV>"` to distinguish auto-staged from archive-staged RAPs.

## v2.26 (Kyuss82 fork)
* **PS Vita `.pkg` in-process decryption + VPK packaging** — completes the Sony PKG family (PS3 / PSP / PSV all share the shared `PsPkgReader` core now). PSV PKGs use the same outer container as PSP but with an extra wrapping layer on the AES data key: `main_key = AES-ECB-encrypt(pkg_vita_X, pkg_data_riv)` where X is picked from the `pkg_ext_header` at offset 0xE4. `PsPkgReader.Parse` detects PSV content (content_type 0x14-0x18) and derives the wrapped key in-line, so the item-table walk + filename + payload decryption pipeline is otherwise unchanged.
* `PsvPkgStaging` extracts the decrypted file tree (`sce_sys/`, `eboot.bin`, `module/*.suprx`, …) into `<cache>/<baseName>/` for the on-launch flow, or zips it as a `<baseName>.vpk` ready to be installed via Vita3K's "Install firmware/.vpk" UI from the right-click menu.
* New `PsvPkgExtractor` (per-emulator opt-in via `PsvPkgCacheOnLaunch`) and `CreatePsvVpkMenuItem` ("Create PSV VPK..."). Config additions: `PsvPkgPlatform`, `PsvPkgOutputPath`, `PsvPkgAddToLibrary`.

## v2.25 (Kyuss82 fork)
* **3DS `.3ds` / `.cci` in-process NCCH decryption** — Azahar / Citra / Lime3DS load encrypted cartridge dumps natively when `aes_keys.txt` is in their sysdata folder, but anything that doesn't (other emus, RetroAchievements, archival tools) needs a decrypted ROM. ACM now produces one in-process, no external `ctrtool.exe` / `3dstool.exe` required.
  * `CtrAesKeys` reads user-supplied `Extractors/aes_keys.txt` (slot0x2CKeyX retail + 0x25/0x18/0x1B for Secure2/3/4 7.x+ titles) and `Extractors/seeddb.bin` (per-TitleID seeds for 7.x+ seed-crypto titles). Same "you bring the keys" model as `wii-titlekeys.bin` / `encTitleKeys.bin`.
  * `CtrKeyScrambler` implements the well-known 3DS keyscrambler `NormalKey = rol((rol(KeyX, 2) ^ KeyY) + C, 87)` with 128-bit big-endian math.
  * `CtrNcchReader` parses NCSD + per-partition NCCH headers; `Ctr3dsDecryptor` decrypts ExHeader / ExeFS / RomFS per partition with the right primary/secondary key (reuses `PsPkgAesCtr` — same AES-128-CTR-BE math) and clears `flags[3] / flags[7]` so emulators see the output as plain.
* **Per-emulator opt-in** — new `Ctr3dsCacheOnLaunch` column in *Extraction Settings* (right of the existing PS3/PSP .pkg columns); when on, encrypted `.3ds`/`.cci` (or `.zip`/`.7z`/`.rar` carrying one) are decrypted into the plugin cache on launch.
* **Right-click `Decrypt 3DS ROM...`** menu — gated by `Ctr3dsPlatform`, outputs a decrypted `.3ds` next to the source archive (or under `Ctr3dsOutputPath`), with optional "Add to library".

## v2.24 (Kyuss82 fork)
* **PSP updates / DLC fetcher + auto-install** — Sony's PSN update endpoint serves both PS3 and PSP titles, so the existing Ps3UpdateFetcher / Ps3UpdateInstaller / Ps3UpdateWindow / Ps3BulkUpdateBuilderWindow now accept a `SonyUpdateContext` and work for PSP too (separate `PspUpdateCachePath`, `PspUpdateOfflineMode`, `PspAutoInstallUpdates` config, USRDIR/CONTENT/ prefix stripping for stage). New right-click menus on PSP games: **"Fetch PSP Updates..."** + **"Build PSP Update DB..."**. New "PSP Auto-Update" section in the Packaging tab mirrors the PS3 one.
* **PSP on-launch auto-update install** — when `PspAutoInstallUpdates` is on, every PSP PKG launch queries Sony, downloads missing patches, and stages them on top of `PSP/GAME/<TITLE_ID>/` in the plugin cache.

## v2.23 (Kyuss82 fork)
* **Offline-first PS3 update DB** — `Ps3UpdateFetcher.QueryAsync` now caches Sony's titlepatch XML alongside the downloaded PKGs (`Ps3UpdateCachePath/<TITLE_ID>/<TITLE_ID>-ver.xml`) and prefers the local copy on subsequent queries. Combined with the existing per-PKG SHA1 cache, the auto-installer is fully self-sufficient after the first online pass.
* **`Ps3UpdateOfflineMode` flag** — new checkbox in the PS3 Auto-Update section: when on, `Query` never contacts Sony, only the local cache is consulted. Useful for LAN / disconnected setups.
* **Bulk PS3 update DB builder** — right-click "Build PS3 Update DB..." on any PS3 game opens a window that enumerates every PS3 title in the LaunchBox library (via `Ps3PkgPlatform`), derives each TITLE_ID using the same inference chain as "Fetch PS3 Updates..." (PKG header / PARAM.SFO / archive-cache fallback), and bulk-queries Sony — saving manifests + PKGs (or just manifests, for a fast first pass) into `Ps3UpdateCachePath`. Cancellable, idempotent (skips already-validated PKGs unless "Re-fetch" is on).
* **Status XML fix** — `Ps3UpdateFetcher.ParseUpdateXml` now treats anything other than `status="deleted"` as parseable. Earlier it required `status="OK"`, which never appears in current Sony responses — they ship `status="alive"`. Fixes the symptom "system never finds updates".

## v2.22 (Kyuss82 fork)
* **PS3 on-launch auto-update install** — new `Ps3AutoInstallUpdates` checkbox in the Packaging tab transparently fetches every published patch / DLC PKG from Sony's PSN update server when a PS3 game is launched, downloads them once into a persistent cache (`Ps3UpdateCachePath`, defaults to `Plugins\ArchiveCacheManager\Ps3UpdateCache`), and stages them on top of the base install — works for both the PKG flow (cache install dir) and the decrypted-ISO flow (RPCS3's real `dev_hdd0/game/<TITLE_ID>/`).
* **TITLE_ID-from-ISO** — for the disc-game flow the installer reads `PS3_GAME/PARAM.SFO` directly out of the decrypted ISO via 7-Zip's UDF support, derives the TITLE_ID, and uses it to query Sony. New `Sfo` helper.
* **Network-failure-safe** — query and download errors are logged but never abort the launch; the game runs with whatever updates are already on disk. PKG cache hits are validated by SHA1 against Sony's manifest so subsequent launches skip the network entirely.

## v2.21 (Kyuss82 fork)
* **PS3 updates fetcher** — right-click "Fetch PS3 Updates..." on PS3 games queries Sony's public PSN update server (`https://a0.ww.np.dl.playstation.net/tpl/np/<TITLE_ID>/<TITLE_ID>-ver.xml`), lists every patch / DLC PKG with version + size + sha1, lets the user pick which to download. Streaming HTTPS download with on-the-fly SHA1 verification (mismatched downloads are deleted). Endpoint + XML schema adapted from [RainbowCookie32/rusty-psn](https://github.com/RainbowCookie32/rusty-psn) (GPL-3.0). New `Ps3UpdateOutputPath` config field for the default download folder.
* **TITLE_ID auto-detection** for the fetcher dialog covers every archive shape: `.pkg` and the `.pkg` inside `.zip` (via `System.IO.Compression`); `.7z` / `.rar` archives carrying either a `.pkg` or a `PARAM.SFO` (via 7-Zip's existing extractor); and `.iso` disc dumps (PS3_GAME/PARAM.SFO via 7-Zip's UDF support). New `Sfo` helper parses Sony PARAM.SFO for TITLE_ID / TITLE / CATEGORY / VERSION.
* **TLS handling** — Sony's `np.dl.playstation.net` cert chain is sometimes flagged by .NET's default validator; `Ps3UpdateFetcher` accepts certs host-pinned to `*.playstation.net` only, so the relaxation can't leak elsewhere.
* **PSP filename sanitisation** — PSP PSN PKGs pad their item table with sealed stub records (the SDK encodes them with garbage filename bytes; only the real EBOOT.PBP carries a printable path). `PsPkgUnpacker.SanitiseRelativePath` now rejects any filename containing control chars, high-bit bytes or Windows-invalid characters, skipping stubs cleanly instead of crashing on `File.Create`.
* **PSP path-transform hook** — `PsPkgUnpacker.Unpack` accepts an optional `pathTransform` callback. `PspPkgStaging` uses it to strip the PSN-internal `USRDIR/CONTENT/` prefix so EBOOT.PBP lands directly under `PSP/GAME/<TITLE_ID>/` (PPSSPP's memstick layout) rather than nested two levels deeper.

## v2.20 (Kyuss82 fork)
* **PSP `.pkg` on-launch packager** — decrypts retail PSP packages (PSP minis, PSN demos, classic PS1 EBOOTs, PSP DLC) into a PPSSPP-bootable memstick folder inside the plugin cache. Output layout is `<baseName>/PSP/GAME/<TITLE_ID>/EBOOT.PBP` plus `<baseName>/PSP/LICENSE/<contentId>.rap`, matching PPSSPP's expected memstick. New: `Ps3PkgStaging`'s sibling `PspPkgStaging`, `Extractors/PspPkgExtractor.cs`, `Plugin/CreatePspPkgMenuItem.cs`.
* **Shared Sony PKG core refactor** — the per-platform staging is now thin glue over a shared algorithmic layer that handles both `pkg_type=0x0001` (PS3) and `pkg_type=0x0002` (PSP/PSV). File renames: `Ps3PkgKeys`→`PsPkgKeys` (+ added `PspKey` / `PsVita2Key` / `PsVita3Key` / `PsVita4Key`), `Ps3PkgAesCtr`→`PsPkgAesCtr`, `Ps3PkgReader`→`PsPkgReader` (new `SelectAesKey(pkg_type, drm_type)` dispatch + `IsRetailPs3` / `IsRetailPspOrPsv` discriminators), `Ps3PkgUnpacker`→`PsPkgUnpacker` (now also auto-detects `EBOOT.PBP`), `Ps3Rap`→`PsRap` (RAP→rifkey is universal across PS3/PSP/PSV). Each staging orchestrator filters by header type so a mixed archive is partitioned safely.
* **PSP licence handling** — `.rap` files are staged into `<install>/PSP/LICENSE/` and, when the new `PspPpssppLicensePath` is configured, also copied into PPSSPP's real memstick `PSP/LICENSE/` folder. The NPDRM EBOOT.PBP is decrypted by PPSSPP at runtime; we don't try to forge a `.rif`.
* **Per-emulator opt-in** — new `PspPkgCacheOnLaunch` column in *Extraction Settings*, sibling to the existing `Ps3PkgCacheOnLaunch`. Toggle on for PPSSPP + Sony PSP only.
* **Packaging tab** — new PSP (.pkg) section with menu-platform list, output folder, PPSSPP memstick LICENSE folder, and add-to-library checkbox, mirroring the PS3 section.

## v2.19 (Kyuss82 fork)
* **PS3 `.pkg` on-launch packager** — decrypts retail PS3 packages into an RPCS3-bootable folder inside the plugin cache, fully in-process (no `pkg2zip.exe` / external dependency). New files: `Ps3PkgKeys.cs`, `Ps3PkgAesCtr.cs` (128-bit big-endian counter), `Ps3PkgReader.cs` (header + decrypted item table), `Ps3PkgUnpacker.cs` (streaming AES-CTR per item), `Ps3PkgStaging.cs` (multi-PKG pipeline), `Ps3PkgExtractor.cs` (LaunchBox-facing).
* **Multi-PKG stacking** — when an archive carries base + update + DLC PKGs together, every PKG installs into the same `<baseName>/` cache root, ordered by `content_type` from each PKG's metadata block (base → patch → DLC). RPCS3 sees a normal, fully patched + DLC-augmented install layout.
* **RAP licence handling** — `.rap` files found alongside the PKGs are copied into `<install>/_rap/`, and, when the new `Ps3RpcsExdataPath` is configured, also into RPCS3's `dev_hdd0/home/00000001/exdata/`. RPCS3's NPDRM path derives the rifkey from the RAP directly, so no per-console keys are needed. `Ps3Rap.RapToRifKey` (ported from make_npdata) and `Ps3Rap.BuildRif` are exposed for callers that want a fully-formed `.rif`.
* **Per-emulator opt-in** — new `Ps3PkgCacheOnLaunch` column in *Extraction Settings*, sibling to `WiiuCacheOnLaunch` / `CiaCacheOnLaunch` / `WadCacheOnLaunch` / `TadCacheOnLaunch`. Toggle on for RPCS3 + Sony Playstation 3 only.
* **Packaging tab** — new PS3 (.pkg) section with menu-platform list, output folder, RPCS3 exdata folder, and add-to-library checkbox, mirroring the existing Wii / Wii U / 3DS / DSi sections.
* **Right-click `Create PS3 Package...` menu** (`CreatePs3PkgMenuItem`) for one-off conversions outside the plugin cache, gated by `Ps3PkgPlatform`. Optionally adds the staged install's `EBOOT.BIN` to the LaunchBox library when `Ps3PkgAddToLibrary` is on.

## v2.18 (Kyuss82 fork)
* **Wii `.wad` builder is now internal C#** (`WadBuilder.cs`, port of the `wii.py` `packdir` logic). Sharpii-NetCore.exe is no longer required in `Extractors/` for the Wii flow — one less external dependency and one fewer command-line-syntax footgun.
* **Wii fakesigned ticket forge** for VC / WiiWare titles whose cetk is not on NUS. When the archive has no cetk and NUS returns 404, the plugin now falls back to a lookup in **`wii-titlekeys.bin`** (`Extractors/wii-titlekeys.bin` or `WadTitleKeysPath` in INI) → `WiiTicketBuilder.Build` (port of `wii.py` `Ticket.fakesign` + `fixpayload`: construct ticket from scratch, zero the RSA signature, iterate the padding field until `SHA1[0] == 0x00`). Accepted by Dolphin and by cIOS-patched real Wii.
* **3DS `.cia` fake-signed ticket forge** for retail / VC / eShop titles whose cetk is not on NUS. When the archive has no cetk and NUS returns 404, the plugin now falls back to:
    1. lookup of the encrypted title key in **`encTitleKeys.bin`** (user-supplied, `Extractors/encTitleKeys.bin` or `CiaEncTitleKeysPath` in INI), then
    2. forge a fakesigned `cetk` from a **donor template** (`Extractors/cetk-donor.bin` or `CiaCetkDonorPath`).
    Same trujivuelta-style fakesign trick used elsewhere; accepted by Citra/Lime3DS and 3DS CFW (FBI/Luma3DS).
* **Packager extractors now opt-in to `AlwaysCache`** — `MinArchiveSize` no longer skips the cache for `.wua`/`.cia`/`.wad`/`.tad` builds. Small CDN dumps (typical VC titles, a few MB) get cached and reused on subsequent launches instead of rebuilding every time.
* **Multi-TMD output naming** for Cia/Tad/Wad: the highest-version build now lands at `<baseName>.<ext>` (no suffix) and older versions get `<baseName>.v<N>.<ext>`. Previously every output got `.v<N>` and the launch-time file-list prediction couldn't find the bare-named file. Single-TMD archives are unchanged.
* `Plugin.csproj` HintPath for the LaunchBox SDK reverted to the upstream relative form (`../../thirdparty/Unbroken.LaunchBox.Plugins/12.8/`) so anyone can build the plugin by dropping their own copy of the SDK there.
* CI workflow restricted to building the `Core` assembly only (Plugin needs the closed-source LaunchBox SDK which CI runners don't have).
* `scripts/release.sh`: one-shot build + tag + GitHub release helper. Detects `dotnet` vs `dotnet.exe` (WSL interop), checks git identity is configured, passes `--repo` to `gh` explicitly.

## v2.17 (Kyuss82 fork)
* **PS3 support**
    * `PS3Dec` extractor for decrypting PS3 ISOs using a `.dkey` file (`ps3decrs.exe` in `Extractors/`); new per-emulator-platform column in *Extraction Settings*.
    * Optional **PS3 ISO mount launcher** for RPCS3: mounts the ISO as a virtual drive via PowerShell, launches RPCS3 on the `EBOOT.BIN` inside, dismounts on exit (toggle: `Ps3UseIsoMountLauncher` in plugin INI). Approach adapted from [ptmorris1/RPCS3-ISOLauncher-Launchbox](https://github.com/ptmorris1/RPCS3-ISOLauncher-Launchbox); the in-plugin variant takes RPCS3 path as a parameter so it works without sitting next to `rpcs3.exe`, and replaces the fixed 2 s post-mount sleep with a bounded volume-letter poll.
* **On-launch native packagers** — turn a CDN-style archive (`.zip`/`.7z`/`.rar`) into the emulator-native format inside the plugin cache, transparently to LaunchBox:
    * **Wii U `.wua`** (Cemu) — pipeline: CDecrypt + zarchive.
    * **3DS `.cia`** — internal C# builder; supports multi-TMD archives (base + updates) → one `.cia` per TMD version, latest version selected as primary.
    * **Wii `.wad`** (Dolphin) — Sharpii-NetCore; supports multi-TMD archives.
    * **DSi `.tad`** (melonDS) — internal C# builder; supports multi-TMD archives.
    * Each is opt-in via a new column in *Extraction Settings* per emulator+platform pair (same UX as `chdman` / `DolphinTool` / `ExtractXiso` / `PS3Dec`).
* **Wii U title-key handling** — primary source is **Cemu's `keys.txt`** (`%APPDATA%\Cemu\keys.txt` auto-detected, overridable in *Packaging* tab). Local, offline, user-curated.
* **Right-click `Create * Package...` menus** retained for one-off permanent conversions; `WiiuPlatform` / `CiaPlatform` / `WadPlatform` / `TadPlatform` CSVs gate where the menu appears.
* **Packaging tab** label cleanup: each control now clearly states whether it is *menu-only* or *shared with auto-cache*.

## v2.16 (2023-02-10)
* New M3U name option - "Disc 1 Filename"
    * Always use the filename of the first disc of a multi-disc game for the m3u file, regardless of which disc was launched
    * Allows better support for The Bezel Project config files, which use config files based on the ROM name
* New batch caching option to pause on caching errors (default is to skip and continue)
* Minor config window tweaks

## v2.15 (2023-01-19)
* New _extract-xiso_ option for Xbox iso conversion
    * Full iso files (redump) automatically converted and cached in xiso format
    * Supports both zipped and unzipped iso files
    * Requires _extract-xiso.exe_ to be added to the `ArchiveCacheManager\Extractors` folder
* Reduced archive cache path lengths, avoiding path too long errors
* Small performance improvement when checking many file priorities
* Smart Extract uses Priority to select file from archive in case where individual ROM file not previously selected
* Fix incorrect path for auto generated M3Us when Launch Path is not Default
* Fix background thread issue when Batch Cache Games window closed while still calculating archive sizes
* Interface tweaks

## v2.14 (2022-05-05)
* New right-click menu option - "Batch Cache Games"
    * Extract or copy multiple games to the cache, ready to play later
    * Bulk cache games from NAS or external storage
        * Cached games can be played even if network or external storage disconnected

## v2.13 (2022-04-28)
* Fix config window DPI scaling issue
* Config window performance improvements
* Option to skip version during update check

## v2.12 (2022-04-21)
* Option to copy non-archive files to cache
* Support for extracting additional formats
    * Option to extract chd to cue+bin using chdman
	* Option to extract rvz, wia, gcz to iso using DolphinTool
* Option to specify a launch folder for cached games (game platform, game title, or emulator title)
    * Useful for managing common RetroArch settings
* Smart Extract option to only extract required files from an archive
    * Useful for merged ROM sets
* Emulator selection when launching a file from "Select ROM In Archive" list
* Option to bypass LaunchBox's check the ROM file exists when launching a game
    * Allows launching cached game immediately - no waiting for slow disk spin-up or network latency
	* Allows playing cached games 'offline' if NAS or cloud storage unavailable
* Config window and UI overhaul
* Minor bug fixes

## v2.11 (2022-03-25)
* Multi-disc support and automatic M3U generation
    * Extract and cache all discs in a multi-disc game
    * Generate and use M3U where supported by emulator / platform
* Custom filename priority for all emulators / platforms
* Option to automatically check for plugin updates
* Updated 7-Zip to version 21.07
* Minor bug fixes

## v2.0.10 (2022-03-08)
* Support for LaunchBox 12.8 Extract ROM per platform setting

## v2.0.9 (2022-01-31)
* Fix file priority for files in subfolder of archive when not in cache
* Fix launching individual file from archive when not in cache

## v2.0.8 (2022-01-13)
* Wildcard based filename matching for file priorities in archive
    * Prioritize a file extension, filename, or combination
    * Create priorities to automatically play preferred ROM region from GoodMerged archives
* Performance improvements, especially for archives with many hundreds or thousands files
* [BigBox] "Select ROM In Archive" menu option (accepts keyboard input only)

## v2.0.7 (2021-03-30)
* Badge to indicate if game is in cache
    * Available under Badges -> Enable Archive Cached menu
* Remember previous selection made in "Select Rom In Archive..." right-click menu
    * Previous selection automatically applied when game started normally

## v2.0.6 (2021-03-26)
* Only remove items from cache path originally extracted by plugin
* Additional checks for invalid cache paths

## v2.0.5 (2021-03-25)
* Fix to ensure cache path valid if config file corrupt

## v2.0.4 (2021-03-24)
* New feature - 'Keep'
    * Keep your favourite games cached and ready to play
    * Games marked 'Keep' will not be removed from the cache, and do not contribute to the used cache size
* Configuration window updates
    * Cache info summary
    * View cached games, toggle the 'Keep' option
    * Manually remove games from the cache or clear it entirely
* Events and errors now logged to `LaunchBox\Plugins\ArchiveCacheManager\Logs`
* Minor bug fixes

## v2.0.3 (2021-03-17)
* Aborting game startup process (`Esc` on Startup Screen) now terminates extract operation
* Cleanup partially extracted archive from cache on 7z error, or previous startup process abort
* Fix archive list error when selecting individual ROM after a previous game launch failure

## v2.0.2 (2021-03-14)
* New feature - Select and play ROM file from archive
    * Right-click a game and click "Select ROM In Archive..."
    * Currently only supports LaunchBox

## v2.0.1 (2021-03-11)
* Support Startup Screen progress bar during initial extraction
* Minor bug fixes

## v2.0.0 (2021-03-10)
* Code now open source under LGPL
* Rewritten to use LaunchBox plugin API
* Added configuration window in Tools menu
* Support for LaunchBox & BigBox 10.x, 11.x

## v1.5 (2018-04-13)
* Add support for LaunchBox.Next

## v1.4 (2018-01-25)
* Add emulator + platform based file priority, for multi-system emulators

## v1.3 (2017-12-24)
* Fix LaunchBox overriding Archive Cache Manager during 7-Zip 16.04 update
* Include 7-Zip version 16.04 with installation

## v1.2 (2017-11-30)
* Support for loading files direct from cache, bypassing LaunchBox temp folder
    * This is now the default behaviour (no hardlinks/junctions)
* Configuration option to force using hardlinks/junctions
* Fix artwork not displaying for game titles containing an apostrophe
* Update to .NET Framework 4.7, inline with LaunchBox

## v1.1 (2017-01-25)
* Show clear logo on loading screen, text title if logo unavailable
* Display cover art in loading screen when region specific art not found
* Configuration option to enable/disable verbose logging
* Configuration option to force file copy from cache to LaunchBox temp folder
* Support non-NTFS volumes
* Support cache stored in network location

## v1.0 (2017-01-03)
* Initial release
