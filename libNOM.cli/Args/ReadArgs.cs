namespace libNOM.cli.Args;


public class ReadArgs
{
    [ArgExistingFile, ArgRequired, ArgDescription("The full path to the file to read."), ArgPosition(1)]
    public required FileInfo Input { get; set; }

    [ArgDefaultValue(false), ArgDescription("Decide whether to deobfuscate or not."), ArgPosition(2)]
    public required bool JsonDeobfuscated { get; set; }
}
