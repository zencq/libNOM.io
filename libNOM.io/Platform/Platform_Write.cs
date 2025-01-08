using CommunityToolkit.HighPerformance;

namespace libNOM.io;


// This partial class contains writing related code.
public abstract partial class Platform : IPlatform, IEquatable<Platform>
{
    #region Container

    public void Write(IContainer container) => Write(container, DateTimeOffset.Now.LocalDateTime);

    public void Write(IContainer container, DateTimeOffset writeTime)
    {
        if (!CanUpdate || !container.IsLoaded)
            return;

        var nonIContainer = container.ToContainer();

        DisableWatcher();

        WritePlatformSpecific(nonIContainer, writeTime);

        EnableWatcher();

        // Always refresh in case something above was executed.
        nonIContainer.RefreshFileInfo();
    }

    protected virtual void WritePlatformSpecific(Container container, DateTimeOffset writeTime)
    {
        // In case LastWriteTime is written inside meta set it before writing.
        if (Settings.SetLastWriteTime)
            container.LastWriteTime = writeTime;

        if (Settings.WriteAlways || !container.IsSynced)
        {
            PrepareWrite(container);

            container.Exists = true;
            container.IsSynced = true;
        }

        // To ensure the timestamp will be the same the next time, the file times are always set to the currently saved one.
        container.DataFile?.SetFileTime(container.LastWriteTime);
        container.MetaFile?.SetFileTime(container.LastWriteTime);
    }

    internal void PrepareWrite(Container container)
    {
        var data = PrepareData(container);
        var meta = PrepareMeta(container, data);

        WriteMeta(container, meta);
        WriteData(container, data);
    }

    #endregion

    #region Data

    /// <summary>
    /// Prepares the ready to write to disk binary data file content.
    /// </summary>
    /// <param name="container"></param>
    /// <returns></returns>
    protected ReadOnlySpan<byte> PrepareData(Container container)
    {
        // 1. Create
        var plain = CreateData(container);
        // 2. Compress
        // 3. Encrypt
        var encrypted = EncryptData(container, CompressData(container, plain));
        // 4. Update Container Information
        UpdateContainerWithDataInformation(container, encrypted, plain);

        return encrypted;
    }

    /// <summary>
    /// Creates binary data file content from the JSON object.
    /// </summary>
    /// <param name="container"></param>
    /// <returns></returns>
    protected virtual ReadOnlySpan<byte> CreateData(Container container)
    {
        var binary = container.GetJsonObject().GetString(false, true, useAccount: container.IsAccount).GetBytesWithTerminator().AsReadOnlySpan();

        foreach (var (Raw, Escaped) in Constants.BINARY_MAPPING)
            binary = Common.ConvertHashedIds(binary, Escaped, Raw);

        return binary;
    }

    /// <summary>
    /// Compresses the created data file content.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    protected virtual ReadOnlySpan<byte> CompressData(Container container, ReadOnlySpan<byte> data)
    {
        if (!container.IsSave || !container.IsVersion360Frontiers)
            return data;

        var position = 0;
        ReadOnlySpan<byte> result = [];

        while (position < data.Length)
        {
            var source = data.Slice(position, Math.Min(Constants.SAVE_STREAMING_CHUNK_LENGTH_MAX, data.Length - position));
            _ = LZ4.Encode(source, out var target);
            position += source.Length;

            var chunkHeader = new ReadOnlySpan<uint>(
            [
                (uint)(target.Length),
                (uint)(source.Length),
                0,
            ]);

            result = result.Concat(Constants.SAVE_STREAMING_HEADER).Concat(chunkHeader.Cast<uint, byte>()).Concat(target);
        }

        return result;
    }

    /// <summary>
    /// Encrypts the created and compressed data file content.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    protected virtual ReadOnlySpan<byte> EncryptData(Container container, ReadOnlySpan<byte> data)
    {
        return data;
    }

    /// <summary>
    /// Writes the final data file content to disk.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="data"></param>
    protected virtual void WriteData(Container container, ReadOnlySpan<byte> data)
    {
        container.DataFile?.WriteAllBytes(data);
    }

