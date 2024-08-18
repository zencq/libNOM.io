namespace libNOM.io;


// This partial class contains FileSystemWatcher related code.
public partial class PlatformPlaystation : Platform
{
    #region Getter

    protected override IEnumerable<Container> GetCacheEvictionContainers(string name)
    {
        if (_usesSaveStreaming)
            return base.GetCacheEvictionContainers(name);

        if (!name.Equals("memory.dat", StringComparison.OrdinalIgnoreCase))
            return [];

        // Cache previous timestamp.
        var lastWriteTicks = _lastWriteTime!.NullifyTicks(4)!.Value.UtcTicks;

        // Refresh will also update _lastWriteTime.
        RefreshContainerCollection();

        // Get all written container that are newer than the previous timestamp.
        return SaveContainerCollection.Where(i => i.Exists && i.LastWriteTime?.UtcTicks >= lastWriteTicks);
    }

    #endregion

    #region Refresh

    /// <summary>
    /// Refreshes all containers in the collection with newly written data from the memory.dat file.
    /// </summary>
    private void RefreshContainerCollection()
    {
        foreach (var metaIndex in Enumerable.Range(0, Constants.OFFSET_INDEX + MAX_SAVE_TOTAL))
            switch (metaIndex)
            {
                case 0:
                    UpdateAndRebuildContainer(AccountContainer!);
                    break;
                case 1:
                    break;
                default:
                    UpdateAndRebuildContainer(SaveContainerCollection[metaIndex - Constants.OFFSET_INDEX]);
                    break;
            }
    }

    private void UpdateAndRebuildContainer(Container container)
    {
        // Reset bytes as trigger to read the file again.
        container.Extra = new ContainerExtra
        {
            Bytes = null,
        };

        // Only rebuild full if container was already loaded and not synced (to not overwrite pending watcher changes).
        if (container.IsLoaded)
        {
            if (container.IsAccount || container.IsSynced)
                RebuildContainerFull(container);
        }
        else
            RebuildContainerHollow(container);

        GenerateBackupCollection(container);
    }

    #endregion
}
