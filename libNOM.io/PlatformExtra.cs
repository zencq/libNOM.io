namespace libNOM.io;


/// <summary>
/// Holds additional information for a single <see cref="Container"/>, mainly used for the meta/manifest file.
/// </summary>
internal record class PlatformExtra
{
#if NETSTANDARD2_0_OR_GREATER
    #region Global

    internal MetaFormatEnum MetaFormat { get; set; }

    internal byte[]? Bytes { get; set; }

    internal uint Size { get; set; }

    internal uint SizeDecompressed { get; set; }

    internal uint SizeDisk { get; set; }

    internal DateTimeOffset? LastWriteTime { get; set; }

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
#else
    #region Global

    internal MetaFormatEnum MetaFormat { get; init; }

    // Microsoft = Meta (Waypoint)
    // Playstation = Data (memory.dat) or Meta (SaveStreaming and manifest00.hg)
    // Steam = Meta (Waypoint or AccountData)
    // Switch = Meta (Waypoint)
    internal byte[]? Bytes { get; init; }

    // Microsoft = Meta
    // Playstation = Data (compressed or decompressed depending on SaveWizard usage)
    // Steam = Meta
    // Switch = Meta
    internal uint Size { get; init; }

    // Microsoft
    // Playstation
    // Steam
    // Switch
    internal uint SizeDecompressed { get; init; }

    // Microsoft = Meta + Data (compressed)
    // Steam = Data (compressed)
    // Playstation = Data (compressed)
    // Switch = Data (compressed)
    internal uint SizeDisk { get; init; }

    // Microsoft
    // Playstation (SAVE_FORMAT_2)
    // Steam
    // Switch
    internal DateTimeOffset? LastWriteTime { get; init; }

    // Microsoft
    // Steam
    // Switch
    internal int BaseVersion { get; init; } // actually uint as well but for better readability due to less casting int is used

    internal ushort GameMode { get; init; }

    internal ushort Season { get; init; }

    internal uint TotalPlayTime { get; init; }

    internal string SaveName { get; init; } = string.Empty;

    internal string SaveSummary { get; init; } = string.Empty;

    internal uint DifficultyPreset { get; init; }

    #endregion

    #region Microsoft

    internal string? MicrosoftSyncTime { get; init; } // Ticks as Hexadecimal as String with surrounding double quotes
    internal byte? MicrosoftBlobContainerExtension { get; init; }
    internal MicrosoftBlobSyncStateEnum? MicrosoftSyncState { get; init; }
    internal Guid? MicrosoftBlobDirectoryGuid { get; init; }
    internal FileInfo? MicrosoftBlobDataFile { get; init; }
    internal Guid? MicrosoftBlobDataSyncGuid { get; init; }
    internal FileInfo? MicrosoftBlobMetaFile { get; init; }
    internal Guid? MicrosoftBlobMetaSyncGuid { get; init; }

    internal DirectoryInfo? MicrosoftBlobDirectory { get; init; }
    internal FileInfo? MicrosoftBlobContainerFile => MicrosoftBlobDirectory is not null && MicrosoftBlobContainerExtension is not null ? new(Path.Combine(MicrosoftBlobDirectory.FullName ?? string.Empty, $"container.{MicrosoftBlobContainerExtension}")) : null;

    #endregion

    #region Playstation

    internal int? PlaystationOffset { get; init; }

    #endregion
#endif
}
