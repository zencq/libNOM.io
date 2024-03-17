using CommunityToolkit.HighPerformance;

namespace libNOM.io;


public partial class PlatformPlaystation : Platform
{
    protected override void WritePlatformSpecific(Container container, DateTimeOffset writeTime)
    {
        if (_usesSaveStreaming)
        {
            base.WritePlatformSpecific(container, writeTime);
        }
        else
        {
            // Write memory.dat file if something needs to be updated.
            if (Settings.WriteAlways || !container.IsSynced || Settings.SetLastWriteTime)
            {
                if (Settings.WriteAlways || !container.IsSynced)
                {
                    container.Exists = true;
                    container.IsSynced = true;

                    _ = PrepareData(container); // stored in container.Extra.Bytes and written in WriteMemoryDat()
                }

                if (Settings.SetLastWriteTime)
                    _lastWriteTime = container.LastWriteTime = writeTime;

                WriteMemoryDat();
            }
        }
    }

    #region Data

    protected override ReadOnlySpan<byte> CompressData(Container container, ReadOnlySpan<byte> data)
    {
        ReadOnlySpan<byte> result;

        if (_usesSaveStreaming)
        {
            if (container.IsAccount) // no compression for account data
                return data;

            result = base.CompressData(container, data);
        }
        else
            _ = LZ4.Encode(data, out result);

        container.Extra = container.Extra with
        {
            Size = _usesSaveWizard ? (uint)(data.Length) : (uint)(result.Length),
            SizeDecompressed = (uint)(data.Length),
            SizeDisk = (uint)(result.Length),
        };

        // SaveWizard will do the compression itself but we need the updated extra data.
        if (_usesSaveWizard)
            return data;

        return result;
    }

    protected override void WriteData(Container container, ReadOnlySpan<byte> data)
    {
        // memory.dat will be written in its own method and therefore we do not need to write anything here.
        if (_usesSaveStreaming)
            // Append data to already written meta.
            if (_usesSaveWizard && !container.IsAccount)
            {
                using var stream = new FileStream(container.DataFile!.FullName, FileMode.Append);
#if NETSTANDARD2_0
                stream.Write(data.ToArray(), 0, data.Length);
#else
                stream.Write(data);
#endif
            }
            // Homebrew and Account always handled as usual.
            else
                base.WriteData(container, data);
    }

    #endregion

    #region Meta

    protected override Span<uint> CreateMeta(Container container, ReadOnlySpan<byte> data)
    {
        var buffer = _usesSaveStreaming ? GetStreamingMeta(container) : GetLegacyMeta(container);
        return buffer.AsSpan().Cast<byte, uint>();
    }

    private byte[] GetStreamingMeta(Container container)
    {
        if (container.IsAccount)
            return CreateAccountStreamingMeta(container);

        if (_usesSaveWizard) // no meta for homebrew if _usesSaveStreaming
            return CreateWizardStreamingMeta(container);

        return [];
    }

    private byte[] CreateAccountStreamingMeta(Container container)
    {
        // Use Switch values of META_LENGTH_TOTAL in fallback.
        var buffer = container.Extra.Bytes ?? new byte[container.MetaFormat == MetaFormatEnum.Waypoint ? 0x164 : 0x64];

        // Overwrite only SizeDecompressed.
        using var writer = new BinaryWriter(new MemoryStream(buffer));
        writer.Write(META_HEADER); // 4
        writer.Write(Constants.SAVE_FORMAT_3); // 4
        writer.Write(container.Extra.SizeDecompressed); // 4

        return buffer;
    }

    private byte[] CreateWizardStreamingMeta(Container container)
    {
        var buffer = new byte[META_LENGTH_TOTAL_WAYPOINT];

        using var writer = new BinaryWriter(new MemoryStream(buffer));

        writer.Write(SAVEWIZARD_HEADER_BINARY); // 8
        writer.Write(SAVEWIZARD_VERSION_2); // 4
        writer.Write(MEMORYDAT_OFFSET_META); // 4
        writer.Write(1); // 4
        writer.Write(container.Extra.SizeDisk); // 4

        writer.Seek(0x28, SeekOrigin.Current); // skip empty

        // Here the same structure as the old memory.dat format starts but with many empty values.
        writer.Seek(0x4, SeekOrigin.Current); // skip META HEADER
        writer.Write(Constants.SAVE_FORMAT_3); // 4
        writer.Seek(0x14, SeekOrigin.Current); // skip COMPRESSED SIZE, CHUNK OFFSET, CHUNK SIZE, META INDEX, TIMESTAMP
        writer.Write(container.Extra.SizeDecompressed); // 4
        writer.Seek(0x4, SeekOrigin.Current); // skip SAVEWIZARD OFFSET
        writer.Write(1); // 4

        return buffer;
    }

