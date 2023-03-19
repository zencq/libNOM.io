using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace libNOM.io.Extensions;


public static class IEnumerableExtensions
{
    #region typeof(T)

    /// <summary>
    /// Determines whether a sequence is long enough to be accessed with a specified index.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public static bool ContainsIndex<T>(this IEnumerable<T> input, int index)
    {
        return 0 <= index && index < input.Count();
    }

    #endregion

    #region typeof(byte)

    /// <summary>
    /// Initializes a new instance of the <see cref="System.Guid"/> structure by using this bytes.
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    internal static Guid GetGuid(this IEnumerable<byte> self)
    {
        return new Guid(self.ToArray());
    }

    /// <summary>
    /// Deserializes and deobfuscates the JSON to an object.
    /// </summary>
    /// <param name="self"></param>
    /// <returns>The deserialized object from the bytes.</returns>
    internal static JObject? GetJson(this IEnumerable<byte> self)
    {
        // Account has no proper decompressed size in the first Fractal update (4.10).
        var count = self.ToList().FindLastIndex(b => b != 0) + 1;
        var json = self.Take(count).GetString().EscapeDataString();

        if (JsonConvert.DeserializeObject(json) is not JObject jsonObject)
            return null;

        return jsonObject;
    }

    /// <summary>
    /// Decodes all the bytes in UTF-8 format into a string.
    /// </summary>
    /// <param name="self"></param>
    /// <returns>A string that contains the results of decoding the specified sequence of bytes.</returns>
    internal static string GetString(this IEnumerable<byte> self)
    {
        return Encoding.UTF8.GetString(self.ToArray());
    }

    /// <summary>
    /// Creates an array of 4-byte unsigned integer from this bytes.
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    /// <seealso href="https://stackoverflow.com/a/5896716"/>
    internal static uint[] GetUInt32(this IEnumerable<byte> self)
    {
        var origin = self.ToArray();

        var result = new uint[origin.Length / sizeof(uint)];
        Buffer.BlockCopy(origin, 0, result, 0, origin.Length);

        return result;
    }

    /// <summary>
    /// Decodes all the bytes in little endian UTF-16 format into a string.
    /// </summary>
    /// <param name="self"></param>
    /// <returns>A string that contains the results of decoding the specified sequence of bytes.</returns>
    internal static string GetUnicode(this IEnumerable<byte> self)
    {
        return Encoding.Unicode.GetString(self.ToArray());
    }

    /// <summary>
    /// Gets whether this enumerable is null, empty, or contains only 0.
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    internal static bool IsNullOrEmpty(this IEnumerable<byte> self)
    {
        return self is null || !self.Any() || self.All(b => b == 0);
    }

    #endregion

    #region typeof(uint)

    /// <summary>
    /// Creates an array of bytes from this 4-byte unsigned integers.
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    /// <seealso href="https://stackoverflow.com/a/5896716"/>
    internal static byte[] GetBytes(this IEnumerable<uint> self)
    {
        var origin = self.ToArray();

        var result = new byte[origin.Length * sizeof(uint)];
        Buffer.BlockCopy(origin, 0, result, 0, result.Length);

        return result;
    }

    #endregion

    #region typeof(JToken)

    /// <summary>
    /// Gets the most common string of a JToken enumerable.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="self"></param>
    /// <returns></returns>
    /// <seealso href="https://stackoverflow.com/a/39599083"/>
    internal static string? MostCommon(this IEnumerable<JToken> self)
    {
        if (!self.Any())
            return null;

        var groups = self.Select(k => k.Value<string>()).Where(k => !string.IsNullOrWhiteSpace(k)).GroupBy(k => k);
        if (!groups.Any())
            return null;

        var max = groups.Max(g => g.Count());
        return groups.Where(g => g.Count() == max).Select(g => g.Key).FirstOrDefault();
    }

    #endregion
}
