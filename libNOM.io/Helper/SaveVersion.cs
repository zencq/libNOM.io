﻿using System.Text.RegularExpressions;
using Regex = System.Text.RegularExpressions.Regex;

namespace libNOM.io.Helper;


internal static partial class SaveVersion
{
    #region Regex

#if NETSTANDARD2_0_OR_GREATER || NET6_0
#pragma warning disable IDE0300 // Use collection expression for array
    private static readonly Regex[] Regexes = new Regex[] { // keep this format to have Regex syntax highlighting
        new("\\\"F2P\\\":(\\d{4,}),", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100)),
        new("\\\"Version\\\":(\\d{4,}),", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100)),
    };
#pragma warning restore IDE0300
#else
    [GeneratedRegex("\\\"F2P\\\":(\\d{4,}),", RegexOptions.Compiled, 100)]
    private static partial Regex RegexObfuscated();

    [GeneratedRegex("\\\"Version\\\":(\\d{4,}),", RegexOptions.Compiled, 100)]
    private static partial Regex RegexPlaintext();
        
    private static readonly Regex[] Regexes = [
        RegexObfuscated(),
        RegexPlaintext(),
    ];
#endif

    #endregion

    #region Calculate

    /// <summary>
    /// Calculates the in-file version based on the base version, game mode, and season.
    /// </summary>
    /// <param name="container"></param>
    /// <returns></returns>
    internal static int Calculate(Container container)
    {
        // Season  1 =    7205 = BaseVersion + (6 * 512) + ( 0 * 128 * 512) = BaseVersion + 3072 + ( 1 * 65536)
        // Season  2 =  138277 = BaseVersion + (6 * 512) + ( 2 * 128 * 512) = BaseVersion + 3072 + ( 2 * 65536)
        // Season  3 =  203815 = BaseVersion + (6 * 512) + ( 3 * 128 * 512) = BaseVersion + 3072 + ( 3 * 65536)
        // Season  4 =  269351 = BaseVersion + (6 * 512) + ( 4 * 128 * 512) = BaseVersion + 3072 + ( 4 * 65536)
        // Season 23 = 1514546 = BaseVersion + (6 * 512) + (23 * 128 * 512) = BaseVersion + 3072 + (23 * 65536)

        // Since Omega only Permadeath has still its own offset.
        if (container.GameMode == PresetGameModeEnum.Seasonal && !container.IsVersion450Omega)
            return container.BaseVersion + Constants.OFFSET_GAMEMODE_SEASONAL + ((int)(container.Season) * Constants.OFFSET_MULTIPLICATION_GAMEMODE_SEASON);

        // Since Waypoint only Permadeath and Seasonal still have their own offset.
        if (container.GameMode == PresetGameModeEnum.Permadeath || !container.IsVersion400Waypoint)
            return container.BaseVersion + ((int)(container.GameMode) * Constants.OFFSET_GAMEMODE);

        return container.BaseVersion + (Constants.GAMEMODE_INT_NORMAL * Constants.OFFSET_GAMEMODE);
    }

    #endregion

    #region Getter

    /// <summary>
    /// Gets the in-file version of the save.
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    internal static int Get(string? json)
    {
        if (json is not null)
        {
            foreach (var regex in Regexes)
                if (Extensions.RegexExtensions.MatchToInt32(regex, json, out int result))
                    return result;
        }
        return 0;
    }

    #endregion
}
