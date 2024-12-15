namespace libNOM.io.Enums;


/// <summary>
/// Specifies all available difficulties (PresetGameModeEnum was used for this before Waypoint).
/// </summary>
/// <seealso href="https://github.com/monkeyman192/MBINCompiler/blob/development/libMBIN/Source/NMS/GameComponents/GcDifficultyPresetType.cs#L7"/>
// EXTERNAL RELEASE: if any, apply changes from libMBIN.
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
