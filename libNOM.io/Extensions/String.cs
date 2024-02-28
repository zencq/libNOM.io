using System.Text;

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

    /// <summary>
    /// Returns a substring of this string as <see cref="Span{T}"/>.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="startIndex"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    internal static ReadOnlySpan<char> AsSpanSubstring(this string self, int startIndex, int length) => self.AsSpan().Slice(startIndex, length);
}
