using System.Text.RegularExpressions;

namespace libNOM.io.Meta;


internal static partial class Context
{
    #region Regex

#if NETSTANDARD2_0_OR_GREATER
#pragma warning disable IDE0300 // Use collection expression for array
    private static readonly Regex[] RegexesActiveContext = new Regex[] { // keep this format to have Regex syntax highlighting
        new("\\\"XTp\\\":\\\"(\\w{4,8})\\\",", RegexOptions.Compiled, TimeSpan.FromMilliseconds(10)),
        new("\\\"ActiveContext\\\":\\\"(\\w{4,8})\\\",", RegexOptions.Compiled, TimeSpan.FromMilliseconds(10)),
    };

    private static readonly Regex[] RegexesBaseContext = new Regex[] { // keep this format to have Regex syntax highlighting
        new("\\\"vLc\\\":{", RegexOptions.Compiled, TimeSpan.FromMilliseconds(1000)),
        new("\\\"BaseContext\\\":{", RegexOptions.Compiled, TimeSpan.FromMilliseconds(1000)),
    };

    private static readonly Regex[] RegexesExpeditionContext = new Regex[] { // keep this format to have Regex syntax highlighting
        new("\\\"2YS\\\":{", RegexOptions.Compiled, TimeSpan.FromMilliseconds(1000)),
        new("\\\"ExpeditionContext\\\":{", RegexOptions.Compiled, TimeSpan.FromMilliseconds(1000)),
    };
#pragma warning restore IDE0300
#else
    [GeneratedRegex("\\\"XTp\\\":\\\"(\\w{4,8})\\\",", RegexOptions.Compiled, 10)]
    private static partial Regex RegexObfuscatedActiveContext();

    [GeneratedRegex("\\\"ActiveContext\\\":\\\"(\\w{4,8})\\\",", RegexOptions.Compiled, 10)]
    private static partial Regex RegexPlaintextActiveContext();

    [GeneratedRegex("\\\"vLc\\\":{", RegexOptions.Compiled, 1000)]
    private static partial Regex RegexObfuscatedBaseContext();

    [GeneratedRegex("\\\"BaseContext\\\":{", RegexOptions.Compiled, 1000)]
    private static partial Regex RegexPlaintextBaseContext();

    [GeneratedRegex("\\\"2YS\\\":{", RegexOptions.Compiled, 1000)]
    private static partial Regex RegexObfuscatedExpeditionContext();

    [GeneratedRegex("\\\"ExpeditionContext\\\":{", RegexOptions.Compiled, 1000)]
    private static partial Regex RegexPlaintextExpeditionContext();

    private static readonly Regex[] RegexesActiveContext = [
        RegexObfuscatedActiveContext(),
        RegexPlaintextActiveContext(),
    ];

    private static readonly Regex[] RegexesBaseContext = [
        RegexObfuscatedBaseContext(),
        RegexPlaintextBaseContext(),
    ];

    private static readonly Regex[] RegexesExpeditionContext = [
        RegexObfuscatedExpeditionContext(),
        RegexPlaintextExpeditionContext(),
    ];
#endif

    #endregion

    #region Getter

    /// <summary>
    /// Gets the active context from the save.
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    internal static SaveContextQueryEnum GetActive(string? json)
    {
        if (RegexesActiveContext.Match(json)?.ToStringValue() is string value)
            return EnumExtensions.Parse<SaveContextQueryEnum>(value) ?? SaveContextQueryEnum.DontCare;

        return SaveContextQueryEnum.DontCare;
    }

    /// <summary>
    /// Gets whether the context can be changed in the save.
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    internal static bool CanSwitch(string? json) => RegexesBaseContext.HasMatch(json) && RegexesExpeditionContext.HasMatch(json);

    #endregion
}
