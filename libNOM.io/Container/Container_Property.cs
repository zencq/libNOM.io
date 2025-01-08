using System.Collections.ObjectModel;

using libNOM.io.Trace;

namespace libNOM.io;


// This partial class contains internal properties.
public partial class Container : IContainer
{
    #region Field

    private bool _canSwitchContext;
    private bool? _exists;

    #endregion

    // public //

    public ObservableCollection<IContainer> BackupCollection { get; } = [];

    public string Identifier { get; }

    public Exception? IncompatibilityException { get; internal set; }

    public string? IncompatibilityTag { get; internal set; }

    public ContainerTrace? Trace { get; internal set; }

    public HashSet<string> UnknownKeys { get; internal set; } = [];

    // internal //

    internal ContainerExtra Extra { get; set; }

    internal UserIdentification? UserIdentification { get; set; }

    #region Flags

    public bool CanSwitchContext // { get; internal set; }
    {
        get => IsLoaded ? (_jsonObject!.ContainsKey(Json.GetPath("BASE_CONTEXT", _jsonObject)) && _jsonObject!.ContainsKey(Json.GetPath("EXPEDITION_CONTEXT", _jsonObject!))) : _canSwitchContext;
        internal set => _canSwitchContext = value;
    }

    public bool HasActiveExpedition => GameMode == PresetGameModeEnum.Seasonal || (IsLoaded && _jsonObject!.ContainsKey(Json.GetPath("EXPEDITION_CONTEXT", _jsonObject!))); // { get; }

    public bool HasBase => IsLoaded && GetJsonValues<PersistentBaseTypesEnum>("PERSISTENT_PLAYER_BASE_ALL_TYPES", ActiveContext).Distinct().Any(i => i is PersistentBaseTypesEnum.HomePlanetBase or PersistentBaseTypesEnum.FreighterBase); // { get; }

    public bool HasFreighter => IsLoaded && GetJsonValues<double>("FREIGHTER_POSITION", ActiveContext).Any(i => i != 0.0); // { get; }

    public bool HasSettlement => IsLoaded && GetJsonValues<string>("SETTLEMENT_ALL_OWNER_LID", ActiveContext).Any(i => !string.IsNullOrEmpty(i)); // { get; }

    public bool IsAccount => PersistentStorageSlot == StoragePersistentSlotEnum.AccountData; // { get; }

    public bool IsBackup { get; internal set; }

    public bool IsCompatible => Exists && string.IsNullOrEmpty(IncompatibilityTag); // { get; }

    public bool IsLoaded => IsCompatible && _jsonObject is not null; // { get; }

    public bool IsOld => Exists && IsSave && GameVersion < Constants.LOWEST_SUPPORTED_VERSION; // { get; }

    public bool IsSave => PersistentStorageSlot >= StoragePersistentSlotEnum.PlayerState1; // { get; }

    public bool IsSynced { get; set; } = true;

    #endregion

    #region FileInfo

    public FileInfo? DataFile { get; internal set; }

    public bool Exists // { get; internal set; }
    {
        get => _exists ?? DataFile?.Exists ?? false;
        internal set => _exists = value;
    }

    public DateTimeOffset? LastWriteTime // { get; internal set; }
    {
        get => Extra.LastWriteTime ?? MetaFile?.LastWriteTime ?? DataFile?.CreationTime;
        internal set => Extra = Extra with { LastWriteTime = value };
    }

    public FileInfo? MetaFile { get; internal set; }

    #endregion

    #region FileSystemWatcher

    public WatcherChangeTypes? WatcherChangeType { get; private set; }

    public bool HasWatcherChange { get; private set; }

    #endregion

    #region Index

    public int CollectionIndex { get; }

    public int MetaIndex { get; }

    public int SlotIndex { get; }

    #endregion
}
