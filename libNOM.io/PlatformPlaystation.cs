using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using libNOM.map;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace libNOM.io;


public partial class PlatformPlaystation : Platform
{
    #region Constant

    protected override int COUNT_SAVE_SLOTS => _usesSaveStreaming ? base.COUNT_SAVE_SLOTS : 5;

    #region Platform Specific

    private const uint MEMORYDAT_LENGTH_ACCOUNTDATA = 0x40000U;
    private const uint MEMORYDAT_LENGTH_CONTAINER = 0x300000U;
    private const uint MEMORYDAT_LENGTH_TOTAL = 0x2000000U; // 32 MB

    protected int MEMORYDAT_META_INDEX_OFFSET => _usesSaveWizard ? 8 : 3;
    protected int MEMORYDAT_META_INDEX_LENGTH => _usesSaveWizard ? 7 : 2;

    protected int MEMORYDAT_OFFSET_META => _usesSaveWizard ? 0x40 : 0x0; // 64 : 0
    protected int MEMORYDAT_OFFSET_DATA => _usesSaveWizard ? 0x1040 : 0x20000; // 4160 : 131072
    private const uint MEMORYDAT_OFFSET_DATA_CONTAINER = 0xE0000U;

    protected const uint META_HEADER = 0xCA55E77E;

    protected override int META_LENGTH_TOTAL_VANILLA => _usesSaveWizard ? 0x30 : 0x20; // 48 : 32
    protected override int META_LENGTH_TOTAL_WAYPOINT => _usesSaveWizard ? 0x70 : 0x0; // 112 : 0 // actually Frontiers but reused as no use otherwise

    protected const string SAVEWIZARD_HEADER = "NOMANSKY";
    protected static readonly byte[] SAVEWIZARD_HEADER_BINARY = SAVEWIZARD_HEADER.GetUTF8Bytes();
    protected const int SAVEWIZARD_VERSION_1 = 1;
    protected const int SAVEWIZARD_VERSION_2 = 2;

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

    #region Directory Data

    public static readonly string[] ANCHOR_FILE_GLOB = new[] { "savedata*.hg", "memory.dat" };
#if NETSTANDARD2_0_OR_GREATER || NET6_0
    public static readonly Regex[] ANCHOR_FILE_REGEX = new Regex[] { AnchorFileRegex0, AnchorFileRegex1 };
#else
    public static readonly Regex[] ANCHOR_FILE_REGEX = new Regex[] { AnchorFileRegex0(), AnchorFileRegex1() };
#endif

    #endregion

    #endregion

    #region Field

    private DateTimeOffset? _lastWriteTime;
    private FileInfo? _memorydat;
    private bool _usesSaveStreaming;
    private bool _usesSaveWizard;

    #endregion

    #region Property

    #region Configuration

    // public //

    public override PlatformEnum PlatformEnum { get; } = PlatformEnum.Playstation;

    // protected //

    protected override string[] PlatformAnchorFileGlob { get; } = ANCHOR_FILE_GLOB;

    protected override Regex[] PlatformAnchorFileRegex { get; } = ANCHOR_FILE_REGEX;

    protected override string? PlatformArchitecture { get; } = "PS4|Final";

    protected override string? PlatformProcessPath { get; } = null;

    protected override string PlatformToken { get; } = "PS";

    #endregion

    #region Flags

    // public //

    public override bool HasAccountData => _usesSaveStreaming && base.HasAccountData; // { get; }

    public override bool HasModding { get; } = false;

    public override bool RestartToApply { get; } = true;

    // protected //

    protected override bool CanCreate { get; } = true;

    protected override bool CanRead { get; } = true;

    protected override bool CanUpdate { get; } = true;

    protected override bool CanDelete { get; } = true;

    protected override bool IsConsolePlatform { get; } = true;

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

        // Cache previous timestamp.
        var lastWriteTicks = _lastWriteTime!.Value.UtcTicks.GetBlobTicks();

        // Refresh will also update _lastWriteTime.
        RefreshContainerCollection();

        // Get all written container that are newer than the previous timestamp.
        return SaveContainerCollection.Where(i => i.Exists && i.LastWriteTime?.UtcTicks >= lastWriteTicks);
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
            if (anchorFileIndex == 1) // memory.dat
            {
                _memorydat = new FileInfo(Path.Combine(directory.FullName, "memory.dat"));
                _lastWriteTime = _memorydat.LastWriteTime;
            }
            else
            {
                _usesSaveStreaming = true;
            }

