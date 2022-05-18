using libNOM.map;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace libNOM.io;


#region Container

internal record class PlaystationContainer
{
    internal byte[]? Bytes;
    internal int Length;
    internal int Offset;
    internal int SizeCompressed;
    internal int SizeDecompressed;
}

public partial class Container
{
    internal PlaystationContainer? PlayStation { get; set; }
}

#endregion

#region PlatformDirectoryData

internal record class PlatformDirectoryDataPlaystation : PlatformDirectoryData
{
    internal override string[] AnchorFileGlob { get; } = new[] { "savedata*.hg", "memory.dat" };

    internal override Regex[] AnchorFileRegex { get; } = new Regex[] { new("savedata\\d{2}\\.hg", RegexOptions.Compiled), new("memory\\.dat", RegexOptions.Compiled) };
}

#endregion

public class PlatformPlaystation : Platform
{
    #region Constant

    private int INDEX_SIZE => _useSaveWizard ? 7 : 2; // decompressed size if SaveWizard
    private int INDEX_OFFSET => _useSaveWizard ? 8 : 3; // additional dynamic block size if SaveWizard

    private const int MEMORYDAT_ANCHORFILE_INDEX = 1;
    private const string MEMORYDAT_NAME = "memory.dat";
    private const uint MEMORYDAT_OFFSET_CONTAINER = 0xE0000U;
    private uint MEMORYDAT_OFFSET_DATA => _useSaveWizard ? 0x1040U : 0x20000U;
    private const uint MEMORYDAT_SIZE_ACCOUNTDATA = 0x40000U;
    private const uint MEMORYDAT_SIZE_CONTAINER = 0x300000U;
    private const uint MEMORYDAT_SIZE_TOTAL = 0x2000000U; // 32 MB

    private const uint META_HEADER = 0xCA55E77EU;
    private int META_OFFSET => _useSaveWizard ? 0x40 : 0x0; // 64 : 0
    private int META_SIZE => _useSaveWizard ? 0x30 : 0x20; // 48 : 32

    private static readonly byte[] SAVEWIZARD_HEADER_BINARY = Global.HEADER_SAVEWIZARD.GetUTF8Bytes();
    private const int SAVEWIZARD_MEMORYDAT_OFFSET_COUNT = 0x10; // 16

    // Overrideable
    internal override int COUNT_SLOTS => _useSaveStreaming ? base.COUNT_SLOTS : 5;

    #endregion

    #region Field

    private FileInfo? _memoryDatFile;
    private bool _useSaveStreaming;
    private bool _useSaveWizard;

    #endregion

    #region Property

    #region Flags

    public override bool CanCreate { get; } = true;

    public override bool CanRead { get; } = true;

    public override bool CanUpdate { get; } = true;

    public override bool CanDelete { get; } = true;

    #endregion

    #region Platform Indicator

    internal static PlatformDirectoryData DirectoryData { get; } = new PlatformDirectoryDataPlaystation();

    internal override PlatformDirectoryData PlatformDirectoryData { get; } = DirectoryData;

    protected override string PlatformArchitecture { get; } = "PS4|Final";

    public override PlatformEnum PlatformEnum { get; } = PlatformEnum.Playstation;

    protected override string PlatformToken { get; } = "PS";

    #endregion

    #endregion

    #region Getter

