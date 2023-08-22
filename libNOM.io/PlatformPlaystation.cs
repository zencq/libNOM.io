using CommunityToolkit.Diagnostics;
using libNOM.map;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace libNOM.io;


public partial class PlatformPlaystation : Platform
{
    #region Constant

    internal override int COUNT_SLOTS => _usesSaveStreaming ? base.COUNT_SLOTS : 5;

    #region Platform Specific

    private const uint META_HEADER = 0xCA55E77E;
    private int META_OFFSET => _usesSaveWizard ? 0x40 : 0x0; // 64 : 0
    private int META_SIZE => _usesSaveWizard ? 0x30 : 0x20; // 48 : 32
    protected override int META_LENGTH_TOTAL_VANILLA => 0x68; // 104 byte
    protected override int META_LENGTH_TOTAL_WAYPOINT => 0x168; // 360 byte

    private const int MEMORYDAT_ANCHORFILE_INDEX = 1;
    private int MEMORYDAT_META_INDEX_OFFSET => _usesSaveWizard ? 8 : 3;
    private int MEMORYDAT_META_INDEX_SIZE => _usesSaveWizard ? 7 : 2;
    private const uint MEMORYDAT_OFFSET_CONTAINER = 0xE0000U;
    private uint MEMORYDAT_OFFSET_DATA => _usesSaveWizard ? 0x1040U : 0x20000U;
    private const uint MEMORYDAT_SIZE_ACCOUNTDATA = 0x40000U;
    private const uint MEMORYDAT_SIZE_CONTAINER = 0x300000U;
    private const uint MEMORYDAT_SIZE_TOTAL = 0x2000000U; // 32 MB

    private const string SAVEWIZARD_HEADER = "NOMANSKY";
    private static readonly byte[] SAVEWIZARD_HEADER_BINARY = SAVEWIZARD_HEADER.GetUTF8Bytes();
    private const int SAVEWIZARD_MEMORYDAT_OFFSET_COUNT = 0x10; // 16

    #endregion

    #region Directory Data

    public static readonly string[] ANCHOR_FILE_GLOB = new[] { "savedata*.hg", "memory.dat" };
#if NETSTANDARD2_0_OR_GREATER || NET6_0
    public static readonly Regex[] ANCHOR_FILE_REGEX = new Regex[] { AnchorFileRegex0!, AnchorFileRegex1! };
#else
    public static readonly Regex[] ANCHOR_FILE_REGEX = new Regex[] { AnchorFileRegex0(), AnchorFileRegex1() };
#endif

    #endregion

    #region Generated Regex

#if NETSTANDARD2_0_OR_GREATER || NET6_0
    private static readonly Regex AnchorFileRegex0 = new("savedata\\d{2}\\.hg", RegexOptions.Compiled);
    private static readonly Regex AnchorFileRegex1 = new("memory\\.dat", RegexOptions.Compiled);
#else
    [GeneratedRegex("savedata\\d{2}\\.hg", RegexOptions.Compiled)]
    private static partial Regex AnchorFileRegex0();

    [GeneratedRegex("memory\\.dat", RegexOptions.Compiled)]
    private static partial Regex AnchorFileRegex1();
#endif

    #endregion

    #endregion

    #region Field

    private FileInfo? _memorydat;
    private bool _usesSaveStreaming;
    private bool _usesSaveWizard;

    #endregion

    #region Property

    #region Flags

    public override bool CanCreate { get; } = true;

    public override bool CanRead { get; } = true;

    public override bool CanUpdate { get; } = true;

    public override bool CanDelete { get; } = true;

    public override bool HasAccountData => _usesSaveStreaming && base.HasAccountData; // { get; }

    public override bool HasModding { get; } = false;

    public override bool IsPersonalComputerPlatform { get; } = false;

    public override bool RestartToApply { get; } = false;

    #endregion

    #region Platform Indicator

    protected override string[] PlatformAnchorFileGlob { get; } = ANCHOR_FILE_GLOB;

