using CommunityToolkit.Diagnostics;

using libNOM.io.Delegates;
using libNOM.io.Interfaces;
using libNOM.map;

using Newtonsoft.Json.Linq;

namespace libNOM.io;


/// <summary>
/// Holds all information about a single save.
/// </summary>
public partial class Container : IContainer
{
    #region Field

    private bool? _exists;
    private GameVersionEnum _gameVersion = GameVersionEnum.Unknown;
    private JObject? _jsonObject;
    private int _saveVersion = -1;

    #endregion

    #region Getter

    public JObject GetJsonObject()
    {
        ThrowHelperIsLoaded();
        return _jsonObject!;
    }

    public JToken? GetJsonToken(string pathIdentifier) => GetJsonToken(pathIdentifier, ActiveContext);

    public JToken? GetJsonToken(string pathIdentifier, SaveContextQueryEnum context)
    {
        ThrowHelperIsLoaded();
        return _jsonObject!.GetValue<JToken>(pathIdentifier, context);
    }

    public IEnumerable<JToken> GetJsonTokens(string pathIdentifier) => GetJsonTokens(pathIdentifier, ActiveContext);

    public IEnumerable<JToken> GetJsonTokens(string pathIdentifier, SaveContextQueryEnum context)
    {
        ThrowHelperIsLoaded();
        return _jsonObject!.GetValues<JToken>(pathIdentifier, context);
    }

    public T? GetJsonValue<T>(ReadOnlySpan<int> indices)
    {
        ThrowHelperIsLoaded();
        return _jsonObject!.GetValue<T>(indices);
    }

    public T? GetJsonValue<T>(string pathIdentifier) => GetJsonValue<T>(pathIdentifier, ActiveContext);

    public T? GetJsonValue<T>(string pathIdentifier, SaveContextQueryEnum context)
    {
        ThrowHelperIsLoaded();
        return _jsonObject!.GetValue<T>(pathIdentifier, context);
    }

    public IEnumerable<T?> GetJsonValues<T>(string pathIdentifier) => GetJsonValues<T>(pathIdentifier, ActiveContext);

    public IEnumerable<T?> GetJsonValues<T>(string pathIdentifier, SaveContextQueryEnum context)
    {
        ThrowHelperIsLoaded();
        return _jsonObject!.GetValues<T>(pathIdentifier, context);
    }

    #endregion

    #region Setter

    public void SetJsonObject(JObject? value)
    {
        // No ThrowHelperIsLoaded as setting this will determine the result.
        _jsonObject = value;

        IsSynced = false;

        // Make sure the data are always in the format that was set in the settings.
        if (_jsonObject is not null && Platform is not null) // happens when the container is unloaded
            if (Platform.Settings.UseMapping)
            {
                UnknownKeys = Mapping.Deobfuscate(_jsonObject);
            }
            else
            {
                Mapping.Obfuscate(_jsonObject);
            }
    }

    public void SetJsonValue(JToken value, ReadOnlySpan<int> indices)
    {
        ThrowHelperIsLoaded();
        // If setting the value was successful, it is not synced anymore.
        IsSynced = !_jsonObject!.SetValue(value, indices);
    }

    public void SetJsonValue(JToken value, string pathIdentifier) => SetJsonValue(value, pathIdentifier, ActiveContext);

    public void SetJsonValue(JToken value, string pathIdentifier, SaveContextQueryEnum context)
    {
        ThrowHelperIsLoaded();
        // If setting the value was successful, it is not synced anymore.
        IsSynced = !_jsonObject!.SetValue(value, pathIdentifier, context);
    }

    public void SetWatcherChange(WatcherChangeTypes changeType)
    {
        HasWatcherChange = true;
        WatcherChangeType = changeType;
    }

    #endregion

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
            ThrowHelper.ThrowInvalidOperationException("Container is not loaded.");
    }
}
