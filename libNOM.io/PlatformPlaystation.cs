using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;

using libNOM.map;

using Newtonsoft.Json.Linq;

namespace libNOM.io;


public partial class PlatformPlaystation : Platform
{
    #region Constant

    internal static readonly string[] ANCHOR_FILE_PATTERN = ["savedata??.hg", "memory.dat"];

    protected override int COUNT_SAVE_SLOTS => _usesSaveStreaming ? base.COUNT_SAVE_SLOTS : 5;

    protected const uint META_HEADER = 0xCA55E77E;
    internal override int META_LENGTH_TOTAL_VANILLA => _usesSaveWizard ? 0x30 : 0x20; // 48 : 32
    internal override int META_LENGTH_TOTAL_WAYPOINT => _usesSaveWizard ? 0x70 : 0x0; // 112 : 0 // actually _FRONTIERS would be more accurate as there was no changed in Waypoint for PlayStation but reusing it as it has to be implemented anyway and would have no use otherwise

    private const uint MEMORYDAT_LENGTH_ACCOUNTDATA = 0x40000U;
    private const uint MEMORYDAT_LENGTH_CONTAINER = 0x300000U;
    private const uint MEMORYDAT_LENGTH_TOTAL = 0x2000000U; // 32 MB
    private const uint MEMORYDAT_LENGTH_TOTAL_SAVEWIZARD = 0x3000000U; // 48 MB // ten uncompressed saves may exceed the default length
    private int MEMORYDAT_META_INDEX_OFFSET => _usesSaveWizard ? 8 : 3;
    private int MEMORYDAT_META_INDEX_LENGTH => _usesSaveWizard ? 7 : 2;
    private int MEMORYDAT_OFFSET_META => _usesSaveWizard ? 0x40 : 0x0; // 64 : 0
    private int MEMORYDAT_OFFSET_DATA => _usesSaveWizard ? 0x1040 : 0x20000; // 4160 : 131072
    private const uint MEMORYDAT_OFFSET_DATA_ACCOUNTDATA = 0x20000U;
    private const uint MEMORYDAT_OFFSET_DATA_CONTAINER = 0xE0000U;

#pragma warning disable IDE0051 // Remove unused private member
    internal const string SAVEWIZARD_HEADER = "NOMANSKY";
    internal static readonly byte[] SAVEWIZARD_HEADER_BINARY = SAVEWIZARD_HEADER.GetUTF8Bytes();
    private const int SAVEWIZARD_VERSION_1 = 1; // not used but for completeness
    private const int SAVEWIZARD_VERSION_2 = 2;
#pragma warning restore IDE0051

    #endregion

    #region Field

    private DateTimeOffset? _lastWriteTime; // will be set to track _memorydat timestamp
    private FileInfo? _memorydat; // will be set if _usesSaveStreaming is false
    private bool _usesSaveStreaming; // will be set to indicate whether save streaming is used
    private bool _usesSaveWizard; // will be set to indicate whether SaveWizard is used

    #endregion

    #region Property

    #region Flags

    // public //

    public override bool CanCreate { get; } = true;

    public override bool CanRead { get; } = true;

    public override bool CanUpdate { get; } = true;

    public override bool CanDelete { get; } = true;

    public override bool HasAccountData => _usesSaveStreaming && base.HasAccountData; // { get; }

    public override bool HasModding { get; } = false;

    public override bool RestartToApply { get; } = true;

    // protected //

    protected override bool IsConsolePlatform { get; } = true;

    #endregion

    #region Platform Indicator

    // public //

    public override PlatformEnum PlatformEnum { get; } = PlatformEnum.Playstation;

    // protected //

    protected override string[] PlatformAnchorFilePattern { get; } = ANCHOR_FILE_PATTERN;

    protected override string? PlatformArchitecture { get; } = "PS4|Final";

    protected override string? PlatformProcessPath { get; } = null;

    protected override string PlatformToken { get; } = "PS";

    #endregion

    #endregion

    // //

    #region Getter

    protected override IEnumerable<Container> GetCacheEvictionContainers(string name)
    {
        if (_usesSaveStreaming)
            return base.GetCacheEvictionContainers(name);

        if (!name.Equals("memory.dat", StringComparison.OrdinalIgnoreCase))
            return [];

        // Cache previous timestamp.
        var lastWriteTicks = _lastWriteTime!.NullifyTicks(4)!.Value.UtcTicks;

        // Refresh will also update _lastWriteTime.
        RefreshContainerCollection();

        // Get all written container that are newer than the previous timestamp.
        return SaveContainerCollection.Where(i => i.Exists && i.LastWriteTime?.UtcTicks >= lastWriteTicks);
    }

