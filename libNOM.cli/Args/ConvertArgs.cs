using libNOM.cli.Enums;

namespace libNOM.cli.Args;


public class ConvertArgs
{
    [ArgExistingFile, ArgRequired, ArgDescription("The full path to the file to convert."), ArgPosition(1)]
    public required FileInfo Input { get; set; }

    [ArgRequired, ArgDescription("The target format to convert into."), ArgPosition(3)]
    public required FormatEnum Format { get; set; }

    // New: Added OutputToStdout argument for printing to stdout instead of file.
    [ArgDefaultValue(false), ArgDescription("If the format is Steam, Output to stdout instead of a file. If set, '-O' is ignored."), ArgPosition(4)]
    public bool StdoutOutput { get; set; } = false;

    // Modified: Added note that Output will be ignored if OutputToStdout is set.
    [ArgExistingDirectory, ArgDescription("The full path where the result will be saved. Ignored if StdoutOutput is set."), ArgPosition(5)]
    public DirectoryInfo? Output { get; set; }

    [ArgDefaultValue(true), ArgDescription("If the format is JSON, decide whether to indent or not."), ArgPosition(6)]
    public required bool JsonIndented { get; set; }

    [ArgDefaultValue(true), ArgDescription("If the format is JSON, decide whether to deobfuscate or not."), ArgPosition(7)]
    public required bool JsonDeobfuscated { get; set; }

}
