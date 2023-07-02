using CommunityToolkit.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace libNOM.io.Extensions;


public static class ReadOnlySpanExtensions
{
    #region typeof(byte)

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
    /// Creates an unsigned integer from this 4 bytes.
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    /// <seealso href="https://github.com/zencq/libNOM.io/issues/17"/>
    internal static uint CastToUInt32(this ReadOnlySpan<byte> self)
    {
        Guard.HasSizeEqualTo(self, 4);
        return MemoryMarshal.Cast<byte, uint>(self)[0];
    }

    #endregion
}
