using System.Security.Cryptography;

using libNOM.cli.Args;
using libNOM.io;
using libNOM.io.Interfaces;
using libNOM.io.Settings;

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

    #region Getter

    private static PlatformCollectionSettings GetCollectionSettings() => new()
    {
        AnalyzeLocal = false,
    };

    private static PlatformSettings GetPlatformSettings(bool unlimited = true, bool trace = true) => new()
    {
        MaxBackupCount = unlimited ? 0 : 3, // 3 is default 
        Trace = trace,
    };

    #endregion

    #region Helper

    private bool GuardArgsLength(FileOperationTwoOperandArgs args) 
    {
        if (args.Source.Length != args.Destination.Length)
        {
            WriteLine("You must specify the same number of saves for Source and Destination to perform this file operation.");
            return false;
        }
        return true;
    }

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

    #region Analyze

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
#if DEBUG
        WriteLine(nameof(Convert));
#endif
    }

    [
        ArgActionMethod,
        ArgDescription("Adds the two operands"),
    ]
    public void Backup(BackupArgs args)
    {
#if DEBUG
        WriteLine(nameof(Backup));
#endif
        IEnumerable<Container>? containers = null;
        IPlatform? platform = null;

        if (Directory.Exists(args.Input))
        {
            if (args.Indices!.IsNullOrEmpty())
            {
                WriteLine("If the input is a directory, you must specify what you want to backup.", 0);
                return;
            }

            var collection = new PlatformCollection(args.Input, new() { MaxBackupCount = 0, Trace = true }, new() { AnalyzeLocal = false });

            platform = collection.FirstOrDefault();
            containers = platform?.GetSaveContainers().Where(i => args.Indices!.Contains(i.CollectionIndex));
        }
        else if (File.Exists(args.Input))
        {
            var container = PlatformCollection.AnalyzeFile(args.Input, new() { Trace = true });
            if (container is null)
            {
                WriteLine("Input file could not be successfully processed.", 0);
                return;
            }

            platform = container.Trace!.Platform;
            containers = [container];
        }

        if (containers is not null && platform is not null)
            foreach (var container in containers)
                platform.Backup(container);
    }

    [
        ArgActionMethod,
        ArgDescription("Adds the two operands"),
    ]
    public void Restore(RestoreArgs args)
    {
#if DEBUG
        WriteLine(nameof(Restore));
#endif

        // TODO if no indices, try extract original save from file name

    }

    [
        ArgActionMethod,
        ArgDescription("Copies the specified saves."),
        ArgExample("-s 1 2 -d 3 4", "Desc1"),
        ArgExample("-s 5 -d 6", "Desc2"),
    ]
    public void Copy(FileOperationTwoOperandArgs args)
    {
#if DEBUG
        WriteLine(nameof(Copy));
#endif
        if (!GuardArgsLength(args))
            return;
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
        ArgDescription("Swap any two save files. They will be swapped without any further questions or additional checks (e.g. you can end up with two completely different saves in one slot if you have auto and manual but only swap one with one from another slot)."),
        ArgExample("--platform <path-to-steam> --source 1 2 --destination 3 4", "Swap 1 with 3 and 2 with 4 on Steam."),
    ]
    public void Swap(FileOperationTwoOperandArgs args)
    {
#if DEBUG
        WriteLine(nameof(Swap));
#endif
        if (!GuardArgsLength(args))
            return;

        var collection = new PlatformCollection(args.Platform.FullName, GetCollectionSettings());
        var platform = collection.FirstOrDefault();
        if (platform is null)
        {
            WriteLine("No valid platform found.", 1);
            return;
        }

        var source = platform.GetSaveContainers().Where(i => args.Source.Contains(i.CollectionIndex));
        var destination = platform.GetSaveContainers().Where(i => args.Destination.Contains(i.CollectionIndex));

        platform.Swap(source.Zip(destination));
    }

    [
        ArgActionMethod,
        ArgDescription("Adds the two operands"),
    ]
    public void Move(FileOperationTwoOperandArgs args)
    {
#if DEBUG
        WriteLine(nameof(Move));
#endif
        if (!GuardArgsLength(args))
            return;

        var collection = new PlatformCollection(args.Platform.FullName, GetCollectionSettings());
        var platform = collection.FirstOrDefault();
        if (platform is null)
        {
            WriteLine("No valid platform found.", 1);
            return;
        }

        var source = platform.GetSaveContainers().Where(i => args.Source.Contains(i.CollectionIndex));
        var destination = platform.GetSaveContainers().Where(i => args.Destination.Contains(i.CollectionIndex));

        platform.Move(source.Zip(destination));
    }
}