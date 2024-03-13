using System.Collections.Concurrent;
using System.Text;

using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;

using Microsoft.Extensions.Caching.Memory;

using Newtonsoft.Json.Linq;

namespace libNOM.io;


public partial class PlatformMicrosoft : Platform
{
    #region Constant

    internal const string ACCOUNT_PATTERN = "*_29070100B936489ABCE8B9AF3980429C";

    internal static readonly string[] ANCHOR_FILE_PATTERN = ["containers.index"];

    internal static readonly string PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", "HelloGames.NoMansSky_bs190hzg1sesy", "SystemAppData", "wgs");

    protected override int META_LENGTH_KNOWN => 0x14; // 20
    internal override int META_LENGTH_TOTAL_VANILLA => 0x18; // 24
    internal override int META_LENGTH_TOTAL_WAYPOINT => 0x118; // 280

    private const int BLOBCONTAINER_HEADER = 0x4; // 4
    private const int BLOBCONTAINER_COUNT = 0x2; // 2
    private const int BLOBCONTAINER_IDENTIFIER_LENGTH = 0x80; // 128
    private const int BLOBCONTAINER_TOTAL_LENGTH = sizeof(int) + sizeof(int) + BLOBCONTAINER_COUNT * (BLOBCONTAINER_IDENTIFIER_LENGTH + 2 * 0x10); // 328

    private const int CONTAINERSINDEX_HEADER = 0xE; // 14
    private const long CONTAINERSINDEX_FOOTER = 0x10000000; // 268.435.456
    private const int CONTAINERSINDEX_OFFSET_BLOBCONTAINER_LIST = 0xC8; // 200

    internal static readonly byte[] SAVE_V2_HEADER = [.. Encoding.ASCII.GetBytes("HGSAVEV2"), 0x00];
    internal const int SAVE_V2_HEADER_PARTIAL_LENGTH = 0x8; // 8
    internal const int SAVE_V2_CHUNK_MAX_LENGTH = 0x100000; // 1.048.576

    #endregion

    #region Field

    private string _accountGuid = null!; // will be set when containers.index is parsed
    private string? _accountId; // will be set if available in path
    private FileInfo _containersindex = null!; // will be set if valid
    private DateTimeOffset _lastWriteTime; // will be set when containers.index is parsed to store global timestamp
    private string _processIdentifier = null!; // will be set when containers.index is parsed
    private PlatformExtra? _settingsContainer; // will be set when containers.index is parsed and exists

    #endregion

    #region Property

    #region Flags

    // public //

    public override bool CanCreate { get; } = true;

    public override bool CanRead { get; } = true;

    public override bool CanUpdate { get; } = true;

    public override bool CanDelete { get; } = true;

    public override bool Exists => base.Exists && _containersindex.Exists; // { get; }

    public override bool HasModding { get; } = false;

    public override bool RestartToApply { get; } = true;

    // protected //

    protected override bool IsConsolePlatform { get; } = false;

    #endregion

    #region Platform Indicator

    // public //

    public override PlatformEnum PlatformEnum { get; } = PlatformEnum.Microsoft;

    // protected //

    protected override string[] PlatformAnchorFilePattern { get; } = ANCHOR_FILE_PATTERN;

    protected override string? PlatformArchitecture { get; } = "XB1|Final";

    // Looks like "C:\\Program Files\\WindowsApps\\HelloGames.NoMansSky_4.38.0.0_x64__bs190hzg1sesy\\Binaries\\NMS.exe"
    protected override string? PlatformProcessPath { get; } = @"bs190hzg1sesy\Binaries\NMS.exe";

    protected override string PlatformToken { get; } = "XB";

    #endregion

    #endregion

    // //

    #region Getter

    protected override IEnumerable<Container> GetCacheEvictionContainers(string name)
    {
        if (!name.Equals("containers.index", StringComparison.OrdinalIgnoreCase))
            return [];

        // Cache previous timestamp.
        var lastWriteTicks = _lastWriteTime.NullifyTicks(4).UtcTicks;

        // Refresh will also update _lastWriteTime.
        RefreshContainerCollection();

        // Get all written container that are newer than the previous timestamp.
        return SaveContainerCollection.Where(i => i.Exists && i.LastWriteTime?.UtcTicks >= lastWriteTicks);
    }

    #endregion

    // //

    #region Constructor

    public PlatformMicrosoft() : base() { }

    public PlatformMicrosoft(string path) : base(path) { }

    public PlatformMicrosoft(string path, PlatformSettings platformSettings) : base(path, platformSettings) { }

    public PlatformMicrosoft(DirectoryInfo directory) : base(directory) { }

