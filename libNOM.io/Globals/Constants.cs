namespace libNOM.io.Globals;


public static class Constants
{
    // public //

    // G = General
    // M = Microsoft
    // O = Gog
    // P = Playstation
    // S = Steam
    // W = Switch
    public const string INCOMPATIBILITY_001 = "001G_Empty";
    public const string INCOMPATIBILITY_002 = "002G_Deserialization_Exception";
    public const string INCOMPATIBILITY_003 = "003G_Deserialization_Null";
    public const string INCOMPATIBILITY_004 = "004M_Microsoft_Deleted";
    public const string INCOMPATIBILITY_005 = "005M_Microsoft_Missing_Blob";
    public const string INCOMPATIBILITY_006 = "006G_Non_Existent";

    // internal //

    internal const int CACHE_EXPIRATION = 250; // milliseconds

    internal static readonly DifficultyPresetData DIFFICULTY_PRESET_NORMAL = new(ActiveSurvivalBarsDifficultyEnum.All, HazardDrainDifficultyEnum.Normal, EnergyDrainDifficultyEnum.Normal, SubstanceCollectionDifficultyEnum.Normal, SprintingCostDifficultyEnum.Full, ScannerRechargeDifficultyEnum.Normal, DamageReceivedDifficultyEnum.Normal, BreakTechOnDamageProbabilityEnum.Low, DeathConsequencesDifficultyEnum.ItemGrave, ChargingRequirementsDifficultyEnum.Normal, FuelUseDifficultyEnum.Normal, LaunchFuelCostDifficultyEnum.Normal, false, CurrencyCostDifficultyEnum.Normal, ItemShopAvailabilityDifficultyEnum.Normal, InventoryStackLimitsDifficultyEnum.High, DamageGivenDifficultyEnum.Normal, CombatTimerDifficultyOptionEnum.Normal, CombatTimerDifficultyOptionEnum.Normal, CreatureHostilityDifficultyEnum.FullEcosystem, false, true, false, ReputationGainDifficultyEnum.Normal);
    internal static readonly DifficultyPresetData DIFFICULTY_PRESET_CREATIVE = new(ActiveSurvivalBarsDifficultyEnum.None, HazardDrainDifficultyEnum.Slow, EnergyDrainDifficultyEnum.Slow, SubstanceCollectionDifficultyEnum.Normal, SprintingCostDifficultyEnum.Free, ScannerRechargeDifficultyEnum.Fast, DamageReceivedDifficultyEnum.None, BreakTechOnDamageProbabilityEnum.None, DeathConsequencesDifficultyEnum.None, ChargingRequirementsDifficultyEnum.None, FuelUseDifficultyEnum.Free, LaunchFuelCostDifficultyEnum.Free, true, CurrencyCostDifficultyEnum.Free, ItemShopAvailabilityDifficultyEnum.High, InventoryStackLimitsDifficultyEnum.High, DamageGivenDifficultyEnum.Normal, CombatTimerDifficultyOptionEnum.Off, CombatTimerDifficultyOptionEnum.Off, CreatureHostilityDifficultyEnum.NeverAttack, true, false, true, ReputationGainDifficultyEnum.Fast);
    internal static readonly DifficultyPresetData DIFFICULTY_PRESET_RELAXED = new(ActiveSurvivalBarsDifficultyEnum.HealthAndHazard, HazardDrainDifficultyEnum.Slow, EnergyDrainDifficultyEnum.Slow, SubstanceCollectionDifficultyEnum.High, SprintingCostDifficultyEnum.Low, ScannerRechargeDifficultyEnum.VeryFast, DamageReceivedDifficultyEnum.Low, BreakTechOnDamageProbabilityEnum.None, DeathConsequencesDifficultyEnum.None, ChargingRequirementsDifficultyEnum.Low, FuelUseDifficultyEnum.Cheap, LaunchFuelCostDifficultyEnum.Low, false, CurrencyCostDifficultyEnum.Cheap, ItemShopAvailabilityDifficultyEnum.High, InventoryStackLimitsDifficultyEnum.High, DamageGivenDifficultyEnum.High, CombatTimerDifficultyOptionEnum.Slow, CombatTimerDifficultyOptionEnum.Slow, CreatureHostilityDifficultyEnum.AttackIfProvoked, true, true, true, ReputationGainDifficultyEnum.Fast);
    internal static readonly DifficultyPresetData DIFFICULTY_PRESET_SURVIVAL = new(ActiveSurvivalBarsDifficultyEnum.All, HazardDrainDifficultyEnum.Fast, EnergyDrainDifficultyEnum.Fast, SubstanceCollectionDifficultyEnum.Low, SprintingCostDifficultyEnum.Full, ScannerRechargeDifficultyEnum.Normal, DamageReceivedDifficultyEnum.High, BreakTechOnDamageProbabilityEnum.High, DeathConsequencesDifficultyEnum.DestroyItems, ChargingRequirementsDifficultyEnum.High, FuelUseDifficultyEnum.Expensive, LaunchFuelCostDifficultyEnum.High, false, CurrencyCostDifficultyEnum.Normal, ItemShopAvailabilityDifficultyEnum.Low, InventoryStackLimitsDifficultyEnum.Normal, DamageGivenDifficultyEnum.Normal, CombatTimerDifficultyOptionEnum.Fast, CombatTimerDifficultyOptionEnum.Fast, CreatureHostilityDifficultyEnum.FullEcosystem, false, true, false, ReputationGainDifficultyEnum.Normal);

    internal const string FILE_TIMESTAMP_FORMAT = "yyyyMMddHHmmssfff";

    internal const short GAMEMODE_INT_PERMADEATH = (int)(PresetGameModeEnum.Permadeath); // 5
    internal const short GAMEMODE_INT_SEASONAL = (int)(PresetGameModeEnum.Seasonal); // 6

    internal const GameVersionEnum LOWEST_SUPPORTED_VERSION = GameVersionEnum.BeyondWithVehicleCam;

    internal const int OFFSET_GAMEMODE = 512;
    internal const int OFFSET_INDEX = 2;
    internal const int OFFSET_SEASON = 128;

    internal const uint SAVE_FORMAT_1 = 0x7D0; // 2000 (1.0) // not used but for completeness
    internal const uint SAVE_FORMAT_2 = 0x7D1; // 2001 (1.1)
    internal const uint SAVE_FORMAT_3 = 0x7D2; // 2002 (3.6)

    internal const int SAVE_RENAMING_LENGTH_MANIFEST = 0x80; // 128
    internal const int SAVE_RENAMING_LENGTH_INGAME = 0x2A; // 42

    internal const uint SAVE_STREAMING_HEADER = 0xFEEDA1E5; // 4276986341
    internal const int SAVE_STREAMING_HEADER_TOTAL_LENGTH = 0x10; // 16
    internal const int SAVE_STREAMING_CHUNK_MAX_LENGTH = 0x80000; // 524288

    internal const int THRESHOLD_VANILLA = 4098;
    internal const int THRESHOLD_VANILLA_GAMEMODE = THRESHOLD_VANILLA + OFFSET_GAMEMODE;
    internal const int THRESHOLD_WAYPOINT = 4140;
    internal const int THRESHOLD_WAYPOINT_GAMEMODE = THRESHOLD_WAYPOINT + OFFSET_GAMEMODE;
}
