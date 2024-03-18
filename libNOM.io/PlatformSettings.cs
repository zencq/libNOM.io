namespace libNOM.io;


/// <summary>
/// Holds settings how a <see cref="Platform"/> should behave.
/// </summary>
public record class PlatformSettings
{
#if NETSTANDARD2_0_OR_GREATER
    /// <summary>
    /// Where to store backups. By default relative to the working directory.
    /// Default: "backup"
    /// </summary>
    public string BackupDirectory { get; set; } = "backup";

    /// <summary>
    /// How to load and keep save information and data.
    /// Default: <see cref="LoadingStrategyEnum.Empty"/>
    /// </summary>
    public LoadingStrategyEnum LoadingStrategy { get; set; } = LoadingStrategyEnum.Empty;

    /// <summary>
    /// Maximum number of backups per save to keep.
    /// Default: 3
    /// </summary>
    public int MaxBackupCount { get; set; } = 3;

    /// <summary>
    /// Whether to set the current timestamp to the file while writing.
    /// Default: true
    /// </summary>
    public bool SetLastWriteTime { get; set; } = true;

    /// <summary>
    /// Whether to use external sources to enhance the user identification process.
    /// Default: true
    /// </summary>
    public bool UseExternalSourcesForUserIdentification { get; set; } = true;

    /// <summary>
    /// Whether to deobfuscated or obfuscated the save data while parsing.
    /// If set to true, the JSON data in the <see cref="Container"/>s will be deobfuscated.
    /// The result of this is deterministic. Even if you load a deobfuscated file, the JSON data in a <see cref="Container"/> will be obfuscated if this setting is set to false.
    /// The written file will always be obfuscated, regardless of this setting.
    /// Default: false
    /// </summary>
    public bool UseMapping { get; set; }

    /// <summary>
    /// Whether to enable a FileSystemWatcher to detect changes in the background.
    /// Default: false
    /// </summary>
    public bool Watcher { get; set; }

    /// <summary>
    /// Whether to write a <see cref="Container"/> always or only if not synced.
    /// Default: false
    /// </summary>
    public bool WriteAlways { get; set; }
#else
    /// <summary>
    /// Where to store backups. By default relative to the working directory.
    /// Default: "backup"
    /// </summary>
    public string BackupDirectory { get; init; } = "backup";

    /// <summary>
    /// How to load and keep save information and data.
    /// Default: <see cref="LoadingStrategyEnum.Empty"/>
    /// </summary>
    public LoadingStrategyEnum LoadingStrategy { get; init; } = LoadingStrategyEnum.Empty;

    /// <summary>
    /// Maximum number of backups per save to keep.
    /// Default: 3
    /// </summary>
    public int MaxBackupCount { get; init; } = 3;

    /// <summary>
    /// Whether to set the LastWriteTime while writing.
    /// Default: true
    /// </summary>
    public bool SetLastWriteTime { get; init; } = true;

    /// <summary>
    /// Whether to use external sources to enhance the user identification process.
    /// Default: true
    /// </summary>
    public bool UseExternalSourcesForUserIdentification { get; init; } = true;

    /// <summary>
    /// Whether to deobfuscated or obfuscated the save data while parsing.
    /// If set to true, the JSON data in the <see cref="Container"/>s will be deobfuscated.
    /// The result of this is deterministic. Even if you load a deobfuscated file, the JSON data in a <see cref="Container"/> will be obfuscated if this setting is set to false.
    /// The written file will always be obfuscated, regardless of this setting.
    /// Default: false
    /// </summary>
    public bool UseMapping { get; init; }

    /// <summary>
    /// Whether to enable a FileSystemWatcher to detect changes in the background.
    /// Default: false
    /// </summary>
    public bool Watcher { get; init; }

    /// <summary>
    /// Whether to write a <see cref="Container"/> always or only if not synced.
    /// Default: false
    /// </summary>
    public bool WriteAlways { get; init; }
#endif
}
