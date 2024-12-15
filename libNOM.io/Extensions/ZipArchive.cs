using System.IO.Compression;

namespace libNOM.io.Extensions;


internal static class ZipArchiveExtensions
{
    #region Archive

    /// <summary>
    /// Archives a file by compressing it and adding it to the specified zip archive.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="sourceFile"></param>
    /// <param name="prefix">Additional information that the entry will be prefixed with.</param>
    /// <returns></returns>
    internal static ZipArchiveEntry CreateEntryFromFile(this ZipArchive self, FileInfo sourceFile, string prefix) => self.CreateEntryFromFile(sourceFile.FullName, $"{prefix}.{sourceFile.Name}");

    #endregion

    #region Entry

    /// <summary>
    /// Reads the content without extracting it.
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    internal static byte[] Read(this ZipArchiveEntry self)
    {
        using var memory = new MemoryStream();
        using var stream = self.Open();

        stream.CopyTo(memory);
        return memory.ToArray();
    }

    #endregion
}
