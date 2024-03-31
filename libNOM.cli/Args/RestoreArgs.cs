namespace libNOM.cli.Args;


public class RestoreArgs
{
    [ArgExistingFile, ArgRequired, ArgDescription("Backup file to restore."), ArgPosition(1)]
    public required FileInfo Backup { get; set; }

    [ArgExistingDirectory, ArgRequired, ArgDescription("Directory of the platform to restore to."), ArgPosition(1)]
    public required DirectoryInfo Platform { get; set; }

    [ArgRange(0, 29), ArgDescription("Index of the save you want restore. If not set it will be extracted from the file if it still has the libNOM.io name format."), ArgPosition(2)]
    public int? Index { get; set; }
}