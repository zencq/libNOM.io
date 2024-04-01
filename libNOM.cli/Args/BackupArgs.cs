namespace libNOM.cli.Args;


public class BackupArgs
{
    [ArgRequired, ArgDescription("File to backup or the directory containing the file."), ArgPosition(1)]
    public required string Input { get; set; }

    [ArgRange(0, 29), ArgDescription("If input is a directory specify what to backup. The indices of the saves are expected: 0 for Slot1Auto, 1 for Slot1Manual, 2 for Slot2Auto, etc)."), ArgPosition(2)]
    public int[]? Indices { get; set; }
}