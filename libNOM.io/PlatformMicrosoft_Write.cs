using CommunityToolkit.HighPerformance;

namespace libNOM.io;


// This partial class contains writing related code.
public partial class PlatformMicrosoft : Platform
{
    protected override void WritePlatformSpecific(Container container, DateTimeOffset writeTime)
    {
        // Writing all Microsoft Store files at once in the same way as the game itself does.
        if (Settings.WriteAlways || !container.IsSynced || Settings.SetLastWriteTime)
        {
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

            if (Settings.SetLastWriteTime)
            {
                _lastWriteTime = writeTime; // global timestamp has full accuracy
                container.LastWriteTime = _lastWriteTime.NullifyTicks(4);

                container.DataFile?.SetFileTime(container.LastWriteTime);
                container.MetaFile?.SetFileTime(container.LastWriteTime);
            }

            // Finally write the containers.index file.
            WriteContainersIndex();
        }
    }

    #region Data

    protected override ReadOnlySpan<byte> CompressData(Container container, ReadOnlySpan<byte> data)
    {
        if (!container.IsSave || !container.IsVersion452OmegaWithV2)
        {
            _ = LZ4.Encode(data, out var target);
            return target;
        }

        // New format is similar to the save streaming introduced with Frontiers.
        var position = 0;
        ReadOnlySpan<byte> result = SAVE_V2_HEADER;

        while (position < data.Length)
        {
            var maxLength = data.Length - position;

            // The tailing \0 needs to compressed separately and must not be part of the actual JSON chunks.
            var source = data.Slice(position, Math.Min(SAVE_V2_CHUNK_MAX_LENGTH, maxLength == 1 ? 1 : maxLength - 1));
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
        var buffer = CreateMetaBuffer(container);

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
            writer.Write(container.TotalPlayTime); // 4

            // Skip EMPTY.
            writer.Seek(0x4, SeekOrigin.Current); // 4

            writer.Write(container.IsVersion452OmegaWithV2 ? container.Extra.SizeDisk : container.Extra.SizeDecompressed); // 4

            // Insert trailing bytes and the extended Waypoint data.
            AppendWaypointMeta(writer, container); // Extra.Bytes is 260 or 4
        }

        return buffer.AsSpan().Cast<byte, uint>();
    }

    private void WriteMeta(Container container, ReadOnlySpan<byte> meta, ContainerExtra original)
    {
        WriteMeta(container, meta);
        original.MicrosoftBlobMetaFile?.Delete();
    }

    #endregion

    #region containers.index

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
                AppendMicrosoftMeta(writer, AccountContainer!.Identifier, AccountContainer!.Extra);

            if (hasSettings)
                AppendMicrosoftMeta(writer, "Settings", _settingsContainer!);

            foreach (var container in collection)
                AppendMicrosoftMeta(writer, container.Identifier, container.Extra);

            buffer = buffer.AsSpan()[..(int)(writer.BaseStream.Position)].ToArray();
        }

        // Write and refresh the containers.index file.
        _containersindex.WriteAllBytes(buffer);
        _containersindex.Refresh();
    }

    private static void AppendMicrosoftMeta(BinaryWriter writer, string identifier, ContainerExtra extra)
    {
        // Make sure to get the latest data.
        extra.MicrosoftBlobDataFile?.Refresh();
        extra.MicrosoftBlobMetaFile?.Refresh();

        AppendDynamicText(writer, identifier, 2);
        AppendDynamicText(writer, extra.MicrosoftSyncTime!, 1);
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
    private static void AppendDynamicText(BinaryWriter writer, string identifier, int count)
    {
        for (var i = 0; i < count; i++)
        {
            writer.Write(identifier.Length);
            writer.Write(identifier.GetUnicodeBytes());
        }
    }

    #endregion
}
