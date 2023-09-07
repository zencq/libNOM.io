using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace libNOM.io.Globals;


internal static partial class Json
{
    #region Regex

#if NETSTANDARD2_0_OR_GREATER || NET6_0
    private static readonly Regex RegexSaveNameObfuscated = new("\\\"Pk4\\\":\\\"(.*?)\\\",", RegexOptions.Compiled, TimeSpan.FromMilliseconds(1000));
    private static readonly Regex RegexSaveNamePlaintext = new("\\\"SaveName\\\":\\\"(.*?)\\\",", RegexOptions.Compiled, TimeSpan.FromMilliseconds(1000));
    private static readonly Regex RegexSaveSummaryObfuscated = new("\\\"n:R\\\":\\\"(.*?)\\\",", RegexOptions.Compiled, TimeSpan.FromMilliseconds(1000));
    private static readonly Regex RegexSaveSummaryPlaintext = new("\\\"SaveSummary\\\":\\\"(.*?)\\\",", RegexOptions.Compiled, TimeSpan.FromMilliseconds(1000));
    private static readonly Regex RegexTotalPlayTimeObfuscated = new("\\\"Lg8\\\":(\\d+),", RegexOptions.Compiled, TimeSpan.FromMilliseconds(1000));
    private static readonly Regex RegexTotalPlayTimePlaintext = new("\\\"TotalPlayTime\\\":(\\d+),", RegexOptions.Compiled, TimeSpan.FromMilliseconds(1000));
    private static readonly Regex RegexVersionObfuscated = new("\\\"F2P\\\":(\\d{4,}),", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));
    private static readonly Regex RegexVersionPlaintext = new("\\\"Version\\\":(\\d{4,}),", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));
#else
    [GeneratedRegex("\\\"Pk4\\\":\\\"(.*?)\\\",", RegexOptions.Compiled, 1000)]
    private static partial Regex RegexSaveNameObfuscated();

    [GeneratedRegex("\\\"SaveName\\\":\\\"(.*?)\\\",", RegexOptions.Compiled, 1000)]
    private static partial Regex RegexSaveNamePlaintext();

    [GeneratedRegex("\\\"n:R\\\":\\\"(.*?)\\\",", RegexOptions.Compiled, 1000)]
    private static partial Regex RegexSaveSummaryObfuscated();

    [GeneratedRegex("\\\"SaveSummary\\\":\\\"(.*?)\\\",", RegexOptions.Compiled, 1000)]
    private static partial Regex RegexSaveSummaryPlaintext();

    [GeneratedRegex("\\\"Lg8\\\":(\\d+),", RegexOptions.Compiled, 1000)]
    private static partial Regex RegexTotalPlayTimeObfuscated();

    [GeneratedRegex("\\\"TotalPlayTime\\\":(\\d+),", RegexOptions.Compiled, 1000)]
    private static partial Regex RegexTotalPlayTimePlaintext();

    [GeneratedRegex("\\\"F2P\\\":(\\d{4,}),", RegexOptions.Compiled, 100)]
    private static partial Regex RegexVersionObfuscated();

    [GeneratedRegex("\\\"Version\\\":(\\d{4,}),", RegexOptions.Compiled, 100)]
    private static partial Regex RegexVersionPlaintext();
