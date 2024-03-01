using System.Collections.ObjectModel;

using CommunityToolkit.Diagnostics;

using libNOM.io.Delegates;
using libNOM.map;

using Newtonsoft.Json.Linq;

namespace libNOM.io;


/// <summary>
/// Holds all information about a single save.
/// </summary>
public class Container : IComparable<Container>, IEquatable<Container>
{
    #region Field

    private bool? _exists;
    private GameVersionEnum _gameVersion = GameVersionEnum.Unknown;
    private JObject? _jsonObject;
    private Platform _platform;
    private int _saveVersion = -1;

    #endregion

    #region Property

    // public //

    /// <summary>
    /// List of related backups.
    /// </summary>
    public ObservableCollection<Container> BackupCollection { get; } = [];

    /// <summary>
    /// Identifier of the save containing the slot number and save type.
    /// </summary>
    public string Identifier { get; }

    /// <summary>
    /// If the incompatibility was caused by an unexpected exception, it stored here.
    /// </summary>
    public Exception? IncompatibilityException { get; internal set; }

    /// <summary>
    /// A tag with information why this save is incompatible. To see what reasons are available have a look at <see cref="Globals.Constants"/>.INCOMPATIBILITY_\d{3}.
    /// </summary>
    public string? IncompatibilityTag { get; internal set; }

    /// <summary>
    /// List of unknown keys collected during deobfuscation.
    /// </summary>
    public HashSet<string> UnknownKeys { get; set; } = [];

    // internal //

    internal PlatformExtra Extra { get; set; }

    internal UserIdentification? UserIdentification { get; set; }

    // //

    #region Flags

    /// <summary>
    /// Whether it is possible to switch context between the main/primary save and an active expedition/season.
    /// </summary>
    public bool CanSwitchContext => IsLoaded && _jsonObject!.ContainsKey(Json.GetPath("BASE_CONTEXT", _jsonObject)) && _jsonObject!.ContainsKey(Json.GetPath("EXPEDITION_CONTEXT", _jsonObject!)); // { get; }

    /// <summary>
    /// Whether this is a save with an ongoing expedition (<see cref="PresetGameModeEnum.Seasonal"/>).
    /// </summary>
    public bool HasActiveExpedition => GameMode == PresetGameModeEnum.Seasonal || (IsLoaded && _jsonObject!.ContainsKey(Json.GetPath("EXPEDITION_CONTEXT", _jsonObject!))); // { get; }

    /// <summary>
    /// Whether this contains potential user owned bases.
    /// </summary>
    public bool HasBase => IsLoaded && GetJsonValues<PersistentBaseTypesEnum>("PERSISTENT_PLAYER_BASE_ALL_TYPES", ActiveContext).Any(i => i is PersistentBaseTypesEnum.HomePlanetBase or PersistentBaseTypesEnum.FreighterBase); // { get; }

    /// <summary>
    /// Whether this contains a user owned freighter.
    /// </summary>
    public bool HasFreighter => IsLoaded && GetJsonValues<double>("FREIGHTER_POSITION", ActiveContext).Any(i => i != 0.0); // { get; }

    /// <summary>
    /// Whether this contains a potential user owned settlement.
    /// </summary>
    public bool HasSettlement => IsLoaded && GetJsonValues<string>("SETTLEMENT_ALL_OWNER_LID", ActiveContext).Any(i => !string.IsNullOrEmpty(i)); // { get; }

    /// <summary>
    /// Whether this contains account data and is not a regular save.
    /// </summary>
    public bool IsAccount => PersistentStorageSlot == StoragePersistentSlotEnum.AccountData; // { get; }

    /// <summary>
    /// Whether this is a backup.
    /// </summary>
    public bool IsBackup { get; internal set; }

    /// <summary>
    /// Whether this was correctly loaded and no exception or an other reason occurred while loading that made it incompatible.
    /// </summary>
    public bool IsCompatible => Exists && string.IsNullOrEmpty(IncompatibilityTag); // { get; }

    /// <summary>
    /// Whether this contains loaded JSON data and is ready to use.
    /// </summary>
    public bool IsLoaded => IsCompatible && _jsonObject is not null; // { get; }

    /// <summary>
    /// Whether this is older than the lowest supported version.
    /// </summary>
    public bool IsOld => IsSave && GameVersion < Constants.LOWEST_SUPPORTED_VERSION; // { get; }

    /// <summary>
    /// Whether this is an actual save and not something else like account data.
    /// </summary>
    public bool IsSave => PersistentStorageSlot >= StoragePersistentSlotEnum.PlayerState1; // { get; }

