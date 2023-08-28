namespace libNOM.io.Globals;


internal static class Calculate
{
    #region BaseVersion

    /// <inheritdoc cref="CalculateBaseVersion(int, short, short)"/>
    internal static int CalculateBaseVersion(Container container) => CalculateBaseVersion(container.SaveVersion, (short)(container.GameMode), (short)(container.Season));

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

    #region SeasonEnum

    /// <summary>
    /// Calculates the season (Expedition) for the specified container.
    /// </summary>
    /// <param name="container"></param>
    /// <returns></returns>
    internal static SeasonEnum CalculateSeason(Container container)
    {
        if (container.GameMode < PresetGameModeEnum.Seasonal)
            return SeasonEnum.None;

        var futureSeason = (short)(SeasonEnum.Future);
        var i = (short)(SeasonEnum.Pioneers); // first season

        // Latest stop if negative but should usually be stopped before with future season.
        while (CalculateBaseVersion(container.SaveVersion, Constants.GAMEMODE_INT_SEASONAL, i) is int baseVersion and > 0)
        {
            if (i >= futureSeason)
                return SeasonEnum.Future;

            if (baseVersion.IsBaseVersion())
                return (SeasonEnum)(i);

            i++;
        }

        return SeasonEnum.None;
    }

    #endregion

    #region Version

    /// <inheritdoc cref="CalculateSaveVersion(int, short, short)"/>
    internal static int CalculateSaveVersion(int baseVersion, PresetGameModeEnum mode, SeasonEnum season) => CalculateSaveVersion(baseVersion, (short)(mode), (short)(season));

    /// <inheritdoc cref="CalculateSaveVersion(int, short, short)"/>
    internal static int CalculateSaveVersion(Container container) => CalculateSaveVersion(container.BaseVersion, (short)(container.GameMode), (short)(container.Season));

    /// <summary>
    /// Calculates the in-file version based on the base version of a save and a specified game mode and season.
    /// </summary>
    /// <param name="baseVersion">Base version of a save.</param>
    /// <param name="mode"></param>
    /// <param name="season"></param>
    /// <returns></returns>
    internal static int CalculateSaveVersion(int baseVersion, short mode, short season)
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
