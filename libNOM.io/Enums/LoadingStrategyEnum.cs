namespace libNOM.io.Enums;


/// <summary>
/// Specifies strategies how to load and keep containers in a <see cref="Platform"/>.
/// </summary>
public enum LoadingStrategyEnum
{
    /// <summary>
    /// No save information and data loaded.
    /// </summary>
    Empty,
    /// <summary>
    /// All save information loaded but no data.
    /// </summary>
    Hollow,
    /// <summary>
    /// All save information loaded but only data of the most recent one.
    /// </summary>
    Current,
    /// <summary>
    /// All save information loaded and data for all previously loaded ones.
    /// </summary>
    Partial,
    /// <summary>
    /// All save information and data loaded.
    /// </summary>
    Full,
}
