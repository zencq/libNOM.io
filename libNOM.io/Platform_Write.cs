using CommunityToolkit.HighPerformance;

using libNOM.io.Interfaces;

namespace libNOM.io;


/// <summary>
/// Abstract base for all platforms which just hook into the methods they need.
/// </summary>
public abstract partial class Platform : IPlatform, IEquatable<Platform>
{
    #region Write

    public void Write(Container container) => Write(container, DateTimeOffset.Now.LocalDateTime);

    public virtual void Write(Container container, DateTimeOffset writeTime)
    {
        if (!CanUpdate || !container.IsLoaded)
            return;

        DisableWatcher();

        // In case LastWriteTime is written inside meta set it before writing.
        if (Settings.SetLastWriteTime)
            container.LastWriteTime = writeTime;

        if (Settings.WriteAlways || !container.IsSynced)
        {
            JustWrite(container);

            container.Exists = true;
            container.IsSynced = true;
        }

        // To ensure the timestamp will be the same the next time, the file times are always set to the currently saved one.
        container.DataFile?.SetFileTime(container.LastWriteTime);
        container.MetaFile?.SetFileTime(container.LastWriteTime);

        EnableWatcher();

        // Always refresh in case something above was executed.
        container.RefreshFileInfo();
        container.WriteCallback.Invoke();
    }

    internal void JustWrite(Container container)
    {
        var data = PrepareData(container);
        var meta = PrepareMeta(container, data);

        WriteMeta(container, meta);
        WriteData(container, data);
    }

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
        return container.GetJsonObject().GetBytes();
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
            var source = data.Slice(position, Math.Min(Constants.SAVE_STREAMING_CHUNK_MAX_LENGTH, data.Length - position));
            _ = LZ4.Encode(source, out var target);
            position += source.Length;

            var chunkHeader = new ReadOnlySpan<uint>(
            [
                Constants.SAVE_STREAMING_HEADER,
                (uint)(target.Length),
                (uint)(source.Length),
                0,
            ]);

            result = result.Concat(chunkHeader.Cast<uint, byte>()).Concat(target);
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
        UpdateContainerWithMetaInformation(container, encrypted, plain);

        return encrypted;
    }

    /// <summary>
    /// Creates binary meta file content with information from the <see cref="Container"/> and the JSON object.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    protected abstract Span<uint> CreateMeta(Container container, ReadOnlySpan<byte> data);

    /// <summary>
    /// Adds meta data that were added with Waypoint.
    /// Contrary to the leading data, this is the same for all platforms.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="writer"></param>
    protected void AddWaypointMeta(BinaryWriter writer, Container container)
    {
        // Always append cached bytes but overwrite afterwards if Waypoint.
        writer.Write(container.Extra.Bytes ?? []); // length depends on platform

        if (container.MetaFormat >= MetaFormatEnum.Waypoint)
        {
            writer.Seek(META_LENGTH_KNOWN, SeekOrigin.Begin);
            writer.Write(container.SaveName.GetBytesWithTerminator()); // 128

            writer.Seek(META_LENGTH_KNOWN + (Constants.SAVE_RENAMING_LENGTH_MANIFEST), SeekOrigin.Begin);
            writer.Write(container.SaveSummary.GetBytesWithTerminator()); // 128

            writer.Seek(META_LENGTH_KNOWN + (Constants.SAVE_RENAMING_LENGTH_MANIFEST * 2), SeekOrigin.Begin);
            writer.Write((byte)(container.Difficulty)); // 1
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
