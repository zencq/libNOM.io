using libNOM.map;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace libNOM.io.Extensions;


public static class NewtonsoftExtensions
{
    /// <summary>
    /// Serializes and encodes the object into a sequence of bytes in UTF-8 format.
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    internal static ReadOnlySpan<byte> GetBytes(this JObject self)
    {
        return self.GetString(false, true).GetUTF8Bytes();
    }

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
            jsonObject = self.DeepClone() as JObject;
            Mapping.Obfuscate(jsonObject!);
        }

        var settings = new JsonSerializerSettings { Formatting = indent ? Formatting.Indented : Formatting.None };
        var jsonString = $"{JsonConvert.SerializeObject(jsonObject, settings)}\0";

        return jsonString.Replace("/", "\\/");
    }

    /// <summary>
    /// Evaluates a JSONPath expression and converts its value to <typeparamref name="T"/>.
    /// Multiple paths can be passed to cover different obfuscation states.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="self"></param>
    /// <param name="paths"></param>
    /// <returns></returns>
    public static T? GetValue<T>(this JObject self, params string[] paths)
    {
        foreach (var path in paths)
        {
            var jToken = self.SelectToken(path);
            if (jToken is not null)
            {
                var type = typeof(T);
                if (type.IsEnum)
                {
                    var value = jToken.Value<string>();
                    return value is null ? default : (T)(Enum.Parse(type, value));
                }
                return jToken.Value<T>();
            }
        }
        return default;
    }

    /// <summary>
    /// Evaluates a JSONPath expression and converts its value to <typeparamref name="T"/>.
    /// Multiple paths can be passed to cover different obfuscation states.
    /// </summary>
    /// <param name="self"></param>
    /// <returns>Whether setting the value was successfull.</returns>
    public static bool SetValue(this JObject self, JToken value, params string[] paths)
    {
        foreach (var path in paths)
        {
            JToken? token = self.SelectToken(path);
            if (token is not null)
            {
                token.Replace(value);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Returns whether the specified object is deobfuscated. Needs to be called on the root object.
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    public static bool UsesMapping(this JObject self)
    {
        return self.ContainsKey("Version");
    }
}