    /// <summary>
    /// Gets whether the files have been created with SaveWizard.
    /// </summary>
    /// <param name="directory"></param>
    /// <returns></returns>
    protected bool GetSaveWizardUsage(DirectoryInfo directory)
    {
        // _MemoryDatFile is set when this is called.
        var file = _useSaveStreaming ? directory.GetFiles().FirstOrDefault(f => PlatformDirectoryData.AnchorFileRegex[AnchorFileIndex].IsMatch(f.Name)) : _memoryDatFile!;
        if (file is null)
            return false;

        using var reader = new BinaryReader(File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
        return SAVEWIZARD_HEADER_BINARY.SequenceEqual(reader.ReadBytes(SAVEWIZARD_HEADER_BINARY.Length));
    }

    #endregion

    // //

    #region Constructor

    public PlatformPlaystation() : base(null, null) { }

    public PlatformPlaystation(DirectoryInfo? directory) : base(directory, null) { }

    public PlatformPlaystation(DirectoryInfo? directory, PlatformSettings? platformSettings) : base(directory, platformSettings) { }

    protected override void InitializeComponent(DirectoryInfo? directory, PlatformSettings? platformSettings)
    {
        // Proceed to base method even if no directory.
        if (directory is not null)
        {
            _memoryDatFile = new FileInfo(Path.Combine(directory.FullName, MEMORYDAT_NAME));
            _useSaveStreaming = GetAnchorFileIndex(directory) != MEMORYDAT_ANCHORFILE_INDEX;
            _useSaveWizard = GetSaveWizardUsage(directory); // works after _MemoryDatFile and _UseSaveStreaming are set
        }

        base.InitializeComponent(directory, platformSettings);
    }

    #endregion

    // //

    #region Container

    protected override IEnumerable<Container> GetCachedContainers(string name)
    {
        if (_useSaveStreaming)
            return base.GetCachedContainers(name);

        if (!name.Equals(MEMORYDAT_NAME, StringComparison.OrdinalIgnoreCase))
            return Array.Empty<Container>();

        RefreshContainerCollection();
        return GetExistingContainers();
    }

    #endregion

    #region Copy

    /// <inheritdoc cref="Platform.Copy(IEnumerable{ContainerOperationData}, bool)"/>
    protected override void Copy(IEnumerable<ContainerOperationData> containerOperationData, bool write)
    {
        if (_useSaveStreaming)
        {
            base.Copy(containerOperationData, write);
        }
        else
        {
            foreach (var (Source, Destination) in containerOperationData.Select(d => (d.Source, d.Destination)))
            {
                if (!Source.Exists)
                {
                    Delete(Destination, false);
                }
                else // if (Destination.Exists || !Destination.Exists && CanCreate)
                {
                    if (Source.PlayStation is null)
                        throw new InvalidOperationException("The source container has no PlayStation extra.");

                    if (!Source.IsLoaded)
                    {
                        BuildContainer(Source);
                    }
                    if (!Source.IsCompatible)
                    {
                        throw new InvalidOperationException(Source.IncompatibilityTag);
                    }

                    // Due to this CanCreate can be true.
                    if (!Destination.Exists)
                    {
                        Destination.PlayStation = new PlaystationContainer
                        {
                            Bytes = Source.PlayStation.Bytes,
                            SizeCompressed = Source.PlayStation.SizeCompressed,
                            SizeDecompressed = Source.PlayStation.SizeDecompressed,
                        };
                    }

                    Destination.SetJsonObject(Source.GetJsonObject());

                    Destination.Exists = true;
                    Destination.IsSynced = true;
                    Destination.LastWriteTime = Source.LastWriteTime;

                    BuildContainer(Destination);
                }
            }

            if (write)
            {
                WriteMemoryDat();
            }

            UpdatePlatformUserIdentification();
        }
    }

    #endregion

    #region Delete

    protected override void Delete(IEnumerable<Container> containers, bool write)
    {
        if (!CanDelete)
            return;

        if (_useSaveStreaming)
        {
            base.Delete(containers, write);
        }
        else
        {
            foreach (var container in containers)
            {
                container.Reset();

                // Set afterwards to make sure it is set to false.
                container.Exists = false;
            }

            if (write)
            {
                WriteMemoryDat();
            }
        }
    }

    #endregion

    #region FileSystemWatcher

    protected override void OnCacheEviction(object key, object value, EvictionReason reason, object state)
    {
        // Necessary to avoid deserialization in BuildContainer for not loaded container.
        _init = true;

        base.OnCacheEviction(key, value, reason, state);

        _init = false;
    }

    /// <summary>
    /// Refreshes all containers in the collection with newly written data from the containers.index file.
    /// </summary>
    private void RefreshContainerCollection()
    {
        for (var slotIndex = 0; slotIndex < COUNT_SLOTS; slotIndex++)
        {
            foreach (var containerIndex in new[] { (COUNT_SAVES_PER_SLOT * slotIndex), (COUNT_SAVES_PER_SLOT * slotIndex + 1) })
            {
                // Reset bytes to read the file again.
                ContainerCollection[containerIndex].PlayStation!.Bytes = null;
                // Rebuilds the container and refreshes all relevant data in that process.
                BuildContainer(ContainerCollection[containerIndex]);
            }
        }
    }

    #endregion

    #region Move

    protected override void Move(IEnumerable<ContainerOperationData> containerOperationData, bool write)
    {
        if (_useSaveStreaming)
        {
            base.Move(containerOperationData, write);
        }
        else
        {
            Copy(containerOperationData, false);
            Delete(containerOperationData.Select(c => c.Source), false);

            if (write)
            {
                WriteMemoryDat();
            }
        }
    }

    #endregion

    #region Read

    #region Create

    protected override Container CreateContainer(int metaIndex, object? extra)
    {
        if (_useSaveStreaming)
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
            DataFile = _memoryDatFile,
            MetaFile = _memoryDatFile,
        };
    }

