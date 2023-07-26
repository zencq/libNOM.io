using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace libNOM.io;


#region Extra

internal record class PlatformExtraMicrosoft
{
    internal FileInfo BlobContainerFile => new(Path.Combine(BlobDirectory.FullName, $"container.{Extension}"));

    internal DirectoryInfo BlobDirectory = null!;

    internal Guid BlobDirectoryGuid;

    internal FileInfo BlobDataFile = null!;

    internal FileInfo BlobMetaFile = null!;

    internal byte Extension;

    internal DateTimeOffset LastWriteTime;

    internal byte[]? MetaTail;

    internal string SyncHex = string.Empty;

    internal MicrosoftBlobSyncStateEnum State;

    internal ulong TotalSize;
}

public partial class Container
{
    internal PlatformExtraMicrosoft? Microsoft { get; set; }

    internal void SetLastWriteTimeMicrosoft(DateTimeOffset value)
    {
        if (Microsoft is not null)
        {
            Microsoft.LastWriteTime = value;
        }
    }

    internal void RefreshFileInfoMicrosoft()
    {
        if (Microsoft is not null)
        {
            Microsoft.BlobContainerFile?.Refresh();
            Microsoft.BlobDirectory?.Refresh();
            Microsoft.BlobDataFile?.Refresh();
            Microsoft.BlobMetaFile?.Refresh();
        }
    }
}

#endregion

public partial class PlatformMicrosoft : Platform
{
    #region Constant

    #region Platform Specific

    private const int CONTAINERSINDEX_HEADER = 0xE; // 14
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

    #region Directory Data

    public const string ACCOUNT_PATTERN = "*_29070100B936489ABCE8B9AF3980429C";
    public static readonly string[] ANCHOR_FILE_GLOB = new[] { "containers.index" };
#if NETSTANDARD2_0_OR_GREATER || NET6_0
    public static readonly Regex[] ANCHOR_FILE_REGEX = new Regex[] { AnchorFileRegex0! };
#else
    public static readonly Regex[] ANCHOR_FILE_REGEX = new Regex[] { AnchorFileRegex0() };
#endif
    public static readonly string PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", "HelloGames.NoMansSky_bs190hzg1sesy", "SystemAppData", "wgs");

    #endregion

    #region Generated Regex

#if NETSTANDARD2_0_OR_GREATER || NET6_0
    private static readonly Regex AnchorFileRegex0 = new("containers\\.index", RegexOptions.Compiled);
#else
    [GeneratedRegex("containers\\.index", RegexOptions.Compiled)]
    private static partial Regex AnchorFileRegex0();
#endif

    #endregion

    #endregion

    #region Field

    private string? _accountId;
    private FileInfo? _containersIndexFile;
    private DateTimeOffset _lastWriteTime;
    private PlatformExtraMicrosoft? _settingsContainer;

    #endregion

    #region Property

    #region Flags

    public override bool CanCreate { get; } = true;

    public override bool CanRead { get; } = true;

    public override bool CanUpdate { get; } = true;

    public override bool CanDelete { get; } = true;

    public override bool HasModding { get; } = false;

    public override bool IsPersonalComputerPlatform { get; } = true;

    public override bool RestartToApply { get; } = true;

    #endregion

    #region Platform Indicator

    protected override string[] PlatformAnchorFileGlob { get; } = ANCHOR_FILE_GLOB;

    protected override Regex[] PlatformAnchorFileRegex { get; } = ANCHOR_FILE_REGEX;

    protected override string? PlatformArchitecture { get; } = "XB1|Final";

    public override PlatformEnum PlatformEnum { get; } = PlatformEnum.Microsoft;

    // Looks like "C:\\Program Files\\WindowsApps\\HelloGames.NoMansSky_4.38.0.0_x64__bs190hzg1sesy\\Binaries\\NMS.exe"
    protected override string? PlatformProcess { get; } = @"bs190hzg1sesy\Binaries\NMS.exe";

    protected override string PlatformToken { get; } = "XB";

    #endregion

    #endregion

    #region Getter

    #region Container

    protected override IEnumerable<Container> GetCacheEvictionContainers(string name)
    {
        if (!name.Equals("containers.index", StringComparison.OrdinalIgnoreCase))
            return Array.Empty<Container>();

        RefreshContainerCollection();

        // Get last written container via timestamp.
        var lastWriteTicks = _lastWriteTime.Ticks - _lastWriteTime.Ticks % (long)(Math.Pow(10, 4));
        var containersLastWrite = SaveContainerCollection.Where(c => c.Exists && c.LastWriteTime.Ticks == lastWriteTicks);

        // In case last written via timestamp returns nothing, fall back to all existing.
        return containersLastWrite.Any() ? containersLastWrite : GetExistingContainers();
    }

