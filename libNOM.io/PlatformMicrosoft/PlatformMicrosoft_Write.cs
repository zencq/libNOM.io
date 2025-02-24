using CommunityToolkit.HighPerformance;

namespace libNOM.io;


// This partial class contains writing related code.
public partial class PlatformMicrosoft : Platform
{
    #region Container

    protected override void WritePlatformSpecific(Container container, DateTimeOffset writeTime)
    {
        // Exit early if it is not necessary to write anything.
        if (!Settings.WriteAlways && container.IsSynced && !Settings.SetLastWriteTime)
            return;

        // Timestamp must be set before creating meta.
        if (Settings.SetLastWriteTime)
        {
            _lastWriteTime = writeTime; // global timestamp has full accuracy
            container.LastWriteTime = _lastWriteTime.NullifyTicks(4);
        }

        // Writing all Microsoft Store files at once in the same way as the game itself does.
        if (Settings.WriteAlways || !container.IsSynced)
        {
            container.Exists = true;
            container.IsSynced = true;

            var data = PrepareData(container);
            var meta = PrepareMeta(container, data);

            // Cache original file information.
            var copy = Common.DeepCopy(container.Extra);

            // Create blob container with new file information.
            var blob = PrepareBlobContainer(container);

            // Write the previously created files and delete the old ones.
            WriteMeta(container, meta, copy);
            WriteData(container, data, copy);
            WriteBlobContainer(container, blob, copy);
        }

        // Must be set after files have been created.
        if (Settings.SetLastWriteTime)
        {
            container.DataFile?.SetFileTime(container.LastWriteTime);
            container.MetaFile?.SetFileTime(container.LastWriteTime);
        }

        // Finally write the containers.index file.
        WriteContainersIndex();
    }

    #endregion

    #region Data

    protected override ReadOnlySpan<byte> CompressData(Container container, ReadOnlySpan<byte> data)
    {
        if (!container.IsSave || !container.IsVersion452OmegaWithMicrosoftV2) // if not Omega 4.52, also not Worlds Part I 5.00
        {
            _ = LZ4.Encode(data, out var target);
            return target;
        }

        // Since Worlds Part I 5.00, the standard save streaming is used.
        if (container.IsVersion500WorldsPartI)
            return base.CompressData(container, data);

        // Special format (similar to the standard streaming) used between Omega 4.52 and Worlds Part I 5.00.
        var position = 0;
        ReadOnlySpan<byte> result = HGSAVEV2_HEADER;

        while (position < data.Length)
        {
            var maxLength = data.Length - position;

            // The tailing \0 needs to compressed separately and must not be part of the actual JSON chunks.
            var source = data.Slice(position, Math.Min(HGSAVEV2_CHUNK_LENGTH_MAX, maxLength == 1 ? 1 : maxLength - 1));
            _ = LZ4.Encode(source, out var target);
            position += source.Length;

            var chunkHeader = new ReadOnlySpan<uint>(
            [
                (uint)(source.Length),
                (uint)(target.Length),
            ]);

            result = result.Concat(chunkHeader.Cast<uint, byte>()).Concat(target);
        }

        return result;
    }

    private void WriteData(Container container, ReadOnlySpan<byte> data, ContainerExtra original)
    {
        WriteData(container, data);
        original.MicrosoftBlobDataFile?.Delete();
    }

    #endregion

    #region Meta

    protected override Span<uint> CreateMeta(Container container, ReadOnlySpan<byte> data)
    {
        var buffer = new byte[GetMetaBufferLength(container)];

        using var writer = new BinaryWriter(new MemoryStream(buffer));

        if (container.IsAccount)
        {
            // Always 1.
            writer.Write(1); // 4

            // GAME MODE, SEASON, and TOTAL PLAY TIME not used.
            writer.Seek(0x10, SeekOrigin.Begin); // 16

            writer.Write(container.Extra.SizeDecompressed); // 4
        }
        else
        {
            writer.Write(container.BaseVersion); // 4
            writer.Write((ushort)(container.GameMode)); // 2
            writer.Write((ushort)(container.Season)); // 2
            writer.Write(container.TotalPlayTime); // 8

            // COMPRESSED SIZE or DECOMPRESSED SIZE depending on game version.
            writer.Write(container.IsVersion452OmegaWithMicrosoftV2 && !container.IsVersion550WorldsPartII ? container.Extra.SizeDisk : container.Extra.SizeDecompressed); // 4

            // Append buffered bytes that follow META_LENGTH_KNOWN_VANILLA.
            writer.Write(container.Extra.Bytes ?? []); // Extra.Bytes is 4 or 260 or 276 or 340

            OverwriteWaypointMeta(writer, container);
            OverwriteWorldsMeta(writer, container);
        }

        return buffer.AsSpan().Cast<byte, uint>();
    }

    private void WriteMeta(Container container, ReadOnlySpan<byte> meta, ContainerExtra original)
    {
        WriteMeta(container, meta);
        original.MicrosoftBlobMetaFile?.Delete();
    }

    #endregion

    // //

    #region Helper (containers.index)