    #endregion

    // //

    #region Constructor

    /// <summary>
    /// Special case for <see cref="PlatformCollection.AnalyzeFile(string)"/> to be able to use selected methods with an empty initialization.
    /// </summary>
    /// <param name="usesSaveWizard"></param>
    internal PlatformPlaystation(bool usesSaveWizard) : base()
    {
        _usesSaveStreaming = true;
        _usesSaveWizard = usesSaveWizard;
    }

    public PlatformPlaystation() : base() { }

    public PlatformPlaystation(string path) : base(path) { }

    public PlatformPlaystation(string path, PlatformSettings platformSettings) : base(path, platformSettings) { }

    public PlatformPlaystation(DirectoryInfo directory) : base(directory) { }

    public PlatformPlaystation(DirectoryInfo directory, PlatformSettings platformSettings) : base(directory, platformSettings) { }

    protected override void InitializeComponent(DirectoryInfo? directory, PlatformSettings? platformSettings)
    {
        // Proceed to base method even if no directory.
        if (GetAnchorFileIndex(directory) is int anchorFileIndex and not -1) // implicit directory is not null
        {
            if (anchorFileIndex == 1) // memory.dat
            {
                _memorydat = new FileInfo(Path.Combine(directory!.FullName, "memory.dat"));
                _lastWriteTime = _memorydat.LastWriteTime;
            }
            else
            {
                _usesSaveStreaming = true;
            }

            // Get first file that is not account data if not _memorydat.
            var f = _memorydat ?? directory!.GetFiles(PlatformAnchorFilePattern[anchorFileIndex]).FirstOrDefault(i => !i.Name.Contains("00"));
            if (f is not null)
            {
                using var reader = new BinaryReader(File.Open(f.FullName, FileMode.Open, FileAccess.Read, FileShare.Read));
                _usesSaveWizard = reader.ReadBytes(SAVEWIZARD_HEADER_BINARY.Length).SequenceEqual(SAVEWIZARD_HEADER_BINARY);
            }
        }

        base.InitializeComponent(directory, platformSettings);
    }

    #endregion

    // // Read / Write

    #region Generate

    private protected override Container CreateContainer(int metaIndex, PlatformExtra? extra)
    {
        if (_usesSaveStreaming)
        {
            var data = new FileInfo(Path.Combine(Location.FullName, $"savedata{metaIndex:D2}.hg"));
            var meta = new FileInfo(Path.Combine(Location.FullName, $"manifest{metaIndex:D2}.hg"));

            // AccountData may have an additional meta/manifest file (for SaveWizard only).
            // Otherwise the meta data are at the beginning of the data file itself (for SaveWizard only).
            return new Container(metaIndex, this)
            {
                DataFile = data,
                MetaFile = metaIndex == 0 ? (meta.Exists ? meta : null) : (_usesSaveWizard ? data : null),
                /// Additional values will be set in <see cref="UpdateContainerWithMetaInformation"/> and <see cref="UpdateContainerWithDataInformation"/>.
                Extra = extra ?? new()
                {
                    LastWriteTime = data.LastWriteTime,
                },
            };
        }

        return new Container(metaIndex, this)
        {
            DataFile = _memorydat,
            MetaFile = _memorydat,
            /// Additional values will be set in <see cref="UpdateContainerWithMetaInformation"/> and <see cref="UpdateContainerWithDataInformation"/>.
            Extra = extra ?? new(),
        };
    }

    #endregion

    #region Load

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

