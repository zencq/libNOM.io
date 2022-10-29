﻿using System.Text;

namespace libNOM.io.Extensions;


public static class StringExtensions
{
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
