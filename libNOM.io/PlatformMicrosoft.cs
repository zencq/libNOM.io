using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace libNOM.io;


public partial class PlatformMicrosoft : Platform
{
    #region Constant

    #region Platform Specific

    protected const int CONTAINERSINDEX_HEADER = 0xE; // 14
    protected const long CONTAINERSINDEX_FOOTER = 0x10000000; // 268435456
    protected const int CONTAINERSINDEX_OFFSET_BLOBCONTAINER_LIST = 0xC8; // 200

    protected const int BLOBCONTAINER_HEADER = 0x4; // 4
    protected const int BLOBCONTAINER_COUNT = 0x2; // 2
    protected const int BLOBCONTAINER_IDENTIFIER_LENGTH = 0x80; // 128
    protected const int BLOBCONTAINER_TOTAL_LENGTH = sizeof(int) + sizeof(int) + BLOBCONTAINER_COUNT * (BLOBCONTAINER_IDENTIFIER_LENGTH + 2 * 0x10); // 328

    protected const int META_LENGTH_KNOWN = 0x14; // 20
    protected override int META_LENGTH_TOTAL_VANILLA => 0x18; // 24
    protected override int META_LENGTH_TOTAL_WAYPOINT => 0x118; // 280

    #endregion

    #region Generated Regex

#if NETSTANDARD2_0_OR_GREATER || NET6_0
    private static readonly Regex AnchorFileRegex0 = new("containers\\.index", RegexOptions.Compiled);
#else
    [GeneratedRegex("containers\\.index", RegexOptions.Compiled)]
    private static partial Regex AnchorFileRegex0();
#endif

    #region Directory Data

    public const string ACCOUNT_PATTERN = "*_29070100B936489ABCE8B9AF3980429C";

    public static readonly string[] ANCHOR_FILE_GLOB = new[] { "containers.index" };
#if NETSTANDARD2_0_OR_GREATER || NET6_0
    public static readonly Regex[] ANCHOR_FILE_REGEX = new Regex[] { AnchorFileRegex0 };
#else
    public static readonly Regex[] ANCHOR_FILE_REGEX = new Regex[] { AnchorFileRegex0() };
#endif

    public static readonly string PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", "HelloGames.NoMansSky_bs190hzg1sesy", "SystemAppData", "wgs");

    #endregion

    #endregion

    #endregion

    #region Field

    private string? _accountId;
    private string _accountGuid = null!; // will be set when containers.index is parsed
    private FileInfo _containersIndexFile = null!; // will be set if valid
    private DateTimeOffset _lastWriteTime;
    private string _processIdentifier = null!; // will be set when containers.index is parsed
    private PlatformExtra? _settingsContainer;

    #endregion

    #region Property

    #region Configuration

    // public //

    public override PlatformEnum PlatformEnum { get; } = PlatformEnum.Microsoft;

    // protected //

    protected override string[] PlatformAnchorFileGlob { get; } = ANCHOR_FILE_GLOB;

    protected override Regex[] PlatformAnchorFileRegex { get; } = ANCHOR_FILE_REGEX;

    protected override string? PlatformArchitecture { get; } = "XB1|Final";

    // Looks like "C:\\Program Files\\WindowsApps\\HelloGames.NoMansSky_4.38.0.0_x64__bs190hzg1sesy\\Binaries\\NMS.exe"
    protected override string? PlatformProcessPath { get; } = @"bs190hzg1sesy\Binaries\NMS.exe";

    protected override string PlatformToken { get; } = "XB";

    #endregion

    #region Flags

    // public //

    public override bool Exists => base.Exists && _containersIndexFile.Exists == true; // { get; }

    public override bool HasModding { get; } = false;

    public override bool RestartToApply { get; } = true;

    // protected //

    protected override bool CanCreate { get; } = true;

    protected override bool CanRead { get; } = true;

    protected override bool CanUpdate { get; } = true;

    protected override bool CanDelete { get; } = true;

    protected override bool IsConsolePlatform { get; } = false;

    #endregion

    #endregion

    #region Getter

    #region Container

    protected override IEnumerable<Container> GetCacheEvictionContainers(string name)
    {
        if (!name.Equals("containers.index", StringComparison.OrdinalIgnoreCase))
            return Array.Empty<Container>();

        // Cache previous timestamp.
        var lastWriteTicks = _lastWriteTime.UtcTicks.GetBlobTicks();

        // Refresh will also update _lastWriteTime.
        RefreshContainerCollection();

        // Get all written container that are newer than the previous timestamp.
        return SaveContainerCollection.Where(i => i.Exists && i.LastWriteTime?.UtcTicks >= lastWriteTicks);
    }

