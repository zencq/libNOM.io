using libNOM.io.Interfaces;

using Newtonsoft.Json.Linq;

namespace libNOM.io;


// This partial class contains UserIdentification related code.
public abstract partial class Platform : IPlatform, IEquatable<Platform>
{
    #region Fields

    protected string? _uid; // will usually be set if available in path

    #endregion

    #region Get

    /// <summary>
    /// Gets the <see cref="UserIdentification"/> for this platform.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <returns></returns>
    private UserIdentification GetUserIdentification(JObject jsonObject) => new()
    {
        LID = GetUserIdentification(jsonObject, "LID"),
        UID = GetUserIdentification(jsonObject, "UID"),
        USN = GetUserIdentification(jsonObject, "USN"),
        PTK = PlatformToken,
    };

    /// <summary>
    /// Gets the <see cref="UserIdentification"/> information for the specified property key.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    protected virtual string GetUserIdentification(JObject jsonObject, string key)
    {
        // Utilize GetPath() to get the right obfuscation state of the key.
        key = key switch
        {
            "LID" => Json.GetPath("RELATIVE_OWNER_LID", jsonObject),
            "UID" => Json.GetPath("RELATIVE_OWNER_UID", jsonObject),
            "USN" => Json.GetPath("RELATIVE_OWNER_USN", jsonObject),
            _ => string.Empty,
        };
        if (string.IsNullOrEmpty(key))
            return string.Empty;

        // ByBase is most reliable due to the BaseType, then BySettlement is second as it is still something you own, and ByDiscovery as last resort which can be a mess.
        return GetUserIdentificationInContext(jsonObject, key, "PERSISTENT_PLAYER_BASE", [("INTERSECTION_PERSISTENT_PLAYER_BASE_OWNERSHIP_EXPRESSION_TYPE_OR_TYPE", [PersistentBaseTypesEnum.HomePlanetBase, PersistentBaseTypesEnum.FreighterBase])])
            ?? GetUserIdentificationInContext(jsonObject, key, "SETTLEMENT")
            ?? GetUserIdentificationInCommon(jsonObject, key, "DISCOVERY_DATA")
            ?? string.Empty;
    }

    /// <summary>
    /// Gets the <see cref="UserIdentification"/> information for the specified key from common data.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <param name="key"></param>
    /// <param name="by"></param>
    /// <param name="additionalExpressions"></param>
    /// <returns></returns>
    private string? GetUserIdentificationInCommon(JObject jsonObject, string key, string by, params (string, object[])[] additionalExpressions)
    {
        var path = Json.GetPath($"INTERSECTION_{by}_OWNERSHIP_KEY", jsonObject, key);
        var result = GetCommonIntersectionExpressions(by).Concat(additionalExpressions).Select(i => Json.GetPath(i.Item1, jsonObject, i.Item2)).Select(i => string.Format(path, i));

        return jsonObject.SelectTokensWithIntersection<string>(result).MostCommon();
    }

    /// <summary>
    /// Gets the <see cref="UserIdentification"/> information for the specified key from within a context.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <param name="key"></param>
    /// <param name="by"></param>
    /// <param name="additionalExpressions"></param>
    /// <returns></returns>
    /// <seealso href="https://stackoverflow.com/a/38256828"/>
    private string? GetUserIdentificationInContext(JObject jsonObject, string key, string by, params (string, object[])[] additionalExpressions)
    {
        var expressions = GetCommonIntersectionExpressions(by).Concat(additionalExpressions).Select(i => Json.GetPath(i.Item1, jsonObject, i.Item2));
        var result = new List<string>();

        foreach (var context in GetContexts(jsonObject))
        {
            var path = Json.GetPath($"INTERSECTION_{by}_OWNERSHIP_KEY", jsonObject, context, key);
            result.AddRange(expressions.Select(i => string.Format(path, i)));
        }

        return jsonObject.SelectTokensWithIntersection<string>(result).MostCommon();
    }

    private (string, object[])[] GetCommonIntersectionExpressions(string category)
    {
        if (_uid is null)
            return [
                ($"INTERSECTION_{category}_OWNERSHIP_EXPRESSION_PTK", [PlatformToken]),
                ($"INTERSECTION_{category}_OWNERSHIP_EXPRESSION_WITH_LID", []),
            ];
        return [
            ($"INTERSECTION_{category}_OWNERSHIP_EXPRESSION_THIS_UID", [_uid]),
        ];
    }

    #endregion

    #region Update

    /// <summary>
    /// Updates the <see cref="UserIdentification"/> with data from all loaded containers.
    /// </summary>
    protected void UpdateUserIdentification()
    {
        PlatformUserIdentification.LID = SaveContainerCollection.Select(i => i.UserIdentification?.LID).MostCommon();
        PlatformUserIdentification.PTK = PlatformToken;
        PlatformUserIdentification.UID = SaveContainerCollection.Select(i => i.UserIdentification?.UID).MostCommon();
        PlatformUserIdentification.USN = SaveContainerCollection.Select(i => i.UserIdentification?.USN).MostCommon();
    }

    #endregion
}
