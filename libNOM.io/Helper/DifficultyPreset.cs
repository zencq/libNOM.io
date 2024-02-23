using Newtonsoft.Json.Linq;

namespace libNOM.io.Helper;


internal static partial class DifficultyPreset
{
    #region Getter

    /// <inheritdoc cref="Get(Container, JObject?)"/>
    internal static DifficultyPresetTypeEnum Get(Container container)
    {
        return Get(container, container.GetJsonObject());
    }

    /// <summary>
    /// Gets the difficulty based on the data inside the save.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="jsonObject"></param>
    /// <returns></returns>
    internal static DifficultyPresetTypeEnum Get(Container container, JObject? jsonObject)
    {
        if (jsonObject is null)
            return DifficultyPresetTypeEnum.Invalid;

        // Since Waypoint the difficulty is handed differently and therefore needs to be checked in more detail.
        if (container.IsVersion400Waypoint && container.GameMode == PresetGameModeEnum.Normal)
        {
            // Compare the current difficulty and then compare with the different presets.
            var difficulty = GetCurrentDifficulty(jsonObject);

            if (difficulty.Equals(Constants.DIFFICULTY_PRESET_NORMAL))
                return DifficultyPresetTypeEnum.Normal;

            if (difficulty.Equals(Constants.DIFFICULTY_PRESET_CREATIVE))
                return DifficultyPresetTypeEnum.Creative;

            if (difficulty.Equals(Constants.DIFFICULTY_PRESET_RELAXED))
                return DifficultyPresetTypeEnum.Relaxed;

            if (difficulty.Equals(Constants.DIFFICULTY_PRESET_SURVIVAL))
                return DifficultyPresetTypeEnum.Survival;

            return DifficultyPresetTypeEnum.Custom;
        }

        return PreWaypointDifficulty(container);
    }

    /// <inheritdoc cref="Get(Container, JObject?)"/>
    /// <param name="json"></param>
    internal static DifficultyPresetTypeEnum Get(Container container, string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return DifficultyPresetTypeEnum.Invalid;

        // Since Omega there can be two difficulties in one save and therefore not determined reliable from a string. Also SeasonData is now stored before DifficultyState.
        if (container.IsVersion450Omega)
        {
            return DifficultyPresetTypeEnum.Custom;
        }
        // Since Waypoint the difficulty is handed differently and therefore needs to be checked in more detail.
        else if (container.IsVersion400Waypoint && container.GameMode == PresetGameModeEnum.Normal)
        {
            // DifficultyState is stored again in SeasonData and therefore cut off here at an appropriate length.
            // DifficultyState ends at around 800 characters if save name and summary are not set. Both are limited to 128 chars so 1500 should be more than enough.
            var jsonSubstring = json.AsSpan(0, 1500);

            if (IsPreset(jsonSubstring, Constants.DIFFICULTY_PRESET_NORMAL))
                return DifficultyPresetTypeEnum.Normal;

            if (IsPreset(jsonSubstring, Constants.DIFFICULTY_PRESET_CREATIVE))
                return DifficultyPresetTypeEnum.Creative;

            if (IsPreset(jsonSubstring, Constants.DIFFICULTY_PRESET_SURVIVAL))
                return DifficultyPresetTypeEnum.Survival;

            if (IsPreset(jsonSubstring, Constants.DIFFICULTY_PRESET_RELAXED))
                return DifficultyPresetTypeEnum.Relaxed;

            return DifficultyPresetTypeEnum.Custom;
        }

        return PreWaypointDifficulty(container);
    }

    #endregion

    #region Setter

