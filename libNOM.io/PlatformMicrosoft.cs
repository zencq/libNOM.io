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

    internal string SyncHex = string.Empty;

    internal MicrosoftSyncFlagEnum SyncFlag;

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

    internal override string DirectoryPathPattern { get; } = "*_*";

    internal override string[] AnchorFileGlob { get; } = new[] { "containers.index" };

    internal override Regex[] AnchorFileRegex { get; } = new Regex[] { new("containers\\.index", RegexOptions.Compiled) };
}

#endregion

public class PlatformMicrosoft : Platform
{
    #region Constant

    private const string CONTAINERSINDEX_GAME_IDENTIFIER = "HelloGames.NoMansSky_bs190hzg1sesy!NoMansSky";
    private const uint CONTAINERSINDEX_HEADER = 0xEU; // 14
    private const string CONTAINERSINDEX_NAME = "containers.index";
    private const int CONTAINERSINDEX_OFFSET_CONTAINER = 0xC8; // 200
    private const int CONTAINERSINDEX_OFFSET_COUNT = 0x4; // 4
    private const int CONTAINERSINDEX_OFFSET_GLOBAL_TIMESTAMP = 0x68; // 104

    private const uint CONTAINER_BLOB_COUNT = 0x2U; // 2
    private const int CONTAINER_BLOB_IDENTIFIER_LENGTH = 0x80; // 128
    private const uint CONTAINER_BLOB_SIZE = 0xA0U; // 160
    private const uint CONTAINER_HEADER = 0x4U; // 4
    private const int CONTAINER_OFFSET_DATA = 0x98; // 152
    private const int CONTAINER_OFFSET_META = 0x138; // 312
    private const uint CONTAINER_SIZE = 0x4U + 0x4U + CONTAINER_BLOB_COUNT * (0x80U + 2 * 0x10U); // 328

    private const uint META_SIZE = 0x18U; // 24

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
            catch (FormatException) { }
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
                        SyncFlag = MicrosoftSyncFlagEnum.Created,
                        LastModifiedTime = Source.Microsoft.LastModifiedTime,
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

            container.Microsoft.SyncFlag = MicrosoftSyncFlagEnum.Deleted;

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

    #region Read

    #region Build