    #endregion

    #region Meta

    /// <summary>
    /// Prepares the ready to write to disk binary meta file content.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    protected ReadOnlySpan<byte> PrepareMeta(Container container, ReadOnlySpan<byte> data)
    {
        // 1. Create
        var plain = CreateMeta(container, data);
        // 2. Compress
        // 3. Encrypt
        var encrypted = EncryptMeta(container, data, CompressMeta(container, data, plain.AsBytes()));
        // 4. Update Container Information
        UpdateContainerWithMetaInformation(container, plain.AsBytes(), plain);

        return encrypted;
    }

    protected int GetMetaBufferLength(Container container, bool force = false)
    {
        var capacity = force ? 0 : (int)(container.Extra.MetaLength);
        if (capacity == 0)
            capacity = container.GameVersion switch
            {
                >= GameVersionEnum.WorldsPartI => META_LENGTH_TOTAL_WORLDS,
                >= GameVersionEnum.Waypoint => META_LENGTH_TOTAL_WAYPOINT,
                _ => META_LENGTH_TOTAL_VANILLA,
            };
        return capacity;
    }

    /// <summary>
    /// Creates binary meta file content with information from the <see cref="Container"/> and the JSON object.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    protected abstract Span<uint> CreateMeta(Container container, ReadOnlySpan<byte> data);

    /// <summary>
    /// Appends metadata that were added or overwrite those that were changed with Waypoint 4.00.
    /// Contrary to the leading data, this is the same for all platforms.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="writer"></param>
    protected void OverwriteWaypointMeta(BinaryWriter writer, Container container)
    {
        if (container.IsVersion400Waypoint)
        {
            writer.Seek(META_LENGTH_KNOWN_VANILLA, SeekOrigin.Begin);
            writer.Write(container.SaveName.GetBytesWithTerminator()); // 128

            writer.Seek(META_LENGTH_KNOWN_NAME, SeekOrigin.Begin); // as a variable number of bytes is written, we seek from SeekOrigin.Begin again
            writer.Write(container.SaveSummary.GetBytesWithTerminator()); // 128

            writer.Seek(META_LENGTH_KNOWN_SUMMARY, SeekOrigin.Begin);
            writer.Write((byte)(container.Difficulty)); // 1
        }
    }

    /// <summary>
    /// Appends metadata that were added or overwrite those that were changed with Worlds Part I 5.00.
    /// Appended data is the same for all platforms.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="writer"></param>
    protected virtual void OverwriteWorldsMeta(BinaryWriter writer, Container container)
    {
        if (container.IsVersion500WorldsPartI)
        {
            writer.Seek(META_LENGTH_KNOWN_SUMMARY, SeekOrigin.Begin);
            writer.Write((uint)(container.Difficulty)); // 4

            // Skip next 8 bytes with SLOT IDENTIFIER.
            writer.Seek(0x8, SeekOrigin.Current);
            writer.Write((uint)(container.LastWriteTime!.Value.ToUniversalTime().ToUnixTimeSeconds())); // 4
            writer.Write(Constants.META_FORMAT_3); // 4
        }
    }

    /// <summary>
    /// Compresses the created meta file content.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="data"></param>
    /// <param name="meta"></param>
    /// <returns></returns>
    protected virtual Span<byte> CompressMeta(Container container, ReadOnlySpan<byte> data, Span<byte> meta)
    {
        return meta;
    }

    /// <summary>
    /// Encrypts the created and compressed meta file content.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="data"></param>
    /// <param name="meta"></param>
    /// <returns></returns>
    protected virtual ReadOnlySpan<byte> EncryptMeta(Container container, ReadOnlySpan<byte> data, Span<byte> meta)
    {
        return meta;
    }

    /// <summary>
    /// Writes the final meta file content to disk.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="meta"></param>
    protected virtual void WriteMeta(Container container, ReadOnlySpan<byte> meta)
    {
        container.MetaFile?.WriteAllBytes(meta);
    }

    #endregion
}
