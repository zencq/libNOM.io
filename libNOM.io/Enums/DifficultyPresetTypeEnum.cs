namespace libNOM.io.Enums;

/// <summary>
/// Specifies all available difficulties (PresetGameModeEnum was used for this before 4.00).
/// </summary>
/// <seealso cref="libMBIN\Source\NMS\GameComponents\GcDifficultyPresetType.cs"/>
public enum DifficultyPresetTypeEnum : uint
{
    Invalid,
    Custom,
    Normal,
    Creative,
    Relaxed,
    Survival,
    Permadeath,
}
