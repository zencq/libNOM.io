using System.Collections.ObjectModel;

using CommunityToolkit.Diagnostics;

using libNOM.io.Delegates;
using libNOM.io.Interfaces;
using libNOM.map;

using Newtonsoft.Json.Linq;

namespace libNOM.io;


/// <summary>
/// Holds all information about a single save.
/// </summary>
public class Container : IContainer
{
    #region Field

    private bool? _exists;
    private GameVersionEnum _gameVersion = GameVersionEnum.Unknown;
    private JObject? _jsonObject;
    private int _saveVersion = -1;

    #endregion

    #region Property

    // public //

    public ObservableCollection<Container> BackupCollection { get; } = [];

    public string Identifier { get; }

    public Exception? IncompatibilityException { get; internal set; }

    public string? IncompatibilityTag { get; internal set; }

    public HashSet<string> UnknownKeys { get; set; } = [];

    // internal //

    internal PlatformExtra Extra { get; set; }

    internal UserIdentification? UserIdentification { get; set; }

    // //

    #region Flags

    public bool CanSwitchContext => IsLoaded && _jsonObject!.ContainsKey(Json.GetPath("BASE_CONTEXT", _jsonObject)) && _jsonObject!.ContainsKey(Json.GetPath("EXPEDITION_CONTEXT", _jsonObject!)); // { get; }

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

    public bool IsVersion211BeyondWithVehicleCam => IsVersion(GameVersionEnum.BeyondWithVehicleCam); // { get; }

    public bool IsVersion220Synthesis => IsVersion(GameVersionEnum.Synthesis); // { get; }

    public bool IsVersion226SynthesisWithJetpack => IsVersion(GameVersionEnum.SynthesisWithJetpack); // { get; }

    public bool IsVersion230LivingShip => IsVersion(GameVersionEnum.LivingShip); // { get; }

    public bool IsVersion240ExoMech => IsVersion(GameVersionEnum.ExoMech); // { get; }

    public bool IsVersion250Crossplay => IsVersion(GameVersionEnum.Crossplay); // { get; }

    public bool IsVersion260Desolation => IsVersion(GameVersionEnum.Desolation); // { get; }

    public bool IsVersion300Origins => IsVersion(GameVersionEnum.Origins); // { get; }

    public bool IsVersion310NextGeneration => IsVersion(GameVersionEnum.NextGeneration); // { get; }

    public bool IsVersion320Companions => IsVersion(GameVersionEnum.Companions); // { get; }

    public bool IsVersion330Expeditions => IsVersion(GameVersionEnum.Expeditions); // { get; }

    public bool IsVersion340Beachhead => IsVersion(GameVersionEnum.Beachhead); // { get; }

    public bool IsVersion350Prisms => IsVersion(GameVersionEnum.Prisms); // { get; }

    public bool IsVersion351PrismsWithByteBeatAuthor => IsVersion(GameVersionEnum.PrismsWithByteBeatAuthor); // { get; }

    public bool IsVersion360Frontiers => IsVersion(GameVersionEnum.Frontiers); // { get; }

    public bool IsVersion370Emergence => IsVersion(GameVersionEnum.Emergence); // { get; }

    public bool IsVersion380Sentinel => IsVersion(GameVersionEnum.Sentinel); // { get; }

    public bool IsVersion381SentinelWithWeaponResource => IsVersion(GameVersionEnum.SentinelWithWeaponResource); // { get; }

    public bool IsVersion384SentinelWithVehicleAI => IsVersion(GameVersionEnum.SentinelWithVehicleAI); // { get; }

    public bool IsVersion385Outlaws => IsVersion(GameVersionEnum.Outlaws); // { get; }

    public bool IsVersion390Leviathan => IsVersion(GameVersionEnum.Leviathan); // { get; }

    public bool IsVersion394Endurance => IsVersion(GameVersionEnum.Endurance); // { get; }

    public bool IsVersion400Waypoint => IsVersion(GameVersionEnum.Waypoint); // { get; }

    public bool IsVersion404WaypointWithAgileStat => IsVersion(GameVersionEnum.WaypointWithAgileStat); // { get; }

    public bool IsVersion405WaypointWithSuperchargedSlots => IsVersion(GameVersionEnum.WaypointWithSuperchargedSlots); // { get; }

    public bool IsVersion410Fractal => IsVersion(GameVersionEnum.Fractal); // { get; }

    public bool IsVersion420Interceptor => IsVersion(GameVersionEnum.Interceptor); // { get; }

    public bool IsVersion430Singularity => IsVersion(GameVersionEnum.Singularity); // { get; }

