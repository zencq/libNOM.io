using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace libNOM.io.Globals;


public static partial class Json
{
    #region Regex

#if NETSTANDARD2_0_OR_GREATER || NET6_0
    private static readonly Regex RegexTotalPlayTimeObfuscated = new("\\\"Lg8\\\":(\\d+),", RegexOptions.Compiled, TimeSpan.FromMilliseconds(1000));
    private static readonly Regex RegexTotalPlayTimePlaintext = new("\\\"TotalPlayTime\\\":(\\d+),", RegexOptions.Compiled, TimeSpan.FromMilliseconds(1000));
    private static readonly Regex RegexVersionObfuscated = new("\\\"F2P\\\":(\\d{4,}),", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));
    private static readonly Regex RegexVersionPlaintext = new("\\\"Version\\\":(\\d{4,}),", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));
#else
    [GeneratedRegex("\\\"Lg8\\\":(\\d+),", RegexOptions.Compiled, 1000)]
    private static partial Regex RegexTotalPlayTimeObfuscated();

    [GeneratedRegex("\\\"TotalPlayTime\\\":(\\d+),", RegexOptions.Compiled, 1000)]
    private static partial Regex RegexTotalPlayTimePlaintext();

    [GeneratedRegex("\\\"F2P\\\":(\\d{4,}),", RegexOptions.Compiled, 100)]
    private static partial Regex RegexVersionObfuscated();

    [GeneratedRegex("\\\"Version\\\":(\\d{4,}),", RegexOptions.Compiled, 100)]
    private static partial Regex RegexVersionPlaintext();
