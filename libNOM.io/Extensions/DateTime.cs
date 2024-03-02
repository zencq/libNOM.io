namespace libNOM.io.Extensions;


internal static class DateTimeOffsetExtensions
{
    /// <summary>
    /// Nullifies the specified number of last digits.
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    internal static DateTimeOffset NullifyTicks(this DateTimeOffset self, int digits)
    {
        var ticks = self.Ticks % (long)(Math.Pow(10, digits)); // get last four digits
        return self.Subtract(new TimeSpan(ticks));
    }

    internal static DateTimeOffset? NullifyTicks(this DateTimeOffset? self, int digits) => self?.NullifyTicks(digits);
}
