using System.Runtime.InteropServices;
using System.Text;

using CommunityToolkit.HighPerformance;

using Newtonsoft.Json.Linq;

namespace libNOM.io.Extensions;


internal static class ReadOnlySpanExtensions
{
    #region typeof(T)

    /// <summary>
    /// Concatenates two ReadOnlySpan<T> to one.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="span0"></param>
    /// <param name="span1"></param>
    /// <returns></returns>
    /// <seealso href="https://stackoverflow.com/a/62525830"/>
    internal static ReadOnlySpan<T> Concat<T>(this ReadOnlySpan<T> span0, ReadOnlySpan<T> span1)
    {
        var result = new T[span0.Length + span1.Length].AsSpan();
        var start = 0;

        span0.CopyTo(result[start..]);
        start += span0.Length;

        span1.CopyTo(result[start..]);
        return result;
    }

    /// <summary>
    /// Concatenates three ReadOnlySpan<T> to one.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="span0"></param>
    /// <param name="span1"></param>
    /// <param name="span2"></param>
    /// <returns></returns>
    /// <seealso href="https://stackoverflow.com/a/62525830"/>
    internal static ReadOnlySpan<T> Concat<T>(ReadOnlySpan<T> span0, ReadOnlySpan<T> span1, ReadOnlySpan<T> span2)
    {
        var result = new T[span0.Length + span1.Length + span2.Length].AsSpan();
        var start = 0;

        span0.CopyTo(result[start..]);
        start += span0.Length;

        span1.CopyTo(result[start..]);
        start += span1.Length;

        span2.CopyTo(result[start..]);
        return result;
    }

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