    public bool IsVersion440Echoes => IsVersion(GameVersionEnum.Echoes); // { get; }

    public bool IsVersion450Omega => IsVersion(GameVersionEnum.Omega); // { get; }

    public bool IsVersion452OmegaWithV2 => IsVersion(GameVersionEnum.OmegaWithV2); // { get; }

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
        get => Extra.LastWriteTime ?? (Exists ? DataFile?.LastWriteTime : null);
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

    #region Save

    // public //

    public SaveContextQueryEnum ActiveContext // { get; set; }
    {
        get => _jsonObject?.GetValue<SaveContextQueryEnum>("ACTIVE_CONTEXT") ?? SaveContextQueryEnum.DontCare;
        set
        {
            Guard.IsTrue(CanSwitchContext, nameof(CanSwitchContext)); // block switching if only one context
            if (IsLoaded && value is SaveContextQueryEnum.Season or SaveContextQueryEnum.Main)
                SetJsonValue(value.ToString(), "ACTIVE_CONTEXT");
        }
    }

    public DifficultyPresetTypeEnum Difficulty // { get; set; }
    {
        get
        {
            if (Extra.DifficultyPreset == 0)
                Extra = Extra with { DifficultyPreset = (uint)(Meta.DifficultyPreset.Get(this, _jsonObject)) };

            return (DifficultyPresetTypeEnum)(Extra.DifficultyPreset);
        }
        set
        {
            Extra = Extra with { DifficultyPreset = (uint)(value) };

            if (IsLoaded && GameMode < PresetGameModeEnum.Seasonal)
            {
                if (IsVersion400Waypoint)
                    Meta.DifficultyPreset.Set(this, value switch
                    {
                        DifficultyPresetTypeEnum.Creative => Constants.DIFFICULTY_PRESET_CREATIVE,
                        DifficultyPresetTypeEnum.Relaxed => Constants.DIFFICULTY_PRESET_RELAXED,
                        DifficultyPresetTypeEnum.Survival => Constants.DIFFICULTY_PRESET_SURVIVAL,
                        DifficultyPresetTypeEnum.Permadeath => Constants.DIFFICULTY_PRESET_PERMADEATH,
                        _ => Constants.DIFFICULTY_PRESET_NORMAL,
                    });

                GameMode = value switch
                {
                    DifficultyPresetTypeEnum.Creative => PresetGameModeEnum.Creative,
                    DifficultyPresetTypeEnum.Survival => PresetGameModeEnum.Survival,
                    DifficultyPresetTypeEnum.Permadeath => PresetGameModeEnum.Permadeath,
                    _ => PresetGameModeEnum.Normal,
                };
            }
        }
    }

    public GameVersionEnum GameVersion // { get; internal set; }
    {
        get
        {
            if (_gameVersion == GameVersionEnum.Unknown && IsLoaded)
                _gameVersion = Meta.GameVersion.Get(BaseVersion, _jsonObject!);

            return _gameVersion;
        }
        internal set => _gameVersion = value;
    }

    public string SaveName // { get; set; }
    {
        get
        {
            if (string.IsNullOrEmpty(Extra.SaveName))
                Extra = Extra with { SaveName = _jsonObject?.GetValue<string>("SAVE_NAME") ?? string.Empty };

            return Extra.SaveName;
        }
        set
        {
            // Maximum length in-game is 42 characters.
            value = value.AsSpan(0, Math.Min(value.Length, Constants.SAVE_RENAMING_LENGTH_INGAME)).ToString();

            Extra = Extra with { SaveName = value };

            if (IsLoaded)
                SetJsonValue(value, "SAVE_NAME");
        }
    }

    public string SaveSummary // { get; set; }
    {
        get
        {
            if (string.IsNullOrEmpty(Extra.SaveSummary))
                Extra = Extra with { SaveSummary = _jsonObject?.GetValue<string>("SAVE_SUMMARY", ActiveContext) ?? string.Empty };

            return Extra.SaveSummary;
        }
        set
        {
            value = value.AsSpan(0, Math.Min(value.Length, Constants.SAVE_RENAMING_LENGTH_MANIFEST - 1)).ToString();

            Extra = Extra with { SaveSummary = value };

            if (IsLoaded)
                SetJsonValue(value, "SAVE_SUMMARY");
        }
    }

    public SaveTypeEnum SaveType { get; }

    public SeasonEnum Season // { get; internal set; }
    {
        get
        {
            if (GameMode == PresetGameModeEnum.Seasonal && Extra.Season == 0)
                Extra = Extra with { Season = (ushort)(Meta.Season.Get(_jsonObject)) };

            return (SeasonEnum)(Extra.Season);
        }
        internal set => Extra = Extra with { Season = (ushort)(value) };
    }