#endif

    private static bool GetRegex(Regex regex, string input, out uint result)
    {
        result = 0;
        Match match;
        try
        {
            match = regex.Match(input);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }

        if (match.Success)
        {
            result = System.Convert.ToUInt32(match.Groups[1].Value);
        }
        return match.Success;
    }

    private static bool GetRegex(Regex regex, string input, out string result)
    {
        result = string.Empty;
        Match match;
        try
        {
            match = regex.Match(input);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }

        if (match.Success)
        {
            result = match.Groups[1].Value;
        }
        return match.Success;
    }

    #endregion

    // //

    #region DifficultyPresetTypeEnum

    /// <summary>
    /// Gets the difficulty based on the data inside the save.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="jsonObject"></param>
    /// <returns></returns>
    internal static DifficultyPresetTypeEnum GetGameDifficulty(Container container, JObject? jsonObject)
    {
        if (jsonObject is null)
            return DifficultyPresetTypeEnum.Invalid;

        // Since Waypoint the difficulty is handed differently and therefore needs to be checked in more detail.
        // IsVersion may not yet be available.
        if (container.GameMode == PresetGameModeEnum.Normal && container.SaveVersion >= Constants.THRESHOLD_WAYPOINT_GAMEMODE)
        {
            // Compare the read difficulty with the different presets.
            var difficulty = GetGameDifficultyPreset(jsonObject);

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

        return container.GameMode switch
        {
            PresetGameModeEnum.Unspecified => DifficultyPresetTypeEnum.Invalid,
            PresetGameModeEnum.Creative => DifficultyPresetTypeEnum.Creative,
            PresetGameModeEnum.Survival => DifficultyPresetTypeEnum.Survival,
            PresetGameModeEnum.Permadeath => DifficultyPresetTypeEnum.Permadeath,
            _ => DifficultyPresetTypeEnum.Normal,
        };
    }

    private static DifficultyPresetData GetGameDifficultyPreset(JObject jsonObject)
    {
        // Survival Elements
        var activeSurvivalBars = jsonObject.GetValue<ActiveSurvivalBarsDifficultyEnum>("6f=.LyC.:fe.tEx.ZeS", "PlayerStateData.DifficultyState.Settings.ActiveSurvivalBars.ActiveSurvivalBarsDifficulty");

        // Survival Difficulty
        var hazardDrain = jsonObject.GetValue<HazardDrainDifficultyEnum>("6f=.LyC.:fe.bGK.ORx", "PlayerStateData.DifficultyState.Settings.HazardDrain.HazardDrainDifficulty");
        var energyDrain = jsonObject.GetValue<EnergyDrainDifficultyEnum>("6f=.LyC.:fe.A:s.Dn>", "PlayerStateData.DifficultyState.Settings.EnergyDrain.EnergyDrainDifficulty");

        // Natural Resources
        var substanceCollection = jsonObject.GetValue<SubstanceCollectionDifficultyEnum>("6f=.LyC.:fe.jH@.9JJ", "PlayerStateData.DifficultyState.Settings.SubstanceCollection.SubstanceCollectionDifficulty");

        // Sprinting
        var sprintingCost = jsonObject.GetValue<SprintingCostDifficultyEnum>("6f=.LyC.:fe.l29.LT:", "PlayerStateData.DifficultyState.Settings.SprintingCost.SprintingCostDifficulty");

        // Scanner Recharge
        var scannerRecharge = jsonObject.GetValue<ScannerRechargeDifficultyEnum>("6f=.LyC.:fe.Lf?.gFS", "PlayerStateData.DifficultyState.Settings.ScannerRecharge.ScannerRechargeDifficulty");

        // Damage Levels
        var damageReceived = jsonObject.GetValue<DamageReceivedDifficultyEnum>("6f=.LyC.:fe.hXp.cYk", "PlayerStateData.DifficultyState.Settings.DamageReceived.DamageReceivedDifficulty");

        // Technology Damage
        var breakTechOnDamage = jsonObject.GetValue<BreakTechOnDamageProbabilityEnum>("6f=.LyC.:fe.gd>.ef4", "PlayerStateData.DifficultyState.Settings.BreakTechOnDamage.BreakTechOnDamageProbability");

        // Death Consequences
        var deathConsequences = jsonObject.GetValue<DeathConsequencesDifficultyEnum>("6f=.LyC.:fe.n7p.q2@", "PlayerStateData.DifficultyState.Settings.DeathConsequences.DeathConsequencesDifficulty");

        // Fuel Usage
        var chargingRequirements = jsonObject.GetValue<ChargingRequirementsDifficultyEnum>("6f=.LyC.:fe.nhq.428", "PlayerStateData.DifficultyState.Settings.ChargingRequirements.ChargingRequirementsDifficulty");
        var fuelUse = jsonObject.GetValue<FuelUseDifficultyEnum>("6f=.LyC.:fe.jnM.Eg1", "PlayerStateData.DifficultyState.Settings.FuelUse.FuelUseDifficulty");
        var launchFuelCost = jsonObject.GetValue<LaunchFuelCostDifficultyEnum>("6f=.LyC.:fe.A9D.iqY", "PlayerStateData.DifficultyState.Settings.LaunchFuelCost.LaunchFuelCostDifficulty");

        // Crafting
        var craftingIsFree = jsonObject.GetValue<bool>("6f=.LyC.:fe.?Dt", "PlayerStateData.DifficultyState.Settings.CraftingIsFree");

        // Purchases
        var currencyCost = jsonObject.GetValue<CurrencyCostDifficultyEnum>("6f=.LyC.:fe.tsk.Ubk", "PlayerStateData.DifficultyState.Settings.CurrencyCost.CurrencyCostDifficulty");

        // Goods Availability
        var itemShopAvailability = jsonObject.GetValue<ItemShopAvailabilityDifficultyEnum>("6f=.LyC.:fe.FB5.TYf", "PlayerStateData.DifficultyState.Settings.ItemShopAvailability.ItemShopAvailabilityDifficulty");

        // Inventory Stack Limits
        var inventoryStackLimits = jsonObject.GetValue<InventoryStackLimitsDifficultyEnum>("6f=.LyC.:fe.kZ5.?SS", "PlayerStateData.DifficultyState.Settings.InventoryStackLimits.InventoryStackLimitsDifficulty");

        // Enemy Strength
        var damageGiven = jsonObject.GetValue<DamageGivenDifficultyEnum>("6f=.LyC.:fe.PYQ.mum", "PlayerStateData.DifficultyState.Settings.DamageGiven.DamageGivenDifficulty");

        // On-Foot Combat
        var groundCombatTimers = jsonObject.GetValue<CombatTimerDifficultyOptionEnum>("6f=.LyC.:fe.jGh.ZbV", "PlayerStateData.DifficultyState.Settings.GroundCombatTimers.CombatTimerDifficultyOption");

        // Space Combat
        var spaceCombatTimers = jsonObject.GetValue<CombatTimerDifficultyOptionEnum>("6f=.LyC.:fe.Od7.ZbV", "PlayerStateData.DifficultyState.Settings.SpaceCombatTimers.CombatTimerDifficultyOption");

        // Creatures
        var creatureHostility = jsonObject.GetValue<CreatureHostilityDifficultyEnum>("6f=.LyC.:fe.BbG.1c;", "PlayerStateData.DifficultyState.Settings.CreatureHostility.CreatureHostilityDifficulty");

        // Inventory Transfer Range
        var inventoriesAlwaysInRange = jsonObject.GetValue<bool>("6f=.LyC.:fe.pS0", "PlayerStateData.DifficultyState.Settings.InventoriesAlwaysInRange");

        // Hyperdrive System Access
        var warpDriveRequirements = jsonObject.GetValue<bool>("6f=.LyC.:fe.aw9", "PlayerStateData.DifficultyState.Settings.WarpDriveRequirements");

        // Base Power
        var baseAutoPower = jsonObject.GetValue<bool>("6f=.LyC.:fe.uo4", "PlayerStateData.DifficultyState.Settings.BaseAutoPower");

        // Reputation & Standing Gain
        var reputationGain = jsonObject.GetValue<ReputationGainDifficultyEnum>("6f=.LyC.:fe.vo>.S@3", "PlayerStateData.DifficultyState.Settings.ReputationGain.ReputationGainDifficulty");

        return new(activeSurvivalBars, hazardDrain, energyDrain, substanceCollection, sprintingCost, scannerRecharge, damageReceived, breakTechOnDamage, deathConsequences, chargingRequirements, fuelUse, launchFuelCost, craftingIsFree, currencyCost, itemShopAvailability, inventoryStackLimits, damageGiven, groundCombatTimers, spaceCombatTimers, creatureHostility, inventoriesAlwaysInRange, warpDriveRequirements, baseAutoPower, reputationGain);
    }

    /// <summary>
    /// Gets the difficulty based on the data inside the save.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="json"></param>
    /// <returns></returns>
    internal static DifficultyPresetTypeEnum GetGameDifficulty(Container container, string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return DifficultyPresetTypeEnum.Invalid;

        // Since Waypoint the difficulty is handed differently and therefore needs to be checked in more detail.
        // IsVersion may not yet be available.
        if (container.GameMode == PresetGameModeEnum.Normal && container.SaveVersion >= Constants.THRESHOLD_WAYPOINT_GAMEMODE)
        {
            // DifficultyState is stored again in SeasonData and therefore cut off here at an appropriate length.
            // DifficultyState ends at around 800 characters if save name and summary are not set. Both are limited to 128 chars so 1500 should be more than enough.
            var jsonSubstring = json.AsSpan(0, 1500);

            if (IsGameDifficultyPreset(jsonSubstring, Constants.DIFFICULTY_PRESET_NORMAL))
                return DifficultyPresetTypeEnum.Normal;

            if (IsGameDifficultyPreset(jsonSubstring, Constants.DIFFICULTY_PRESET_CREATIVE))
                return DifficultyPresetTypeEnum.Creative;

            if (IsGameDifficultyPreset(jsonSubstring, Constants.DIFFICULTY_PRESET_SURVIVAL))
                return DifficultyPresetTypeEnum.Survival;

            if (IsGameDifficultyPreset(jsonSubstring, Constants.DIFFICULTY_PRESET_RELAXED))
                return DifficultyPresetTypeEnum.Relaxed;

            return DifficultyPresetTypeEnum.Custom;
        }

        return container.GameMode switch
        {
            PresetGameModeEnum.Unspecified => DifficultyPresetTypeEnum.Invalid,
            PresetGameModeEnum.Creative => DifficultyPresetTypeEnum.Creative,
            PresetGameModeEnum.Survival => DifficultyPresetTypeEnum.Survival,
            PresetGameModeEnum.Permadeath => DifficultyPresetTypeEnum.Permadeath,
            _ => DifficultyPresetTypeEnum.Normal,
        };
    }

    private static bool IsGameDifficultyPreset(ReadOnlySpan<char> json, DifficultyPresetData difficultyPreset)
    {
        // Survival Elements
        var activeSurvivalBars = $"\"tEx\":{{\"ZeS\":\"{difficultyPreset.ActiveSurvivalBars}\"".AsSpan();
        if (!json.Contains(activeSurvivalBars, StringComparison.OrdinalIgnoreCase))
            return false;

        // Survival Difficulty
        var hazardDrain = $"\"bGK\":{{\"ORx\":\"{difficultyPreset.HazardDrain}\"".AsSpan();
        if (!json.Contains(hazardDrain, StringComparison.OrdinalIgnoreCase))
            return false;
        var energyDrain = $"\"A:s\":{{\"Dn>\":\"{difficultyPreset.EnergyDrain}\"".AsSpan();
        if (!json.Contains(energyDrain, StringComparison.OrdinalIgnoreCase))
            return false;

        // Natural Resources
        var substanceCollection = $"\"jH@\":{{\"9JJ\":\"{difficultyPreset.SubstanceCollection}\"".AsSpan();
        if (!json.Contains(substanceCollection, StringComparison.OrdinalIgnoreCase))
            return false;

        // Sprinting
        var sprintingCost = $"\"l29\":{{\"LT:\":\"{difficultyPreset.SprintingCost}\"".AsSpan();
        if (!json.Contains(sprintingCost, StringComparison.OrdinalIgnoreCase))
            return false;

        // Scanner Recharge
        var scannerRecharge = $"\"Lf?\":{{\"gFS\":\"{difficultyPreset.ScannerRecharge}\"".AsSpan();
        if (!json.Contains(scannerRecharge, StringComparison.OrdinalIgnoreCase))
            return false;

        // Damage Levels
        var damageReceived = $"\"hXp\":{{\"cYk\":\"{difficultyPreset.DamageReceived}\"".AsSpan();
        if (!json.Contains(damageReceived, StringComparison.OrdinalIgnoreCase))
            return false;

        // Technology Damage
        var breakTechOnDamage = $"\"gd>\":{{\"ef4\":\"{difficultyPreset.BreakTechOnDamage}\"".AsSpan();
        if (!json.Contains(breakTechOnDamage, StringComparison.OrdinalIgnoreCase))
            return false;

        // Death Consequences
        var deathConsequences = $"\"n7p\":{{\"q2@\":\"{difficultyPreset.DeathConsequences}\"".AsSpan();
        if (!json.Contains(deathConsequences, StringComparison.OrdinalIgnoreCase))
            return false;

        // Fuel Usage
        var chargingRequirements = $"\"nhq\":{{\"428\":\"{difficultyPreset.ChargingRequirements}\"".AsSpan();
        if (!json.Contains(chargingRequirements, StringComparison.OrdinalIgnoreCase))
            return false;
        var fuelUse = $"\"jnM\":{{\"Eg1\":\"{difficultyPreset.FuelUse}\"".AsSpan();
        if (!json.Contains(fuelUse, StringComparison.OrdinalIgnoreCase))
            return false;
        var launchFuelCost = $"\"A9D\":{{\"iqY\":\"{difficultyPreset.LaunchFuelCost}\"".AsSpan();
        if (!json.Contains(launchFuelCost, StringComparison.OrdinalIgnoreCase))
            return false;

        // Crafting
        var craftingIsFree = $"\"?Dt\":{difficultyPreset.CraftingIsFree},".AsSpan();
        if (!json.Contains(craftingIsFree, StringComparison.OrdinalIgnoreCase))
            return false;

        // Purchases
        var currencyCost = $"\"tsk\":{{\"Ubk\":\"{difficultyPreset.CurrencyCost}\"".AsSpan();
        if (!json.Contains(currencyCost, StringComparison.OrdinalIgnoreCase))
            return false;

        // Goods Availability
        var itemShopAvailability = $"\"FB5\":{{\"TYf\":\"{difficultyPreset.ItemShopAvailability}\"".AsSpan();
        if (!json.Contains(itemShopAvailability, StringComparison.OrdinalIgnoreCase))
            return false;

        // Inventory Stack Limits
        var inventoryStackLimits = $"\"kZ5\":{{\"?SS\":\"{difficultyPreset.InventoryStackLimits}\"".AsSpan();
        if (!json.Contains(inventoryStackLimits, StringComparison.OrdinalIgnoreCase))
            return false;

        // Enemy Strength
        var damageGiven = $"\"PYQ\":{{\"mum\":\"{difficultyPreset.DamageGiven}\"".AsSpan();
        if (!json.Contains(damageGiven, StringComparison.OrdinalIgnoreCase))
            return false;

        // On-Foot Combat
        var groundCombatTimers = $"\"jGh\":{{\"ZbV\":\"{difficultyPreset.GroundCombatTimers}\"".AsSpan();
        if (!json.Contains(groundCombatTimers, StringComparison.OrdinalIgnoreCase))
            return false;

        // Space Combat
        var spaceCombatTimers = $"\"Od7\":{{\"ZbV\":\"{difficultyPreset.SpaceCombatTimers}\"".AsSpan();
        if (!json.Contains(spaceCombatTimers, StringComparison.OrdinalIgnoreCase))
            return false;

        // Creatures
        var creatureHostility = $"\"BbG\":{{\"1c;\":\"{difficultyPreset.CreatureHostility}\"".AsSpan();
        if (!json.Contains(creatureHostility, StringComparison.OrdinalIgnoreCase))
            return false;

        // Inventory Transfer Range
        var inventoriesAlwaysInRange = $"\"pS0\":{difficultyPreset.InventoriesAlwaysInRange},".AsSpan();
        if (!json.Contains(inventoriesAlwaysInRange, StringComparison.OrdinalIgnoreCase))
            return false;

        // Hyperdrive System Access
        var warpDriveRequirements = $"\"aw9\":{difficultyPreset.WarpDriveRequirements},".AsSpan();
        if (!json.Contains(warpDriveRequirements, StringComparison.OrdinalIgnoreCase))
            return false;

        // Base Power
        var baseAutoPower = $"\"uo4\":{difficultyPreset.BaseAutoPower},".AsSpan();
        if (!json.Contains(baseAutoPower, StringComparison.OrdinalIgnoreCase))
            return false;

        // Reputation & Standing Gain
        var reputationGain = $"\"vo>\":{{\"S@3\":\"{difficultyPreset.ReputationGain}\"".AsSpan();
        return json.Contains(reputationGain, StringComparison.OrdinalIgnoreCase); // if true all previous values where true as well and therefore it matches the preset
    }

    #endregion

    #region Save Name

    /// <summary>
    /// Gets the in-file name of the save.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <returns></returns>
    internal static string GetSaveName(JObject? jsonObject)
    {
        return jsonObject?.GetValue<string>("6f=.Pk4", "PlayerStateData.SaveName") ?? string.Empty;
    }

    /// <summary>
    /// Gets the in-file name of the save.
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    internal static string GetSaveName(string? json)
    {
        if (json is not null)
        {
#if NETSTANDARD2_0_OR_GREATER || NET6_0
            if (GetRegex(RegexSaveNameObfuscated, json, out string resultObfuscated))
                return resultObfuscated;

            if (GetRegex(RegexSaveNamePlaintext, json, out string resultPlaintext))
                return resultPlaintext;
#else
            if (GetRegex(RegexSaveNameObfuscated(), json, out string resultObfuscated))
                return resultObfuscated;

            if (GetRegex(RegexSaveNamePlaintext(), json, out string resultPlaintext))
                return resultPlaintext;
#endif
        }
        return string.Empty;
    }

    #endregion

    #region Save Summary

    /// <summary>
    /// Gets the in-file summary of the save.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <returns></returns>
    internal static string GetSaveSummary(JObject? jsonObject)
    {
        return jsonObject?.GetValue<string>("6f=.n:R", "PlayerStateData.SaveSummary") ?? string.Empty;
    }

    /// <summary>
    /// Gets the in-file summary of the save.
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    internal static string GetSaveSummary(string? json)
    {
        if (json is not null)
        {
#if NETSTANDARD2_0_OR_GREATER || NET6_0
            if (GetRegex(RegexSaveSummaryObfuscated, json, out string resultObfuscated))
                return resultObfuscated;

            if (GetRegex(RegexSaveSummaryPlaintext, json, out string resultPlaintext))
                return resultPlaintext;
#else
            if (GetRegex(RegexSaveSummaryObfuscated(), json, out string resultObfuscated))
                return resultObfuscated;

            if (GetRegex(RegexSaveSummaryPlaintext(), json, out string resultPlaintext))
                return resultPlaintext;
#endif
        }
        return string.Empty;
    }

    #endregion

    #region Total Play Time

    /// <summary>
    /// Gets the in-file total play time of the save.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <returns></returns>
    internal static uint GetTotalPlayTime(JObject? jsonObject)
    {
        return jsonObject?.GetValue<uint?>("6f=.Lg8", "PlayerStateData.TotalPlayTime") ?? 0;
    }

    /// <summary>
    /// Gets the in-file total play time of the save.
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public static uint GetTotalPlayTime(string? json)
    {
        if (json is not null)
        {
#if NETSTANDARD2_0_OR_GREATER || NET6_0
            if (GetRegex(RegexTotalPlayTimeObfuscated, json, out uint resultObfuscated))
                return resultObfuscated;

            if (GetRegex(RegexTotalPlayTimePlaintext, json, out uint resultPlaintext))
                return resultPlaintext;
#else
            if (GetRegex(RegexTotalPlayTimeObfuscated(), json, out uint resultObfuscated))
                return resultObfuscated;

            if (GetRegex(RegexTotalPlayTimePlaintext(), json, out uint resultPlaintext))
                return resultPlaintext;
#endif
        }
        return 0;
    }

    #endregion

    #region Version

    /// <summary>
    /// Gets the in-file version of the save.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <returns></returns>
    internal static int GetVersion(JObject? jsonObject)
    {
        return jsonObject?.GetValue<int?>("F2P", "Version") ?? 0;
    }

    /// <summary>
    /// Gets the in-file version of the save.
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    internal static int GetVersion(string? json)
    {
        if (json is not null)
        {
#if NETSTANDARD2_0_OR_GREATER || NET6_0
            if (GetRegex(RegexVersionObfuscated, json, out uint resultObfuscated))
                return (int)(resultObfuscated);

            if (GetRegex(RegexVersionPlaintext, json, out uint resultPlaintext))
                return (int)(resultPlaintext);
#else
            if (GetRegex(RegexVersionObfuscated(), json, out uint resultObfuscated))
                return (int)(resultObfuscated);

            if (GetRegex(RegexVersionPlaintext(), json, out uint resultPlaintext))
                return (int)(resultPlaintext);
#endif
        }
        return 0;
    }

    #endregion

    #region GameVersionEnum

    /// <summary>
    /// Gets the game version for the specified container.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="jsonObject"></param>
    /// <returns></returns>
    internal static GameVersionEnum GetGameVersionEnum(Container container, JObject jsonObject)
    {
        /** SaveVersion and new Keys to determine the GameVersion.

        GameVersion = CreativeVersion/BaseVersion (Obfuscated = Deobfuscated)
        ??? = ????/???? (??? = ?)

        Echoes
        441 = 4658/4146
        440 = 4658/4146

        Singularity
        438 = 4657/4145
        437 = 4657/4145
        436 = 4657/4145
        434 = 4657/4145
        433 = 4657/4145
        430 = 4657/4145 (:?x = GreyIfCantStart) // only in PresetGameModeEnum.Seasonal saves

        Mac
        426 = 4657/4145
        425 = 4657/4145 (XEk = SeasonStartMusicOverride)

        Interceptor
        423 = 4657/4145
        422 = 4657/4145
        421 = 4656/4144
        420 = 4656/4144

        Fractal
        415 = 4655/4143
        414 = 4655/4143
        413 = 4655/4143
        412 = 4655/4143
        410 = 4655/4143

        WaypointWithSuperchargedSlots
        408 = 4654/4142
        407 = 4654/4142
        406 = 4654/4142
        405 = 4654/4142

        WaypointWithAgileStat
        404 = 4653/4141

        Waypoint
        403 = 4652/4140
        400 = 4652/4140

        Endurance
        399 = 5163/4139
        398 = 5163/4139
        397 = 5163/4139
        396 = 5163/4139
        395 = 5163/4139
        394 = 5163/4139

        Leviathan
        393 = 5162/4138
        392 = 5162/4138
        391 = 5162/4138
        390 = 5162/4138 (Sd6 = NextLoadSpawnsWithFreshStart)

        Outlaws
        389 = 5162/4138
        388 = 5162/4138
        387 = 5162/4138
        385 = 5162/4138

        SentinelWithVehicleAI
        384 = 5161/4137 (Agx = VehicleAIControlEnabled)

        SentinelWithWeaponResource
        382 = 5161/4137
        381 = 5161/4137

        Sentinel
        380 = 5160/4136

        Emergence
        375 = 5159/4135
        374 = 5159/4135
        373 = 5159/4135
        371 = 5159/4135
        370 = 5159/4135 (qs? = SandwormOverrides)

        Frontiers
        368 = 5159/4135
        367 = 5159/4135
        366 = 5159/4135
        365 = 5159/4135
        364 = 5159/4135
        363 = 5159/4135
        362 = 5159/4135
        361 = 5159/4135
        360 = 5159/4135

        PrismsWithBytebeatAuthor
        353 = 5158/4134
        352 = 5158/4134
        351 = 5157/4133 (m7b = AuthorOnlineID)

        Prisms
        350 = 5157/4133 (8iI = ByteBeatLibrary)

        Beachhead
        342 = 5157/4133
        341 = 5157/4133
        340 = 5157/4133 (Whh = MainMissionTitle)

        Expeditions
        338 = 5157/4133
        337 = 5157/4133
        335 = 5154/4130
        334 = 5153/4129
        333 = 5153/4129
        332 = 5153/4129
        330 = 5153/4129

        Companions
        322 = 5151/4127
        321 = 5151/4127
        320 = 5151/4127 (Mcl = Pets)

        NextGeneration
        315 = 5151/4127
        313 = 5151/4127
        310 = 5151/4127

        Origins
        305 = 5150/4126
        303 = 5150/4126
        302 = 5150/4126
        301 = 5150/4126
        300 = 5150/4126 (ux@ = PreviousUniverseAddress)

        Desolation
        262 = 5150/4126
        261 = 5150/4126
        260 = 5150/4126 (Ovv = AbandonedFreighterPositionInSystem)

        Crossplay
        255 = 5150/4126
        254 = 5150/4126
        253 = 5150/4126
        252 = 5150/4126
        251 = 5150/4126

        ExoMech
        241 = 5149/4125
        240 = 5149/4125

        LivingShip
        233 = 5148/4124
        232 = 5148/4124
        231 = 5148/4124
        230 = 5148/4124 (Xf4 = CurrentPos)

        SynthesisWithJetpack
        227 = 5148/4124
        226 = 5148/4124

        Synthesis
        224 = 5147/4123
        223 = 5146/4122
        222 = 5146/4122
        220 = 5146/4122

        BeyondWithVehicleCam
        216 = 5143/4119
        215 = 5143/4119
        214 = 5143/4119
        213 = 5143/4119
        212 = 5143/4119
        211 = 5143/4119 (wb: = UsesThirdPersonVehicleCam)

        Beyond
        209 = 5141/4117
        */

        var usesMapping = jsonObject.UsesMapping();

        if (container.BaseVersion >= 4146) // 4.40
        {
            return GameVersionEnum.Echoes;
        }

        if (container.BaseVersion >= 4144) // 4.20, 4.25, 4.30
        {
            // Only used in actual Expedition saves.
            //var greyIfCantStart = jsonObject.SelectTokens(usesMapping ? "PlayerStateData.SeasonData.Stages[*].Milestones[*].GreyIfCantStart" : "6f=.Rol.3Mw[*].kr6[*].:?x");
            //if (greyIfCantStart.Any())
            //    return VersionEnum.Singularity;

            // This is actually VersionEnum.Mac but it made most of the preperation for Singularity Expedition and therefore we already use this here.
            var seasonStartMusicOverride = jsonObject.SelectToken(usesMapping ? "PlayerStateData.SeasonData.SeasonStartMusicOverride" : "6f=.Rol.XEk");
            if (seasonStartMusicOverride is not null)
                return GameVersionEnum.Singularity;

            return GameVersionEnum.Interceptor;
        }

        if (container.BaseVersion >= 4143) // 4.10
            return GameVersionEnum.Fractal;

        if (container.BaseVersion >= 4142) // 4.05
            return GameVersionEnum.WaypointWithSuperchargedSlots;

        if (container.BaseVersion >= 4141) // 4.04
            return GameVersionEnum.WaypointWithAgileStat;

        if (container.BaseVersion >= 4140) // 4.00
            return GameVersionEnum.Waypoint;

        if (container.BaseVersion >= 4139) // 3.94
            return GameVersionEnum.Endurance;

        if (container.BaseVersion >= 4138) // 3.85, 3.90
        {
            var nextLoadSpawnsWithFreshStart = jsonObject.SelectToken(usesMapping ? "PlayerStateData.NextLoadSpawnsWithFreshStart" : "6f=.Sd6");
            if (nextLoadSpawnsWithFreshStart is not null)
                return GameVersionEnum.Leviathan;

            return GameVersionEnum.Outlaws;
        }

        if (container.BaseVersion >= 4137) // 3.81, 3.84
        {
            var vehicleAIControlEnabled = jsonObject.SelectToken(usesMapping ? "PlayerStateData.VehicleAIControlEnabled" : "6f=.Agx");
            if (vehicleAIControlEnabled is not null)
                return GameVersionEnum.SentinelWithVehicleAI;

            return GameVersionEnum.SentinelWithWeaponResource;
        }

        if (container.BaseVersion >= 4136) // 3.80
            return GameVersionEnum.Sentinel;

        if (container.BaseVersion >= 4135) // 3.60, 3.70
        {
            var sandwormOverrides = jsonObject.SelectTokens(usesMapping ? "PlayerStateData.SeasonData.SandwormOverrides" : "6f=.Rol.qs?");
            if (sandwormOverrides.Any())
                return GameVersionEnum.Emergence;

            return GameVersionEnum.Frontiers;
        }

        if (container.BaseVersion >= 4129) // 3.30, 3.40, 3.50, 3.51
        {
            var authorOnlineID = jsonObject.SelectTokens(usesMapping ? "PlayerStateData.ByteBeatLibrary.MySongs..AuthorOnlineID" : "6f=.8iI.ON4..m7b");
            if (authorOnlineID.Any())
                return GameVersionEnum.PrismsWithBytebeatAuthor;

            var byteBeatLibrary = jsonObject.SelectToken(usesMapping ? "PlayerStateData.ByteBeatLibrary" : "6f=.8iI");
            if (byteBeatLibrary is not null)
                return GameVersionEnum.Prisms;

            var mainMissionTitle = jsonObject.SelectToken(usesMapping ? "PlayerStateData.SeasonData.MainMissionTitle" : "6f=.Rol.Whh");
            if (mainMissionTitle is not null)
                return GameVersionEnum.Beachhead;

            return GameVersionEnum.Expeditions;
        }

        if (container.BaseVersion >= 4127) // 3.10, 3.20
        {
            var pets = jsonObject.SelectToken(usesMapping ? "PlayerStateData.Pets" : "6f=.Mcl");
            if (pets is not null)
                return GameVersionEnum.Companions;

            return GameVersionEnum.NextGeneration;
        }

        if (container.BaseVersion >= 4126) // 2.50, 2.60, 3.00
        {
            var previousUniverseAddress = jsonObject.SelectToken(usesMapping ? "PlayerStateData.PreviousUniverseAddress" : "6f=.ux@");
            if (previousUniverseAddress is not null)
                return GameVersionEnum.Origins;

            var abandonedFreighterPositionInSystem = jsonObject.SelectToken(usesMapping ? "SpawnStateData.AbandonedFreighterPositionInSystem" : "rnc.Ovv");
            if (abandonedFreighterPositionInSystem is not null)
                return GameVersionEnum.Desolation;

            return GameVersionEnum.Crossplay;
        }

        if (container.BaseVersion >= 4125) // 2.40
            return GameVersionEnum.ExoMech;

        if (container.BaseVersion >= 4124) // 2.26, 2.30
        {
            var currentPos = jsonObject.SelectTokens(usesMapping ? "PlayerStateData..CurrentPos" : "6f=..Xf4");
            if (currentPos.Any())
                return GameVersionEnum.LivingShip;

            return GameVersionEnum.SynthesisWithJetpack;
        }

        if (container.BaseVersion >= 4122) // 2.20
            return GameVersionEnum.Synthesis;

        if (container.BaseVersion >= 4119) // 2.11
            return GameVersionEnum.BeyondWithVehicleCam;

        return GameVersionEnum.Unknown;
    }

    /// <inheritdoc cref="GetGameVersionEnum(Container, JObject)"/>
    internal static GameVersionEnum GetGameVersionEnum(Container container, string json)
    {
        if (container.BaseVersion >= 4146) // 4.40
        {
            return GameVersionEnum.Echoes;
        }

        if (container.BaseVersion >= 4144) // 4.20, 4.25, 4.30
        {
            // Only used in actual Expedition saves.
            //var greyIfCantStart = json.Contains("\":?x\":") || json.Contains("\"GreyIfCantStart\":");
            //if (greyIfCantStart)
            //    return VersionEnum.Singularity;

            // This is actually VersionEnum.Mac but it made most of the preperation for Singularity Expedition and therefore we already use this here.
            var seasonStartMusicOverride = json.Contains("\"XEk\":") || json.Contains("\"SeasonStartMusicOverride\":");
            if (seasonStartMusicOverride)
                return GameVersionEnum.Singularity;

            return GameVersionEnum.Interceptor;
        }

        if (container.BaseVersion >= 4143) // 4.10
            return GameVersionEnum.Fractal;

        if (container.BaseVersion >= 4142) // 4.05
            return GameVersionEnum.WaypointWithSuperchargedSlots;

        if (container.BaseVersion >= 4141) // 4.04
            return GameVersionEnum.WaypointWithAgileStat;

        if (container.BaseVersion >= 4140) // 4.00
            return GameVersionEnum.Waypoint;

        if (container.BaseVersion >= 4139) // 3.94
            return GameVersionEnum.Endurance;

        if (container.BaseVersion >= 4138) // 3.85, 3.90
        {
            var nextLoadSpawnsWithFreshStart = json.Contains("\"Sd6\":") || json.Contains("\"NextLoadSpawnsWithFreshStart\":");
            if (nextLoadSpawnsWithFreshStart)
                return GameVersionEnum.Leviathan;

            return GameVersionEnum.Outlaws;
        }

        if (container.BaseVersion >= 4137) // 3.81, 3.84
        {
            var vehicleAIControlEnabled = json.Contains("\"Agx\":") || json.Contains("\"VehicleAIControlEnabled\":");
            if (vehicleAIControlEnabled)
                return GameVersionEnum.SentinelWithVehicleAI;

            return GameVersionEnum.SentinelWithWeaponResource;
        }

        if (container.BaseVersion >= 4136) // 3.80
            return GameVersionEnum.Sentinel;

        if (container.BaseVersion >= 4135) // 3.60, 3.70
        {
            var sandwormOverrides = json.Contains("\"qs?\":") || json.Contains("\"SandwormOverrides\":");
            if (sandwormOverrides)
                return GameVersionEnum.Emergence;

            return GameVersionEnum.Frontiers;
        }

        if (container.BaseVersion >= 4129) // 3.30, 3.40, 3.50, 3.51
        {
            var authorOnlineID = json.Contains("\"m7b\":") || json.Contains("\"AuthorOnlineID\":");
            if (authorOnlineID)
                return GameVersionEnum.PrismsWithBytebeatAuthor;

            var byteBeatLibrary = json.Contains("\"8iI\":") || json.Contains("\"ByteBeatLibrary\":");
            if (byteBeatLibrary)
                return GameVersionEnum.Prisms;

            var mainMissionTitle = json.Contains("\"Whh\":") || json.Contains("\"MainMissionTitle\":");
            if (mainMissionTitle)
                return GameVersionEnum.Beachhead;

            return GameVersionEnum.Expeditions;
        }

        if (container.BaseVersion >= 4127) // 3.10, 3.20
        {
            var pets = json.Contains("\"Mcl\":") || json.Contains("\"Pets\":");
            if (pets)
                return GameVersionEnum.Companions;

            return GameVersionEnum.NextGeneration;
        }

        if (container.BaseVersion >= 4126) // 2.50, 2.60, 3.00
        {
            var previousUniverseAddress = json.Contains("\"ux@\":") || json.Contains("\"PreviousUniverseAddress\":");
            if (previousUniverseAddress)
                return GameVersionEnum.Origins;

            var abandonedFreighterPositionInSystem = json.Contains("\"Ovv\":") || json.Contains("\"AbandonedFreighterPositionInSystem\":");
            if (abandonedFreighterPositionInSystem)
                return GameVersionEnum.Desolation;

            return GameVersionEnum.Crossplay;
        }

        if (container.BaseVersion >= 4125) // 2.40
            return GameVersionEnum.ExoMech;

        if (container.BaseVersion >= 4124) // 2.26, 2.30
        {
            var currentPos = json.Contains("\"Xf4\":") || json.Contains("\"CurrentPos\":");
            if (currentPos)
                return GameVersionEnum.LivingShip;

            return GameVersionEnum.SynthesisWithJetpack;
        }

        if (container.BaseVersion >= 4122) // 2.20
            return GameVersionEnum.Synthesis;

        if (container.BaseVersion >= 4119) // 2.11
            return GameVersionEnum.BeyondWithVehicleCam;

        return GameVersionEnum.Unknown;
    }

    #endregion
}
