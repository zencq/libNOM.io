using K4os.Compression.LZ4;
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

    #region LZ4

    /// <summary>
    /// Compresses data from one buffer into another.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="target"></param>
    /// <returns>Number of bytes written, or negative value if output buffer is too small.</returns>
    internal static int LZ4_Encode(this byte[] source, out byte[] target)
    {
        target = new byte[LZ4Codec.MaximumOutputSize(source.Length)];
        var bytesWritten = LZ4Codec.Encode(source, 0, source.Length, target, 0, target.Length);

        target = target.Take(bytesWritten).ToArray();
        return bytesWritten;
    }

    /// <summary>
    /// Decompresses data from one buffer into another.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="target"></param>
    /// <param name="targetLength"></param>
    /// <returns>Number of bytes written, or negative value if output buffer is too small.</returns>
    internal static int LZ4_Decode(this byte[] self, out byte[] target, int targetLength)
    {
        target = Array.Empty<byte>();
        var bytesWritten = -1;

        if (targetLength > 0)
        {
            target = new byte[targetLength];
            bytesWritten = LZ4Codec.Decode(self, 0, self.Length, target, 0, target.Length);
        }

        // Fallback. https://github.com/MiloszKrajewski/K4os.Compression.LZ4#decompression
        if (bytesWritten < 0)
        {
            target = new byte[self.Length * 255];
            bytesWritten = LZ4Codec.Decode(self, 0, self.Length, target, 0, target.Length);
        }

        return bytesWritten;
    }

    #endregion

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

    #region typeof(string)

    /// <summary>
    /// Gets the most common string of an enumerable.
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    /// <seealso href="https://stackoverflow.com/a/39599083"/>
    internal static string? MostCommon(this IEnumerable<string?> self)
    {
        self = self.Where(i => i is not null);

        if (!self.Any())
            return null;

        var groups = self.GroupBy(i => i);
        if (!groups.Any())
            return null;

        var max = groups.Max(i => i.Count());
        return groups.Where(i => i.Count() == max).Select(j => j.Key).FirstOrDefault();
    }

    #endregion
}