    public uint TotalPlayTime // { get; set; }
    {
        get
        {
            if (Extra.TotalPlayTime == 0)
                Extra = Extra with { TotalPlayTime = _jsonObject?.GetValue<uint?>("TOTAL_PLAY_TIME") ?? 0 };

            return Extra.TotalPlayTime;
        }
        set
        {
            Extra = Extra with { TotalPlayTime = value };

            if (IsLoaded)
                SetJsonValue(value, "TOTAL_PLAY_TIME");
        }
    }

    // internal //

    internal int BaseVersion // { get; set; }
    {
        get
        {
            if (Extra.BaseVersion <= 0)
                Extra = Extra with { BaseVersion = SaveVersion - (((int)(GameMode) + ((int)(Season) * Constants.OFFSET_SEASON)) * Constants.OFFSET_GAMEMODE) };

            return Extra.BaseVersion;
        }
        set => Extra = Extra with { BaseVersion = value };
    }

    internal PresetGameModeEnum GameMode // { get; set; }
    {
        get
        {
            if (Extra.GameMode == 0)
            {
                var mode = SaveVersion switch
                {
                    >= Constants.THRESHOLD_GAMEMODE_SEASONAL => PresetGameModeEnum.Seasonal,
                    >= Constants.THRESHOLD_GAMEMODE_PERMADEATH => PresetGameModeEnum.Permadeath,
                    >= Constants.THRESHOLD_GAMEMODE_SURVIVAL => PresetGameModeEnum.Survival,
                    >= Constants.THRESHOLD_GAMEMODE_CREATIVE => PresetGameModeEnum.Creative,
                    >= Constants.THRESHOLD_GAMEMODE_NORMAL => PresetGameModeEnum.Normal,
                    _ => PresetGameModeEnum.Unspecified,
                };
                if (mode < PresetGameModeEnum.Seasonal && ActiveContext == SaveContextQueryEnum.Season)
                    mode = PresetGameModeEnum.Seasonal;

                Extra = Extra with { GameMode = (ushort)(mode) };
            }
            return (PresetGameModeEnum)(Extra.GameMode);
        }
        set
        {
            Extra = Extra with { GameMode = (ushort)(value) };

            if (IsLoaded)
                SaveVersion = Meta.SaveVersion.Calculate(this);
        }
    }

    internal MetaFormatEnum MetaFormat // { get; set; }
    {
        get => Extra.MetaFormat;
        set => Extra = Extra with { MetaFormat = value };
    }

    internal int MetaSize => Platform is null ? -1 : MetaFormat switch // { get; }
    {
        MetaFormatEnum.Waypoint => Platform.META_LENGTH_TOTAL_WAYPOINT,
        _ => Platform.META_LENGTH_TOTAL_VANILLA,
    };

    internal int SaveVersion // { get; set; }
    {
        get
        {
            if (_saveVersion <= 0)
                _saveVersion = _jsonObject?.GetValue<int>("VERSION") ?? -1;

            return _saveVersion;
        }
        set
        {
            _saveVersion = value;

            if (IsLoaded)
                SetJsonValue(value, "VERSION");
        }
    }

    internal StoragePersistentSlotEnum PersistentStorageSlot { get; }

    internal Platform? Platform { get; set; }

    #endregion

    #endregion

    // //

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

    // public //

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

    // //

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

    #region IComparable, IEquatable

    public int CompareTo(Container? other)
    {
        return MetaIndex.CompareTo(other?.MetaIndex);
    }

    public bool Equals(Container? other)
    {
        if (other is null)
            return this is null;

        return GetHashCode() == other.GetHashCode();
    }

    public override bool Equals(object? other)
    {
        return other is Container otherContainer && Equals(otherContainer);
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
        var e = Exists ? (IsBackup ? "Backup" : (IsAccount ? "Account" : (IsSave ? "Save" : null))) : null;
        if (e is not null)
            e = $" // {e}";

        return $"{nameof(Container)} {PersistentStorageSlot} {Identifier}{(e ?? string.Empty)}";
    }

    #endregion

    #region ThrowHelper

    private void ThrowHelperIsLoaded()
    {
        if (!IsLoaded)
            ThrowHelper.ThrowInvalidOperationException("Container is not loaded.");
    }

    #endregion

    // public //

    public bool IsVersion(GameVersionEnum versionEnum)
    {
        return GameVersion >= versionEnum;
    }

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
}
