using CommunityToolkit.Diagnostics;

using libNOM.map;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace libNOM.io.Extensions;


public static class NewtonsoftExtensions
{
    #region Flags

    /// <summary>
    /// Whether the specified object is the root object.
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    internal static bool IsRoot(this JObject self) => Constants.JSONPATH["VERSION"].Any(self.ContainsKey);

    /// <summary>
    /// Whether the specified object is deobfuscated. Needs to be called on the root object.
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    internal static bool UsesMapping(this JObject self) => self.ContainsKey(Constants.JSONPATH["VERSION"][1]);

    #endregion

    #region GetBytes

    /// <summary>
    /// Serializes and encodes the object into a sequence of bytes in UTF-8 format.
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    internal static ReadOnlySpan<byte> GetBytes(this JObject self) => self.GetString(false, true).GetUTF8Bytes();

    #endregion

    #region GetString

    /// <summary>
    /// Serializes the object to a JSON string according to the specified options.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="indent">Whether the result will be indented.</param>
    /// <param name="obfuscate">Whether the result will be obfuscated.</param>
    /// <returns>A JSON string representation of the object.</returns>
    public static string GetString(this JObject self, bool indent, bool obfuscate)
    {
        var jsonObject = self;

        if (obfuscate)
        {
            jsonObject = Common.DeepCopy(self);
            Mapping.Obfuscate(jsonObject);
        }

        var settings = new JsonSerializerSettings { Formatting = indent ? Formatting.Indented : Formatting.None };
        var jsonString = $"{JsonConvert.SerializeObject(jsonObject, settings)}\0";

        return jsonString.Replace("/", "\\/");
    }

    #endregion

    #region GetValue(s)

    /// <summary>
    /// Gets the value of the JSON element that matches the path of indices.
    /// Except the last one, each index in the entire path must point to either a JArray or a JObject.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="self"></param>
    /// <param name="indices"></param>
    /// <returns>The value at the end of the path of indices.</returns>
    /// <exception cref="ArgumentException" />
    /// <exception cref="InvalidOperationException" />
    public static T? GetValue<T>(this JObject self, ReadOnlySpan<int> indices)
    {
        Guard.HasSizeGreaterThan(indices, 0, nameof(indices));

        JToken? jToken = self;
        for (var i = 0; i < indices.Length; i++)
        {
            var index = indices[i];
            var path = jToken!.Path;

            if (jToken is JArray jArray)
            {
                jToken = jArray.ContainsIndex(index) ? jToken[index] : null;
            }
            else if (jToken is JObject jObject)
                jToken = jObject.Children().ElementAtOrDefault(index);

            if (jToken is JProperty jProperty)
                jToken = jProperty.Value;

            if (jToken is null)
                ThrowHelper.ThrowInvalidOperationException($"Index {indices[i]} at position {i} is not available ({path}).");
        }

        return ConvertToken<T>(jToken);
    }

    /// <inheritdoc cref="GetValue{T}(JObject, string, SaveContextQueryEnum)"/>
    public static T? GetValue<T>(this JObject self, string pathIdentifier) => GetValue<T>(self, Json.GetPaths(pathIdentifier, self));

    /// <summary>
    /// Evaluates a JSONPath expression and converts its value to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="self"></param>
    /// <param name="pathIdentifier"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public static T? GetValue<T>(this JObject self, string pathIdentifier, SaveContextQueryEnum context) => GetValue<T>(self, Json.GetPaths(pathIdentifier, self, context));

    private static T? GetValue<T>(this JObject self, IEnumerable<string> paths)
    {
        foreach (var path in paths)
            if (self.SelectToken(path) is JToken jToken)
                return ConvertToken<T>(jToken);

        return default;
    }

    /// <inheritdoc cref="GetValues{T}(JObject, string, SaveContextQueryEnum)"/>
    public static IEnumerable<T> GetValues<T>(this JObject self, string pathIdentifier) => GetValues<T>(self, Json.GetPaths(pathIdentifier, self));