    /// <summary>
    /// Builds a <see cref="Container"/> for each blob in the containers.index file.
    /// </summary>
    /// <returns></returns>
    protected override IEnumerable<Container> BuildContainerCollection()
    {
        var bag = new ConcurrentBag<Container>();
        var tasks = new Task[COUNT_SLOTS * COUNT_SAVES_PER_SLOT + 1];

        var microsoft = ReadContainersIndex();
        if (microsoft.Count == 0)
            return bag;

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

                tasks[containerIndex] = Task.Run(() =>
                {
                    var container = BuildContainer(metaIndex, extra);
                    LoadBackupCollection(container);
                    bag.Add(container);
                });
            }
        }
        if (microsoft.ContainsKey(0))
        {
#if NET47_OR_GREATER || NETSTANDARD2_0_OR_GREATER
            tasks[tasks.Length - 1] = Task.Run(() => AccountContainer = BuildContainer(0, microsoft[0]));
#elif NET5_0_OR_GREATER
            tasks[^1] = Task.Run(() => AccountContainer = BuildContainer(0, microsoft[0]));
#endif
        }

        Task.WaitAll(tasks);

        _init = false;
        return bag;
    }

    /// <summary>
    /// Reads the containers.index file to get information where each blob is.
    /// </summary>
    /// <returns></returns>
    private Dictionary<int, MicrosoftContainer> ReadContainersIndex()
    {
        /** containers.index structure

        containers.index data
         0. HEADER (14)                     (  4)
         1. NUMBER OF BLOB CONTAINERS       (  8)
         2. GAME IDENTIFIER LENGTH (44)     (  4)
         3. GAME IDENTIFIER                 ( 88) (UTF-16)
         4. LAST MODIFIED TIME              (  8)
         5. ?                               (  4)
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
        17. SYNC FLAG                       (  4) (0 = ?, 1 = SYNCED, 2 = MODIFIED, 3 = DELETED, 4 = ?, 5 = CREATED)
        18. DIRECTORY                       ( 16) (GUID)
        19. LAST MODIFIED TIME              (  8)
        20. ?                               (  8)
        21. TOTAL SIZE OF FILES             (  8) (BLOB CONTAINER EXCLUDED)
        */

        var result = new Dictionary<int, MicrosoftContainer>();

        using var readerIndex = new BinaryReader(File.Open(_containersIndexFile!.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));

        if (readerIndex.BaseStream.Length < CONTAINERSINDEX_OFFSET_CONTAINER)
            return result;

        if (readerIndex.ReadInt32() != CONTAINERSINDEX_HEADER)
            return result;

        // Total number of blob containers in the containers.index file.
        var containerCount = readerIndex.ReadInt64();

        // Read length of the identifier and then the identifier itself.
        if (readerIndex.ReadBytes(readerIndex.ReadInt32() * 2).GetUnicode() != CONTAINERSINDEX_GAME_IDENTIFIER)
            return result;

        _lastModifiedTime = DateTimeOffset.FromFileTime(readerIndex.ReadInt64()).ToLocalTime();

        readerIndex.BaseStream.Seek(88, SeekOrigin.Current); // unknown data (4) + account identifier (4 + 2 * 36) + unknown data (8)
        if (readerIndex.BaseStream.Position != CONTAINERSINDEX_OFFSET_CONTAINER)
            return result;

        for (var i = 0; i < containerCount; i++)
        {
            var container = new MicrosoftContainer();

            // Read length of the identifier and then the identifier itself. Repeats itself.
            var saveIdentifier = readerIndex.ReadBytes(readerIndex.ReadInt32() * 2).GetUnicode();
            if (saveIdentifier != readerIndex.ReadBytes(readerIndex.ReadInt32() * 2).GetUnicode())
                continue;

            container.SyncHex = readerIndex.ReadBytes(readerIndex.ReadInt32() * 2).GetUnicode();
            container.Extension = readerIndex.ReadByte();
            container.SyncFlag = readerIndex.ReadInt32().DenumerateTo<MicrosoftSyncFlagEnum>();
            container.BlobGuid = readerIndex.ReadBytes(0x10).GetGuid();
            container.BlobDirectory = new DirectoryInfo(System.IO.Path.Combine(Location!.FullName, container.BlobGuid.ToPath()));
            container.LastModifiedTime = DateTimeOffset.FromFileTime(readerIndex.ReadInt64()).ToLocalTime();

            readerIndex.BaseStream.Seek(8, SeekOrigin.Current); // unknown data

            container.TotalSize = readerIndex.ReadUInt64();

            if (container.SyncFlag != MicrosoftSyncFlagEnum.Deleted)
            {
                var directory = System.IO.Path.Combine(container.BlobDirectory.FullName, $"container.{container.Extension}");

                using var readerBlob = new BinaryReader(File.Open(directory, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));

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

                    var file = new FileInfo(System.IO.Path.Combine(container.BlobDirectory.FullName, guid.ToPath()));

                    if (blobIdentifier == "data")
                    {
                        container.BlobDataFile = file;
                        continue;
                    }

                    if (blobIdentifier == "meta")
                    {
                        container.BlobMetaFile = file;
                        continue;
                    }
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

#if NET47_OR_GREATER || NETSTANDARD2_0_OR_GREATER
        foreach (var (Source, Destination) in sourceTransferData.Containers.Zip(destinationContainers, (Source, Destination) => (Source, Destination)))
#elif NET5_0_OR_GREATER
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
                        SyncFlag = MicrosoftSyncFlagEnum.Created,
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

                // VersionEnum must be updated to determine what needs to be transferred after setting the jsonObject in Destination.
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
        if (Settings.LastWriteTime)
        {
            _lastModifiedTime = DateTimeOffset.Now.LocalDateTime;

            writeTime = writeTime.Equals(default) ? _lastModifiedTime : writeTime;
            var ticks = writeTime.Ticks % (long)(Math.Pow(10, 4)) * -1;

            base.Write(container, writeTime: writeTime.AddTicks(ticks));
        }
        else
        {
            base.Write(container, writeTime: writeTime);
        }

        WriteBlobContainer(container);
        WriteContainersIndex();
    }

    /// <summary>
    /// Writes to updated blob container to disk.
    /// </summary>
    /// <param name="container"></param>
    private static void WriteBlobContainer(Container container)
    {
        // Generate.
        var dataGuid = Guid.NewGuid();
        var metaGuid = Guid.NewGuid();

        // Combine.
        var dataPath = Path.Combine(container.DataFile!.Directory!.FullName, dataGuid.ToPath());
        var metaPath = Path.Combine(container.MetaFile!.Directory!.FullName, metaGuid.ToPath());

        // Rename File.
#if NET47_OR_GREATER || NETSTANDARD2_0_OR_GREATER
        File.Move(container.DataFile.FullName, dataPath);
        File.Move(container.MetaFile.FullName, metaPath);
#elif NET5_0_OR_GREATER
        File.Move(container.DataFile.FullName, dataPath, true);
        File.Move(container.MetaFile.FullName, metaPath, true);
#endif

        // Update Container.
        container.DataFile = new FileInfo(dataPath);
        container.MetaFile = new FileInfo(metaPath);

        var directory = Path.Combine(container.Microsoft!.BlobDirectory.FullName, $"container.{container.Microsoft.Extension}");
        using (var writerBlob = new BinaryWriter(File.Open(directory, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite)))
        {
            writerBlob.Seek(CONTAINER_OFFSET_DATA, SeekOrigin.Begin);
            writerBlob.Write(dataGuid.ToByteArray());

            writerBlob.Seek(CONTAINER_OFFSET_META, SeekOrigin.Begin);
            writerBlob.Write(metaGuid.ToByteArray());
        }

        if (container.Microsoft.SyncFlag == MicrosoftSyncFlagEnum.Synced)
        {
            container.Microsoft.SyncFlag = MicrosoftSyncFlagEnum.Modified;
        }

        // Rename Blob Container.
        container.Microsoft.Extension = (byte)(container.Microsoft.Extension == byte.MaxValue ? 1 : container.Microsoft.Extension + 1);
#if NET47_OR_GREATER || NETSTANDARD2_0_OR_GREATER
        File.Move(directory, Path.Combine(container.Microsoft.BlobDirectory.FullName, $"container.{container.Microsoft.Extension}"));
#elif NET5_0_OR_GREATER
        File.Move(directory, Path.Combine(container.Microsoft.BlobDirectory.FullName, $"container.{container.Microsoft.Extension}"), true);
#endif
    }

    /// <summary>
    /// Writes the updated containers.index to disk.
    /// </summary>
    private void WriteContainersIndex()
    {
        DisableWatcher();

        var hasAccountData = AccountContainer is not null;
        var hasSettings = _settingsContainer is not null;

        var collection = ContainerCollection.Where(c => c.Microsoft?.SyncFlag > MicrosoftSyncFlagEnum.Unknown_Zero);
        var count = collection.Count() + (hasAccountData ? 1 : 0) + (hasSettings ? 1 : 0);

        var buffer = new byte[CONTAINERSINDEX_OFFSET_CONTAINER + (count * 0x90)];

        using (var readerIndex = new BinaryReader(File.Open(_containersIndexFile!.FullName, FileMode.Open, FileAccess.Read, FileShare.Read)))
        {
            readerIndex.Read(buffer, 0, CONTAINERSINDEX_OFFSET_CONTAINER);
        }

        using (var writer = new BinaryWriter(new MemoryStream(buffer)))
        {
            writer.Seek(CONTAINERSINDEX_OFFSET_COUNT, SeekOrigin.Begin);
            writer.Write(count);

            writer.Seek(CONTAINERSINDEX_OFFSET_GLOBAL_TIMESTAMP, SeekOrigin.Begin);
            writer.Write(_lastModifiedTime.ToUniversalTime().ToFileTime());

            writer.Seek(CONTAINERSINDEX_OFFSET_CONTAINER, SeekOrigin.Begin);

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

                writer.Write(AccountContainer!.Microsoft.SyncFlag.Numerate());

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

                writer.Write(_settingsContainer!.SyncFlag.Numerate());

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

                writer.Write(container.Microsoft!.SyncFlag.Numerate());

                writer.Write(container.Microsoft!.BlobGuid.ToByteArray());

                writer.Write(container.LastWriteTime.ToUniversalTime().ToFileTime());

                writer.Write(0L);

                writer.Write(container.DataFile!.Length + container.DataFile!.Length);
            }

            buffer = buffer.Take((int)(writer.BaseStream.Position)).ToArray();
        }

        File.WriteAllBytes(_containersIndexFile.FullName, buffer);

        EnableWatcher();
    }

    protected override byte[] CompressData(Container container, byte[] data)
    {
        _ = LZ4_Encode(data, out byte[] target);
        return target;
    }

    protected override byte[] CreateMeta(Container container, byte[] data, int decompressedSize)
    {
        // 0. SAVE VERSION      ( 4)
        // 1. GAME MODE         ( 4)
        // 2. TOTAL PLAY TIME   ( 8)
        // 3. DECOMPRESSED SIZE ( 4)
        // 4. UNKNOWN           ( 4)
        //                      (24)

        var buffer = new byte[META_SIZE];

        if (container.MetaIndex == 0)
        {
            using var writer = new BinaryWriter(new MemoryStream(buffer));

            // Always 1.
            writer.Write(1); // 4 >> 1

            // GAME MODE and TOTAL PLAY TIME not used.
            writer.Seek(0xC, SeekOrigin.Current); // 12

            writer.Write(decompressedSize); // 4 >> 1
        }
        else
        {
            using var writer = new BinaryWriter(new MemoryStream(buffer));

            writer.Write(container.BaseVersion); // 4 >> 1
            writer.Write((ushort)(container.GameModeEnum)); // 2 >> 0.5
            writer.Write((ushort)(container.SeasonEnum)); // 2 >> 0.5
            writer.Write(container.TotalPlayTime); // 8 >> 2
            writer.Write(decompressedSize); // 4 >> 1
        }

        return EncryptMeta(container, data, CompressMeta(container, data, buffer));
    }

    #endregion
}
