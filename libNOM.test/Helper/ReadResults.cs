namespace libNOM.test.Helper;


public record class ReadResults(
    int CollectionIndex,
    string Identifier,
    bool Exists,
    bool IsCompatible,
    bool IsOld,
    bool HasBase,
    bool HasFreighter,
    bool HasSettlement,
    bool HasActiveExpedition,
    bool CanSwitchContext,
    SaveContextQueryEnum ActiveContext,
    string GameMode, // PresetGameModeEnum
    DifficultyPresetTypeEnum Difficulty,
    SeasonEnum Season,
    int BaseVersion,
    int SaveVersion,
    GameVersionEnum GameVersion,
    string SaveName,
    string SaveSummary,
    ulong TotalPlayTime
);