            // Get first file that is not account data if not _memorydat.
            var f = _memorydat ?? directory.GetFiles().FirstOrDefault(f => PlatformAnchorFileRegex[anchorFileIndex].IsMatch(f.Name) && !f.Name.Contains("00"));
            if (f is not null)
            {
                using var reader = new BinaryReader(File.Open(f.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                _usesSaveWizard = SAVEWIZARD_HEADER_BINARY.SequenceEqual(reader.ReadBytes(SAVEWIZARD_HEADER_BINARY.Length));
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
            return new Container(metaIndex)
            {
                DataFile = data,
                MetaFile = metaIndex == 0 ? (meta.Exists ? meta : null) : (_usesSaveWizard ? data : null),
                /// Additional values will be set in <see cref="UpdateContainerWithMetaInformation"/> and <see cref="UpdateContainerWithDataInformation"/>.
                Extra = new()
                {
                    LastWriteTime = data.LastWriteTime,
                },
            };
        }

        return new Container(metaIndex)
        {
            DataFile = _memorydat,
            MetaFile = _memorydat,
            /// Additional values will be set in <see cref="UpdateContainerWithMetaInformation"/> and <see cref="UpdateContainerWithDataInformation"/>.
            Extra = new(),
        };
    }

    protected override JObject? DeserializeContainer(Container container, ReadOnlySpan<byte> binary)
    {
        JObject? jsonObject;
        try
        {
            jsonObject = binary.GetJson();
        }
        catch (Exception ex) when (ex is JsonReaderException or JsonSerializationException)
        {
            container.IncompatibilityException = ex;
            container.IncompatibilityTag = Constants.INCOMPATIBILITY_002;
            return null;
        }
        if (jsonObject is null)
        {
            container.IncompatibilityTag = Constants.INCOMPATIBILITY_003;
            return null;
        }

        // Deobfuscate anyway if _useSaveWizard to realign mapping by SaveWizard.
        if (Settings.UseMapping || _usesSaveWizard)
        {
            container.UnknownKeys = Mapping.Deobfuscate(jsonObject);
        }

        // Do deliver a consistent experience, make sure the file is obfuscated if the setting is set to false.
        if (Settings.UseMapping is false) // is false is more visible than a !
        {
            Mapping.Obfuscate(jsonObject);
        }

        return jsonObject;
    }

    #endregion

    #region Load

    protected override ReadOnlySpan<byte> LoadContainer(Container container)
    {
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
            {
                container.IncompatibilityTag = Constants.INCOMPATIBILITY_001;
            }
            else
            {
                return data;
            }
        }

        container.IncompatibilityTag ??= Constants.INCOMPATIBILITY_006;
        return Array.Empty<byte>();
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
        return Array.Empty<byte>();
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
            return Array.Empty<byte>();

        if (_usesSaveStreaming && (container.IsAccount || !_usesSaveWizard))
            return base.ReadData(container);

        // memory.dat and _usesSaveStreaming with _usesSaveWizard files contain multiple information and need to be cut out.
        using var reader = new BinaryReader(File.Open(container.DataFile!.FullName, FileMode.Open, FileAccess.Read, FileShare.Read));

        reader.BaseStream.Seek(container.Extra.PlaystationOffset!.Value, SeekOrigin.Begin);
        var data = reader.ReadBytes((int)(container.Extra.Size));

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
        // Sizes other than for AccountData need to be set directly in CompressData() as the compressed data wont be retunred if _usesSaveWizard.
        if (container.IsAccount)
        {
            container.Extra = container.Extra with
            {
                Size = _usesSaveWizard ? (uint)(decompressed.Length) : (uint)(disk.Length),
                SizeDecompressed = (uint)(decompressed.Length),
                SizeDisk = (uint)(disk.Length),
            };
        }
        // Save memory by only storing it when necessary.
        else if (!_usesSaveStreaming)
        {
            container.Extra = container.Extra with
            {
                Bytes = disk.ToArray(),
            };
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

                    _ = PrepareData(container); // stored in container.Extra.Bytes and written in WriteMemoryDat()
                }

                if (Settings.SetLastWriteTime)
                {
                    _lastWriteTime = container.LastWriteTime = writeTime;
                }

                WriteMemoryDat();
            }

            EnableWatcher();