    #endregion

    private static string GetBlobContainerPath(PlatformExtraMicrosoft microsoft) => GetBlobContainerPath(microsoft, microsoft.Extension);

    private static string GetBlobContainerPath(PlatformExtraMicrosoft microsoft, byte extension)
    {
        return Path.Combine(microsoft!.BlobDirectory.FullName, $"container.{extension}");
    }

    private static FileInfo GetBlobFileInfo(PlatformExtraMicrosoft microsoft, Guid guid)
    {
        return new(Path.Combine(microsoft.BlobDirectory.FullName, guid.ToPath()));
    }

    private string GetTemporaryAccountFile(string fileToWrite)
    {
        return Path.Combine(PATH, "t", $"{_containersIndexFile!.DirectoryName}_{fileToWrite}");
    }

    private string GetTemporaryBlobFile(string fileToWrite, Container container) => GetTemporaryAccountFile($"{container.Microsoft!.BlobDirectory.Name}_{fileToWrite}");

    #endregion

    // //

    #region Constructor

    public PlatformMicrosoft(string path) : base(path) { }

    public PlatformMicrosoft(string path, PlatformSettings platformSettings) : base(path, platformSettings) { }

    public PlatformMicrosoft(DirectoryInfo directory) : base(directory) { }

    public PlatformMicrosoft(DirectoryInfo directory, PlatformSettings platformSettings) : base(directory, platformSettings) { }

    protected override void InitializeComponent(DirectoryInfo? directory, PlatformSettings? platformSettings)
    {
        // Proceed to base method even if no directory.
        if (directory is not null)
        {
            try
            {
                _accountId = System.Convert.ToInt64(directory.Name.Split('_')[0], 16).ToString();
            }
            catch (Exception ex) when (ex is FormatException or OverflowException)
            {
                // Nothing to do.
            }
            _containersIndexFile = new FileInfo(Path.Combine(directory.FullName, "containers.index"));
        }

        base.InitializeComponent(directory, platformSettings);
    }

    #endregion

    // // Read / Write

    #region Generate

    protected override IEnumerable<Container> GenerateContainerCollection()
    {
        var microsoft = ParseContainersIndex();
        if (microsoft.Count == 0)
            return Array.Empty<Container>();

        var bag = new ConcurrentBag<Container>();

        var tasks = Enumerable.Range(0, Globals.Constants.OFFSET_INDEX + COUNT_SAVES_TOTAL).Select((metaIndex) =>
        {
            return Task.Run(() =>
            {
                _ = microsoft.TryGetValue(metaIndex, out var extra);
                if (metaIndex == 0)
                {
                    AccountContainer = CreateContainer(metaIndex, extra);
                    BuildContainerFull(AccountContainer);
                }
                else if (metaIndex == 1)
                {
                    if (extra is not null)
                        _settingsContainer = microsoft[metaIndex];
                }
                else
                {
                    var container = CreateContainer(metaIndex, extra);
                    if (Settings.LoadingStrategy < LoadingStrategyEnum.Full)
                    {
                        BuildContainerHollow(container);
                    }
                    else
                    {
                        BuildContainerFull(container);
                    }
                    GenerateBackupCollection(container);
                    bag.Add(container);
                }
            });
        });
        Task.WaitAll(tasks.ToArray());

        return bag;
    }

    /// <summary>
    /// Reads the containers.index file to get information where each blob is.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidDataException"/>
    private Dictionary<int, PlatformExtraMicrosoft> ParseContainersIndex()
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
         8. UNKNOWN                         (  8)
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
        20. UNKNOWN                         (  8)
        21. TOTAL SIZE OF FILES             (  8) (BLOB CONTAINER EXCLUDED)
        */

        var result = new Dictionary<int, PlatformExtraMicrosoft>();

        using var readerIndex = new BinaryReader(File.Open(_containersIndexFile!.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));

        if (readerIndex.ReadInt32() is int version && version != CONTAINERSINDEX_HEADER)
            throw new InvalidDataException($"Wrong version of containers.index file! Expected {CONTAINERSINDEX_HEADER} but got {version}.");

        // Total number of blob containers in the containers.index file.
        var containerCount = readerIndex.ReadInt64();

