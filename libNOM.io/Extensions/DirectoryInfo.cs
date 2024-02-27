namespace libNOM.io.Extensions;


internal static class DirectoryInfoExtensions
{
    /// <summary>
    /// Combines this directory with the specified Guid to a new file.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="guid"></param>
    /// <returns></returns>
    internal static FileInfo GetBlobFileInfo(this DirectoryInfo self, Guid guid) => new(Path.Combine(self.FullName, guid.ToPath()));
}