#endif

    private static bool GetRegex(Regex regex, string input, out long result)
    {
        result = -1;
        Match match;
        try
        {
            match = regex.Match(input);
        }
        catch (Exception ex) when (ex is RegexMatchTimeoutException)
        {
            return false;
        }

        if (match.Success)
        {
            result = System.Convert.ToInt64(match.Groups[1].Value);
        }
        return match.Success;
    }

    #endregion

    // //

    #region GameModeEnum

    internal static PresetGameModeEnum? GetGameModeEnum(Container container)
    {
        if (container.Version.IsGameMode(PresetGameModeEnum.Seasonal))
            return PresetGameModeEnum.Seasonal;

        if (container.Version.IsGameMode(PresetGameModeEnum.Permadeath))
            return PresetGameModeEnum.Permadeath;

        if (container.Version.IsGameMode(PresetGameModeEnum.Ambient))
            return PresetGameModeEnum.Ambient;

        if (container.Version.IsGameMode(PresetGameModeEnum.Survival))
            return PresetGameModeEnum.Survival;

        if (container.Version.IsGameMode(PresetGameModeEnum.Creative))
            return PresetGameModeEnum.Creative;

        if (container.Version.IsGameMode(PresetGameModeEnum.Normal))
            return PresetGameModeEnum.Normal;

        return null;
    }

    /// <summary>
    /// Gets the game mode of a save based on the in-file version.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="json"></param>
    /// <returns></returns>
    internal static PresetGameModeEnum? GetGameModeEnum(Container container, string json)
    {
        var result = GetGameModeEnum(container);

        // Since Waypoint the difficulty is handed differently and therefore needs to be checked in more detail.
        if (result == PresetGameModeEnum.Normal && container.Version >= Globals.Constant.THRESHOLD_WAYPOINT_GAMEMODE)
        {
            // DifficultyState is stored again in SeasonData and therefore cut off here at an appropriate length.
            // Without save name and summary DifficultyState ends at around 800 characters.
            var jsonSubstring = json.AsSpan(0, 2000);

            if (IsGameModePreset(jsonSubstring, "All", "Normal", "Normal", "Normal", "Full", "Normal", "Normal", "Low", "ItemGrave", "Normal", "Normal", "Normal", "false", "Normal", "Normal", "High", "Normal", "Normal", "Normal", "FullEcosystem", "false", "true", "false", "Normal"))
                return PresetGameModeEnum.Normal;

            if (IsGameModePreset(jsonSubstring, "None", "Slow", "Slow", "Normal", "Free", "Fast", "None", "None", "None", "None", "Free", "Free", "true", "Free", "High", "High", "Normal", "Off", "Off", "NeverAttack", "true", "false", "true", "Fast"))
                return PresetGameModeEnum.Creative;

            if (IsGameModePreset(jsonSubstring, "All", "Fast", "Fast", "Low", "Full", "Normal", "High", "High", "DestroyItems", "High", "Expensive", "High", "false", "Normal", "Low", "Normal", "Normal", "Fast", "Fast", "FullEcosystem", "false", "true", "false", "Normal"))
                return PresetGameModeEnum.Survival;

            if (IsGameModePreset(jsonSubstring, "HealthAndHazard", "Slow", "Slow", "High", "Low", "VeryFast", "Low", "None", "None", "Low", "Cheap", "Low", "false", "Cheap", "High", "High", "High", "Slow", "Slow", "AttackIfProvoked", "true", "true", "true", "Fast"))
                return PresetGameModeEnum.Ambient;

            return PresetGameModeEnum.Unspecified;
        }

        return result;
    }

    /// <summary>
    /// Gets the game mode of a save based on the in-file version.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="json"></param>
    /// <returns></returns>
    internal static PresetGameModeEnum? GetGameModeEnum(Container container, JObject jsonObject)
    {
        var result = GetGameModeEnum(container);

        // Since Waypoint the difficulty is handed differently and therefore needs to be checked in more detail.
        if (result == PresetGameModeEnum.Normal && container.Version >= Globals.Constant.THRESHOLD_WAYPOINT_GAMEMODE)
        {
            // Survival Elements
            var activeSurvivalBars = jsonObject.GetValue<string>("6f=.LyC.:fe.tEx.ZeS", "PlayerStateData.DifficultyState.Settings.ActiveSurvivalBars.ActiveSurvivalBarsDifficulty");

            // Survival Difficulty
            var hazardDrain = jsonObject.GetValue<string>("6f=.LyC.:fe.bGK.ORx", "PlayerStateData.DifficultyState.Settings.HazardDrain.HazardDrainDifficulty");
            var energyDrain = jsonObject.GetValue<string>("6f=.LyC.:fe.A:s.Dn>", "PlayerStateData.DifficultyState.Settings.EnergyDrain.EnergyDrainDifficulty");

            // Natural Resources
            var substanceCollection = jsonObject.GetValue<string>("6f=.LyC.:fe.jH@.9JJ", "PlayerStateData.DifficultyState.Settings.SubstanceCollection.SubstanceCollectionDifficulty");

            // Sprinting
            var sprintingCost = jsonObject.GetValue<string>("6f=.LyC.:fe.l29.LT:", "PlayerStateData.DifficultyState.Settings.SprintingCost.SprintingCostDifficulty");

            // Scanner Recharge
            var scannerRecharge = jsonObject.GetValue<string>("6f=.LyC.:fe.Lf?.gFS", "PlayerStateData.DifficultyState.Settings.ScannerRecharge.ScannerRechargeDifficulty");

            // Damage Levels
            var damageReceived = jsonObject.GetValue<string>("6f=.LyC.:fe.hXp.cYk", "PlayerStateData.DifficultyState.Settings.DamageReceived.DamageReceivedDifficulty");

            // Technology Damage
            var breakTechOnDamage = jsonObject.GetValue<string>("6f=.LyC.:fe.gd>.ef4", "PlayerStateData.DifficultyState.Settings.BreakTechOnDamage.BreakTechOnDamageProbability");

            // Death Consequences
            var deathConsequences = jsonObject.GetValue<string>("6f=.LyC.:fe.n7p.q2@", "PlayerStateData.DifficultyState.Settings.DeathConsequences.DeathConsequencesDifficulty");

            // Fuel Usage
            var chargingRequirements = jsonObject.GetValue<string>("6f=.LyC.:fe.nhq.428", "PlayerStateData.DifficultyState.Settings.ChargingRequirements.ChargingRequirementsDifficulty");
            var fuelUse = jsonObject.GetValue<string>("6f=.LyC.:fe.jnM.Eg1", "PlayerStateData.DifficultyState.Settings.FuelUse.FuelUseDifficulty");
            var launchFuelCost = jsonObject.GetValue<string>("6f=.LyC.:fe.A9D.iqY", "PlayerStateData.DifficultyState.Settings.LaunchFuelCost.LaunchFuelCostDifficulty");

            // Crafting
            var craftingIsFree = jsonObject.GetValue<bool?>("6f=.LyC.:fe.?Dt", "PlayerStateData.DifficultyState.Settings.CraftingIsFree");

            // Purchases
            var currencyCost = jsonObject.GetValue<string>("6f=.LyC.:fe.tsk.Ubk", "PlayerStateData.DifficultyState.Settings.CurrencyCost.CurrencyCostDifficulty");

            // Goods Availability
            var itemShopAvailability = jsonObject.GetValue<string>("6f=.LyC.:fe.FB5.TYf", "PlayerStateData.DifficultyState.Settings.ItemShopAvailability.ItemShopAvailabilityDifficulty");

            // Inventory Stack Limits
            var inventoryStackLimits = jsonObject.GetValue<string>("6f=.LyC.:fe.kZ5.?SS", "PlayerStateData.DifficultyState.Settings.InventoryStackLimits.InventoryStackLimitsDifficulty");

            // Enemy Strength
            var damageGiven = jsonObject.GetValue<string>("6f=.LyC.:fe.PYQ.mum", "PlayerStateData.DifficultyState.Settings.DamageGiven.DamageGivenDifficulty");

            // On-Foot Combat
            var groundCombatTimers = jsonObject.GetValue<string>("6f=.LyC.:fe.jGh.ZbV", "PlayerStateData.DifficultyState.Settings.GroundCombatTimers.CombatTimerDifficultyOption");

            // Space Combat
            var spaceCombatTimers = jsonObject.GetValue<string>("6f=.LyC.:fe.Od7.ZbV", "PlayerStateData.DifficultyState.Settings.SpaceCombatTimers.CombatTimerDifficultyOption");

            // Creatures
            var creatureHostility = jsonObject.GetValue<string>("6f=.LyC.:fe.BbG.1c;", "PlayerStateData.DifficultyState.Settings.CreatureHostility.CreatureHostilityDifficulty");

            // Inventory Transfer Range
            var inventoriesAlwaysInRange = jsonObject.GetValue<bool?>("6f=.LyC.:fe.pS0", "PlayerStateData.DifficultyState.Settings.InventoriesAlwaysInRange");

            // Hyperdrive System Access
            var warpDriveRequirements = jsonObject.GetValue<bool?>("6f=.LyC.:fe.aw9", "PlayerStateData.DifficultyState.Settings.WarpDriveRequirements");

            // Base Power
            var baseAutoPower = jsonObject.GetValue<bool?>("6f=.LyC.:fe.uo4", "PlayerStateData.DifficultyState.Settings.BaseAutoPower");

            // Reputation & Standing Gain
            var reputationGain = jsonObject.GetValue<string>("6f=.LyC.:fe.vo>.S@3", "PlayerStateData.DifficultyState.Settings.ReputationGain.ReputationGainDifficulty");

            var stringValues = new[]
            {
                activeSurvivalBars,
                hazardDrain,
                energyDrain,
                substanceCollection,
                sprintingCost,
                scannerRecharge,
                damageReceived,
                breakTechOnDamage,
                deathConsequences,
                chargingRequirements,
                fuelUse,
                launchFuelCost,
                currencyCost,
                itemShopAvailability,
                inventoryStackLimits,
                damageGiven,
                groundCombatTimers,
                spaceCombatTimers,
                creatureHostility,
                reputationGain,
            };
            var boolValues = new[]
            {
                craftingIsFree,
                inventoriesAlwaysInRange,
                warpDriveRequirements,
                baseAutoPower,
            };

            if (IsGameModePreset(stringValues, boolValues, "All", "Normal", "Normal", "Normal", "Full", "Normal", "Normal", "Low", "ItemGrave", "Normal", "Normal", "Normal", "false", "Normal", "Normal", "High", "Normal", "Normal", "Normal", "FullEcosystem", "false", "true", "false", "Normal"))
                return PresetGameModeEnum.Normal;

            if (IsGameModePreset(stringValues, boolValues, "None", "Slow", "Slow", "Normal", "Free", "Fast", "None", "None", "None", "None", "Free", "Free", "true", "Free", "High", "High", "Normal", "Off", "Off", "NeverAttack", "true", "false", "true", "Fast"))
                return PresetGameModeEnum.Creative;

            if (IsGameModePreset(stringValues, boolValues, "All", "Fast", "Fast", "Low", "Full", "Normal", "High", "High", "DestroyItems", "High", "Expensive", "High", "false", "Normal", "Low", "Normal", "Normal", "Fast", "Fast", "FullEcosystem", "false", "true", "false", "Normal"))
                return PresetGameModeEnum.Survival;

            if (IsGameModePreset(stringValues, boolValues, "HealthAndHazard", "Slow", "Slow", "High", "Low", "VeryFast", "Low", "None", "None", "Low", "Cheap", "Low", "false", "Cheap", "High", "High", "High", "Slow", "Slow", "AttackIfProvoked", "true", "true", "true", "Fast"))
                return PresetGameModeEnum.Ambient;

            return PresetGameModeEnum.Unspecified;
        }

        return result;
    }

    private static bool IsGameModePreset(ReadOnlySpan<char> json, params string[] setpoints)
    {
        // SettingsLocked
        // AllSlotsUnlocked
        // TutorialEnabled
        // StartWithAllItemsKnown



        // Survival Elements
        ReadOnlySpan<char> activeSurvivalBars = $"\"tEx\":{{\"ZeS\":\"{setpoints[0]}\"".ToReadOnlySpan();
        if (!json.Contains(activeSurvivalBars, StringComparison.OrdinalIgnoreCase))
            return false;

        // Survival Difficulty
        ReadOnlySpan<char> hazardDrain = $"\"bGK\":{{\"ORx\":\"{setpoints[1]}\"".ToReadOnlySpan();
        if (!json.Contains(hazardDrain, StringComparison.OrdinalIgnoreCase))
            return false;
        ReadOnlySpan<char> energyDrain = $"\"A:s\":{{\"Dn>\":\"{setpoints[2]}\"".ToReadOnlySpan();
        if (!json.Contains(energyDrain, StringComparison.OrdinalIgnoreCase))
            return false;

        // Natural Resources
        ReadOnlySpan<char> substanceCollection = $"\"jH@\":{{\"9JJ\":\"{setpoints[3]}\"".ToReadOnlySpan();
        if (!json.Contains(substanceCollection, StringComparison.OrdinalIgnoreCase))
            return false;

        // Sprinting
        ReadOnlySpan<char> sprintingCost = $"\"l29\":{{\"LT:\":\"{setpoints[4]}\"".ToReadOnlySpan();
        if (!json.Contains(sprintingCost, StringComparison.OrdinalIgnoreCase))
            return false;

        // Scanner Recharge
        ReadOnlySpan<char> scannerRecharge = $"\"Lf?\":{{\"gFS\":\"{setpoints[5]}\"".ToReadOnlySpan();
        if (!json.Contains(scannerRecharge, StringComparison.OrdinalIgnoreCase))
            return false;

        // Damage Levels
        ReadOnlySpan<char> damageReceived = $"\"hXp\":{{\"cYk\":\"{setpoints[6]}\"".ToReadOnlySpan();
        if (!json.Contains(damageReceived, StringComparison.OrdinalIgnoreCase))
            return false;

        // Technology Damage
        ReadOnlySpan<char> breakTechOnDamage = $"\"gd>\":{{\"ef4\":\"{setpoints[7]}\"".ToReadOnlySpan();
        if (!json.Contains(breakTechOnDamage, StringComparison.OrdinalIgnoreCase))
            return false;

        // Death Consequences
        ReadOnlySpan<char> deathConsequences = $"\"n7p\":{{\"q2@\":\"{setpoints[8]}\"".ToReadOnlySpan();
        if (!json.Contains(deathConsequences, StringComparison.OrdinalIgnoreCase))
            return false;

        // Fuel Usage
        ReadOnlySpan<char> chargingRequirements = $"\"nhq\":{{\"428\":\"{setpoints[9]}\"".ToReadOnlySpan();
        if (!json.Contains(chargingRequirements, StringComparison.OrdinalIgnoreCase))
            return false;
        ReadOnlySpan<char> fuelUse = $"\"jnM\":{{\"Eg1\":\"{setpoints[10]}\"".ToReadOnlySpan();
        if (!json.Contains(fuelUse, StringComparison.OrdinalIgnoreCase))
            return false;
        ReadOnlySpan<char> launchFuelCost = $"\"A9D\":{{\"iqY\":\"{setpoints[11]}\"".ToReadOnlySpan();
        if (!json.Contains(launchFuelCost, StringComparison.OrdinalIgnoreCase))
            return false;

        // Crafting
        ReadOnlySpan<char> craftingIsFree = $"\"?Dt\":{setpoints[12]},".ToReadOnlySpan();
        if (!json.Contains(craftingIsFree, StringComparison.OrdinalIgnoreCase))
            return false;

        // Purchases
        ReadOnlySpan<char> currencyCost = $"\"tsk\":{{\"Ubk\":\"{setpoints[13]}\"".ToReadOnlySpan();
        if (!json.Contains(currencyCost, StringComparison.OrdinalIgnoreCase))
            return false;

        // Goods Availability
        ReadOnlySpan<char> itemShopAvailability = $"\"FB5\":{{\"TYf\":\"{setpoints[14]}\"".ToReadOnlySpan();
        if (!json.Contains(itemShopAvailability, StringComparison.OrdinalIgnoreCase))
            return false;

        // Inventory Stack Limits
        ReadOnlySpan<char> inventoryStackLimits = $"\"kZ5\":{{\"?SS\":\"{setpoints[15]}\"".ToReadOnlySpan();
        if (!json.Contains(inventoryStackLimits, StringComparison.OrdinalIgnoreCase))
            return false;

        // Enemy Strength
        ReadOnlySpan<char> damageGiven = $"\"PYQ\":{{\"mum\":\"{setpoints[16]}\"".ToReadOnlySpan();
        if (!json.Contains(damageGiven, StringComparison.OrdinalIgnoreCase))
            return false;

        // On-Foot Combat
        ReadOnlySpan<char> groundCombatTimers = $"\"jGh\":{{\"ZbV\":\"{setpoints[17]}\"".ToReadOnlySpan();
        if (!json.Contains(groundCombatTimers, StringComparison.OrdinalIgnoreCase))
            return false;

        // Space Combat
        ReadOnlySpan<char> spaceCombatTimers = $"\"Od7\":{{\"ZbV\":\"{setpoints[18]}\"".ToReadOnlySpan();
        if (!json.Contains(spaceCombatTimers, StringComparison.OrdinalIgnoreCase))
            return false;

        // Creatures
        ReadOnlySpan<char> creatureHostility = $"\"BbG\":{{\"1c;\":\"{setpoints[19]}\"".ToReadOnlySpan();
        if (!json.Contains(creatureHostility, StringComparison.OrdinalIgnoreCase))
            return false;

        // Inventory Transfer Range
        ReadOnlySpan<char> inventoriesAlwaysInRange = $"\"pS0\":{setpoints[20]},".ToReadOnlySpan();
        if (!json.Contains(inventoriesAlwaysInRange, StringComparison.OrdinalIgnoreCase))
            return false;

        // Hyperdrive System Access
        ReadOnlySpan<char> warpDriveRequirements = $"\"aw9\":{setpoints[21]},".ToReadOnlySpan();
        if (!json.Contains(warpDriveRequirements, StringComparison.OrdinalIgnoreCase))
            return false;

        // Base Power
        ReadOnlySpan<char> baseAutoPower = $"\"uo4\":{setpoints[22]},".ToReadOnlySpan();
        if (!json.Contains(baseAutoPower, StringComparison.OrdinalIgnoreCase))
            return false;

        // Reputation & Standing Gain
        ReadOnlySpan<char> reputationGain = $"\"vo>\":{{\"S@3\":\"{setpoints[23]}\"".ToReadOnlySpan();
        return json.Contains(reputationGain, StringComparison.OrdinalIgnoreCase); // if true all previous values where true as well and therefore it matches the preset
    }

    private static bool IsGameModePreset(string?[] stringValues, bool?[] boolValues, params string[] setpoints)
    {
        // Survival Elements
        if (stringValues[0] != setpoints[0])
            return false;

        // Survival Difficulty
        if (stringValues[1] != setpoints[1]) // hazardDrain
            return false;
        if (stringValues[2] != setpoints[2]) // energyDrain
            return false;

        // Natural Resources
        if (stringValues[3] != setpoints[3]) // substanceCollection
            return false;

        // Sprinting
        if (stringValues[4] != setpoints[4]) // sprintingCost
            return false;

        // Scanner Recharge
        if (stringValues[5] != setpoints[5]) // scannerRecharge
            return false;

        // Damage Levels
        if (stringValues[6] != setpoints[6]) // damageReceived
            return false;

        // Technology Damage
        if (stringValues[7] != setpoints[7]) // breakTechOnDamage
            return false;

        // Death Consequences
        if (stringValues[8] != setpoints[8]) // deathConsequences
            return false;

        // Fuel Usage
        if (stringValues[9] != setpoints[9]) // chargingRequirements
            return false;
        if (stringValues[10] != setpoints[10]) // fuelUse
            return false;
        if (stringValues[11] != setpoints[11]) // launchFuelCost
            return false;

        // Crafting
        if (!boolValues[0].IsValue(bool.Parse(setpoints[12]))) // craftingIsFree
            return false;

        // Purchases
        if (stringValues[12] != setpoints[13]) // currencyCost
            return false;

        // Goods Availability
        if (stringValues[13] != setpoints[14]) // itemShopAvailability
            return false;

        // Inventory Stack Limits
        if (stringValues[14] != setpoints[15]) // inventoryStackLimits
            return false;

        // Enemy Strength
        if (stringValues[15] != setpoints[16]) // damageGiven
            return false;

        // On-Foot Combat
        if (stringValues[16] != setpoints[17]) // groundCombatTimers
            return false;

        // Space Combat
        if (stringValues[17] != setpoints[18]) // spaceCombatTimers
            return false;

        // Creatures
        if (stringValues[18] != setpoints[19]) // creatureHostility
            return false;

        // Inventory Transfer Range
        if (!boolValues[1].IsValue(bool.Parse(setpoints[20]))) // inventoriesAlwaysInRange
            return false;

        // Hyperdrive System Access
        if (!boolValues[2].IsValue(bool.Parse(setpoints[21]))) // warpDriveRequirements
            return false;

        // Base Power
        if (!boolValues[3].IsValue(bool.Parse(setpoints[22]))) // baseAutoPower
            return false;

        // Reputation & Standing Gain
        return stringValues[19] == setpoints[23]; // reputationGain // if true all previous values where true as well and therefore it matches the preset
    }

    #endregion

    #region SeasonEnum

    /// <summary>
    /// Gets the Season (Expedition) for the specified container.
    /// </summary>
    /// <param name="container"></param>
    /// <returns></returns>
    internal static SeasonEnum GetSeasonEnum(Container container)
    {
        var mode = Globals.Json.GetGameModeEnum(container);
        if (mode is null or < PresetGameModeEnum.Seasonal)
            return SeasonEnum.None;

        var futureSeason = (int)(SeasonEnum.Future);
        var i = (int)(SeasonEnum.Pioneers);

        // Latest stop if negative base version but should usually be stopped before with future season.
        while (Globals.Calculate.CalculateBaseVersion(container.Version, mode.Value, i) is int baseVersion && baseVersion > 0)
        {
            if (i >= futureSeason)
                return SeasonEnum.Future;

            if (baseVersion is >= Globals.Constant.THRESHOLD_VANILLA and < Globals.Constant.THRESHOLD_VANILLA_GAMEMODE)
                return (SeasonEnum)(i);

            i++;
        }

        return SeasonEnum.None;
    }

    #endregion

    #region Total Play Time

    /// <summary>
    /// Gets the total play time from within the save.
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public static long GetTotalPlayTime(string json)
    {
#if NETSTANDARD2_0_OR_GREATER || NET6_0
        if (GetRegex(RegexTotalPlayTimeObfuscated, json, out var resultObfuscated))
#else
        if (GetRegex(RegexTotalPlayTimeObfuscated(), json, out var resultObfuscated))
#endif
            return resultObfuscated;

#if NETSTANDARD2_0_OR_GREATER || NET6_0
        if (GetRegex(RegexTotalPlayTimePlaintext, json, out var resultPlaintext))
#else
        if (GetRegex(RegexTotalPlayTimePlaintext(), json, out var resultPlaintext))
#endif
            return resultPlaintext;

        return -1;
    }

    /// <summary>
    /// Gets the total play time from within the save.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <returns></returns>
    internal static long GetTotalPlayTime(JObject jsonObject)
    {
        return jsonObject.GetValue<long?>("6f=.Lg8", "PlayerStateData.TotalPlayTime") ?? 0;
    }

    #endregion

    #region Version

    /// <summary>
    /// Gets the in-file version of the save.
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public static int GetVersion(string json)
    {
#if NETSTANDARD2_0_OR_GREATER || NET6_0
        if (GetRegex(RegexVersionObfuscated, json, out var resultObfuscated))
#else
        if (GetRegex(RegexVersionObfuscated(), json, out var resultObfuscated))
#endif
            return (int)(resultObfuscated);

#if NETSTANDARD2_0_OR_GREATER || NET6_0
        if (GetRegex(RegexVersionPlaintext, json, out var resultPlaintext))
#else
        if (GetRegex(RegexVersionPlaintext(), json, out var resultPlaintext))
#endif
            return (int)(resultPlaintext);

        return -1;
    }

    /// <summary>
    /// Gets the in-file version of the save.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <returns></returns>
    internal static int GetVersion(JObject jsonObject)
    {
        return jsonObject.GetValue<int?>("F2P", "Version") ?? -1;
    }

    #endregion

    #region VersionEnum

    /// <summary>
    /// Gets the game version for the specified container.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="jsonObject"></param>
    /// <returns></returns>
    internal static VersionEnum GetVersionEnum(Container container, JObject jsonObject)
    {
        /** SaveVersion and new Keys to determine the GameVersion.

        GameVersion = CreativeVersion/BaseVersion (Obfuscated = Deobfuscated)
        ??? = ????/???? (??? = ?)

        Singularity
        434 = 4657/4145
        433 = 4657/4145
        430 = 4657/4145 (:?x = GreyIfCantStart)

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

        if (container.BaseVersion >= 4144) // 4.20, 4.25, 4.30
        {
            // Only used in actual Expedition saves.
            //var greyIfCantStart = jsonObject.SelectTokens(usesMapping ? "PlayerStateData.SeasonData.Stages[*].Milestones[*].GreyIfCantStart" : "6f=.Rol.3Mw[*].kr6[*].:?x");
            //if (greyIfCantStart.Any())
            //    return VersionEnum.Singularity;

            // This is actually VersionEnum.Mac but it made most of the preperation for Singularity Expedition and therefore we already use this here.
            var seasonStartMusicOverride = jsonObject.SelectToken(usesMapping ? "PlayerStateData.SeasonData.SeasonStartMusicOverride" : "6f=.Rol.XEk");
            if (seasonStartMusicOverride is not null)
                return VersionEnum.Singularity;

            return VersionEnum.Interceptor;
        }

        if (container.BaseVersion >= 4143) // 4.10
            return VersionEnum.Fractal;

        if (container.BaseVersion >= 4142) // 4.05
            return VersionEnum.WaypointWithSuperchargedSlots;

        if (container.BaseVersion >= 4141) // 4.04
            return VersionEnum.WaypointWithAgileStat;

        if (container.BaseVersion >= 4140) // 4.00
            return VersionEnum.Waypoint;

        if (container.BaseVersion >= 4139) // 3.94
            return VersionEnum.Endurance;

        if (container.BaseVersion >= 4138) // 3.85, 3.90
        {
            var nextLoadSpawnsWithFreshStart = jsonObject.SelectToken(usesMapping ? "PlayerStateData.NextLoadSpawnsWithFreshStart" : "6f=.Sd6");
            if (nextLoadSpawnsWithFreshStart is not null)
                return VersionEnum.Leviathan;

            return VersionEnum.Outlaws;
        }

        if (container.BaseVersion >= 4137) // 3.81, 3.84
        {
            var vehicleAIControlEnabled = jsonObject.SelectToken(usesMapping ? "PlayerStateData.VehicleAIControlEnabled" : "6f=.Agx");
            if (vehicleAIControlEnabled is not null)
                return VersionEnum.SentinelWithVehicleAI;

            return VersionEnum.SentinelWithWeaponResource;
        }

        if (container.BaseVersion >= 4136) // 3.80
            return VersionEnum.Sentinel;

        if (container.BaseVersion >= 4135) // 3.60, 3.70
        {
            var sandwormOverrides = jsonObject.SelectTokens(usesMapping ? "PlayerStateData.SeasonData.SandwormOverrides" : "6f=.Rol.qs?");
            if (sandwormOverrides.Any())
                return VersionEnum.Emergence;

            return VersionEnum.Frontiers;
        }

        if (container.BaseVersion >= 4129) // 3.30, 3.40, 3.50, 3.51
        {
            var authorOnlineID = jsonObject.SelectTokens(usesMapping ? "PlayerStateData.ByteBeatLibrary.MySongs..AuthorOnlineID" : "6f=.8iI.ON4..m7b");
            if (authorOnlineID.Any())
                return VersionEnum.PrismsWithBytebeatAuthor;

            var byteBeatLibrary = jsonObject.SelectToken(usesMapping ? "PlayerStateData.ByteBeatLibrary" : "6f=.8iI");
            if (byteBeatLibrary is not null)
                return VersionEnum.Prisms;

            var mainMissionTitle = jsonObject.SelectToken(usesMapping ? "PlayerStateData.SeasonData.MainMissionTitle" : "6f=.Rol.Whh");
            if (mainMissionTitle is not null)
                return VersionEnum.Beachhead;

            return VersionEnum.Expeditions;
        }

        if (container.BaseVersion >= 4127) // 3.10, 3.20
        {
            var pets = jsonObject.SelectToken(usesMapping ? "PlayerStateData.Pets" : "6f=.;4P");
            if (pets is not null)
                return VersionEnum.Companions;

            return VersionEnum.NextGeneration;
        }

        if (container.BaseVersion >= 4126) // 2.50, 2.60, 3.00
        {
            var previousUniverseAddress = jsonObject.SelectToken(usesMapping ? "PlayerStateData.PreviousUniverseAddress" : "6f=.ux@");
            if (previousUniverseAddress is not null)
                return VersionEnum.Origins;

            var abandonedFreighterPositionInSystem = jsonObject.SelectToken(usesMapping ? "SpawnStateData.AbandonedFreighterPositionInSystem" : "6f=.Ovv");
            if (abandonedFreighterPositionInSystem is not null)
                return VersionEnum.Desolation;

            return VersionEnum.Crossplay;
        }

        if (container.BaseVersion >= 4125) // 2.40
            return VersionEnum.ExoMech;

        if (container.BaseVersion >= 4124) // 2.26, 2.30
        {
            var currentPos = jsonObject.SelectTokens(usesMapping ? "PlayerStateData..CurrentPos" : "6f=..Xf4");
            if (currentPos.Any())
                return VersionEnum.LivingShip;

            return VersionEnum.SynthesisWithJetpack;
        }

        if (container.BaseVersion >= 4122) // 2.20
            return VersionEnum.Synthesis;

        if (container.BaseVersion >= 4119) // 2.11
            return VersionEnum.BeyondWithVehicleCam;

        return VersionEnum.Unknown;
    }

    /// <inheritdoc cref="GetVersionEnum(Container, JObject)"/>
    internal static VersionEnum GetVersionEnum(Container container, string json)
    {
        if (container.BaseVersion >= 4144) // 4.20, 4.25, 4.30
        {
            // Only used in actual Expedition saves.
            //var greyIfCantStart = json.Contains("\":?x\":"); // GreyIfCantStart
            //if (greyIfCantStart)
            //    return VersionEnum.Singularity;

            // This is actually VersionEnum.Mac but it made most of the preperation for Singularity Expedition and therefore we already use this here.
            var seasonStartMusicOverride = json.Contains("\"XEk\":"); // SeasonStartMusicOverride
            if (seasonStartMusicOverride)
                return VersionEnum.Singularity;

            return VersionEnum.Interceptor;
        }

        if (container.BaseVersion >= 4143) // 4.10
            return VersionEnum.Fractal;

        if (container.BaseVersion >= 4142) // 4.05
            return VersionEnum.WaypointWithSuperchargedSlots;

        if (container.BaseVersion >= 4141) // 4.04
            return VersionEnum.WaypointWithAgileStat;

        if (container.BaseVersion >= 4140) // 4.00
            return VersionEnum.Waypoint;

        if (container.BaseVersion >= 4139) // 3.94
            return VersionEnum.Endurance;

        if (container.BaseVersion >= 4138) // 3.85, 3.90
        {
            var nextLoadSpawnsWithFreshStart = json.Contains("\"Sd6\":"); // NextLoadSpawnsWithFreshStart
            if (nextLoadSpawnsWithFreshStart)
                return VersionEnum.Leviathan;

            return VersionEnum.Outlaws;
        }

        if (container.BaseVersion >= 4137) // 3.81, 3.84
        {
            var vehicleAIControlEnabled = json.Contains("\"Agx\":"); // VehicleAIControlEnabled
            if (vehicleAIControlEnabled)
                return VersionEnum.SentinelWithVehicleAI;

            return VersionEnum.SentinelWithWeaponResource;
        }

        if (container.BaseVersion >= 4136) // 3.80
            return VersionEnum.Sentinel;

        if (container.BaseVersion >= 4135) // 3.60, 3.70
        {
            var sandwormOverrides = json.Contains("\"qs?\":"); // SandwormOverrides
            if (sandwormOverrides)
                return VersionEnum.Emergence;

            return VersionEnum.Frontiers;
        }

        if (container.BaseVersion >= 4129) // 3.30, 3.40, 3.50, 3.51
        {
            var authorOnlineID = json.Contains("\"m7b\":"); // AuthorOnlineID
            if (authorOnlineID)
                return VersionEnum.PrismsWithBytebeatAuthor;

            var byteBeatLibrary = json.Contains("\"8iI\":"); // ByteBeatLibrary
            if (byteBeatLibrary)
                return VersionEnum.Prisms;

            var mainMissionTitle = json.Contains("\"Whh\":"); // MainMissionTitle
            if (mainMissionTitle)
                return VersionEnum.Beachhead;

            return VersionEnum.Expeditions;
        }

        if (container.BaseVersion >= 4127) // 3.10, 3.20
        {
            var pets = json.Contains("\"Mcl\":"); // Pets
            if (pets)
                return VersionEnum.Companions;

            return VersionEnum.NextGeneration;
        }

        if (container.BaseVersion >= 4126) // 2.50, 2.60, 3.00
        {
            var previousUniverseAddress = json.Contains("\"ux@\":"); // PreviousUniverseAddress
            if (previousUniverseAddress)
                return VersionEnum.Origins;

            var abandonedFreighterPositionInSystem = json.Contains("\"Ovv\":"); // AbandonedFreighterPositionInSystem
            if (abandonedFreighterPositionInSystem)
                return VersionEnum.Desolation;

            return VersionEnum.Crossplay;
        }

        if (container.BaseVersion >= 4125) // 2.40
            return VersionEnum.ExoMech;

        if (container.BaseVersion >= 4124) // 2.26, 2.30
        {
            var currentPos = json.Contains("\"Xf4\":"); // CurrentPos
            if (currentPos)
                return VersionEnum.LivingShip;

            return VersionEnum.SynthesisWithJetpack;
        }

        if (container.BaseVersion >= 4122) // 2.20
            return VersionEnum.Synthesis;

        if (container.BaseVersion >= 4119) // 2.11
            return VersionEnum.BeyondWithVehicleCam;

        return VersionEnum.Unknown;
    }

    #endregion
}
