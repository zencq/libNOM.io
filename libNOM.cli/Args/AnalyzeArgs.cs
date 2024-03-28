using libNOM.cli.Enums;

namespace libNOM.cli.Args;


public class AnalyzeArgs
{
    [ArgRequired, ArgDescription("The path to a file or directory."), ArgPosition(1)]
    public required string Input { get; set; }
}