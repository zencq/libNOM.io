using CommunityToolkit.Diagnostics;

namespace libNOM.io;


// This partial class contains public properties.
public partial class Container : IContainer
{
    #region Field

    private SaveContextQueryEnum _activeContext = SaveContextQueryEnum.DontCare;
    private GameVersionEnum _gameVersion = GameVersionEnum.Unknown;
    private int _saveVersion = -1;

    #endregion

    // public //

    public SaveContextQueryEnum ActiveContext // { get; set; }
    {
        get => _jsonObject?.GetValue<SaveContextQueryEnum>("ACTIVE_CONTEXT") ?? _activeContext;
        set
        {
            if (IsLoaded && value is SaveContextQueryEnum.Main or SaveContextQueryEnum.Season)
            {
                Guard.IsTrue(CanSwitchContext, nameof(CanSwitchContext)); // prevent switching context if only one exists
                SetJsonValue(value.ToString(), "ACTIVE_CONTEXT");
            }

            _activeContext = value;
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

    internal int SaveVersion // { get; set; }
    {
        get
        {
            if (_saveVersion < 0)
                _saveVersion = _jsonObject?.GetValue<int>("VERSION") ?? -1;

            return _saveVersion;
        }
        set
        {
            _saveVersion = value;
            if (IsLoaded && value >= 0) // only positive to not overwrite in JSON when resetting
                SetJsonValue(value, "VERSION");
        }
    }

    internal StoragePersistentSlotEnum PersistentStorageSlot { get; }

    internal IPlatform Platform { get; set; }
}
