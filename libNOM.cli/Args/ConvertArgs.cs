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

    [ArgDefaultValue(true), ArgDescription("If the format is JSON, decide whether it should be pretty printed."), ArgPosition(4)]
    public required bool JsonPrettyPrint { get; set; }

    [ArgDefaultValue(true), ArgDescription("If the format is JSON, decide whether the keys should be human readable."), ArgPosition(5)]
    public required bool JsonHumanReadable { get; set; }
}
