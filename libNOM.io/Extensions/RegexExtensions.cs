using System.Text.RegularExpressions;

namespace libNOM.io.Extensions;


internal static partial class RegexExtensions
{
    internal static bool MatchToInt32(System.Text.RegularExpressions.Regex regex, string input, out int result)
    {
        result = 0;

        if (GetMatch(regex, input) is string stringValue)
        {
            result = System.Convert.ToInt32(stringValue);
            return true;
        }
        return false;
    }

    internal static bool MatchToString(System.Text.RegularExpressions.Regex regex, string input, out string result)
    {
        result = string.Empty;

        if (GetMatch(regex, input) is string stringValue)
        {
            result = stringValue;
            return true;
        }
        return false;
    }

    internal static bool MatchesToInt32(System.Text.RegularExpressions.Regex regex, string input, out int[] result)
    {
        result = GetMatches(regex, input).Select(i => System.Convert.ToInt32(i)).ToArray();
        return result.Length != 0;
    }

    internal static bool MatchesToString(System.Text.RegularExpressions.Regex regex, string input, out string[] result)
    {
        result = [.. GetMatches(regex, input)];
        return result.Length != 0;
    }

    #region GetMatch(es)

    private static string? GetMatch(System.Text.RegularExpressions.Regex regex, string input)
    {
        try
        {
            var match = regex.Match(input);
            return match.Success ? match.Groups[0].Value : null;
        }
        catch (RegexMatchTimeoutException)
        {
            return null;
        }
    }
    private static List<string> GetMatches(System.Text.RegularExpressions.Regex regex, string input)
    {
        try
        {
            List<string> result = [];

            foreach (Match match in regex.Matches(input))
                result.Add(match.Groups[1].Value);

            return result;
        }
        catch (RegexMatchTimeoutException)
        {
            return [];
        }
    }

    #endregion
}