    internal static void Set(Container container, DifficultyPresetData preset)
    {
        var jsonObject = container.GetJsonObject();

        // Survival Elements
        jsonObject.SetValue(preset.ActiveSurvivalBars.ToString(), "DIFFICULTY_ACTIVE_SURVIVAL_BARS");

        // Survival Difficulty
        jsonObject.SetValue(preset.HazardDrain.ToString(), "DIFFICULTY_HAZARD_DRAIN");
        jsonObject.SetValue(preset.EnergyDrain.ToString(), "DIFFICULTY_ENERGY_DRAIN");

        // Natural Resources
        jsonObject.SetValue(preset.SubstanceCollection.ToString(), "DIFFICULTY_SUBSTANCE_COLLECTION");

        // Sprinting
        jsonObject.SetValue(preset.SprintingCost.ToString(), "DIFFICULTY_SPRINTING_COST");

        // Scanner Recharge
        jsonObject.SetValue(preset.ScannerRecharge.ToString(), "DIFFICULTY_SCANNER_RECHARGE");

        // Damage Levels
        jsonObject.SetValue(preset.DamageReceived.ToString(), "DIFFICULTY_DAMAGE_RECEIVED");

        // Technology Damage
        jsonObject.SetValue(preset.BreakTechOnDamage.ToString(), "DIFFICULTY_BREAK_TECH_ON_DAMAGE");

        // Death Consequences
        jsonObject.SetValue(preset.DeathConsequences.ToString(), "DIFFICULTY_DEATH_CONSEQUENCES");

        // Fuel Usage
        jsonObject.SetValue(preset.ChargingRequirements.ToString(), "DIFFICULTY_CHARGING_REQUIREMENTS");
        jsonObject.SetValue(preset.FuelUse.ToString(), "DIFFICULTY_FUEL_USE");
        jsonObject.SetValue(preset.LaunchFuelCost.ToString(), "DIFFICULTY_LAUNCH_FUEL_COST");

        // Crafting
        jsonObject.SetValue(preset.CraftingIsFree, "DIFFICULTY_CRAFTING_IS_FREE");

        // Purchases
        jsonObject.SetValue(preset.CurrencyCost.ToString(), "DIFFICULTY_CURRENCY_COST");

        // Goods Availability
        jsonObject.SetValue(preset.ItemShopAvailability.ToString(), "DIFFICULTY_ITEM_SHOP_AVAILABILITY");

        // Inventory Stack Limits
        jsonObject.SetValue(preset.InventoryStackLimits.ToString(), "DIFFICULTY_INVENTORY_STACK_LIMITS");

        // Enemy Strength
        jsonObject.SetValue(preset.DamageGiven.ToString(), "DIFFICULTY_DAMAGE_GIVEN");

        // On-Foot Combat
        jsonObject.SetValue(preset.GroundCombatTimers.ToString(), "DIFFICULTY_GROUND_COMBAT_TIMERS");

        // Space Combat
        jsonObject.SetValue(preset.SpaceCombatTimers.ToString(), "DIFFICULTY_SPACE_COMBAT_TIMERS");

        // Creatures
        jsonObject.SetValue(preset.CreatureHostility.ToString(), "DIFFICULTY_CREATURE_HOSTILITY");

        // Inventory Transfer Range
        jsonObject.SetValue(preset.InventoriesAlwaysInRange, "DIFFICULTY_INVENTORIES_ALWAYS_IN_RANGE");

        // Hyperdrive System Access
        jsonObject.SetValue(preset.WarpDriveRequirements, "DIFFICULTY_WARP_DRIVE_REQUIREMENTS");

        // Base Power
        jsonObject.SetValue(preset.BaseAutoPower, "DIFFICULTY_BASE_AUTO_POWER");

        // Reputation & Standing Gain
        jsonObject.SetValue(preset.ReputationGain.ToString(), "DIFFICULTY_REPUTATION_GAIN");
    }

    #endregion

    #region Helper

