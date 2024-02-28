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
#if !NETSTANDARD2_0
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0057: Use range operator", Justification = "The range operator is not supported in netstandard2.0 and Slice() has no performance penalties.")]
#endif
    internal static int Encode(ReadOnlySpan<byte> source, out ReadOnlySpan<byte> target)
    {
        var output = new Span<byte>(new byte[LZ4Codec.MaximumOutputSize(source.Length)]);
        var bytesWritten = LZ4Codec.Encode(source, output);

        target = output.Slice(0, bytesWritten);

        return bytesWritten;
    }

    /// <summary>
    /// Decompresses data from one buffer into another.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="target"></param>
    /// <param name="targetLength"></param>
    /// <returns>Number of bytes written, or negative value if output buffer is too small.</returns>

    internal static int Decode(ReadOnlySpan<byte> source, out Span<byte> target, int targetLength)
    {
        target = new Span<byte>(new byte[targetLength]);
        var bytesWritten = LZ4Codec.Decode(source, target);

        // Fallback. https://github.com/MiloszKrajewski/K4os.Compression.LZ4#decompression
        if (bytesWritten == -1)
        {
            target = new Span<byte>(new byte[source.Length * 255]);
            bytesWritten = LZ4Codec.Decode(source, target);
        }

        return bytesWritten;
    }
}