    public PlatformMicrosoft(DirectoryInfo directory, PlatformSettings platformSettings) : base(directory, platformSettings) { }

#if NETSTANDARD2_1_OR_GREATER || NET6_0
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0057: Use range operator", Justification = "The range operator is not supported in netstandard2.0 and we do not want three ways, so we only do oldest and newest.")]
#endif
    protected override void InitializeComponent(DirectoryInfo? directory, PlatformSettings? platformSettings)
    {
        // Proceed to base method even if no directory.
        if (directory is not null)
        {
#if NETSTANDARD2_0_OR_GREATER || NET6_0
            if (directory.Name.Length == 49 && directory.Name.EndsWith(ACCOUNT_PATTERN.Substring(1)) && directory.Name.Substring(0, 16).All("0123456789ABCDEFabcdef".Contains))
                _accountId = System.Convert.ToInt64(directory.Name.Split('_')[0], 16).ToString();
#else
            if (directory.Name.Length == 49 && directory.Name.EndsWith(ACCOUNT_PATTERN[1..]) && directory.Name[..16].All(char.IsAsciiHexDigit))
                _accountId = System.Convert.ToInt64(directory.Name.Split('_')[0], 16).ToString();
#endif

            _containersindex = new FileInfo(Path.Combine(directory.FullName, "containers.index"));
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
            return [];

        var bag = new ConcurrentBag<Container>();

        var tasks = Enumerable.Range(0, Constants.OFFSET_INDEX + COUNT_SAVES_TOTAL).Select((metaIndex) =>
        {
            return Task.Run(() =>
            {
                _ = containersIndex.TryGetValue(metaIndex, out var extra);
                if (metaIndex == 0)
                {
                    AccountContainer = CreateContainer(metaIndex, extra);
                    BuildContainerFull(AccountContainer); // always full
                }
                else if (metaIndex == 1)
                {
                    _settingsContainer = extra; // just caching it to be able to write it again
                }
                else
                {
                    var container = CreateContainer(metaIndex, extra);

                    if (Settings.LoadingStrategy < LoadingStrategyEnum.Full)
                        BuildContainerHollow(container);
                    else
                        BuildContainerFull(container);

                    GenerateBackupCollection(container);
                    bag.Add(container);
                }
            });
        });
        Task.WaitAll(tasks.ToArray());

        return bag;
    }

    private protected override Container CreateContainer(int metaIndex, PlatformExtra? extra)
    {
        if (extra is null)
            return new Container(metaIndex, this) { Extra = new() };

        return new Container(metaIndex, this)
        {
            DataFile = extra.MicrosoftBlobDataFile,
            MetaFile = extra.MicrosoftBlobMetaFile,
            /// Additional values will be set in <see cref="UpdateContainerWithMetaInformation"/> and <see cref="Platform.UpdateContainerWithDataInformation"/>.
            Extra = extra,
        };
    }

    /// <summary>
    /// Reads the containers.index file to get the exact information where each file is.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidDataException"/>
    private Dictionary<int, PlatformExtra> ParseContainersIndex()
    {
        var offset = ParseGlobalIndex(out var bytes);
        Dictionary<int, PlatformExtra> result = [];

        for (var i = 0; i < bytes.Cast<long>(4); i++) // container count
        {
            offset = ParseBlobContainerIndex(bytes, offset, out var saveIdentifier, out var extra);

            // Store collected data for further processing.
            if (saveIdentifier.StartsWith("Slot"))
            {
                var slot = System.Convert.ToInt32(string.Concat(saveIdentifier.Where(char.IsDigit)));
                var metaIndex = Constants.OFFSET_INDEX + ((slot - 1) * 2) + System.Convert.ToInt32(saveIdentifier.EndsWith("Manual"));

                result.Add(metaIndex, extra);
            }
            else if (saveIdentifier == "AccountData")
            {
                result.Add(0, extra);
            }
            else if (saveIdentifier == "Settings")
            {
                result.Add(1, extra);
            }
        }

        return result;
    }

    private int ParseGlobalIndex(out ReadOnlySpan<byte> bytes)
    {
        /**
         0. HEADER (14)                     (  4)
         1. NUMBER OF BLOB CONTAINERS       (  8)
         2. PROCESS IDENTIFIER LENGTH (44)  (  4)
         3. PROCESS IDENTIFIER              ( 88) (UTF-16)
         4. LAST MODIFIED TIME              (  8)
         5. SYNC STATE                      (  4) (0 = ?, 1 = ?, 2 = MODIFIED, 3 = SYNCED)
         6. ACCOUNT IDENTIFIER LENGTH (36)  (  4)
         7. ACCOUNT IDENTIFIER              ( 72) (UTF-16)
         8. FOOTER (268435456)              (  8)
                                            (200)
        */
        bytes = _containersindex.ReadAllBytes();

        if (bytes.Cast<int>(0) is int header && header != CONTAINERSINDEX_HEADER)
            ThrowHelper.ThrowInvalidDataException($"Wrong header in containers.index file! Expected {CONTAINERSINDEX_HEADER} but got {header}.");

        var offset = 12 + bytes.ReadString(12, out _processIdentifier); // 0, 1

        _lastWriteTime = DateTimeOffset.FromFileTime(bytes.Cast<long>(offset)).ToLocalTime();

        offset += 12 + bytes.ReadString(offset + 12, out _accountGuid); // 4, 5

        if (bytes.Cast<long>(offset) is long footer && footer != CONTAINERSINDEX_FOOTER)
            ThrowHelper.ThrowInvalidDataException($"Wrong footer in containers.index file! Expected {CONTAINERSINDEX_FOOTER} but got {footer}.");

        return offset + 8; // 8
    }

    private int ParseBlobContainerIndex(ReadOnlySpan<byte> bytes, int offset, out string saveIdentifier, out PlatformExtra extra)
    {
        /**
         9. SAVE IDENTIFIER LENGTH          (  4)
        10. SAVE IDENTIFIER                 (VAR) (UTF-16)
        11. SAVE IDENTIFIER LENGTH          (  4)
        12. SAVE IDENTIFIER                 (VAR) (UTF-16)
        13. SYNC HEX LENGTH                 (  4)
        14. SYNC HEX                        (VAR) (UTF-16)
        15. BLOB CONTAINER FILE EXTENSION   (  1)
        16. SYNC STATE                      (  4) (0 = ?, 1 = SYNCED, 2 = MODIFIED, 3 = DELETED, 4 = ?, 5 = CREATED)
        17. DIRECTORY                       ( 16) (GUID)
        18. LAST MODIFIED TIME              (  8)
        19. EMPTY                           (  8)
        20. TOTAL SIZE OF FILES             (  8) (BLOB CONTAINER EXCLUDED)
        */

        offset += bytes.ReadString(offset, out saveIdentifier) * 2; // saveIdentifier two times in a row
        offset += bytes.ReadString(offset, out var syncTime);

        var directoryGuid = bytes.GetGuid(offset + 5);

        extra = new()
        {
            LastWriteTime = DateTimeOffset.FromFileTime(bytes.Cast<long>(offset + 21)).ToLocalTime(),
            SizeDisk = (uint)(bytes.Cast<long>(offset + 37)),

            MicrosoftSyncTime = syncTime,
            MicrosoftBlobContainerExtension = bytes[offset],
            MicrosoftSyncState = (MicrosoftBlobSyncStateEnum)(bytes.Cast<int>(offset + 1)),
            MicrosoftBlobDirectoryGuid = directoryGuid,

            MicrosoftBlobDirectory = new DirectoryInfo(Path.Combine(Location!.FullName, directoryGuid.ToPath())),
        };

        // Ignore if already marked as deleted.
        if (extra.MicrosoftBlobDirectory.Exists && extra.MicrosoftSyncState != MicrosoftBlobSyncStateEnum.Deleted)
            extra = ParseBlobContainer(extra);

        return offset + 45; // 15, 16, 17, 18, 19, 20
    }

    private static PlatformExtra ParseBlobContainer(PlatformExtra extra)
    {
        /**
         0. HEADER (4)                      (  4)
         1. NUMBER OF BLOBS (2)             (  4)
         2. DATA BLOB IDENTIFIER            ( 80) (UTF-16)
         3. DATA FILE CLOUD                 ( 16) (GUID)
         4. DATA FILE LOCAL                 ( 16) (GUID)
         5. META BLOB IDENTIFIER            ( 80) (UTF-16)
         6. META FILE CLOUD                 ( 16) (GUID)
         7. META FILE LOCAL                 ( 16) (GUID)
                                            (328)
        */

        var blobContainerIndex = extra.MicrosoftBlobContainerFile!;
        var files = new HashSet<FileInfo>();

        // In case the blob container extension does not match the containers.index value, try all existing ones until a data file is found.
        if (blobContainerIndex.Exists)
            files.Add(blobContainerIndex);
        else
            foreach (var file in extra.MicrosoftBlobDirectory!.EnumerateFiles("container.*"))
                files.Add(file);

        // Start with the presumably newest one.
        foreach (var blobContainer in files.OrderByDescending(i => i.Extension))
        {
            ReadOnlySpan<byte> bytes = blobContainer.ReadAllBytes();
            var offset = 8; // start after header

            if (bytes.Length != BLOBCONTAINER_TOTAL_LENGTH || bytes.Cast<int>(0) != BLOBCONTAINER_HEADER)
                continue;

            for (var j = 0; j < bytes.Cast<int>(4); j++) // blob count
            {
                offset += bytes.ReadString(offset, BLOBCONTAINER_IDENTIFIER_LENGTH, out var blobIdentifier);

                // Second Guid is the one to use as the first one is probably the current name in the cloud.
                var blobFile = extra.MicrosoftBlobDirectory!.GetBlobFileInfo(bytes.GetGuid(offset + 16));
                var blobSyncGuid = bytes.GetGuid(offset);

                offset += 32; // 2 * sizeof(Guid)

#if NETSTANDARD2_0
                if (blobIdentifier.ToString().Equals("data"))
                    extra = extra with
                    {
                        MicrosoftBlobDataFile = blobFile,
                        MicrosoftBlobDataSyncGuid = blobSyncGuid,
                    };
                else if (blobIdentifier.ToString().Equals("meta"))
                    extra = extra with
                    {
                        MicrosoftBlobMetaFile = blobFile,
                        MicrosoftBlobMetaSyncGuid = blobSyncGuid,
                    };
#else
                if (blobIdentifier.Equals("data".AsSpan(), StringComparison.Ordinal))
                    extra = extra with
                    {
                        MicrosoftBlobDataFile = blobFile,
                        MicrosoftBlobDataSyncGuid = blobSyncGuid,
                    };
                else if (blobIdentifier.Equals("meta".AsSpan(), StringComparison.Ordinal))
                    extra = extra with
                    {
                        MicrosoftBlobMetaFile = blobFile,
                        MicrosoftBlobMetaSyncGuid = blobSyncGuid,
                    };
#endif
            }

            // Update extension in case the read one was not found and break the loop.
            if (extra.MicrosoftBlobDataFile?.Exists == true)
            {
                extra = extra with
                {
                    MicrosoftBlobContainerExtension = System.Convert.ToByte(blobContainer.Extension.TrimStart('.')),
                };
                break;
            }
        }

        // Mark as deleted if there is no existing data file.
        if (extra.MicrosoftBlobDataFile?.Exists != true)
            extra = extra with
            {
                MicrosoftSyncState = MicrosoftBlobSyncStateEnum.Deleted,
            };

        return extra;
    }

    #endregion

    #region Load

    protected override ReadOnlySpan<byte> LoadContainer(Container container)
    {
        var result = base.LoadContainer(container);

        // Use more precise Microsoft tags if container does not exist.
        // Result is already empty if so and tags set if none of the rules here apply.
        if (!container.Exists)
            if (container.Extra.MicrosoftSyncState == MicrosoftBlobSyncStateEnum.Deleted)
                container.IncompatibilityTag = Constants.INCOMPATIBILITY_004;
            else if (container.Extra.MicrosoftBlobDirectory?.Exists != true)
                container.IncompatibilityTag = Constants.INCOMPATIBILITY_005;

        return result;
    }

#if !NETSTANDARD2_0
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0057: Use range operator", Justification = "The range operator is not supported in netstandard2.0 and Slice() has no performance penalties.")]
#endif
    protected override void UpdateContainerWithMetaInformation(Container container, ReadOnlySpan<byte> disk, ReadOnlySpan<uint> decompressed)
    {
        /**
          0. BASE VERSION                   (  4)
          1. GAME MODE                      (  2)
          1. SEASON                         (  2)
          2. TOTAL PLAY TIME                (  4)
          3. EMPTY                          (  4)
          4. DECOMPRESSED SIZE              (  4) // before Omega 4.52
          4. COMPRESSED SIZE                (  4) // since Omega 4.52

          5. EMPTY                          (  4)
                                            ( 24)

          5. SAVE NAME                      (128) // may contain additional junk data after null terminator
         37. SAVE SUMMARY                   (128) // may contain additional junk data after null terminator
         69. DIFFICULTY PRESET              (  1)
         69. EMPTY                          (  3) // may contain additional junk data
                                            (280)
        */
        if (disk.IsEmpty())
            return;

        // Vanilla data always available.
        container.Extra = container.Extra with
        {
            MetaFormat = disk.Length == META_LENGTH_TOTAL_VANILLA ? MetaFormatEnum.Foundation : (disk.Length == META_LENGTH_TOTAL_WAYPOINT ? MetaFormatEnum.Waypoint : MetaFormatEnum.Unknown),
            Bytes = disk.Slice(META_LENGTH_KNOWN).ToArray(),
            Size = (uint)(disk.Length),
            BaseVersion = (int)(decompressed[0]),
            GameMode = disk.Cast<ushort>(4),
            Season = disk.Cast<ushort>(6) is var season && season == ushort.MaxValue ? (ushort)(0) : season,
            TotalPlayTime = decompressed[2],
        };

        // Extended data since Waypoint.
        if (disk.Length == META_LENGTH_TOTAL_WAYPOINT)
            container.Extra = container.Extra with
            {
                SaveName = disk.Slice(20, 128).GetStringUntilTerminator(),
                SaveSummary = disk.Slice(148, 128).GetStringUntilTerminator(),
                DifficultyPreset = disk[276],
            };

        // As data has a save streaming like format since Omega 4.52, the disk size now stored.
        if (Meta.GameVersion.Get(container.Extra.BaseVersion) < GameVersionEnum.OmegaWithV2)
            container.Extra = container.Extra with
            {
                SizeDecompressed = decompressed[4],
            };
        else
            container.Extra = container.Extra with
            {
                SizeDisk = decompressed[4],
            };

        // GameVersion with BaseVersion only is not 100% accurate but good enough to calculate SaveVersion.
        container.SaveVersion = Meta.SaveVersion.Calculate(container, Meta.GameVersion.Get(container.Extra.BaseVersion));
    }

    protected override ReadOnlySpan<byte> DecompressData(Container container, ReadOnlySpan<byte> data)
    {
        if (container.IsAccount || !data.StartsWith(SAVE_V2_HEADER)) // single chunk compression for Account and before Omega 4.52
        {
            _ = LZ4.Decode(data, out var target, (int)(container.Extra.SizeDecompressed));
            return target;
        }

        // New format is similar to the save streaming introduced with Frontiers.
        var offset = SAVE_V2_HEADER.Length;
        ReadOnlySpan<byte> result = [];

        while (offset < data.Length)
        {
            var chunkHeader = data.Slice(offset, SAVE_V2_HEADER_PARTIAL_LENGTH).Cast<byte, uint>();
            var sizeCompressed = (int)(chunkHeader[1]);

            offset += SAVE_V2_HEADER_PARTIAL_LENGTH;
            _ = LZ4.Decode(data.Slice(offset, sizeCompressed), out var target, (int)(chunkHeader[0]));
            offset += sizeCompressed;

            result = result.Concat(target);
        }

        return result;
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

                var data = PrepareData(container);
                var meta = PrepareMeta(container, data);

                // Cache original file information.
                var cache = new PlatformExtra()
                {
                    MicrosoftBlobContainerExtension = container.Extra.MicrosoftBlobContainerExtension,
                    MicrosoftBlobDataFile = container.Extra.MicrosoftBlobDataFile,
                    MicrosoftBlobMetaFile = container.Extra.MicrosoftBlobMetaFile,
                };

                // Create blob container with new file information.
                var blob = PrepareBlobContainer(container);

                // Write the previously created files and delete the old ones.
                WriteMeta(container, meta);
                cache.MicrosoftBlobMetaFile?.Delete();

                WriteData(container, data);
                cache.MicrosoftBlobDataFile?.Delete();

                WriteBlobContainer(container, blob);
                cache.MicrosoftBlobContainerFile?.Delete();
            }

            if (Settings.SetLastWriteTime)
            {
                _lastWriteTime = writeTime; // global timestamp has full accuracy
                container.LastWriteTime = _lastWriteTime.NullifyTicks(4);

                container.DataFile?.SetFileTime(container.LastWriteTime);
                container.MetaFile?.SetFileTime(container.LastWriteTime);
            }

            // Finally write the containers.index file.
            WriteContainersIndex();
        }

