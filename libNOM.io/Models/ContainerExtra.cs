namespace libNOM.io.Models;


/// <summary>
/// Holds additional information for a single <see cref="Container"/>, mainly gathered from the meta/manifest file or on the way to access it.
/// Most of them need to be written back to the related files.
/// </summary>
internal record class ContainerExtra
{
    #region Meta

    // Microsoft = Meta (Waypoint)
    // Playstation = Data (Legacy) or Meta (SaveStreaming)
    // Steam = Meta (Waypoint or AccountData)
    // Switch = Meta (Waypoint)
    internal byte[]? Bytes { get; init; }

    // Microsoft
    // Playstation
    // Steam
    // Switch
    internal uint MetaLength { get; init; }

    #endregion

    #region Data

    // Microsoft
    // Playstation
    // Steam
    // Switch
    internal uint SizeDecompressed { get; init; }

    // Microsoft
    // Steam
    // Playstation
    // Switch
    internal uint SizeDisk { get; init; }

    // Microsoft
    // Playstation (META_FORMAT_1)
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
}
