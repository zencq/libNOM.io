﻿using libNOM.cli.Args;

namespace libNOM.cli;


public partial class Executor
{
    [
        ArgActionMethod,
        ArgDescription("Writes plaintext JSON from stdin to a save file in the specified format."),
    ]
    public static void Write(WriteArgs args)
    {
        var json = Console.In.ReadToEnd();
        var platform = Enum.Parse<io.Enums.PlatformEnum>(args.Format.ToString());

        io.Global.Convert.ToSaveFile(json, platform, args.Index, args.Output?.FullName);
    }
}
