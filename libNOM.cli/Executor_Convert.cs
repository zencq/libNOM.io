using libNOM.cli.Args;

namespace libNOM.cli;


public partial class Executor
{
    [
        ArgActionMethod,
        ArgDescription("Convert a save file from any format to any format."),
    ]
    public static void Convert(ConvertArgs args)
    {
        if (args.Format == Enums.FormatEnum.Json)
            _ = io.Global.Convert.ToJson(args.Input.FullName, args.Output?.FullName ?? args.Input.Directory!.FullName, args.JsonIndented, args.JsonDeobfuscated);
        else
        {
            var platform = Enum.Parse<io.Enums.PlatformEnum>(args.Format.ToString());
            io.Global.Convert.ToSaveFile(args.Input.FullName, platform, args.Output?.FullName);
        }
    }
}
