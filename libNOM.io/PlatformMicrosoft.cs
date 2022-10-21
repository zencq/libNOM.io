using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace libNOM.io;


#region Container

internal record class MicrosoftContainer
{
    internal FileInfo BlobContainerFile => new(Path.Combine(BlobDirectory.FullName, $"container.{Extension}"));

    internal DirectoryInfo BlobDirectory = null!;

    internal Guid BlobGuid;

    internal FileInfo BlobDataFile = null!;

    internal FileInfo BlobMetaFile = null!;

    internal byte Extension;

    internal DateTimeOffset LastModifiedTime;

    internal byte[]? MetaTail;

    internal string SyncHex = string.Empty;

    internal MicrosoftBlobStateEnum State;

    internal ulong TotalSize;
}

public partial class Container
{
    internal MicrosoftContainer? Microsoft { get; set; }
}

#endregion

#region PlatformDirectoryData

internal record class PlatformDirectoryDataMicrosoft : PlatformDirectoryData
{
    internal override string DirectoryPath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", "HelloGames.NoMansSky_bs190hzg1sesy", "SystemAppData", "wgs");

    internal override string DirectoryPathPattern { get; } = "*_29070100B936489ABCE8B9AF3980429C";

    internal override string[] AnchorFileGlob { get; } = new[] { "containers.index" };

    internal override Regex[] AnchorFileRegex { get; } = new Regex[] { new("containers\\.index", RegexOptions.Compiled) };
}

#endregion

public class PlatformMicrosoft : Platform
{
    #region Constant

    private const int CONTAINERSINDEX_HEADER = 0xE; // 14
    private const string CONTAINERSINDEX_NAME = "containers.index";
    private const long CONTAINERSINDEX_UNKNOWN_CONST = 0x10000000; // 268,435,456
    private const int CONTAINERSINDEX_OFFSET_CONTAINER = 0xC8; // 200
    private const int CONTAINERSINDEX_OFFSET_DYNAMIC = 0xC; // 12

    private const int CONTAINER_BLOB_COUNT = 0x2; // 2
    private const int CONTAINER_BLOB_IDENTIFIER_LENGTH = 0x80; // 128
    private const int CONTAINER_BLOB_SIZE = 0xA0; // 160
    private const int CONTAINER_HEADER = 0x4; // 4
    private const int CONTAINER_OFFSET_DATA = 0x98; // 152
    private const int CONTAINER_OFFSET_META = 0x138; // 312
    private const int CONTAINER_SIZE = 0x4 + 0x4 + CONTAINER_BLOB_COUNT * (0x80 + 2 * 0x10); // 328

    private const int META_KNOWN = 0x14; // 20
    private const int META_SIZE = 0x18; // 24
    private const int META_SIZE_WAYPOINT = 0x118; // 280

    #endregion

    #region Field

    private string? _accountId;
    private FileInfo? _containersIndexFile;
    private DateTimeOffset _lastModifiedTime;
    private MicrosoftContainer? _settingsContainer;

    #endregion

    #region Property

    #region Flags

    public override bool CanCreate { get; } = true;

    public override bool CanRead { get; } = true;

    public override bool CanUpdate { get; } = true;

    public override bool CanDelete { get; } = true;

    public override bool RestartToApply { get; } = true;

    public override bool IsWindowsPlatform { get; } = true;

    #endregion

    #region Platform Indicator

    internal static PlatformDirectoryData DirectoryData { get; } = new PlatformDirectoryDataMicrosoft();

    internal override PlatformDirectoryData PlatformDirectoryData { get; } = DirectoryData;

    protected override string PlatformArchitecture { get; } = "XB1|Final";

    public override PlatformEnum PlatformEnum { get; } = PlatformEnum.Microsoft;

    protected override string PlatformToken { get; } = "XB";

    #endregion

    #region Process (System)

    protected override string? ProcessPath { get; } = "bs190hzg1sesy\\Binaries\\NMS.exe";

    #endregion

    #endregion

    // //

    #region Constructor

    public PlatformMicrosoft() : base(null, null) { }

    public PlatformMicrosoft(DirectoryInfo? directory) : base(directory, null) { }

    public PlatformMicrosoft(DirectoryInfo? directory, PlatformSettings? platformSettings) : base(directory, platformSettings) { }

    protected override void InitializeComponent(DirectoryInfo? directory, PlatformSettings? platformSettings)
    {
        // Proceed to base method even if no directory.
        if (directory is not null)
        {
            try
            {
                _accountId = System.Convert.ToInt64(directory.Name.Split('_')[0], 16).ToString();
            }
            catch (Exception x) when (x is FormatException or OverflowException) { }
            _containersIndexFile = new FileInfo(Path.Combine(directory.FullName, CONTAINERSINDEX_NAME));
        }

        base.InitializeComponent(directory, platformSettings);
    }