        // Read length of the identifier and then the identifier itself.
        readerIndex.BaseStream.Seek(readerIndex.ReadInt32() * 2, SeekOrigin.Current);

        // Store timestamp.
        _lastWriteTime = DateTimeOffset.FromFileTime(readerIndex.ReadInt64()).ToLocalTime();

        // Skip containers.index state enum.
        readerIndex.BaseStream.Seek(0x4, SeekOrigin.Current);

        // Skip account identifier and unknown data (0x8) at the end of the header.
        readerIndex.BaseStream.Seek(readerIndex.ReadInt32() * 2 + 0x8, SeekOrigin.Current);

        for (var i = 0; i < containerCount; i++)
        {
            var container = new PlatformExtraMicrosoft();

            // Read length of the identifier and then the identifier itself. Repeats itself.
            var saveIdentifier = readerIndex.ReadBytes(readerIndex.ReadInt32() * 2).GetUnicode();
            if (saveIdentifier != readerIndex.ReadBytes(readerIndex.ReadInt32() * 2).GetUnicode())
                continue;

            container.SyncHex = readerIndex.ReadBytes(readerIndex.ReadInt32() * 2).GetUnicode();
            container.Extension = readerIndex.ReadByte();
            container.State = (MicrosoftBlobSyncStateEnum)(readerIndex.ReadInt32());
            container.BlobDirectoryGuid = readerIndex.ReadBytes(0x10).GetGuid();
            container.BlobDirectory = new DirectoryInfo(Path.Combine(Location!.FullName, container.BlobDirectoryGuid.ToPath()));
            container.LastWriteTime = DateTimeOffset.FromFileTime(readerIndex.ReadInt64()).ToLocalTime();

            readerIndex.BaseStream.Seek(0x8, SeekOrigin.Current); // unknown data

            container.TotalSize = readerIndex.ReadUInt64();

            // Ignore if already marked as deleted.
            if (container.BlobDirectory.Exists && container.State != MicrosoftBlobSyncStateEnum.Deleted)
            {
                var blobContainerPath = GetBlobContainerPath(container);
                var fileInfos = new HashSet<FileInfo>();

                // In case the blob container extension does not match the containers.index value, try all existing ones until a data file is found.
                if (File.Exists(blobContainerPath))
                {
                    fileInfos.Add(new(blobContainerPath));
                }
                else
                {
                    foreach (var file in container.BlobDirectory.GetFiles("container.*"))
                        fileInfos.Add(file);
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

                        var blobFile = GetBlobFileInfo(container, guid);

                        if (blobIdentifier == "data")
                        {
                            container.BlobDataFile = blobFile;
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
                        container.Extension = System.Convert.ToByte(file.Extension.Substring(1));
                        break;
                    }
                }

                // Mark as deleted if there is no existing data file.
                if (container.BlobDataFile?.Exists != true)
                    container.State = MicrosoftBlobSyncStateEnum.Deleted;
            }

            // Store collected data for further processing.
            if (saveIdentifier.StartsWith("Slot"))
            {
                var isManual = System.Convert.ToInt32(saveIdentifier.EndsWith("Manual"));
                var slot = System.Convert.ToInt32(string.Concat(saveIdentifier.Where(char.IsDigit)));

                var metaIndex = Globals.Constants.OFFSET_INDEX + ((slot - 1) * 2) + isManual;

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

    protected override Container CreateContainer(int metaIndex, object? extra)
    {
        if (extra is not PlatformExtraMicrosoft microsoft)
            return new Container(metaIndex);

        return new Container(metaIndex)
        {
            DataFile = microsoft.BlobDataFile,
            LastWriteTime = microsoft.LastWriteTime,
            MetaFile = microsoft.BlobMetaFile,
            Microsoft = microsoft,
        };
    }

    #endregion

    #region Load

    protected override byte[] LoadContainer(Container container)
    {
        // Any incompatibility will be set again while loading.
        container.ClearIncompatibility();

        if (container.Exists)
        {
            var meta = LoadMeta(container);
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
        else if (container.Microsoft is not null && container.Microsoft.State == MicrosoftBlobSyncStateEnum.Deleted)
        {
            container.IncompatibilityTag = Globals.Constants.INCOMPATIBILITY_004;
        }
        else if (container.Microsoft is not null && !container.Microsoft.BlobDirectory.Exists)
        {
            container.IncompatibilityTag = Globals.Constants.INCOMPATIBILITY_005;
        }

        container.IncompatibilityTag ??= Globals.Constants.INCOMPATIBILITY_006;
        return Array.Empty<byte>();
    }

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
        _ = Globals.LZ4.Decode(data, out byte[] target, length);
        return target;
    }

    #endregion

    #region Write

    public override void Write(Container container, DateTimeOffset writeTime)
    {
        if (!CanUpdate || !container.IsLoaded)
            return;

        DisableWatcher();

        // Writing all Microsoft Store files at once in the same way as the game itself does.
        if (Settings.WriteAlways || !container.IsSynced || Settings.SetLastWriteTime)
        {
            if (Settings.WriteAlways || !container.IsSynced)
            {
                container.Exists = true;
                container.IsSynced = true;

                var (data, decompressedSize) = CreateData(container);
                var meta = CreateMeta(container, data, decompressedSize);

                // Cache original file information.
                var oldContainerExtension = container.Microsoft?.Extension;
                var oldData = container.DataFile?.FullName;
                var oldMeta = container.MetaFile?.FullName;

                // Update blob with new file information.
                var blob = CreateBlob(container);

                // Write the previously created files.
                WriteMeta(container, meta);
                if (oldMeta is not null)
                    File.Delete(oldMeta);

                WriteData(container, data);
                if (oldData is not null)
                    File.Delete(oldData);

                WriteBlob(container, blob);
                if (oldContainerExtension is not null)
                    File.Delete(GetBlobContainerPath(container.Microsoft!, oldContainerExtension.Value));
            }

            if (Settings.SetLastWriteTime)
            {
                _lastWriteTime = writeTime;

                var ticks = writeTime.Ticks % (long)(Math.Pow(10, 4)) * -1; // get last four digits negative
                container.LastWriteTime = writeTime.AddTicks(ticks); // set container time without the ticks

                if (container.DataFile is not null)
                {
                    File.SetCreationTime(container.DataFile.FullName, container.LastWriteTime.LocalDateTime);
                    File.SetLastWriteTime(container.DataFile.FullName, container.LastWriteTime.LocalDateTime);
                }

                if (container.MetaFile is not null)
                {
                    File.SetCreationTime(container.MetaFile.FullName, container.LastWriteTime.LocalDateTime);
                    File.SetLastWriteTime(container.MetaFile.FullName, container.LastWriteTime.LocalDateTime);
                }
            }

            // Finally write the containers.index file.
            WriteContainersIndex();
        }

        EnableWatcher();

        // Always refresh in case something above was executed.
        container.RefreshFileInfo();
        container.WriteCallback.Invoke();
    }

    protected override byte[] CompressData(Container container, byte[] data)
    {
        _ = Globals.LZ4.Encode(data, out byte[] target);
        return target;
    }

    protected override void WriteData(Container container, byte[] data)
    {
        var t = GetTemporaryBlobFile(container.DataFile!.Name, container);
        File.WriteAllBytes(t, data);
        File.Move(t, container.DataFile!.FullName);
    }

    protected override byte[] CreateMeta(Container container, byte[] data, int decompressedSize)
    {
        //  0. SAVE VERSION      (  4)
        //  1. GAME MODE         (  2)
        //  1. SEASON            (  2)
        //  2. TOTAL PLAY TIME   (  8)
        //  4. DECOMPRESSED SIZE (  4)
        //  5. UNKNOWN           (  4) // Foundation
        //                       ( 24)

        //  5. UNKNOWN           (260) // Waypoint
        //                       (280)

        // Use default size if tail is not set.
        var bufferSize = container.Microsoft?.MetaTail is not null ? (META_KNOWN + container.Microsoft!.MetaTail!.Length) : (container.IsWaypoint ? META_SIZE_WAYPOINT : META_SIZE);
        var buffer = new byte[bufferSize];

        using var writer = new BinaryWriter(new MemoryStream(buffer));

        if (container.MetaIndex == 0)
        {
            // Always 1.
            writer.Write(1); // 4

            // GAME MODE/SEASON and TOTAL PLAY TIME not used.
            writer.Seek(0x10, SeekOrigin.Begin); // 16

            writer.Write(decompressedSize); // 4
        }
        else
        {
            writer.Write(container.BaseVersion); // 4

            writer.Write((ushort)(container.GameModeEnum ?? 0)); // 2
            writer.Write((ushort)(container.SeasonEnum)); // 2

            writer.Write(container.TotalPlayTime); // 4

            writer.Write(decompressedSize); // 4
            writer.Write(container.Microsoft?.MetaTail ?? Array.Empty<byte>()); // 4 or 260
        }

        return EncryptMeta(container, data, CompressMeta(container, data, buffer));
    }

    protected override void WriteMeta(Container container, byte[] meta)
    {
        var t = GetTemporaryBlobFile(container.MetaFile!.Name, container);
        File.WriteAllBytes(t, meta);
        File.Move(t, container.MetaFile!.FullName);
    }

    /// <summary>
    /// Updates the data and meta file information for the new writing.
    /// </summary>
    /// <param name="container"></param>
    /// <returns></returns>
    private static byte[] CreateBlob(Container container)
    {
        var dataGuid = Guid.NewGuid();
        var metaGuid = Guid.NewGuid();

        var buffer = File.ReadAllBytes(GetBlobContainerPath(container.Microsoft!));

        // Update blob container content.
        using (var writer = new BinaryWriter(new MemoryStream(buffer)))
        {
            writer.Seek(CONTAINER_OFFSET_DATA, SeekOrigin.Begin);
            writer.Write(dataGuid.ToByteArray());

            writer.Seek(CONTAINER_OFFSET_META, SeekOrigin.Begin);
            writer.Write(metaGuid.ToByteArray());
        }

        // Update library container information.
        container.Microsoft!.BlobDataFile = container.DataFile = GetBlobFileInfo(container.Microsoft!, dataGuid);
        container.Microsoft!.BlobMetaFile = container.MetaFile = GetBlobFileInfo(container.Microsoft!, metaGuid);
        container.Microsoft!.Extension = (byte)(container.Microsoft!.Extension == byte.MaxValue ? 1 : container.Microsoft!.Extension + 1);

        if (container.Microsoft!.State == MicrosoftBlobSyncStateEnum.Synced)
            container.Microsoft!.State = MicrosoftBlobSyncStateEnum.Modified;

        return buffer;
    }

    /// <summary>
    /// Writes the blob container file content to disk.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="blob"></param>
    private void WriteBlob(Container container, byte[] blob)
    {
        var t = GetTemporaryBlobFile($"container.{container.Microsoft!.Extension}", container);
        File.WriteAllBytes(t, blob);
        File.Move(t, GetBlobContainerPath(container.Microsoft!));
    }

    /// <summary>
    /// Creates and writes the containers.index file content to disk.
    /// </summary>
    private void WriteContainersIndex()
    {
        var hasSettings = _settingsContainer is not null;

        var collection = SaveContainerCollection.Where(i => i.Microsoft is not null);
        var count = (long)(collection.Count() + (HasAccountData ? 1 : 0) + (hasSettings ? 1 : 0));

        // Longest name (e.g. Slot10Manual) has a total length of 0x8C and therefore 0x90 is more than enough.
        // Leftover will be cut off by using only data up to the current writer position.
        var buffer = new byte[CONTAINERSINDEX_OFFSET_CONTAINER + (count * 0x90)];

        var accountIdentifier = string.Empty;
        var gameIdentifier = string.Empty;
        var state = MicrosoftIndexSyncStateEnum.Unknown_Zero;

        using (var reader = new BinaryReader(File.Open(_containersIndexFile!.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
        {
            // Skip version and count.
            reader.BaseStream.Seek(CONTAINERSINDEX_OFFSET_DYNAMIC, SeekOrigin.Begin);
            gameIdentifier = reader.ReadBytes(reader.ReadInt32() * 2).GetUnicode();

            // Skip timestamp.
            reader.BaseStream.Seek(0x8, SeekOrigin.Current);
            state = (MicrosoftIndexSyncStateEnum)(reader.ReadInt32());

            accountIdentifier = reader.ReadBytes(reader.ReadInt32() * 2).GetUnicode();
        }

        using (var writer = new BinaryWriter(new MemoryStream(buffer)))
        {
            writer.Write(CONTAINERSINDEX_HEADER);
            writer.Write(count);

            writer.Write(gameIdentifier.Length);
            writer.Write(gameIdentifier.GetUnicodeBytes());

            writer.Write(_lastWriteTime.ToUniversalTime().ToFileTime());

            writer.Write((int)(state == MicrosoftIndexSyncStateEnum.Synced ? MicrosoftIndexSyncStateEnum.Modified : state));

            writer.Write(accountIdentifier.Length);
            writer.Write(accountIdentifier.GetUnicodeBytes());

            writer.Write(CONTAINERSINDEX_UNKNOWN_CONST);

            if (HasAccountData)
            {
                for (var i = 0; i < 2; i++)
                {
                    writer.Write(AccountContainer!.Identifier.Length);
                    writer.Write(AccountContainer!.Identifier.GetUnicodeBytes());
                }

                writer.Write(AccountContainer!.Microsoft!.SyncHex.Length);
                writer.Write(AccountContainer!.Microsoft!.SyncHex.GetUnicodeBytes());

                writer.Write(AccountContainer!.Microsoft!.Extension);

                writer.Write((int)(AccountContainer!.Microsoft!.State));

                writer.Write(AccountContainer!.Microsoft!.BlobDirectoryGuid.ToByteArray());

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

                writer.Write((int)(_settingsContainer!.State));

                writer.Write(_settingsContainer!.BlobDirectoryGuid.ToByteArray());

                writer.Write(_settingsContainer!.LastWriteTime.ToUniversalTime().ToFileTime());

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

                writer.Write((int)(container.Microsoft!.State));

                writer.Write(container.Microsoft!.BlobDirectoryGuid.ToByteArray());

                writer.Write(container.Microsoft!.LastWriteTime.ToUniversalTime().ToFileTime());

                writer.Write(0L);

                // Make sure to get the latest data.
                container.RefreshFileInfo();

                writer.Write((container.DataFile?.Exists == true ? container.DataFile!.Length : 0) + (container.MetaFile?.Exists == true ? container.MetaFile!.Length : 0));
            }

            buffer = buffer.Take((int)(writer.BaseStream.Position)).ToArray();
        }

        // Delete and recreate file.
        File.Delete(_containersIndexFile!.FullName);

        var t = GetTemporaryAccountFile(_containersIndexFile!.Name);
        File.WriteAllBytes(t, buffer);
        File.Move(t, _containersIndexFile!.FullName);
        _containersIndexFile.Refresh();
    }

    #endregion

    // // File Operation

    private void ExecuteCanCreate(Container Destination)
    {
        var directory = new DirectoryInfo(Path.Combine(Location!.FullName, Destination.Microsoft!.BlobDirectoryGuid.ToPath()));

        // Set new dummy files. They will be overwritten while writing.
        Destination.DataFile = new(Path.Combine(directory.FullName, "data.guid"));
        Destination.MetaFile = new(Path.Combine(directory.FullName, "meta.guid"));

        // Update Microsoft container with new directory and file information.
        Destination.Microsoft!.BlobDirectory = directory;
        Destination.Microsoft!.BlobDataFile = Destination.DataFile;
        Destination.Microsoft!.BlobMetaFile = Destination.MetaFile;

        // Prepare blob container file content. Guid of data and meta file will be set while writing.
        var blobContainerBinary = new byte[CONTAINER_SIZE];
        using (var writer = new BinaryWriter(new MemoryStream(blobContainerBinary)))
        {
            writer.Write(CONTAINER_HEADER);
            writer.Write(CONTAINER_BLOB_COUNT);

            var data = "data".GetUnicodeBytes();
            writer.Write(data);

            writer.BaseStream.Seek(CONTAINER_BLOB_SIZE - data.Length, SeekOrigin.Current);

            writer.Write("meta".GetUnicodeBytes());
        }

        Directory.CreateDirectory(Destination.Microsoft!.BlobDirectory.FullName);
        File.WriteAllBytes(Destination.Microsoft!.BlobContainerFile.FullName, blobContainerBinary);
    }

    #region Copy

    protected override void Copy(IEnumerable<(Container Source, Container Destination)> operationData, bool write)
    {
        foreach (var (Source, Destination) in operationData)
        {
            if (!Source.Exists)
            {
                Delete(Destination, write);
            }
            else if (Destination.Exists || (!Destination.Exists && CanCreate))
            {
                if (GuardPlatformExtra(Source))
                    throw new InvalidOperationException("Cannot copy as the source container has no platform extra.");

                if (!Source.IsLoaded)
                    BuildContainerFull(Source);

                if (!Source.IsCompatible)
                    throw new InvalidOperationException($"Cannot copy as the source container is not compatible: {Source.IncompatibilityTag}");

                // Due to this CanCreate can be true.
                if (!Destination.Exists)
                {
                    CopyPlatformExtra(Destination, Source);
                    ExecuteCanCreate(Destination);
                }

                // Faking relevant properties to force it to Write().
                Destination.Exists = true;
                Destination.IsSynced = false;

                // Properties requied to properly build the container below.
                Destination.BaseVersion = Source.BaseVersion;
                Destination.VersionEnum = Source.VersionEnum;
                Destination.SeasonEnum = Source.SeasonEnum;

                Destination.SetJsonObject(Source.GetJsonObject());

                // This "if" is not really useful in this method but properly implemented nonetheless.
                if (write)
                {
                    Write(Destination, writeTime: Source.LastWriteTime);
                    BuildContainerFull(Destination);
                }
            }
            //else
            //    continue;
        }

        UpdateUserIdentification();
    }

    protected override bool GuardPlatformExtra(Container source)
    {
        return source.Microsoft is null;
    }

    protected override void CopyPlatformExtra(Container destination, Container source)
    {
        destination.Microsoft = new PlatformExtraMicrosoft
        {
            BlobDirectoryGuid = Guid.NewGuid(),
            Extension = 0,
            LastWriteTime = source.Microsoft!.LastWriteTime,
            MetaTail = source.Microsoft!.MetaTail,
            State = MicrosoftBlobSyncStateEnum.Created,
        };
    }

    #endregion

    #region Delete

    protected override void Delete(IEnumerable<Container> containers, bool write)
    {
        if (!CanDelete)
            return;

        DisableWatcher();

        foreach (var container in containers)
        {
            if (container.Microsoft is null)
                continue;

            if (write)
            {
                if (container.Microsoft.BlobDirectory.Exists)
                {
                    try
                    {
                        Directory.Delete(container.Microsoft.BlobDirectory.FullName, true);
                    }
                    catch (Exception ex) when (ex is DirectoryNotFoundException or IOException or PathTooLongException or UnauthorizedAccessException)
                    {
                        // Nothing to do.
                    }
                }
            }

            if (Settings.SetLastWriteTime)
            {
                _lastWriteTime = DateTimeOffset.Now.LocalDateTime;

                var ticks = _lastWriteTime.Ticks % (long)(Math.Pow(10, 4)) * -1; // get last four digits negative
                container.LastWriteTime = _lastWriteTime.AddTicks(ticks); // set container time without the ticks
            }

            container.Reset();
            container.IncompatibilityTag = Globals.Constants.INCOMPATIBILITY_004;
            container.Microsoft.State = MicrosoftBlobSyncStateEnum.Deleted;
        }

        // Refresh to get the new offsets.
        if (write)
        {
            WriteContainersIndex();
            RefreshContainerCollection();
        }

        EnableWatcher();
    }

    #endregion

    #region Transfer

    protected override void Transfer(ContainerTransferData sourceTransferData, int destinationSlot, bool write)
    {
        if (!sourceTransferData.UserIdentification.IsComplete() || !PlatformUserIdentification.IsComplete())
            throw new InvalidOperationException("Cannot transfer as at least one user identification is not complete.");

        foreach (var (Source, Destination) in sourceTransferData.Containers.Zip(GetSlotContainers(destinationSlot), (Source, Destination) => (Source, Destination)))
        {
            if (!Source.Exists)
            {
                Delete(Destination, write);
            }
            else if (Destination.Exists || !Destination.Exists && CanCreate)
            {
                if (!Source.IsLoaded)
                    BuildContainerFull(Source);

                if (!Source.IsCompatible)
                    throw new InvalidOperationException($"Cannot transfer as the source container is not compatible: {Source.IncompatibilityTag}");

                // Due to this CanCreate can be true.
                if (!Destination.Exists)
                {
                    CreatePlatformExtra(Destination, Source);
                    ExecuteCanCreate(Destination);
                }

                // Faking relevant properties to force it to Write().
                Destination.Exists = true;
                Destination.IsSynced = false;

                // Properties requied to properly build the container below.
                Destination.BaseVersion = Source.BaseVersion;
                Destination.VersionEnum = Source.VersionEnum;
                Destination.SeasonEnum = Source.SeasonEnum;

                Destination.SetJsonObject(Source.GetJsonObject());
                TransferOwnership(Destination, sourceTransferData);

                if (write)
                {
                    Write(Destination, Source.LastWriteTime);
                    BuildContainerFull(Destination);
                }
            }
            //else
            //    continue;
        }
    }

    protected override void CreatePlatformExtra(Container destination, Container source)
    {
        destination.Microsoft = new PlatformExtraMicrosoft
        {
            BlobDirectoryGuid = Guid.NewGuid(),
            Extension = 0,
            LastWriteTime = source.LastWriteTime,
            MetaTail = new byte[(source.IsWaypoint ? META_SIZE_WAYPOINT : META_SIZE) - META_KNOWN],
            State = MicrosoftBlobSyncStateEnum.Created,
        };
    }

    #endregion

    // // FileSystemWatcher

    #region FileSystemWatcher

    /// <summary>
    /// Refreshes all containers in the collection with newly written data from the containers.index file.
    /// </summary>
    private void RefreshContainerCollection()
    {
        var microsoft = ParseContainersIndex();
        if (microsoft.Count == 0)
            return;

        if (microsoft.ContainsKey(0))
        {
            var extra = AccountContainer!.Microsoft = microsoft[0];

            AccountContainer!.LastWriteTime = extra.LastWriteTime;
            AccountContainer!.DataFile = extra.BlobDataFile;
            AccountContainer!.MetaFile = extra.BlobMetaFile;
        }

        // Just store the possibly updated data.
        if (microsoft.ContainsKey(1))
        {
            _settingsContainer = microsoft[1];
        }

        for (var containerIndex = 0; containerIndex < COUNT_SAVES_TOTAL; containerIndex++)
        {
            var metaIndex = Globals.Constants.OFFSET_INDEX + containerIndex;

            var contains = microsoft.TryGetValue(metaIndex, out var extra);
            if (contains)
            {
                // Cache whether this container exists as next stell will change the result.
                var exists = SaveContainerCollection[containerIndex].Exists;

                // Update same properties that will be set in <see cref="CreateContainer()"/>.
                SaveContainerCollection[containerIndex].Microsoft = extra!;

                SaveContainerCollection[containerIndex].DataFile = extra!.BlobDataFile;
                SaveContainerCollection[containerIndex].LastWriteTime = extra!.LastWriteTime;
                SaveContainerCollection[containerIndex].MetaFile = extra!.BlobMetaFile;

                // Rebuild new container to set its properties.
                if (!exists)
                {
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
            else
            {
                // Delete in memory.
                SaveContainerCollection[containerIndex].Reset();
            }
        }
    }

    #endregion

    // // User Identification

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

    protected override IEnumerable<string> GetUserIdentificationByBase(JObject jsonObject, string key)
    {
        if (_accountId is null)
            return base.GetUserIdentificationByBase(jsonObject, key);

        var usesMapping = jsonObject.UsesMapping();

        var path = usesMapping ? $"PlayerStateData.PersistentPlayerBases[?({{0}})].Owner.{key}" : $"6f=.F?0[?({{0}})].3?K.{key}";
        var expressions = new[]
        {
            usesMapping ? $"@.BaseType.PersistentBaseTypes == '{PersistentBaseTypesEnum.HomePlanetBase}' || @.BaseType.PersistentBaseTypes == '{PersistentBaseTypesEnum.FreighterBase}'" : $"@.peI.DPp == '{PersistentBaseTypesEnum.HomePlanetBase}' || @.peI.DPp == '{PersistentBaseTypesEnum.FreighterBase}'", // only with own base
            usesMapping ? $"@.Owner.UID == '{_accountId}'" : $"@.3?K.K7E == '{_accountId}'", // only with specified value
        };

        return GetUserIdentificationIntersection(jsonObject, path, expressions);
    }

    protected override IEnumerable<string> GetUserIdentificationByDiscovery(JObject jsonObject, string key)
    {
        if (_accountId is null)
            return base.GetUserIdentificationByBase(jsonObject, key);

        var usesMapping = jsonObject.UsesMapping();

        var path = usesMapping ? $"DiscoveryManagerData.DiscoveryData-v1.Store.Record[?({{0}})].OWS.{key}" : $"fDu.ETO.OsQ.?fB[?({{0}})].ksu.{key}";
        var expressions = new[]
        {
            usesMapping ? $"@.OWS.UID == '{_accountId}'" : $"@.ksu.K7E == '{_accountId}'", // only with specified value
        };

        return GetUserIdentificationIntersection(jsonObject, path, expressions);
    }

    protected override IEnumerable<string> GetUserIdentificationBySettlement(JObject jsonObject, string key)
    {
        if (_accountId is null)
            return base.GetUserIdentificationByBase(jsonObject, key);

        var usesMapping = jsonObject.UsesMapping();

        var path = usesMapping ? $"PlayerStateData.SettlementStatesV2[?({{0}})].Owner.{key}" : $"6f=.GQA[?({{0}})].3?K.{key}";
        var expressions = new[]
        {
            usesMapping ? $"@.Owner.UID == '{_accountId}'" : $"@.3?K.K7E == '{_accountId}'", // only with specified value
        };

        return GetUserIdentificationIntersection(jsonObject, path, expressions);
    }

    #endregion
}