    /// <summary>
    /// Whether this is identical to the data on the drive.
    /// </summary>
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

    public bool IsVersion351PrismsWithBytebeatAuthor => IsVersion(GameVersionEnum.PrismsWithBytebeatAuthor); // { get; }

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

    /// <summary>
    /// Starts at 0 for PlayerState1, AccountData will be -2.
    /// </summary>
    public int CollectionIndex { get; }

    /// <summary>
    /// Starts at 0 for AccountData, PlayerState1 will be 2.
    /// </summary>
    public int MetaIndex { get; }

    /// <summary>
    /// Starts at 0 for PlayerState1 and PlayerState2, AccountData will be -1.
    /// </summary>
    public int SlotIndex { get; }

    #endregion

    #region Save

    // public //

    public SaveContextQueryEnum ActiveContext // { get; set; }
    {
        get => _jsonObject?.GetValue<SaveContextQueryEnum>("ACTIVE_CONTEXT") ?? SaveContextQueryEnum.DontCare; // DontCare is used for pre-Omega saves
        set
        {
            Guard.IsTrue(CanSwitchContext, nameof(CanSwitchContext)); // block switching if only one context
            if (IsLoaded && value is SaveContextQueryEnum.Season or SaveContextQueryEnum.Main)
                SetJsonValue(value.ToString(), "ACTIVE_CONTEXT");
        }
    }

    public DifficultyPresetTypeEnum GameDifficulty // { get; set; }
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

