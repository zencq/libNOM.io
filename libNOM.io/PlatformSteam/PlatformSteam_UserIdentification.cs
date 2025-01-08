using libNOM.io.Services;

using Newtonsoft.Json.Linq;

namespace libNOM.io;


/// <summary>
/// Implementation for the Steam platform.
/// </summary>
// This partial class contains UserIdentification related code.
public partial class PlatformSteam : Platform
{
    // Accessor

    #region Getter

    protected override string GetUserIdentification(JObject jsonObject, string key)
    {
        // Base call not as default as _uid can also be null.
        var result = key switch
        {
            "LID" => _uid,
            "UID" => _uid,
            _ => null,
        } ?? base.GetUserIdentification(jsonObject, key);

        // Get via API only if not found in-file.
        if (key == "USN" && string.IsNullOrEmpty(result) && Settings.UseExternalSourcesForUserIdentification && _uid is not null)
            result = GetUserIdentificationBySteam();

        return result ?? string.Empty;
    }

    /// <summary>
    /// Gets the <see cref="UserIdentification"/> information for the USN by calling the Steam Web-API.
    /// </summary>
    /// <returns></returns>
    private string? GetUserIdentificationBySteam()
    {
        // Ensure STEAM_API_KEY is a formal valid one.
        if (!Properties.Resources.STEAM_API_KEY.All(char.IsLetterOrDigit))
            return null;

        var task = SteamService.GetPersonaNameAsync(_uid!); // _uid has been checked before
        task.Wait();
        return task.Result;
    }

    #endregion
}