    /// <summary>
    /// Updates the data and meta file information for the new writing.
    /// </summary>
    /// <param name="container"></param>
    /// <returns></returns>
    private static byte[] PrepareBlobContainer(Container container)
    {
        // Initializes a new Guid to use as file names.
        var dataGuid = Guid.NewGuid();
        var metaGuid = Guid.NewGuid();

        // Update container and its extra.
        container.Extra = container.Extra with
        {
            MicrosoftBlobContainerExtension = (byte)(container.Extra.MicrosoftBlobContainerExtension == byte.MaxValue ? 1 : container.Extra.MicrosoftBlobContainerExtension!.Value + 1),
            MicrosoftSyncState = container.Extra.MicrosoftSyncState == MicrosoftBlobSyncStateEnum.Synced ? MicrosoftBlobSyncStateEnum.Modified : container.Extra.MicrosoftSyncState,
            MicrosoftBlobDataFile = container.DataFile = container.Extra.MicrosoftBlobDirectory!.GetBlobFileInfo(dataGuid),
            MicrosoftBlobMetaFile = container.MetaFile = container.Extra.MicrosoftBlobDirectory!.GetBlobFileInfo(metaGuid),
        };

        // Create new blob container file content.
        var buffer = new byte[BLOBCONTAINER_TOTAL_LENGTH];

        using var writer = new BinaryWriter(new MemoryStream(buffer));
        writer.Write(BLOBCONTAINER_HEADER);
        writer.Write(BLOBCONTAINER_COUNT);

        writer.Write("data".GetUnicodeBytes());
        writer.Seek(BLOBCONTAINER_IDENTIFIER_LENGTH - 8, SeekOrigin.Current); // 8 = sizeof(data as UTF-16)
        writer.Write(container.Extra.MicrosoftBlobDataSyncGuid?.ToByteArray() ?? new byte[16]);
        writer.Write(dataGuid.ToByteArray());

        writer.Write("meta".GetUnicodeBytes());
        writer.Seek(BLOBCONTAINER_IDENTIFIER_LENGTH - 8, SeekOrigin.Current); // 8 = sizeof(meta as UTF-16)
        writer.Write(container.Extra.MicrosoftBlobMetaSyncGuid?.ToByteArray() ?? new byte[16]);
        writer.Write(metaGuid.ToByteArray());

        return buffer;
    }

    /// <summary>
    /// Writes the blob container file content to disk.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="blob"></param>
    private static void WriteBlobContainer(Container container, byte[] blob, ContainerExtra original)
    {
        container.Extra.MicrosoftBlobContainerFile?.WriteAllBytes(blob);
        original.MicrosoftBlobContainerFile?.Delete();
    }

    /// <summary>
    /// Creates and writes the containers.index file content to disk.
    /// </summary>
    private void WriteContainersIndex()
    {
        var hasSettings = _settingsContainer is not null;

        var collection = SaveContainerCollection.Where(i => i.Extra.MicrosoftBlobDirectoryGuid is not null);
        var count = (long)(collection.Count() + HasAccountData.ToByte() + hasSettings.ToByte());

        // Longest name (e.g. Slot10Manual) has a total length of 0x8C (140) and any leftover for short ones will be cut off.
        var buffer = new byte[CONTAINERSINDEX_OFFSET_BLOBCONTAINER_LIST + (count * 0x8C)];

        using (var writer = new BinaryWriter(new MemoryStream(buffer)))
        {
            writer.Write(CONTAINERSINDEX_HEADER);
            writer.Write(count);
            AppendDynamicText(writer, _processIdentifier, 1);
            writer.Write(_lastWriteTime.ToUniversalTime().ToFileTime());
            writer.Write((int)(MicrosoftIndexSyncStateEnum.Modified));
            AppendDynamicText(writer, _accountGuid, 1);
            writer.Write(CONTAINERSINDEX_FOOTER);

            if (HasAccountData)
                AppendMicrosoftMeta(writer, AccountContainer!);

            if (hasSettings)
                AppendMicrosoftMeta(writer, _settingsContainer!, "Settings");

            foreach (var container in collection)
                AppendMicrosoftMeta(writer, container);

            buffer = buffer.AsSpan()[..(int)(writer.BaseStream.Position)].ToArray();
        }

        // Write and refresh the containers.index file.
        _containersindex.WriteAllBytes(buffer);
        _containersindex.Refresh();
    }

    private static void AppendMicrosoftMeta(BinaryWriter writer, Container container) => AppendMicrosoftMeta(writer, container.Extra, container.Identifier);

    private static void AppendMicrosoftMeta(BinaryWriter writer, ContainerExtra extra, string identifier)
    {
        // Make sure to get the latest data.
        extra.MicrosoftBlobDataFile?.Refresh();
        extra.MicrosoftBlobMetaFile?.Refresh();

        if (extra.MicrosoftHasSecondIdentifier!.Value)
        {
            AppendDynamicText(writer, identifier, count: 2);
        }
        else
        {
            AppendDynamicText(writer, identifier);
            writer.Write((int)(0)); // length (0) of second identifier is still necessary
        }

        AppendDynamicText(writer, extra.MicrosoftSyncTime!);
        writer.Write(extra.MicrosoftBlobContainerExtension!.Value);
        writer.Write((int)(extra.MicrosoftSyncState!.Value));
        writer.Write(extra.MicrosoftBlobDirectoryGuid!.Value.ToByteArray());
        writer.Write(extra.LastWriteTime!.Value.ToUniversalTime().ToFileTime());
        writer.Write((long)(0));
        writer.Write((extra.MicrosoftBlobDataFile?.Exists == true ? extra.MicrosoftBlobDataFile!.Length : 0) + (extra.MicrosoftBlobMetaFile?.Exists == true ? extra.MicrosoftBlobMetaFile!.Length : 0));
    }

    /// <summary>
    /// Appends the length of the specified string and the string itself as unicode to the writer.
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="identifier"></param>
    /// <param name="count">How many times it should be added.</param>
    private static void AppendDynamicText(BinaryWriter writer, string identifier, int count = 1)
    {
        for (var i = 0; i < count; i++)
        {
            writer.Write(identifier.Length);
            writer.Write(identifier.GetUnicodeBytes());
        }
    }

    #endregion
}
