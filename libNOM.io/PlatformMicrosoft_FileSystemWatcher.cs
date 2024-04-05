using Microsoft.Extensions.Caching.Memory;

namespace libNOM.io;


// This partial class contains FileSystemWatcher related code.
public partial class PlatformMicrosoft : Platform
{
    // Accessor

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

    #region Events

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

    #endregion

    #region Refresh

    /// <summary>
    /// Refreshes all containers in the collection with newly written data from the containers.index file.
    /// Basically a single thread combination of <seealso cref="GenerateContainerCollection"/> and <seealso cref="CreateContainer"/>.
    /// </summary>
    private void RefreshContainerCollection()
    {
        var containersIndex = ParseContainersIndex();

        foreach (var metaIndex in Enumerable.Range(0, Constants.OFFSET_INDEX + COUNT_SAVES_TOTAL))
            switch (metaIndex)
            {
                case 0:
                    AccountContainer = GetOrResetContainer(containersIndex, AccountContainer!);
                    break;
                case 1:
#if NETSTANDARD2_0
                    _ = containersIndex.TryGetValue(metaIndex, out var extra);
                    _settingsContainer = extra;
#else
                    _settingsContainer = containersIndex.GetValueOrDefault(metaIndex);
#endif
                    break;
                default:
                    var collectionIndex = metaIndex - Constants.OFFSET_INDEX;
                    SaveContainerCollection[collectionIndex] = GetOrResetContainer(containersIndex, SaveContainerCollection[collectionIndex]);
                    break;
            }
    }

    private Container GetOrResetContainer(Dictionary<int, ContainerExtra> containersIndex, Container container)
    {
        if (containersIndex.TryGetValue(container.MetaIndex, out var extra))
        {
            container = GetRefreshedContainer(container, extra);

            // Only rebuild full if container was already loaded and not synced (to not overwrite pending watcher changes).
            if (container.IsLoaded)
            {
                if (container.IsAccount || container.IsSynced) // account data are always loaded
                    RebuildContainerFull(container);
            }
            else
                RebuildContainerHollow(container);

            GenerateBackupCollection(container);
        }
        else
            container.Reset();

        return container;
    }

    private Container GetRefreshedContainer(Container container, ContainerExtra extra)
    {
        if (container.Exists)
        {
            // Set all properties that would be set in CreateContainer().
            container.DataFile = extra.MicrosoftBlobDataFile;
            container.MetaFile = extra.MicrosoftBlobMetaFile;
            container.Extra = extra;

            return container;
        }

        return CreateContainer(container.MetaIndex, extra); // even a container shell has the MetaIndex set
    }

    #endregion
}
