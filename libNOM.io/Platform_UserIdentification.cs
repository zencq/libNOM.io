using libNOM.io.Interfaces;

using Newtonsoft.Json.Linq;

namespace libNOM.io;


/// <summary>
/// Abstract base for all platforms which just hook into the methods they need.
/// </summary>
public abstract partial class Platform : IPlatform, IEquatable<Platform>
{
    #region UserIdentification

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

    /// <summary>
    /// Gets the <see cref="UserIdentification"/> for this platform.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <returns></returns>
    protected UserIdentification GetUserIdentification(JObject jsonObject)
    {
        return new UserIdentification
        {
            LID = GetUserIdentification(jsonObject, "LID"),
            UID = GetUserIdentification(jsonObject, "UID"),
            USN = GetUserIdentification(jsonObject, "USN"),
            PTK = PlatformToken,
        };
    }

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
        var a = GetUserIdentificationByBase(jsonObject, key);
        var b = GetUserIdentificationByDiscovery(jsonObject, key);
        var c = GetUserIdentificationBySettlement(jsonObject, key);

        return GetUserIdentificationByBase(jsonObject, key) ?? GetUserIdentificationBySettlement(jsonObject, key) ?? GetUserIdentificationByDiscovery(jsonObject, key) ?? string.Empty;
    }

    /// <summary>
    /// Gets the <see cref="UserIdentification"/> information for the specified property key from bases.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    /// <seealso href="https://stackoverflow.com/a/38256828"/>
    protected virtual string? GetUserIdentificationByBase(JObject jsonObject, string key)
    {
        var expressions = GetIntersectionExpressionsByBase(jsonObject);
        var result = new List<string>();

        foreach (var context in GetContexts(jsonObject))
        {
            var path = Json.GetPath("INTERSECTION_PERSISTENT_PLAYER_BASE_OWNERSHIP_KEY", jsonObject, context, key);
            result.AddRange(expressions.Select(i => string.Format(path, i)));
        }

        return jsonObject.SelectTokensWithIntersection<string>(result).MostCommon();
    }

    protected virtual string[] GetIntersectionExpressionsByBase(JObject jsonObject)
    {
        return
        [
            Json.GetPath("INTERSECTION_PERSISTENT_PLAYER_BASE_OWNERSHIP_EXPRESSION_TYPE_OR_TYPE", jsonObject, PersistentBaseTypesEnum.HomePlanetBase, PersistentBaseTypesEnum.FreighterBase),
            Json.GetPath("INTERSECTION_PERSISTENT_PLAYER_BASE_OWNERSHIP_EXPRESSION_PTK", jsonObject, PlatformToken),
            Json.GetPath("INTERSECTION_PERSISTENT_PLAYER_BASE_OWNERSHIP_EXPRESSION_WITH_LID", jsonObject),
        ];
    }

    /// <summary>
    /// Gets the <see cref="UserIdentification"/> information for the specified property key from discoveries.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    protected virtual string? GetUserIdentificationByDiscovery(JObject jsonObject, string key)
    {
        var path = Json.GetPath("INTERSECTION_DISCOVERY_DATA_OWNERSHIP_KEY", jsonObject, key);
        var result = GetIntersectionExpressionsByDiscovery(jsonObject).Select(i => string.Format(path, i));

        return jsonObject.SelectTokensWithIntersection<string>(result).MostCommon();
    }

    protected virtual string[] GetIntersectionExpressionsByDiscovery(JObject jsonObject)
    {
        return
        [
            Json.GetPath("INTERSECTION_DISCOVERY_DATA_OWNERSHIP_EXPRESSION_PTK", jsonObject, PlatformToken),
            Json.GetPath("INTERSECTION_DISCOVERY_DATA_OWNERSHIP_EXPRESSION_WITH_LID", jsonObject),
        ];
    }

    /// <summary>
    /// Gets the <see cref="UserIdentification"/> information for the specified property key from settlements.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    protected virtual string? GetUserIdentificationBySettlement(JObject jsonObject, string key)
    {
        var expressions = GetIntersectionExpressionsBySettlement(jsonObject);
        var result = new List<string>();

        foreach (var context in GetContexts(jsonObject))
        {
            var path = Json.GetPath("INTERSECTION_SETTLEMENT_OWNERSHIP_KEY", jsonObject, context, key);
            result.AddRange(expressions.Select(i => string.Format(path, i)));
        }

        return jsonObject.SelectTokensWithIntersection<string>(result).MostCommon();
    }

    protected virtual string[] GetIntersectionExpressionsBySettlement(JObject jsonObject)
    {
        return
        [
            Json.GetPath("INTERSECTION_SETTLEMENT_OWNERSHIP_EXPRESSION_PTK", jsonObject, PlatformToken),
            Json.GetPath("INTERSECTION_SETTLEMENT_OWNERSHIP_EXPRESSION_WITH_LID", jsonObject),
        ];
    }

    #endregion
}
