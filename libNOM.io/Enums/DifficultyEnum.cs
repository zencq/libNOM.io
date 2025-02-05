namespace libNOM.io.Enums;


// EXTERNAL RELEASE: if any, apply changes from libMBIN.
// https://github.com/monkeyman192/MBINCompiler/blob/development/libMBIN/Source/NMS/GameComponents/GcActiveSurvivalBarsDifficultyOption.cs#L7
internal enum ActiveSurvivalBarsDifficultyEnum : uint
{
    None,
    HealthOnly,
    HealthAndHazard,
    All,
}

// https://github.com/monkeyman192/MBINCompiler/blob/development/libMBIN/Source/NMS/GameComponents/GcBreakTechOnDamageDifficultyOption.cs#L7
internal enum BreakTechOnDamageProbabilityEnum : uint
{
    None,
    Low,
    High,
}

// https://github.com/monkeyman192/MBINCompiler/blob/development/libMBIN/Source/NMS/GameComponents/GcChargingRequirementsDifficultyOption.cs#L7
internal enum ChargingRequirementsDifficultyEnum : uint
{
    None,
    Low,
    Normal,
    High,
}

// https://github.com/monkeyman192/MBINCompiler/blob/development/libMBIN/Source/NMS/GameComponents/GcCombatTimerDifficultyOption.cs#L7
internal enum CombatTimerDifficultyOptionEnum : uint
{
    Off,
    Slow,
    Normal,
    Fast,
}

// https://github.com/monkeyman192/MBINCompiler/blob/development/libMBIN/Source/NMS/GameComponents/GcCreatureHostilityDifficultyOption.cs#L7
internal enum CreatureHostilityDifficultyEnum : uint
{
    NeverAttack,
    AttackIfProvoked,
    FullEcosystem,
}

// https://github.com/monkeyman192/MBINCompiler/blob/development/libMBIN/Source/NMS/GameComponents/GcCurrencyCostDifficultyOption.cs#L7
internal enum CurrencyCostDifficultyEnum : uint
{
    Free,
    Cheap,
    Normal,
    Expensive,
}

// https://github.com/monkeyman192/MBINCompiler/blob/development/libMBIN/Source/NMS/GameComponents/GcDamageGivenDifficultyOption.cs#L7
internal enum DamageGivenDifficultyEnum : uint
{
    High,
    Normal,
    Low,
}

// https://github.com/monkeyman192/MBINCompiler/blob/development/libMBIN/Source/NMS/GameComponents/GcDamageReceivedDifficultyOption.cs#L7
internal enum DamageReceivedDifficultyEnum : uint
{
    None,
    Low,
    Normal,
    High,
}

// https://github.com/monkeyman192/MBINCompiler/blob/development/libMBIN/Source/NMS/GameComponents/GcDeathConsequencesDifficultyOption.cs#L7
internal enum DeathConsequencesDifficultyEnum : uint
{
    None,
    ItemGrave,
    DestroyItems,
    DestroySave,
}

// https://github.com/monkeyman192/MBINCompiler/blob/development/libMBIN/Source/NMS/GameComponents/GcEnergyDrainDifficultyOption.cs#L7
internal enum EnergyDrainDifficultyEnum : uint
{
    Slow,
    Normal,
    Fast,
}

// https://github.com/monkeyman192/MBINCompiler/blob/development/libMBIN/Source/NMS/GameComponents/GcFishingDifficultyOption.cs#L7
public enum FishingDifficultyEnum : uint // added in Aquarius 5.10
{
    AutoCatch,
    LongCatchWindow,
    NormalCatchWindow,
    ShortCatchWindow,
}

// https://github.com/monkeyman192/MBINCompiler/blob/development/libMBIN/Source/NMS/GameComponents/GcFuelUseDifficultyOption.cs#L7
internal enum FuelUseDifficultyEnum : uint
{
    Free,
    Cheap,
    Normal,
    Expensive,
}

// https://github.com/monkeyman192/MBINCompiler/blob/development/libMBIN/Source/NMS/GameComponents/GcHazardDrainDifficultyOption.cs#L7
internal enum HazardDrainDifficultyEnum : uint
{
    Slow,
    Normal,
    Fast,
}

// https://github.com/monkeyman192/MBINCompiler/blob/development/libMBIN/Source/NMS/GameComponents/GcInventoryStackLimitsDifficultyOption.cs#L7
internal enum InventoryStackLimitsDifficultyEnum : uint
{
    High,
    Normal,
    Low,
}

// https://github.com/monkeyman192/MBINCompiler/blob/development/libMBIN/Source/NMS/GameComponents/GcItemShopAvailabilityDifficultyOption.cs#L7
internal enum ItemShopAvailabilityDifficultyEnum : uint
{
    High,
    Normal,
    Low,
}

// https://github.com/monkeyman192/MBINCompiler/blob/development/libMBIN/Source/NMS/GameComponents/GcLaunchFuelCostDifficultyOption.cs#L7
internal enum LaunchFuelCostDifficultyEnum : uint
{
    Free,
    Low,
    Normal,
    High,
}

// https://github.com/monkeyman192/MBINCompiler/blob/development/libMBIN/Source/NMS/GameComponents/GcNPCPopulationDifficultyOption.cs#L7
internal enum NPCPopulationDifficultyEnum : uint // added in Worlds Part II 5.50
{
    Full,
    Abandoned,
}

// https://github.com/monkeyman192/MBINCompiler/blob/development/libMBIN/Source/NMS/GameComponents/GcReputationGainDifficultyOption.cs#L7
internal enum ReputationGainDifficultyEnum : uint
{
    VeryFast,
    Fast,
    Normal,
    Slow,
}

// https://github.com/monkeyman192/MBINCompiler/blob/development/libMBIN/Source/NMS/GameComponents/GcScannerRechargeDifficultyOption.cs#L7
internal enum ScannerRechargeDifficultyEnum : uint
{
    VeryFast,
    Fast,
    Normal,
    Slow,
}

// https://github.com/monkeyman192/MBINCompiler/blob/development/libMBIN/Source/NMS/GameComponents/GcSprintingCostDifficultyOption.cs#L7
internal enum SprintingCostDifficultyEnum : uint
{
    Free,
    Low,
    Full,
}

// https://github.com/monkeyman192/MBINCompiler/blob/development/libMBIN/Source/NMS/GameComponents/GcSubstanceCollectionDifficultyOption.cs#L7
internal enum SubstanceCollectionDifficultyEnum : uint
{
    High,
    Normal,
    Low,
}
