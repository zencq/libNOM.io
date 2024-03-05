namespace libNOM.test.Helper;


public record class WriteResults(
    uint BaseVersion,
    ushort GameMode, // PresetGameModeEnum
    ushort Season, // SeasonEnum
    uint TotalPlayTime,

    string SaveName,
    string SaveSummary,
    byte Difficulty // DifficultyPresetTypeEnum
);
