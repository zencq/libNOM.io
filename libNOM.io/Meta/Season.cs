using System.Text.RegularExpressions;

using Newtonsoft.Json.Linq;

namespace libNOM.io.Meta;


internal static partial class Season
{
    #region Regex

#if NETSTANDARD2_0_OR_GREATER || NET6_0
#pragma warning disable IDE0300 // Use collection expression for array
    private static readonly Regex[] RegexesSeasonId = new Regex[] { // keep this format to have Regex syntax highlighting
        new("\\\"gou\\\":(\\d{4,}),", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100)),
        new("\\\"SeasonId\\\":(\\d{4,}),", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100)),
    };
#pragma warning restore IDE0300
#else
    [GeneratedRegex("\\\"gou\\\":\\\"(.*?)\\\",", RegexOptions.Compiled, 100)]
    private static partial Regex RegexObfuscatedSeasonId();

    [GeneratedRegex("\\\"SeasonId\\\":\\\"(.*?)\\\",", RegexOptions.Compiled, 100)]
    private static partial Regex RegexPlaintextSeasonId();

    private static readonly Regex[] RegexesSeasonId = [
        RegexObfuscatedSeasonId(),
        RegexPlaintextSeasonId(),
    ];
#endif

    #endregion

    #region Getter

    /// <summary>
    /// Gets the in-file game mode of the save.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <returns></returns>
    internal static SeasonEnum Get(JObject? jsonObject)
    {
        if (jsonObject is null)
            return SeasonEnum.None;

        // SEASON_ID works in most case, SEASON_ID_LEGACY was only used for the first few
        var id = jsonObject.GetValue<int?>("SEASON_ID", SaveContextQueryEnum.Season) ?? jsonObject.GetValue<int?>("SEASON_ID_LEGACY") ?? 0;
        return SeasonIdToEnum(id);
    }

    /// <inheritdoc cref="Get(JObject?)"/>
    /// <param name="json"></param>
    internal static SeasonEnum Get(string? json)
    {
        if (RegexesSeasonId.Match(json)?.ToInt32Value() is int id)
            return SeasonIdToEnum(id);

        return SeasonEnum.None;
    }

    private static SeasonEnum SeasonIdToEnum(int id)
    {
        if (id == 1) // there is no entry in the enum with value 1
            return SeasonEnum.Pioneers;

        if (id < (int)(SeasonEnum.Future))
            return (SeasonEnum)(id);

        return SeasonEnum.Future;
    }

    #endregion
}
