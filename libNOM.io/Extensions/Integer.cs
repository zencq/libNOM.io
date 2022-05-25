namespace libNOM.io.Extensions;


internal static class IntegerExtensions
{
    /// <summary>
    /// Determines whether the number is high enough to be the specified game mode.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="mode"></param>
    /// <returns></returns>
    internal static bool IsGameMode(this int input, PresetGameModeEnum mode)
    {
        return input > (Global.THRESHOLD + (mode.Numerate() * Global.OFFSET_GAMEMODE));
    }
}
