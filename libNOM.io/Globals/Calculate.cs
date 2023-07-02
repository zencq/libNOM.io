namespace libNOM.io.Globals;


public static class Calculate
{
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
    /// Calculates the base version of a save, based on the in-file version and a specified game mode and season.
    /// </summary>
    /// <param name="version">Version as specified in-file.</param>
    /// <param name="mode"></param>
    /// <param name="season"></param>
    /// <returns></returns>
    internal static int CalculateBaseVersion(int version, int mode, int season)
    {
        // Only Permadeath and Seasonal still have their game mode offset in Waypoint (4.00) and up.
        if (version >= Constant.THRESHOLD_WAYPOINT_GAMEMODE && mode < Constant.GAMEMODE_INT_PERMADEATH)
        {
            mode = Constant.GAMEMODE_INT_NORMAL;
        }
        return version - ((mode + (season * Constant.OFFSET_SEASON)) * Constant.OFFSET_GAMEMODE);
    }

    #endregion

    #region Version

    /// <inheritdoc cref="CalculateVersion(int, int, int)"/>
    internal static int CalculateVersion(int baseVersion, PresetGameModeEnum? mode, SeasonEnum season)
    {
        if (mode is null)
            return baseVersion;

        return CalculateVersion(baseVersion, mode.Numerate(), season.Numerate());
    }
    /// <inheritdoc cref="CalculateVersion(int, int, int)"/>
    internal static int CalculateVersion(int baseVersion, PresetGameModeEnum? mode, int season)
    {
        if (mode is null)
            return baseVersion;

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
        if (mode == (int)(PresetGameModeEnum.Seasonal))
            return baseVersion + ((mode + (season * Constant.OFFSET_SEASON)) * Constant.OFFSET_GAMEMODE);

        // Only Permadeath and Seasonal still have their game mode offset in Waypoint (4.00) and up.
        if (mode == (int)(PresetGameModeEnum.Permadeath) || baseVersion < Constant.THRESHOLD_WAYPOINT)
            return baseVersion + (mode * Constant.OFFSET_GAMEMODE);

        return baseVersion + ((int)(PresetGameModeEnum.Normal) * Constant.OFFSET_GAMEMODE);
    }

    #endregion
}
