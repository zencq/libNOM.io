using System.Text.RegularExpressions;

namespace libNOM.io.Meta;


internal static partial class SaveName
{
    #region Regex

#if NETSTANDARD2_0_OR_GREATER
#pragma warning disable IDE0300 // Use collection expression for array
    private static readonly Regex[] RegexesSaveName = new Regex[] { // keep this format to have Regex syntax highlighting
        new("\\\"Pk4\\\":\\\"(.*?)\\\",", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100)),
        new("\\\"SaveName\\\":\\\"(.*?)\\\",", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100)),
    };
#pragma warning restore IDE0300
#else
    [GeneratedRegex("\\\"Pk4\\\":\\\"(.*?)\\\",", RegexOptions.Compiled, 100)]
    private static partial Regex RegexObfuscatedSaveName();

    [GeneratedRegex("\\\"SaveName\\\":\\\"(.*?)\\\",", RegexOptions.Compiled, 100)]
    private static partial Regex RegexPlaintextSaveName();
        
    private static readonly Regex[] RegexesSaveName = [
        RegexObfuscatedSaveName(),
        RegexPlaintextSaveName(),
    ];
#endif

    #endregion

    #region Getter

    /// <summary>
    /// Gets the in-file name of the save.
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    internal static string Get(string? json) => RegexesSaveName.Match(json)?.ToStringValue() ?? string.Empty;

    #endregion
}
