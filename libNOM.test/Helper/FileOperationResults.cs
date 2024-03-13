namespace libNOM.test.Helper;


public record class FileOperationResults(
    string GameMode, // PresetGameModeEnum
    DifficultyPresetTypeEnum Difficulty,
    SeasonEnum Season,
    int BaseVersion,
    GameVersionEnum GameVersion,
    uint TotalPlayTime
);