    protected override Regex[] PlatformAnchorFileRegex { get; } = ANCHOR_FILE_REGEX;

    protected override string? PlatformArchitecture { get; } = "PS4|Final";

    public override PlatformEnum PlatformEnum { get; } = PlatformEnum.Playstation;

    protected override string? PlatformProcess { get; } = null; // console platform has no PC process

    protected override string PlatformToken { get; } = "PS";

    #endregion

    #endregion

    #region Getter

    #region Container

    protected override IEnumerable<Container> GetCacheEvictionContainers(string name)
    {
        if (_usesSaveStreaming)
            return base.GetCacheEvictionContainers(name);

        if (!name.Equals("memory.dat", StringComparison.OrdinalIgnoreCase))
            return Array.Empty<Container>();

        RefreshContainerCollection();

        return GetExistingContainers();
    }

    #endregion

    #endregion

    // //

    #region Constructor

    public PlatformPlaystation(string path) : base(path) { }

    public PlatformPlaystation(string path, PlatformSettings platformSettings) : base(path, platformSettings) { }

    public PlatformPlaystation(DirectoryInfo directory) : base(directory) { }

    public PlatformPlaystation(DirectoryInfo directory, PlatformSettings platformSettings) : base(directory, platformSettings) { }

    protected override void InitializeComponent(DirectoryInfo? directory, PlatformSettings? platformSettings)
    {
        // Proceed to base method even if no directory.
        if (directory is not null)
        {
            var anchorFileIndex = GetAnchorFileIndex(directory);

            _usesSaveStreaming = anchorFileIndex != MEMORYDAT_ANCHORFILE_INDEX;

            _memorydat = _usesSaveStreaming ? null : new FileInfo(Path.Combine(directory.FullName, "memory.dat"));
            var file = _usesSaveStreaming ? directory.GetFiles().FirstOrDefault(f => PlatformAnchorFileRegex[anchorFileIndex].IsMatch(f.Name) && !f.Name.Contains("00")) : _memorydat;
            if (file is null)
            {
                _usesSaveWizard = false;
            }
            else
            {
                using var reader = new BinaryReader(File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                _usesSaveWizard = SAVEWIZARD_HEADER_BINARY.SequenceEqual(reader.ReadBytes(SAVEWIZARD_HEADER_BINARY.Length));
            }
        }

        base.InitializeComponent(directory, platformSettings);
    }

    #endregion

    // //

    // // Read / Write

    #region Generate

    private protected override Container CreateContainer(int metaIndex, PlatformExtra? extra)
    {
        if (_usesSaveStreaming)
        {
            var fileInfo = new FileInfo(Path.Combine(Location!.FullName, $"savedata{metaIndex:D2}.hg"));
            return new Container(metaIndex)
            {
                DataFile = fileInfo,
                MetaFile = fileInfo,
            };
        }

        return new Container(metaIndex)
        {
            DataFile = _memorydat,
            MetaFile = _memorydat,
        };
    }

    protected override JObject? DeserializeContainer(Container container, byte[] binary)
    {
        JObject? jsonObject;
        try
        {
            jsonObject = binary.GetJson();
        }
        catch (Exception ex) when (ex is JsonReaderException or JsonSerializationException)
        {
            container.IncompatibilityException = ex;
            container.IncompatibilityTag = Globals.Constants.INCOMPATIBILITY_002;
            return null;
        }
        if (jsonObject is null)
        {
            container.IncompatibilityTag = Globals.Constants.INCOMPATIBILITY_003;
            return null;
        }

        // Deobfuscate anyway if _useSaveWizard to realign mapping by SaveWizard.
        if (Settings.UseMapping || _usesSaveWizard)
        {
            container.UnknownKeys = Mapping.Deobfuscate(jsonObject);
        }

        // Do deliver a consistent experience, obfuscate if _usesSaveWizard and mapping is disabled.
        if (_usesSaveWizard && !Settings.UseMapping)
        {
            Mapping.Obfuscate(jsonObject);
        }

        return jsonObject;
    }

    #endregion

    #region Load

    protected override byte[] LoadContainer(Container container)
    {
        if (_usesSaveStreaming)
            return base.LoadContainer(container);

        // Any incompatibility will be set again while loading.
        container.ClearIncompatibility();

        // Load meta data outside the if as it sets whether the container exists.
        var meta = LoadMeta(container);

        //container.Exists &= meta?.Any() == true;
        if (container.Exists)
        {
            var data = LoadData(container, meta);
            if (data.IsNullOrEmpty())
            {
                container.IncompatibilityTag = Globals.Constants.INCOMPATIBILITY_001;
            }
            else
            {
                return data;
            }
        }

        container.IncompatibilityTag ??= Globals.Constants.INCOMPATIBILITY_006;
        return Array.Empty<byte>();
    }

    protected override byte[] ReadMeta(Container container)
    {
        if (_usesSaveStreaming)
        {
            if (_usesSaveWizard && container.MetaFile?.Exists == true)
            {
                using var reader = new BinaryReader(File.Open(container.MetaFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                reader.BaseStream.Seek(0x10, SeekOrigin.Begin);
                return reader.ReadBytes(0x60);
            }
        }
        else if (_memorydat?.Exists == true)
        {
            using var reader = new BinaryReader(File.Open(_memorydat.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            reader.BaseStream.Seek(META_OFFSET + (container.MetaIndex * META_SIZE), SeekOrigin.Begin);
            return reader.ReadBytes(META_SIZE);
        }

        return Array.Empty<byte>();
    }

    protected override void UpdateContainerWithMetaInformation(Container container, byte[] raw, uint[] converted)
    {
        if (_usesSaveStreaming)
        {
            if (_usesSaveWizard)
            {
                if (!converted.Any())
                    return;

                container.Extra = new PlatformExtra
                {
                    PlaystationOffset = 0x70,
                    Size = converted[19],
                    SizeDisk = converted[1],
                    SizeDecompressed = converted[19],
                };
            }
            //else
            //    // no meta data for homebrew save streaming
        }
        else
        {
            if (!converted.Any())
            {
                container.Exists = false;
                return;
            }

            container.Extra = new PlatformExtra
            {
                PlaystationOffset = (int)(converted[MEMORYDAT_META_INDEX_OFFSET]),
                Size = converted[MEMORYDAT_META_INDEX_SIZE], // either compressed or decompressed size depending on SaveWizard usage
                SizeDisk = converted[2],
                SizeDecompressed = converted[7],
            };

            container.Exists = converted[2] != 0;
            container.LastWriteTime = DateTimeOffset.FromUnixTimeSeconds(converted[6]).ToLocalTime();
        }
    }

    protected override byte[] LoadData(Container container, uint[] meta)
    {
        return LoadData(container, meta, container.Extra.Bytes ?? ReadData(container));
    }

    protected override byte[] ReadData(Container container)
    {
        if (!_usesSaveStreaming || (_usesSaveWizard && container.IsSave))
        {
            using var reader = new BinaryReader(File.Open(container.DataFile!.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            reader.BaseStream.Seek(container.Extra.PlaystationOffset!.Value, SeekOrigin.Begin);

            var data = reader.ReadBytes((int)(container.Extra.Size));

            // Store raw bytes as the block size is dynamic and moves if SaveWizard is used. Therefore the entire file needs to be rebuild.
            if (!_usesSaveStreaming)
                container.Extra.Bytes = data;

            return data;
        }

        return base.ReadData(container);
    }

    protected override byte[] DecompressData(Container container, uint[] meta, byte[] data)
    {
        // SaveWizard already did the decompression.
        if (_usesSaveWizard)
            return data;

        if (_usesSaveStreaming)
        {
            // Same as for Steam.
            return base.DecompressData(container, meta, data);
        }
        else
        {
            _ = Globals.LZ4.Decode(data, out byte[] target, (int)(container.Extra.SizeDecompressed));
            return target;
        }
    }

    protected override void UpdateContainerWithDataInformation(Container container, byte[] raw, byte[] converted)
    {
        if (_usesSaveStreaming)
        {
            if (container.IsSave)
            {
                container.Extra = container.Extra with
                {
                    SizeDecompressed = (uint)(converted.Length),
                    SizeDisk = (uint)(raw.Length),
                };
            }
            // No compression for account data.
            else
            {
                container.Extra = container.Extra with
                {
                    SizeDecompressed = (uint)(raw.Length),
                    SizeDisk = (uint)(raw.Length),
                };
            }
        }
    }

    #endregion

    #region Write

    public override void Write(Container container, DateTimeOffset writeTime)
    {
        if (_usesSaveStreaming)
        {
            base.Write(container, writeTime);
        }
        else
        {
            if (!CanUpdate || !container.IsLoaded)
                return;

            DisableWatcher();

            // Writing all memory.dat file if something needs to be updated.
            if (Settings.WriteAlways || !container.IsSynced || Settings.SetLastWriteTime)
            {
                if (Settings.WriteAlways || !container.IsSynced)
                {
                    container.Exists = true;
                    container.IsSynced = true;

                    var data = CreateData(container); // updates container.Extra inside
                    container.Extra = container.Extra with
                    {
                        Bytes = data,
                    };

                    //container.Extra.Bytes = CreateData(container);
                }

                if (Settings.SetLastWriteTime)
                {
                    container.LastWriteTime = writeTime;
                }

                WriteMemoryDat();
            }

            EnableWatcher();

            // Always refresh in case something above was executed.
            container.RefreshFileInfo();
            container.WriteCallback.Invoke();
        }
    }
    protected override byte[] CreateData(Container container)
    {
        var plain = container.GetJsonObject()!.GetBytes(Settings.UseMapping);
        var encrypted = EncryptData(container, CompressData(container, plain));

        container.Extra = container.Extra with
        {
            Size = _usesSaveWizard ? (uint)(plain.Length) : (uint)(encrypted.Length), // override because of this
            SizeDecompressed = (uint)(plain.Length),
            SizeDisk = (uint)(encrypted.Length),
        };

        return encrypted;
    }

    protected override byte[] CompressData(Container container, byte[] data)
    {
        if (_usesSaveStreaming)
        {
            if (!container.IsSave)
                return data;

            var result = base.CompressData(container, data);

            //container.Extra.SizeDecompressed = (uint)(data.Length);
            //container.Extra.SizeDisk = (uint)(result.Length);

            // SaveWizard will do the compression itself.
            if (_usesSaveWizard)
                return data;

            return result;
        }
        else
        {
            //container.Extra.SizeDecompressed = (uint)(data.Length);
            //container.Extra.SizeDisk = (uint)(Globals.LZ4.Encode(data, out byte[] target));

            _ = Globals.LZ4.Encode(data, out byte[] target);

            // SaveWizard will do the compression itself.
            if (_usesSaveWizard)
                return data;

            return target;
        }
    }

    protected override void WriteData(Container container, byte[] data)
    {
        if (_usesSaveWizard)
        {
            // Append data to already written meta.
            using var stream = new FileStream(container.DataFile!.FullName, FileMode.Append);
            stream.Write(data, 0, data.Length);
        }
        else
        {
            base.WriteData(container, data);
        }
    }

    protected override byte[] CreateMeta(Container container, byte[] data)
    {
        if (_usesSaveStreaming)
        {
            if (_usesSaveWizard && container.IsSave)
            {
                var buffer = new byte[0x70];

                using var writer = new BinaryWriter(new MemoryStream(buffer));

                writer.Write(SAVEWIZARD_HEADER_BINARY);
                writer.Write(2);
                writer.Write(META_OFFSET);
                writer.Write(1);
                writer.Write(container.Extra.SizeDisk);

                writer.Seek(META_OFFSET + 0x4, SeekOrigin.Begin);

                writer.Write(Globals.Constants.SAVE_FORMAT_3);

                writer.Seek(0x5C, SeekOrigin.Begin);

                writer.Write(container.Extra.SizeDecompressed);

                writer.Seek(4, SeekOrigin.Current);

                writer.Write(1);

                return EncryptMeta(container, data, CompressMeta(container, data, buffer));
            }
            return Array.Empty<byte>();
        }
        else
        {
            //  0. CASSETTE             ( 4)
            //  1. META FORMAT          ( 4)
            //  2. COMPRESSED SIZE      ( 4)
            //  3. CHUNK OFFSET         ( 4)
            //  4. CHUNK SIZE           ( 4)
            //  5. META INDEX           ( 4)
            //  6. TIMESTAMP            ( 4)
            //  7. DECOMPRESSED SIZE    ( 4)
            //                          (32) // Vanilla

            //  8. SAVEWIZARD OFFSET    ( 4)
            //  9. 1                    ( 4)
            // 10. EMPTY                ( 8)
            //                          (48) // SaveWizard

            var buffer = new byte[META_SIZE];

            if (container.Exists)
            {
                var legacyOffset = !container.IsSave ? 0x20000 : (uint)(MEMORYDAT_OFFSET_CONTAINER + (container.CollectionIndex * MEMORYDAT_SIZE_CONTAINER));
                var legacyLength = !container.IsSave ? MEMORYDAT_SIZE_ACCOUNTDATA : MEMORYDAT_SIZE_CONTAINER;
                var unixSeconds = (uint)(container.LastWriteTime!.Value.ToUniversalTime().ToUnixTimeSeconds());

                using var writer = new BinaryWriter(new MemoryStream(buffer));

                writer.Write(META_HEADER);
                writer.Write(Globals.Constants.SAVE_FORMAT_2);
                writer.Write(container.Extra.SizeDisk);
                writer.Write(legacyOffset);
                writer.Write(legacyLength);
                writer.Write(container.MetaIndex);
                writer.Write(unixSeconds);
                writer.Write(container.Extra.SizeDecompressed);

                if (_usesSaveWizard)
                {
                    var offset = MEMORYDAT_OFFSET_DATA + SAVEWIZARD_HEADER.Length;
                    if (container.MetaIndex > 0)
                    {
                        var precedingContainer = SaveContainerCollection.Where(i => i.Exists && i.CollectionIndex < container.CollectionIndex);

                        offset += AccountContainer!.Extra.SizeDecompressed;
                        offset += precedingContainer.Sum(i => SAVEWIZARD_HEADER.Length + i.Extra.SizeDecompressed);
                        offset += SAVEWIZARD_HEADER.Length;
                    }
                    writer.Write((uint)(offset));
                    writer.Write(1);
                }
            }
            else if (!_usesSaveWizard)
            {
                using var writer = new BinaryWriter(new MemoryStream(buffer));

                writer.Write(META_HEADER);
                writer.Write(Globals.Constants.SAVE_FORMAT_2);

                writer.BaseStream.Seek(0xC, SeekOrigin.Current);
                writer.Write(uint.MaxValue);
            }

            return EncryptMeta(container, data, CompressMeta(container, data, buffer));
        }
    }

    /// <summary>
    /// Writes the memory.dat file for the previous format.
    /// </summary>
    private void WriteMemoryDat()
    {
        var buffer = new byte[MEMORYDAT_OFFSET_DATA];
        IEnumerable<byte>? result = Array.Empty<byte>();

        if (_usesSaveWizard)
        {
            using var reader = new BinaryReader(File.Open(_memorydat!.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            reader.Read(buffer, 0, META_OFFSET);
        }

        using (var writer = new BinaryWriter(new MemoryStream(buffer)))
        {
            if (_usesSaveWizard)
            {
                writer.BaseStream.Seek(SAVEWIZARD_MEMORYDAT_OFFSET_COUNT, SeekOrigin.Begin);
                writer.Write(SaveContainerCollection.Where(i => i.Exists).Count() + 1); // 1 for AccountData

                writer.BaseStream.Seek(META_OFFSET, SeekOrigin.Begin);
            }

            // AccountData
            {
                var meta = CreateMeta(AccountContainer!, AccountContainer!.Extra!.Bytes!);
                writer.Write(meta);
                writer.BaseStream.Seek(META_SIZE, SeekOrigin.Current); // skip index 1 as not used
            }

            // Container
            foreach (var container in SaveContainerCollection)
            {
                var meta = CreateMeta(container, container.Extra.Bytes!);
                writer.Write(meta);
            }
        }

        result = result.Concat(buffer);

        // Add cached data to content.
        if (_usesSaveWizard)
        {
            // AccountData
            result = result.Concat(SAVEWIZARD_HEADER_BINARY).Concat(AccountContainer!.Extra.Bytes!);

            // Container
            foreach (var container in SaveContainerCollection.Where(i => i.Exists))
                result = result.Concat(SAVEWIZARD_HEADER_BINARY).Concat(container.Extra.Bytes!);
        }
        else
        {
            // AccountData
            buffer = new byte[MEMORYDAT_OFFSET_CONTAINER - MEMORYDAT_OFFSET_DATA];
            using (var writer = new BinaryWriter(new MemoryStream(buffer)))
            {
                writer.Write(AccountContainer!.Extra.Bytes!);
            }
            result = result.Concat(buffer);

            // Container
            foreach (var container in SaveContainerCollection)
            {
                buffer = new byte[MEMORYDAT_SIZE_CONTAINER];
                if (container.Exists)
                {
                    using var writer = new BinaryWriter(new MemoryStream(buffer));
                    writer.Write(container.Extra.Bytes!);
                }
                result = result.Concat(buffer);
            }

            // Fill with empty bytes to total file size.
            result = result.Concat(new byte[MEMORYDAT_SIZE_TOTAL - result.Count()]);
        }

        File.WriteAllBytes(_memorydat!.FullName, result.ToArray());
        _memorydat!.Refresh();
    }

    #endregion

    // // File Operation

    #region Copy

    protected override void Copy(IEnumerable<(Container Source, Container Destination)> operationData, bool write)
    {
        if (_usesSaveStreaming)
        {
            base.Copy(operationData, write);
        }
        else
        {
            foreach (var (Source, Destination) in operationData)
            {
                if (!Source.Exists)
                {
                    Delete(Destination, false);
                }
                else if (Destination.Exists || !Destination.Exists && CanCreate)
                {
                    if (!Source.IsLoaded)
                        BuildContainerFull(Source);

                    if (!Source.IsCompatible)
                        throw new InvalidOperationException($"Cannot copy as the source container is not compatible: {Source.IncompatibilityTag}");

                    // Due to this CanCreate can be true.
                    if (!Destination.Exists)
                    {
                        CopyPlatformExtra(Destination, Source);
                    }

                    // Faking relevant properties to force it to Write().
                    Destination.Exists = true;
                    Destination.IsSynced = false;

                    // Properties requied to properly build the container below.
                    Destination.BaseVersion = Source.BaseVersion;
                    Destination.GameVersionEnum = Source.GameVersionEnum;
                    Destination.SeasonEnum = Source.SeasonEnum;

                    Destination.SetJsonObject(Source.GetJsonObject());

                    BuildContainerFull(Destination);
                }
            }
            if (write)
            {
                WriteMemoryDat();
            }
        }

        UpdateUserIdentification();
    }

    protected override void CopyPlatformExtra(Container destination, Container source)
    {
        destination.Extra = new PlatformExtra
        {
            Bytes = source.Extra.Bytes,
            SizeDisk = source.Extra.SizeDisk,
            SizeDecompressed = source.Extra.SizeDecompressed,
        };
    }

    #endregion

    #region Delete

    protected override void Delete(IEnumerable<Container> containers, bool write)
    {
        Guard.IsTrue(CanDelete);

        if (_usesSaveStreaming)
        {
            base.Delete(containers, write);
        }
        else
        {
            DisableWatcher();

            foreach (var container in containers)
            {
                container.Reset();

                // TODO check
                // Set afterwards again to make sure it is set to false.
                container.Exists = false;
            }
            if (write)
            {
                WriteMemoryDat();
            }

            EnableWatcher();
        }
    }

    #endregion

    #region Move

    protected override void Move(IEnumerable<(Container Source, Container Destination)> containerOperationData, bool write)
    {
        if (_usesSaveStreaming)
        {
            base.Move(containerOperationData, write);
        }
        else
        {
            Copy(containerOperationData, false);
            Delete(containerOperationData.Select(i => i.Source), false);

            if (write)
            {
                WriteMemoryDat();
            }
        }
    }

    #endregion

    #region Transfer

    protected override void Transfer(ContainerTransferData sourceTransferData, int destinationSlot, bool write)
    {
        if (!sourceTransferData.UserIdentification.IsComplete() || !PlatformUserIdentification.IsComplete())
            throw new InvalidOperationException("Cannot transfer as at least one user identification is not complete.");

        if (_usesSaveStreaming)
        {
            base.Transfer(sourceTransferData, destinationSlot, write);
        }
        else
        {
            foreach (var (Source, Destination) in sourceTransferData.Containers.Zip(GetSlotContainers(destinationSlot), (Source, Destination) => (Source, Destination)))
            {
                if (!Source.Exists)
                {
                    Delete(Destination, false);
                }
                else if (Destination.Exists || !Destination.Exists && CanCreate)
                {
                    if (!Source.IsLoaded)
                        BuildContainerFull(Source);

                    if (!Source.IsCompatible)
                        throw new InvalidOperationException($"Cannot transfer as the source container is not compatible: {Source.IncompatibilityTag}");

                    // Due to this CanCreate can be true (combined with CreateData below).
                    if (!Destination.Exists)
                    {
                        CreatePlatformExtra(Destination, Source);
                    }

                    // Faking relevant properties to force it to Write().
                    Destination.Exists = true;
                    Destination.IsSynced = false;

                    // Properties requied to properly build the container below.
                    Destination.BaseVersion = Source.BaseVersion;
                    Destination.GameVersionEnum = Source.GameVersionEnum;
                    Destination.SeasonEnum = Source.SeasonEnum;

                    Destination.SetJsonObject(Source.GetJsonObject());
                    TransferOwnership(Destination, sourceTransferData);

                    // Update bytes in platform extra as it is what will be written later.
                    Destination.Extra!.Bytes = CreateData(Destination);

                    BuildContainerFull(Destination);
                }
                //else
                //    continue;
            }
            if (write)
            {
                WriteMemoryDat();
            }
        }
    }

    protected override void CreatePlatformExtra(Container destination, Container source)
    {
        destination.Extra = new();
    }

    #endregion

    // // FileSystemWatcher

    #region FileSystemWatcher

    /// <summary>
    /// Refreshes all containers in the collection with newly written data from the containers.index file.
    /// </summary>
    private void RefreshContainerCollection()
    {
        for (var containerIndex = 0; containerIndex < COUNT_SAVES_TOTAL; containerIndex++)
        {
            // Reset bytes to read the file again.
            SaveContainerCollection[containerIndex].Extra.Bytes = null;

            // Rebuild new container to set its properties.
            if (Settings.LoadingStrategy < LoadingStrategyEnum.Full)
            {
                BuildContainerHollow(SaveContainerCollection[containerIndex]);
            }
            else
            {
                BuildContainerFull(SaveContainerCollection[containerIndex]);
            }
        }
    }

    #endregion
}
