using System.IO.Compression;

namespace libNOM.io.Extensions;


internal static class ZipArchiveExtensions
{
    /// <summary>
    /// Reads the specified entry without extracting it.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="entryName"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    internal static bool ReadEntry(this ZipArchive self, string entryName, out byte[] result)
    {
        if (self.GetEntry(entryName) is ZipArchiveEntry entry)
        {
            using var memory = new MemoryStream();
            using var stream = entry.Open();

            stream.CopyTo(memory);
            result = memory.ToArray();
            return true;
        }

        result = [];
        return false;
    }
}
