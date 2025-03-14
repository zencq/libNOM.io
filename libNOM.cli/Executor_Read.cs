using System.Text;

using libNOM.cli.Args;

namespace libNOM.cli;


public partial class Executor
{
    [
        ArgActionMethod,
        ArgDescription("Reads a save file from any format to stdout."),
    ]
    public static void Read(ReadArgs args)
    {
        var json = io.Global.Convert.ToJson(args.Input.FullName, false, args.JsonHumanReadable);

        Console.OutputEncoding = Encoding.UTF8;
        Console.Out.Write(json);
    }
}
