using CommunityToolkit.Diagnostics;

using libNOM.io.Delegates;
using libNOM.io.Interfaces;

namespace libNOM.io;


/// <summary>
/// Holds all information about a single save.
/// </summary>
// This partial class contains some general code.
public partial class Container : IContainer
{
    #region Delegate

    public NotifyBackupCreatedEventHandler BackupCreatedCallback { get; set; } = delegate { };

    public NotifyBackupRestoredEventHandler BackupRestoredCallback { get; set; } = delegate { };

    public NotifyWriteEventHandler WriteCallback { get; set; } = delegate { };

    #endregion

    #region Constructor

    public Container(int metaIndex, Platform platform) : this(metaIndex, platform, new()) { }

    internal Container(int metaIndex, Platform platform, PlatformExtra extra)
    {
        CollectionIndex = metaIndex - Constants.OFFSET_INDEX;
        Extra = extra;
        MetaIndex = metaIndex;
        Platform = platform;

        PersistentStorageSlot = MetaIndex == 0 ? StoragePersistentSlotEnum.AccountData : (StoragePersistentSlotEnum)(MetaIndex);
        SaveType = (SaveTypeEnum)(CollectionIndex % 2);
        SlotIndex = CollectionIndex / 2; // integer division

        Identifier = MetaIndex == 0 ? "AccountData" : $"Slot{SlotIndex + 1}{SaveType}";
    }

    #endregion

    #region ToString

    public override string ToString()
    {
        var e = Exists ? (IsBackup ? "Backup" : (IsAccount ? "Account" : (IsSave ? "Save" : null))) : null;
        if (e is not null)
            e = $" // {e}";

        return $"{nameof(Container)} {PersistentStorageSlot} {Identifier}{(e ?? string.Empty)}";
    }

    #endregion

    // internal //

    internal void ClearIncompatibility()
    {
        IncompatibilityException = null;
        IncompatibilityTag = null;
    }

    /// <summary>
    /// Refreshes all <see cref="FileInfo"/> used for this save.
    /// </summary>
    internal void RefreshFileInfo()
    {
        DataFile?.Refresh();
        MetaFile?.Refresh();

        Extra.MicrosoftBlobContainerFile?.Refresh();
        Extra.MicrosoftBlobDirectory?.Refresh();
        Extra.MicrosoftBlobDataFile?.Refresh();
        Extra.MicrosoftBlobMetaFile?.Refresh();
    }

    /// <summary>
    /// Resets the container to the default state except for properties set in ctor (except Extra).
    /// </summary>
    internal void Reset()
    {
        _gameVersion = GameVersionEnum.Unknown;
        _exists = null;
        _jsonObject = null;
        _saveVersion = -1;

        BackupCollection.Clear();
        Extra = new();
        IsSynced = true;
        UserIdentification = null;
        UnknownKeys.Clear();

        ClearIncompatibility();
        RefreshFileInfo();
        ResolveWatcherChange();
    }

    /// <summary>
    /// Resets the property that a watcher changed this container.
    /// </summary>
    internal void ResolveWatcherChange()
    {
        HasWatcherChange = false;
        WatcherChangeType = null;
    }

    internal bool IsVersion(GameVersionEnum versionEnum)
    {
        return GameVersion >= versionEnum;
    }

    // private //

    private void ThrowHelperIsLoaded()
    {
        if (!IsLoaded)
        {
            var message = IsCompatible ? "Container is not loaded." : $"Container cannot be loaded due to incompatibilities: {IncompatibilityException?.Message ?? IncompatibilityTag}";
            ThrowHelper.ThrowInvalidOperationException(message);
        }
    }
}
