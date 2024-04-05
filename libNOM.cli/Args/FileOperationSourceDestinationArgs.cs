namespace libNOM.cli.Args;


public class FileOperationSourceDestinationArgs
{
    [ArgExistingDirectory, ArgRequired, ArgDescription("Directory of the platform to perform a file operation in."), ArgPosition(1)]
    public required DirectoryInfo Platform { get; set; }

    [ArgRange(0, 29), ArgRequired, ArgDescription("Specify the saves used as source. These indices are expected to be: 0 for Slot1Auto, 1 for Slot1Manual, 2 for Slot2Auto, etc)."), ArgPosition(2)]
    public required int[] Source { get; set; }

    [ArgRange(0, 29), ArgRequired, ArgDescription("Specify the saves used as destination."), ArgPosition(3)]
    public required int[] Destination { get; set; }
}