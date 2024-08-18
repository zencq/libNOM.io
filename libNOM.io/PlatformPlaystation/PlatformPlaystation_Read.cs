using libNOM.map;

using Newtonsoft.Json.Linq;

namespace libNOM.io;


// This partial class contains reading and processing related code.
public partial class PlatformPlaystation : Platform
{
    // //

    #region Container

    protected override ReadOnlySpan<byte> LoadContainer(Container container)
    {
        // With save streaming base can be used, otherwise meta needs to be loaded earlier.
        if (_usesSaveStreaming)
            return base.LoadContainer(container);

        // Any incompatibility will be set again while loading.
        container.ClearIncompatibility();

        // Load meta data outside the if as it sets whether the container exists.
        LoadMeta(container);

        if (container.Exists)
        {
            var data = LoadData(container);
            if (data.IsEmpty())
                container.IncompatibilityTag = Constants.INCOMPATIBILITY_001;
            else
                return data;
        }

        container.IncompatibilityTag ??= Constants.INCOMPATIBILITY_006;
        return [];
    }

    #endregion

    #region Meta

    protected override Span<byte> ReadMeta(Container container)
    {
        if (_usesSaveStreaming)
        {
            if (_usesSaveWizard && container.MetaFile?.Exists == true)
            {
                // Read entire file as it is in Switch format.
                if (container.IsAccount)
                    return base.ReadMeta(container);

                using var reader = new BinaryReader(File.Open(container.MetaFile!.FullName, FileMode.Open, FileAccess.Read, FileShare.Read));
                return reader.ReadBytes(META_LENGTH_TOTAL_WAYPOINT);
            }
            //else
            //    // no meta data for homebrew save streaming
        }
        else if (_memorydat?.Exists == true)
        {
            using var reader = new BinaryReader(File.Open(_memorydat!.FullName, FileMode.Open, FileAccess.Read, FileShare.Read));
            reader.BaseStream.Seek(MEMORYDAT_OFFSET_META + (container.MetaIndex * META_LENGTH_TOTAL_VANILLA), SeekOrigin.Begin);
            return reader.ReadBytes(META_LENGTH_TOTAL_VANILLA);
        }
        return [];
    }

    #endregion

    #region Data

    protected override ReadOnlySpan<byte> LoadData(Container container)
    {
        // 1. Read
        return LoadData(container, container.IsAccount || container.Extra.Bytes?.AsSpan().IsEmpty() != false ? ReadData(container) : container.Extra.Bytes);
    }

    protected override ReadOnlySpan<byte> ReadData(Container container)
    {
        if (container.DataFile?.Exists != true)
            return [];

        if (_usesSaveStreaming && (container.IsAccount || !_usesSaveWizard))
            return base.ReadData(container);

        // memory.dat and _usesSaveStreaming with _usesSaveWizard files contain multiple information and need to be cut out.
        using var reader = new BinaryReader(File.Open(container.DataFile!.FullName, FileMode.Open, FileAccess.Read, FileShare.Read));

        reader.BaseStream.Seek(container.Extra.PlaystationOffset!.Value, SeekOrigin.Begin);
        var data = reader.ReadBytes((int)(container.Extra.Bytes!.Length));

        // Store raw bytes as the block size is dynamic and moves if SaveWizard is used. Therefore the entire file needs to be rebuild.
        if (!_usesSaveStreaming)
            container.Extra = container.Extra with { Bytes = data };

        return data;
    }

    protected override ReadOnlySpan<byte> DecompressData(Container container, ReadOnlySpan<byte> data)
    {
        // SaveWizard already did the decompression.
        if (_usesSaveWizard)
            return data;

        if (_usesSaveStreaming)
            return base.DecompressData(container, data);

        _ = LZ4.Decode(data, out var target, (int)(container.Extra.SizeDecompressed));
        return target;
    }

    protected override JObject? Deserialize(Container container, ReadOnlySpan<byte> binary)
    {
        var jsonObject = base.Deserialize(container, binary);
        if (jsonObject is null) // incompatibility properties are set in base
            return null;

        // Deobfuscate anyway if _useSaveWizard to realign mapping by SaveWizard.
        if (_usesSaveWizard)
            container.UnknownKeys = Mapping.Deobfuscate(jsonObject, container.IsAccount);

        return jsonObject;
    }

    #endregion
}
