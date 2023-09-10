using CommunityToolkit.HighPerformance;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace libNOM.io.Extensions;


internal static class ReadOnlySpanExtensions
{
    #region typeof(T)

    /// <summary>
    /// Gets the value at the specified index.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="self"></param>
    /// <param name="index">The index of the value to get.</param>
    /// <param name="value">When this method returns, contains the value associated with the specified index, if the index is valid; otherwise, the default value for the type of the value parameter.</param>
    /// <returns>true if the <see cref="ReadOnlySpan{T}"/> contains an element at the specified index; otherwise, false.</returns>
    internal static bool TryGetValue<T>(this ReadOnlySpan<T> self, int index, out T? value)
    {
        if (0 <= index && index < self.Length)
        {
            value = self[index];
            return true;
        }

        value = default;
        return false;
    }

    #endregion

    #region typeof(byte)

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="self"></param>
    /// <param name="start"></param>
    /// <returns></returns>
    internal static T Cast<T>(this ReadOnlySpan<byte> self, int start) where T : unmanaged
    {
        return self.Slice(start, Marshal.SizeOf<T>()).Cast<byte, T>()[0];
    }

    /// <summary>
    /// Concatenates two sequences.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="second"></param>
    /// <returns></returns>
    /// <seealso href="https://github.com/dotnet/runtime/issues/30140#issuecomment-509375982"/>
    internal static ReadOnlySpan<byte> Concat(this ReadOnlySpan<byte> self, ReadOnlySpan<byte> second)
    {
        var array = new byte[self.Length + second.Length];
        self.CopyTo(array);
        second.CopyTo(array.AsSpan(self.Length));
        return array;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Guid"/> structure by using this bytes.
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    internal static Guid GetGuid(this ReadOnlySpan<byte> self, int start = 0)
    {
#if NETSTANDARD2_0
        return new Guid(self.Slice(start, 16).ToArray());
#else
        return new Guid(self.Slice(start, 16));
#endif
    }

    /// <summary>
    /// Deserializes and deobfuscates the raw binary JSON to an object.
    /// </summary>
    /// <param name="self"></param>
    /// <returns>The deserialized object from the bytes.</returns>
    internal static JObject? GetJson(this ReadOnlySpan<byte> self)
    {
        // Account has no proper decompressed size in the initial Fractal update (4.10).
        var length = self.IndexOf((byte)(0));
        // Escaping gone wrong by HG. The backslash is in the file but instead of one of the chars below, still the unescaped control char.
        var json = self.Slice(0, length).GetString().Replace((char)(0x9), 't').Replace((char)(0xA), 'n').Replace((char)(0xD), 'r');

        if (JsonConvert.DeserializeObject(json) is JObject jsonObject)
            return jsonObject;

        return null;
    }

    /// <summary>
    /// Gets all bytes until the first \0 as string.
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    internal static string GetSaveRenamingString(this ReadOnlySpan<byte> self)
    {
        return GetString(self.Slice(0, self.IndexOf((byte)(0))));
    }

    /// <summary>
    /// Decodes all the bytes in UTF-8 format into a string.
    /// </summary>
    /// <param name="self"></param>
    /// <returns>A string that contains the results of decoding the specified sequence of bytes.</returns>
    internal static string GetString(this ReadOnlySpan<byte> self)
    {
#if NETSTANDARD2_0
        return Encoding.UTF8.GetString(self.ToArray());
#else
        return Encoding.UTF8.GetString(self);
#endif
    }

    /// <summary>
    /// Decodes all the bytes in little endian UTF-16 format into a string.
    /// </summary>
    /// <param name="self"></param>
    /// <returns>A string that contains the results of decoding the specified sequence of bytes.</returns>
    internal static string GetUnicode(this ReadOnlySpan<byte> self)
    {
#if NETSTANDARD2_0
        return Encoding.Unicode.GetString(self.ToArray());
#else
        return Encoding.Unicode.GetString(self);
#endif
    }

    /// <summary>
    /// Gets whether this is empty, or contains only 0 bytes.
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    internal static bool IsEmpty(this ReadOnlySpan<byte> self)
    {
#if NETSTANDARD2_0_OR_GREATER
        return self.IsEmpty || self.ToArray().IsEmpty();
#else
        return self.IsEmpty || self.Trim(byte.MinValue).IsEmpty;
#endif
    }

    internal static int ReadString(this ReadOnlySpan<byte> self, out string result, int start)
    {
        var length = self.Cast<int>(start) * 2;
        result = self.Slice(start + 4, length).Cast<byte, char>().ToString();
        return sizeof(int) + length;
    }

    internal static int ReadString(this ReadOnlySpan<byte> self, out ReadOnlySpan<char> result, int start, int length)
    {
        result = self.Slice(start, length).Cast<byte, char>().TrimEnd('\0');
        return length;
    }

    #endregion
}

internal static class SpanExtensions
{
    #region typeof(byte)

    /// <summary>
    /// Gets whether this is empty, or contains only 0 bytes.
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    internal static bool IsEmpty(this Span<byte> self)
    {
#if NETSTANDARD2_0_OR_GREATER
        return self.IsEmpty || self.ToArray().IsEmpty();
#else
        return self.IsEmpty || self.Trim(byte.MinValue).IsEmpty;
#endif
    }

    #endregion
}
