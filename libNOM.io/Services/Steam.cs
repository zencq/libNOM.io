using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace libNOM.io.Services;


/// <summary>
/// Specialized client to query the Steam Web-API.
/// </summary>
internal class SteamService
{
    #region Field

    private HttpClient? _httpClient;

    #endregion

    #region Property

    private HttpClient HttpClient => _httpClient ??= new();

    #endregion

    // //

    internal SteamService() { }

    // //

    /// <summary>
    /// Queries the Steam Account Name for the specified SteamID.
    /// This method does not block the calling thread.
    /// </summary>
    /// <returns></returns>
    internal async Task<string?> GetPersonaNameAsync(string steamId)
    {
        var content = string.Empty;
        var requestUri = $"http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/?key={Properties.Resources.STEAM_API_KEY}&steamids={steamId}";

        try
        {
            using HttpResponseMessage response = await HttpClient.GetAsync(requestUri);
            response.EnsureSuccessStatusCode();
            content = await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return null;
        }

        if (JsonConvert.DeserializeObject(content) is JObject jsonObject)
            return jsonObject.GetValue<string>("response.players[0].personaname");

        return null;
    }
}
