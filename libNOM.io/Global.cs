//global using libNOM.io.Data;
//global using libNOM.io.Enums;
//global using libNOM.io.Extensions;

using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace libNOM.io;


internal static class Global
{
    #region Constant

    internal const string FILE_TIMESTAMP_FORMAT = "yyyyMMddHHmmss";

    internal const int GAMEMODE_NORMAL = (int)(PresetGameModeEnum.Normal); // 1
    internal const int GAMEMODE_PERMADEATH = (int)(PresetGameModeEnum.Permadeath); // 5

    internal const string HEADER_PLAINTEXT = "{\"Version\":";
    internal const string HEADER_PLAINTEXT_OBFUSCATED = "{\"F2P\":";
    internal const uint HEADER_SAVE_STREAMING_CHUNK = 0xFEEDA1E5; // 4276986341
    internal const string HEADER_SAVEWIZARD = "NOMANSKY";

    internal const int OFFSET_GAMEMODE = 512;
    internal const int OFFSET_INDEX = 2;
    internal const int OFFSET_SEASON = 128;

    internal const int THRESHOLD = 4098;
    internal const int THRESHOLD_GAMEMODE = THRESHOLD + OFFSET_GAMEMODE;
    internal const int THRESHOLD_WAYPOINT = 4140;
    internal const int THRESHOLD_WAYPOINT_GAMEMODE = THRESHOLD_WAYPOINT + OFFSET_GAMEMODE;

