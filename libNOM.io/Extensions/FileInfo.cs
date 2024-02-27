namespace libNOM.io.Extensions;


internal static class FileInfoExtensions
{
    /// <summary>
    /// Reads the contents of this file into a byte array.
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    internal static byte[] ReadAllBytes(this FileInfo self)
    {
        if (self.Exists)
            return File.ReadAllBytes(self.FullName);

        return [];
    }
}