            if (IsVersion400Waypoint)
            {
                if (GameMode < PresetGameModeEnum.Seasonal)
                {
                    if (value == DifficultyPresetTypeEnum.Permadeath)
                    {
                        GameMode = PresetGameModeEnum.Permadeath;
                        Meta.DifficultyPreset.Set(this, Constants.DIFFICULTY_PRESET_PERMADEATH);
                    }
                    else if (value < DifficultyPresetTypeEnum.Permadeath)
                    {
                        GameMode = PresetGameModeEnum.Normal;
                        Meta.DifficultyPreset.Set(this, value switch
                        {
                            DifficultyPresetTypeEnum.Creative => Constants.DIFFICULTY_PRESET_CREATIVE,
                            DifficultyPresetTypeEnum.Relaxed => Constants.DIFFICULTY_PRESET_RELAXED,
                            DifficultyPresetTypeEnum.Survival => Constants.DIFFICULTY_PRESET_SURVIVAL,
                            _ => Constants.DIFFICULTY_PRESET_NORMAL,
                        });
                    }
                }
            }
            else
            {
                if (GameMode < PresetGameModeEnum.Seasonal)
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
            value = value.AsSpanSubstring(0, Math.Min(value.Length, Constants.SAVE_RENAMING_LENGTH_INGAME)).ToString();

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
                Extra = Extra with { SaveSummary = _jsonObject?.GetValue<string>("SAVE_SUMMARY") ?? string.Empty };

            return Extra.SaveSummary;
        }
        set
        {
            value = value.AsSpanSubstring(0, Math.Min(value.Length, Constants.SAVE_RENAMING_LENGTH_MANIFEST - 1)).ToString();

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
            if (Extra.BaseVersion == 0)
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

    internal int MetaSize => MetaFormat switch // { get; }
    {
        MetaFormatEnum.Waypoint => _platform.META_LENGTH_TOTAL_WAYPOINT,
        _ => _platform.META_LENGTH_TOTAL_VANILLA,
    };

    internal int SaveVersion // { get; set; }
    {
        get
        {
            if (_saveVersion == -1)
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

    #endregion

    #endregion

    // //

    #region Getter

    /// <summary>
    /// Gets the entire JSON object.
    /// </summary>
    /// <returns></returns>
    public JObject GetJsonObject()
    {
        ThrowHelperIsLoaded();
        return _jsonObject!;
    }

    /// <inheritdoc cref="GetJsonToken(string, SaveContextQueryEnum)"/>
    /// <summary>
    /// Gets a JSON element that matches the JSONPath expression.
    /// For saves from Omega and up this will use the active context if the path goes there and you use "{0}.PlayerStateData" in the expression.
    /// </summary>
    public JToken? GetJsonToken(string pathIdentifier) => GetJsonToken(pathIdentifier, ActiveContext);

    /// <summary>
    /// Gets a JSON element that matches the JSONPath expression.
    /// For saves from Omega and up this will use the specified context if the path goes there and you use "{0}.PlayerStateData" in the expression.
    /// </summary>
    /// <param name="pathIdentifier">A JSONPath expressions.</param>
    /// <param name="context"></param>
    /// <returns></returns>
    public JToken? GetJsonToken(string pathIdentifier, SaveContextQueryEnum context)
    {
        ThrowHelperIsLoaded();
        return _jsonObject!.GetValue<JToken>(pathIdentifier, context);
    }

    /// <inheritdoc cref="GetJsonTokens(string, SaveContextQueryEnum)"/>
    /// <summary>
    /// Gets a collection of JSON elements that matches the JSONPath expression.
    /// For saves from Omega and up this will use the active context if the path goes there and you use "{0}.PlayerStateData" in the expression.
    /// </summary>
    public IEnumerable<JToken> GetJsonTokens(string pathIdentifier) => GetJsonTokens(pathIdentifier, ActiveContext);

    /// <summary>
    /// Gets a collection of JSON elements that matches the JSONPath expression.
    /// For saves from Omega and up this will use the specified context if the path goes there and you use "{0}.PlayerStateData" in the expression.
    /// </summary>
    /// <param name="pathIdentifier">A JSONPath expressions.</param>
    /// <param name="context"></param>
    /// <returns></returns>
    public IEnumerable<JToken> GetJsonTokens(string pathIdentifier, SaveContextQueryEnum context)
    {
        ThrowHelperIsLoaded();
        return _jsonObject!.GetValues<JToken>(pathIdentifier, context);
    }

    /// <summary>
    /// Gets the value of the JSON element that matches the path of indices.
    /// Except the last one, each index in the entire path must point to either a JArray or a JObject.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="indices"></param>
    /// <returns>The value at the end of the path of indices.</returns>
    public T? GetJsonValue<T>(ReadOnlySpan<int> indices)
    {
        ThrowHelperIsLoaded();
        return _jsonObject!.GetValue<T>(indices);
    }

    /// <inheritdoc cref="GetJsonValue{T}(string, SaveContextQueryEnum)"/>
    public T? GetJsonValue<T>(string pathIdentifier) => GetJsonValue<T>(pathIdentifier, ActiveContext);

    /// <summary>
    /// Gets the actual value of the JSON element that matches the JSONPath expression.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="pathIdentifier">A JSONPath expressions.</param>
    /// <param name="context"></param>
    /// <returns></returns>
    public T? GetJsonValue<T>(string pathIdentifier, SaveContextQueryEnum context)
    {
        ThrowHelperIsLoaded();
        return _jsonObject!.GetValue<T>(pathIdentifier, context);
    }

    /// <inheritdoc cref="GetJsonValue{T}(string, SaveContextQueryEnum)"/>
    public IEnumerable<T?> GetJsonValues<T>(string pathIdentifier) => GetJsonValues<T>(pathIdentifier, ActiveContext);

    /// <summary>
    /// Gets the actual values of all JSON elements that matches the JSONPath expression.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="pathIdentifier">A JSONPath expressions.</param>
    /// <param name="context">A JSONPath expressions.</param>
    /// <returns></returns>
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
        if (_jsonObject is not null) // happens when the container is unloaded
            if (_platform.Settings.UseMapping)
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
        // If setting the value was successful, it is now unsynced.
        IsSynced = !_jsonObject!.SetValue(value, indices);
    }

    public void SetJsonValue(JToken value, string pathIdentifier) => SetJsonValue(value, pathIdentifier, ActiveContext);

    public void SetJsonValue(JToken value, string pathIdentifier, SaveContextQueryEnum context)
    {
        ThrowHelperIsLoaded();
        // If setting the value was successful, it is now unsynced.
        IsSynced = !_jsonObject!.SetValue(value, pathIdentifier, context);
    }

    public void SetWatcherChange(WatcherChangeTypes changeType)
    {
        HasWatcherChange = true;
        WatcherChangeType = changeType;
    }

    // internal //

    internal void SetPlatform(Platform platform)
    {
        _platform = platform;
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
        _platform = platform;

        CollectionIndex = metaIndex - Constants.OFFSET_INDEX;
        Extra = extra;
        MetaIndex = metaIndex;

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

    /// <summary>
    /// Whether the save is up-to-date to the specified version of the game.
    /// </summary>
    /// <param name="versionEnum"></param>
    /// <returns></returns>
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
    /// Resets the container to the default state except for properties set in ctor.
    /// </summary>
    internal void Reset()
    {
        _exists = null;
        _jsonObject = null;

        BackupCollection.Clear();
        UserIdentification = null;
        UnknownKeys.Clear();

        ClearIncompatibility();

        Extra = new();
        IsSynced = true;

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