    /// <summary>
    /// Evaluates a JSONPath expression and converts all values to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="self"></param>
    /// <param name="pathIdentifier"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public static IEnumerable<T> GetValues<T>(this JObject self, string pathIdentifier, SaveContextQueryEnum context) => GetValues<T>(self, Json.GetPaths(pathIdentifier, self, context));

    private static IEnumerable<T> GetValues<T>(this JObject self, IEnumerable<string> paths)
    {
        foreach (var path in paths)
            foreach (var jToken in self.SelectTokens(path).Select(ConvertToken<T>).Where(i => i is not null))
                yield return jToken!;
    }

    #endregion

    #region SetValue

    /// <summary>
    /// Gets the value of the JSON element that matches the path of indices and replaces the current value with the specified one.
    /// Except the last one, each index in the entire path must point to either a JArray or a JObject.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="value"></param>
    /// <param name="indices"></param>
    /// <returns>Whether setting the value was successful.</returns>
    /// <exception cref="ArgumentException" />
    /// <exception cref="InvalidOperationException" />
    public static bool SetValue(this JObject self, JToken value, ReadOnlySpan<int> indices)
    {
        if (GetValue<JToken>(self, indices) is JToken jToken)
        {
            jToken.Replace(value);
            return true;
        }
        return false;
    }

    /// <inheritdoc cref="SetValue(JObject, JToken, string, SaveContextQueryEnum)"/>
    public static bool SetValue(this JObject self, JToken value, string pathIdentifier) => SetValue(self, value, Json.GetPaths(pathIdentifier, self));

    /// <summary>
    /// Evaluates a JSONPath expression and replaces the current value with the specified one.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="value"></param>
    /// <param name="pathIdentifier"></param>
    /// <param name="context"></param>
    /// <returns>Whether setting the value was successful.</returns>
    public static bool SetValue(this JObject self, JToken value, string pathIdentifier, SaveContextQueryEnum context) => SetValue(self, value, Json.GetPaths(pathIdentifier, self, context));

    private static bool SetValue(this JObject self, JToken value, IEnumerable<string> paths)
    {
        foreach (var path in paths)
            if (self.SelectToken(path) is JToken jToken)
            {
                jToken.Replace(value);
                return true;
            }

        return false;
    }

    /// <summary>
    /// Evaluates a JSONPath expression and sets the new value only if there is one to replace.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <param name="value"></param>
    /// <param name="pathIdentifier"></param>
    internal static void SetValueIfNotNullOrEmpty(this JObject self, JToken value, string pathIdentifier)
    {
        // Only called on relative objects and therefore context does not matter.
        var paths = Json.GetPaths(pathIdentifier, self);

        if (!string.IsNullOrEmpty(GetValue<string>(self, paths)))
            SetValue(self, value, paths);
    }

    #endregion

    #region SelectToken(s)

    /// <summary>
    /// Selects a collection of elements using multiple JSONPath expression with conjunction.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="path"></param>
    /// <param name="expressions"></param>
    /// <returns></returns>
    internal static IEnumerable<T> SelectTokensWithIntersection<T>(this JObject self, IEnumerable<string> paths)
    {
        IEnumerable<JToken>? result = null; // starting with a [] would always be empty
        foreach (var path in paths)
        {
            var query = self.SelectTokens(path);
            if (query.Any())
                result = result is null ? query : result.Intersect(query);
        }
        if (result is not null)
            foreach (var value in result.Select(ConvertToken<T>).Where(i => i is not null))
                yield return value!;
    }

    #endregion

    #region Helper

    private static T? ConvertToken<T>(this JToken self)
    {
        var type = typeof(T);

        if (type.IsSubclassOf(typeof(JToken)) || type == typeof(JToken))
            if (self is T subclassOfJToken)
                return subclassOfJToken;
            else
                return default;

        if (type.IsEnum)
        {
            // integer
            if (self.Type == JTokenType.Integer && self.Value<int>() is int intValue)
                return (T)(object)(intValue); // https://stackoverflow.com/a/10387134

            // string
            if (self.Type == JTokenType.String && self.Value<string>() is string stringValue)
                return (T)(Enum.Parse(type, stringValue));
        }
        else
            return self.Value<T>();

        return default;
    }

    #endregion
}