    internal static ReadOnlySpan<byte> EscapeHashedIds(this ReadOnlySpan<byte> input)
    {
        var result = input;

        foreach (var pair in Constants.BINARY_MAPPING)
        {
            var indices = result.IndicesOf(pair.Key).ToArray();
            if (indices.Length > 0)
            {
                var value = pair.Value.AsSpan();

                for (int i = 0; i < indices.Length; i++)
                {
                    var index = indices[i] + ((pair.Value.Length - pair.Key.Length) * i);

                    var before = result[..index];
                    var after = result.Slice(index + pair.Key.Length, result.Length - before.Length - pair.Key.Length);

                    result = Concat(before, value, after);
                }
            }
        }

        return result;
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
    /// Deserializes a raw binary JSON to an object.
    /// </summary>
    /// <param name="self"></param>
    /// <returns>The deserialized object from the bytes.</returns>
    internal static JObject? GetJson(this ReadOnlySpan<byte> self, bool escapeHashedIds = false)
    {
        var binary = escapeHashedIds ? self.EscapeHashedIds() : self;

        // Account has no proper decompressed size in the initial Fractal update (4.10) and therefore we look for the first.
        // Escaping gone wrong by HG. The backslash is in the file but instead of one of the chars below, still the unescaped control char.
        var json = GetStringUntilTerminator(binary).Replace((char)(0x9), 't').Replace((char)(0xA), 'n').Replace((char)(0xD), 'r');
        return json.GetJson();
    }

    /// <summary>
    /// Gets all bytes until the first \0 as string.
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    internal static string GetStringUntilTerminator(this ReadOnlySpan<byte> self)
    {
        return GetString(self[..self.IndexOf(Constants.BINARY_TERMINATOR)]);
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

    internal static IEnumerable<int> IndicesOf(this ReadOnlySpan<byte> haystack, byte[] needle, int startIndex = 0, bool includeOverlapping = false)
    {
        var result = new List<int>();

        int matchIndex = haystack[startIndex..].IndexOf(needle);
        while (matchIndex >= 0)
        {
            result.Add(startIndex + matchIndex);

            startIndex += matchIndex + (includeOverlapping ? 1 : needle.Length);
            matchIndex = haystack[startIndex..].IndexOf(needle);
        }

        return result;
    }

    /// <summary>
    /// Gets whether this is empty, or contains only 0 bytes.
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    internal static bool IsEmpty(this ReadOnlySpan<byte> self)
    {
#if NETSTANDARD2_0_OR_GREATER
        return self.IsEmpty || self.ToArray().All(i => i == byte.MinValue);
#else
        return self.IsEmpty || self.Trim(byte.MinValue).IsEmpty;
#endif
    }

    internal static int ReadString(this ReadOnlySpan<byte> self, int start, out string result)
    {
        var length = self.Cast<int>(start) * 2; // times two as it is UTF-16
        result = self.Slice(start + sizeof(int), length).Cast<byte, char>().ToString();
        return sizeof(int) + length;
    }

    internal static int ReadString(this ReadOnlySpan<byte> self, int start, int length, out ReadOnlySpan<char> result)
    {
        result = self.Slice(start, length).Cast<byte, char>().TrimEnd('\0');
        return length;
    }

    #endregion
}

internal static class SpanExtensions
{
    #region typeof(T)

    /// <summary>
    /// Concatenates two Span<T> to one.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="span0"></param>
    /// <param name="span1"></param>
    /// <returns></returns>
    /// <seealso href="https://stackoverflow.com/a/62525830"/>
    internal static Span<T> Concat<T>(this Span<T> span0, Span<T> span1)
    {
        var result = new T[span0.Length + span1.Length].AsSpan();
        var start = 0;

        span0.CopyTo(result[start..]);
        start += span0.Length;

        span1.CopyTo(result[start..]);
        return result;
    }

    /// <summary>
    /// Concatenates three Span<T> to one.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="span0"></param>
    /// <param name="span1"></param>
    /// <param name="span2"></param>
    /// <returns></returns>
    /// <seealso href="https://stackoverflow.com/a/62525830"/>
    internal static Span<T> Concat<T>(Span<T> span0, Span<T> span1, Span<T> span2)
    {
        var result = new T[span0.Length + span1.Length + span2.Length].AsSpan();
        var start = 0;

        span0.CopyTo(result[start..]);
        start += span0.Length;

        span1.CopyTo(result[start..]);
        start += span1.Length;

        span2.CopyTo(result[start..]);
        return result;
    }

    #endregion

    #region typeof(byte)

    internal static IEnumerable<int> IndicesOf(this Span<byte> haystack, byte[] needle, int startIndex = 0, bool includeOverlapping = false)
    {
        var result = new List<int>();

        int matchIndex = haystack[startIndex..].IndexOf(needle);
        while (matchIndex >= 0)
        {
            result.Add(startIndex + matchIndex);

            startIndex += matchIndex + (includeOverlapping ? 1 : needle.Length);
            matchIndex = haystack[startIndex..].IndexOf(needle);
        }

        return result;
    }

    /// <summary>
    /// Gets whether this is empty, or contains only 0 bytes.
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    internal static bool IsEmpty(this Span<byte> self)
    {
#if NETSTANDARD2_0_OR_GREATER
        return self.IsEmpty || self.ToArray().All(i => i == byte.MinValue);
#else
        return self.IsEmpty || self.Trim(byte.MinValue).IsEmpty;
#endif
    }

    internal static Span<byte> UnescapeHashedIds(this Span<byte> input)
    {
        var result = input;

        foreach (var pair in Constants.BINARY_MAPPING)
        {
            var indices = result.IndicesOf(pair.Value).ToArray();
            if (indices.Length > 0)
            {
                var value = pair.Key.AsSpan();

                for (int i = 0; i < indices.Length; i++)
                {
                    var index = indices[i] + ((pair.Key.Length - pair.Value.Length) * i);

                    var before = result[..index];
                    var after = result.Slice(index + pair.Value.Length, result.Length - before.Length - pair.Value.Length);

                    result = Concat(before, value, after);
                }
            }
        }

        return result;
    }

    #endregion
}
