namespace libNOM.io.Enums;

/// <summary>
/// Specifies all available difficulties (PresetGameModeEnum was used for this before 4.00).
/// </summary>
/// <seealso href="https://github.com/monkeyman192/MBINCompiler/blob/development/libMBIN/Source/NMS/GameComponents/GcDifficultyPresetType.cs#L7"/>
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
