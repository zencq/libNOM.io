namespace libNOM.cli.Args;


public class BackupArgs
{
    [ArgRequired, ArgDescription("File to backup or the directory containing the file."), ArgPosition(1)]
    public required string Input { get; set; }

    [ArgRange(0, 29), ArgDescription("If Input* is a directory specify what to backup. These indices are expected to be: 0 for Slot1Auto, 1 for Slot1Manual, 2 for Slot2Auto, etc)."), ArgShortcut("N"), ArgPosition(2)]
    public int[]? Indices { get; set; }
}