using libNOM.cli.Args;
using libNOM.io;
using libNOM.io.Interfaces;
using libNOM.io.Settings;

namespace libNOM.cli;


public partial class Executor
{
    [
        ArgActionMethod,
        ArgDescription("Analyze a file or directory and print all kind of information about it."),
    ]
    public void Analyze(AnalyzeArgs args)
    {
#if DEBUG
        WriteLine(nameof(Analyze));
#endif
        if (Directory.Exists(args.Input))
        {
            AnalyzeDirectory(new(args.Input), 0);
        }
        else if (File.Exists(args.Input))
        {
            AnalyzeFile(new(args.Input), 0);
        }
    }

    private static void AnalyzeFile(FileInfo info, int indentionLevel)
    {
        // If that file, analyze directory to automatically get the most information.
        if (info.Name.Equals("containers.index"))
        {
            AnalyzeDirectory(info.Directory!, 0);
            return;
        }

        WriteLine(info.Name, indentionLevel);

        var container = io.Global.Analyze.AnalyzeFile(info.FullName);

        PrintContainerInformation(container, indentionLevel + 1);
    }

    private static void PrintContainerInformation(Container? container, int indentionLevel)
    {
        if (container is null)
        {
            WriteLine("Analysis failed.", indentionLevel);
            return;
        }

        WriteLine($"Backups: {container.BackupCollection.Count}", indentionLevel);
        WriteLine($"Identifier: {container.Identifier}", indentionLevel);
        WriteLine($"Incompatibility: {container.IncompatibilityException?.Message ?? container.IncompatibilityTag}", indentionLevel);
        WriteLine($"UnknownKeys: {string.Join(", ", container.UnknownKeys)}", indentionLevel);

        WriteLine($"CanSwitchContext: {container.CanSwitchContext}", indentionLevel);
        WriteLine($"HasActiveExpedition: {container.HasActiveExpedition}", indentionLevel);
        WriteLine($"HasBase: {container.HasBase}", indentionLevel); // count of base / base parts
        WriteLine($"HasFreighter: {container.HasFreighter}", indentionLevel);
        WriteLine($"HasSettlement: {container.HasSettlement}", indentionLevel);
        WriteLine($"IsAccount: {container.IsAccount}", indentionLevel);
        WriteLine($"IsBackup: {container.IsBackup}", indentionLevel);
        WriteLine($"IsCompatible: {container.IsCompatible}", indentionLevel);
        WriteLine($"IsLoaded: {container.IsLoaded}", indentionLevel);
        WriteLine($"IsOld: {container.IsOld}", indentionLevel);
        WriteLine($"IsSave: {container.IsSave}", indentionLevel);
        WriteLine($"IsSynced: {container.IsSynced}", indentionLevel);

        WriteLine($"DataFile: {container.DataFile?.Length}", indentionLevel);
        WriteLine($"LastWriteTime: {container.LastWriteTime}", indentionLevel);
        WriteLine($"MetaFile: {container.MetaFile?.Length}", indentionLevel);

        WriteLine($"ActiveContext: {container.ActiveContext}", indentionLevel);
        WriteLine($"Difficulty: {container.Difficulty}", indentionLevel);
        WriteLine($"GameVersion: {container.GameVersion}", indentionLevel);
        WriteLine($"SaveName: {container.SaveName}", indentionLevel);
        WriteLine($"SaveSummary: {container.SaveSummary}", indentionLevel);
        WriteLine($"SaveType: {container.SaveType}", indentionLevel);
        WriteLine($"Season: {container.Season}", indentionLevel);
        WriteLine($"TotalPlayTime: {container.TotalPlayTime}", indentionLevel);

        //WriteLine($"Extra.: {container.Extra.}", indentionLevel);

        //WriteLine($"UserIdentification.: {container.UserIdentification.}", indentionLevel);

        //WriteLine($"BaseVersion: {container.BaseVersion}", indentionLevel);
        //WriteLine($"GameMode: {container.GameMode}", indentionLevel);
        //WriteLine($"MetaFormat: {container.MetaFormat}", indentionLevel);
        //WriteLine($"MetaSize: {container.MetaSize}", indentionLevel);
        //WriteLine($"SaveVersion: {container.SaveVersion}", indentionLevel);
        //WriteLine($"PersistentStorageSlot: {container.PersistentStorageSlot}", indentionLevel);
        //WriteLine($"Platform: {container.Platform}", indentionLevel);





    }

    private static void AnalyzeDirectory(DirectoryInfo info, int indentionLevel)
    {
        WriteLine(info.Name, indentionLevel);

        var collection = new PlatformCollection(info.FullName, new() { Trace = true }, new() { AnalyzeLocal = false });
        var platform = collection.FirstOrDefault();
        if (platform is null)
        {
            WriteLine("Analysis failed.", indentionLevel);
            return;
        }

    }


}