namespace libNOM.io.Extensions;


internal static class IntegerExtensions
{

    #region typeof(int)

    /// <summary>
    /// Determines whether the number is between the vanilla thresholds to be a valid BaseVersion.
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    internal static bool IsBaseVersion(this int self)
    {
        return self is >= Constants.THRESHOLD_VANILLA and < Constants.THRESHOLD_VANILLA_GAMEMODE;
    }

    /// <summary>
    /// Determines whether the number is high enough to be the specified game mode.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="mode"></param>
    /// <returns></returns>
    internal static bool IsGameMode(this int self, PresetGameModeEnum mode)
    {
        return self >= (Constants.THRESHOLD_VANILLA + ((int)(mode) * Constants.OFFSET_GAMEMODE));
    }

    #endregion

    #region typeof(long)

    internal static long GetBlobTicks(this long self)
    {
        return self - self % (long)(Math.Pow(10, 4));
    }

    #endregion

    #region typeof(ushort)

    /// <summary>
    /// Determines whether the number is in the range of the <see cref="PresetGameModeEnum"/>.
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    internal static bool IsGameMode(this ushort self)
    {
        return self is >= ((ushort)(PresetGameModeEnum.Normal)) and <= ((ushort)(PresetGameModeEnum.Seasonal));
    }

    /// <summary>
    /// Determines whether the number is in the range of the <see cref="SeasonEnum"/>.
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    internal static bool IsSeason(this ushort self)
    {
        return self is >= ((ushort)(SeasonEnum.Pioneers)) and <= ((ushort)(SeasonEnum.Future));
    }

    #endregion
}
