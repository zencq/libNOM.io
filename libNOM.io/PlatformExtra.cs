namespace libNOM.io;


internal record class PlatformExtra
{
    #region Global

    // Microsoft = Meta (tail)
    //     Playstation = Data
    // Steam = Meta (tail)
    //     Switch = Tail
    internal byte[]? Bytes;

    // Microsoft = Meta
    //     Playstation = Data (compressed or decompressed depending on SaveWizard usage)
    // Steam = Meta
    internal uint Size;

    // Microsoft = Data (decompressed)
    //     Playstation = Data (decompressed)
    // Steam = Data (decompressed)
    //     Switch = Data (decompressed)
    internal uint SizeDecompressed;

    // Microsoft = Meta + Data (compressed)
    // Steam = Data (compressed)
    //     Playstation = Data (compressed)
    internal uint SizeDisk;

    // Microsoft
    // Steam
    //     Switch
    internal DateTimeOffset? LastWriteTime;

    // Microsoft
    // Steam
    //     Switch
    internal int BaseVersion;

    // Microsoft
    // Steam
    //     Switch
    internal short GameMode;

    internal short Season;

    // Microsoft
    // Steam
    //     Switch
    internal uint TotalPlayTime;

    internal string SaveName = string.Empty;

    internal string SaveSummary = string.Empty;

    #endregion

    #region Microsoft

    internal string? MicrosoftSyncTime; // Ticks Hexadecimal as String with surrounding double quotes
    internal byte? MicrosoftBlobContainerExtension;
    internal MicrosoftBlobSyncStateEnum? MicrosoftSyncState;
    internal Guid? MicrosoftBlobDirectoryGuid;
    internal FileInfo? MicrosoftBlobDataFile;
    internal FileInfo? MicrosoftBlobMetaFile;

    internal DirectoryInfo? MicrosoftBlobDirectory;
    internal FileInfo? MicrosoftBlobContainerFile => MicrosoftBlobDirectory is not null && MicrosoftBlobContainerExtension is not null ? new(Path.Combine(MicrosoftBlobDirectory.FullName ?? string.Empty, $"container.{MicrosoftBlobContainerExtension}")) : null;

    #endregion

    #region Playstation

    internal int? PlaystationOffset;

    #endregion
}
