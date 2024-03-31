namespace libNOM.cli.Args;


public class BackupArgs
{
    [ArgRequired, ArgDescription("File to backup or the directory containing the file."), ArgPosition(1)]
    public required string Input { get; set; }

    [ArgRange(0, 29), ArgDescription("If input is a directory specify what to backup."), ArgPosition(2)]
    public int[]? Indices { get; set; }
}