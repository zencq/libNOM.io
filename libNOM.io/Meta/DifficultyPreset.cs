using Newtonsoft.Json.Linq;

namespace libNOM.io.Meta;


// EXTERNAL RELEASE: Update if any changes related to the difficulty have been made.
internal static class DifficultyPreset
{
    #region Getter

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
            return GetCurrentDifficulty(jsonObject, container.ActiveContext) switch
            {
                var difficulty when IsPreset(difficulty, Constants.DIFFICULTY_PRESET_NORMAL) => DifficultyPresetTypeEnum.Normal,
                var difficulty when IsPreset(difficulty, Constants.DIFFICULTY_PRESET_CREATIVE) => DifficultyPresetTypeEnum.Creative,
                var difficulty when IsPreset(difficulty, Constants.DIFFICULTY_PRESET_RELAXED) => DifficultyPresetTypeEnum.Relaxed,
                var difficulty when IsPreset(difficulty, Constants.DIFFICULTY_PRESET_SURVIVAL) => DifficultyPresetTypeEnum.Survival,
                _ => DifficultyPresetTypeEnum.Custom,
            };

        return PreWaypointDifficulty(container);
    }

    /// <inheritdoc cref="Get(Container, JObject?)"/>
    /// <param name="json"></param>
    internal static DifficultyPresetTypeEnum Get(Container container, string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return DifficultyPresetTypeEnum.Invalid;

        // Since Omega 4.50 there can be two difficulties in one save and therefore not determined reliable from a string. Also SeasonData is now stored before DifficultyState.
        if (container.IsVersion450Omega)
        {
            return DifficultyPresetTypeEnum.Custom;
        }
        // Since Waypoint the difficulty is handed differently and therefore needs to be checked in more detail.
        else if (container.IsVersion400Waypoint && container.GameMode == PresetGameModeEnum.Normal)
        {
            // DifficultyState is stored again in SeasonData and therefore cut off here at an appropriate length.
            // DifficultyState ends at around 800 characters if save name and summary are not set. Both are limited to 128 chars so 1500 should be more than enough.
            return json.AsSpan(0, 1500).ToString() switch
            {
                var jsonSubstring when IsPreset(jsonSubstring, Constants.DIFFICULTY_PRESET_NORMAL) => DifficultyPresetTypeEnum.Normal,
                var jsonSubstring when IsPreset(jsonSubstring, Constants.DIFFICULTY_PRESET_CREATIVE) => DifficultyPresetTypeEnum.Creative,
                var jsonSubstring when IsPreset(jsonSubstring, Constants.DIFFICULTY_PRESET_RELAXED) => DifficultyPresetTypeEnum.Relaxed,
                var jsonSubstring when IsPreset(jsonSubstring, Constants.DIFFICULTY_PRESET_SURVIVAL) => DifficultyPresetTypeEnum.Survival,
                _ => DifficultyPresetTypeEnum.Custom,
            };
        }

        return PreWaypointDifficulty(container);
    }

    #endregion

    #region Setter

    internal static void Set(Container container, Difficulty preset)
    {
        var jsonObject = container.GetJsonObject();
        var parameters = new List<(JToken, string)> {
            // Survival Elements
            (preset.ActiveSurvivalBars.ToString(), "DIFFICULTY_ACTIVE_SURVIVAL_BARS"),

            // Survival Difficulty
            (preset.HazardDrain.ToString(), "DIFFICULTY_HAZARD_DRAIN"),
            (preset.EnergyDrain.ToString(), "DIFFICULTY_ENERGY_DRAIN"),

            // Natural Resources
            (preset.SubstanceCollection.ToString(), "DIFFICULTY_SUBSTANCE_COLLECTION"),

            // Sprinting
            (preset.SprintingCost.ToString(), "DIFFICULTY_SPRINTING_COST"),

            // Scanner Recharge
            (preset.ScannerRecharge.ToString(), "DIFFICULTY_SCANNER_RECHARGE"),

            // Damage Levels
            (preset.DamageReceived.ToString(), "DIFFICULTY_DAMAGE_RECEIVED"),

            // Technology Damage
            (preset.BreakTechOnDamage.ToString(), "DIFFICULTY_BREAK_TECH_ON_DAMAGE"),

            // Death Consequences
            (preset.DeathConsequences.ToString(), "DIFFICULTY_DEATH_CONSEQUENCES"),

            // Fuel Usage
            (preset.ChargingRequirements.ToString(), "DIFFICULTY_CHARGING_REQUIREMENTS"),
            (preset.FuelUse.ToString(), "DIFFICULTY_FUEL_USE"),
            (preset.LaunchFuelCost.ToString(), "DIFFICULTY_LAUNCH_FUEL_COST"),

            // Crafting
            (preset.CraftingIsFree, "DIFFICULTY_CRAFTING_IS_FREE"),

            // Purchases
            (preset.CurrencyCost.ToString(), "DIFFICULTY_CURRENCY_COST"),

            // Goods Availability
            (preset.ItemShopAvailability.ToString(), "DIFFICULTY_ITEM_SHOP_AVAILABILITY"),

            // Inventory Stack Limits
            (preset.InventoryStackLimits.ToString(), "DIFFICULTY_INVENTORY_STACK_LIMITS"),

            // Enemy Strength
            (preset.DamageGiven.ToString(), "DIFFICULTY_DAMAGE_GIVEN"),

            // On-Foot Combat
            (preset.GroundCombatTimers.ToString(), "DIFFICULTY_GROUND_COMBAT_TIMERS"),

            // Space Combat
            (preset.SpaceCombatTimers.ToString(), "DIFFICULTY_SPACE_COMBAT_TIMERS"),

            // Creatures
            (preset.CreatureHostility.ToString(), "DIFFICULTY_CREATURE_HOSTILITY"),

            // Inventory Transfer Range
            (preset.InventoriesAlwaysInRange, "DIFFICULTY_INVENTORIES_ALWAYS_IN_RANGE"),

            // Hyperdrive System Access
            (preset.WarpDriveRequirements, "DIFFICULTY_WARP_DRIVE_REQUIREMENTS"),

            // Base Power
            (preset.BaseAutoPower, "DIFFICULTY_BASE_AUTO_POWER"),

            // Reputation & Standing Gain
            (preset.ReputationGain.ToString(), "DIFFICULTY_REPUTATION_GAIN"),
        };

        if (container.IsVersion510Aquarius)
        {
            // Fishing Timing
            parameters.Add((preset.Fishing.ToString(), "DIFFICULTY_FISHING"));
        }

        if (container.IsVersion550WorldsPartII)
        {
            // Universe Population
            parameters.Add((preset.NPCPopulation.ToString(), "DIFFICULTY_NPC_POPULATION"));
        }

        foreach (var (value, pathIdentifier) in parameters)
            jsonObject.SetValue(value, pathIdentifier);
    }

    #endregion

    #region Helper

    private static Difficulty GetCurrentDifficulty(JObject jsonObject, SaveContextQueryEnum context)
    {
        // Survival Elements
        var activeSurvivalBars = jsonObject.GetValue<ActiveSurvivalBarsDifficultyEnum>("DIFFICULTY_ACTIVE_SURVIVAL_BARS", context);

        // Survival Difficulty
        var hazardDrain = jsonObject.GetValue<HazardDrainDifficultyEnum>("DIFFICULTY_HAZARD_DRAIN", context);
        var energyDrain = jsonObject.GetValue<EnergyDrainDifficultyEnum>("DIFFICULTY_ENERGY_DRAIN", context);

        // Natural Resources
        var substanceCollection = jsonObject.GetValue<SubstanceCollectionDifficultyEnum>("DIFFICULTY_SUBSTANCE_COLLECTION", context);

        // Sprinting
        var sprintingCost = jsonObject.GetValue<SprintingCostDifficultyEnum>("DIFFICULTY_SPRINTING_COST", context);

        // Scanner Recharge
        var scannerRecharge = jsonObject.GetValue<ScannerRechargeDifficultyEnum>("DIFFICULTY_SCANNER_RECHARGE", context);

        // Damage Levels
        var damageReceived = jsonObject.GetValue<DamageReceivedDifficultyEnum>("DIFFICULTY_DAMAGE_RECEIVED", context);

        // Technology Damage
        var breakTechOnDamage = jsonObject.GetValue<BreakTechOnDamageProbabilityEnum>("DIFFICULTY_BREAK_TECH_ON_DAMAGE", context);

        // Death Consequences
        var deathConsequences = jsonObject.GetValue<DeathConsequencesDifficultyEnum>("DIFFICULTY_DEATH_CONSEQUENCES", context);

        // Universe Population
        var npcPopulation = jsonObject.GetValue<NPCPopulationDifficultyEnum?>("DIFFICULTY_NPC_POPULATION", context);

        // Fuel Usage
        var chargingRequirements = jsonObject.GetValue<ChargingRequirementsDifficultyEnum>("DIFFICULTY_CHARGING_REQUIREMENTS", context);
        var fuelUse = jsonObject.GetValue<FuelUseDifficultyEnum>("DIFFICULTY_FUEL_USE", context);
        var launchFuelCost = jsonObject.GetValue<LaunchFuelCostDifficultyEnum>("DIFFICULTY_LAUNCH_FUEL_COST", context);

        // Crafting
        var craftingIsFree = jsonObject.GetValue<bool>("DIFFICULTY_CRAFTING_IS_FREE", context);

        // Purchases
        var currencyCost = jsonObject.GetValue<CurrencyCostDifficultyEnum>("DIFFICULTY_CURRENCY_COST", context);

        // Goods Availability
        var itemShopAvailability = jsonObject.GetValue<ItemShopAvailabilityDifficultyEnum>("DIFFICULTY_ITEM_SHOP_AVAILABILITY", context);

        // Inventory Stack Limits
        var inventoryStackLimits = jsonObject.GetValue<InventoryStackLimitsDifficultyEnum>("DIFFICULTY_INVENTORY_STACK_LIMITS", context);

        // Enemy Strength
        var damageGiven = jsonObject.GetValue<DamageGivenDifficultyEnum>("DIFFICULTY_DAMAGE_GIVEN", context);

        // On-Foot Combat
        var groundCombatTimers = jsonObject.GetValue<CombatTimerDifficultyOptionEnum>("DIFFICULTY_GROUND_COMBAT_TIMERS", context);

        // Space Combat
        var spaceCombatTimers = jsonObject.GetValue<CombatTimerDifficultyOptionEnum>("DIFFICULTY_SPACE_COMBAT_TIMERS", context);

        // Creatures
        var creatureHostility = jsonObject.GetValue<CreatureHostilityDifficultyEnum>("DIFFICULTY_CREATURE_HOSTILITY", context);

        // Inventory Transfer Range
        var inventoriesAlwaysInRange = jsonObject.GetValue<bool>("DIFFICULTY_INVENTORIES_ALWAYS_IN_RANGE", context);

        // Hyperdrive System Access
        var warpDriveRequirements = jsonObject.GetValue<bool>("DIFFICULTY_WARP_DRIVE_REQUIREMENTS", context);

        // Base Power
        var baseAutoPower = jsonObject.GetValue<bool>("DIFFICULTY_BASE_AUTO_POWER", context);

        // Fishing Timing
        var fishing = jsonObject.GetValue<FishingDifficultyEnum?>("DIFFICULTY_FISHING", context);

        // Reputation & Standing Gain
        var reputationGain = jsonObject.GetValue<ReputationGainDifficultyEnum>("DIFFICULTY_REPUTATION_GAIN", context);

        return new(activeSurvivalBars, hazardDrain, energyDrain, substanceCollection, sprintingCost, scannerRecharge, damageReceived, breakTechOnDamage, deathConsequences, npcPopulation, chargingRequirements, fuelUse, launchFuelCost, craftingIsFree, currencyCost, itemShopAvailability, inventoryStackLimits, damageGiven, groundCombatTimers, spaceCombatTimers, creatureHostility, inventoriesAlwaysInRange, warpDriveRequirements, baseAutoPower, fishing, reputationGain);
    }

    private static bool IsPreset(Difficulty difficulty, Difficulty preset)
    {
        foreach (var property in typeof(Difficulty).GetProperties())
        {
            var value = property.GetValue(difficulty);

            // This is a case for settings that have been added in a later version.
            if (property.PropertyType.IsNullable() && value is null)
                continue;

            if (!value!.Equals(property.GetValue(preset)))
                return false;
        }
        return true;
    }

    // Newly added settings can be ignored here as it will never be reached (see comment about Omega 4.50 in calling method). 
    private static bool IsPreset(string json, Difficulty preset)
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
        return settings.All(json.Contains);
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
