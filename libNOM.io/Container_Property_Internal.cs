using libNOM.io.Interfaces;

namespace libNOM.io;


/// <summary>
/// Holds all information about a single save.
/// </summary>
public partial class Container : IContainer
{
    #region Additional Information

    internal PlatformExtra Extra { get; set; }

    internal UserIdentification? UserIdentification { get; set; }

    #endregion

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
}
