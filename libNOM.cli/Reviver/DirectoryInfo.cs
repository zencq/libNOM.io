namespace libNOM.cli.Reviver;


[ArgReviverType]
public class DirectoryInfoReviver
{
    [ArgReviver]
    public static DirectoryInfo Revive(string _, string value)
    {
        return new DirectoryInfo(value);
    }
}
