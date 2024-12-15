using System.Text.RegularExpressions;

namespace libNOM.io.Meta;


internal static partial class GameMode
{
    #region Regex

#if NETSTANDARD2_0_OR_GREATER
#pragma warning disable IDE0300 // Use collection expression for array
    private static readonly Regex[] RegexesGameMode = new Regex[] { // keep this format to have Regex syntax highlighting
        new("\\\"idA\\\":\\\"(\\d{1})\\\",", RegexOptions.Compiled, TimeSpan.FromMilliseconds(1000)),
        new("\\\"GameMode\\\":\\\"(\\d{1})\\\",", RegexOptions.Compiled, TimeSpan.FromMilliseconds(1000)),
    };
#pragma warning restore IDE0300
#else
    [GeneratedRegex("\\\"idA\\\":\\\"(}´\\d{1})\\\",", RegexOptions.Compiled, 1000)]
    private static partial Regex RegexObfuscatedGameMode();

    [GeneratedRegex("\\\"GameMode\\\":\\\"(\\d{1})\\\",", RegexOptions.Compiled, 1000)]
    private static partial Regex RegexPlaintextGameMode();

    private static readonly Regex[] RegexesGameMode = [
        RegexObfuscatedGameMode(),
        RegexPlaintextGameMode(),
    ];
#endif

    #endregion

    #region Getter

    /// <summary>
    /// Gets the in-file game mode of the save.
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    internal static PresetGameModeEnum Get(string? json)
    {
        if (RegexesGameMode.Matches(json) is MatchCollection collection)
            // If more than one match, there is an expedition started from an existing save and we have to check which one to use.
            if (collection.Count > 1)
            {
                // Main is always first and Season second.
                return Meta.Context.GetActive(json) switch
                {
                    SaveContextQueryEnum.Main => (PresetGameModeEnum)(collection[0].ToInt32Value()),
                    SaveContextQueryEnum.Season => (PresetGameModeEnum)(collection[1].ToInt32Value()),
                    _ => PresetGameModeEnum.Unspecified,
                };
            }
            else
                return (PresetGameModeEnum)(collection[0].ToInt32Value());

        return PresetGameModeEnum.Unspecified;
    }

    #endregion
}
