using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace libNOM.io;


public class PlatformGog : PlatformSteam
{
    #region Constant

    internal new const string ACCOUNT_PATTERN = "DefaultUser";

    internal static new readonly string PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HelloGames", "NMS");

    #endregion

    #region Field

    private string? _userId; // both will be set if GOG Galaxy config file exists
    private string? _username;

    #endregion

    #region Property

    #region Platform Indicator

    // public //

    public override PlatformEnum PlatformEnum { get; } = PlatformEnum.Gog;

    // protected //

    protected override string? PlatformArchitecture { get; } = "Win|Final";

    protected override string? PlatformProcessPath { get; } = @"GOG Galaxy\No Man's Sky\Binaries\NMS.exe";

    protected override string PlatformToken { get; } = "GX";

    #endregion

    #endregion

    // //

    #region Constructor

    public PlatformGog() : base() { }

    public PlatformGog(string path) : base(path) { }

    public PlatformGog(string path, PlatformSettings platformSettings) : base(path, platformSettings) { }

    public PlatformGog(DirectoryInfo directory) : base(directory) { }

    public PlatformGog(DirectoryInfo directory, PlatformSettings platformSettings) : base(directory, platformSettings) { }

    protected override void InitializeComponent(DirectoryInfo? directory, PlatformSettings? platformSettings)
    {
        // Proceed to base method even if no directory.
        if (directory is not null && platformSettings?.UseExternalSourcesForUserIdentification == true)
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GOG.com", "Galaxy", "Configuration", "config.json");
            if (File.Exists(path))
            {
                var jsonObject = JsonConvert.DeserializeObject(File.ReadAllText(path)) as JObject;
                _userId = jsonObject?.GetValue<string>("userId");
                _username = jsonObject?.GetValue<string>("username");
            }
        }

        base.InitializeComponent(directory, platformSettings);
    }

    #endregion

    // // User Identification

    #region User Identification

    protected override string GetUserIdentification(JObject jsonObject, string key)
    {
        // Base call not as default as _userId and _username can also be null.
        var result = key switch
        {
            "UID" => _userId,
            "USN" => _username,
            _ => null,
        } ?? base.GetUserIdentification(jsonObject, key);

        // Fallback as it was the default for a long time and could not be changed.
        if (key == "USN" && string.IsNullOrEmpty(result))
            result = "Explorer";

        return result ?? string.Empty;
    }

    protected override string[] GetIntersectionExpressionsByBase(JObject jsonObject)
    {
        if (_userId is null)
            return base.GetIntersectionExpressionsByBase(jsonObject);
        return
        [
            Json.GetPath("INTERSECTION_PERSISTENT_PLAYER_BASE_OWNERSHIP_EXPRESSION_TYPE_OR_TYPE", jsonObject, PersistentBaseTypesEnum.HomePlanetBase, PersistentBaseTypesEnum.FreighterBase),
            Json.GetPath("INTERSECTION_PERSISTENT_PLAYER_BASE_OWNERSHIP_EXPRESSION_THIS_UID", jsonObject, _userId),
        ];
    }

    protected override string[] GetIntersectionExpressionsByDiscovery(JObject jsonObject)
    {
        if (_userId is null)
            return base.GetIntersectionExpressionsByDiscovery(jsonObject);
        return
        [
            Json.GetPath("INTERSECTION_DISCOVERY_DATA_OWNERSHIP_EXPRESSION_THIS_UID", jsonObject, _userId),
        ];
    }

    protected override string[] GetIntersectionExpressionsBySettlement(JObject jsonObject)
    {
        if (_userId is null)
            return base.GetIntersectionExpressionsByDiscovery(jsonObject);
        return
        [
            Json.GetPath("INTERSECTION_SETTLEMENT_OWNERSHIP_EXPRESSION_THIS_UID", jsonObject, _userId),
        ];
    }

    #endregion
}
