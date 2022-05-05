using System.Text;

namespace libNOM.io.Extensions;


public static class StringExtensions
{
    /// <summary>
    /// Encodes all the characters in the string into a sequence of bytes in UTF-16 format.
    /// </summary>
    /// <param name="input"></param>
    /// <returns>A byte array containing the results of encoding the set of characters.</returns>
    public static byte[] GetUnicodeBytes(this string input)
    {
        return Encoding.Unicode.GetBytes(input);
    }

    /// <summary>
    /// Encodes all the characters in the specified string into a sequence of bytes in UTF-8 format.
    /// </summary>
    /// <param name="input"></param>
    /// <returns>A byte array containing the results of encoding the set of characters.</returns>
    public static byte[] GetUTF8Bytes(this string input)
    {
        return Encoding.UTF8.GetBytes(input);
    }
}
