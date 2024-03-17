// See https://aka.ms/new-console-template for more information
Args.InvokeAction<libNOM.cli.Executor>(args);

//void ProcessCommandLineArgs(string[] args)
//{
//    var input = GetArgValue(args, new[] { "input", "in", "i" });
//    if (input is null || !File.Exists(input))
//        throw new ArgumentException("Input not provided, is empty, or does not exist!");

//    var json = GetArgFlag(args, new[] { "json", "j" });
//    var save = GetArgFlag(args, new[] { "save", "s" });
//    if (save & json)
//        throw new InvalidOperationException("Both directions specified. Use only one at a time!");

//    var output = GetArgValue(args, new[] { "output", "out", "o" });
//    if (output is not null && !Directory.Exists(output))
//        throw new InvalidOperationException("Output specified, but is not a valid directory!");

//    if (json)
//    {
//        var indent = GetArgFlag(args, new[] { "indent", "d" });
//        var mapped = GetArgFlag(args, new[] { "mapped", "map", "p" });

//        libNOM.io.Globals.Convert.ToJson(input, output, indent, mapped);
//    }
//    else if (save)
//    {
//        var platform = GetArgValue(args, new[] { "platform", "f" });

//        var @gog = new[] { "gog.com", "gogcom", "gog" }.Contains(platform);
//        var @microsoft = new[] { "microsoft", "ms", "xbox", "xb", "x" }.Contains(platform);
//        var @playstation = new[] { "sony", "playstation", "ps", "ps4" }.Contains(platform);
//        var @steam = new[] { "steam", "st" }.Contains(platform);
//        var @switch = new[] { "nintendo", "switch", "sw" }.Contains(platform);

//        if (new[] { @gog, @microsoft, @playstation, @steam, @switch }.All(b => !b))
//            throw new InvalidOperationException("No output platform specified where to convert to!");

//        var platformEnum = libNOM.io.Enums.PlatformEnum.Unknown;
//        if (@gog)
//        {
//            platformEnum = libNOM.io.Enums.PlatformEnum.Gog;
//        }
//        else if (@microsoft)
//        {
//            platformEnum = libNOM.io.Enums.PlatformEnum.Microsoft;
//        }
//        else if (@playstation)
//        {
//            platformEnum = libNOM.io.Enums.PlatformEnum.Playstation;
//        }
//        else if (@steam)
//        {
//            platformEnum = libNOM.io.Enums.PlatformEnum.Steam;
//        }
//        //else if (@switch)
//        //{
//        //    platformEnum = libNOM.io.Enums.PlatformEnum.Switch;
//        //}

//        libNOM.io.Globals.Convert.ToSaveFile(input, platformEnum, output);
//    }
//}
