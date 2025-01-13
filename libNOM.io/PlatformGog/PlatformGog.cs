namespace libNOM.io;


/// <summary>
/// Implementation for the GOG.com platform.
/// </summary>
// This partial class contains all related code.
public partial class PlatformGog : PlatformSteam
{
    #region Constant

    internal new const string ACCOUNT_PATTERN = "DefaultUser";

    private static readonly string GALAXY_CONFIG_JSON = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GOG.com", "Galaxy", "Configuration", "config.json");

    public static new readonly string PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HelloGames", "NMS");

    #endregion
}