        EnableWatcher();

        // Always refresh in case something above was executed.
        container.RefreshFileInfo();
        container.WriteCallback.Invoke();
    }

    protected override ReadOnlySpan<byte> CompressData(Container container, ReadOnlySpan<byte> data)
    {
        if (!container.IsSave || !container.IsVersion452OmegaWithV2)
        {
            _ = LZ4.Encode(data, out var target);
            return target;
        }

        // New format is similar to the save streaming introduced with Frontiers.
        var position = 0;
        ReadOnlySpan<byte> result = SAVE_V2_HEADER;

        while (position < data.Length)
        {
            var maxLength = data.Length - position;

            // The tailing \0 needs to compressed separately and must not be part of the actual JSON chunks.
            var source = data.Slice(position, Math.Min(SAVE_V2_CHUNK_MAX_LENGTH, maxLength == 1 ? 1 : maxLength - 1));
            _ = LZ4.Encode(source, out var target);
            position += source.Length;

            var chunkHeader = new ReadOnlySpan<uint>(
            [
                (uint)(source.Length),
                (uint)(target.Length),
            ]);

            result = result.Concat(chunkHeader.Cast<uint, byte>()).Concat(target);
        }

        return result;
    }

    protected override Span<uint> CreateMeta(Container container, ReadOnlySpan<byte> data)
    {
        var buffer = new byte[container.MetaSize];

        using var writer = new BinaryWriter(new MemoryStream(buffer));
        if (container.IsAccount)
        {
            // Always 1.
            writer.Write(1); // 4

            // GAME MODE, SEASON, and TOTAL PLAY TIME not used.
            writer.Seek(0x10, SeekOrigin.Begin); // 16

            writer.Write(container.Extra.SizeDecompressed); // 4
        }
        else
        {
            writer.Write(container.BaseVersion); // 4
            writer.Write((ushort)(container.GameMode)); // 2
            writer.Write((ushort)(container.Season)); // 2
            writer.Write(container.TotalPlayTime); // 4

            // Skip EMPTY.
            writer.Seek(0x4, SeekOrigin.Current); // 4

            writer.Write(container.IsVersion452OmegaWithV2 ? container.Extra.SizeDisk : container.Extra.SizeDecompressed); // 4

            // Insert trailing bytes and the extended Waypoint data.
            AddWaypointMeta(writer, container); // Extra.Bytes is 260 or 4
        }

        return buffer.AsSpan().Cast<byte, uint>();
    }

    /// <summary>
    /// Updates the data and meta file information for the new writing.
    /// </summary>
    /// <param name="container"></param>
    /// <returns></returns>
    private static byte[] PrepareBlobContainer(Container container)
    {
        // Initializes a new Guid to use as file names.
        var dataGuid = Guid.NewGuid();
        var metaGuid = Guid.NewGuid();

        // Update container and its extra.
        container.Extra = container.Extra with
        {
            MicrosoftBlobContainerExtension = (byte)(container.Extra.MicrosoftBlobContainerExtension == byte.MaxValue ? 1 : container.Extra.MicrosoftBlobContainerExtension!.Value + 1),
            MicrosoftSyncState = container.Extra.MicrosoftSyncState == MicrosoftBlobSyncStateEnum.Synced ? MicrosoftBlobSyncStateEnum.Modified : container.Extra.MicrosoftSyncState,
            MicrosoftBlobDataFile = container.DataFile = container.Extra.MicrosoftBlobDirectory!.GetBlobFileInfo(dataGuid),
            MicrosoftBlobMetaFile = container.MetaFile = container.Extra.MicrosoftBlobDirectory!.GetBlobFileInfo(metaGuid),
        };

        // Create new blob container file content.
        var buffer = new byte[BLOBCONTAINER_TOTAL_LENGTH];

        using var writer = new BinaryWriter(new MemoryStream(buffer));
        writer.Write(BLOBCONTAINER_HEADER);
        writer.Write(BLOBCONTAINER_COUNT);

        writer.Write("data".GetUnicodeBytes());
        writer.Seek(BLOBCONTAINER_IDENTIFIER_LENGTH - 8, SeekOrigin.Current); // 8 = sizeof(data as UTF-16)
        writer.Write(container.Extra.MicrosoftBlobDataSyncGuid?.ToByteArray() ?? new byte[16]);
        writer.Write(dataGuid.ToByteArray());

        writer.Write("meta".GetUnicodeBytes());
        writer.Seek(BLOBCONTAINER_IDENTIFIER_LENGTH - 8, SeekOrigin.Current); // 8 = sizeof(meta as UTF-16)
        writer.Write(container.Extra.MicrosoftBlobMetaSyncGuid?.ToByteArray() ?? new byte[16]);
        writer.Write(metaGuid.ToByteArray());

        return buffer;
    }

    /// <summary>
    /// Writes the blob container file content to disk.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="blob"></param>
    private static void WriteBlobContainer(Container container, byte[] blob)
    {
        container.Extra.MicrosoftBlobContainerFile?.WriteAllBytes(blob);
    }

    /// <summary>
    /// Creates and writes the containers.index file content to disk.
    /// </summary>
