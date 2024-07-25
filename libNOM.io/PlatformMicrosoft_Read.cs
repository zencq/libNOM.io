using CommunityToolkit.HighPerformance;

namespace libNOM.io;


// This partial class contains reading and processing related code.
public partial class PlatformMicrosoft : Platform
{
    #region Container

    protected override ReadOnlySpan<byte> LoadContainer(Container container)
    {
        var result = base.LoadContainer(container);

        // Use more precise Microsoft tags if container does not exist.
        // Result is already empty if so and tags set if none of the rules here apply.
        if (!container.Exists)
            if (container.Extra.MicrosoftSyncState == MicrosoftBlobSyncStateEnum.Deleted)
                container.IncompatibilityTag = Constants.INCOMPATIBILITY_004;
            else if (container.Extra.MicrosoftBlobDirectory?.Exists != true)
                container.IncompatibilityTag = Constants.INCOMPATIBILITY_005;

        return result;
    }

    #endregion

    #region Data

    protected override ReadOnlySpan<byte> DecompressData(Container container, ReadOnlySpan<byte> data)
    {
        // Single chunk compression for Account and before Omega 4.52.
        if (container.IsAccount || (!data.StartsWith(HGSAVEV2_HEADER) && !data.StartsWith(Constants.SAVE_STREAMING_HEADER)))
        {
            _ = LZ4.Decode(data, out var target, (int)(container.Extra.SizeDecompressed));
            return target;
        }

        // Since Worlds 5.00, the standard save streaming is used.
        if (data.StartsWith(Constants.SAVE_STREAMING_HEADER))
            return base.DecompressData(container, data);

        // Special format (similar to the standard streaming) used between Omega 4.52 and Worlds 5.00.
        var offset = HGSAVEV2_HEADER.Length;
        ReadOnlySpan<byte> result = [];

        while (offset < data.Length)
        {
            var chunkHeader = data.Slice(offset, HGSAVEV2_HEADER_LENGTH).Cast<byte, uint>();
            var sizeCompressed = (int)(chunkHeader[1]);

            offset += HGSAVEV2_HEADER_LENGTH;
            _ = LZ4.Decode(data.Slice(offset, sizeCompressed), out var target, (int)(chunkHeader[0]));
            offset += sizeCompressed;

            result = result.Concat(target);
        }

        return result;
    }

    #endregion
}
