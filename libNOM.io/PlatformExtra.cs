namespace libNOM.io;


/// <summary>
/// Holds additional information for a single <see cref="Container"/>, mainly used for the meta/manifest file.
/// </summary>
internal record class PlatformExtra
{
#if NETSTANDARD2_0_OR_GREATER
    #region Global

    // Microsoft = Meta (tail)
    //     Playstation = Data
    // Steam = Meta (tail)
    //     Switch = Tail
    internal byte[]? Bytes { get; set; }

    // Microsoft = Meta
    //     Playstation = Data (compressed or decompressed depending on SaveWizard usage)
    // Steam = Meta
    internal uint Size { get; set; }

    // Microsoft = Data (decompressed)
    //     Playstation = Data (decompressed)
    // Steam = Data (decompressed)
    //     Switch = Data (decompressed)
    internal uint SizeDecompressed { get; set; }

    // Microsoft = Meta + Data (compressed)
    // Steam = Data (compressed)
    //     Playstation = Data (compressed)
    internal uint SizeDisk { get; set; }

    // Microsoft
    // Steam
    //     Switch
    //     Playstation (0x7D1 only)
    internal DateTimeOffset? LastWriteTime { get; set; }

    // Microsoft
    // Steam
    //     Switch
    internal int BaseVersion { get; set; } // actually uint as well but for better readability due to less casting int is used

    // Microsoft
    // Steam
    //     Switch
    internal ushort GameMode { get; set; }

    internal ushort Season { get; set; }

    // Microsoft
    // Steam
    //     Switch
    internal uint TotalPlayTime { get; set; }

    // Microsoft
    // Steam
    //     Switch
    internal string SaveName { get; set; } = string.Empty;

    // Microsoft
    // Steam
    //     Switch
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

    // Microsoft = Meta (tail)
    //     Playstation = Data
    // Steam = Meta (tail)
    //     Switch = Tail
    internal byte[]? Bytes { get; init; }

    // Microsoft = Meta
    //     Playstation = Data (compressed or decompressed depending on SaveWizard usage)
    // Steam = Meta
    internal uint Size { get; init; }

    // Microsoft = Data (decompressed)
    //     Playstation = Data (decompressed)
    // Steam = Data (decompressed)
    //     Switch = Data (decompressed)
    internal uint SizeDecompressed { get; init; }

    // Microsoft = Meta + Data (compressed)
    // Steam = Data (compressed)
    //     Playstation = Data (compressed)
    internal uint SizeDisk { get; init; }

    // Microsoft
    // Steam
    //     Switch
    //     Playstation (0x7D1 only)
    internal DateTimeOffset? LastWriteTime { get; init; }

    // Microsoft
    // Steam
    //     Switch
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
