namespace libNOM.io;


/// <summary>
/// Holds settings how a <see cref="Platform"/> should behave.
/// </summary>
public record class PlatformSettings
{
#if NETSTANDARD2_0_OR_GREATER
    /// <summary>
    /// Where to store backups.
    /// </summary>
    public string Backup { get; set; } = "backup";

    /// <summary>
    /// Where to download things.
    /// </summary>
    public string Download { get; set; } = "download";

    /// <summary>
    /// How to load and keep save information and data.
    /// </summary>
    public LoadingStrategyEnum LoadingStrategy { get; set; } = LoadingStrategyEnum.Empty;

    /// <summary>
    /// Maximum number of backups to keep.
    /// </summary>
    public int MaxBackupCount { get; set; } = 3;

    /// <summary>
    /// Whether to set the current timestamp to the file while writing.
    /// </summary>
    public bool SetLastWriteTime { get; set; } = true;

    /// <summary>
    /// Whether to use external sources to enhance the user identification process.
    /// </summary>
    public bool UseExternalSourcesForUserIdentification { get; set; } = true;

    /// <summary>
    /// Whether to deobfuscated and obfuscated the save data while parsing.
    /// </summary>
    public bool UseMapping { get; set; } = true;

    /// <summary>
    /// Whether to enable a FileSystemWatcher to detect changes in the background.
    /// </summary>
    public bool Watcher { get; set; } = true;

    /// <summary>
    /// Whether to write a <see cref="Container"/> always or only if unsynced.
    /// </summary>
    public bool WriteAlways { get; set; } = true;
#else
    /// <summary>
    /// Where to store backups.
    /// </summary>
    public string Backup { get; init; } = "backup";

    /// <summary>
    /// Where to download things.
    /// </summary>
    public string Download { get; init; } = "download";

    /// <summary>
    /// How to load and keep save information and data.
    /// </summary>
    public LoadingStrategyEnum LoadingStrategy { get; init; } = LoadingStrategyEnum.Empty;

    /// <summary>
    /// Maximum number of backups to keep.
    /// </summary>
    public int MaxBackupCount { get; init; } = 3;

    /// <summary>
    /// Whether to set the LastWriteTime while writing.
    /// </summary>
    public bool SetLastWriteTime { get; init; } = true;

    /// <summary>
    /// Whether to use external sources to enhance the user identification process.
    /// Currently used for: GOG.com
    /// </summary>
    public bool UseExternalSourcesForUserIdentification { get; init; } = true;

    /// <summary>
    /// Whether to deobfuscated and obfuscated the save data.
    /// </summary>
    public bool UseMapping { get; init; } = true;

    /// <summary>
    /// Whether to enable a FileSystemWatcher to detect changes in the background.
    /// </summary>
    public bool Watcher { get; init; } = true;

    /// <summary>
    /// Whether to write a <see cref="Container"/> always or only if unsynced.
    /// </summary>
    public bool WriteAlways { get; init; } = true;
#endif
}
