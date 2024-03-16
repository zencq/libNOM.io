﻿using System.Collections.Concurrent;

using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;

namespace libNOM.io;


public partial class PlatformMicrosoft : Platform
{
    #region Constructor

    public PlatformMicrosoft() : base() { }

    public PlatformMicrosoft(string path) : base(path) { }

    public PlatformMicrosoft(string path, PlatformSettings platformSettings) : base(path, platformSettings) { }

    public PlatformMicrosoft(DirectoryInfo directory) : base(directory) { }

    public PlatformMicrosoft(DirectoryInfo directory, PlatformSettings platformSettings) : base(directory, platformSettings) { }

    #endregion

    #region Initialize

#if !NETSTANDARD2_0
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0057: Use range operator", Justification = "The range operator is not supported in netstandard2.0 and Slice() has no performance penalties.")]
#endif
    protected override void InitializePlatformSpecific()
    {
#if NETSTANDARD2_0_OR_GREATER || NET6_0
        if (Location.Name.Length == 49 && Location.Name.EndsWith(ACCOUNT_PATTERN.Substring(1)) && Location.Name.Substring(0, 16).All("0123456789ABCDEFabcdef".Contains))
            _uid = System.Convert.ToInt64(Location.Name.Split('_')[0], 16).ToString();
#else
        if (Location.Name.Length == 49 && Location.Name.EndsWith(ACCOUNT_PATTERN[1..]) && Location.Name[..16].All(char.IsAsciiHexDigit))
            _uid = System.Convert.ToInt64(Location.Name.Split('_')[0], 16).ToString();
#endif

        _containersindex = new FileInfo(Path.Combine(Location.FullName, "containers.index"));
    }

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

    #region Process

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
