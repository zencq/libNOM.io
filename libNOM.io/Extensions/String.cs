using System.Text;

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
            if (charValue < 0x20 || charValue >= 0x7F)
            {
                // This character is too big for ASCII or a control character.
                builder.Append($"\\u{(int)(charValue):x4}");
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
    public static byte[] GetUnicodeBytes(this string self)
    {
        return Encoding.Unicode.GetBytes(self);
    }

    /// <summary>
    /// Encodes all the characters in the specified string into a sequence of bytes in UTF-8 format.
    /// </summary>
    /// <param name="self"></param>
    /// <returns>A byte array containing the results of encoding the set of characters.</returns>
    public static byte[] GetUTF8Bytes(this string self)
    {
        return Encoding.UTF8.GetBytes(self);
    }
}
