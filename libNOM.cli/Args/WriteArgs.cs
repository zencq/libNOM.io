using libNOM.cli.Enums;

namespace libNOM.cli.Args;


public class WriteArgs
{
    [ArgExistingDirectory, ArgRequired, ArgDescription("The full path where the result will be saved."), ArgPosition(1)]
    public DirectoryInfo? Output { get; set; }

    [ArgRequired, ArgDescription("The target format to write into."), ArgPosition(2)]
    public required FormatEnum Format { get; set; }

    [ArgRange(0, 29), ArgRequired, ArgDescription("Index of the save you want to write. The index is expected to be: 0 for Slot1Auto, 1 for Slot1Manual, 2 for Slot2Auto, etc)."), ArgPosition(3)]
    public int? Index { get; set; }
}