    protected override void UpdateContainerWithMetaInformation(Container container, ReadOnlySpan<byte> disk, ReadOnlySpan<uint> decompressed)
    {
        if (_usesSaveStreaming)
        {
            if (container.IsAccount && container.MetaFile?.Exists == true)
            {
                container.Extra = container.Extra with
                {
                    MetaFormat = MetaFormatEnum.Waypoint,
                    Bytes = disk.ToArray(),
                    Size = decompressed[2],
                    SizeDecompressed = decompressed[2],
                    SizeDisk = decompressed[2],
                };
            }
            else if (_usesSaveWizard)
            {
                //  0. META HEADER          (  8) // here the same structure as used at the beginning of the memory.dat
                //  2. CONST (2)            (  4)
                //  3. META OFFSET          (  4)
                //  4. CONST (1)            (  4)
                //  5. COMPRESSED SIZE      (  4)
                //  6. EMPTY                ( 44) // here the same structure as the old memory.dat format starts but with many empty values
                // 17. META FORMAT          (  4)
                // 18. EMPTY                ( 20)
                // 23. DECOMPRESSED SIZE    (  4)
                // 24. EMPTY                (  4)
                // 25. CONST (1)            (  4)
                // 26. EMPTY                (  8)
                //                          (112)

                container.Extra = container.Extra with
                {
                    MetaFormat = MetaFormatEnum.Frontiers,
                    Size = decompressed[23],
                    SizeDecompressed = decompressed[23],
                    SizeDisk = decompressed[5],

                    PlaystationOffset = META_LENGTH_TOTAL_WAYPOINT,
                };
            }
        }
        else
        {
            //  0. META HEADER          ( 4)
            //  1. META FORMAT          ( 4)
            //  2. COMPRESSED SIZE      ( 4)
            //  3. CHUNK OFFSET         ( 4)
            //  4. CHUNK SIZE           ( 4)
            //  5. META INDEX           ( 4)
            //  6. TIMESTAMP            ( 4)
            //  7. DECOMPRESSED SIZE    ( 4)
            //                          (32)

            //  8. SAVEWIZARD OFFSET    ( 4)
            //  9. CONST (1)            ( 4)
            // 10. EMPTY                ( 8)
            //                          (48)

            if (decompressed.IsEmpty || decompressed[3] == 0)
            {
                container.Exists = false;
                return;
            }

            container.Extra = container.Extra with
            {
                MetaFormat = MetaFormatEnum.Foundation,
                Size = decompressed[MEMORYDAT_META_INDEX_LENGTH], // either COMPRESSED SIZE or DECOMPRESSED SIZE depending on SaveWizard usage
                SizeDisk = decompressed[2],
                SizeDecompressed = decompressed[7],
                LastWriteTime = DateTimeOffset.FromUnixTimeSeconds(decompressed[6]).ToLocalTime(),

                PlaystationOffset = (int)(decompressed[MEMORYDAT_META_INDEX_OFFSET]), // either CHUNK OFFSET or SAVEWIZARD OFFSET depending on SaveWizard usage
            };
        }
    }

