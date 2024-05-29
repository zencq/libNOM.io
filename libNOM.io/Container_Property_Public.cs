using System.Collections.ObjectModel;

using CommunityToolkit.Diagnostics;

using libNOM.io.Trace;

namespace libNOM.io;


// This partial class contains internal properties.
public partial class Container : IContainer
{
    #region Field

    private bool? _exists;
    private GameVersionEnum _gameVersion = GameVersionEnum.Unknown;

    #endregion

    public ObservableCollection<IContainer> BackupCollection { get; } = [];

    public string Identifier { get; }

    public Exception? IncompatibilityException { get; internal set; }

    public string? IncompatibilityTag { get; internal set; }

    public ContainerTrace? Trace { get; internal set; }

    public HashSet<string> UnknownKeys { get; internal set; } = [];

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

    #region IsVersion

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

    public bool IsVersion460Orbital => IsVersion(GameVersionEnum.Orbital); // { get; }

    public bool IsVersion470Adrift => IsVersion(GameVersionEnum.Adrift); // { get; }

    #endregion

    #region Save

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

    #endregion
}
