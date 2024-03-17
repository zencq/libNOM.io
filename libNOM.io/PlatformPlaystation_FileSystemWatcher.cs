namespace libNOM.io;


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
        for (var metaIndex = 0; metaIndex < Constants.OFFSET_INDEX + COUNT_SAVES_TOTAL; metaIndex++)
            if (metaIndex == 0)
            {
                // Reset bytes to trigger to read the file again.
                AccountContainer.Extra = new PlatformExtra
                {
                    MetaFormat = MetaFormatEnum.Unknown,
                    Bytes = null,
                };
                RebuildContainerFull(AccountContainer);
            }
            else if (metaIndex > 1) // skip index 1
            {
                var collectionIndex = metaIndex - Constants.OFFSET_INDEX;
                var container = SaveContainerCollection[collectionIndex];

                // Reset bytes as trigger to read the file again.
                container.Extra = new PlatformExtra
                {
                    MetaFormat = MetaFormatEnum.Unknown,
                    Bytes = null,
                };

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
    }

    #endregion
}