    protected override ReadOnlySpan<byte> LoadData(Container container)
    {
        // 1. Read
        return LoadData(container, container.IsAccount ? ReadData(container) : (container.Extra.Bytes ?? ReadData(container)));
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
        var data = reader.ReadBytes((int)(reader.BaseStream.Length)); // read till end of stream as size from meta might not be correct

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

    protected override void UpdateContainerWithDataInformation(Container container, ReadOnlySpan<byte> disk, ReadOnlySpan<byte> decompressed)
    {
        // Sizes other than for AccountData need to be set directly in CompressData() as the compressed data wont be returned if _usesSaveWizard.
        if (container.IsAccount)
            container.Extra = container.Extra with
            {
                Size = _usesSaveWizard ? (uint)(decompressed.Length) : (uint)(disk.Length),
                SizeDecompressed = (uint)(decompressed.Length),
                SizeDisk = (uint)(disk.Length),
            };
        // Save memory by only storing it when necessary.
        else if (!_usesSaveStreaming)
            container.Extra = container.Extra with
            {
                Bytes = disk.ToArray(),
            };
    }

    protected override JObject? DeserializeContainer(Container container, ReadOnlySpan<byte> binary)
    {
        var jsonObject = base.DeserializeContainer(container, binary);
        if (jsonObject is null) // incompatibility properties are set in base
            return null;

        // Deobfuscate anyway if _useSaveWizard to realign mapping by SaveWizard.
        if (_usesSaveWizard)
            container.UnknownKeys = Mapping.Deobfuscate(jsonObject);

        return jsonObject;
    }

    #endregion

    #region Write

    public override void Write(Container container, DateTimeOffset writeTime)
    {
        if (_usesSaveStreaming)
        {
            base.Write(container, writeTime);
            return;
        }

        if (!CanUpdate || !container.IsLoaded)
            return;

        DisableWatcher();

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

        EnableWatcher();

        // Always refresh in case something above was executed.
        container.RefreshFileInfo();
        container.WriteCallback.Invoke();
    }

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

    protected override Span<uint> CreateMeta(Container container, ReadOnlySpan<byte> data)
    {
        var buffer = _usesSaveStreaming ? CreateSaveStreamingMeta(container) : CreateLegacyMeta(container);
        return buffer.AsSpan().Cast<byte, uint>();
    }

    private byte[] CreateSaveStreamingMeta(Container container)
    {
        byte[] buffer = [];

        if (container.IsAccount)
        {
            // Use Switch values of META_LENGTH_TOTAL in fallback.
            buffer = container.Extra.Bytes ?? new byte[container.MetaFormat == MetaFormatEnum.Waypoint ? 0x164 : 0x64];

            // Overwrite only SizeDecompressed.
            using var writer = new BinaryWriter(new MemoryStream(buffer));
            writer.Write(META_HEADER); // 4
            writer.Write(Constants.SAVE_FORMAT_3); // 4
            writer.Write(container.Extra.SizeDecompressed); // 4
        }
        else if (_usesSaveWizard) // no meta for homebrew if _usesSaveStreaming
        {
            buffer = new byte[META_LENGTH_TOTAL_WAYPOINT];

            using var writer = new BinaryWriter(new MemoryStream(buffer));

            writer.Write(SAVEWIZARD_HEADER_BINARY); // 8
            writer.Write(SAVEWIZARD_VERSION_2); // 4
            writer.Write(MEMORYDAT_OFFSET_META); // 4
            writer.Write(1); // 4
            writer.Write(container.Extra.SizeDisk); // 4

            writer.Seek(44, SeekOrigin.Current); // skip empty

            writer.Write(Constants.SAVE_FORMAT_3); // 4

            writer.Seek(20, SeekOrigin.Current); // skip empty

            writer.Write(container.Extra.SizeDecompressed); // 4

            writer.Seek(04, SeekOrigin.Current); // skip empty

            writer.Write(1); // 4
        }

        return buffer;
    }

    private byte[] CreateLegacyMeta(Container container)
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
        }
        else
        {
            using var writer = new BinaryWriter(new MemoryStream(buffer));
            writer.Write(META_HEADER); // 4
            writer.Write(Constants.SAVE_FORMAT_2); // 4

            writer.Seek(12, SeekOrigin.Current);

            writer.Write(uint.MaxValue); // 4
        }

        return buffer;
    }

    /// <summary>
    /// Writes the memory.dat file for the previous format.
    /// </summary>
