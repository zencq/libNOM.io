using libNOM.cli.Enums;

namespace libNOM.cli.Args;


public class ConvertArgs
{
    [ArgRequired, ArgDescription("The target format to convert into."), ArgPosition(1)]
    public required FormatEnum Format { get; set; }

    [ArgRequired, ArgDescription("The full path to the file to convert."), ArgPosition(2)]
    public required FileInfo Input { get; set; }

    [ArgDescription("The full path where the result will be saved."), ArgPosition(3)]
    public FileInfo? Output { get; set; }

    [ArgDescription("If format is JSON, decide whether to indent or not."), ArgPosition(3)]
    public bool JsonIndent { get; set; }

    [ArgDescription("If format is JSON, decide whether to deobfusacte or not."), ArgPosition(3)]
    public bool JsonDeobfusacte { get; set; }
}