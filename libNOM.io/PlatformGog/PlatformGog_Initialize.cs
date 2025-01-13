using libNOM.io.Settings;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace libNOM.io;


/// <summary>
/// Implementation for the GOG.com platform.
/// </summary>
// This partial class contains all initialization related code.
public partial class PlatformGog : PlatformSteam
{
    #region Constructor

    public PlatformGog() : base() { }

    public PlatformGog(string? path) : base(path) { }

    public PlatformGog(string? path, PlatformSettings? platformSettings) : base(path, platformSettings) { }

    public PlatformGog(PlatformSettings? platformSettings) : base(platformSettings) { }

    public PlatformGog(DirectoryInfo? directory) : base(directory) { }

    public PlatformGog(DirectoryInfo? directory, PlatformSettings? platformSettings) : base(directory, platformSettings) { }

    #endregion

    #region Initialize

    protected override void InitializePlatformSpecific()
    {
        if (Settings.UseExternalSourcesForUserIdentification && File.Exists(GALAXY_CONFIG_JSON))
        {
            var jsonObject = JsonConvert.DeserializeObject(File.ReadAllText(GALAXY_CONFIG_JSON)) as JObject;
            _uid = jsonObject?.GetValue<string>("userId");
            _usn = jsonObject?.GetValue<string>("username");
        }
    }

    #endregion
}
