global using libNOM.io.Data;
global using libNOM.io.Enums;
global using libNOM.io.Extensions;

using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace libNOM.io;


internal static class Global
{
    #region Constant

    internal const string FILE_TIMESTAMP_FORMAT = "yyyyMMddHHmmss";

    internal const string HEADER_PLAINTEXT = "{\"Version\":";
    internal const string HEADER_PLAINTEXT_OBFUSCATED = "{\"F2P\":";
    internal const uint HEADER_SAVE_STREAMING_CHUNK = 0xFEEDA1E5U; // 4276986341
    internal const string HEADER_SAVEWIZARD = "NOMANSKY";

    internal const int OFFSET_GAMEMODE = 512;
    internal const int OFFSET_INDEX = 2;
    internal const int OFFSET_SEASON = 128;

    internal const int THRESHOLD = 4100;
    internal const int THRESHOLD_GAMEMODE = THRESHOLD + OFFSET_GAMEMODE;

    private static readonly Regex REGEX_TOTALPLAYTIME = new("\\\"Lg8\\\":(\\d+),", RegexOptions.Compiled, TimeSpan.FromSeconds(1));
    private static readonly Regex REGEX_VERSION = new("\\\"F2P\\\":(\\d{4,}),", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    #endregion

    #region BaseVersion

    /// <inheritdoc cref="CalculateBaseVersion(int, int, int)"/>
    internal static int CalculateBaseVersion(int version, PresetGameModeEnum mode, SeasonEnum season)
    {
        return CalculateBaseVersion(version, mode.Numerate(), season.Numerate());
    }
    /// <inheritdoc cref="CalculateBaseVersion(int, int, int)"/>
    internal static int CalculateBaseVersion(int version, PresetGameModeEnum mode, int season)
    {
        return CalculateBaseVersion(version, mode.Numerate(), season);
    }
    /// <inheritdoc cref="CalculateBaseVersion(int, int, int)"/>
    internal static int CalculateBaseVersion(int version, int mode, SeasonEnum season)
    {
        return CalculateBaseVersion(version, mode, season.Numerate());
    }
    /// <summary>
    /// Calculates the base version of a save based on the in-file version and a specified game mode and season.
    /// </summary>
    /// <param name="version">Version as specified in-file.</param>
    /// <param name="mode"></param>
    /// <param name="season"></param>
    /// <returns></returns>
    internal static int CalculateBaseVersion(int version, int mode, int season)
    {
        return version - ((mode + (season * OFFSET_SEASON)) * OFFSET_GAMEMODE);
    }

    #endregion

    #region GameMode

    /// <summary>
    /// Gets the game mode of a save based on the in-file version.
    /// </summary>
    /// <param name="container"></param>
    /// <returns></returns>
    internal static PresetGameModeEnum GetGameModeEnum(Container container)
    {
        if (container.Version.IsGameMode(PresetGameModeEnum.Seasonal))
            return PresetGameModeEnum.Seasonal;

        if (container.Version.IsGameMode(PresetGameModeEnum.Permadeath))
            return PresetGameModeEnum.Permadeath;

        if (container.Version.IsGameMode(PresetGameModeEnum.Ambient))
            return PresetGameModeEnum.Ambient;

        if (container.Version.IsGameMode(PresetGameModeEnum.Survival))
            return PresetGameModeEnum.Survival;

        if (container.Version.IsGameMode(PresetGameModeEnum.Creative))
            return PresetGameModeEnum.Creative;

        if (container.Version.IsGameMode(PresetGameModeEnum.Normal))
            return PresetGameModeEnum.Normal;

        return PresetGameModeEnum.Unspecified;
    }

    #endregion

    #region TotalPlayTime

    /// <summary>
    /// Gets the in-file version.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <returns></returns>
    internal static long GetTotalPlayTime(JObject jsonObject)
    {
        return jsonObject.SelectToken(jsonObject.UseMapping() ? "PlayerStateData.TotalPlayTime" : "6f=.Lg8")?.Value<long>() ?? 0;
    }

    /// <inheritdoc cref="GetTotalPlayTime(JObject)"/>
    internal static long GetTotalPlayTime(string json)
    {
        var match = REGEX_TOTALPLAYTIME.Match(json);
        return match.Success ? System.Convert.ToInt64(match.Groups[1].Value) : 0;
    }

    #endregion

    #region Version

    /// <inheritdoc cref="CalculateVersion(int, int, int)"/>
    internal static int CalculateVersion(int baseVersion, PresetGameModeEnum mode, SeasonEnum season)
    {
        return CalculateVersion(baseVersion, mode.Numerate(), season.Numerate());
    }
    /// <inheritdoc cref="CalculateVersion(int, int, int)"/>
    internal static int CalculateVersion(int baseVersion, PresetGameModeEnum mode, int season)
    {
        return CalculateVersion(baseVersion, mode.Numerate(), season);
    }
    /// <inheritdoc cref="CalculateVersion(int, int, int)"/>
    internal static int CalculateVersion(int baseVersion, int mode, SeasonEnum season)
    {
        return CalculateVersion(baseVersion, mode, season.Numerate());
    }
    /// <summary>
    /// Calculates the in-file version based on the base version of a save and a specified game mode and season.
    /// </summary>
    /// <param name="baseVersion">Base version of a save.</param>
    /// <param name="mode"></param>
    /// <param name="season"></param>
    /// <returns></returns>
    internal static int CalculateVersion(int baseVersion, int mode, int season)
    {
        // Season 1 =   7205 = BaseVersion + (6 * 512) + (  0 * 512) = BaseVersion + ((6 + (0 * 128)) * 512)
        // Season 2 = 138277 = BaseVersion + (6 * 512) + (256 * 512) = BaseVersion + ((6 + (2 * 128)) * 512)
        // Season 3 = 203815 = BaseVersion + (6 * 512) + (384 * 512) = BaseVersion + ((6 + (3 * 128)) * 512)
        // Season 4 = 269351 = BaseVersion + (6 * 512) + (512 * 512) = BaseVersion + ((6 + (4 * 128)) * 512)
        if (mode < (int)(PresetGameModeEnum.Seasonal))
        {
            return baseVersion + (mode * OFFSET_GAMEMODE);
        }
        return baseVersion + ((mode + (season * OFFSET_SEASON)) * OFFSET_GAMEMODE);
    }

    /// <summary>
    /// Gets the in-file version.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <returns></returns>
    internal static int GetVersion(JObject jsonObject)
    {
        return jsonObject.SelectToken(jsonObject.UseMapping() ? "Version" : "F2P")?.Value<int>() ?? -1;
    }

    /// <inheritdoc cref="GetVersion(JObject)"/>
    internal static int GetVersion(string json)
    {
        var match = REGEX_VERSION.Match(json);
        return match.Success ? System.Convert.ToInt32(match.Groups[1].Value) : -1;
    }

    #endregion
}