#if !NETSTANDARD2_0
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0057: Use range operator", Justification = "The range operator is not supported in netstandard2.0 and Slice() has no performance penalties.")]
#endif
    private void WriteMemoryDat()
    {
        var buffer = new byte[_usesSaveWizard ? MEMORYDAT_LENGTH_TOTAL_SAVEWIZARD : MEMORYDAT_LENGTH_TOTAL];

        using var writer = new BinaryWriter(new MemoryStream(buffer));

        if (_usesSaveWizard)
        {
            writer.Write(SAVEWIZARD_HEADER_BINARY);
            writer.Write(Constants.SAVE_FORMAT_2);
            writer.Write(MEMORYDAT_OFFSET_META);
            writer.Write(SaveContainerCollection.Where(i => i.Exists).Count() + 1); // + 1 for AccountData that ia always present in memory.dat
            writer.Write(MEMORYDAT_LENGTH_TOTAL);

            writer.Seek(MEMORYDAT_OFFSET_META, SeekOrigin.Begin);
        }

        // AccountData
        AddContainerMeta(writer, AccountContainer);

        writer.Seek(META_LENGTH_TOTAL_VANILLA, SeekOrigin.Current);

        // Container
        foreach (var container in SaveContainerCollection)
            AddContainerMeta(writer, container);

        writer.Seek(MEMORYDAT_OFFSET_DATA, SeekOrigin.Begin);

        if (_usesSaveWizard)
        {
            // AccountData
            AddSaveWizardContainer(writer, AccountContainer);

            // Container
            foreach (var container in SaveContainerCollection.Where(i => i.Exists))
                AddSaveWizardContainer(writer, container);

            buffer = buffer.AsSpan().Slice(0, (int)(writer.BaseStream.Position)).ToArray();
        }
        else
        {
            // AccountData
            writer.Write(AccountContainer.Extra.Bytes!);

            // Container
            foreach (var container in SaveContainerCollection.Where(i => i.Exists))
            {
                writer.Seek(container.Extra.PlaystationOffset!.Value, SeekOrigin.Begin);
                writer.Write(container.Extra.Bytes!);
            }
        }

        // Write and refresh the memory.dat file.
        _memorydat!.WriteAllBytes(buffer);
        _memorydat!.Refresh();
    }

    private void AddContainerMeta(BinaryWriter writer, Container container)
    {
        var meta = CreateMeta(container, container.Extra.Bytes);
#if NETSTANDARD2_0
        writer.Write(meta.AsBytes().ToArray());
#else
        writer.Write(meta.AsBytes());
#endif
    }

    private static void AddSaveWizardContainer(BinaryWriter writer, Container container)
    {
        writer.Write(SAVEWIZARD_HEADER_BINARY);
        writer.Write(container.Extra.Bytes!);
    }

    #endregion

    // // File Operation

    #region Copy

    protected override void Copy(IEnumerable<(Container Source, Container Destination)> operationData, bool write)
    {
        if (_usesSaveStreaming)
        {
            base.Copy(operationData, write);
            return;
        }

        foreach (var (Source, Destination) in operationData)
            if (!Source.Exists)
            {
                Delete(Destination, false);
            }
            else if (Destination.Exists || (!Destination.Exists && CanCreate))
            {
                if (!Source.IsLoaded)
                    BuildContainerFull(Source);

                if (!Source.IsCompatible)
                    ThrowHelper.ThrowInvalidOperationException($"Cannot copy as the source container is not compatible: {Source.IncompatibilityTag}");

                Destination.SetJsonObject(Source.GetJsonObject());
                Destination.ClearIncompatibility();

                // Due to this CanCreate can be true.
                CopyPlatformExtra(Destination, Source);

                // Faking relevant properties to force it to Write().
                Destination.Exists = true;

                // Additional properties required to properly rebuild the container.
                Destination.GameVersion = Source.GameVersion;
                Destination.SaveVersion = Source.SaveVersion;

                // Update bytes in platform extra as it is what will be written later.
                // Could also be done in CopyPlatformExtra but here we do not need to override another method.
                Destination.Extra = Destination.Extra with
                {
                    Bytes = CreateData(Destination).ToArray(),
                    LastWriteTime = Source.LastWriteTime ?? DateTimeOffset.Now,
                };
            }

        if (write)
            WriteMemoryDat();
    }

    #endregion

    #region Delete

    protected override void Delete(IEnumerable<Container> containers, bool write)
    {
        if (_usesSaveStreaming)
        {
            base.Delete(containers, write);
            return;
        }

        Guard.IsTrue(CanDelete);

        DisableWatcher();

        foreach (var container in containers)
        {
            container.Reset();
            container.IncompatibilityTag = Constants.INCOMPATIBILITY_006;

            // Set afterwards again to make sure it is set to false.
            container.Exists = false;
        }

        if (write)
            WriteMemoryDat();

        EnableWatcher();
    }

    #endregion

    #region Move

    protected override void Move(IEnumerable<(Container Source, Container Destination)> containerOperationData, bool write)
    {
        if (_usesSaveStreaming)
        {
            base.Move(containerOperationData, write);
            return;
        }

        Copy(containerOperationData, false);
        Delete(containerOperationData.Select(i => i.Source), false);

        if (write)
            WriteMemoryDat();
    }

    #endregion

    #region Swap

    protected override void Swap(IEnumerable<(Container Source, Container Destination)> containerOperationData, bool write)
    {
        if (_usesSaveStreaming)
        {
            base.Move(containerOperationData, write);
            return;
        }

        // Make sure everything can be swapped.
        foreach (var (Source, Destination) in containerOperationData.Where(i => i.Source.Exists && i.Destination.Exists))
        {
            if (!Source.IsLoaded)
                BuildContainerFull(Source);

            if (!Destination.IsLoaded)
                BuildContainerFull(Destination);

            if (!Source.IsCompatible || !Destination.IsCompatible)
                ThrowHelper.ThrowInvalidOperationException($"Cannot swap as at least one container is not compatible: {Source.IncompatibilityTag} >> {Destination.IncompatibilityTag}");
        }

        foreach (var (Source, Destination) in containerOperationData)
        {
            if (Source.Exists)
            {
                // Source and Destination exists. Swap.
                if (Destination.Exists)
                {
                    // Keep a copy to be able to set Source correctly after Destination is done.
                    var copy = Common.DeepCopy(Destination);

                    // Write Source to Destination.
                    Destination.LastWriteTime = Source.LastWriteTime ?? DateTimeOffset.Now;
                    Destination.SetJsonObject(Source.GetJsonObject());
                    CopyPlatformExtra(Destination, Source);
                    RebuildContainerFull(Destination);

                    // Write Destination to Source.
                    Destination.LastWriteTime = copy.LastWriteTime ?? DateTimeOffset.Now;
                    Source.SetJsonObject(copy.GetJsonObject());
                    CopyPlatformExtra(Source, copy);
                    RebuildContainerFull(Source);
                }
                // Source exists only. Move to Destination.
                else
                    Move(Source, Destination, false);
            }
            // Destination exists only. Move to Source.
            else if (Destination.Exists)
                Move(Destination, Source, false);
        }

        UpdateUserIdentification();

        if (write)
            WriteMemoryDat();
    }

    #endregion

    #region Transfer

    protected override void Transfer(TransferData sourceTransferData, int destinationSlotIndex, bool write)
    {
        if (_usesSaveStreaming)
        {
            base.Transfer(sourceTransferData, destinationSlotIndex, write);
            return;
        }

        PrepareTransferDestination(destinationSlotIndex);

        if (!sourceTransferData.UserIdentification.IsComplete() || !PlatformUserIdentification.IsComplete())
            ThrowHelper.ThrowInvalidOperationException("Cannot transfer as at least one user identification is not complete.");

        foreach (var (Source, Destination) in sourceTransferData.Containers.Zip(SaveContainerCollection.Where(i => i.SlotIndex == destinationSlotIndex), (Source, Destination) => (Source, Destination)))
            if (!Source.Exists)
            {
                Delete(Destination, false);
            }
            else if (Destination.Exists || !Destination.Exists && CanCreate)
            {
                if (!Source.IsCompatible)
                    ThrowHelper.ThrowInvalidOperationException($"Cannot copy as the source container is not compatible: {Source.IncompatibilityTag}");

                Destination.SetPlatform(this);
                Destination.SetJsonObject(Source.GetJsonObject());
                Destination.ClearIncompatibility();

                // Due to this CanCreate can be true.
                CreatePlatformExtra(Destination, Source);

                // Faking relevant properties to force it to Write().
                Destination.Exists = true;

                // Additional properties required to properly rebuild the container.
                Destination.GameVersion = Source.GameVersion;
                Destination.SaveVersion = Source.SaveVersion;
                Destination.UserIdentification = PlatformUserIdentification; // update to match new platform

                // Update bytes in platform extra as it is what will be written later.
                // Could also be done in CopyPlatformExtra but here we do not need to override another method.
                Destination.Extra = Destination.Extra with
                {
                    Bytes = CreateData(Destination).ToArray(),
                    LastWriteTime = Source.LastWriteTime ?? DateTimeOffset.Now,
                };

                TransferOwnership(Destination, sourceTransferData);
            }

        if (write)
            WriteMemoryDat();
    }

    #endregion

    // // FileSystemWatcher

    #region FileSystemWatcher

    /// <summary>
    /// Refreshes all containers in the collection with newly written data from the memory.dat file.
    /// </summary>
    private void RefreshContainerCollection()
    {
        for (var metaIndex = 0; metaIndex < Constants.OFFSET_INDEX + COUNT_SAVES_TOTAL; metaIndex++)
            if (metaIndex == 0)
            {
                // Reset bytes to trigger to read the file again.
                AccountContainer.Extra = new PlatformExtra
                {
                    MetaFormat = MetaFormatEnum.Unknown,
                    Bytes = null,
                };
                RebuildContainerFull(AccountContainer);
            }
            else if (metaIndex > 1) // skip index 1
            {
                var collectionIndex = metaIndex - Constants.OFFSET_INDEX;
                var container = SaveContainerCollection[collectionIndex];

                // Reset bytes as trigger to read the file again.
                container.Extra = new PlatformExtra
                {
                    MetaFormat = MetaFormatEnum.Unknown,
                    Bytes = null,
                };

                // Only rebuild full if container was already loaded and not synced (to not overwrite pending watcher changes).
                if (container.IsLoaded)
                {
                    if (container.IsSynced)
                        RebuildContainerFull(container);
                }
                else
                    RebuildContainerHollow(container);

                GenerateBackupCollection(container);
            }
    }

    #endregion
}
