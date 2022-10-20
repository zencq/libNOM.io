using libNOM.map;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace libNOM.io.Extensions;


public static class NewtonsoftExtensions
{
    /// <inheritdoc cref="GetBytes(JObject, bool, bool)"/>
    public static byte[] GetBytes(this JObject self)
    {
        return GetBytes(self, true);
    }

    /// <summary>
    /// Serializes and encodes the object into a sequence of bytes in UTF-8 format.
    /// </summary>
    /// <param name="self"></param>
    /// <returns>A byte array containing the results of encoding and serializing the object.</returns>
    public static byte[] GetBytes(this JObject self, bool obfuscate)
    {
        if (self is null)
            return Array.Empty<byte>();

        return self.GetString(false, obfuscate).GetUTF8Bytes();
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
        if (self is null)
            return string.Empty;

        var jsonObject = self;

        if (obfuscate)
        {
            jsonObject = self.DeepClone() as JObject;

            Mapping.Obfuscate(jsonObject!);
        }

        string json;
        if (indent)
        {
            var settings = new JsonSerializerSettings { Formatting = Formatting.Indented };
            json = JsonConvert.SerializeObject(jsonObject, settings);
        }
        else
        {
            json = JsonConvert.SerializeObject(jsonObject) + "\0";
        }

        return json.Replace("/", "\\/");
    }

    internal static T? GetValue<T>(this JObject self, string pathObfuscated, string pathDeobfuscated)
    {
        var token = self.SelectToken(self.UseMapping() ? pathDeobfuscated : pathObfuscated);
        if (token is null)
            return default;

        return token.Value<T>();
    }

    /// <summary>
    /// Returns whether the specified object is deobfuscated. Needs to be the root object.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    internal static bool UseMapping(this JObject input)
    {
        return input.ContainsKey("Version");
    }
}
