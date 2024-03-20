namespace libNOM.io.Models;


/// <summary>
/// Holds additional information for a single <see cref="Container"/>, mainly used for the meta/manifest file.
/// As this is only used internally, it is okay to always have { get; set; } instead of { get; init; }.
/// </summary>
internal record class PlatformExtra
{
    #region Global

    internal MetaFormatEnum MetaFormat { get; set; }

    // Microsoft = Meta (Waypoint)
    // Playstation = Data (memory.dat) or Meta (SaveStreaming and manifest00.hg)
    // Steam = Meta (Waypoint or AccountData)
    // Switch = Meta (Waypoint)
    internal byte[]? Bytes { get; set; }

    // Microsoft = Meta
    // Playstation = Data (compressed or decompressed depending on SaveWizard usage)
    // Steam = Meta
    // Switch = Meta
    internal uint Size { get; set; }

    // Microsoft
    // Playstation
    // Steam
    // Switch
    internal uint SizeDecompressed { get; set; }

    // Microsoft
    // Steam
    // Playstation
    // Switch
    internal uint SizeDisk { get; set; }

    // Microsoft
    // Playstation (SAVE_FORMAT_2)
    // Steam
    // Switch
    internal DateTimeOffset? LastWriteTime { get; set; }

    // Microsoft
    // Steam
    // Switch
    internal int BaseVersion { get; set; } // actually uint as well but for better readability due to less casting int is used

    internal ushort GameMode { get; set; }

    internal ushort Season { get; set; }

    internal uint TotalPlayTime { get; set; }

    internal string SaveName { get; set; } = string.Empty;

    internal string SaveSummary { get; set; } = string.Empty;

    internal uint DifficultyPreset { get; set; }

    #endregion

    #region Microsoft

    internal string? MicrosoftSyncTime { get; set; } // Ticks as Hexadecimal as String with surrounding double quotes
    internal byte? MicrosoftBlobContainerExtension { get; set; }
    internal MicrosoftBlobSyncStateEnum? MicrosoftSyncState { get; set; }
    internal Guid? MicrosoftBlobDirectoryGuid { get; set; }
    internal FileInfo? MicrosoftBlobDataFile { get; set; }
    internal Guid? MicrosoftBlobDataSyncGuid { get; set; }
    internal FileInfo? MicrosoftBlobMetaFile { get; set; }
    internal Guid? MicrosoftBlobMetaSyncGuid { get; set; }

    internal DirectoryInfo? MicrosoftBlobDirectory { get; set; }

    internal FileInfo? MicrosoftBlobContainerFile => MicrosoftBlobDirectory is not null && MicrosoftBlobContainerExtension is not null ? new(Path.Combine(MicrosoftBlobDirectory.FullName ?? string.Empty, $"container.{MicrosoftBlobContainerExtension}")) : null;

    #endregion

    #region Playstation

    internal int? PlaystationOffset { get; set; }

    #endregion
}
