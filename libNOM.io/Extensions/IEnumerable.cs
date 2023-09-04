namespace libNOM.io.Extensions;


public static class IEnumerableExtensions
{
    #region typeof(T)

    /// <summary>
    /// Determines whether a sequence is long enough to be accessed with the specified index.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    internal static bool ContainsIndex<T>(this IEnumerable<T> self, int index)
    {
        return 0 <= index && index < self.Count();
    }

    #endregion

    #region typeof(byte)

    /// <summary>
    /// Whether this enumerable is empty, or contains only 0.
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    internal static bool IsEmpty(this IEnumerable<byte> self)
    {
        return !self.Any() || self.All(i => i == byte.MinValue);
    }

    #endregion

    #region typeof(string)

    /// <summary>
    /// Gets the most common string.
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    /// <seealso href="https://stackoverflow.com/a/39599083"/>
    internal static string? MostCommon(this IEnumerable<string?> self)
    {
        self = self.Where(i => !string.IsNullOrWhiteSpace(i));

        if (!self.Any())
            return null;

        var groups = self.GroupBy(i => i);
        if (!groups.Any())
            return null;

        var max = groups.Max(i => i.Count());
        return groups.First(i => i.Count() == max).Key;
    }

    #endregion
}
