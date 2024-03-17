using CommunityToolkit.HighPerformance;

using libNOM.io.Interfaces;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace libNOM.io;


// This partial class contains reading and processing related code.
public abstract partial class Platform : IPlatform, IEquatable<Platform>
{
    #region Container

    /// <summary>
    /// Loads the save data of a <see cref="Container"/> into a processable format using meta data.
    /// </summary>
    /// <param name="container"></param>
    /// <returns></returns>
    protected virtual ReadOnlySpan<byte> LoadContainer(Container container)
    {
        // Any incompatibility will be set again while loading.
        container.ClearIncompatibility();

        if (container.Exists)
        {
            // Loads all meta information into the extra property.
            LoadMeta(container);

            var data = LoadData(container);
            if (data.IsEmpty())
                container.IncompatibilityTag = Constants.INCOMPATIBILITY_001;
            else
                return data;
        }

        container.IncompatibilityTag ??= Constants.INCOMPATIBILITY_006;
        return [];
    }

    public void Reload(Container container)
    {
        if (container.IsLoaded)
            RebuildContainerFull(container);
        else
            RebuildContainerHollow(container);
    }

    /// <summary>
    /// Rebuilds a <see cref="Container"/> by loading from disk and processing it by deserializing the data.
    /// </summary>
    /// <param name="container"></param>
    protected void RebuildContainerFull(Container container) => BuildContainerFull(container);

    /// <summary>
    /// Rebuilds a <see cref="Container"/> by loading from disk and processing it by extracting from the string representation.
    /// </summary>
    /// <param name="container"></param>
    protected void RebuildContainerHollow(Container container)
    {
        var binary = LoadContainer(container);

        if (container.IsCompatible)
            UpdateContainerWithJsonInformation(container, binary.GetString(), true); // force
    }

    #endregion

    #region Meta

    /// <inheritdoc cref="LoadMeta(Container, Span{byte})"/>
    protected void LoadMeta(Container container)
    {
        // 1. Read
        LoadMeta(container, ReadMeta(container));
    }

    /// <summary>
    /// Loads the meta file into a processable format including reading, decrypting, and decompressing.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="read">Already read content of the meta file.</param>
    /// <returns></returns>
    protected void LoadMeta(Container container, Span<byte> read)
    {
        // 2. Decrypt
        // 3. Decompress
        var result = read.IsEmpty() ? [] : DecompressMeta(container, DecryptMeta(container, read));
        // 4. Update Container Information
        UpdateContainerWithMetaInformation(container, result.AsBytes(), result);
    }

    /// <summary>
    /// Reads the content of the meta file.
    /// </summary>
    /// <param name="container"></param>
    /// <returns></returns>
    protected virtual Span<byte> ReadMeta(Container container)
    {
        return container.MetaFile?.ReadAllBytes() ?? [];
    }

    /// <summary>
    /// Decrypts the read content of the meta file.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="meta"></param>
    /// <returns></returns>
    protected virtual Span<uint> DecryptMeta(Container container, Span<byte> meta)
    {
        return meta.Cast<byte, uint>();
    }

    /// <summary>
    /// Decompresses the read and decrypted content of the meta file.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="meta"></param>
    /// <returns></returns>
    protected virtual ReadOnlySpan<uint> DecompressMeta(Container container, ReadOnlySpan<uint> meta)
    {
        return meta;
    }

    #endregion

    #region Data

    /// <inheritdoc cref="LoadData(Container, ReadOnlySpan{byte})"/>
    protected virtual ReadOnlySpan<byte> LoadData(Container container)
    {
        // 1. Read
        return LoadData(container, ReadData(container));
    }

    /// <summary>
    /// Loads the data file into a processable format including reading, decrypting, and decompressing.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="read"></param>
    /// <returns></returns>
    protected ReadOnlySpan<byte> LoadData(Container container, ReadOnlySpan<byte> read)
    {
        if (read.IsEmpty())
            return read;

        // 2. Decrypt
        // 3. Decompress
        var result = DecompressData(container, DecryptData(container, read));
        // 4. Update Container Information
        UpdateContainerWithDataInformation(container, read, result);

        return result;
    }

    /// <summary>
    /// Reads the content of the data file.
    /// </summary>
    /// <param name="container"></param>
    /// <returns></returns>
    protected virtual ReadOnlySpan<byte> ReadData(Container container)
    {
        return container.DataFile?.ReadAllBytes() ?? [];
    }

    /// <summary>
    /// Decrypts the read content of the data file.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    protected virtual ReadOnlySpan<byte> DecryptData(Container container, ReadOnlySpan<byte> data)
    {
        return data;
    }

    /// <summary>
    /// Decompresses the read and decrypted content of the data file.
    /// </summary>
    /// <param name="container"></param
    /// <param name="data"></param>
    /// <returns></returns>
    protected virtual ReadOnlySpan<byte> DecompressData(Container container, ReadOnlySpan<byte> data)
    {
        if (container.IsAccount || data.Cast<uint>(0) != Constants.SAVE_STREAMING_HEADER) // no compression before Frontiers
            return data;

        var offset = 0;
        ReadOnlySpan<byte> result = [];

        while (offset < data.Length)
        {
            var chunkHeader = data.Slice(offset, Constants.SAVE_STREAMING_HEADER_TOTAL_LENGTH).Cast<byte, uint>();
            var sizeCompressed = (int)(chunkHeader[1]);

            offset += Constants.SAVE_STREAMING_HEADER_TOTAL_LENGTH;
            _ = LZ4.Decode(data.Slice(offset, sizeCompressed), out var target, (int)(chunkHeader[2]));
            offset += sizeCompressed;

            result = result.Concat(target);
        }

        return result;
    }

    /// <summary>
    /// Deserializes the read data of a <see cref="Container"/> into a JSON object.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="binary"></param>
    /// <returns></returns>
    protected virtual JObject? Deserialize(Container container, ReadOnlySpan<byte> binary)
    {
        JObject? jsonObject = null;
        try
        {
            jsonObject = binary.GetJson();
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException or JsonReaderException or JsonSerializationException)
        {
            container.IncompatibilityException = ex;
            container.IncompatibilityTag = Constants.INCOMPATIBILITY_002;
        }
        if (jsonObject is null)
        {
            container.IncompatibilityTag = Constants.INCOMPATIBILITY_003;
        }
        return jsonObject;
    }

    #endregion
}