    #endregion

    #region Load

    protected override byte[] LoadContainer(Container container)
    {
        if (_useSaveStreaming)
            return base.LoadContainer(container);

        // Do LoadMeta outside the if as it sets whether the container exists.
        var meta = LoadMeta(container);

        if (container.Exists)
        {
            var data = LoadData(container, meta);
            if (!data.IsNullOrEmpty())
                return data;
        }

        container.IncompatibilityTag = "F001_Empty";
        return Array.Empty<byte>();
    }

    protected override byte[] ReadMeta(Container container)
    {
        if (_useSaveStreaming)
        {
            if (_useSaveWizard && container.MetaFile is not null)
            {
                using var reader = new BinaryReader(File.Open(container.MetaFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                reader.BaseStream.Seek(0x10, SeekOrigin.Begin);
                return reader.ReadBytes(0x60);
            }
        }
        else if (_memoryDatFile is not null)
        {
            using var reader = new BinaryReader(File.Open(_memoryDatFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            reader.BaseStream.Seek(META_OFFSET + (container.MetaIndex * META_SIZE), SeekOrigin.Begin);
            return reader.ReadBytes(META_SIZE);
        }

        return Array.Empty<byte>();
    }

    protected override uint[] DecryptMeta(Container container, byte[] meta)
    {
        var data = base.DecryptMeta(container, meta);

        if (_useSaveStreaming)
        {
            if (_useSaveWizard)
            {
                container.PlayStation = new PlaystationContainer
                {
                    Length = (int)(data[19]),
                    Offset = 0x70,
                    SizeCompressed = (int)(data[1]),
                    SizeDecompressed = (int)(data[19]),
                };
            }
        }
        else
        {
            container.Exists = data[2] != 0;
            container.LastWriteTime = DateTimeOffset.FromUnixTimeSeconds(data[6]).ToLocalTime();
            container.PlayStation = new PlaystationContainer
            {
                Length = (int)(data[INDEX_SIZE]),
                Offset = (int)(data[INDEX_OFFSET]),
                SizeCompressed = (int)(data[2]),
                SizeDecompressed = (int)(data[7]),
            };
        }

        return data;
    }

    protected override byte[] LoadData(Container container, uint[] meta)
    {
        var data = container.PlayStation!.Bytes is not null ? container.PlayStation!.Bytes : ReadData(container);
        return LoadData(container, meta, data);
    }

    protected override byte[] ReadData(Container container)
    {
        if (!_useSaveStreaming || _useSaveWizard)
        {
            var path = _useSaveStreaming ? container.DataFile!.FullName : _memoryDatFile!.FullName;

            using var reader = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read));
            reader.BaseStream.Seek(container.PlayStation!.Offset, SeekOrigin.Begin);

            var data = reader.ReadBytes(container.PlayStation.Length);

            // Store raw bytes as the block size is dynamic and moves if SaveWizard is used. Therefore the entire file needs to be rebuild.
            if (!_useSaveStreaming)
            {
                container.PlayStation.Bytes = data;
            }

            return data;
        }

        return base.ReadData(container);
    }

    protected override byte[] DecompressData(Container container, uint[] meta, byte[] data)
    {
        // SaveWizard already did the decompression.
        if (_useSaveWizard)
            return data;

        // Same as for Steam.
        if (_useSaveStreaming)
        {
            var result = new List<byte>();

            var offset = 0;
            while (offset < data.Length)
            {
                var chunkHeader = data.Skip(offset).Take(SAVE_STREAMING_CHUNK_HEADER_SIZE).GetUInt32();
                offset += SAVE_STREAMING_CHUNK_HEADER_SIZE;

                var chunkCompressed = (int)(chunkHeader[1]);
                var chunkDecompressed = (int)(chunkHeader[2]);

                var source = data.Skip(offset).Take(chunkCompressed).ToArray();
                offset += chunkCompressed;

                _ = LZ4_Decode(source, out byte[] target, chunkDecompressed);
                result.AddRange(target);
            }

            return result.ToArray();
        }
        else
        {
            _ = LZ4_Decode(data, out byte[] target, container.PlayStation!.SizeDecompressed);
            return target;
        }
    }

    #endregion

    #region Process

    protected override JObject? DeserializeContainer(Container container, byte[] binary)
    {
        // Convert binary data into a deobfuscated JObject and get list of unknown mapping keys as side product.
        JObject? jsonObject;
        try
        {
            jsonObject = binary.GetJson();
        }
        catch (Exception x) when (x is JsonReaderException or JsonSerializationException)
        {
            container.IncompatibilityException = x;
            container.IncompatibilityTag = "F002_Deserialization";
            return null;
        }
        if (jsonObject is null)
        {
            container.IncompatibilityTag = "F002_Deserialization";
            return null;
        }

        // Deobfuscate anyway if _useSaveWizard to realign mapping by SaveWizard.
        if (Settings.Mapping || _useSaveWizard)
        {
            container.UnknownKeys = Mapping.Deobfuscate(jsonObject);
        }
        // Do deliver a consistent experience, obfuscate if _useSaveWizard and mapping is disabled.
        if (_useSaveWizard && !Settings.Mapping)
        {
            Mapping.Obfuscate(jsonObject);
        }

        return jsonObject;
    }

    #endregion

    #endregion

    #region Transfer

    protected override void Transfer(ContainerTransferData sourceTransferData, int destinationSlot, bool write)
    {
        if (!sourceTransferData.UserIdentification.IsComplete() || !PlatformUserIdentification.IsComplete())
            throw new InvalidOperationException("Cannot transfer as at least one UserIdentification is not complete.");

        if (_useSaveStreaming)
        {
            base.Transfer(sourceTransferData, destinationSlot, write);
        }
        else
        {
            var destinationContainers = GetSlotContainers(destinationSlot);

#if NET47_OR_GREATER || NETSTANDARD2_0_OR_GREATER
            foreach (var (Source, Destination) in sourceTransferData.Containers.Zip(destinationContainers, (Source, Destination) => (Source, Destination)))
#elif NET5_0_OR_GREATER
            foreach (var (Source, Destination) in sourceTransferData.Containers.Zip(destinationContainers))
#endif
            {
                if (!Source.Exists)
                {
                    Delete(Destination, false);
                }
                else // if (Destination.Exists || !Destination.Exists && CanCreate)
                {
                    if (!Source.IsLoaded)
                    {
                        BuildContainer(Source);
                    }
                    if (!Source.IsCompatible)
                    {
                        throw new InvalidOperationException(Source.IncompatibilityTag);
                    }

                    // Due to this CanCreate can be true (combined with CreateData below).
                    if (!Destination.Exists)
                    {
                        Destination.PlayStation = new();
                    }

                    // VersionEnum must be updated to determine what needs to be transferred after setting the jsonObject in Destination.
                    Destination.VersionEnum = Source.VersionEnum;

                    Destination.SetJsonObject(Source.GetJsonObject());
                    TransferOwnership(Destination, sourceTransferData);

                    Destination.Exists = true;
                    Destination.IsSynced = true;
                    Destination.LastWriteTime = Source.LastWriteTime;

                    (Destination.PlayStation!.Bytes, _) = CreateData(Destination); // properties SizeCompressed and SizeDecompressed are set inside

                    BuildContainer(Destination);
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

    #endregion

    #region Write

    public override void Write(Container container, DateTimeOffset writeTime)
    {
        if (_useSaveStreaming)
        {
            base.Write(container, writeTime: writeTime);
        }
        else
        {
            if (!CanUpdate || !container.IsLoaded)
                return;

            if (!container.IsSynced)
            {
                container.Exists = true;
                container.IsSynced = true;

                (container.PlayStation!.Bytes, _) = CreateData(container); // properties SizeCompressed and SizeDecompressed are set inside
            }

            if (Settings.LastWriteTime)
            {
                container.LastWriteTime = writeTime.Equals(default) ? DateTimeOffset.Now.LocalDateTime : writeTime;
                WriteTime(container);
            }

            WriteMemoryDat();

            container.RefreshFileInfo();
            container.WriteCallback.Invoke();
        }
    }

    /// <summary>
    /// Writes the memory.dat file for the previous format.
    /// </summary>
    private void WriteMemoryDat()
    {
        DisableWatcher();

        var cached = new List<byte[]>();
        var content = new List<byte>();

        var buffer = new byte[MEMORYDAT_OFFSET_DATA];

        using (var writer = new BinaryWriter(new MemoryStream(buffer)))
        {
            if (_useSaveWizard)
            {
                using var reader = new BinaryReader(File.Open(_memoryDatFile!.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                reader.Read(buffer, 0, META_OFFSET);

                writer.BaseStream.Seek(SAVEWIZARD_MEMORYDAT_OFFSET_COUNT, SeekOrigin.Begin);
                writer.Write(ContainerCollection.Where(c => c.Exists).Count() + 1); // 1 for AccountData

                writer.BaseStream.Seek(META_OFFSET, SeekOrigin.Begin);
            }

            // AccountData
            {
                var meta = CreateMeta(AccountContainer!, AccountContainer!.PlayStation!.Bytes!, AccountContainer.PlayStation.SizeDecompressed);
                writer.Write(meta);
            }

            // META INDEX 1
            {
                writer.BaseStream.Seek(META_SIZE, SeekOrigin.Current);
            }

            // Container
            foreach (var container in ContainerCollection)
            {
                var meta = CreateMeta(container, container.PlayStation!.Bytes!, container.PlayStation.SizeDecompressed);
                writer.Write(meta);
            }
        }

        // Add buffer to content.
        content.AddRange(buffer);

        // Add cached data to content.
        if (_useSaveWizard)
        {
            // AccountData
            content.AddRange(SAVEWIZARD_HEADER_BINARY);
            content.AddRange(AccountContainer.PlayStation.Bytes!);

            // Container
            foreach (var container in ContainerCollection.Where(c => c.Exists))
            {
                content.AddRange(SAVEWIZARD_HEADER_BINARY);
                content.AddRange(container.PlayStation!.Bytes!);
            }
        }
        else
        {
            // AccountData
            buffer = new byte[MEMORYDAT_OFFSET_CONTAINER - MEMORYDAT_OFFSET_DATA];
            using (var writer = new BinaryWriter(new MemoryStream(buffer)))
            {
                writer.Write(AccountContainer.PlayStation.Bytes!);
            }
            content.AddRange(buffer);

            // Container
            foreach (var container in ContainerCollection)
            {
                buffer = new byte[MEMORYDAT_SIZE_CONTAINER];
                if (container.Exists)
                {
                    using var writer = new BinaryWriter(new MemoryStream(buffer));
                    writer.Write(container.PlayStation!.Bytes!);
                }
                content.AddRange(buffer);
            }
            // Fill with empty bytes to total file size.
            content.AddRange(new byte[MEMORYDAT_SIZE_TOTAL - content.Count]);
        }

        File.WriteAllBytes(_memoryDatFile!.FullName, content.ToArray());

        EnableWatcher();
    }

    protected override byte[] CompressData(Container container, byte[] data)
    {
        container.PlayStation!.SizeDecompressed = data.Length;
        container.PlayStation!.SizeCompressed = LZ4_Encode(data, out byte[] target);

        if (_useSaveWizard)
            return data;

        return target;
    }

    protected override void WriteData(Container container, byte[] data)
    {
        if (_useSaveWizard)
        {
            // Append data to already written meta.
            using var stream = new FileStream(container.DataFile!.FullName, FileMode.Append);
            stream.Write(data, 0, data.Length);

            File.SetLastWriteTime(container.DataFile.FullName, container.LastWriteTime.LocalDateTime);
        }
        else
        {
            base.WriteData(container, data);
        }
    }

    protected override byte[] CreateMeta(Container container, byte[] data, int decompressedSize)
    {
        if (_useSaveStreaming)
        {
            if (_useSaveWizard)
            {
                var buffer = new byte[0x70];

                using var writer = new BinaryWriter(new MemoryStream(buffer));

                writer.Write(SAVEWIZARD_HEADER_BINARY);
                writer.Write(2);
                writer.Write(META_OFFSET);
                writer.Write(1);
                writer.Write(container.PlayStation!.SizeCompressed);

                writer.Seek(META_OFFSET + 4, SeekOrigin.Begin);

                writer.Write(SAVE_FORMAT_360);

                writer.Seek(0x5C, SeekOrigin.Begin);

                writer.Write(container.PlayStation!.SizeDecompressed);

                writer.Seek(4, SeekOrigin.Current);

                writer.Write(1);

                return buffer;
            }
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
            //                          (32)
            //  8. SAVEWIZARD OFFSET    ( 4)
            //  9. 1                    ( 4)
            // 10. UNKNOWN              ( 8)
            //                          (48)

            var buffer = new byte[META_SIZE];

            if (container.Exists)
            {
                var legacyOffset = container.MetaIndex == 0 ? 0x20000 : (uint)(MEMORYDAT_OFFSET_CONTAINER + (container.CollectionIndex * MEMORYDAT_SIZE_CONTAINER));
                var legacyLength = container.MetaIndex == 0 ? MEMORYDAT_SIZE_ACCOUNTDATA : MEMORYDAT_SIZE_CONTAINER;
                var unixSeconds = (uint)(container.LastWriteTime.ToUniversalTime().ToUnixTimeSeconds());

                using var writer = new BinaryWriter(new MemoryStream(buffer));

                writer.Write(META_HEADER);
                writer.Write(SAVE_FORMAT_110);
                writer.Write(container.PlayStation!.SizeCompressed);
                writer.Write(legacyOffset);

                writer.Write(legacyLength);
                writer.Write(container.MetaIndex);
                writer.Write(unixSeconds);
                writer.Write(container.PlayStation!.SizeDecompressed);

                if (_useSaveWizard)
                {
                    var offset = MEMORYDAT_OFFSET_DATA + Global.HEADER_SAVEWIZARD.Length;
                    if (container.MetaIndex > 0)
                    {
                        var precedingContainer = ContainerCollection.Where(e => e.Exists && e.CollectionIndex < container.CollectionIndex);

                        offset += AccountContainer!.PlayStation!.SizeDecompressed;
                        offset += precedingContainer.Sum(e => Global.HEADER_SAVEWIZARD.Length + e.PlayStation!.SizeDecompressed);
                        offset += Global.HEADER_SAVEWIZARD.Length;
                    }
                    writer.Write(offset);

                    writer.Write(1);
                }
            }
            else if (!_useSaveWizard)
            {
                using var writer = new BinaryWriter(new MemoryStream(buffer));

                writer.Write(META_HEADER);
                writer.Write(SAVE_FORMAT_110);

                writer.BaseStream.Seek(0xC, SeekOrigin.Current);
                writer.Write(uint.MaxValue);
            }

            return buffer;
        }

        return Array.Empty<byte>();
    }

    #endregion
}
