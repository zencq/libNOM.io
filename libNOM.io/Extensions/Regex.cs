using System.Text.RegularExpressions;

namespace libNOM.io.Extensions;


internal static partial class MatchExtensions
{
    /// <summary>
    /// Gets the captured substring from the input string as an integer.
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    /// <exception cref="FormatException" />
    /// <exception cref="OverflowException" />
    internal static int ToInt32Value(this Match self) => System.Convert.ToInt32(self.Groups[1].Value);

    /// <Gets>
    /// Gets the captured substring from the input string.
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    internal static string ToStringValue(this Match self) => self.Groups[1].Value;
}