    #endregion

    private static FileInfo GetBlobFileInfo(PlatformExtra extra, Guid guid)
    {
        return new(Path.Combine(extra.MicrosoftBlobDirectory!.FullName, guid.ToPath()));
    }


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
#if NETSTANDARD2_0
            if (directory.Name.Length == 49 && directory.Name.EndsWith(ACCOUNT_PATTERN.Substring(1)) && directory.Name.Substring(0, 16).All("0123456789ABCDEFabcdef".Contains))
                _accountId = System.Convert.ToInt64(directory.Name.Split('_')[0], 16).ToString();
#elif NETSTANDARD2_1 || NET6_0
            if (directory.Name.Length == 49 && directory.Name.EndsWith(ACCOUNT_PATTERN[1..]) && directory.Name[..16].All("0123456789ABCDEFabcdef".Contains))
                _accountId = System.Convert.ToInt64(directory.Name.Split('_')[0], 16).ToString();
#else
            if (directory.Name.Length == 49 && directory.Name.EndsWith(ACCOUNT_PATTERN[1..]) && directory.Name[..16].All(char.IsAsciiHexDigit))
                _accountId = System.Convert.ToInt64(directory.Name.Split('_')[0], 16).ToString();
#endif

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

    private protected override Container CreateContainer(int metaIndex, PlatformExtra? extra)
    {
        if (extra is null)
            return new Container(metaIndex);

        return new Container(metaIndex)
        {
            DataFile = extra.MicrosoftBlobDataFile,
            MetaFile = extra.MicrosoftBlobMetaFile,
            /// Additional values will be set in <see cref="UpdateContainerWithMetaInformation"/> and <see cref="UpdateContainerWithDataInformation"/>.
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
        /**
        global index data
         0. HEADER (14)                     (  4)
         1. NUMBER OF BLOB CONTAINERS       (  8)
         2. PROCESS IDENTIFIER LENGTH (44)  (  4)
         3. PROCESS IDENTIFIER              ( 88) (UTF-16)
         4. LAST MODIFIED TIME              (  8)
         5. SYNC STATE                      (  4) (0 = ?, 1 = ?, 2 = MODIFIED, 3 = SYNCED)
         6. ACCOUNT IDENTIFIER LENGTH (36)  (  4)
         7. ACCOUNT IDENTIFIER              ( 72) (UTF-16)
         8. FOOTER (268435456)              (  8)

        blob container index loop
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

        blob container data
         0. HEADER (4)                      (  4)
         1. NUMBER OF BLOBS (2)             (  4)
         2. DATA BLOB IDENTIFIER            ( 80) (UTF-16)
         3. DATA FILE CLOUD                 ( 16) (GUID)
         4. DATA FILE LOCAL                 ( 16) (GUID)
         5. META BLOB IDENTIFIER            ( 80) (UTF-16)
         6. META FILE CLOUD                 ( 16) (GUID)
         7. META FILE LOCAL                 ( 16) (GUID)
        */

        ReadOnlySpan<byte> bytesIndex = File.ReadAllBytes(_containersIndexFile.FullName);
        Dictionary<int, PlatformExtra> result = new();
        int offsetIndex = 0;

        if (bytesIndex.Cast<int>(0) is int header && header != CONTAINERSINDEX_HEADER)
            throw new InvalidDataException($"Wrong header in containers.index file! Expected {CONTAINERSINDEX_HEADER} but got {header}.");

        offsetIndex += 12;
        offsetIndex += bytesIndex.ReadString(out _processIdentifier, offsetIndex);

        _lastWriteTime = DateTimeOffset.FromFileTime(bytesIndex.Cast<long>(offsetIndex)).ToLocalTime();

        offsetIndex += 12;
        offsetIndex += bytesIndex.ReadString(out _accountGuid, offsetIndex) + 8; // Marshal.SizeOf(CONTAINERSINDEX_FOOTER);

        for (var i = 0; i < bytesIndex.Cast<long>(4); i++) // container count
        {
            offsetIndex += bytesIndex.ReadString(out var saveIdentifier, offsetIndex) * 2; // saveIdentifier two times in a row
            offsetIndex += bytesIndex.ReadString(out var syncTime, offsetIndex);

            var directoryGuid = bytesIndex.GetGuid(offsetIndex + 5);
            var extra = new PlatformExtra()
            {
                // Read long as it has 8 bytes but other platforms only 4.
                SizeDisk = (uint)(bytesIndex.Cast<long>(offsetIndex + 37)),

                LastWriteTime = DateTimeOffset.FromFileTime(bytesIndex.Cast<long>(offsetIndex + 21)).ToLocalTime(),

                MicrosoftSyncTime = syncTime,
                MicrosoftBlobContainerExtension = bytesIndex[offsetIndex],
                MicrosoftSyncState = (MicrosoftBlobSyncStateEnum)(bytesIndex.Cast<int>(offsetIndex + 1)),
                MicrosoftBlobDirectoryGuid = directoryGuid,

                MicrosoftBlobDirectory = new DirectoryInfo(Path.Combine(Location!.FullName, directoryGuid.ToPath())),
            };

            offsetIndex += 45;

            // Ignore if already marked as deleted.
            if (extra.MicrosoftBlobDirectory.Exists && extra.MicrosoftSyncState != MicrosoftBlobSyncStateEnum.Deleted)
            {
                var blobContainerIndex = extra.MicrosoftBlobContainerFile!;
                var files = new HashSet<FileInfo>();

                // In case the blob container extension does not match the containers.index value, try all existing ones until a data file is found.
                if (blobContainerIndex.Exists)
                {
                    files.Add(blobContainerIndex);
                }
                else
                {
                    foreach (var file in extra.MicrosoftBlobDirectory.GetFiles("container.*"))
                        files.Add(file);
                }

                // Start with the presumably newest one.
                foreach (var blobContainer in files.OrderByDescending(j => j.Extension))
                {
                    ReadOnlySpan<byte> bytesBlobContainer = File.ReadAllBytes(blobContainer.FullName);
                    int offsetBlobContainer = 8; // start after header

                    if (bytesBlobContainer.Length != BLOBCONTAINER_TOTAL_LENGTH || bytesBlobContainer.Cast<int>(0) != CONTAINERSINDEX_HEADER)
                        continue;

                    for (var k = 0; k < bytesBlobContainer.Cast<int>(4); k++) // blob count
                    {
                        offsetBlobContainer += bytesBlobContainer.ReadString(out var blobIdentifier, offsetBlobContainer, BLOBCONTAINER_IDENTIFIER_LENGTH);

                        // Second Guid is the one to use as the first one is propably the current name in the cloud.
                        var blobFile = GetBlobFileInfo(extra, bytesBlobContainer.GetGuid(offsetBlobContainer + 16));
                        var blobSyncGuid = bytesBlobContainer.GetGuid(offsetBlobContainer);

                        offsetBlobContainer += 32; // 2 * sizeof(Guid)

#if NETSTANDARD2_0
                        if (blobIdentifier.ToString() == "data")
                        {
                            extra = extra with
                            {
                                MicrosoftBlobDataFile = blobFile,
                                MicrosoftBlobDataSyncGuid = blobSyncGuid,
                            };
                            continue;
                        }
                        if (blobIdentifier.ToString() == "meta")
                        {
                            extra = extra with
                            {
                                Size = blobFile.Exists ? (uint)(blobFile.Length) : 0,

                                MicrosoftBlobMetaFile = blobFile,
                                MicrosoftBlobMetaSyncGuid = blobSyncGuid,
                            };
                            continue;
                        }
#else
                        if (blobIdentifier == "data")
                        {
                            extra = extra with
                            {
                                MicrosoftBlobDataFile = blobFile,
                                MicrosoftBlobDataSyncGuid = blobSyncGuid,
                            };
                            continue;
                        }
                        if (blobIdentifier == "meta")
                        {
                            extra = extra with
                            {
                                MicrosoftBlobMetaFile = blobFile,
                                MicrosoftBlobMetaSyncGuid = blobSyncGuid,
                            };
                            continue;
                        }
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
            }

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

    #endregion

    #region Load

    protected override ReadOnlySpan<byte> LoadContainer(Container container)
    {
        // Any incompatibility will be set again while loading.
        container.ClearIncompatibility();

        if (container.Exists)
        {
            // Loads all meta information into the extra property.
            LoadMeta(container);

            var data = LoadData(container);
            if (data.IsTrimEmpty())
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

    protected override void UpdateContainerWithMetaInformation(Container container, ReadOnlySpan<byte> raw, ReadOnlySpan<uint> converted)
    {
        //  0. BASE VERSION         (  4)
        //  1. GAME MODE            (  2)
        //  1. SEASON               (  2)
        //  2. TOTAL PLAY TIME      (  4)
        //  3. EMPTY                (  4)
        //  4. DECOMPRESSED SIZE    (  4)

        //  5. EMPTY                (  4)
        //                          ( 24)

        //  5. SAVE NAME            (128) // may contain additional junk data after null terminator
        // 37. SAVE SUMMARY         (128) // may contain additional junk data after null terminator
        // 69. DIFFICULTY PRESET    (  1)
        // 69. EMPTY                (  3) // may contain additional junk data
        //                          (280)

        var season = raw.Cast<ushort>(6);

        // Vanilla data always available.
        container.Extra = container.Extra with
        {
            Bytes = raw.Slice(META_LENGTH_KNOWN).ToArray(),
            Size = (uint)(raw.Length),
            SizeDecompressed = converted[4],
            BaseVersion = (int)(converted[0]),
            GameMode = raw.Cast<ushort>(4),
            Season = (ushort)(season == ushort.MaxValue ? 0 : season),
            TotalPlayTime = converted[2],
        };

        // Extended data since Waypoint.
        if (raw.Length == META_LENGTH_TOTAL_WAYPOINT)
        {
            container.Extra = container.Extra with
            {
                SaveName = converted.Slice(5, 32).GetSaveRenamingString(),
                SaveSummary = converted.Slice(37, 32).GetSaveRenamingString(),
                DifficultyPreset = raw[276],
            };
        }

        container.SaveVersion = Calculate.CalculateSaveVersion(container);
    }

    protected override ReadOnlySpan<byte> DecompressData(Container container, ReadOnlySpan<byte> data)
    {
        _ = LZ4.Decode(data, out var target, (int)(container.Extra.SizeDecompressed));
        return target;
    }

    protected override void UpdateContainerWithDataInformation(Container container, ReadOnlySpan<byte> raw, ReadOnlySpan<byte> converted)
    {
        // Removed SizeDisk as it is the sum of data and meta for this platform.
        container.Extra = container.Extra with
        {
            SizeDecompressed = (uint)(converted.Length),
        };
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

                    MicrosoftBlobDirectory = container.Extra.MicrosoftBlobDirectory,
                };

                // Create blob container with new file information.
                var blob = PrepareBlobContainer(container);

                // Write the previously created files and delete the old ones.
                WriteMeta(container, meta);
                if (cache.MicrosoftBlobMetaFile is not null)
                    File.Delete(cache.MicrosoftBlobMetaFile.FullName);

                WriteData(container, data);
                if (cache.MicrosoftBlobDataFile is not null)
                    File.Delete(cache.MicrosoftBlobDataFile.FullName);

                WriteBlob(container, blob);
                if (cache.MicrosoftBlobContainerFile is not null)
                    File.Delete(cache.MicrosoftBlobContainerFile.FullName);
            }

            if (Settings.SetLastWriteTime)
            {
                _lastWriteTime = writeTime;
                container.LastWriteTime = _lastWriteTime.GetBlobTime();

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

    protected override ReadOnlySpan<byte> CompressData(Container container, ReadOnlySpan<byte> data)
    {
        _ = LZ4.Encode(data, out var target);
        return target;
    }

    protected override Span<uint> CreateMeta(Container container, ReadOnlySpan<byte> data)
    {
        var buffer = new byte[GetMetaSize(container)];

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

            writer.Write(container.Extra.SizeDecompressed); // 4

            // Extended data since Waypoint.
            if (container.MetaFormat >= MetaFormatEnum.Waypoint)
            {
                // Append cached bytes and overwrite afterwards.
                writer.Write(container.Extra.Bytes ?? Array.Empty<byte>()); // 260

                writer.Seek(META_LENGTH_KNOWN, SeekOrigin.Begin);
                writer.Write(container.SaveName.GetSaveRenamingBytes()); // 128

                writer.Seek(META_LENGTH_KNOWN + Constants.SAVE_RENAMING_LENGTH, SeekOrigin.Begin);
                writer.Write(container.SaveSummary.GetSaveRenamingBytes()); // 128

                writer.Seek(META_LENGTH_KNOWN + 2 * Constants.SAVE_RENAMING_LENGTH, SeekOrigin.Begin);
                writer.Write((byte)(container.GameDifficulty)); // 1
            }
            else
            {
                writer.Write(container.Extra.Bytes ?? Array.Empty<byte>()); // 4
            }
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
            MicrosoftBlobDataFile = container.DataFile = GetBlobFileInfo(container.Extra, dataGuid),
            MicrosoftBlobMetaFile = container.MetaFile = GetBlobFileInfo(container.Extra, metaGuid),
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
    private static void WriteBlob(Container container, byte[] blob)
    {
        File.WriteAllBytes(container.Extra.MicrosoftBlobContainerFile!.FullName, blob);
    }

    /// <summary>
    /// Creates and writes the containers.index file content to disk.
    /// </summary>
    private void WriteContainersIndex()
    {
        var hasSettings = _settingsContainer is not null;

        var collection = SaveContainerCollection.Where(i => i.Extra.MicrosoftBlobDirectoryGuid is not null);
        var count = (long)(collection.Count() + (HasAccountData ? 1 : 0) + (hasSettings ? 1 : 0));

        // Longest name (e.g. Slot10Manual) has a total length of 0x8C (140) and any leftover for short ones will be cut off.
        var buffer = new byte[CONTAINERSINDEX_OFFSET_BLOBCONTAINER_LIST + (count * 0x8C)];

        using (var writer = new BinaryWriter(new MemoryStream(buffer)))
        {
            writer.Write(CONTAINERSINDEX_HEADER);
            writer.Write(count);
            writer.Write(_processIdentifier.Length);
            writer.Write(_processIdentifier.GetUnicodeBytes());
            writer.Write(_lastWriteTime.ToUniversalTime().ToFileTime());
            writer.Write((int)(MicrosoftIndexSyncStateEnum.Modified));
            writer.Write(_accountGuid.Length);
            writer.Write(_accountGuid.GetUnicodeBytes());
            writer.Write(CONTAINERSINDEX_FOOTER);

            if (HasAccountData)
            {
                // Make sure to get the latest data.
                AccountContainer.RefreshFileInfo();

                for (var i = 0; i < 2; i++)
                {
                    writer.Write(AccountContainer.Identifier.Length);
                    writer.Write(AccountContainer.Identifier.GetUnicodeBytes());
                }
                writer.Write(AccountContainer.Extra.MicrosoftSyncTime!.Length);
                writer.Write(AccountContainer.Extra.MicrosoftSyncTime!.GetUnicodeBytes());
                writer.Write(AccountContainer.Extra.MicrosoftBlobContainerExtension!.Value);
                writer.Write((int)(AccountContainer.Extra.MicrosoftSyncState!.Value));
                writer.Write(AccountContainer.Extra.MicrosoftBlobDirectoryGuid!.Value.ToByteArray());
                writer.Write(AccountContainer.LastWriteTime!.Value.ToUniversalTime().ToFileTime());
                writer.Write((long)(0));
                writer.Write((AccountContainer.DataFile?.Exists == true ? AccountContainer.DataFile.Length : 0) + (AccountContainer.MetaFile?.Exists == true ? AccountContainer.MetaFile.Length : 0));
            }

            if (hasSettings)
            {
                _settingsContainer!.MicrosoftBlobDataFile?.Refresh();
                _settingsContainer!.MicrosoftBlobMetaFile?.Refresh();

                for (var i = 0; i < 2; i++)
                {
                    writer.Write("Settings".Length);
                    writer.Write("Settings".GetUnicodeBytes());
                }
                writer.Write(_settingsContainer!.MicrosoftSyncTime!.Length);
                writer.Write(_settingsContainer!.MicrosoftSyncTime!.GetUnicodeBytes());
                writer.Write(_settingsContainer!.MicrosoftBlobContainerExtension!.Value);
                writer.Write((int)(_settingsContainer!.MicrosoftSyncState!.Value));
                writer.Write(_settingsContainer!.MicrosoftBlobDirectoryGuid!.Value.ToByteArray());
                writer.Write(_settingsContainer!.LastWriteTime!.Value.ToUniversalTime().ToFileTime());
                writer.Write((long)(0));
                writer.Write((_settingsContainer!.MicrosoftBlobDataFile?.Exists == true ? _settingsContainer!.MicrosoftBlobDataFile!.Length : 0) + (_settingsContainer!.MicrosoftBlobMetaFile?.Exists == true ? _settingsContainer!.MicrosoftBlobMetaFile!.Length : 0));
            }

            foreach (var container in collection)
            {
                // Make sure to get the latest data.
                container.RefreshFileInfo();

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
                writer.Write((long)(0));
                writer.Write((container.DataFile?.Exists == true ? container.DataFile!.Length : 0) + (container.MetaFile?.Exists == true ? container.MetaFile!.Length : 0));
            }

            buffer = buffer.AsSpan().Slice(0, (int)(writer.BaseStream.Position)).ToArray();
        }

        // Write and refresh the containers.index file.
        File.WriteAllBytes(_containersIndexFile.FullName, buffer);
        _containersIndexFile.Refresh();
    }

    #endregion

    // // File Operation

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

                // Properties required to properly build the container below (order is important).
                Destination.GameVersion = Source.GameVersion;
                Destination.SaveName = Source.SaveName;
                Destination.SaveSummary = Source.SaveSummary;
                Destination.TotalPlayTime = Source.TotalPlayTime;

                Destination.BaseVersion = Source.BaseVersion;
                Destination.GameMode = Source.GameMode;
                Destination.Season = Source.Season;

                Destination.SaveVersion = Source.SaveVersion;

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
        Guard.IsTrue(CanDelete);

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
                container.LastWriteTime = _lastWriteTime.GetBlobTime();
            }

            container.Reset();
            container.IncompatibilityTag = Constants.INCOMPATIBILITY_004;

            container.DataFile = container.MetaFile = null; // set to null as it constantly changes anyway
            container.Extra = container.Extra with { MicrosoftSyncState = MicrosoftBlobSyncStateEnum.Deleted };
        }

        if (write)
        {
            WriteContainersIndex();
        }

        EnableWatcher();
    }

    #endregion

    // TODO Transfer Refactoring

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

                // Properties required to properly build the container below.
                Destination.BaseVersion = Source.BaseVersion;
                Destination.GameVersion = Source.GameVersion;
                Destination.Season = Source.Season;

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

    protected override void CreatePlatformExtra(Container destination, Container source)
    {
        destination.Extra = new()
        {
            MicrosoftBlobDirectoryGuid = Guid.NewGuid(),
            MicrosoftBlobContainerExtension = 0,
            LastWriteTime = source.LastWriteTime,
            Bytes = new byte[(source.IsVersion400Waypoint ? META_LENGTH_TOTAL_WAYPOINT : META_LENGTH_TOTAL_VANILLA) - META_LENGTH_KNOWN],
            MicrosoftSyncState = MicrosoftBlobSyncStateEnum.Created,
        };
    }

    #endregion

    private void ExecuteCanCreate(Container Destination)
    {
        // New directory Guid was set in the method before this was called.
        var directory = new DirectoryInfo(Path.Combine(Location!.FullName, Destination.Extra.MicrosoftBlobDirectoryGuid!.Value.ToPath()));

        // Update container and its extra with dummy data.
        Destination.Extra = Destination.Extra with
        {
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

    // // FileSystemWatcher

    #region FileSystemWatcher

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
                    if (AccountContainer.Exists)
                    {
                        // Set all properties that would be set in CreateContainer().
                        AccountContainer.DataFile = extra!.MicrosoftBlobDataFile;
                        AccountContainer.MetaFile = extra!.MicrosoftBlobMetaFile;
                        AccountContainer.Extra = extra;
                    }
                    else
                    {
                        AccountContainer = CreateContainer(metaIndex, extra);
                    }
                    RebuildContainerFull(AccountContainer);
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
                        container.DataFile = extra!.MicrosoftBlobDataFile;
                        container.MetaFile = extra!.MicrosoftBlobMetaFile;
                        container.Extra = extra;
                    }
                    else
                    {
                        container = SaveContainerCollection[collectionIndex] = CreateContainer(metaIndex, extra);
                    }

                    // Only build full if container was already loaded.
                    if (Settings.LoadingStrategy < LoadingStrategyEnum.Full && !container.IsLoaded)
                    {
                        RebuildContainerHollow(container);
                    }
                    else
                    {
                        RebuildContainerFull(container);
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

        return base.GetUserIdentification(jsonObject, key);
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
