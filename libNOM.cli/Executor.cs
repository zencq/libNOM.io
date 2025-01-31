using libNOM.io.Settings;

namespace libNOM.cli;


[ArgExceptionBehavior(ArgExceptionPolicy.StandardExceptionHandling)]
public partial class Executor
{
    #region Constant

    private const int INDENTION_SIZE = 2;

    #endregion

    #region Property

    [HelpHook, ArgDescription("Shows this help. Please not that all file operations are executed without any further questions or checks (e.g. you can end up with two completely different saves in one slot if you have auto and manual but only do one with one from another slot).")]
    public bool Help { get; set; }

    #endregion

    #region Getter

    private static PlatformCollectionSettings GetCollectionSettings() => new()
    {
        AnalyzeLocal = false,
    };

    private static PlatformSettings GetPlatformSettings(bool unlimited = true, bool trace = true) => new()
    {
        MaxBackupCount = unlimited ? int.MaxValue : 3, // 3 is default 
        Trace = trace,
    };

    #endregion

    // //

    #region Helper

    private static void WriteLine(string message) => WriteLine(message, 0);

    private static void WriteLine(string message, int indentionLevel)
    {
        Console.WriteLine($"{"".PadLeft(indentionLevel * INDENTION_SIZE)}{message}");
    }

    private static void WriteLine(string message, int indentionLevel, bool interpolation)
    {
        var msg = string.Format(message, interpolation ? "yes" : "no");
        Console.WriteLine($"{"".PadLeft(indentionLevel * INDENTION_SIZE)}{msg}");
    }

    #endregion
}
