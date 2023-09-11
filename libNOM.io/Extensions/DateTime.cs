namespace libNOM.io.Extensions;


internal static class DateTimeOffsetExtensions
{
    /// <summary>
    /// Nullifies the last four digits of the current <see cref="DateTimeOffset"/> object.
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    internal static DateTimeOffset ToBlobFileTime(this DateTimeOffset self)
    {
        var ticks = self.Ticks % (long)(Math.Pow(10, 4)) * -1; // get last four digits negative
        return self.AddTicks(ticks);
    }
}
