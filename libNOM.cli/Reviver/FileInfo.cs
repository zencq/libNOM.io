namespace libNOM.cli.Reviver;


[ArgReviverType]
public class FileInfoReviver
{
    [ArgReviver]
    public static FileInfo Revive(string _, string value)
    {
        return new FileInfo(value);
    }
}