    private static DifficultyPresetData GetCurrentDifficulty(JObject jsonObject)
    {
        // Survival Elements
        var activeSurvivalBars = jsonObject.GetValue<ActiveSurvivalBarsDifficultyEnum>("DIFFICULTY_ACTIVE_SURVIVAL_BARS");

        // Survival Difficulty
        var hazardDrain = jsonObject.GetValue<HazardDrainDifficultyEnum>("DIFFICULTY_HAZARD_DRAIN");
        var energyDrain = jsonObject.GetValue<EnergyDrainDifficultyEnum>("DIFFICULTY_ENERGY_DRAIN");

        // Natural Resources
        var substanceCollection = jsonObject.GetValue<SubstanceCollectionDifficultyEnum>("DIFFICULTY_SUBSTANCE_COLLECTION");

        // Sprinting
        var sprintingCost = jsonObject.GetValue<SprintingCostDifficultyEnum>("DIFFICULTY_SPRINTING_COST");

        // Scanner Recharge
        var scannerRecharge = jsonObject.GetValue<ScannerRechargeDifficultyEnum>("DIFFICULTY_SCANNER_RECHARGE");

        // Damage Levels
        var damageReceived = jsonObject.GetValue<DamageReceivedDifficultyEnum>("DIFFICULTY_DAMAGE_RECEIVED");

        // Technology Damage
        var breakTechOnDamage = jsonObject.GetValue<BreakTechOnDamageProbabilityEnum>("DIFFICULTY_BREAK_TECH_ON_DAMAGE");

        // Death Consequences
        var deathConsequences = jsonObject.GetValue<DeathConsequencesDifficultyEnum>("DIFFICULTY_DEATH_CONSEQUENCES");

        // Fuel Usage
        var chargingRequirements = jsonObject.GetValue<ChargingRequirementsDifficultyEnum>("DIFFICULTY_CHARGING_REQUIREMENTS");
        var fuelUse = jsonObject.GetValue<FuelUseDifficultyEnum>("DIFFICULTY_FUEL_USE");
        var launchFuelCost = jsonObject.GetValue<LaunchFuelCostDifficultyEnum>("DIFFICULTY_LAUNCH_FUEL_COST");

        // Crafting
        var craftingIsFree = jsonObject.GetValue<bool>("DIFFICULTY_CRAFTING_IS_FREE");

        // Purchases
        var currencyCost = jsonObject.GetValue<CurrencyCostDifficultyEnum>("DIFFICULTY_CURRENCY_COST");

        // Goods Availability
        var itemShopAvailability = jsonObject.GetValue<ItemShopAvailabilityDifficultyEnum>("DIFFICULTY_ITEM_SHOP_AVAILABILITY");

        // Inventory Stack Limits
        var inventoryStackLimits = jsonObject.GetValue<InventoryStackLimitsDifficultyEnum>("DIFFICULTY_INVENTORY_STACK_LIMITS");

        // Enemy Strength
        var damageGiven = jsonObject.GetValue<DamageGivenDifficultyEnum>("DIFFICULTY_DAMAGE_GIVEN");

        // On-Foot Combat
        var groundCombatTimers = jsonObject.GetValue<CombatTimerDifficultyOptionEnum>("DIFFICULTY_GROUND_COMBAT_TIMERS");

        // Space Combat
        var spaceCombatTimers = jsonObject.GetValue<CombatTimerDifficultyOptionEnum>("DIFFICULTY_SPACE_COMBAT_TIMERS");

        // Creatures
        var creatureHostility = jsonObject.GetValue<CreatureHostilityDifficultyEnum>("DIFFICULTY_CREATURE_HOSTILITY");

        // Inventory Transfer Range
        var inventoriesAlwaysInRange = jsonObject.GetValue<bool>("DIFFICULTY_INVENTORIES_ALWAYS_IN_RANGE");

        // Hyperdrive System Access
        var warpDriveRequirements = jsonObject.GetValue<bool>("DIFFICULTY_WARP_DRIVE_REQUIREMENTS");

        // Base Power
        var baseAutoPower = jsonObject.GetValue<bool>("DIFFICULTY_BASE_AUTO_POWER");

        // Reputation & Standing Gain
        var reputationGain = jsonObject.GetValue<ReputationGainDifficultyEnum>("DIFFICULTY_REPUTATION_GAIN");

        return new(activeSurvivalBars, hazardDrain, energyDrain, substanceCollection, sprintingCost, scannerRecharge, damageReceived, breakTechOnDamage, deathConsequences, chargingRequirements, fuelUse, launchFuelCost, craftingIsFree, currencyCost, itemShopAvailability, inventoryStackLimits, damageGiven, groundCombatTimers, spaceCombatTimers, creatureHostility, inventoriesAlwaysInRange, warpDriveRequirements, baseAutoPower, reputationGain);
    }

