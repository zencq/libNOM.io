using libNOM.io.Interfaces;

using Newtonsoft.Json.Linq;

namespace libNOM.io;


// This partial class contains UserIdentification related code.
public abstract partial class Platform : IPlatform, IEquatable<Platform>
{
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
        return GetUserIdentificationInContext(jsonObject, key, GetIntersectionExpressionsByBase, "INTERSECTION_PERSISTENT_PLAYER_BASE_OWNERSHIP_KEY")
            ?? GetUserIdentificationInContext(jsonObject, key, GetIntersectionExpressionsBySettlement, "INTERSECTION_SETTLEMENT_OWNERSHIP_KEY")
            ?? GetUserIdentificationInCommon(jsonObject, key, GetIntersectionExpressionsByDiscovery, "INTERSECTION_DISCOVERY_DATA_OWNERSHIP_KEY")
            ?? string.Empty;
    }

    /// <summary>
    /// Gets the <see cref="UserIdentification"/> information for the specified key from common data.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <param name="key"></param>
    /// <param name="GetIntersectionExpressions">Function to get all intersection expressions for this type.</param>
    /// <param name="pathIdentifier">Path with placeholders where the intersection expressions are inserted.</param>
    /// <returns></returns>
    private static string? GetUserIdentificationInCommon(JObject jsonObject, string key, Func<JObject, string[]> GetIntersectionExpressions, string pathIdentifier)
    {
        var path = Json.GetPath(pathIdentifier, jsonObject, key);
        var result = GetIntersectionExpressions(jsonObject).Select(i => string.Format(path, i));

        return jsonObject.SelectTokensWithIntersection<string>(result).MostCommon();
    }

    /// <summary>
    /// Gets the <see cref="UserIdentification"/> information for the specified key from within a context.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <param name="key"></param>
    /// <param name="GetIntersectionExpressions">Function to get all intersection expressions for this type.</param>
    /// <param name="pathIdentifier">Path with placeholders where the intersection expressions are inserted.</param>
    /// <returns></returns>
    /// <seealso href="https://stackoverflow.com/a/38256828"/>
    private static string? GetUserIdentificationInContext(JObject jsonObject, string key, Func<JObject, string[]> GetIntersectionExpressions, string pathIdentifier)
    {
        var expressions = GetIntersectionExpressions(jsonObject);
        var result = new List<string>();

        foreach (var context in GetContexts(jsonObject))
        {
            var path = Json.GetPath(pathIdentifier, jsonObject, context, key);
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

    protected virtual string[] GetIntersectionExpressionsByDiscovery(JObject jsonObject)
    {
        return
        [
            Json.GetPath("INTERSECTION_DISCOVERY_DATA_OWNERSHIP_EXPRESSION_PTK", jsonObject, PlatformToken),
            Json.GetPath("INTERSECTION_DISCOVERY_DATA_OWNERSHIP_EXPRESSION_WITH_LID", jsonObject),
        ];
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
