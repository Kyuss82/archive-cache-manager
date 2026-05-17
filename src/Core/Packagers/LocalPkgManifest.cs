/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Data model for the offline-mirror manifest written by `LocalPkgIndexer` and
 * read by `LocalManifestResolver` / `Ps3UpdateInstaller` / `Ps3DlcInstaller`.
 * Split out of `LocalPkgIndexer.cs` in v2.72 so the orchestrator file is just
 * the scan logic + I/O — the JSON schema lives here.
 *
 * Manifest layout (one per platform, stored under each platform's update cache root):
 *
 *   {
 *     "platform": "PS3",
 *     "generated_at": "2026-05-16T18:00:00Z",
 *     "titles": {
 *       "NPEB00768": {
 *         "updates": [
 *           { "content_id":"EP4295-NPEB00768_00-…", "pkg_path":"D:\\PS3\\Updates\\…pkg",
 *             "size":169123456, "sha1":"" }
 *         ],
 *         "dlcs": [
 *           { "content_id":"EP4295-NPEB00768_00-AMYDLC01000001",
 *             "pkg_path":"D:\\PS3\\DLC\\…pkg",
 *             "rap_path":"D:\\PS3\\DLC\\…rap"  // sibling .rap, NPS-style export
 *           }
 *         ]
 *       }
 *     }
 *   }
 */
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ArchiveCacheManager
{
    public enum LocalPkgPlatform { Ps3, Psp, Psv, Wiiu, Ctr3ds }

    public class LocalPkgEntry
    {
        [JsonPropertyName("content_id")] public string ContentId { get; set; }
        [JsonPropertyName("title_id")]   public string TitleId   { get; set; }
        [JsonPropertyName("pkg_path")]   public string PkgPath   { get; set; }
        [JsonPropertyName("rap_path")]   public string RapPath   { get; set; }
        [JsonPropertyName("size")]       public long   Size      { get; set; }
        [JsonPropertyName("sha1")]       public string Sha1      { get; set; }     // empty unless caller filled it
        [JsonPropertyName("content_type")] public uint ContentType { get; set; }
        /// <summary>When the PKG lives inside a `.zip`/`.7z`/`.rar` wrapper, this is the path to the wrapper.</summary>
        [JsonPropertyName("archive_path")] public string ArchivePath { get; set; }
        /// <summary>Entry name of the .pkg inside the wrapper archive (relative path).</summary>
        [JsonPropertyName("archive_entry")] public string ArchiveEntry { get; set; }
        /// <summary>Entry name of the companion .rap inside the wrapper archive, if found.</summary>
        [JsonPropertyName("rap_archive_entry")] public string RapArchiveEntry { get; set; }
    }

    public class LocalPkgTitle
    {
        [JsonPropertyName("updates")] public List<LocalPkgEntry> Updates { get; set; } = new List<LocalPkgEntry>();
        [JsonPropertyName("dlcs")]    public List<LocalPkgEntry> Dlcs    { get; set; } = new List<LocalPkgEntry>();
    }

    public class LocalPkgManifest
    {
        [JsonPropertyName("platform")]     public string Platform     { get; set; }
        [JsonPropertyName("generated_at")] public string GeneratedAt  { get; set; }
        [JsonPropertyName("titles")]       public Dictionary<string, LocalPkgTitle> Titles { get; set; } = new Dictionary<string, LocalPkgTitle>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Per-tick progress snapshot. The scanner mutates a single instance; the UI thread
    /// reads it via the `Reporter` callback. `LogLine` is a one-shot — cleared between ticks.
    /// </summary>
    public class LocalPkgScanProgress
    {
        public int FilesSeen;
        public int Indexed;
        public int Updates;
        public int Dlcs;
        public int Skipped;
        public int Errors;
        public string CurrentFile;
        /// <summary>Optional one-shot log line to emit on the UI on this tick (cleared between ticks).</summary>
        public string LogLine;
    }
}
