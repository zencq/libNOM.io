using CommunityToolkit.Diagnostics;
using libNOM.io.Delegates;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;

namespace libNOM.io;


/// <summary>
/// Holds all information about a single save.
/// </summary>
public class Container : IComparable<Container>, IEquatable<Container>
{
    #region Field

    private bool? _exists;
    private JObject? _jsonObject;
    private int _saveVersion;

    #endregion

    #region Property

    // public //

    /// <summary>
    /// List of related backups.
    /// </summary>
    public ObservableCollection<Container> BackupCollection { get; } = new();

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
    public HashSet<string> UnknownKeys { get; set; } = new();

    // internal //

    internal PlatformExtra Extra { get; set; }

    internal UserIdentificationData? UserIdentification { get; set; }

    // //

    #region Flags

    // public //

    /// <summary>
    /// Whether this contains potential user owned bases.
    /// </summary>
    public bool HasBase => IsLoaded && GetJsonValues<PersistentBaseTypesEnum>("6f=.F?0[*].peI.DPp", "PlayerStateData.PersistentPlayerBases[*].BaseType.PersistentBaseTypes").Any(i => i is PersistentBaseTypesEnum.HomePlanetBase or PersistentBaseTypesEnum.FreighterBase); // { get; }

    /// <summary>
    /// Whether this contains a user owned freighter.
    /// </summary>
    public bool HasFreighter => IsLoaded && (GetJsonValues<double>("6f=.lpm[*]", "PlayerStateData.FreighterMatrixPos[*]")?.Any(i => i != 0.0) ?? false); // { get; }

    /// <summary>
    /// Whether this contains a potential user owned settlement.
    /// </summary>
    public bool HasSettlement => IsLoaded && (GetJsonValues<string>("6f=.GQA[*].3?K.f5Q", "PlayerStateData.SettlementStatesV2[*].Owner.LID")?.Any(i => !string.IsNullOrEmpty(i)) ?? false); // { get; }

    /// <summary>
    /// Whether this contains account data and is not a regular save.
    /// </summary>
    public bool IsAccount => MetaIndex == 0; // { get; }

    /// <summary>
    /// Whether this is a backup.
    /// </summary>
    public bool IsBackup { get; internal set; }

    /// <summary>
    /// Whether this was correctly loaded and no exception or an other reason occurred while loading that made it incompatible.
    /// </summary>
    public bool IsCompatible => Exists && string.IsNullOrEmpty(IncompatibilityTag); // { get; }

    /// <summary>
    /// Whether this is a save with an ongoing expedition (<see cref="PresetGameModeEnum.Seasonal"/>).
    /// </summary>
    public bool IsExpedition => GameMode == PresetGameModeEnum.Seasonal; // { get; }

    /// <summary>
    /// Whether this contains loaded JSON data and is ready to use.
    /// </summary>
    public bool IsLoaded => IsCompatible && _jsonObject is not null; // { get; }

    /// <summary>
    /// Whether this is older than the lowest supported version.
    /// </summary>
    public bool IsOld => Exists && IsSave && GameVersion < Constants.LOWEST_SUPPORTED_VERSION; // { get; }

    /// <summary>
    /// Whether this is an actual save and not something else like account data.
    /// </summary>
    public bool IsSave => MetaIndex >= Constants.OFFSET_INDEX; // { get; }

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

    #endregion

    #region FileInfo

    public FileInfo? DataFile { get; internal set; }

