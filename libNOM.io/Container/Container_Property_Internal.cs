namespace libNOM.io;


// This partial class contains public properties.
public partial class Container : IContainer
{
    #region Field

    private int _saveVersion = -1;

    #endregion

    internal ContainerExtra Extra { get; set; }

    internal UserIdentification? UserIdentification { get; set; }

    #region Save

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

    #endregion
}
