using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace libNOM.io;


public partial class PlatformMicrosoft : Platform
{
    #region Constant

    #region Platform Specific

    protected const int CONTAINERSINDEX_HEADER = 0xE; // 14
    protected const long CONTAINERSINDEX_UNKNOWN_CONST = 0x10000000; // 268,435,456
    protected const int CONTAINERSINDEX_OFFSET_CONTAINER = 0xC8; // 200
    protected const int CONTAINERSINDEX_OFFSET_DYNAMIC = 0xC; // 12

    protected const int CONTAINER_BLOB_COUNT = 0x2; // 2
    protected const int CONTAINER_BLOB_IDENTIFIER_LENGTH = 0x80; // 128
    protected const int CONTAINER_BLOB_SIZE = 0xA0; // 160
    protected const int CONTAINER_HEADER = 0x4; // 4
    protected const int CONTAINER_OFFSET_DATA = 0x98; // 152
    protected const int CONTAINER_OFFSET_META = 0x138; // 312
    protected const int CONTAINER_SIZE = 0x4 + 0x4 + CONTAINER_BLOB_COUNT * (0x80 + 2 * 0x10); // 328

    protected const int META_LENGTH_KNOWN = 0x14; // 20 byte // usage: 3 byte
    protected override int META_LENGTH_TOTAL_VANILLA => 0x18; // 24 byte
    protected override int META_LENGTH_TOTAL_WAYPOINT => 0x118; // 280 byte

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
    private PlatformExtra? _settingsContainer;

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

        // Cache previous timestamp.
        var lastWriteTicks = _lastWriteTime.UtcTicks - _lastWriteTime.UtcTicks % (long)(Math.Pow(10, 4));

        // Refresh will also update _lastWriteTime.
        RefreshContainerCollection();

