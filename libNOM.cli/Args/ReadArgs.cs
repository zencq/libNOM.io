namespace libNOM.cli.Args;


public class ReadArgs
{
    [ArgExistingFile, ArgRequired, ArgDescription("The full path to the file to read."), ArgPosition(1)]
    public required FileInfo Input { get; set; }

    [ArgDefaultValue(false), ArgDescription("Decide whether the keys should be human readable."), ArgPosition(5)]
    public required bool JsonHumanReadable { get; set; }
}
