// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");



if (args.ContainsArgument("help", out _))
{
    Console.WriteLine($"There are 4 supported arguments:");
    Console.WriteLine($"/{argRoot}: Directory where all unpacked game files are stored. Each version must have a separate sub-directory with its number (e.g. '387' forOutlaws 3.87).");
    Console.WriteLine($"/{argJson}: Where to store the exported JSON database.");
    Console.WriteLine($"/{argResources}: Where to copy the gathered images.");
    Console.WriteLine($"/{argReuse}: Whether to reuse existing files instead of creating them again.");
    Console.WriteLine($"/{argTranslations}: Where to store the created translation files.");
    Console.WriteLine($"");
    Console.WriteLine($"Notes: For compatibility purposes, all options can be specified using these prefixes: '/', '-', '--'.");
    Console.WriteLine($"       If a path is invalid, it will fall back to a default one based onn the current direcroey.");
    return;
}








//var args = Environment.GetCommandLineArgs();
//if (args.Length > 1)
//{
//    ProcessCommandLineArgs(args);
//    Global.Exit();
//    return;
//}


//bool GetArgFlag(string[] args, string[] aliases)
//{
//    foreach (var alias in aliases.Select(a => $"-{a}"))
//    {
//        if (args.Contains(alias))
//            return true;
//    }
//    return false;
//}

//string? GetArgValue(string[] args, string[] aliases)
//{
//    foreach (var alias in aliases.Select(a => $"-{a}"))
//    {
//        if (args.Contains(alias))
//        {
//            var index = Array.IndexOf(args, alias) + 1;
//            if (args.ContainsIndex(index))
//            {
//                return args[index];
//            }
//        }
//    }
//    return null;
//}

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