    public bool Exists // { get; internal set; }
    {
        get => _exists ?? DataFile?.Exists == true;
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

    public DifficultyPresetTypeEnum GameDifficulty // { get; set; }
    {
        get => (DifficultyPresetTypeEnum)(Extra.DifficultyPreset);
        set
        {
            if (IsVersion400Waypoint)
            {
                if (GameMode < PresetGameModeEnum.Seasonal)
                {
                    if (value == DifficultyPresetTypeEnum.Permadeath)
                    {
                        GameMode = PresetGameModeEnum.Permadeath;
                    }
                    else if (value < DifficultyPresetTypeEnum.Permadeath)
                    {
                        GameMode = PresetGameModeEnum.Normal;
                    }
                }
                // TODO set preset
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

            Extra = Extra with { DifficultyPreset = (uint)(value) };
        }
    }

    public GameVersionEnum GameVersion { get; internal set; } = GameVersionEnum.Unknown;

    // Maximum length in-game is 42 characters.
    public string SaveName // { get; set; }
    {
        get => string.IsNullOrEmpty(Extra.SaveName) ? Json.GetSaveName(_jsonObject) : Extra.SaveName;
        set
        {
            value = value.AsSpanSubstring(0, Math.Min(value.Length, Constants.SAVE_RENAMING_LENGTH_INGAME)).ToString();

            if (_jsonObject is not null)
                SetJsonValue(value, "6f=.Pk4", "PlayerStateData.SaveName");

            Extra = Extra with { SaveName = value };
        }
    }

    public string SaveSummary // { get; set; }
    {
        get => string.IsNullOrEmpty(Extra.SaveSummary) ? Json.GetSaveSummary(_jsonObject) : Extra.SaveSummary;
        set
        {
            value = value.AsSpanSubstring(0, Math.Min(value.Length, Constants.SAVE_RENAMING_LENGTH_MANIFEST - 1)).ToString();

            if (_jsonObject is not null)
                SetJsonValue(value, "6f=.n:R", "PlayerStateData.SaveSummary");

            Extra = Extra with { SaveSummary = value };
        }
    }

    public SaveTypeEnum SaveType { get; }

    public SeasonEnum Season // { get; internal set; }
    {
        get => (SeasonEnum)(Extra.Season);
        internal set
        {
            if (BaseVersion.IsBaseVersion() && GameMode == PresetGameModeEnum.Seasonal)
            {
                SaveVersion = Calculate.CalculateSaveVersion(BaseVersion, GameMode, value);
            }
            Extra = Extra with { Season = (ushort)(value) };
        }
    }

    public uint TotalPlayTime // { get; set; }
    {
        get => Extra.TotalPlayTime == 0 ? Json.GetTotalPlayTime(_jsonObject) : Extra.TotalPlayTime;
        set
        {
            if (_jsonObject is not null)
                SetJsonValue(value, "6f=.Lg8", "PlayerStateData.TotalPlayTime");

            Extra = Extra with { TotalPlayTime = value };
        }
    }

    public bool UsesMapping { get; private set; }

    // internal //

    internal int BaseVersion // { get; set; }
    {
        get => Extra.BaseVersion;
        set => Extra = Extra with { BaseVersion = value };
    }

    internal PresetGameModeEnum GameMode // { get; set; }
    {
        get
        {
            if (Extra.GameMode == 0)
            {
                if (SaveVersion.IsGameMode(PresetGameModeEnum.Seasonal))
                    return PresetGameModeEnum.Seasonal;

                if (SaveVersion.IsGameMode(PresetGameModeEnum.Permadeath))
                    return PresetGameModeEnum.Permadeath;

                if (SaveVersion.IsGameMode(PresetGameModeEnum.Ambient))
                    return PresetGameModeEnum.Ambient;

                if (SaveVersion.IsGameMode(PresetGameModeEnum.Survival))
                    return PresetGameModeEnum.Survival;

                if (SaveVersion.IsGameMode(PresetGameModeEnum.Creative))
                    return PresetGameModeEnum.Creative;

                if (SaveVersion.IsGameMode(PresetGameModeEnum.Normal))
                    return PresetGameModeEnum.Normal;
            }
            else
                return (PresetGameModeEnum)(Extra.GameMode);

            return PresetGameModeEnum.Unspecified;
        }
        set
        {
            if (BaseVersion.IsBaseVersion())
                SaveVersion = Calculate.CalculateSaveVersion(BaseVersion, value, Season);

            Extra = Extra with { GameMode = (ushort)(value) };
        }
    }

    internal MetaFormatEnum MetaFormat // { get; set; }
    {
        get => Extra.MetaFormat;
        set => Extra = Extra with { MetaFormat = value };
    }

    internal int SaveVersion // { get; set; }
    {
        get => _saveVersion == 0 ? Json.GetVersion(_jsonObject) : _saveVersion;
        set
        {
            if (_jsonObject is not null)
                SetJsonValue(value, "F2P", "Version");

            _saveVersion = value;
        }
    }

    internal StoragePersistentSlotEnum PersistentStorageSlot { get; }

    #endregion

    #endregion

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

    /// <summary>
    /// Gets a JSON element that matches the JSONPath expression.
    /// </summary>
    /// <param name="paths">A collection of JSONPath expressions.</param>
    /// <returns>The element of the first valid expression or null if none is valid.</returns>
    public JToken? GetJsonToken(params string[] paths)
    {
        ThrowHelperIsLoaded();

        foreach (var path in paths)
        {
            var jToken = _jsonObject?.SelectToken(path);
            if (jToken is not null)
                return jToken;
        }
        return null;
    }

    /// <summary>
    /// Gets a collection of JSON elements that matches the JSONPath expression.
    /// </summary>
    /// <param name="paths">A collection of JSONPath expressions.</param>
    /// <returns>The collection of the first valid expression or an empty one  if none is valid.</returns>
    public IEnumerable<JToken> GetJsonTokens(params string[] paths)
    {
        ThrowHelperIsLoaded();

        foreach (var path in paths)
        {
            var jTokens = _jsonObject?.SelectTokens(path);
            if (jTokens is not null)
                return jTokens;
        }
        return Enumerable.Empty<JToken>();
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
        return GetJsonTokenWithValue(indices).Value<T>();
    }

    /// <summary>
    /// Gets the actual value of the JSON element that matches the first valid JSONPath expression.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="paths">A collection of JSONPath expressions.</param>
    /// <returns>The value of the first valid expression.</returns>
    public T? GetJsonValue<T>(params string[] paths)
    {
        ThrowHelperIsLoaded();
        return _jsonObject!.GetValue<T>(paths);
    }

    /// <summary>
    /// Gets the actual values of all JSON elements that matches the first valid JSONPath expression.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="paths">A collection of JSONPath expressions.</param>
    /// <returns>The value of the first valid expression.</returns>
    public IEnumerable<T?> GetJsonValues<T>(params string[] paths)
    {
        ThrowHelperIsLoaded();
        return _jsonObject!.GetValues<T>(paths);
    }

    // private //

    private JToken GetJsonTokenWithValue(ReadOnlySpan<int> indices)
    {
        ThrowHelperIsLoaded();
        Guard.HasSizeGreaterThan(indices, 0, nameof(indices));

        JToken? jToken = _jsonObject;
        for (var i = 0; i < indices.Length; i++)
        {
            var index = indices[i];
            var jPath = jToken!.Path;

            if (jToken is JArray jArray)
            {
                jToken = jArray.ContainsIndex(index) ? jToken[index] : null;
            }
            else if (jToken is JObject jObject)
            {
                jToken = jObject.Children().ElementAtOrDefault(index);
            }

            if (jToken is JProperty jProperty)
                jToken = jProperty.Value;

            if (jToken is null)
                ThrowHelper.ThrowInvalidOperationException($"Index {indices[i]} at position {i} is not available ({jPath}).");
        }
        return jToken!;
    }

    #endregion

    #region Setter

    public void SetJsonObject(JObject? value)
    {
        _jsonObject = value;

        IsSynced = false;
        UsesMapping = _jsonObject?.UsesMapping() == true;
    }

    public void SetJsonValue(JToken value, ReadOnlySpan<int> indices)
    {
        GetJsonTokenWithValue(indices).Replace(value);

        IsSynced = false;
    }

    public void SetJsonValue(JToken value, params string[] paths)
    {
        ThrowHelperIsLoaded();
        Guard.HasSizeGreaterThan(paths, 0, nameof(paths));

        // If setting the value was successfull mark as unsynced.
        IsSynced = !_jsonObject!.SetValue(value, paths);
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

    public Container(int metaIndex) : this(metaIndex, new()) { }

    internal Container(int metaIndex, PlatformExtra extra)
    {
        CollectionIndex = metaIndex - Constants.OFFSET_INDEX;
        Extra = extra;
        MetaIndex = metaIndex;

        SaveType = (SaveTypeEnum)(CollectionIndex % 2);
        SlotIndex = CollectionIndex / 2; // integer division

        Identifier = MetaIndex == 0 ? "AccountData" : $"Slot{SlotIndex + 1}{SaveType}";
        PersistentStorageSlot = MetaIndex == 0 ? StoragePersistentSlotEnum.AccountData : (StoragePersistentSlotEnum)(MetaIndex);
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

        return $"{nameof(Container)} {MetaIndex:D2} {Identifier}{(e ?? string.Empty)}";
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
    /// Refreshes all <see cref="FileInfo"/> used for a save.
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
        _saveVersion = 0;

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
