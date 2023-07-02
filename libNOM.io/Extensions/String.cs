using System.Text;
using System.Text.RegularExpressions;

namespace libNOM.io.Extensions;


public static class StringExtensions
{


    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <seealso href="https://stackoverflow.com/a/1615860"/>
    internal static string EscapeDataString(this string self)
    {
        var builder = new StringBuilder();

        foreach (var charValue in self)
        {
            // Escaping gone wrong by HG. The backslash is in the file but instead of one of the chars below, still the unescaped control char.
            if (charValue == 0x09)
            {
                builder.Append('t');
            }
            else if (charValue == 0x0A)
            {
                builder.Append('n');
            }
            else if (charValue == 0x0D)
            {
                builder.Append('r');
            }
            else
            {
                builder.Append(charValue);
            }
        }
        return builder.ToString();
    }

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

    internal static bool IsAllDigits(this string self)
    {
        return self.All(char.IsDigit);
    }

    /// <summary>
    /// Converts this string to a character <see cref="ReadOnlySpan{T}"/>.
    /// </summary>
    /// <param name="self"></param>
    /// <returns>A byte array containing the results of encoding the set of characters.</returns>
    internal static ReadOnlySpan<char> ToReadOnlySpan(this string self)
    {
#if NETSTANDARD2_0
        return self.ToCharArray();
#else
        return self;
#endif
    }
}
