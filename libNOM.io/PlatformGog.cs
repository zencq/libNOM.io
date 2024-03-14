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

    private string? _usn; // will be set together with _uid if GOG Galaxy config file exists

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
                _uid = jsonObject?.GetValue<string>("userId");
                _usn = jsonObject?.GetValue<string>("username");
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
            "UID" => _uid,
            "USN" => _usn,
            _ => null,
        } ?? base.GetUserIdentification(jsonObject, key);

        // Fallback as it was the default for a long time and could not be changed.
        if (key == "USN" && string.IsNullOrEmpty(result))
            result = "Explorer";

        return result ?? string.Empty;
    }

    #endregion
}
