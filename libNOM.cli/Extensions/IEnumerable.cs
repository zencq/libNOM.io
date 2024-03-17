namespace libNOM.cli.Extensions;


internal static class IEnumerableExtensions
{
    #region typeof(string)

    internal static bool ContainsArgument(this IEnumerable<string> input, string arg, out string usedArg)
    {
        usedArg = string.Empty;

        if (input.Contains($"--{arg}"))
            usedArg = $"--{arg}";
        else if (input.Contains($"-{arg}"))
            usedArg = $"-{arg}";
        else if (input.Contains($"/{arg}"))
            usedArg = $"/{arg}";

        return !string.IsNullOrEmpty(usedArg);
    }

    #endregion
}
