using System.Text.RegularExpressions;

namespace libNOM.io.Extensions;


internal static partial class MatchExtensions
{
    /// <summary>
    /// Converts the captured substring to an integer.
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    /// <exception cref="FormatException" />
    /// <exception cref="OverflowException" />
    internal static int ToInt32Value(this Match self) => System.Convert.ToInt32(self.Groups[1].Value);

    /// <summary>
    /// Converts the captured substring to an unsigned integer.
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    /// <exception cref="FormatException" />
    /// <exception cref="OverflowException" />
    internal static ulong ToUInt64Value(this Match self) => System.Convert.ToUInt64(self.Groups[1].Value);

    /// <Gets>
    /// Gets the captured substring.
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    internal static string ToStringValue(this Match self) => self.Groups[1].Value;
}
