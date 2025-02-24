namespace libNOM.test.Helper;


public record class WriteResults(
    uint MetaIndex,
    uint BaseVersion,
    ushort GameMode, // PresetGameModeEnum
    ushort Season, // SeasonEnum
    ulong TotalPlayTime,

    string SaveName,
    string SaveSummary,
    byte Difficulty // DifficultyPresetTypeEnum
);
