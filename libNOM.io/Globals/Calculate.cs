namespace libNOM.io.Globals;


internal static class Calculate
{
    #region BaseVersion

    /// <inheritdoc cref="CalculateBaseVersion(int, short, short)"/>
    internal static int CalculateBaseVersion(int version, PresetGameModeEnum mode, SeasonEnum season) => CalculateBaseVersion(version, (short)(mode), (short)(season));

    /// <inheritdoc cref="CalculateBaseVersion(int, short, short)"/>
    internal static int CalculateBaseVersion(int version, PresetGameModeEnum mode, short season) => CalculateBaseVersion(version, (short)(mode), season);

    /// <inheritdoc cref="CalculateBaseVersion(int, short, short)"/>
    internal static int CalculateBaseVersion(int version, short mode, SeasonEnum season) => CalculateBaseVersion(version, mode, (short)(season));

    /// <summary>
    /// Calculates the base version of a save, based on the in-file version and a specified game mode and season.
    /// </summary>
    /// <param name="version">Version as specified in-file.</param>
    /// <param name="mode"></param>
    /// <param name="season"></param>
    /// <returns></returns>
    internal static int CalculateBaseVersion(int version, short mode, short season)
    {
        // Only Permadeath and Seasonal still have their game mode offset in Waypoint (4.00) and up and custom game mode was introduced.
        var baseVersion = version - ((mode + (season * Constants.OFFSET_SEASON)) * Constants.OFFSET_GAMEMODE);
        if (baseVersion < Constants.THRESHOLD_VANILLA && mode < Constants.GAMEMODE_INT_PERMADEATH || baseVersion >= Constants.THRESHOLD_WAYPOINT_GAMEMODE && mode == Constants.GAMEMODE_INT_UNSPECIFIED)
            baseVersion = CalculateBaseVersion(version, Constants.GAMEMODE_INT_NORMAL, season);

        return baseVersion;


        //// Only Permadeath and Seasonal still have their game mode offset in Waypoint (4.00) and up.
        //if (version >= Constants.THRESHOLD_WAYPOINT_GAMEMODE && mode < Constants.GAMEMODE_INT_PERMADEATH)
        //{
        //    mode = Constants.GAMEMODE_INT_NORMAL;
        //}
        //return version - ((mode + (season * Constants.OFFSET_SEASON)) * Constants.OFFSET_GAMEMODE);
    }

    #endregion

    #region Version

    /// <inheritdoc cref="CalculateVersion(int, short, short)"/>
    internal static int CalculateVersion(int baseVersion, PresetGameModeEnum mode, SeasonEnum season) => CalculateVersion(baseVersion, (short)(mode), (short)(season));

    /// <inheritdoc cref="CalculateVersion(int, short, short)"/>
    internal static int CalculateVersion(int baseVersion, PresetGameModeEnum mode, short season) => CalculateVersion(baseVersion, (short)(mode), season);

    /// <inheritdoc cref="CalculateVersion(int, short, short)"/>
    internal static int CalculateVersion(int baseVersion, short mode, SeasonEnum season) => CalculateVersion(baseVersion, mode, (short)(season));

    /// <summary>
    /// Calculates the in-file version based on the base version of a save and a specified game mode and season.
    /// </summary>
    /// <param name="baseVersion">Base version of a save.</param>
    /// <param name="mode"></param>
    /// <param name="season"></param>
    /// <returns></returns>
    internal static int CalculateVersion(int baseVersion, short mode, short season)
    {
        // Season 1 =   7205 = BaseVersion + (6 * 512) + (  0 * 512) = BaseVersion + ((6 + (0 * 128)) * 512)
        // Season 2 = 138277 = BaseVersion + (6 * 512) + (256 * 512) = BaseVersion + ((6 + (2 * 128)) * 512)
        // Season 3 = 203815 = BaseVersion + (6 * 512) + (384 * 512) = BaseVersion + ((6 + (3 * 128)) * 512)
        // Season 4 = 269351 = BaseVersion + (6 * 512) + (512 * 512) = BaseVersion + ((6 + (4 * 128)) * 512)
        if (mode == Constants.GAMEMODE_INT_SEASONAL)
            return baseVersion + ((mode + (season * Constants.OFFSET_SEASON)) * Constants.OFFSET_GAMEMODE);

        // Only Permadeath and Seasonal still have their game mode offset in Waypoint (4.00) and up.
        if (mode == Constants.GAMEMODE_INT_PERMADEATH || baseVersion < Constants.THRESHOLD_WAYPOINT)
            return baseVersion + (mode * Constants.OFFSET_GAMEMODE);

        return baseVersion + ((int)(PresetGameModeEnum.Normal) * Constants.OFFSET_GAMEMODE);
    }

    #endregion
}