    private static bool IsPreset(ReadOnlySpan<char> json, DifficultyPresetData preset)
    {
        string[] settings = [
            // Survival Elements
            $"\"tEx\":{{\"ZeS\":\"{preset.ActiveSurvivalBars}\"",

            // Survival Difficulty
            $"\"bGK\":{{\"ORx\":\"{preset.HazardDrain}\"",
            $"\"A:s\":{{\"Dn>\":\"{preset.EnergyDrain}\"",

            // Natural Resources
            $"\"jH@\":{{\"9JJ\":\"{preset.SubstanceCollection}\"",

            // Sprinting
            $"\"l29\":{{\"LT:\":\"{preset.SprintingCost}\"",

            // Scanner Recharge
            $"\"Lf?\":{{\"gFS\":\"{preset.ScannerRecharge}\"",

            // Damage Levels
            $"\"hXp\":{{\"cYk\":\"{preset.DamageReceived}\"",

            // Technology Damage
            $"\"gd>\":{{\"ef4\":\"{preset.BreakTechOnDamage}\"",

            // Death Consequences
            $"\"n7p\":{{\"q2@\":\"{preset.DeathConsequences}\"",

            // Fuel Usage
            $"\"nhq\":{{\"428\":\"{preset.ChargingRequirements}\"",
            $"\"jnM\":{{\"Eg1\":\"{preset.FuelUse}\"",
            $"\"A9D\":{{\"iqY\":\"{preset.LaunchFuelCost}\"",

            // Crafting
            $"\"?Dt\":{preset.CraftingIsFree},",

            // Purchases
            $"\"tsk\":{{\"Ubk\":\"{preset.CurrencyCost}\"",

            // Goods Availability
            $"\"FB5\":{{\"TYf\":\"{preset.ItemShopAvailability}\"",

            // Inventory Stack Limits
            $"\"kZ5\":{{\"?SS\":\"{preset.InventoryStackLimits}\"",

            // Enemy Strength
            $"\"PYQ\":{{\"mum\":\"{preset.DamageGiven}\"",

            // On-Foot Combat
            $"\"jGh\":{{\"ZbV\":\"{preset.GroundCombatTimers}\"",

            // Space Combat
            $"\"Od7\":{{\"ZbV\":\"{preset.SpaceCombatTimers}\"",

            // Creatures
            $"\"BbG\":{{\"1c;\":\"{preset.CreatureHostility}\"",

            // Inventory Transfer Range
            $"\"pS0\":{preset.InventoriesAlwaysInRange},",

            // Hyperdrive System Access
            $"\"aw9\":{preset.WarpDriveRequirements},",

            // Base Power
            $"\"uo4\":{preset.BaseAutoPower},",

            // Reputation & Standing Gain
            $"\"vo>\":{{\"S@3\":\"{preset.ReputationGain}\"",
        ];

        foreach (var value in settings)
            if (json.Contains(value.AsSpan(), StringComparison.OrdinalIgnoreCase))
                return false;

        // All values are there, therefore it matches the preset.
        return true;
    }

    private static DifficultyPresetTypeEnum PreWaypointDifficulty(Container container) => container.GameMode switch
    {
        PresetGameModeEnum.Unspecified => DifficultyPresetTypeEnum.Invalid,
        PresetGameModeEnum.Creative => DifficultyPresetTypeEnum.Creative,
        PresetGameModeEnum.Survival => DifficultyPresetTypeEnum.Survival,
        PresetGameModeEnum.Permadeath => DifficultyPresetTypeEnum.Permadeath,
        _ => DifficultyPresetTypeEnum.Normal,
    };

    #endregion
}