    #endregion

    // //

    #region Container

    protected override IEnumerable<Container> GetCachedContainers(string name)
    {
        if (!name.Equals(CONTAINERSINDEX_NAME, StringComparison.OrdinalIgnoreCase))
            return Array.Empty<Container>();

        RefreshContainerCollection();
        return GetExistingContainers();
    }

    #endregion

    #region Copy

    /// <inheritdoc cref="Platform.Copy(IEnumerable{ContainerOperationData}, bool)"/>
    protected override void Copy(IEnumerable<ContainerOperationData> containerOperationData, bool write)
    {
        foreach (var (Source, Destination) in containerOperationData.Select(d => (d.Source, d.Destination)))
        {
            if (!Source.Exists)
            {
                Delete(Destination, write);
            }
            else // if (d.Destination.Exists || !d.Destination.Exists && CanCreate)
            {
                if (Source.Microsoft is null)
                    throw new InvalidOperationException("The source container has no Microsoft extra.");

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
                    // Prepare a new Microsoft container.
                    Destination.Microsoft = new MicrosoftContainer
                    {
                        BlobGuid = Guid.NewGuid(),
                        Extension = 0,
                        LastModifiedTime = Source.Microsoft.LastModifiedTime,
                        MetaTail = Source.Microsoft.MetaTail,
                        State = MicrosoftBlobStateEnum.Created,
                    };

                    var directory = new DirectoryInfo(Path.Combine(Location!.FullName, Destination.Microsoft.BlobGuid.ToPath()));

                    // Set dummy files. Will be overwritten while writing.
                    Destination.DataFile = new(Path.Combine(directory.FullName, "data.guid"));
                    Destination.MetaFile = new(Path.Combine(directory.FullName, "meta.guid"));

                    // Update Microsoft container with new directory and file information.
                    Destination.Microsoft.BlobDirectory = directory;
                    Destination.Microsoft.BlobDataFile = Destination.DataFile;
                    Destination.Microsoft.BlobMetaFile = Destination.MetaFile;

                    // Prepare blob container file content. Guid of data and meta file will be set while writing.
                    var blobContainerBinary = new byte[CONTAINER_SIZE];
                    using (var writer = new BinaryWriter(new MemoryStream(blobContainerBinary)))
                    {
                        var data = "data".GetUnicodeBytes();

                        writer.Write(CONTAINER_HEADER);
                        writer.Write(CONTAINER_BLOB_COUNT);
                        writer.Write(data);

                        writer.BaseStream.Seek(CONTAINER_BLOB_SIZE - data.Length, SeekOrigin.Current);

                        writer.Write("meta".GetUnicodeBytes());
                    }

                    Directory.CreateDirectory(Destination.Microsoft.BlobDirectory.FullName);
                    File.WriteAllBytes(Destination.Microsoft.BlobContainerFile.FullName, blobContainerBinary);
                }

                // Properties requied to properly build the container below.
                Destination.BaseVersion = Source.BaseVersion;

                Destination.SetJsonObject(Source.GetJsonObject());

                // This "if" is not really useful in this method but properly implemented nonetheless.
                if (write)
                {
                    Write(Destination, writeTime: Source.LastWriteTime);
                    BuildContainer(Destination);
                }
            }
        }

