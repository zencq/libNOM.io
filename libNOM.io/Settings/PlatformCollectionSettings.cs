namespace libNOM.io.Settings;


/// <summary>
/// Holds settings how a <see cref="PlatformCollection"/> should behave.
/// </summary>
public record class PlatformCollectionSettings
{
#if NETSTANDARD2_0_OR_GREATER
    /// <summary>
    /// Which platform to prefer if a location or file matches more than one.
    /// Default: Unknown
    /// </summary>
    public PlatformEnum PreferredPlatform { get; set; } = PlatformEnum.Unknown;

    /// <summary>
    /// Whether to analyze local platforms at start.
    /// Default: true
    /// </summary>
    public bool AnalyzeLocal { get; set; } = true;
#else
    /// <summary>
    /// Which platform to prefer if a location or file matches more than one.
    /// Default: Unknown
    /// </summary>
    public PlatformEnum PreferredPlatform { get; init; } = PlatformEnum.Unknown;

    /// <summary>
    /// Whether to analyze local platforms at start.
    /// Default: true
    /// </summary>
    public bool AnalyzeLocal { get; init; } = true;
#endif
}
