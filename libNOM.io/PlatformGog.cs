using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace libNOM.io;


#region PlatformDirectoryDataSteam

internal record class PlatformDirectoryDataGog : PlatformDirectoryDataSteam
{
    internal override string DirectoryPathPattern { get; } = "DefaultUser";
}

#endregion

public class PlatformGog : PlatformSteam
{
    #region Field

    private string? _userId;
    private string? _username;

    #endregion

    #region Property

    #region Platform Indicator

    internal static new PlatformDirectoryData DirectoryData { get; } = new PlatformDirectoryDataGog();

    internal override PlatformDirectoryData PlatformDirectoryData { get; } = DirectoryData;

    public override PlatformEnum PlatformEnum { get; } = PlatformEnum.Gog;

    protected override string PlatformToken { get; } = "GX";

    #endregion

    #region Process (System)

    protected override string? ProcessPath { get; } = default;

    #endregion

    #endregion

    // //

    #region Constructor

    public PlatformGog() : base(null, null) { }

    public PlatformGog(DirectoryInfo? directory) : base(directory, null) { }

    public PlatformGog(DirectoryInfo? directory, PlatformSettings? platformSettings) : base(directory, platformSettings) { }

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

    // //

    #region UserIdentification

    protected override string GetUserIdentification(JObject jsonObject, string key)
    {
        if (key is "UID" && _userId is not null)
            return _userId;

        if (key is "USN" && _username is not null)
            return _username;

        var result = base.GetUserIdentification(jsonObject, key);
        if (!string.IsNullOrEmpty(result))
            return result;

        if (key is "USN")
            return "Explorer";

        return result;
    }

    protected override IEnumerable<JToken> GetUserIdentificationByBase(JObject jsonObject, string key)
    {
        if (_userId is null)
            return base.GetUserIdentificationByBase(jsonObject, key);

        var path = Settings.Mapping ? $"PlayerStateData.PersistentPlayerBases[?({{0}})].Owner.{key}" : $"6f=.F?0[?({{0}})].3?K.{key}";
        var expressions = new[]
        {
            Settings.Mapping ? $"@.BaseType.PersistentBaseTypes == '{PersistentBaseTypesEnum.HomePlanetBase}' || @.BaseType.PersistentBaseTypes == '{PersistentBaseTypesEnum.FreighterBase}'" : $"@.peI.DPp == '{PersistentBaseTypesEnum.HomePlanetBase}' || @.peI.DPp == '{PersistentBaseTypesEnum.FreighterBase}'", // only with own base
            Settings.Mapping ? $"@.Owner.UID == '{_userId}'" : $"@.3?K.K7E == '{_userId}'", // only with specified value
        };

        return GetUserIdentificationIntersection(jsonObject, path, expressions);
    }

    protected override IEnumerable<JToken> GetUserIdentificationByDiscovery(JObject jsonObject, string key)
    {
        if (_userId is null)
            return base.GetUserIdentificationByBase(jsonObject, key);

        var path = Settings.Mapping ? $"DiscoveryManagerData.DiscoveryData-v1.Store.Record[?({{0}})].OWS.{key}" : $"fDu.ETO.OsQ.?fB[?({{0}})].ksu.{key}";
        var expressions = new[]
        {
            Settings.Mapping ? $"@.OWS.UID == '{_userId}'" : $"@.ksu.K7E == '{_userId}'", // only with specified value
        };

        return GetUserIdentificationIntersection(jsonObject, path, expressions);
    }

    protected override IEnumerable<JToken> GetUserIdentificationBySettlement(JObject jsonObject, string key)
    {
        if (_userId is null)
            return base.GetUserIdentificationByBase(jsonObject, key);

        var path = Settings.Mapping ? $"PlayerStateData.SettlementStatesV2[?({{0}})].Owner.{key}" : $"6f=.GQA[?({{0}})].3?K.{key}";
        var expressions = new[]
        {
            Settings.Mapping ? $"@.Owner.UID == '{_userId}'" : $"@.3?K.K7E == '{_userId}'", // only with specified value
        };

        return GetUserIdentificationIntersection(jsonObject, path, expressions);
    }

    #endregion
}
