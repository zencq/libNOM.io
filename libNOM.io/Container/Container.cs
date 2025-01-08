using CommunityToolkit.Diagnostics;

using libNOM.io.Delegates;

using Newtonsoft.Json.Linq;

namespace libNOM.io;


/// <summary>
/// Holds all information about a single save.
/// </summary>
// This partial class contains some general code.
public partial class Container : IContainer
{
    #region Field

    private JObject? _jsonObject;

    #endregion

    #region Delegate

    public NotifyBackupCreatedEventHandler BackupCreatedCallback { get; set; } = delegate { };

    public NotifyJsonChangedEventHandler JsonChangedCallback { get; set; } = delegate { };

    public NotifyPropertiesChangedEventHandler PropertiesChangedCallback { get; set; } = delegate { };

    #endregion

    #region Constructor

    public Container(int metaIndex, IPlatform platform) : this(metaIndex, platform, new()) { }

    internal Container(int metaIndex, IPlatform platform, ContainerExtra extra)
    {
        CollectionIndex = metaIndex - Constants.OFFSET_INDEX;
        Extra = extra;
        MetaIndex = metaIndex;
        Platform = platform;

        PersistentStorageSlot = MetaIndex == 0 ? StoragePersistentSlotEnum.AccountData : (StoragePersistentSlotEnum)(MetaIndex);
        SaveType = (SaveTypeEnum)(CollectionIndex % 2);
        SlotIndex = CollectionIndex / 2; // integer division

        Identifier = MetaIndex == 0 ? "AccountData" : $"Slot{SlotIndex + 1}{SaveType}"; // ignore 1 as it will not used here
    }

    #endregion

    #region IComparable, IEquatable

    public int CompareTo(IContainer? other)
    {
        return MetaIndex.CompareTo(other?.MetaIndex);
    }

    public bool Equals(IContainer? other)
    {
        if (other is null)
            return this is null;

        return GetHashCode() == other.GetHashCode();
    }

    public override bool Equals(object? other)
    {
        return other is IContainer otherContainer && Equals(otherContainer);
    }

    public override int GetHashCode()
    {
        return DataFile?.GetHashCode() ?? MetaIndex.GetHashCode();
    }

    public static bool operator ==(Container left, Container right)
    {
        if (left is null)
            return right is null;

        return left.Equals(right);
    }

    public static bool operator !=(Container left, Container right)
    {
        return !(left == right);
    }

    public static bool operator <(Container left, Container right)
    {
        return left is null ? right is not null : left.CompareTo(right) < 0;
    }

    public static bool operator <=(Container left, Container right)
    {
        return left is null || left.CompareTo(right) <= 0;
    }

    public static bool operator >(Container left, Container right)
    {
        return left is not null && left.CompareTo(right) > 0;
    }

    public static bool operator >=(Container left, Container right)
    {
        return left is null ? right is null : left.CompareTo(right) >= 0;
    }

    #endregion

    #region ToString

    public override string ToString()
    {
        string? type = null;

        if (Exists)
        {
            if (IsBackup) // potentially to most (multiple per Container)
            {
                type = "Backup";
            }
            else if (IsSave) // multiple per Platform
            {
                type = "Save";
            }
            else if (IsAccount) // one per Platform
            {
                type = "Account";
            }

            if (type is not null)
                type = $" // {type}";
        }

        return $"{nameof(Container)} {PersistentStorageSlot} {Identifier}{(type ?? string.Empty)}";
    }

    #endregion

    // internal //

    internal void ClearIncompatibility()
    {
        IncompatibilityException = null;
        IncompatibilityTag = null;

        PropertiesChangedCallback.Invoke();
    }

    internal void CopyImportantProperties(Container other)
    {
        // Faking properties to force it to Write().
        Exists = true;

        // Additional properties required to properly rebuild the container.
        GameVersion = other.GameVersion;
        SaveVersion = other.SaveVersion;
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

        // Invoke whenever these values update.
        PropertiesChangedCallback.Invoke();
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
        ResolveWatcherChange();

        // Last so PropertiesChangedCallback includes everything.
        RefreshFileInfo();
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
