namespace libNOM.io.Extensions;


internal static class DateTimeOffsetExtensions
{
    /// <summary>
    /// Nullifies the specified number of last digits.
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    internal static DateTimeOffset NullifyTicks(this DateTimeOffset self, int digits) => self.Subtract(new TimeSpan((long)(Math.Pow(10, digits))));
}
