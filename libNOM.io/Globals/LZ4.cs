using K4os.Compression.LZ4;

namespace libNOM.io.Globals;


internal static class LZ4
{
    /// <summary>
    /// Compresses data from one buffer into another.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="target"></param>
    /// <returns>Number of bytes written, or negative value if output buffer is too small.</returns>
    internal static int Encode(this byte[] source, out byte[] target)
    {
        target = new byte[LZ4Codec.MaximumOutputSize(source.Length)];
        var bytesWritten = LZ4Codec.Encode(source, 0, source.Length, target, 0, target.Length);

        target = target.Take(bytesWritten).ToArray();
        return bytesWritten;
    }

    /// <summary>
    /// Decompresses data from one buffer into another.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="target"></param>
    /// <param name="targetLength"></param>
    /// <returns>Number of bytes written, or negative value if output buffer is too small.</returns>
    internal static int Decode(this byte[] self, out byte[] target, int targetLength)
    {
        target = Array.Empty<byte>();
        var bytesWritten = -1;

        if (targetLength > 0)
        {
            target = new byte[targetLength];
            // TODO ReadOnlySpan
            bytesWritten = LZ4Codec.Decode(self, 0, self.Length, target, 0, target.Length);
        }

        // Fallback. https://github.com/MiloszKrajewski/K4os.Compression.LZ4#decompression
        if (bytesWritten < 0)
        {
            target = new byte[self.Length * 255];
            bytesWritten = LZ4Codec.Decode(self, 0, self.Length, target, 0, target.Length);
        }

        return bytesWritten;
    }
}
