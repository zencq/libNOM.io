using System.IO.Compression;

namespace libNOM.io.Extensions;


internal static class ZipArchiveExtensions
{
    /// <summary>
    /// Reads the binary of a zip archive entry.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="entryName"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    internal static bool ReadEntry(this ZipArchive self, string entryName, out byte[] result)
    {
        var entry = self.GetEntry(entryName);
        if (entry is null)
        {
            result = Array.Empty<byte>();
            return false;
        }

        using var stream = new MemoryStream();
        entry.Open().CopyTo(stream);
        result = stream.ToArray();
        return true;
    }
}