#if !NETSTANDARD2_0
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0057: Use range operator", Justification = "The range operator is not supported in netstandard2.0 and Slice() has no performance penalties.")]
#endif
    private void WriteContainersIndex()
    {
        var hasSettings = _settingsContainer is not null;

        var collection = SaveContainerCollection.Where(i => i.Extra.MicrosoftBlobDirectoryGuid is not null);
        var count = (long)(collection.Count() + HasAccountData.ToByte() + hasSettings.ToByte());

        // Longest name (e.g. Slot10Manual) has a total length of 0x8C (140) and any leftover for short ones will be cut off.
        var buffer = new byte[CONTAINERSINDEX_OFFSET_BLOBCONTAINER_LIST + (count * 0x8C)];

        using (var writer = new BinaryWriter(new MemoryStream(buffer)))
        {
            writer.Write(CONTAINERSINDEX_HEADER);
            writer.Write(count);
            AddDynamicText(writer, _processIdentifier, 1);
            writer.Write(_lastWriteTime.ToUniversalTime().ToFileTime());
            writer.Write((int)(MicrosoftIndexSyncStateEnum.Modified));
            AddDynamicText(writer, _accountGuid, 1);
            writer.Write(CONTAINERSINDEX_FOOTER);

            if (HasAccountData)
                AddMicrosoftMeta(writer, AccountContainer.Identifier, AccountContainer.Extra);

            if (hasSettings)
                AddMicrosoftMeta(writer, "Settings", _settingsContainer!);

            foreach (var container in collection)
                AddMicrosoftMeta(writer, container.Identifier, container.Extra);

            buffer = buffer.AsSpan().Slice(0, (int)(writer.BaseStream.Position)).ToArray();
        }

        // Write and refresh the containers.index file.
        _containersindex.WriteAllBytes(buffer);
        _containersindex.Refresh();
    }

    private static void AddMicrosoftMeta(BinaryWriter writer, string identifier, PlatformExtra extra)
    {
        // Make sure to get the latest data.
        extra.MicrosoftBlobDataFile?.Refresh();
        extra.MicrosoftBlobMetaFile?.Refresh();

        AddDynamicText(writer, identifier, 2);
        AddDynamicText(writer, extra.MicrosoftSyncTime!, 1);
        writer.Write(extra.MicrosoftBlobContainerExtension!.Value);
        writer.Write((int)(extra.MicrosoftSyncState!.Value));
        writer.Write(extra.MicrosoftBlobDirectoryGuid!.Value.ToByteArray());
        writer.Write(extra.LastWriteTime!.Value.ToUniversalTime().ToFileTime());
        writer.Write((long)(0));
        writer.Write((extra.MicrosoftBlobDataFile?.Exists == true ? extra.MicrosoftBlobDataFile!.Length : 0) + (extra.MicrosoftBlobMetaFile?.Exists == true ? extra.MicrosoftBlobMetaFile!.Length : 0));
    }

    /// <summary>
    /// Adds the length of the specified string and the string itself as unicode to the writer.
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="identifier"></param>
    /// <param name="count">How many times it should be added.</param>
    private static void AddDynamicText(BinaryWriter writer, string identifier, int count)
    {
        for (var i = 0; i < count; i++)
        {
            writer.Write(identifier.Length);
            writer.Write(identifier.GetUnicodeBytes());
        }
    }

    #endregion

    // // File Operation

    #region Copy

    protected override void CopyPlatformExtra(Container destination, Container source)
    {
        base.CopyPlatformExtra(destination, source);

        // Creating dummy blob data only necessary if destination does not exist.
        if (!destination.Exists)
            ExecuteCanCreate(destination);
    }

    private void ExecuteCanCreate(Container Destination)
    {
        var directoryGuid = Guid.NewGuid();
        var directory = new DirectoryInfo(Path.Combine(Location!.FullName, directoryGuid.ToPath()));

        // Update container and its extra with dummy data.
        Destination.Extra = Destination.Extra with
        {
            MicrosoftSyncTime = string.Empty,
            MicrosoftBlobContainerExtension = 0,
            MicrosoftSyncState = MicrosoftBlobSyncStateEnum.Created,
            MicrosoftBlobDirectoryGuid = directoryGuid,
            MicrosoftBlobDataFile = Destination.DataFile = new(Path.Combine(directory.FullName, "data.guid")),
            MicrosoftBlobMetaFile = Destination.MetaFile = new(Path.Combine(directory.FullName, "meta.guid")),

            MicrosoftBlobDirectory = directory,
        };

        // Prepare blob container file content. Guid of data and meta file will be set while executing Write().
        var buffer = new byte[BLOBCONTAINER_TOTAL_LENGTH];
        using (var writer = new BinaryWriter(new MemoryStream(buffer)))
        {
            writer.Write(BLOBCONTAINER_HEADER);
            writer.Write(BLOBCONTAINER_COUNT);

            writer.Write("data".GetUnicodeBytes());
            writer.Seek(BLOBCONTAINER_IDENTIFIER_LENGTH - 8 + 32, SeekOrigin.Current);

            writer.Write("meta".GetUnicodeBytes());
        }

        // Write a dummy file.
        Directory.CreateDirectory(Destination.Extra.MicrosoftBlobDirectory!.FullName);
        File.WriteAllBytes(Destination.Extra.MicrosoftBlobContainerFile!.FullName, buffer);
    }

    #endregion

    #region Delete

    protected override void Delete(IEnumerable<Container> containers, bool write)
    {
        Guard.IsTrue(CanDelete);

        DisableWatcher();

        foreach (var container in containers)
        {
            if (write)
            {
                try
                {
                    container.Extra.MicrosoftBlobDirectory?.Delete();
                }
                catch (Exception ex) when (ex is DirectoryNotFoundException or IOException or UnauthorizedAccessException) { } // nothing to do
            }

            if (Settings.SetLastWriteTime)
            {
                _lastWriteTime = DateTimeOffset.Now.LocalDateTime; // global timestamp has full accuracy
                container.LastWriteTime = _lastWriteTime.NullifyTicks(4);
            }

            container.Reset();
            container.IncompatibilityTag = Constants.INCOMPATIBILITY_004;

            container.DataFile = container.MetaFile = null; // set to null as it constantly changes anyway
            container.Extra = container.Extra with { MicrosoftSyncState = MicrosoftBlobSyncStateEnum.Deleted };
        }

        if (write)
            WriteContainersIndex();

        EnableWatcher();
    }

    #endregion

    #region Transfer

    protected override void CreatePlatformExtra(Container destination, Container source)
    {
        base.CreatePlatformExtra(destination, source);

        // Always creating dummy blob data (already created in CopyPlatformExtra() if destination does not exist).
        if (destination.Exists)
            ExecuteCanCreate(destination);
    }

    #endregion

    // // FileSystemWatcher

    #region FileSystemWatcher

    protected override void OnCacheEviction(object key, object value, EvictionReason reason, object state)
    {
        /** Microsoft WatcherChangeTypes

        All changes by game:
         * containers.index (Deleted)
         * containers.index (Created)

        All changes by an editor:
         * containers.index (Changed)
         */

        if (reason is not EvictionReason.Expired and not EvictionReason.TokenExpired)
            return;

        // Choose what actually happened based on the combined change types combinations listed at the beginning of this method.
        var changeType = (WatcherChangeTypes)(value) switch
        {
            WatcherChangeTypes.Deleted | WatcherChangeTypes.Created => WatcherChangeTypes.Changed, // game
            _ => (WatcherChangeTypes)(value), // editor
        };
        foreach (var container in GetCacheEvictionContainers((string)(key)))
        {
            container.SetWatcherChange(changeType);
            if (container.IsSynced)
                OnWatcherDecision(container, true);
        }
    }

    /// <summary>
    /// Refreshes all containers in the collection with newly written data from the containers.index file.
    /// Basically a single thread combination of <seealso cref="GenerateContainerCollection"/> and <seealso cref="CreateContainer"/>.
    /// </summary>
    private void RefreshContainerCollection()
    {
        var containersIndex = ParseContainersIndex();
        if (containersIndex.Count == 0)
            return;

        for (var metaIndex = 0; metaIndex < Constants.OFFSET_INDEX + COUNT_SAVES_TOTAL; metaIndex++)
        {
            var contains = containersIndex.TryGetValue(metaIndex, out var extra);
            if (metaIndex == 0)
            {
                if (contains)
                {
                    AccountContainer = GetRefreshedContainer(AccountContainer, extra!);
                    RebuildContainerFull(AccountContainer);
                }
                else
                    AccountContainer.Reset();
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
                    container = SaveContainerCollection[collectionIndex] = GetRefreshedContainer(container, extra!);

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
                else
                    container.Reset();
            }
        }
    }

    private Container GetRefreshedContainer(Container container, PlatformExtra extra)
    {
        if (container.Exists)
        {
            // Set all properties that would be set in CreateContainer().
            container.DataFile = extra!.MicrosoftBlobDataFile;
            container.MetaFile = extra!.MicrosoftBlobMetaFile;
            container.Extra = extra;

            return container;
        }

        return CreateContainer(container.MetaIndex, extra); // even a container shell has the MetaIndex set
    }

    #endregion

    // // User Identification

    #region UserIdentification

    protected override string GetUserIdentification(JObject jsonObject, string key)
    {
        if (key is "UID" && _accountId is not null)
            return _accountId;

        return base.GetUserIdentification(jsonObject, key);
    }

    protected override string[] GetIntersectionExpressionsByBase(JObject jsonObject)
    {
        if (_accountId is null)
            return base.GetIntersectionExpressionsByBase(jsonObject);
        return
        [
            Json.GetPath("INTERSECTION_PERSISTENT_PLAYER_BASE_OWNERSHIP_EXPRESSION_TYPE_OR_TYPE", jsonObject, PersistentBaseTypesEnum.HomePlanetBase, PersistentBaseTypesEnum.FreighterBase),
            Json.GetPath("INTERSECTION_PERSISTENT_PLAYER_BASE_OWNERSHIP_EXPRESSION_THIS_UID", jsonObject, _accountId),
        ];
    }

    protected override string[] GetIntersectionExpressionsByDiscovery(JObject jsonObject)
    {
        if (_accountId is null)
            return base.GetIntersectionExpressionsByDiscovery(jsonObject);
        return
        [
            Json.GetPath("INTERSECTION_DISCOVERY_DATA_OWNERSHIP_EXPRESSION_THIS_UID", jsonObject, _accountId),
        ];
    }

    protected override string[] GetIntersectionExpressionsBySettlement(JObject jsonObject)
    {
        if (_accountId is null)
            return base.GetIntersectionExpressionsByDiscovery(jsonObject);
        return
        [
            Json.GetPath("INTERSECTION_SETTLEMENT_OWNERSHIP_EXPRESSION_THIS_UID", jsonObject, _accountId),
        ];
    }

    #endregion
}
