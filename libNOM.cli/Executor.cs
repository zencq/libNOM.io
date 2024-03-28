using libNOM.cli.Args;
using libNOM.io;

namespace libNOM.cli;


[ArgExceptionBehavior(ArgExceptionPolicy.StandardExceptionHandling)]
public partial class Executor
{
    #region Constant

    private const int INDENTION_SIZE = 2;

    #endregion

    #region Property

    [HelpHook, ArgShortcut("-?"), ArgDescription("Shows this help")]
    public bool Help { get; set; }

    #endregion

    private static void WriteLine(string message, int indentionLevel)
    {
        Console.WriteLine($"{"".PadLeft(indentionLevel * INDENTION_SIZE)}{message}");
    }


    #region Analyze

    [
        ArgActionMethod,
        ArgDescription("Analyze a file or directory and print all kind of information about it."),
    ]
    public void Analyze(AnalyzeArgs args)
    {
        Console.WriteLine(nameof(Analyze));
        if (Directory.Exists(args.Input))
        {
            AnalyzeDirectory(new(args.Input), 0);
        }
        else if (File.Exists(args.Input))
        {
            AnalyzeFile(new(args.Input), 0);
        }
    }

    private void AnalyzeFile(FileInfo info, int indentionLevel)
    {
        // If that file, analyze directory to automatically get the most information.
        if (info.Name.Equals("containers.index"))
        {
            AnalyzeDirectory(info.Directory!, 0);
            return;
        }

        WriteLine(info.Name, indentionLevel);

        var container = PlatformCollection.AnalyzeFile(info.FullName);

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

    private void AnalyzeDirectory(DirectoryInfo info, int indentionLevel)
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

    #endregion

    [
        ArgActionMethod,
        ArgDescription("Adds the two operands"),
    ]
    public void Convert(ConvertArgs args)
    {
        Console.WriteLine("Convert");
    }

    [
        ArgActionMethod,
        ArgDescription("Adds the two operands"),
    ]
    public void Backup(FileOperationOneOperandArgs args)
    {
        Console.WriteLine(nameof(Backup));

        IEnumerable<Container>? containers = null;

        if (Directory.Exists(args.Input))
        {
            if (args.Indices!.IsNullOrEmpty())
            {
                WriteLine("If the input is a directory, you must specify what you want to backup.", 0);
                return;
            }

            var collection = new PlatformCollection(args.Input, new() { Trace = true }, new() { AnalyzeLocal = false });

            containers = collection.FirstOrDefault()?.GetSaveContainers().Where(i => args.Indices!.Contains(i.CollectionIndex));
        }
        else if (File.Exists(args.Input))
        {
            var container = PlatformCollection.AnalyzeFile(args.Input);
            if (container is null)
            {
                WriteLine("Input file could not be successfully processed.", 0);
                return;
            }

            containers = [container];
        }

        //if (containers is not null)
        //    containers.Select(i => i.tr)
    }

    [
        ArgActionMethod,
        ArgDescription("Adds the two operands"),
    ]
    public void Restore(FileOperationOneOperandArgs args)
    {
        Console.WriteLine("Restore");

    }

    [
        ArgActionMethod,
        ArgDescription("Copies the specified saves."),
        ArgExample("-s 1 2 -d 3 4", "Desc1"),
        ArgExample("-s 5 -d 6", "Desc2"),
    ]
    public void Copy(FileOperationTwoOperandArgs args)
    {
        Console.WriteLine("Copy");
    }

    [
        ArgActionMethod,
        ArgDescription("Adds the two operands"),
    ]
    public void Delete(FileOperationOneOperandArgs args)
    {
        Console.WriteLine("Delete");
    }

    [
        ArgActionMethod,
        ArgDescription("Adds the two operands"),
    ]
    public void Swap(FileOperationTwoOperandArgs args)
    {
        Console.WriteLine("Swap");
    }

    [
        ArgActionMethod,
        ArgDescription("Adds the two operands"),
    ]
    public void Move(FileOperationTwoOperandArgs args)
    {
        Console.WriteLine("Move");
    }
}