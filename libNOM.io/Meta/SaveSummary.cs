using System.Text.RegularExpressions;

namespace libNOM.io.Meta;


internal static partial class SaveSummary
{
    #region Regex

#if NETSTANDARD2_0_OR_GREATER || NET6_0
#pragma warning disable IDE0300 // Use collection expression for array
    private static readonly Regex[] RegexesSaveSummary = new Regex[] { // keep this format to have Regex syntax highlighting
        new("\\\"n:R\\\":\\\"(.*?)\\\",", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100)),
        new("\\\"SaveSummary\\\":\\\"(.*?)\\\",", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100)),
    };
#pragma warning restore IDE0300
#else
    [GeneratedRegex("\\\"n:R\\\":\\\"(.*?)\\\",", RegexOptions.Compiled, 1000)]
    private static partial Regex RegexObfuscatedSaveSummary();

    [GeneratedRegex("\\\"SaveSummary\\\":\\\"(.*?)\\\",", RegexOptions.Compiled, 1000)]
    private static partial Regex RegexPlaintextSaveSummary();
        
    private static readonly Regex[] RegexesSaveSummary = [
        RegexObfuscatedSaveSummary(),
        RegexPlaintextSaveSummary(),
    ];
#endif

    #endregion

    #region Getter

    /// <summary>
    /// Gets the in-file summary of the save.
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    internal static string Get(string? json) => RegexesSaveSummary.Match(json)?.ToStringValue() ?? string.Empty;

    #endregion
}
