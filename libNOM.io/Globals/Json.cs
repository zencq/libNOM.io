using CommunityToolkit.HighPerformance;

using Newtonsoft.Json.Linq;

namespace libNOM.io.Globals;


internal static partial class Json
{
    #region JSONPath

    /// <summary>
    /// Returns all JSONPath expressions for the specified identifier.
    /// </summary>
    /// <param name="identifier"></param>
    /// <returns></returns>
    /// <exception cref="KeyNotFoundException"></exception>
    private static string[] GetPaths(string identifier)
    {
        var all = Constants.JSONPATH.Concat(Constants.JSONPATH_EXTENSION).ToDictionary(i => i.Key, i => i.Value);
        var upper = identifier.ToUpperInvariant();

        if (all.ContainsKey(upper))
            return Constants.JSONPATH[upper];

        return [identifier]; // return original and not upper variant
    }

    /// <inheritdoc cref="GetPaths(string, JObject?, SaveContextQueryEnum, object?[])"/>
    internal static string[] GetPaths(string identifier, JObject? jsonObject)
    {
        return GetPaths(identifier, jsonObject, SaveContextQueryEnum.DontCare);
    }

    /// <inheritdoc cref="GetPaths(string, JObject?, SaveContextQueryEnum, object?[])"/>
    internal static string[] GetPaths(string identifier, JObject? jsonObject, SaveContextQueryEnum context)
    {
        var paths = GetPaths(identifier);
        if (paths[0] == identifier || jsonObject?.IsRoot() != true) // without root it is not possible to determine obfuscation state and save format reliable
            return paths;

        var format = Constants.JSONPATH["ACTIVE_CONTEXT"].Any(jsonObject.ContainsKey) ? SaveFormatEnum.Omega : SaveFormatEnum.Vanilla;
        var contextKey = (jsonObject.UsesMapping() ? Constants.JSONPATH_CONTEXT_PLAINTEXT : Constants.JSONPATH_CONTEXT_OBFUSCATED)[(int)(context)];
        var index = ((int)(format) - 1) * 2 + jsonObject.UsesMapping().ToByte(); // 2 obfuscation states per save format

        while (index >= 0) // to not store unchanged paths multiple times
        {
            if (paths.ContainsIndex(index) && !string.IsNullOrEmpty(paths[index])) // skip empty strings for Vanialla save format
                return string.IsNullOrEmpty(contextKey) ? [paths[index]] : [string.Format(paths[index], contextKey)]; // [] to have a consistent return type

            index -= 2; // 2 obfuscation states per save format
        }

        return [];
    }

    /// <inheritdoc cref="GetPaths(string, JObject?, SaveContextQueryEnum, object?[])"/>
    internal static string[] GetPaths(string identifier, JObject? jsonObject, params object?[] interpolations)
    {
        return GetPaths(identifier, jsonObject).Select(i => string.Format(i, interpolations)).ToArray();
    }

    /// <summary>
    /// Returns the correct JSONPath expression for the current obfuscation state and save format.
    /// </summary>
    /// <param name="identifier">Identifier for the desired JSONPath.</param>
    /// <param name="jsonObject">JSON object to determine obfuscation state and save format.</param>
    /// <param name="context">Context the path should be build with.</param>
    /// <param name="interpolations">Additional interpolations to insert into the path if neccessary.</param>
    /// <returns></returns>
    /// <exception cref="KeyNotFoundException"></exception>
    internal static string[] GetPaths(string identifier, JObject? jsonObject, SaveContextQueryEnum context, params object?[] interpolations)
    {
        return GetPaths(identifier, jsonObject, context).Select(i => string.Format(i, interpolations)).ToArray();
    }

    #endregion

    /// <summary>
    /// Selects a collection of elements using multiple JSONPath expression with conjunction.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="path"></param>
    /// <param name="expressions"></param>
    /// <returns></returns>
    internal static IEnumerable<JToken> SelectTokensWithIntersection(JObject self, string path, params string[] expressions)
    {
        if (expressions.Length == 0)
            return [];

        IEnumerable<JToken> result = null!;
        foreach (var expression in expressions)
        {
            var query = self.SelectTokens(string.Format(path, expression));
            result = result is null ? query : result.Intersect(query);
        }
        return result;
    }
    /// <summary>
    /// Returns the format of the specified save file object. Needs to be called on the root object.
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    internal static SaveFormatEnum GetSaveFormat(JObject self)
    {
        return Constants.JSONPATH["ACTIVE_CONTEXT"].Any(self.ContainsKey) ? SaveFormatEnum.Omega : SaveFormatEnum.Vanilla;
    }
}
