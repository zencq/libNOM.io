using System.Collections.Concurrent;

using CommunityToolkit.Diagnostics;

using libNOM.io.Settings;

namespace libNOM.io;


// This partial class contains initialization related code.
public partial class PlatformMicrosoft : Platform
{
    #region Constructor

    public PlatformMicrosoft() : base() { }

    public PlatformMicrosoft(string? path) : base(path) { }

    public PlatformMicrosoft(string? path, PlatformSettings? platformSettings) : base(path, platformSettings) { }

    public PlatformMicrosoft(PlatformSettings? platformSettings) : base(platformSettings) { }

    public PlatformMicrosoft(DirectoryInfo? directory) : base(directory) { }

    public PlatformMicrosoft(DirectoryInfo? directory, PlatformSettings? platformSettings) : base(directory, platformSettings) { }

    #endregion

    #region Initialize

    protected override void InitializePlatformSpecific()
    {
#if NETSTANDARD2_0_OR_GREATER || NET6_0
        Func<char, bool> IsAsciiHexDigit = "0123456789ABCDEFabcdef".Contains;
#else
        Func<char, bool> IsAsciiHexDigit = char.IsAsciiHexDigit;
#endif

        if (Location.Name.Length == 49 && Location.Name.EndsWith(ACCOUNT_PATTERN[1..]) && Location.Name[..16].All(IsAsciiHexDigit))
            _uid = System.Convert.ToInt64(Location.Name.Split('_')[0], 16).ToString();

        _containersindex = new FileInfo(Path.Combine(Location.FullName, "containers.index"));
    }

    protected override IEnumerable<Container> GenerateContainerCollection()
    {
        var bag = new ConcurrentBag<Container>();
        var containersIndex = ParseContainersIndex();

        if (containersIndex.Count != 0)
        {
            var tasks = Enumerable.Range(0, Constants.OFFSET_INDEX + COUNT_SAVES_TOTAL).Select((metaIndex) => Task.Run(() =>
            {
                _ = containersIndex.TryGetValue(metaIndex, out var extra);
                switch (metaIndex)
                {
                    case 0:
                        AccountContainer = InitializeContainer(metaIndex, extra);
                        break;
                    case 1:
                        _settingsContainer = extra; // just caching it to be able to write it again
                        break;
                    default:
                        bag.Add(InitializeContainer(metaIndex, extra));
                        break;
                }
            }));
            Task.WaitAll(tasks.ToArray());
        }

        return bag;
    }

    private protected override Container CreateContainer(int metaIndex, ContainerExtra? extra)
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
    private Dictionary<int, ContainerExtra> ParseContainersIndex()
    {
        var offset = ParseGlobalIndex(out var bytes);
        Dictionary<int, ContainerExtra> result = [];

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

    private int ParseBlobContainerIndex(ReadOnlySpan<byte> bytes, int offset, out string saveIdentifier, out ContainerExtra extra)
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

    private static ContainerExtra ParseBlobContainer(ContainerExtra extra)
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

        // Start with the presumably newest one.
        foreach (var blobContainer in GetPossibleBlobContainers(extra).OrderByDescending(i => i.Extension))
        {
            ReadOnlySpan<byte> bytes = blobContainer.ReadAllBytes();
            var offset = 8; // start after header

            if (bytes.Length != BLOBCONTAINER_TOTAL_LENGTH || bytes.Cast<int>(0) != BLOBCONTAINER_HEADER)
                continue;

            for (var j = 0; j < bytes.Cast<int>(4); j++) // blob count
            {
                offset += bytes.ReadString(offset, BLOBCONTAINER_IDENTIFIER_LENGTH, out var blobIdentifier);
                offset += UpdateExtraWithBlobInformation(blobIdentifier, bytes[offset..], extra, out extra);
            }

            // Update extension in case the read one was not found and break the loop.
            if (extra.MicrosoftBlobDataFile?.Exists == true)
            {
                extra = extra with { MicrosoftBlobContainerExtension = System.Convert.ToByte(blobContainer.Extension.TrimStart('.')) };
                break;
            }
        }

        // Mark as deleted if there is no existing data file.
        if (extra.MicrosoftBlobDataFile?.Exists != true)
            extra = extra with { MicrosoftSyncState = MicrosoftBlobSyncStateEnum.Deleted };

        return extra;
    }

    private static HashSet<FileInfo> GetPossibleBlobContainers(ContainerExtra extra)
    {
        var files = new HashSet<FileInfo>();

        // In case the blob container extension does not match the containers.index value, try all existing ones until a data file is found.
        if (extra.MicrosoftBlobContainerFile!.Exists)
            files.Add(extra.MicrosoftBlobContainerFile!);
        else
            foreach (var file in extra.MicrosoftBlobDirectory!.EnumerateFiles("container.*"))
                files.Add(file);

        return files;
    }

    private static int UpdateExtraWithBlobInformation(ReadOnlySpan<char> blobIdentifier, ReadOnlySpan<byte> bytes, ContainerExtra extra, out ContainerExtra result)
    {
        // Second Guid is the one to use as the first one is probably the current name in the cloud.
        var blobSyncGuid = bytes.GetGuid();
        var blobFile = extra.MicrosoftBlobDirectory!.GetBlobFileInfo(bytes.GetGuid(16));

        result = blobIdentifier switch
        {
            "data" => extra with { MicrosoftBlobDataFile = blobFile, MicrosoftBlobDataSyncGuid = blobSyncGuid },
            "meta" => extra with { MicrosoftBlobMetaFile = blobFile, MicrosoftBlobMetaSyncGuid = blobSyncGuid },
            _ => extra,
        };

        return 32; // 2 * sizeof(Guid)
    }

    #endregion

    #region Process

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
            Bytes = disk[META_LENGTH_KNOWN..].ToArray(),
            MetaLength = (uint)(disk.Length),
            BaseVersion = (int)(decompressed[0]),
            GameMode = disk.Cast<ushort>(4),
            Season = disk.Cast<ushort>(6) is var season && season == ushort.MaxValue ? (ushort)(0) : season,
            TotalPlayTime = decompressed[2],
        };

        if (container.IsAccount)
            container.GameVersion = Meta.GameVersion.Get(this, disk.Length, Constants.META_FORMAT_2);

        // Extended data since Waypoint.
        UpdateContainerWithWaypointMetaInformation(container, disk);

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

    #endregion
}
