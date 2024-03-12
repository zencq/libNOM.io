using System.Text.RegularExpressions;

namespace libNOM.io.Extensions;


public static class IEnumerableExtensions
{
    #region typeof(Container)

    /// <summary>
    /// Gets the maximum number of slots based on the <see cref="Container"/> count.
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    public static int GetMaximumSlots(this IEnumerable<Container> self)
    {
        return self.Count() / 2;
    }

    #endregion

    #region typeof(RegEx)

    /// <summary>
    /// Searches the input string for the first occurrence of the specified regular expressions.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="json"></param>
    /// <returns></returns>
    internal static Match? Match(this IEnumerable<Regex> self, string? json)
    {
        if (json is not null)
        {
            foreach (var regex in self)
                if (regex.Match(json) is Match match && match.Success)
                    return match;
        }
        return null;
    }

    /// <summary>
    /// Searches the input string for all occurrences of the specified regular expressions.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="json"></param>
    /// <returns></returns>
    internal static MatchCollection? Matches(this IEnumerable<Regex> self, string? json)
    {
        if (json is not null)
        {
            foreach (var regex in self)
                if (regex.Matches(json) is MatchCollection collection && collection.Count != 0)
                    return collection;
        }
        return null;
    }

    #endregion

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
        var groups = self.Where(i => !string.IsNullOrEmpty(i)).GroupBy(j => j);
        if (groups.Any())
        {
            var maxCount = groups.Max(i => i.Count());
            return groups.First(i => i.Count() == maxCount).Key;
        }
        return null;
    }

    #endregion
}
