using Newtonsoft.Json.Linq;

namespace libNOM.io;


/// <summary>
/// Implementation for the GOG.com platform.
/// </summary>
// This partial class contains all UserIdentification related code.
public partial class PlatformGog : PlatformSteam
{
    #region Field

    private string? _usn; // will be set together with _uid if GOG Galaxy config file exists

    #endregion

    // Accessor

    #region Getter

    protected override string GetUserIdentification(JObject jsonObject, string key)
    {
        // Base call not as default as _uid and _usn can also be null.
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
