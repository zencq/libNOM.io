using System.IO.Compression;

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

    /// <summary>
    /// Sets the CreationTime and LastWriteTime for this file to the specified timestamp.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="timestamp"></param>
    internal static void SetFileTime(this FileInfo self, DateTimeOffset? timestamp)
    {
        if (self.Exists && timestamp is not null)
        {
            File.SetCreationTime(self.FullName, timestamp!.Value.LocalDateTime);
            File.SetLastWriteTime(self.FullName, timestamp!.Value.LocalDateTime);
        }
    }

    /// <summary>
    /// Writes the specified bytes to this file.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="bytes"></param>
    internal static void WriteAllBytes(this FileInfo self, ReadOnlySpan<byte> bytes)
    {
        File.WriteAllBytes(self.FullName, bytes.ToArray());
    }
}
