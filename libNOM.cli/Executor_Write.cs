using libNOM.cli.Args;

namespace libNOM.cli;


public partial class Executor
{
    [
        ArgActionMethod,
        ArgDescription("Writes plaintext JSON from stdin to a save file in the specified format."),
    ]
    public static void Write(WriteArgs args)
    {
        // 1. Read from stdin
        // 2. Determine Platform/Location
        // 3. Write

        //var json = Console.In.ReadToEnd();
        var json = io.Global.Convert.ToJson("C:\\Users\\Christian\\AppData\\Roaming\\HelloGames\\NMS\\st_76561198042453834\\save3.hg", false, false);

        var platform = Enum.Parse<io.Enums.PlatformEnum>(args.Format.ToString());
        io.Global.Convert.ToSaveFile(json, platform, args.Output?.FullName); // TODO: change to take a file or a JSON string
    }
}
