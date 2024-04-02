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

    [HelpHook, ArgShortcut("-?"), ArgDescription("Shows this help. Please not that all file operations are executed without any further questions or checks (e.g. you can end up with two completely different saves in one slot if you have auto and manual but only do one with one from another slot).")]
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

    // //

    #region Helper

    private static bool GetPlatformFromDirectory(DirectoryInfo directory, out IPlatform? platform)
    {
        var collection = new PlatformCollection(directory.FullName, GetCollectionSettings());

        platform = collection.FirstOrDefault();
        if (platform is null)
        {
            WriteLine("No valid platform found.", 1);
            return false;
        }
        return true;
    }

    private static bool GuardArgsLength(FileOperationSourceDestinationArgs args)
    {
        if (args.Source.Length != args.Destination.Length)
        {
            WriteLine("You must specify the same number of saves for Source and Destination to perform this file operation.", 1);
            return false;
        }
        return true;
    }

    private static void PreprocessFileOperation(FileOperationSourceDestinationArgs args, out IPlatform? platform, out IEnumerable<(Container Source, Container Destination)> operationData)
    {
        if (GetPlatformFromDirectory(args.Platform, out platform) && GuardArgsLength(args)) // GatherPlatformFromCollection first to ensure platform is set
        {
            var source = platform!.GetSaveContainers().Where(i => args.Source.Contains(i.CollectionIndex));
            var destination = platform.GetSaveContainers().Where(i => args.Destination.Contains(i.CollectionIndex));

            operationData = source.Zip(destination);
        }
        else
            operationData = [];
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

    // //

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

    #region Backup

    [
        ArgActionMethod,
        ArgDescription("Create a backup of all specified saves. No old backups will be deleted in this process."),
        ArgExample("-Input <path-to-save-location> -Indices 0 29", "Backup Slot1Auto and Slot15Manual."), // directory
        ArgExample("-Input <path-to-save-location>/save.hg", "Backup Slot1Auto."), // file
    ]
    public static void Backup(BackupArgs args)
    {
        IPlatform? platform = null;
        IEnumerable<Container>? containers = null;

        if (Directory.Exists(args.Input))
            GetBackupDataFromDirectory(args, out platform, out containers);
        else if (File.Exists(args.Input))
            GetBackupDataFromFile(args, out platform, out containers);

        if (platform is null && containers!.IsNullOrEmpty())
        {
            WriteLine("Could not find valid platform files or a save.", 1);
            return;
        }

        foreach (var container in containers!)
            platform!.Backup(container);
    }

    private static void GetBackupDataFromDirectory(BackupArgs args, out IPlatform? platform, out IEnumerable<Container> containers)
    {
        if (args.Indices!.IsNullOrEmpty())
        {
            WriteLine("If the input is a directory, you must specify what you want to backup.", 1);
            platform = null;
            containers = [];
            return; // early exit
        }

        var collection = new PlatformCollection(args.Input, GetPlatformSettings(), GetCollectionSettings());

        platform = collection.FirstOrDefault();
        containers = platform?.GetSaveContainers().Where(i => args.Indices!.Contains(i.CollectionIndex)) ?? [];
    }

    private static void GetBackupDataFromFile(BackupArgs args, out IPlatform? platform, out IEnumerable<Container> containers)
    {
        var container = io.Global.Analyze.AnalyzeFile(args.Input, GetPlatformSettings());
        if (container is null)
        {
            WriteLine("Input file could not be successfully processed.", 1);
            platform = null;
            containers = [];
            return; // early exit
        }

        platform = container.Trace!.Platform;
        containers = [container];
    }

    [
        ArgActionMethod,
        ArgDescription("Restore the specified backup."),
        ArgExample("-Backup <path-to-backup-location>/backup.steam.02.20240402162157827.440.zip -Platform <path-to-save-location> -Index 5", "Restore to Slot3Manual."), // with index
        ArgExample("-Backup <path-to-backup-location>/backup.steam.02.20240402162157827.440.zip -Platform <path-to-save-location>", "Restore to Slot1Auto."), // without index
    ]
    public static void Restore(RestoreArgs args)
    {
        var parts = Path.GetFileNameWithoutExtension(args.Backup.Name).Split('.');

        var collection = new PlatformCollection(args.Platform, GetPlatformSettings(), GetCollectionSettings());
        var platform = collection.FirstOrDefault();

        if (platform is null || parts.Length < 5)
        {
            WriteLine("Could not find valid platform files or backup file is in the wrong format.", 1);
            return;
        }

        var backup = platform.CreateBackupContainer(args.Backup.FullName, args.Index ?? System.Convert.ToInt32(parts[3]));
        if (backup is null)
        {
            WriteLine("Backup could not be read.", 1);
            return;
        }

        platform.Restore(backup);
    }

    #endregion

    #region Convert

    [
        ArgActionMethod,
        ArgDescription("Adds the two operands"),
    ]
    public void Convert(ConvertArgs args)
    {
    }

    #endregion

    #region File Operation

    [
        ArgActionMethod,
        ArgDescription("Copy any save files."),
        ArgExample("--Platform <path-to-save-location> --Source 1 2 --Destination 3 4", "Copy Slot1Manual to Slot2Manual and Slot2Auto to Slot3Auto."),
    ]
    public static void Copy(FileOperationSourceDestinationArgs args)
    {
        PreprocessFileOperation(args, out var platform, out var data);
        platform?.Copy(data);
    }

    [
        ArgActionMethod,
        ArgDescription("Delete any save files."),
        ArgExample("--Platform <path-to-save-location> --Indices 0 29", "Delete Slot1Auto and Slot15Manual."),
    ]
    public static void Delete(FileOperationPlatformIndicesArgs args)
    {
        if (GetPlatformFromDirectory(args.Platform, out var platform))
        {
            var containers = platform!.GetSaveContainers().Where(i => args.Indices.Contains(i.CollectionIndex));
            platform!.Delete(containers);
        }
    }

    [
        ArgActionMethod,
        ArgDescription("Swap any save files."),
        ArgExample("--Platform <path-to-save-location> --Source 1 2 --Destination 3 4", "Swap Slot1Manual with Slot2Manual and Slot2Auto with Slot3Auto."),
    ]
    public static void Swap(FileOperationSourceDestinationArgs args)
    {
        PreprocessFileOperation(args, out var platform, out var data);
        platform?.Swap(data);
    }

    [
        ArgActionMethod,
        ArgDescription("Move any save files."),
        ArgExample("--Platform <path-to-save-location> --Source 1 2 --Destination 3 4", "Move Slot1Manual to Slot2Manual and Slot2Auto to Slot3Auto."),
    ]
    public static void Move(FileOperationSourceDestinationArgs args)
    {
        PreprocessFileOperation(args, out var platform, out var data);
        platform?.Move(data);
    }

    #endregion
}