        // Get all written container that are newer than the previous timestamp.
        return SaveContainerCollection.Where(c => c.Exists && c.LastWriteTime?.UtcTicks >= lastWriteTicks);
    }

    #endregion

    private static string GetBlobContainerPath(PlatformExtra extra) => GetBlobContainerPath(extra, extra.MicrosoftBlobContainerExtension!.Value);

    private static string GetBlobContainerPath(PlatformExtra extra, byte extension)
    {
        return Path.Combine(extra!.MicrosoftBlobDirectory!.FullName, $"container.{extension}");
    }

    private static FileInfo GetBlobFileInfo(PlatformExtra extra, Guid guid)
    {
        return new(Path.Combine(extra.MicrosoftBlobDirectory!.FullName, guid.ToPath()));
    }

    private string GetTemporaryAccountFile(string fileToWrite)
    {
        return Path.Combine(PATH, "t", $"{_containersIndexFile!.DirectoryName}_{fileToWrite}");
    }

    private string GetTemporaryBlobFile(string fileToWrite, Container container) => GetTemporaryAccountFile($"{container.Extra.MicrosoftBlobDirectory!.Name}_{fileToWrite}");

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
        var containersIndex = ParseContainersIndex();
        if (containersIndex.Count == 0)
            return Array.Empty<Container>();

        var bag = new ConcurrentBag<Container>();

        var tasks = Enumerable.Range(0, Globals.Constants.OFFSET_INDEX + COUNT_SAVES_TOTAL).Select((metaIndex) =>
        {
            return Task.Run(() =>
            {
                _ = containersIndex.TryGetValue(metaIndex, out var extra);
                if (metaIndex == 0)
                {
                    AccountContainer = CreateContainer(metaIndex, extra);
                    BuildContainerFull(AccountContainer);
                }
                else if (metaIndex == 1)
                {
                    _settingsContainer = extra;
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
    private Dictionary<int, PlatformExtra> ParseContainersIndex()
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

        var result = new Dictionary<int, PlatformExtra>();

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
            var extra = new PlatformExtra();

            // Read length of the identifier and then the identifier itself. Repeats itself.
            var saveIdentifier = readerIndex.ReadBytes(readerIndex.ReadInt32() * 2).GetUnicode();
            if (saveIdentifier != readerIndex.ReadBytes(readerIndex.ReadInt32() * 2).GetUnicode())
                continue;

            extra.MicrosoftSyncTime = readerIndex.ReadBytes(readerIndex.ReadInt32() * 2).GetUnicode();
            extra.MicrosoftBlobContainerExtension = readerIndex.ReadByte();
            extra.MicrosoftSyncState = (MicrosoftBlobSyncStateEnum)(readerIndex.ReadInt32());
            extra.MicrosoftBlobDirectoryGuid = readerIndex.ReadBytes(0x10).GetGuid();
            extra.MicrosoftBlobDirectory = new DirectoryInfo(Path.Combine(Location!.FullName, extra.MicrosoftBlobDirectoryGuid!.Value.ToPath()));
            extra.LastWriteTime = DateTimeOffset.FromFileTime(readerIndex.ReadInt64()).ToLocalTime();

            readerIndex.BaseStream.Seek(0x8, SeekOrigin.Current); // empty data

            // Read Int64 as it has 8 bytes but other platforms only 4.
            extra.SizeDisk = (uint)(readerIndex.ReadInt64());

            // Ignore if already marked as deleted.
            if (extra.MicrosoftBlobDirectory.Exists && extra.MicrosoftSyncState != MicrosoftBlobSyncStateEnum.Deleted)
            {
                var blobContainerPath = GetBlobContainerPath(extra);
                var fileInfos = new HashSet<FileInfo>();

                // In case the blob container extension does not match the containers.index value, try all existing ones until a data file is found.
                if (File.Exists(blobContainerPath))
                {
                    fileInfos.Add(new(blobContainerPath));
                }
                else
                {
                    foreach (var file in extra.MicrosoftBlobDirectory.GetFiles("container.*"))
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

                        var blobFile = GetBlobFileInfo(extra, guid);

                        if (blobIdentifier == "data")
                        {
                            extra.MicrosoftBlobDataFile = blobFile;
                            continue;
                        }

                        if (blobIdentifier == "meta")
                        {
                            extra.Size = blobFile.Exists ? (uint)(blobFile.Length) : 0;
                            extra.MicrosoftBlobMetaFile = blobFile;
                            continue;
                        }
                    }

                    // Update extension in case the read one was not found and break the loop.
                    if (extra.MicrosoftBlobDataFile?.Exists == true)
                    {
                        extra.MicrosoftBlobContainerExtension = System.Convert.ToByte(file.Extension.Substring(1));
                        break;
                    }
                }

                // Mark as deleted if there is no existing data file.
                if (extra.MicrosoftBlobDataFile?.Exists != true)
                    extra.MicrosoftSyncState = MicrosoftBlobSyncStateEnum.Deleted;
            }

            // Store collected data for further processing.
            if (saveIdentifier.StartsWith("Slot"))
            {
                var isManual = System.Convert.ToInt32(saveIdentifier.EndsWith("Manual"));
                var slot = System.Convert.ToInt32(string.Concat(saveIdentifier.Where(char.IsDigit)));

                var metaIndex = Constants.OFFSET_INDEX + ((slot - 1) * 2) + isManual;

                result.Add(metaIndex, extra);
            }
            else if (saveIdentifier == "Settings")
            {
                result.Add(1, extra);
            }
            else if (saveIdentifier == "AccountData")
            {
                result.Add(0, extra);
            }
        }

        return result;
    }

    private protected override Container CreateContainer(int metaIndex, PlatformExtra? extra)
    {
        if (extra is null)
            return new Container(metaIndex);

        return new Container(metaIndex)
        {
            DataFile = extra.MicrosoftBlobDataFile,
            MetaFile = extra.MicrosoftBlobMetaFile,
            Extra = extra,
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
                container.IncompatibilityTag = Constants.INCOMPATIBILITY_001;
            }
            else
            {
                return data;
            }
        }
        else if (container.Extra.MicrosoftSyncState == MicrosoftBlobSyncStateEnum.Deleted)
        {
            container.IncompatibilityTag = Constants.INCOMPATIBILITY_004;
        }
        else if (container.Extra.MicrosoftBlobDirectory?.Exists != true)
        {
            container.IncompatibilityTag = Constants.INCOMPATIBILITY_005;
        }

        container.IncompatibilityTag ??= Constants.INCOMPATIBILITY_006;
        return Array.Empty<byte>();
    }

    protected override uint[] DecryptMeta(Container container, byte[] meta)
    {
        var value = base.DecryptMeta(container, meta);
        var season = BitConverter.ToInt16(meta, 1 * sizeof(uint) + 2);

        container.Extra = container.Extra with
        {
#if NETSTANDARD2_0
            Bytes = meta.Skip(META_LENGTH_KNOWN).ToArray(),
#else
            Bytes = meta[META_LENGTH_KNOWN..],
#endif

            SizeDecompressed = value[4],
            BaseVersion = (int)(value[0]),
            GameMode = BitConverter.ToInt16(meta, 1 * sizeof(uint)),
            Season = (short)(season <= 0 ? 0 : season),
            TotalPlayTime = value[2],
        };
        container.SaveVersion = Calculate.CalculateVersion(container.Extra.BaseVersion, container.Extra.GameMode, container.Extra.Season);

        return value;
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

                var data = CreateData(container);
                var meta = CreateMeta(container, data);

                // Cache original file information.
                var oldContainerExtension = container.Extra.MicrosoftBlobContainerExtension;
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
                    File.Delete(GetBlobContainerPath(container.Extra, oldContainerExtension.Value));
            }

            if (Settings.SetLastWriteTime)
            {
                _lastWriteTime = writeTime;

                var ticks = writeTime.Ticks % (long)(Math.Pow(10, 4)) * -1; // get last four digits negative
                container.LastWriteTime = writeTime.AddTicks(ticks); // set container time without the ticks

                if (container.DataFile is not null)
                {
                    File.SetCreationTime(container.DataFile.FullName, container.LastWriteTime!.Value.LocalDateTime);
                    File.SetLastWriteTime(container.DataFile.FullName, container.LastWriteTime!.Value.LocalDateTime);
                }
                if (container.MetaFile is not null)
                {
                    File.SetCreationTime(container.MetaFile.FullName, container.LastWriteTime!.Value.LocalDateTime);
                    File.SetLastWriteTime(container.MetaFile.FullName, container.LastWriteTime!.Value.LocalDateTime);
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

    protected override byte[] CreateMeta(Container container, byte[] data)
    {
        //  0. SAVE VERSION      (  4)
        //  1. GAME MODE         (  2)
        //  1. SEASON            (  2)
        //  2. TOTAL PLAY TIME   (  4)
        //  3. EMPTY             (  4)
        //  4. DECOMPRESSED SIZE (  4)
        //  5. UNKNOWN           (  4) // Foundation
        //                       ( 24)

        //  5. UNKNOWN           (260) // Waypoint
        //                       (280)

        // Use default size if tail is not set.
        var buffer = new byte[GetMetaSize(container)];

        using var writer = new BinaryWriter(new MemoryStream(buffer));

        if (container.IsAccount)
        {
            // Always 1.
            writer.Write(1); // 4

            // GAME MODE/SEASON and TOTAL PLAY TIME not used.
            writer.Seek(0x10, SeekOrigin.Begin); // 16

            writer.Write(container.Extra.SizeDecompressed); // 4
        }
        else
        {
            writer.Write(container.Extra.BaseVersion); // 4

            writer.Write(container.IsWaypoint && container.GameModeEnum < PresetGameModeEnum.Permadeath ? (short)(PresetGameModeEnum.Normal) : container.Extra.GameMode); // 2
            writer.Write(container.Extra.Season); // 2

            writer.Write((long)(container.Extra.TotalPlayTime)); // 8

            writer.Write(container.Extra.SizeDecompressed); // 4
        }

        // Seek to position of last known byte and append the tail.
        writer.Seek(META_LENGTH_KNOWN, SeekOrigin.Begin);
        writer.Write(container.Extra.Bytes ?? Array.Empty<byte>()); // 4 or 260

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

        var buffer = File.ReadAllBytes(GetBlobContainerPath(container.Extra));

        // Update blob container content.
        using (var writer = new BinaryWriter(new MemoryStream(buffer)))
        {
            writer.Seek(CONTAINER_OFFSET_DATA, SeekOrigin.Begin);
            writer.Write(dataGuid.ToByteArray());

            writer.Seek(CONTAINER_OFFSET_META, SeekOrigin.Begin);
            writer.Write(metaGuid.ToByteArray());
        }

        // Update library container information.
        container.Extra.MicrosoftBlobDataFile = container.DataFile = GetBlobFileInfo(container.Extra, dataGuid);
        container.Extra.MicrosoftBlobMetaFile = container.MetaFile = GetBlobFileInfo(container.Extra, metaGuid);
        container.Extra.MicrosoftBlobContainerExtension = (byte)(container.Extra.MicrosoftBlobContainerExtension == byte.MaxValue ? 1 : container.Extra.MicrosoftBlobContainerExtension! + 1);

        if (container.Extra.MicrosoftSyncState == MicrosoftBlobSyncStateEnum.Synced)
            container.Extra.MicrosoftSyncState = MicrosoftBlobSyncStateEnum.Modified;

        return buffer;
    }

    /// <summary>
    /// Writes the blob container file content to disk.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="blob"></param>
    private void WriteBlob(Container container, byte[] blob)
    {
        var t = GetTemporaryBlobFile($"container.{container.Extra.MicrosoftBlobContainerExtension}", container);
        File.WriteAllBytes(t, blob);
        File.Move(t, GetBlobContainerPath(container.Extra));
    }

    /// <summary>
    /// Creates and writes the containers.index file content to disk.
    /// </summary>
    private void WriteContainersIndex()
    {
        var hasSettings = _settingsContainer is not null;

        var collection = SaveContainerCollection.Where(i => i.Extra.MicrosoftSyncTime is not null); // MicrosoftSyncTime is the first written
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

                writer.Write(AccountContainer!.Extra.MicrosoftSyncTime!.Length);
                writer.Write(AccountContainer!.Extra.MicrosoftSyncTime!.GetUnicodeBytes());

                writer.Write(AccountContainer!.Extra.MicrosoftBlobContainerExtension!.Value);

                writer.Write((int)(AccountContainer!.Extra.MicrosoftSyncState!));

                writer.Write(AccountContainer!.Extra.MicrosoftBlobDirectoryGuid!.Value.ToByteArray());

                writer.Write(AccountContainer!.LastWriteTime!.Value.ToUniversalTime().ToFileTime());

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

                writer.Write(_settingsContainer!.MicrosoftSyncTime!.Length);
                writer.Write(_settingsContainer!.MicrosoftSyncTime!.GetUnicodeBytes());

                writer.Write(_settingsContainer!.MicrosoftBlobContainerExtension!.Value);

                writer.Write((int)(_settingsContainer!.MicrosoftSyncState!));

                writer.Write(_settingsContainer!.MicrosoftBlobDirectoryGuid!.Value.ToByteArray());

                writer.Write(_settingsContainer!.LastWriteTime!.Value.ToUniversalTime().ToFileTime());

                writer.Write(0L);

                writer.Write(_settingsContainer!.MicrosoftBlobDataFile!.Length + _settingsContainer!.MicrosoftBlobMetaFile!.Length);
            }

            foreach (var container in collection)
            {
                for (var i = 0; i < 2; i++)
                {
                    writer.Write(container.Identifier.Length);
                    writer.Write(container.Identifier.GetUnicodeBytes());
                }

                writer.Write(container.Extra.MicrosoftSyncTime!.Length);
                writer.Write(container.Extra.MicrosoftSyncTime!.GetUnicodeBytes());

                writer.Write(container.Extra.MicrosoftBlobContainerExtension!.Value);

                writer.Write((int)(container.Extra.MicrosoftSyncState!.Value));

                writer.Write(container.Extra.MicrosoftBlobDirectoryGuid!.Value.ToByteArray());

                writer.Write(container.Extra.LastWriteTime!.Value.ToUniversalTime().ToFileTime());

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
        var directory = new DirectoryInfo(Path.Combine(Location!.FullName, Destination.Extra.MicrosoftBlobDirectoryGuid!.Value.ToPath()));

        // Set new dummy files. They will be overwritten while writing.
        Destination.DataFile = new(Path.Combine(directory.FullName, "data.guid"));
        Destination.MetaFile = new(Path.Combine(directory.FullName, "meta.guid"));

        // Update Microsoft container with new directory and file information.
        Destination.Extra.MicrosoftBlobDirectory = directory;
        Destination.Extra.MicrosoftBlobDataFile = Destination.DataFile;
        Destination.Extra.MicrosoftBlobMetaFile = Destination.MetaFile;

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

        Directory.CreateDirectory(Destination.Extra.MicrosoftBlobDirectory.FullName);
        File.WriteAllBytes(Destination.Extra.MicrosoftBlobContainerFile!.FullName, blobContainerBinary);
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
                Destination.SaveVersion = Source.SaveVersion;
                Destination.SaveName = Source.SaveName;
                Destination.SaveSummary = Source.SaveSummary;
                Destination.TotalPlayTime = Source.TotalPlayTime;
                Destination.GameModeEnum = Source.GameModeEnum;
                Destination.SeasonEnum = Source.SeasonEnum;
                Destination.BaseVersion = Source.BaseVersion;
                Destination.GameVersionEnum = Source.GameVersionEnum;

                Destination.SetJsonObject(Source.GetJsonObject());

                // This "if" is not really useful in this method but properly implemented nonetheless.
                if (write)
                {
                    Write(Destination, Source.LastWriteTime ?? DateTimeOffset.Now);
                    BuildContainerFull(Destination);
                }
            }
            //else
            //    continue;
        }

        UpdateUserIdentification();
    }

    protected override void CopyPlatformExtra(Container destination, Container source)
    {
        base.CopyPlatformExtra(destination, source);

        destination.Extra = destination.Extra with
        {
            MicrosoftSyncTime = string.Empty,
            MicrosoftBlobContainerExtension = 0,
            MicrosoftSyncState = MicrosoftBlobSyncStateEnum.Created,
            MicrosoftBlobDirectoryGuid = Guid.NewGuid(),
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
            if (write)
            {
                if (container.Extra.MicrosoftBlobDirectory?.Exists == true)
                {
                    try
                    {
                        Directory.Delete(container.Extra.MicrosoftBlobDirectory.FullName, true);
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
            container.DataFile = container.MetaFile = null; // set to null as it constantly changes anyway
            container.IncompatibilityTag = Constants.INCOMPATIBILITY_004;
            container.Extra.MicrosoftSyncState = MicrosoftBlobSyncStateEnum.Deleted;
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
                Destination.GameVersionEnum = Source.GameVersionEnum;
                Destination.SeasonEnum = Source.SeasonEnum;

                Destination.SetJsonObject(Source.GetJsonObject());
                TransferOwnership(Destination, sourceTransferData);

                if (write)
                {
                    Write(Destination, Source.LastWriteTime ?? DateTimeOffset.Now);
                    BuildContainerFull(Destination);
                }
            }
            //else
            //    continue;
        }
    }

    // TODO
    protected override void CreatePlatformExtra(Container destination, Container source)
    {
        destination.Extra = new()
        {
            MicrosoftBlobDirectoryGuid = Guid.NewGuid(),
            MicrosoftBlobContainerExtension = 0,
            LastWriteTime = source.LastWriteTime,
            Bytes = new byte[(source.IsWaypoint ? META_LENGTH_TOTAL_WAYPOINT : META_LENGTH_TOTAL_VANILLA) - META_LENGTH_KNOWN],
            MicrosoftSyncState = MicrosoftBlobSyncStateEnum.Created,
        };
    }

    #endregion

    // // FileSystemWatcher

    #region FileSystemWatcher

    // TODO IsLoaded?
    /// <summary>
    /// Refreshes all containers in the collection with newly written data from the containers.index file.
    /// </summary>
    private void RefreshContainerCollection()
    {
        var containersIndex = ParseContainersIndex();
        if (containersIndex.Count == 0)
            return;

        for (var metaIndex = 0; metaIndex < Globals.Constants.OFFSET_INDEX + COUNT_SAVES_TOTAL; metaIndex++)
        {
            var contains = containersIndex.TryGetValue(metaIndex, out var extra);
            if (metaIndex == 0)
            {
                if (contains)
                {
                    if (AccountContainer.Exists)
                    {
                        // Set all properties that would be set in CreateContainer().
                        AccountContainer.DataFile = extra.MicrosoftBlobDataFile;
                        AccountContainer.MetaFile = extra.MicrosoftBlobMetaFile;
                        AccountContainer.Extra = extra;
                    }
                    else
                    {
                        AccountContainer = CreateContainer(metaIndex, extra);
                    }
                    BuildContainerFull(AccountContainer);
                }
                else
                {
                    AccountContainer.Reset();
                }
            }
            else if (metaIndex == 1)
            {
                _settingsContainer = extra;
            }
            else
            {
                var collectionIndex = metaIndex - Constants.OFFSET_INDEX;
                var container = SaveContainerCollection[collectionIndex];

                if (contains)
                {
                    if (container.Exists)
                    {
                        // Set all properties that would be set in CreateContainer().
                        container.DataFile = extra.MicrosoftBlobDataFile;
                        container.MetaFile = extra.MicrosoftBlobMetaFile;
                        container.Extra = extra;
                    }
                    else
                    {
                        container = SaveContainerCollection[collectionIndex] = CreateContainer(metaIndex, extra);
                    }
                    if (Settings.LoadingStrategy < LoadingStrategyEnum.Full && !container.IsLoaded)
                    {
                        BuildContainerHollow(container);
                    }
                    else
                    {
                        BuildContainerFull(container);
                    }
                    GenerateBackupCollection(container);
                }
                else
                {
                    container.Reset();
                }
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
