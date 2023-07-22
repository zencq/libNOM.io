namespace libNOM.io.Extensions;


internal static class IntegerExtensions
{
    /// <summary>
    /// Determines whether the number is high enough to be the specified game mode.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="mode"></param>
    /// <returns></returns>
    internal static bool IsGameMode(this int self, PresetGameModeEnum mode)
    {
        return self >= (Globals.Constants.THRESHOLD_VANILLA + ((int)(mode) * Globals.Constants.OFFSET_GAMEMODE));
    }
}
