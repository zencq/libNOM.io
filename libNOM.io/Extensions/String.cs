using CommunityToolkit.HighPerformance;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

using System.Text;

namespace libNOM.io.Extensions;


public static class StringExtensions
{
    // public

    internal static JObject? GetJson(this string self)
    {
        if (JsonConvert.DeserializeObject(self) is JObject jsonObject)
            return jsonObject;

        return null;
    }

    // internal

    /// <summary>
    /// Encodes all the characters in the string into a sequence of bytes in UTF-16 format.
    /// </summary>
    /// <param name="self"></param>
    /// <returns>A byte array containing the results of encoding the set of characters.</returns>
    internal static byte[] GetUnicodeBytes(this string self)
    {
        return Encoding.Unicode.GetBytes(self);
    }

    /// <summary>
    /// Encodes all the characters in the specified string into a sequence of bytes in UTF-8 format.
    /// </summary>
    /// <param name="self"></param>
    /// <returns>A byte array containing the results of encoding the set of characters.</returns>
    internal static byte[] GetUTF8Bytes(this string self)
    {
        return Encoding.UTF8.GetBytes(self);
    }

    /// <inheritdoc cref="AsSpanSubstring(string, int, int)"/>
#if !NETSTANDARD2_0
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0057: Use range operator", Justification = "The range operator is not supported in netstandard2.0 and Slice() has no performance penalties.")]
#endif
    internal static ReadOnlySpan<char> AsSpanSubstring(this string self, int startIndex)
    {
        return self.AsSpan().Slice(startIndex);
    }

    /// <summary>
    /// Returns a substring of this string as <see cref="Span{T}"/>.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="startIndex"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    internal static ReadOnlySpan<char> AsSpanSubstring(this string self, int startIndex, int length)
    {
        return self.AsSpan().Slice(startIndex, length);
    }

    /// <summary>
    /// Prepares a save renaming text for writing.
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    internal static byte[] GetSaveRenamingBytes(this string self)
    {
#if NETSTANDARD2_0_OR_GREATER
        return $"{self.Substring(0, self.Length)}\0".GetUTF8Bytes();
#else
        return $"{self.AsSpanSubstring(0, self.Length)}\0".GetUTF8Bytes();
#endif
    }
}
