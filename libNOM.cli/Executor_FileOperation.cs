using libNOM.cli.Args;
using libNOM.io;
using libNOM.io.Interfaces;

namespace libNOM.cli;


public partial class Executor
{
    #region Helper

    private static bool GuardArgsLength(FileOperationSourceDestinationArgs args)
    {
        if (args.Source.Length != args.Destination.Length)
        {
            WriteLine("You must specify the same number of saves for Source and Destination to perform this file operation.", 1);
            return false;
        }
        return true;
    }

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

    private static void PreprocessFileOperation(FileOperationSourceDestinationArgs args, out IPlatform? platform, out IEnumerable<(IContainer Source, IContainer Destination)> operationData)
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

    #endregion

    // //

    #region File Operation

    [
        ArgActionMethod,
        ArgDescription("Copy any save files."),
        ArgExample("--Platform <path-to-save-location-a> --Source 1 2 --Destination 3 4", "Copy Slot1Manual to Slot2Manual and Slot2Auto to Slot3Auto."),
    ]
    public static void Copy(FileOperationSourceDestinationArgs args)
    {
        PreprocessFileOperation(args, out var platform, out var data);
        platform?.Copy(data);
    }

    [
        ArgActionMethod,
        ArgDescription("Delete any save files."),
        ArgExample("--Platform <path-to-save-location-b> --Indices 0 29", "Delete Slot1Auto and Slot15Manual."),
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
        ArgExample("--Platform <path-to-save-location-c> --Source 3 4 --Destination 5 6", "Swap Slot2Manual with Slot3Manual and Slot3Auto with Slot4Auto."),
    ]
    public static void Swap(FileOperationSourceDestinationArgs args)
    {
        PreprocessFileOperation(args, out var platform, out var data);
        platform?.Swap(data);
    }

    [
        ArgActionMethod,
        ArgDescription("Move any save files."),
        ArgExample("--Platform <path-to-save-location-d> --Source 5 6 --Destination 7 8", "Move Slot3Manual to Slot4Manual and Slot4Auto to Slot5Auto."),
    ]
    public static void Move(FileOperationSourceDestinationArgs args)
    {
        PreprocessFileOperation(args, out var platform, out var data);
        platform?.Move(data);
    }

    #endregion
}