    private static readonly Regex REGEX_TOTALPLAYTIME = new("\\\"Lg8\\\":(\\d+),", RegexOptions.Compiled, TimeSpan.FromSeconds(1));
    private static readonly Regex REGEX_TOTALPLAYTIME_PLAIN = new("\\\"TotalPlayTime\\\":(\\d+),", RegexOptions.Compiled, TimeSpan.FromSeconds(1));
    private static readonly Regex REGEX_VERSION = new("\\\"F2P\\\":(\\d{4,}),", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));
    private static readonly Regex REGEX_VERSION_PLAIN = new("\\\"Version\\\":(\\d{4,}),", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    #endregion

    #region BaseVersion

    /// <inheritdoc cref="CalculateBaseVersion(int, int, int)"/>
    internal static int CalculateBaseVersion(int version, PresetGameModeEnum mode, SeasonEnum season)
    {
        return CalculateBaseVersion(version, mode.Numerate(), season.Numerate());
    }
    /// <inheritdoc cref="CalculateBaseVersion(int, int, int)"/>
    internal static int CalculateBaseVersion(int version, PresetGameModeEnum mode, int season)
    {
        return CalculateBaseVersion(version, mode.Numerate(), season);
    }
    /// <inheritdoc cref="CalculateBaseVersion(int, int, int)"/>
    internal static int CalculateBaseVersion(int version, int mode, SeasonEnum season)
    {
        return CalculateBaseVersion(version, mode, season.Numerate());
    }
    /// <summary>
    /// Calculates the base version of a save based on the in-file version and a specified game mode and season.
    /// </summary>
    /// <param name="version">Version as specified in-file.</param>
    /// <param name="mode"></param>
    /// <param name="season"></param>
    /// <returns></returns>
    internal static int CalculateBaseVersion(int version, int mode, int season)
    {
        if (version >= THRESHOLD_WAYPOINT_GAMEMODE && mode < GAMEMODE_PERMADEATH)
        {
            mode = GAMEMODE_NORMAL;
        }
        return version - ((mode + (season * OFFSET_SEASON)) * OFFSET_GAMEMODE);
    }

    #endregion

    #region GameMode

    internal static PresetGameModeEnum? GetGameModeEnum(Container container)
    {
        return GetGameModeEnum(container, null);
    }

    /// <summary>
    /// Gets the game mode of a save based on the in-file version.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="json"></param>
    /// <returns></returns>
    internal static PresetGameModeEnum? GetGameModeEnum(Container container, object? json)
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
        {
            if (container.Version < (THRESHOLD_WAYPOINT + OFFSET_GAMEMODE) || json is not (string or JObject))
                return PresetGameModeEnum.Normal;

            return GetCustomGameMode(json);
        }

        return null;
    }

    private static PresetGameModeEnum GetCustomGameMode(object? json)
    {
        if (json is JObject jObject)
            return GetCustomGameMode(jObject);

        // DifficultyState is stored again in SeasonData and therefore limit the following check to the first part.
        // Without save name and summary around 800 characters.
        if (json is string stringObject)
#if NETSTANDARD2_0
            return GetCustomGameMode(stringObject.Substring(0, 1000));
#else
            return GetCustomGameMode(stringObject[..1000]); // sett
#endif

        return PresetGameModeEnum.Normal; // same as usual as this is only called if normal
    }

    private static PresetGameModeEnum GetCustomGameMode(JObject jsonObject)
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

        // Twice as InventoryStackLimits can be "Normal" if it was converted from pre-Waypoint.
        if (IsGameModePreset(stringValues, boolValues, "All", "Fast", "Fast", "Low", "Full", "Normal", "High", "High", "DestroyItems", "High", "Expensive", "High", "false", "Normal", "Low", "High", "Normal", "Fast", "Fast", "FullEcosystem", "false", "true", "false", "Normal"))
            return PresetGameModeEnum.Survival;
        if (IsGameModePreset(stringValues, boolValues, "All", "Fast", "Fast", "Low", "Full", "Normal", "High", "High", "DestroyItems", "High", "Expensive", "High", "false", "Normal", "Low", "Normal", "Normal", "Fast", "Fast", "FullEcosystem", "false", "true", "false", "Normal"))
            return PresetGameModeEnum.Survival;

        if (IsGameModePreset(stringValues, boolValues, "HealthAndHazard", "Slow", "Slow", "High", "Low", "VeryFast", "Low", "None", "None", "Low", "Cheap", "Low", "false", "Cheap", "High", "High", "High", "Slow", "Slow", "AttackIfProvoked", "true", "true", "true", "Fast"))
            return PresetGameModeEnum.Ambient;

        return PresetGameModeEnum.Unspecified;
    }

    private static bool IsGameModePreset(string?[] stringValues, bool?[] boolValues, params string[] setpoints)
    {
        // Survival Elements
        if (stringValues[0] != setpoints[0])
            return false;

        // Survival Difficulty
        if (stringValues[1] != setpoints[1])
            return false;
        if (stringValues[2] != setpoints[2])
            return false;

        // Natural Resources
        if (stringValues[3] != setpoints[3])
            return false;

        // Sprinting
        if (stringValues[4] != setpoints[4])
            return false;

        // Scanner Recharge
        if (stringValues[5] != setpoints[5])
            return false;

        // Damage Levels
        if (stringValues[6] != setpoints[6])
            return false;

        // Technology Damage
        if (stringValues[7] != setpoints[7])
            return false;

        // Death Consequences
        if (stringValues[8] != setpoints[8])
            return false;

        // Fuel Usage
        if (stringValues[9] != setpoints[9])
            return false;
        if (stringValues[10] != setpoints[10])
            return false;
        if (stringValues[11] != setpoints[11])
            return false;

        // Crafting
        if (!boolValues[0].IsValue(bool.Parse(setpoints[12])))
            return false;

        // Purchases
        if (stringValues[12] != setpoints[13])
            return false;

        // Goods Availability
        if (stringValues[13] != setpoints[14])
            return false;

        // Inventory Stack Limits
        if (stringValues[14] != setpoints[15]) // always "High"
            return false;

        // Enemy Strength
        if (stringValues[15] != setpoints[16])
            return false;

        // On-Foot Combat
        if (stringValues[16] != setpoints[17])
            return false;

        // Space Combat
        if (stringValues[17] != setpoints[18])
            return false;

        // Creatures
        if (stringValues[18] != setpoints[19])
            return false;

        // Inventory Transfer Range
        if (!boolValues[1].IsValue(bool.Parse(setpoints[20])))
            return false;

        // Hyperdrive System Access
        if (!boolValues[2].IsValue(bool.Parse(setpoints[21])))
            return false;

        // Base Power
        if (!boolValues[3].IsValue(bool.Parse(setpoints[22])))
            return false;

        // Reputation & Standing Gain
        return stringValues[19] == setpoints[23]; // if true all previous values where true as well and therefore it matches the preset
    }

    private static PresetGameModeEnum GetCustomGameMode(string json)
    {
        if (IsGameModePreset(json, "All", "Normal", "Normal", "Normal", "Full", "Normal", "Normal", "Low", "ItemGrave", "Normal", "Normal", "Normal", "false", "Normal", "Normal", "High", "Normal", "Normal", "Normal", "FullEcosystem", "false", "true", "false", "Normal"))
            return PresetGameModeEnum.Normal;

        if (IsGameModePreset(json, "None", "Slow", "Slow", "Normal", "Free", "Fast", "None", "None", "None", "None", "Free", "Free", "true", "Free", "High", "High", "Normal", "Off", "Off", "NeverAttack", "true", "false", "true", "Fast"))
            return PresetGameModeEnum.Creative;

        if (IsGameModePreset(json, "All", "Fast", "Fast", "Low", "Full", "Normal", "High", "High", "DestroyItems", "High", "Expensive", "High", "false", "Normal", "Low", "High", "Normal", "Fast", "Fast", "FullEcosystem", "false", "true", "false", "Normal"))
            return PresetGameModeEnum.Survival;

        if (IsGameModePreset(json, "HealthAndHazard", "Slow", "Slow", "High", "Low", "VeryFast", "Low", "None", "None", "Low", "Cheap", "Low", "false", "Cheap", "High", "High", "High", "Slow", "Slow", "AttackIfProvoked", "true", "true", "true", "Fast"))
            return PresetGameModeEnum.Ambient;

        return PresetGameModeEnum.Unspecified;
    }

    private static bool IsGameModePreset(string json, params string[] setpoints)
    {
        // Survival Elements
        if (!json.Contains($"\"tEx\":{{\"ZeS\":\"{setpoints[0]}\""))
            return false;

        // Survival Difficulty
        if (!json.Contains($"\"bGK\":{{\"ORx\":\"{setpoints[1]}\"")) // hazardDrain
            return false;
        if (!json.Contains($"\"A:s\":{{\"Dn>\":\"{setpoints[2]}\"")) // energyDrain
            return false;

        // Natural Resources
        if (!json.Contains($"\"jH@\":{{\"9JJ\":\"{setpoints[3]}\""))
            return false;

        // Sprinting
        if (!json.Contains($"\"l29\":{{\"LT:\":\"{setpoints[4]}\""))
            return false;

        // Scanner Recharge
        if (!json.Contains($"\"Lf?\":{{\"gFS\":\"{setpoints[5]}\""))
            return false;

        // Damage Levels
        if (!json.Contains($"\"hXp\":{{\"cYk\":\"{setpoints[6]}\""))
            return false;

        // Technology Damage
        if (!json.Contains($"\"gd>\":{{\"ef4\":\"{setpoints[7]}\""))
            return false;

        // Death Consequences
        if (!json.Contains($"\"n7p\":{{\"q2@\":\"{setpoints[8]}\""))
            return false;

        // Fuel Usage
        if (!json.Contains($"\"nhq\":{{\"428\":\"{setpoints[9]}\"")) // chargingRequirements
            return false;
        if (!json.Contains($"\"jnM\":{{\"Eg1\":\"{setpoints[10]}\"")) // fuelUse
            return false;
        if (!json.Contains($"\"A9D\":{{\"iqY\":\"{setpoints[11]}\"")) // launchFuelCost
            return false;

        // Crafting
        if (!json.Contains($"\"?Dt\":{setpoints[12]},"))
            return false;

        // Purchases
        if (!json.Contains($"\"tsk\":{{\"Ubk\":\"{setpoints[13]}\""))
            return false;

        // Goods Availability
        if (!json.Contains($"\"FB5\":{{\"TYf\":\"{setpoints[14]}\""))
            return false;

        // Inventory Stack Limits
        if (!json.Contains($"\"kZ5\":{{\"?SS\":\"{setpoints[15]}\"")) // always "High"
            return false;

        // Enemy Strength
        if (!json.Contains($"\"PYQ\":{{\"mum\":\"{setpoints[16]}\""))
            return false;

        // On-Foot Combat
        if (!json.Contains($"\"jGh\":{{\"ZbV\":\"{setpoints[17]}\""))
            return false;

        // Space Combat
        if (!json.Contains($"\"Od7\":{{\"ZbV\":\"{setpoints[18]}\""))
            return false;

        // Creatures
        if (!json.Contains($"\"BbG\":{{\"1c;\":\"{setpoints[19]}\""))
            return false;

        // Inventory Transfer Range
        if (!json.Contains($"\"pS0\":{setpoints[20]},"))
            return false;

        // Hyperdrive System Access
        if (!json.Contains($"\"aw9\":{setpoints[21]},"))
            return false;

        // Base Power
        if (!json.Contains($"\"uo4\":{setpoints[22]},"))
            return false;

        // Reputation & Standing Gain
        return json.Contains($"\"vo>\":{{\"S@3\":\"{setpoints[23]}\""); // if true all previous values where true as well and therefore it matches the preset
    }

    #endregion

    #region TotalPlayTime

    /// <summary>
    /// Gets the total play time from within the save file.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <returns></returns>
    internal static long GetTotalPlayTime(JObject jsonObject)
    {
        return jsonObject.GetValue<long?>("6f=.Lg8", "PlayerStateData.TotalPlayTime") ?? 0;
    }

    /// <inheritdoc cref="GetTotalPlayTime(JObject)"/>
    internal static long GetTotalPlayTime(string json)
    {
        var match = REGEX_TOTALPLAYTIME.Match(json);
        if (match.Success)
            return System.Convert.ToInt32(match.Groups[1].Value);

        var plain = REGEX_TOTALPLAYTIME_PLAIN.Match(json);
        return plain.Success ? System.Convert.ToInt32(plain.Groups[1].Value) : -1;
    }

    #endregion

    #region Version

    /// <inheritdoc cref="CalculateVersion(int, int, int)"/>
    internal static int CalculateVersion(int baseVersion, PresetGameModeEnum? mode, SeasonEnum season)
    {
        if (mode is null)
            return baseVersion;

        return CalculateVersion(baseVersion, mode.Numerate(), season.Numerate());
    }
    /// <inheritdoc cref="CalculateVersion(int, int, int)"/>
    internal static int CalculateVersion(int baseVersion, PresetGameModeEnum? mode, int season)
    {
        if (mode is null)
            return baseVersion;

        return CalculateVersion(baseVersion, mode.Numerate(), season);
    }
    /// <inheritdoc cref="CalculateVersion(int, int, int)"/>
    internal static int CalculateVersion(int baseVersion, int mode, SeasonEnum season)
    {
        return CalculateVersion(baseVersion, mode, season.Numerate());
    }
    /// <summary>
    /// Calculates the in-file version based on the base version of a save and a specified game mode and season.
    /// </summary>
    /// <param name="baseVersion">Base version of a save.</param>
    /// <param name="mode"></param>
    /// <param name="season"></param>
    /// <returns></returns>
    internal static int CalculateVersion(int baseVersion, int mode, int season)
    {
        // Season 1 =   7205 = BaseVersion + (6 * 512) + (  0 * 512) = BaseVersion + ((6 + (0 * 128)) * 512)
        // Season 2 = 138277 = BaseVersion + (6 * 512) + (256 * 512) = BaseVersion + ((6 + (2 * 128)) * 512)
        // Season 3 = 203815 = BaseVersion + (6 * 512) + (384 * 512) = BaseVersion + ((6 + (3 * 128)) * 512)
        // Season 4 = 269351 = BaseVersion + (6 * 512) + (512 * 512) = BaseVersion + ((6 + (4 * 128)) * 512)
        if (mode == (int)(PresetGameModeEnum.Seasonal))
            return baseVersion + ((mode + (season * OFFSET_SEASON)) * OFFSET_GAMEMODE);

        if (mode == (int)(PresetGameModeEnum.Permadeath) || baseVersion < THRESHOLD_WAYPOINT)
            return baseVersion + (mode * OFFSET_GAMEMODE);

        return baseVersion + ((int)(PresetGameModeEnum.Normal) * OFFSET_GAMEMODE);
    }

    /// <summary>
    /// Gets the in-file version.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <returns></returns>
    internal static int GetVersion(JObject jsonObject)
    {
        return jsonObject.GetValue<int?>("F2P", "Version") ?? -1;
    }

    /// <inheritdoc cref="GetVersion(JObject)"/>
    internal static int GetVersion(string json)
    {
        var match = REGEX_VERSION.Match(json);
        if (match.Success)
            return System.Convert.ToInt32(match.Groups[1].Value);

        var plain = REGEX_VERSION_PLAIN.Match(json);
        return plain.Success ? System.Convert.ToInt32(plain.Groups[1].Value) : -1;
    }

    #endregion
}
