namespace libNOM.cli.Args;


public class FileOperationPlatformIndicesArgs
{
    [ArgExistingDirectory, ArgRequired, ArgDescription("Directory of the platform to restore to."), ArgPosition(1)]
    public required DirectoryInfo Platform { get; set; }

    [ArgRange(0, 29), ArgRequired, ArgDescription("If Input* is a directory specify what to backup. These indices are expected to be: 0 for Slot1Auto, 1 for Slot1Manual, 2 for Slot2Auto, etc)."), ArgShortcut("N"), ArgPosition(2)]
    public required int[] Indices { get; set; }
}