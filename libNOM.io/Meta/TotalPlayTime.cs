using System.Text.RegularExpressions;

namespace libNOM.io.Meta;


internal static partial class TotalPlayTime
{
    #region Regex

#if NETSTANDARD2_0_OR_GREATER
#pragma warning disable IDE0300 // Use collection expression for array
    private static readonly Regex[] RegexesTotalPlayTime = new Regex[] { // keep this format to have Regex syntax highlighting
        new("\\\"Lg8\\\":(\\d+),", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100)),
        new("\\\"TotalPlayTime\\\":(\\d+),", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100)),
    };
#pragma warning restore IDE0300
#else
    [GeneratedRegex("\\\"Lg8\\\":(\\d+),", RegexOptions.Compiled, 100)]
    private static partial Regex RegexObfuscatedTotalPlayTime();

    [GeneratedRegex("\\\"TotalPlayTime\\\":(\\d+),", RegexOptions.Compiled, 100)]
    private static partial Regex RegexPlaintextTotalPlayTime();

    private static readonly Regex[] RegexesTotalPlayTime = [
        RegexObfuscatedTotalPlayTime(),
        RegexPlaintextTotalPlayTime(),
    ];
#endif

    #endregion

    #region Total Play Time

    /// <summary>
    /// Gets the in-file total play time of the save.
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public static ulong Get(string? json) => RegexesTotalPlayTime.Match(json)?.ToUInt64Value() ?? 0;

    #endregion
}
