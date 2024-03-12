namespace libNOM.io.Meta;


internal static class BaseVersion
{
    #region Calculate

    /// <summary>
    /// Calculates the base version of a save, based on the in-file version and a specified game mode and season.
    /// </summary>
    /// <param name="container"></param>
    /// <returns></returns>
    internal static int Calculate(Container container)
    {
        return container.SaveVersion - (((int)(container.GameMode) + ((int)(container.Season) * Constants.OFFSET_SEASON)) * Constants.OFFSET_GAMEMODE);
    }

    #endregion
}
