using libNOM.cli.Enums;

namespace libNOM.cli.Args;


public class ConvertArgs
{
    [ArgExistingFile, ArgRequired, ArgDescription("The full path to the file to convert."), ArgPosition(1)]
    public required FileInfo Input { get; set; }

    [ArgExistingDirectory, ArgDescription("The full path where the result will be saved."), ArgPosition(2)]
    public DirectoryInfo? Output { get; set; }

    [ArgRequired, ArgDescription("The target format to convert into."), ArgPosition(3)]
    public required FormatEnum Format { get; set; }

    [ArgDefaultValue(true), ArgDescription("If the format is JSON, decide whether to indent or not."), ArgPosition(4)]
    public required bool JsonIndented { get; set; }

    [ArgDefaultValue(true), ArgDescription("If the format is JSON, decide whether to deobfuscate or not."), ArgPosition(5)]
    public required bool JsonDeobfuscated { get; set; }
}
