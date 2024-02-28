using System.Text.RegularExpressions;

namespace libNOM.io.Meta;


internal static partial class GameMode
{
    #region Regex

#if NETSTANDARD2_0_OR_GREATER || NET6_0
#pragma warning disable IDE0300 // Use collection expression for array
    private static readonly Regex[] RegexesActiveContext = new Regex[] { // keep this format to have Regex syntax highlighting
        new("\\\"XTp\\\":\\\"(\\w{4,8})\\\",", RegexOptions.Compiled, TimeSpan.FromMilliseconds(10)),
        new("\\\"ActiveContext\\\":\\\"(\\w{4,8})\\\",", RegexOptions.Compiled, TimeSpan.FromMilliseconds(10)),
    };

    private static readonly Regex[] Regexes = new Regex[] { // keep this format to have Regex syntax highlighting
        new("\\\"idA\\\":\\\"(\\d{1})\\\",", RegexOptions.Compiled, TimeSpan.FromMilliseconds(1000)),
        new("\\\"GameMode\\\":\\\"(\\d{1})\\\",", RegexOptions.Compiled, TimeSpan.FromMilliseconds(1000)),
    };
#pragma warning restore IDE0300
#else
    [GeneratedRegex("\\\"XTp\\\":\\\"(\\w{4,8})\\\",", RegexOptions.Compiled, 10)]
    private static partial Regex RegexObfuscatedActiveContext();

    [GeneratedRegex("\\\"ActiveContext\\\":\\\"(\\w{4,8})\\\",", RegexOptions.Compiled, 10)]
    private static partial Regex RegexPlaintextActiveContext();

    [GeneratedRegex("\\\"idA\\\":\\\"(}´\\d{1})\\\",", RegexOptions.Compiled, 1000)]
    private static partial Regex RegexObfuscated();

    [GeneratedRegex("\\\"GameMode\\\":\\\"(\\d{1})\\\",", RegexOptions.Compiled, 1000)]
    private static partial Regex RegexPlaintext();

    private static readonly Regex[] RegexesActiveContext = [
        RegexObfuscatedActiveContext(),
        RegexPlaintextActiveContext(),
    ];

    private static readonly Regex[] Regexes = [
        RegexObfuscated(),
        RegexPlaintext(),
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
        if (Regexes.Matches(json) is MatchCollection collection)
            // If more than one match, there is an expedition started from an existing save and we have to check which one to use.
            if (collection.Count > 1)
            {
                var context = SaveContextQueryEnum.DontCare;

                if (RegexesActiveContext.Match(json)?.ToStringValue() is string value)
                    context = EnumExtensions.Parse<SaveContextQueryEnum>(value) ?? SaveContextQueryEnum.DontCare;

                // Main is always first and Season second.
                return context switch
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
