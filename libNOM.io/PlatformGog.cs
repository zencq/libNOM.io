using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace libNOM.io;


public class PlatformGog : PlatformSteam
{
    #region Constant

    #region Directory Data

    public new const string ACCOUNT_PATTERN = "DefaultUser";
    public static new readonly string PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HelloGames", "NMS");

    #endregion

    #endregion

    #region Field

    private string? _userId;
    private string? _username;

    #endregion

    #region Property

    #region Platform Indicator

    public override PlatformEnum PlatformEnum { get; } = PlatformEnum.Gog;

    protected override string? PlatformProcess { get; } = @"GOG Galaxy\No Man's Sky\Binaries\NMS.exe";

    protected override string PlatformToken { get; } = "GX";

    #endregion

    #endregion

    // //

    #region Constructor

    public PlatformGog(string path) : base(path) { }

    public PlatformGog(string path, PlatformSettings platformSettings) : base(path, platformSettings) { }

    public PlatformGog(DirectoryInfo directory) : base(directory) { }

    public PlatformGog(DirectoryInfo directory, PlatformSettings platformSettings) : base(directory, platformSettings) { }

    protected override void InitializeComponent(DirectoryInfo? directory, PlatformSettings? platformSettings)
    {
        // Proceed to base method even if no directory.
        if (directory is not null)
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GOG.com", "Galaxy", "Configuration", "config.json");
            if (File.Exists(path))
            {
                var jsonObject = JsonConvert.DeserializeObject(File.ReadAllText(path)) as JObject;

                _userId = jsonObject?.SelectToken("userId")?.Value<string>();
                _username = jsonObject?.SelectToken("username")?.Value<string>();
            }
        }

        base.InitializeComponent(directory, platformSettings);
    }

    #endregion

    // // User Identification

    #region User Identification

    protected override string GetUserIdentification(JObject jsonObject, string key)
    {
        if (key is "UID" && Settings.UseExternalSourcesForUserIdentification && _userId is not null)
            return _userId;

        if (key is "USN" && Settings.UseExternalSourcesForUserIdentification && _username is not null)
            return _username;

        var result = base.GetUserIdentification(jsonObject, key);
        if (!string.IsNullOrEmpty(result))
            return result;

        // Fallback as it was the default for a long time and could not be changed.
        if (key is "USN")
            return "Explorer";

        return result;
    }

    protected override IEnumerable<string> GetUserIdentificationByBase(JObject jsonObject, string key)
    {
        if (!Settings.UseExternalSourcesForUserIdentification || _userId is null)
            return base.GetUserIdentificationByBase(jsonObject, key);

        var usesMapping = jsonObject.UsesMapping();

        var path = usesMapping ? $"PlayerStateData.PersistentPlayerBases[?({{0}})].Owner.{key}" : $"6f=.F?0[?({{0}})].3?K.{key}";
        var expressions = new[]
        {
            usesMapping ? $"@.BaseType.PersistentBaseTypes == '{PersistentBaseTypesEnum.HomePlanetBase}' || @.BaseType.PersistentBaseTypes == '{PersistentBaseTypesEnum.FreighterBase}'" : $"@.peI.DPp == '{PersistentBaseTypesEnum.HomePlanetBase}' || @.peI.DPp == '{PersistentBaseTypesEnum.FreighterBase}'", // only with own base
            usesMapping ? $"@.Owner.UID == '{_userId}'" : $"@.3?K.K7E == '{_userId}'", // only with specified value
        };

        return GetUserIdentificationIntersection(jsonObject, path, expressions);
    }

    protected override IEnumerable<string> GetUserIdentificationByDiscovery(JObject jsonObject, string key)
    {
        if (!Settings.UseExternalSourcesForUserIdentification || _userId is null)
            return base.GetUserIdentificationByBase(jsonObject, key);

        var usesMapping = jsonObject.UsesMapping();

        var path = usesMapping ? $"DiscoveryManagerData.DiscoveryData-v1.Store.Record[?({{0}})].OWS.{key}" : $"fDu.ETO.OsQ.?fB[?({{0}})].ksu.{key}";
        var expressions = new[]
        {
            usesMapping ? $"@.OWS.UID == '{_userId}'" : $"@.ksu.K7E == '{_userId}'", // only with specified value
        };

        return GetUserIdentificationIntersection(jsonObject, path, expressions);
    }

    protected override IEnumerable<string> GetUserIdentificationBySettlement(JObject jsonObject, string key)
    {
        if (!Settings.UseExternalSourcesForUserIdentification || _userId is null)
            return base.GetUserIdentificationByBase(jsonObject, key);

        var usesMapping = jsonObject.UsesMapping();

        var path = usesMapping ? $"PlayerStateData.SettlementStatesV2[?({{0}})].Owner.{key}" : $"6f=.GQA[?({{0}})].3?K.{key}";
        var expressions = new[]
        {
            usesMapping ? $"@.Owner.UID == '{_userId}'" : $"@.3?K.K7E == '{_userId}'", // only with specified value
        };

        return GetUserIdentificationIntersection(jsonObject, path, expressions);
    }

    #endregion
}