            // Always refresh in case something above was executed.
            container.RefreshFileInfo();
            container.WriteCallback.Invoke();
        }
    }

    protected override ReadOnlySpan<byte> CompressData(Container container, ReadOnlySpan<byte> data)
    {
        ReadOnlySpan<byte> result;

        if (_usesSaveStreaming)
        {
            if (container.IsAccount)
                return data;

            result = base.CompressData(container, data);
        }
        else
        {
            // Compression for AccountData as well.
            _ = LZ4.Encode(data, out var r);
            result = r;
        }

        container.Extra = container.Extra with
        {
            Size = _usesSaveWizard ? (uint)(data.Length) : (uint)(result.Length),
            SizeDecompressed = (uint)(data.Length),
            SizeDisk = (uint)(result.Length),
        };

        // SaveWizard will do the compression itself.
        if (_usesSaveWizard)
            return data;

        return result;
    }

    protected override void WriteData(Container container, ReadOnlySpan<byte> data)
    {
        // memory.dat will be written in its own method and therefore we do not need to write anything here.
        if (_usesSaveStreaming)
            if (_usesSaveWizard)
            {
                // Append data to already written meta.
                using var stream = new FileStream(container.DataFile!.FullName, FileMode.Append);
#if NETSTANDARD2_0
                stream.Write(data.ToArray(), 0, data.Length);
#else
                stream.Write(data);
#endif
            }
            else
            {
                // Homebrew handled as usual.
                base.WriteData(container, data);
            }
    }

    protected override Span<uint> CreateMeta(Container container, ReadOnlySpan<byte> data)
    {
        byte[] buffer = Array.Empty<byte>();

        if (_usesSaveStreaming)
        {
            if (container.IsAccount)
            {
                // Use Switch values of META_LENGTH_TOTAL in fallback.
                buffer = container.Extra.Bytes ?? new byte[container.MetaFormat == MetaFormatEnum.Waypoint ? 0x164 : 0x64];

                using var writer = new BinaryWriter(new MemoryStream(buffer));

                // Overwrite only SizeDecompressed.
                writer.Write(META_HEADER); // 4
                writer.Write(Constants.SAVE_FORMAT_3); // 4
                writer.Write(container.Extra.SizeDecompressed); // 4
            }
            else if (_usesSaveWizard)
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
        }
        else
        {
            buffer = new byte[GetMetaSize(container)];

            if (container.Exists)
            {
                var legacyOffset = container.IsAccount ? 0x20000 : (uint)(MEMORYDAT_OFFSET_DATA_CONTAINER + (container.CollectionIndex * MEMORYDAT_LENGTH_CONTAINER));
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

                writer.BaseStream.Seek(12, SeekOrigin.Current);
                writer.Write(uint.MaxValue); // 4
            }
        }

        return buffer.AsSpan().Cast<byte, uint>();
    }

    /// <summary>
    /// Writes the memory.dat file for the previous format.
    /// </summary>
    private void WriteMemoryDat()
    {
        // 16 MB more for SaveWizard as ten uncompressed saves may exceed the default length.
        var buffer = new byte[_usesSaveWizard ? 0x3000000U : MEMORYDAT_LENGTH_TOTAL];

        using (var writer = new BinaryWriter(new MemoryStream(buffer)))
        {
            if (_usesSaveWizard)
            {
                writer.Write(SAVEWIZARD_HEADER_BINARY);
                writer.Write(Constants.SAVE_FORMAT_2);
                writer.Write(MEMORYDAT_OFFSET_META);
                writer.Write(GetExistingContainers().Count() + 1); // + 1 for AccountData that are always present in memory.dat
                writer.Write(MEMORYDAT_LENGTH_TOTAL);

                writer.BaseStream.Seek(MEMORYDAT_OFFSET_META, SeekOrigin.Begin);
            }

            // AccountData
            {
#if NETSTANDARD2_0
                var meta = CreateMeta(AccountContainer, AccountContainer.Extra.Bytes);
                writer.Write(meta.AsBytes().ToArray());
#else
                var meta = CreateMeta(AccountContainer, AccountContainer.Extra.Bytes);
                writer.Write(meta.AsBytes());
#endif
            }

            writer.BaseStream.Seek(META_LENGTH_TOTAL_VANILLA, SeekOrigin.Current);

            // Container
            foreach (var container in SaveContainerCollection)
            {
#if NETSTANDARD2_0
                var meta = CreateMeta(container, container.Extra.Bytes);
                writer.Write(meta.AsBytes().ToArray());
#else
                var meta = CreateMeta(container, container.Extra.Bytes);
                writer.Write(meta.AsBytes());
#endif
            }

            writer.BaseStream.Seek(MEMORYDAT_OFFSET_DATA, SeekOrigin.Begin);

            if (_usesSaveWizard)
            {
                // AccountData
                writer.Write(SAVEWIZARD_HEADER_BINARY);
                writer.Write(AccountContainer.Extra.Bytes!);

                // Container
                foreach (var container in GetExistingContainers())
                {
                    writer.Write(SAVEWIZARD_HEADER_BINARY);
                    writer.Write(container.Extra.Bytes!);
                }

                buffer = buffer.AsSpan().Slice(0, (int)(writer.BaseStream.Position)).ToArray();
            }
            else
            {
                // AccountData
                writer.Write(AccountContainer.Extra.Bytes!);

                // Container
                foreach (var container in SaveContainerCollection)
                {
                    writer.BaseStream.Seek(MEMORYDAT_OFFSET_DATA_CONTAINER + (container.CollectionIndex * MEMORYDAT_LENGTH_CONTAINER), SeekOrigin.Begin);

                    if (container.Exists)
                    {
                        writer.Write(container.Extra.Bytes!);
                    }
                }
            }
        }

        // Write and refresh the memory.dat file.
        File.WriteAllBytes(_memorydat!.FullName, buffer);
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
                    CopyPlatformExtra(Destination, Source);

                    // Additional properties required to properly rebuild the container. 
                    Destination.GameVersion = Source.GameVersion;
                    Destination.SaveVersion = Source.SaveVersion;

                    // Faking relevant properties to force it to Write().
                    Destination.Exists = true;

                    Destination.SetJsonObject(Source.GetJsonObject());

                    // This "if" is not really useful in this method but properly implemented nonetheless.
                    if (write)
                    {
                        Write(Destination, Source.LastWriteTime ?? DateTimeOffset.Now);
                        RebuildContainerFull(Destination);
                    }
                }
            }
            //else
            //    continue;
        }

        UpdateUserIdentification();
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
                container.IncompatibilityTag = Constants.INCOMPATIBILITY_006;

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

    #region Swap

    protected override void Swap(IEnumerable<(Container Source, Container Destination)> containerOperationData, bool write)
    {
        if (_usesSaveStreaming)
        {
            base.Move(containerOperationData, write);
        }
        else
        {
            // Make sure everything can be swapped.
            foreach (var (Source, Destination) in containerOperationData.Where(i => i.Source.Exists && i.Destination.Exists))
            {
                if (!Source.IsLoaded)
                    BuildContainerFull(Source);

                if (!Destination.IsLoaded)
                    BuildContainerFull(Destination);

                if (!Source.IsCompatible || !Destination.IsCompatible)
                    throw new InvalidOperationException($"Cannot swap as at least one container is not compatible: {Source.IncompatibilityTag} / {Destination.IncompatibilityTag}");
            }

            foreach (var (Source, Destination) in containerOperationData)
            {
                if (Source.Exists)
                {
                    // Source and Destination exists. Swap.
                    if (Destination.Exists)
                    {
                        // Cache.
                        var jsonObject = Destination.GetJsonObject();
                        var writeTime = Destination.LastWriteTime;

                        // Write Source to Destination.
                        Destination.LastWriteTime = Source.LastWriteTime;
                        Destination.SetJsonObject(Source.GetJsonObject());
                        RebuildContainerFull(Destination);

                        // Write Destination to Source.
                        Destination.LastWriteTime = writeTime;
                        Source.SetJsonObject(jsonObject);
                        RebuildContainerFull(Source);
                    }
                    // Source exists only. Move to Destination.
                    else
                    {
                        Move(Source, Destination, false);
                    }
                }
                // Destination exists only. Move to Source.
                else if (Destination.Exists)
                {
                    Move(Destination, Source, false);
                }
            }

            UpdateUserIdentification();

            if (write)
            {
                WriteMemoryDat();
            }
        }
    }

    #endregion

    // TODO Transfer Refactoring

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
                    Destination.GameVersion = Source.GameVersion;
                    Destination.Season = Source.Season;

                    Destination.SetJsonObject(Source.GetJsonObject());
                    TransferOwnership(Destination, sourceTransferData);

                    // Update bytes in platform extra as it is what will be written later.
                    Destination.Extra = Destination.Extra with { Bytes = CreateData(Destination).ToArray() };

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
        destination.Extra = new() { MetaFormat = source.Extra.MetaFormat };
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
        {
            if (metaIndex == 0)
            {
                // Reset bytes as trigger to read the file again.
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

                // Only build full if container was already loaded.
                if (Settings.LoadingStrategy < LoadingStrategyEnum.Full && !container.IsLoaded)
                {
                    RebuildContainerHollow(container);
                }
                else
                {
                    // Do not rebuild if not synced (to not overwrite pending watcher changes).
                    if (container.IsSynced)
                        RebuildContainerFull(container);
                }
            }
        }
    }

    #endregion
}
