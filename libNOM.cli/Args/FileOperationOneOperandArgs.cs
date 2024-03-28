namespace libNOM.cli.Args;


public class FileOperationOneOperandArgs
{
    [ArgRequired, ArgDescription("List of container indices used as source."), ArgPosition(1)]
    public required string Input { get; set; }

    [ArgDescription("List of container indices to work with."), ArgPosition(2)]
    public int[]? Indices { get; set; }
}