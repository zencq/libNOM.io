using libNOM.cli.Enums;

namespace libNOM.cli.Args;


public class AnalyzeArgs
{
    [ArgRequired, ArgDescription("The full path to the file to convert."), ArgPosition(2)]
    public required FileInfo Input { get; set; }
}