namespace libNOM.io;


/// <summary>
/// Holds settings how a <see cref="Platform"/> should behave.
/// </summary>
public record class PlatformSettings
{
#if NET47_OR_GREATER || NETSTANDARD2_0_OR_GREATER
    /// <summary>
    /// Where to store backups.
    /// </summary>
    public string Backup { get; set; } = "backup";

    /// <summary>
    /// Where to download things.
    /// </summary>
    public string Download { get; set; } = "download";

    /// <summary>
    /// Whether to set the current timestamp to the file while writing.
    /// </summary>
    public bool LastWriteTime { get; set; } = true;

    /// <summary>
    /// How to load and keep save information and data.
    /// </summary>
    public LoadingStrategyEnum LoadingStrategy { get; set; } = LoadingStrategyEnum.Empty;

    /// <summary>
    /// Whether to deobfuscated and obfuscated the save data.
    /// </summary>
    public bool Mapping { get; set; } = true;

    /// <summary>
    /// Maximum number of backups to keep.
    /// </summary>
    public int MaxBackupCount { get; set; } = 3;

    /// <summary>
    /// Whether to enable a FileSystemWatcher to detect changes in the background.
    /// </summary>
    public bool Watcher { get; set; } = true;
#elif NET5_0_OR_GREATER
    /// <summary>
    /// Where to store backups.
    /// </summary>
    public string Backup { get; init; } = "backup";

    /// <summary>
    /// Where to download things.
    /// </summary>
    public string Download { get; init; } = "download";

    /// <summary>
    /// Whether to set the LastWriteTime while writing.
    /// </summary>
    public bool LastWriteTime { get; init; } = true;

    /// <summary>
    /// How to load and keep save information and data.
    /// </summary>
    public LoadingStrategyEnum LoadingStrategy { get; init; } = LoadingStrategyEnum.Empty;

    /// <summary>
    /// Whether to deobfuscated and obfuscated the save data.
    /// </summary>
    public bool Mapping { get; init; } = true;

    /// <summary>
    /// Maximum number of backups to keep.
    /// </summary>
    public int MaxBackupCount { get; init; } = 3;

    /// <summary>
    /// Whether to enable a FileSystemWatcher to detect changes in the background.
    /// </summary>
    public bool Watcher { get; init; } = true;
#endif
}