        UpdatePlatformUserIdentification();
    }

    #endregion

    #region Delete

    protected override void Delete(IEnumerable<Container> containers, bool write)
    {
        if (!CanDelete)
            return;

        foreach (var container in containers)
        {
            if (!container.Exists || container.Microsoft is null)
                continue;

            container.Microsoft.State = MicrosoftBlobStateEnum.Deleted;

            if (write)
            {
                if (container.Microsoft.BlobDirectory.Exists)
                {
                    Directory.Delete(container.Microsoft.BlobDirectory.FullName, true);
                }
            }

            container.Reset();
        }

        _lastModifiedTime = DateTimeOffset.Now.LocalDateTime;

        // Refresh to get the new offsets.
        if (write)
        {
            WriteContainersIndex();
            RefreshContainerCollection();
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
        var microsoft = ReadContainersIndex();
        if (microsoft.Count == 0)
            return;

        // Just store it to write it later.
        if (microsoft.ContainsKey(1))
        {
            _settingsContainer = microsoft[1];
        }

        for (var slotIndex = 0; slotIndex < COUNT_SLOTS; slotIndex++)
        {
            foreach (var containerIndex in new[] { (COUNT_SAVES_PER_SLOT * slotIndex), (COUNT_SAVES_PER_SLOT * slotIndex + 1) })
            {
                var contains = microsoft.TryGetValue(containerIndex + Global.OFFSET_INDEX, out var extra);
                if (contains)
                {
                    ContainerCollection[containerIndex].DataFile = extra!.BlobDataFile;
                    ContainerCollection[containerIndex].LastWriteTime = extra!.LastModifiedTime;
                    ContainerCollection[containerIndex].MetaFile = extra!.BlobMetaFile;
                    ContainerCollection[containerIndex].Microsoft = extra!;
                }
            }
        }

        if (microsoft.ContainsKey(0))
        {
            var extra = microsoft[0];

            AccountContainer!.DataFile = extra.BlobDataFile;
            AccountContainer!.LastWriteTime = extra.LastModifiedTime;
            AccountContainer!.MetaFile = extra.BlobMetaFile;
            AccountContainer!.Microsoft = extra;
        }
    }

    #endregion

    #region Getter

    private static string GetBlobContainerPath(MicrosoftContainer microsoft)
    {
        return GetBlobContainerPath(microsoft, microsoft.Extension);
    }

    private static string GetBlobContainerPath(MicrosoftContainer microsoft, byte extension)
    {
        return Path.Combine(microsoft!.BlobDirectory.FullName, $"container.{extension}");
    }

    private static FileInfo GetBlobFileInfo(string path, Guid guid)
    {
        return new(Path.Combine(path, guid.ToPath()));
    }

    private string GetTemporaryName(string fileToWrite)
    {
        return Path.Combine(DirectoryData.DirectoryPath, "t", $"{_containersIndexFile!.DirectoryName}_{fileToWrite}");
    }

    private string GetTemporaryName(string fileToWrite, Container container)
    {
        return Path.Combine(DirectoryData.DirectoryPath, "t", $"{_containersIndexFile!.DirectoryName}_{container.Microsoft!.BlobDirectory.Name}_{fileToWrite}");
    }

    #endregion

    #region Read

    #region Build

    /// <summary>
    /// Builds a <see cref="Container"/> for each blob in the containers.index file.
    /// </summary>
    /// <returns></returns>
    protected override IEnumerable<Container> BuildContainerCollection()
    {
        var bag = new ConcurrentBag<Container>();
        var tasks = new List<Task>();

        var microsoft = ReadContainersIndex();
        if (microsoft.Count == 0)
            return bag;

        if (microsoft.ContainsKey(0))
        {
            tasks.Add(Task.Run(() => AccountContainer = BuildContainer(0, microsoft[0])));
        }

        // Just store it to write it later.
        if (microsoft.ContainsKey(1))
        {
            _settingsContainer = microsoft[1];
        }

        _init = true;

        for (var slotIndex = 0; slotIndex < COUNT_SLOTS; slotIndex++)
        {
            foreach (var containerIndex in new[] { (COUNT_SAVES_PER_SLOT * slotIndex), (COUNT_SAVES_PER_SLOT * slotIndex + 1) })
            {
                var metaIndex = containerIndex + Global.OFFSET_INDEX;
                _ = microsoft.TryGetValue(metaIndex, out var extra);

                tasks.Add(Task.Run(() =>
                {
                    var container = BuildContainer(metaIndex, extra);
                    LoadBackupCollection(container);
                    bag.Add(container);
                }));
            }
        }

        Task.WaitAll(tasks.ToArray());

        _init = false;
        return bag;
    }

    /// <summary>
    /// Reads the containers.index file to get information where each blob is.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidDataException"/>
    private Dictionary<int, MicrosoftContainer> ReadContainersIndex()
    {
        /** containers.index structure

        containers.index data
         0. HEADER (14)                     (  4)
         1. NUMBER OF BLOB CONTAINERS       (  8)
         2. GAME IDENTIFIER LENGTH (44)     (  4)
         3. GAME IDENTIFIER                 ( 88) (UTF-16)
         4. LAST MODIFIED TIME              (  8)
         5. SYNC STATE                      (  4) (0 = ?, 1 = ?, 2 = MODIFIED, 3 = SYNCED)
         6. ACCOUNT IDENTIFIER LENGTH (36)  (  4)
         7. ACCOUNT IDENTIFIER              ( 72) (UTF-16)
         8. ?                               (  8)
                                            (200)

        blob container data loop
        10. SAVE IDENTIFIER LENGTH          (  4)
        11. SAVE IDENTIFIER                 (VAR) (UTF-16)
        12. SAVE IDENTIFIER LENGTH          (  4)
        13. SAVE IDENTIFIER                 (VAR) (UTF-16)
        14. SYNC HEX LENGTH                 (  4)
        15. SYNC HEX                        (VAR) (UTF-16)
        16. BLOB CONTAINER FILE EXTENSION   (  1)
        17. SYNC STATE                      (  4) (0 = ?, 1 = SYNCED, 2 = MODIFIED, 3 = DELETED, 4 = ?, 5 = CREATED)
        18. DIRECTORY                       ( 16) (GUID)
        19. LAST MODIFIED TIME              (  8)
        20. ?                               (  8)
        21. TOTAL SIZE OF FILES             (  8) (BLOB CONTAINER EXCLUDED)
        */

        var result = new Dictionary<int, MicrosoftContainer>();

        using var readerIndex = new BinaryReader(File.Open(_containersIndexFile!.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));

        if (readerIndex.ReadInt32() is int version && version != CONTAINERSINDEX_HEADER)
            throw new InvalidDataException($"Wrong version of containers.index file! Expected {CONTAINERSINDEX_HEADER} but got {version}.");

        // Total number of blob containers in the containers.index file.
        var containerCount = readerIndex.ReadInt64();

        // Read length of the identifier and then the identifier itself.
        readerIndex.BaseStream.Seek(readerIndex.ReadInt32() * 2, SeekOrigin.Current);

        // Store timestamp.
        _lastModifiedTime = DateTimeOffset.FromFileTime(readerIndex.ReadInt64()).ToLocalTime();

        // Skip containers.index state enum.
        readerIndex.BaseStream.Seek(0x4, SeekOrigin.Current);

        // Skip account identifier and unknown data (0x8) end the end of the header.
        readerIndex.BaseStream.Seek(readerIndex.ReadInt32() * 2 + 0x8, SeekOrigin.Current);

        for (var i = 0; i < containerCount; i++)
        {
            var container = new MicrosoftContainer();

            // Read length of the identifier and then the identifier itself. Repeats itself.
            var saveIdentifier = readerIndex.ReadBytes(readerIndex.ReadInt32() * 2).GetUnicode();
            if (saveIdentifier != readerIndex.ReadBytes(readerIndex.ReadInt32() * 2).GetUnicode())
                continue;

            container.SyncHex = readerIndex.ReadBytes(readerIndex.ReadInt32() * 2).GetUnicode();
            container.Extension = readerIndex.ReadByte();
            container.State = readerIndex.ReadInt32().DenumerateTo<MicrosoftBlobStateEnum>();
            container.BlobGuid = readerIndex.ReadBytes(0x10).GetGuid();
            container.BlobDirectory = new DirectoryInfo(Path.Combine(Location!.FullName, container.BlobGuid.ToPath()));
            container.LastModifiedTime = DateTimeOffset.FromFileTime(readerIndex.ReadInt64()).ToLocalTime();

            readerIndex.BaseStream.Seek(8, SeekOrigin.Current); // unknown data

            container.TotalSize = readerIndex.ReadUInt64();

            // Stop if directory specified in the entry does not exist.
            if (!container.BlobDirectory.Exists)
                continue;

            if (container.State != MicrosoftBlobStateEnum.Deleted)
            {
                var blobContainer = GetBlobContainerPath(container);
                var fileInfos = new HashSet<FileInfo>();

                // In case the blob container extension does not match the containers.index value, try all existing ones until a data file is found.
                if (!File.Exists(blobContainer))
                {
                    foreach (var file in container.BlobDirectory.GetFiles("container.*"))
                    {
                        fileInfos.Add(file);
                    }
                }
                else
                {
                    fileInfos.Add(new(blobContainer));
                }

                foreach (var file in fileInfos)
                {
                    using var readerBlob = new BinaryReader(File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));

                    if (readerBlob.BaseStream.Length != CONTAINER_SIZE)
                        continue;

                    if (readerBlob.ReadInt32() != CONTAINER_HEADER)
                        continue;

                    var blobCount = readerBlob.ReadInt32();
                    if (blobCount != CONTAINER_BLOB_COUNT)
                        continue;

                    for (var j = 0; j < blobCount; j++)
                    {
                        var blobIdentifier = readerBlob.ReadBytes(CONTAINER_BLOB_IDENTIFIER_LENGTH).GetUnicode().Trim('\0');

                        // Needs to be the second GUID as the first one not always contains the local name.
                        readerBlob.BaseStream.Seek(0x10, SeekOrigin.Current);
                        var guid = readerBlob.ReadBytes(0x10).GetGuid();

                        var blobFile = GetBlobFileInfo(container.BlobDirectory.FullName, guid);

                        if (blobIdentifier == "data")
                        {
                            container.BlobDataFile = blobFile;
                            container.LastModifiedTime = new[] { container.LastModifiedTime, container.BlobDataFile.CreationTime }.Max();
                            continue;
                        }

                        if (blobIdentifier == "meta")
                        {
                            container.BlobMetaFile = blobFile;
                            continue;
                        }
                    }

                    // Update extension in case the read one was not found and break the loop.
                    if (container.BlobDataFile?.Exists == true)
                    {
#if NETSTANDARD2_0
                        container.Extension = System.Convert.ToByte(file.Extension.Substring(1));
#else
                        container.Extension = System.Convert.ToByte(file.Extension[1..]);
#endif
                        break;
                    }
                }

                // Mark as deleted if there is no existing data file.
                if (container.BlobDataFile?.Exists != true)
                {
                    container.State = MicrosoftBlobStateEnum.Deleted;
                }
            }

            // Store collected data for further processing.
            if (saveIdentifier.StartsWith("Slot"))
            {
                var manual = System.Convert.ToInt32(saveIdentifier.EndsWith("Manual"));
                var slot = System.Convert.ToInt32(string.Concat(saveIdentifier.Where(char.IsDigit)));

                var metaIndex = Global.OFFSET_INDEX + ((slot - 1) * 2) + manual;

                result.Add(metaIndex, container);
            }
            else if (saveIdentifier == "Settings")
            {
                result.Add(1, container);
            }
            else if (saveIdentifier == "AccountData")
            {
                result.Add(0, container);
            }
        }

        return result;
    }

    #endregion

    #region Create

    protected override Container CreateContainer(int metaIndex, object? extra)
    {
        if (extra is not MicrosoftContainer microsoft)
            return new Container(metaIndex);

        return new Container(metaIndex)
        {
            DataFile = microsoft.BlobDataFile,
            LastWriteTime = microsoft.LastModifiedTime,
            MetaFile = microsoft.BlobMetaFile,
            Microsoft = microsoft,
        };
    }

    #endregion

    #region Load

    protected override uint[] DecryptMeta(Container container, byte[] meta)
    {
#if NETSTANDARD2_0
        container.Microsoft!.MetaTail = meta.Skip(META_KNOWN).ToArray();
#else
        container.Microsoft!.MetaTail = meta[META_KNOWN..];
#endif

        return base.DecryptMeta(container, meta);
    }

    protected override byte[] DecompressData(Container container, uint[] meta, byte[] data)
    {
        var length = meta.ContainsIndex(4) ? (int)(meta[4]) : data.Length;
        _ = LZ4_Decode(data, out byte[] target, length);
        return target;
    }

    #endregion

    #endregion

    #region Transfer

    protected override void Transfer(ContainerTransferData sourceTransferData, int destinationSlot, bool write)
    {
        if (!sourceTransferData.UserIdentification.IsComplete() || !PlatformUserIdentification.IsComplete())
            throw new InvalidOperationException("Cannot transfer as at least one UserIdentification is not complete.");

        var destinationContainers = GetSlotContainers(destinationSlot);

#if NETSTANDARD2_0_OR_GREATER
        foreach (var (Source, Destination) in sourceTransferData.Containers.Zip(destinationContainers, (Source, Destination) => (Source, Destination)))
#else
        foreach (var (Source, Destination) in sourceTransferData.Containers.Zip(destinationContainers))
#endif
        {
            if (!Source.Exists)
            {
                Delete(Destination, write);
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

                // Due to this CanCreate can be true.
                if (!Destination.Exists)
                {
                    Destination.Microsoft = new MicrosoftContainer
                    {
                        BlobGuid = Guid.NewGuid(),
                        Extension = 0,
                        State = MicrosoftBlobStateEnum.Created,
                        LastModifiedTime = Source.LastWriteTime,
                    };

                    var directory = new DirectoryInfo(Path.Combine(Location!.FullName, Destination.Microsoft.BlobGuid.ToPath()));

                    // Set dummy files. Will be overwritten while writing.
                    Destination.DataFile = new(Path.Combine(directory.FullName, "data.guid"));
                    Destination.MetaFile = new(Path.Combine(directory.FullName, "meta.guid"));

                    // Update Microsoft container with new directory and file information.
                    Destination.Microsoft.BlobDirectory = directory;
                    Destination.Microsoft.BlobDataFile = Destination.DataFile;
                    Destination.Microsoft.BlobMetaFile = Destination.MetaFile;

                    var blobContainerBinary = new byte[CONTAINER_SIZE];
                    using (var writer = new BinaryWriter(new MemoryStream(blobContainerBinary)))
                    {
                        var data = "data".GetUnicodeBytes();

                        writer.Write(CONTAINER_HEADER);
                        writer.Write(CONTAINER_BLOB_COUNT);
                        writer.Write(data);

                        writer.BaseStream.Seek(CONTAINER_BLOB_SIZE - data.Length, SeekOrigin.Current);

                        writer.Write("meta".GetUnicodeBytes());
                    }

                    Directory.CreateDirectory(Destination.Microsoft.BlobDirectory.FullName);
                    File.WriteAllBytes(Destination.Microsoft.BlobContainerFile.FullName, blobContainerBinary);
                }

                // Properties requied to properly build the container below.
                Destination.BaseVersion = Source.BaseVersion;
                Destination.VersionEnum = Source.VersionEnum;

                Destination.SetJsonObject(Source.GetJsonObject());
                TransferOwnership(Destination, sourceTransferData);

                if (write)
                {
                    Write(Destination, writeTime: Source.LastWriteTime);
                    BuildContainer(Destination);
                }
            }
            //else
            //    continue;
        }
    }

    #endregion

    #region UserIdentification

    protected override string GetUserIdentification(JObject jsonObject, string key)
    {
        if (key is "UID" && _accountId is not null)
            return _accountId;

        var result = base.GetUserIdentification(jsonObject, key);
        if (!string.IsNullOrEmpty(result))
            return result;

        return result;
    }

    protected override IEnumerable<JToken> GetUserIdentificationByBase(JObject jsonObject, string key)
    {
        if (_accountId is null)
            return base.GetUserIdentificationByBase(jsonObject, key);

        var path = Settings.Mapping ? $"PlayerStateData.PersistentPlayerBases[?({{0}})].Owner.{key}" : $"6f=.F?0[?({{0}})].3?K.{key}";
        var expressions = new[]
        {
            Settings.Mapping ? $"@.BaseType.PersistentBaseTypes == '{PersistentBaseTypesEnum.HomePlanetBase}' || @.BaseType.PersistentBaseTypes == '{PersistentBaseTypesEnum.FreighterBase}'" : $"@.peI.DPp == '{PersistentBaseTypesEnum.HomePlanetBase}' || @.peI.DPp == '{PersistentBaseTypesEnum.FreighterBase}'", // only with own base
            Settings.Mapping ? $"@.Owner.UID == '{_accountId}'" : $"@.3?K.K7E == '{_accountId}'", // only with specified value
        };

        return GetUserIdentificationIntersection(jsonObject, path, expressions);
    }

    protected override IEnumerable<JToken> GetUserIdentificationByDiscovery(JObject jsonObject, string key)
    {
        if (_accountId is null)
            return base.GetUserIdentificationByBase(jsonObject, key);

        var path = Settings.Mapping ? $"DiscoveryManagerData.DiscoveryData-v1.Store.Record[?({{0}})].OWS.{key}" : $"fDu.ETO.OsQ.?fB[?({{0}})].ksu.{key}";
        var expressions = new[]
        {
            Settings.Mapping ? $"@.OWS.UID == '{_accountId}'" : $"@.ksu.K7E == '{_accountId}'", // only with specified value
        };

        return GetUserIdentificationIntersection(jsonObject, path, expressions);
    }

    protected override IEnumerable<JToken> GetUserIdentificationBySettlement(JObject jsonObject, string key)
    {
        if (_accountId is null)
            return base.GetUserIdentificationByBase(jsonObject, key);

        var path = Settings.Mapping ? $"PlayerStateData.SettlementStatesV2[?({{0}})].Owner.{key}" : $"6f=.GQA[?({{0}})].3?K.{key}";
        var expressions = new[]
        {
            Settings.Mapping ? $"@.Owner.UID == '{_accountId}'" : $"@.3?K.K7E == '{_accountId}'", // only with specified value
        };

        return GetUserIdentificationIntersection(jsonObject, path, expressions);
    }

    #endregion

    #region Write

    public override void Write(Container container, DateTimeOffset writeTime)
    {
        if (!CanUpdate || !container.IsLoaded)
            return;

        container.Exists = true;
        container.IsSynced = true;

        var (data, decompressedSize) = CreateData(container);
        var meta = CreateMeta(container, data, decompressedSize);

        // Writing all Microsoft Store files at once in the same way as the game itself does.
        {
            // First update blob with new values.
            var blobReturn = CreateBlob(container);

            // Second write the previously created data and meta blob files.
            WriteMeta(container, meta);
            WriteData(container, data);

            // Third write the blob container.
            var tBlob = GetTemporaryName($"container.{container.Microsoft!.Extension}", container);
            File.WriteAllBytes(tBlob, blobReturn.UpdatedContainerBytes);
            File.Move(tBlob, GetBlobContainerPath(container.Microsoft!));

            // Update timestamps if enabled.
            if (Settings.LastWriteTime)
            {
                _lastModifiedTime = DateTimeOffset.Now.LocalDateTime;

                writeTime = writeTime.Equals(default) ? _lastModifiedTime : writeTime;
                var ticks = writeTime.Ticks % (long)(Math.Pow(10, 4)) * -1;

                container.LastWriteTime = writeTime.AddTicks(ticks);
            }

            // Finally write the containers.index file and delete all old blob files.
            WriteContainersIndex();

            File.Delete(GetBlobContainerPath(container.Microsoft!, blobReturn.OldExtension));
            File.Delete(blobReturn.OldDataFile!.FullName);
            File.Delete(blobReturn.OldMetaFile!.FullName);
        }

        // Always refresh in case something above was executed.
        container.RefreshFileInfo();
        container.WriteCallback.Invoke();
    }

    /// <summary>
    /// Updates the library container with new blob data and returns the old ones.
    /// </summary>
    /// <param name="container"></param>
    private static CreateBlobReturnData CreateBlob(Container container)
    {
        // Generate things.
        var dataGuid = Guid.NewGuid();
        var metaGuid = Guid.NewGuid();

        var blobReturn = new CreateBlobReturnData
        {
            OldDataFile = container.DataFile!,
            OldExtension = container.Microsoft!.Extension,
            OldMetaFile = container.MetaFile!,
            UpdatedContainerBytes = File.ReadAllBytes(GetBlobContainerPath(container.Microsoft!)),
        };

        // Update blob container content.
        using (var writerBlob = new BinaryWriter(new MemoryStream(blobReturn.UpdatedContainerBytes)))
        {
            writerBlob.Seek(CONTAINER_OFFSET_DATA, SeekOrigin.Begin);
            writerBlob.Write(dataGuid.ToByteArray());

            writerBlob.Seek(CONTAINER_OFFSET_META, SeekOrigin.Begin);
            writerBlob.Write(metaGuid.ToByteArray());
        }

        // Update library container information.
        container.DataFile = GetBlobFileInfo(container.DataFile!.Directory!.FullName, dataGuid);
        container.MetaFile = GetBlobFileInfo(container.MetaFile!.Directory!.FullName, metaGuid);
        container.Microsoft!.Extension = (byte)(container.Microsoft!.Extension == byte.MaxValue ? 1 : container.Microsoft!.Extension + 1);

        if (container.Microsoft!.State == MicrosoftBlobStateEnum.Synced)
        {
            container.Microsoft!.State = MicrosoftBlobStateEnum.Modified;
        }

        return blobReturn;
    }

    /// <summary>
    /// Writes the specified bytes to the containers.index file.
    /// </summary>
    private void WriteContainersIndex()
    {
        var hasAccountData = AccountContainer is not null;
        var hasSettings = _settingsContainer is not null;

        var collection = ContainerCollection.Where(c => c.Microsoft?.State > MicrosoftBlobStateEnum.Unknown_Zero);
        var count = (long)(collection.Count() + (hasAccountData ? 1 : 0) + (hasSettings ? 1 : 0));

        // Longest name (e.g. Slot10Manual) has a total length of 0x8C and therefore 0x90 is more than enough.
        // Leftover will be cut off by using only data up to the current writer position.
        var bytes = new byte[CONTAINERSINDEX_OFFSET_CONTAINER + (count * 0x90)];

        var accountIdentifier = string.Empty;
        var gameIdentifier = string.Empty;
        var state = MicrosoftIndexStateEnum.Unknown_Zero;

        using (var readerIndex = new BinaryReader(File.Open(_containersIndexFile!.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
        {
            // Skip version and count.
            readerIndex.BaseStream.Seek(CONTAINERSINDEX_OFFSET_DYNAMIC, SeekOrigin.Begin);
            gameIdentifier = readerIndex.ReadBytes(readerIndex.ReadInt32() * 2).GetUnicode();

            // Skip timestamp.
            readerIndex.BaseStream.Seek(0x8, SeekOrigin.Current);
            state = readerIndex.ReadInt32().DenumerateTo<MicrosoftIndexStateEnum>();

            accountIdentifier = readerIndex.ReadBytes(readerIndex.ReadInt32() * 2).GetUnicode();
        }

        using (var writer = new BinaryWriter(new MemoryStream(bytes)))
        {
            writer.Write(CONTAINERSINDEX_HEADER);
            writer.Write(count);

            writer.Write(gameIdentifier.Length);
            writer.Write(gameIdentifier.GetUnicodeBytes());

            writer.Write(_lastModifiedTime.ToUniversalTime().ToFileTime());

            writer.Write((state == MicrosoftIndexStateEnum.Synced ? MicrosoftIndexStateEnum.Modified : state).Numerate());

            writer.Write(accountIdentifier.Length);
            writer.Write(accountIdentifier.GetUnicodeBytes());

            writer.Write(CONTAINERSINDEX_UNKNOWN_CONST);

            if (hasAccountData)
            {
                for (var i = 0; i < 2; i++)
                {
                    writer.Write(AccountContainer!.Identifier.Length);
                    writer.Write(AccountContainer!.Identifier.GetUnicodeBytes());
                }

                writer.Write(AccountContainer!.Microsoft!.SyncHex.Length);
                writer.Write(AccountContainer!.Microsoft.SyncHex.GetUnicodeBytes());

                writer.Write(AccountContainer!.Microsoft.Extension);

                writer.Write(AccountContainer!.Microsoft.State.Numerate());

                writer.Write(AccountContainer!.Microsoft.BlobGuid.ToByteArray());

                writer.Write(AccountContainer!.LastWriteTime.ToUniversalTime().ToFileTime());

                writer.Write(0L);

                writer.Write(AccountContainer!.DataFile!.Length + AccountContainer!.MetaFile!.Length);
            }

            if (hasSettings)
            {
                var identifier = "Settings";

                for (var i = 0; i < 2; i++)
                {
                    writer.Write(identifier.Length);
                    writer.Write(identifier.GetUnicodeBytes());
                }

                writer.Write(_settingsContainer!.SyncHex.Length);
                writer.Write(_settingsContainer!.SyncHex.GetUnicodeBytes());

                writer.Write(_settingsContainer!.Extension);

                writer.Write(_settingsContainer!.State.Numerate());

                writer.Write(_settingsContainer!.BlobGuid.ToByteArray());

                writer.Write(_settingsContainer!.LastModifiedTime.ToUniversalTime().ToFileTime());

                writer.Write(0L);

                writer.Write(_settingsContainer!.BlobDataFile!.Length + _settingsContainer!.BlobMetaFile!.Length);
            }

            foreach (var container in collection)
            {
                for (var i = 0; i < 2; i++)
                {
                    writer.Write(container.Identifier.Length);
                    writer.Write(container.Identifier.GetUnicodeBytes());
                }

                writer.Write(container.Microsoft!.SyncHex.Length);
                writer.Write(container.Microsoft!.SyncHex.GetUnicodeBytes());

                writer.Write(container.Microsoft!.Extension);

                writer.Write(container.Microsoft!.State.Numerate());

                writer.Write(container.Microsoft!.BlobGuid.ToByteArray());

                writer.Write(container.LastWriteTime.ToUniversalTime().ToFileTime());

                writer.Write(0L);

                // Make sure to get the latest data.
                container.DataFile?.Refresh();
                container.MetaFile?.Refresh();

                writer.Write((container.DataFile?.Length ?? 0) + (container.MetaFile?.Length ?? 0));
            }

            bytes = bytes.Take((int)(writer.BaseStream.Position)).ToArray();
        }

        // Write the generated content while the watcher is disabled.
        DisableWatcher();

        File.Delete(_containersIndexFile!.FullName);

        var tIndex = GetTemporaryName(_containersIndexFile!.Name);
        File.WriteAllBytes(tIndex, bytes);
        File.Move(tIndex, _containersIndexFile!.FullName);

        EnableWatcher();
    }

    protected override byte[] CompressData(Container container, byte[] data)
    {
        _ = LZ4_Encode(data, out byte[] target);
        return target;
    }

    protected override void WriteData(Container container, byte[] data)
    {
        var t = GetTemporaryName(container.DataFile!.Name, container);
        File.WriteAllBytes(t, data);
        File.Move(t, container.DataFile!.FullName);
        container.DataFile!.Refresh();
    }

    protected override byte[] CreateMeta(Container container, byte[] data, int decompressedSize)
    {
        //  0. SAVE VERSION      (  4)
        //  1. GAME MODE         (  2)
        //  1. SEASON            (  2)
        //  2. TOTAL PLAY TIME   (  8)
        //  3. DECOMPRESSED SIZE (  4)
        //  4. UNKNOWN           (  4)
        //                       ( 24)

        //  4. UNKNOWN           (260) // Waypoint
        //                       (280)

        // Use default size if tail is not set.
        var bufferSize = container.Microsoft!.MetaTail is not null ? (META_KNOWN + container.Microsoft!.MetaTail!.Length) : (container.IsWaypoint ? META_SIZE_WAYPOINT : META_SIZE);
        var buffer = new byte[bufferSize];

        if (container.MetaIndex == 0)
        {
            using var writer = new BinaryWriter(new MemoryStream(buffer));

            // Always 1.
            writer.Write(1); // 4

            // GAME MODE/SEASON and TOTAL PLAY TIME not used.
            writer.Seek(0x10, SeekOrigin.Begin); // 16

            writer.Write(decompressedSize); // 4
        }
        else
        {
            using var writer = new BinaryWriter(new MemoryStream(buffer));

            writer.Write(container.BaseVersion); // 4

            writer.Write((ushort)(container.GameModeEnum ?? 0)); // 2
            writer.Write((ushort)(container.SeasonEnum)); // 2

            writer.Write(container.TotalPlayTime); // 4

            writer.Write(decompressedSize); // 4
            writer.Write(container.Microsoft!.MetaTail ?? Array.Empty<byte>()); // 4 or 260
        }

        return EncryptMeta(container, data, CompressMeta(container, data, buffer));
    }

    protected override void WriteMeta(Container container, byte[] meta)
    {
        var t = GetTemporaryName(container.MetaFile!.Name, container);
        File.WriteAllBytes(t, meta);
        File.Move(t, container.MetaFile!.FullName);
        container.MetaFile!.Refresh();
    }

    #endregion
}
