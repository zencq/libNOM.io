using System.Text;

using CommunityToolkit.Diagnostics;

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

    #region UserIdentification

    protected override string GetUserIdentification(JObject jsonObject, string key)
    {
        if (key is "UID" && _uid is not null)
            return _uid;

        return base.GetUserIdentification(jsonObject, key);
    }

    #endregion
}
