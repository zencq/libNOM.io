namespace libNOM.cli.Args;


public class FileOperationTwoOperandArgs
{
    [ArgExistingDirectory, ArgRequired, ArgDescription("List of container indices used as source."), ArgPosition(1)]
    public required DirectoryInfo Platform { get; set; }

    [ArgRange(0, 29), ArgRequired, ArgDescription("List of container indices used as source."), ArgPosition(2)]
    public required int[] Source { get; set; }

    [ArgRange(0, 29), ArgRequired, ArgDescription("List of container indices used as destination."), ArgPosition(3)]
    public required int[] Destination { get; set; }
}