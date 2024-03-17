namespace libNOM.cli.Args;


public class FileOperationOneOperandArgs
{
    [ArgRequired, ArgDescription("List of container indices to work with."), ArgPosition(1)]
    public required int[] Indices { get; set; }
}