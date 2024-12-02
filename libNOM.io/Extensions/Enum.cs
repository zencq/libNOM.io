namespace libNOM.io.Extensions;


public static class EnumExtensions
{
    #region Enum

    internal static T[] GetValues<T>() where T : struct, Enum
    {
#if NETSTANDARD2_0_OR_GREATER
        return (T[])(Enum.GetValues(typeof(T)));
#else
        return Enum.GetValues<T>();
#endif
    }

    internal static T? Parse<T>(string? value) where T : struct, Enum
    {
        if (value is null)
            return null;

#if NETSTANDARD2_0
        return (T)(Enum.Parse(typeof(T), value));
#else
        return Enum.Parse<T>(value);
#endif
    }

    #endregion

    #region Season

    /// <summary>
    /// Maps the internal number to the one it actually is (e.g. Cartographers is always 3rd, Liquidators 14th).
    /// </summary>
    /// <param name="season"></param>
    /// <returns></returns>
    // EXTERNAL RELEASE: Update if a new set of redux expeditions is added.
    public static int GetActualNumber(this SeasonEnum season)
    {
        return season switch
        {
            >= SeasonEnum.OmegaRedux => (int)(season) - 19, // 31 > 12
            >= SeasonEnum.BeachheadRedux2024 => (int)(season) - 28, // 30 > 2
            >= SeasonEnum.Omega => (int)(season) - 13, // 25 > 12
            >= SeasonEnum.VoyagersRedux => (int)(season) - 12, // 23 > 11
            >= SeasonEnum.CartographersRedux2023 => (int)(season) - 19, // 22 > 3
            >= SeasonEnum.UtopiaRedux => (int)(season) - 11, // 20 > 9
            >= SeasonEnum.ExobiologyRedux => (int)(season) - 8, // 13 > 5
            >= SeasonEnum.PioneersRedux => (int)(season) - 4, // 5 > 1
            >= SeasonEnum.Pioneers => (int)(season) + 1, // 0 > 1
            _ => 0,
        };
    }

    #endregion
}
