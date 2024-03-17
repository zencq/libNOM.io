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
        if (container.IsAccount || !data.StartsWith(SAVE_V2_HEADER)) // single chunk compression for Account and before Omega 4.52
        {
            _ = LZ4.Decode(data, out var target, (int)(container.Extra.SizeDecompressed));
            return target;
        }

        // New format is similar to the save streaming introduced with Frontiers.
        var offset = SAVE_V2_HEADER.Length;
        ReadOnlySpan<byte> result = [];

        while (offset < data.Length)
        {
            var chunkHeader = data.Slice(offset, SAVE_V2_HEADER_PARTIAL_LENGTH).Cast<byte, uint>();
            var sizeCompressed = (int)(chunkHeader[1]);

            offset += SAVE_V2_HEADER_PARTIAL_LENGTH;
            _ = LZ4.Decode(data.Slice(offset, sizeCompressed), out var target, (int)(chunkHeader[0]));
            offset += sizeCompressed;

            result = result.Concat(target);
        }

        return result;
    }

    #endregion
}