    private byte[] GetLegacyMeta(Container container)
    {
        var buffer = new byte[container.MetaSize];

        if (container.Exists)
        {
            var legacyOffset = container.IsAccount ? MEMORYDAT_OFFSET_DATA_ACCOUNTDATA : (uint)(MEMORYDAT_OFFSET_DATA_CONTAINER + (container.CollectionIndex * MEMORYDAT_LENGTH_CONTAINER));
            var legacyLength = container.IsAccount ? MEMORYDAT_LENGTH_ACCOUNTDATA : MEMORYDAT_LENGTH_CONTAINER;

            using var writer = new BinaryWriter(new MemoryStream(buffer));

            writer.Write(META_HEADER); // 4
            writer.Write(Constants.SAVE_FORMAT_2); // 4
            writer.Write(container.Extra.SizeDisk); // 4
            writer.Write(legacyOffset); // 4
            writer.Write(legacyLength); // 4
            writer.Write(container.MetaIndex); // 4
            writer.Write((uint)(container.LastWriteTime!.Value.ToUniversalTime().ToUnixTimeSeconds())); // 4
            writer.Write(container.Extra.SizeDecompressed); // 4

            if (_usesSaveWizard)
                AppendLegacySaveWizardMeta(writer, container);
        }
        else
        {
            using var writer = new BinaryWriter(new MemoryStream(buffer));

            writer.Write(META_HEADER); // 4
            writer.Write(Constants.SAVE_FORMAT_2); // 4
            writer.Seek(0xC, SeekOrigin.Current); // skip empty
            writer.Write(uint.MaxValue); // 4
        }

        return buffer;
    }

    private void AppendLegacySaveWizardMeta(BinaryWriter writer, Container container)
    {
        var offset = MEMORYDAT_OFFSET_DATA + SAVEWIZARD_HEADER.Length;
        if (container.MetaIndex > 0)
        {
            var precedingContainer = SaveContainerCollection.Where(i => i.Exists && i.MetaIndex < container.MetaIndex);

            offset += (int)(AccountContainer.Extra.SizeDecompressed);
            offset += (int)(precedingContainer.Sum(i => SAVEWIZARD_HEADER.Length + i.Extra.SizeDecompressed));
            offset += SAVEWIZARD_HEADER.Length;
        }
        writer.Write(offset); // 4
        writer.Write(1); // 4
    }

    #endregion

    #region memory.dat

    /// <summary>
    /// Writes the memory.dat file for the previous format.
    /// </summary>
    private void WriteMemoryDat()
    {
        var buffer = new byte[_usesSaveWizard ? MEMORYDAT_LENGTH_TOTAL_SAVEWIZARD : MEMORYDAT_LENGTH_TOTAL];

        using var writer = new BinaryWriter(new MemoryStream(buffer));

        if (_usesSaveWizard)
            AppendWizardPreamble(writer);

        // AccountData
        AppendContainerMeta(writer, AccountContainer);

        writer.Seek(META_LENGTH_TOTAL_VANILLA, SeekOrigin.Current);

        // Container
        foreach (var container in SaveContainerCollection)
            AppendContainerMeta(writer, container);

        writer.Seek(MEMORYDAT_OFFSET_DATA, SeekOrigin.Begin);

        if (_usesSaveWizard)
        {
            // AccountData
            AppendWizardContainer(writer, AccountContainer);

            // Container
            foreach (var container in SaveContainerCollection.Where(i => i.Exists))
                AppendWizardContainer(writer, container);

            buffer = buffer.AsSpan()[..(int)(writer.BaseStream.Position)].ToArray();
        }
        else
        {
            //TODO has AccountContainer proper PlaystationOffset?
            // AccountData
            AppendHomebrewContainer(writer, AccountContainer);

            // Container
            foreach (var container in SaveContainerCollection.Where(i => i.Exists))
                AppendHomebrewContainer(writer, container);
        }

        // Write and refresh the memory.dat file.
        _memorydat!.WriteAllBytes(buffer);
        _memorydat!.Refresh();
    }

    private void AppendWizardPreamble(BinaryWriter writer)
    {
        writer.Write(SAVEWIZARD_HEADER_BINARY);
        writer.Write(Constants.SAVE_FORMAT_2);
        writer.Write(MEMORYDAT_OFFSET_META);
        writer.Write(SaveContainerCollection.Where(i => i.Exists).Count() + 1); // + 1 for AccountData that ia always present in memory.dat
        writer.Write(MEMORYDAT_LENGTH_TOTAL);

        writer.Seek(MEMORYDAT_OFFSET_META, SeekOrigin.Begin);
    }

    private void AppendContainerMeta(BinaryWriter writer, Container container)
    {
        var meta = CreateMeta(container, container.Extra.Bytes);
#if NETSTANDARD2_0
        writer.Write(meta.AsBytes().ToArray());
#else
        writer.Write(meta.AsBytes());
#endif
    }

    private static void AppendHomebrewContainer(BinaryWriter writer, Container container)
    {
        writer.Seek(container.Extra.PlaystationOffset!.Value, SeekOrigin.Begin);
        writer.Write(container.Extra.Bytes!);
    }

    private static void AppendWizardContainer(BinaryWriter writer, Container container)
    {
        writer.Write(SAVEWIZARD_HEADER_BINARY);
        writer.Write(container.Extra.Bytes!);
    }

    #endregion
}
