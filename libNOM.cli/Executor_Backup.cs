using libNOM.cli.Args;
using libNOM.io;
using libNOM.io.Interfaces;

namespace libNOM.cli;


public partial class Executor
{
    [
        ArgActionMethod,
        ArgDescription("Create a backup of all specified saves. No old backups will be deleted in this process."),
        ArgExample("-Input <path-to-save-location> -Indices 0 29", "Backup Slot1Auto and Slot15Manual."), // directory
        ArgExample("-Input <path-to-save-location>/save.hg", "Backup Slot1Auto."), // file
    ]
    public static void Backup(BackupArgs args)
    {
        IPlatform? platform = null;
        IEnumerable<IContainer>? containers = null;

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

    private static void GetBackupDataFromDirectory(BackupArgs args, out IPlatform? platform, out IEnumerable<IContainer> containers)
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

    private static void GetBackupDataFromFile(BackupArgs args, out IPlatform? platform, out IEnumerable<IContainer> containers)
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
}