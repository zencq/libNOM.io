using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace libNOM.io.Extensions;


internal static class StringExtensions
{
    /// <summary>
    /// Adds the null terminator at the end and encodes all the characters into a sequence of bytes.
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    internal static byte[] GetBytesWithTerminator(this string self) => $"{self}\0".GetUTF8Bytes();

    /// <summary>
    /// Deserializes a JSON string to an object.
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    internal static JObject? GetJson(this string self) => JsonConvert.DeserializeObject(self) is JObject jsonObject ? jsonObject : null;

    /// <summary>
    /// Encodes all the characters in the string into a sequence of bytes in UTF-16 format.
    /// </summary>
    /// <param name="self"></param>
    /// <returns>A byte array containing the results of encoding the set of characters.</returns>
    internal static byte[] GetUnicodeBytes(this string self) => Encoding.Unicode.GetBytes(self);

    /// <summary>
    /// Encodes all the characters in the specified string into a sequence of bytes in UTF-8 format.
    /// </summary>
    /// <param name="self"></param>
    /// <returns>A byte array containing the results of encoding the set of characters.</returns>
    internal static byte[] GetUTF8Bytes(this string self) => Encoding.UTF8.GetBytes(self);
}
