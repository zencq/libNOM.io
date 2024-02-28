﻿namespace libNOM.io.Globals;


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

    public static readonly Dictionary<string, string[]> JSONPATH_EXTENSION = []; // provide possibilitly to extend the internal JSONPath dictionary by the using apps

    // internal //

    internal const int CACHE_EXPIRATION = 250; // milliseconds

    internal static readonly Difficulty DIFFICULTY_PRESET_NORMAL = new(ActiveSurvivalBarsDifficultyEnum.All, HazardDrainDifficultyEnum.Normal, EnergyDrainDifficultyEnum.Normal, SubstanceCollectionDifficultyEnum.Normal, SprintingCostDifficultyEnum.Full, ScannerRechargeDifficultyEnum.Normal, DamageReceivedDifficultyEnum.Normal, BreakTechOnDamageProbabilityEnum.Low, DeathConsequencesDifficultyEnum.ItemGrave, ChargingRequirementsDifficultyEnum.Normal, FuelUseDifficultyEnum.Normal, LaunchFuelCostDifficultyEnum.Normal, false, CurrencyCostDifficultyEnum.Normal, ItemShopAvailabilityDifficultyEnum.Normal, InventoryStackLimitsDifficultyEnum.High, DamageGivenDifficultyEnum.Normal, CombatTimerDifficultyOptionEnum.Normal, CombatTimerDifficultyOptionEnum.Normal, CreatureHostilityDifficultyEnum.FullEcosystem, false, true, false, ReputationGainDifficultyEnum.Normal);
    internal static readonly Difficulty DIFFICULTY_PRESET_CREATIVE = new(ActiveSurvivalBarsDifficultyEnum.None, HazardDrainDifficultyEnum.Slow, EnergyDrainDifficultyEnum.Slow, SubstanceCollectionDifficultyEnum.Normal, SprintingCostDifficultyEnum.Free, ScannerRechargeDifficultyEnum.Fast, DamageReceivedDifficultyEnum.None, BreakTechOnDamageProbabilityEnum.None, DeathConsequencesDifficultyEnum.None, ChargingRequirementsDifficultyEnum.None, FuelUseDifficultyEnum.Free, LaunchFuelCostDifficultyEnum.Free, true, CurrencyCostDifficultyEnum.Free, ItemShopAvailabilityDifficultyEnum.High, InventoryStackLimitsDifficultyEnum.High, DamageGivenDifficultyEnum.Normal, CombatTimerDifficultyOptionEnum.Off, CombatTimerDifficultyOptionEnum.Off, CreatureHostilityDifficultyEnum.NeverAttack, true, false, true, ReputationGainDifficultyEnum.Fast);
    internal static readonly Difficulty DIFFICULTY_PRESET_RELAXED = new(ActiveSurvivalBarsDifficultyEnum.HealthAndHazard, HazardDrainDifficultyEnum.Slow, EnergyDrainDifficultyEnum.Slow, SubstanceCollectionDifficultyEnum.High, SprintingCostDifficultyEnum.Low, ScannerRechargeDifficultyEnum.VeryFast, DamageReceivedDifficultyEnum.Low, BreakTechOnDamageProbabilityEnum.None, DeathConsequencesDifficultyEnum.None, ChargingRequirementsDifficultyEnum.Low, FuelUseDifficultyEnum.Cheap, LaunchFuelCostDifficultyEnum.Low, false, CurrencyCostDifficultyEnum.Cheap, ItemShopAvailabilityDifficultyEnum.High, InventoryStackLimitsDifficultyEnum.High, DamageGivenDifficultyEnum.High, CombatTimerDifficultyOptionEnum.Slow, CombatTimerDifficultyOptionEnum.Slow, CreatureHostilityDifficultyEnum.AttackIfProvoked, true, true, true, ReputationGainDifficultyEnum.Fast);
    internal static readonly Difficulty DIFFICULTY_PRESET_SURVIVAL = new(ActiveSurvivalBarsDifficultyEnum.All, HazardDrainDifficultyEnum.Fast, EnergyDrainDifficultyEnum.Fast, SubstanceCollectionDifficultyEnum.Low, SprintingCostDifficultyEnum.Full, ScannerRechargeDifficultyEnum.Normal, DamageReceivedDifficultyEnum.High, BreakTechOnDamageProbabilityEnum.High, DeathConsequencesDifficultyEnum.DestroyItems, ChargingRequirementsDifficultyEnum.High, FuelUseDifficultyEnum.Expensive, LaunchFuelCostDifficultyEnum.High, false, CurrencyCostDifficultyEnum.Normal, ItemShopAvailabilityDifficultyEnum.Low, InventoryStackLimitsDifficultyEnum.Normal, DamageGivenDifficultyEnum.Normal, CombatTimerDifficultyOptionEnum.Fast, CombatTimerDifficultyOptionEnum.Fast, CreatureHostilityDifficultyEnum.FullEcosystem, false, true, false, ReputationGainDifficultyEnum.Normal);
    internal static readonly Difficulty DIFFICULTY_PRESET_PERMADEATH = new(ActiveSurvivalBarsDifficultyEnum.All, HazardDrainDifficultyEnum.Fast, EnergyDrainDifficultyEnum.Fast, SubstanceCollectionDifficultyEnum.Low, SprintingCostDifficultyEnum.Full, ScannerRechargeDifficultyEnum.Normal, DamageReceivedDifficultyEnum.High, BreakTechOnDamageProbabilityEnum.High, DeathConsequencesDifficultyEnum.DestroySave, ChargingRequirementsDifficultyEnum.High, FuelUseDifficultyEnum.Expensive, LaunchFuelCostDifficultyEnum.High, false, CurrencyCostDifficultyEnum.Normal, ItemShopAvailabilityDifficultyEnum.Low, InventoryStackLimitsDifficultyEnum.Normal, DamageGivenDifficultyEnum.Normal, CombatTimerDifficultyOptionEnum.Fast, CombatTimerDifficultyOptionEnum.Fast, CreatureHostilityDifficultyEnum.FullEcosystem, false, true, false, ReputationGainDifficultyEnum.Normal);

    internal const string FILE_TIMESTAMP_FORMAT = "yyyyMMddHHmmssfff";

    internal const short GAMEMODE_INT_NORMAL = (int)(PresetGameModeEnum.Normal); // 1
    internal const short GAMEMODE_INT_PERMADEATH = (int)(PresetGameModeEnum.Permadeath); // 5
    internal const short GAMEMODE_INT_SEASONAL = (int)(PresetGameModeEnum.Seasonal); // 6

    internal const GameVersionEnum LOWEST_SUPPORTED_VERSION = GameVersionEnum.BeyondWithVehicleCam;

    internal static readonly Dictionary<string, string[]> JSONPATH = new()
    {
        // absolut from root, common
        { "ACTIVE_CONTEXT", [ "", "", "XTp", "ActiveContext" ] },
        { "BASE_CONTEXT", [ "", "", "vLc", "BaseContext" ] },
        { "EXPEDITION_CONTEXT", [ "", "", "2YS", "ExpeditionContext" ] },
        { "GAME_VERSION_230", [ "6f=..Xf4", "PlayerStateData..CurrentPos" ] },
        { "GAME_VERSION_260", [ "rnc.Ovv", "SpawnStateData.AbandonedFreighterPositionInSystem" ] },
        { "GAME_VERSION_300", [ "6f=.ux@", "PlayerStateData.PreviousUniverseAddress" ] },
        { "GAME_VERSION_320", [ "6f=.Mcl", "PlayerStateData.Pets" ] },
        { "GAME_VERSION_340", [ "6f=.Rol.Whh", "PlayerStateData.SeasonData.MainMissionTitle" ] },
        { "GAME_VERSION_350", [ "6f=.8iI", "PlayerStateData.ByteBeatLibrary" ] },
        { "GAME_VERSION_351", [ "6f=.8iI.ON4..m7b", "PlayerStateData.ByteBeatLibrary.MySongs..AuthorOnlineID" ] },
        { "GAME_VERSION_370", [ "6f=.Rol.qs?",  "PlayerStateData.SeasonData.SandwormOverrides" ] },
        { "GAME_VERSION_384", [ "6f=.Agx",  "PlayerStateData.VehicleAIControlEnabled" ] },
        { "GAME_VERSION_390", [ "6f=.Sd6", "PlayerStateData.NextLoadSpawnsWithFreshStart" ] },
        { "GAME_VERSION_425", [ "6f=.Rol.XEk", "PlayerStateData.SeasonData.SeasonStartMusicOverride" ] },
        { "PLATFORM", [ "8>q", "Platform" ] },
        { "SAVE_NAME", [ "6f=.Pk4", "PlayerStateData.SaveName", "<h0.Pk4", "CommonStateData.SaveName" ] },
        { "SEASON_ID", [ "6f=.Rol.gou", "PlayerStateData.SeasonData.SeasonId", "<h0.Rol.gou", "CommonStateData.SeasonData.SeasonId" ] },
        { "TOTAL_PLAY_TIME", [ "6f=.Lg8", "PlayerStateData.TotalPlayTime", "<h0.Lg8", "CommonStateData.TotalPlayTime" ] },
        { "TRANSFER_UID_BYTEBEAT", [ "6f=.8iI.ON4[?(@.m7b == '{0}')]", "PlayerStateData.ByteBeatLibrary.MySongs[?(@.AuthorOnlineID == '{0}')]", "<h0.8iI.ON4[?(@.m7b == '{0}')]", "CommonStateData.ByteBeatLibrary.MySongs[?(@.AuthorOnlineID == '{0}')]" ] },
        { "TRANSFER_UID_DISCOVERY", [ "fDu.ETO.OsQ.?fB..[?(@.K7E == '{0}')]", "DiscoveryManagerData.DiscoveryData-v1.Store.Record..[?(@.UID == '{0}')]" ] },
        { "VERSION", [ "F2P", "Version" ] },

        // absolut from root, context depended
        { "DIFFICULTY_ACTIVE_SURVIVAL_BARS", [ "6f=.LyC.:fe.tEx.ZeS", "PlayerStateData.DifficultyState.Settings.ActiveSurvivalBars.ActiveSurvivalBarsDifficulty", "{0}.6f=.LyC.:fe.tEx.ZeS", "{0}.PlayerStateData.DifficultyState.Settings.ActiveSurvivalBars.ActiveSurvivalBarsDifficulty" ] },
        { "DIFFICULTY_HAZARD_DRAIN", [ "6f=.LyC.:fe.bGK.ORx", "PlayerStateData.DifficultyState.Settings.HazardDrain.HazardDrainDifficulty", "{0}.6f=.LyC.:fe.bGK.ORx", "{0}.PlayerStateData.DifficultyState.Settings.HazardDrain.HazardDrainDifficulty" ] },
        { "DIFFICULTY_ENERGY_DRAIN", [ "6f=.LyC.:fe.A:s.Dn>", "PlayerStateData.DifficultyState.Settings.EnergyDrain.EnergyDrainDifficulty", "{0}.6f=.LyC.:fe.A:s.Dn>", "{0}.PlayerStateData.DifficultyState.Settings.EnergyDrain.EnergyDrainDifficulty" ] },
        { "DIFFICULTY_SUBSTANCE_COLLECTION", [ "6f=.LyC.:fe.jH@.9JJ", "PlayerStateData.DifficultyState.Settings.SubstanceCollection.SubstanceCollectionDifficulty", "{0}.6f=.LyC.:fe.jH@.9JJ", "{0}.PlayerStateData.DifficultyState.Settings.SubstanceCollection.SubstanceCollectionDifficulty" ] },
        { "DIFFICULTY_SPRINTING_COST", [ "6f=.LyC.:fe.l29.LT:", "PlayerStateData.DifficultyState.Settings.SprintingCost.SprintingCostDifficulty", "{0}.6f=.LyC.:fe.l29.LT:", "{0}.PlayerStateData.DifficultyState.Settings.SprintingCost.SprintingCostDifficulty" ] },
        { "DIFFICULTY_SCANNER_RECHARGE", [ "6f=.LyC.:fe.Lf?.gFS", "PlayerStateData.DifficultyState.Settings.ScannerRecharge.ScannerRechargeDifficulty", "{0}.6f=.LyC.:fe.Lf?.gFS", "{0}.PlayerStateData.DifficultyState.Settings.ScannerRecharge.ScannerRechargeDifficulty" ] },
        { "DIFFICULTY_DAMAGE_RECEIVED", [ "6f=.LyC.:fe.hXp.cYk", "PlayerStateData.DifficultyState.Settings.DamageReceived.DamageReceivedDifficulty", "{0}.6f=.LyC.:fe.hXp.cYk", "{0}.PlayerStateData.DifficultyState.Settings.DamageReceived.DamageReceivedDifficulty" ] },
        { "DIFFICULTY_BREAK_TECH_ON_DAMAGE", [ "6f=.LyC.:fe.gd>.ef4", "PlayerStateData.DifficultyState.Settings.BreakTechOnDamage.BreakTechOnDamageProbability", "{0}.6f=.LyC.:fe.gd>.ef4", "{0}.PlayerStateData.DifficultyState.Settings.BreakTechOnDamage.BreakTechOnDamageProbability" ] },
        { "DIFFICULTY_DEATH_CONSEQUENCES", [ "6f=.LyC.:fe.n7p.q2@", "PlayerStateData.DifficultyState.Settings.DeathConsequences.DeathConsequencesDifficulty", "{0}.6f=.LyC.:fe.n7p.q2@", "{0}.PlayerStateData.DifficultyState.Settings.DeathConsequences.DeathConsequencesDifficulty" ] },
        { "DIFFICULTY_CHARGING_REQUIREMENTS", [ "6f=.LyC.:fe.nhq.428", "PlayerStateData.DifficultyState.Settings.ChargingRequirements.ChargingRequirementsDifficulty", "{0}.6f=.LyC.:fe.nhq.428", "{0}.PlayerStateData.DifficultyState.Settings.ChargingRequirements.ChargingRequirementsDifficulty" ] },
        { "DIFFICULTY_FUEL_USE", [ "6f=.LyC.:fe.jnM.Eg1", "PlayerStateData.DifficultyState.Settings.FuelUse.FuelUseDifficulty", "{0}.6f=.LyC.:fe.jnM.Eg1", "{0}.PlayerStateData.DifficultyState.Settings.FuelUse.FuelUseDifficulty" ] },
        { "DIFFICULTY_LAUNCH_FUEL_COST", [ "6f=.LyC.:fe.A9D.iqY", "PlayerStateData.DifficultyState.Settings.LaunchFuelCost.LaunchFuelCostDifficulty", "{0}.6f=.LyC.:fe.A9D.iqY", "{0}.PlayerStateData.DifficultyState.Settings.LaunchFuelCost.LaunchFuelCostDifficulty" ] },
        { "DIFFICULTY_CRAFTING_IS_FREE", [ "6f=.LyC.:fe.?Dt", "PlayerStateData.DifficultyState.Settings.CraftingIsFree", "{0}.6f=.LyC.:fe.?Dt", "{0}.PlayerStateData.DifficultyState.Settings.CraftingIsFree" ] },
        { "DIFFICULTY_CURRENCY_COST", [ "6f=.LyC.:fe.tsk.Ubk", "PlayerStateData.DifficultyState.Settings.CurrencyCost.CurrencyCostDifficulty", "{0}.6f=.LyC.:fe.tsk.Ubk", "{0}.PlayerStateData.DifficultyState.Settings.CurrencyCost.CurrencyCostDifficulty" ] },
        { "DIFFICULTY_ITEM_SHOP_AVAILABILITY", [ "6f=.LyC.:fe.FB5.TYf", "PlayerStateData.DifficultyState.Settings.ItemShopAvailability.ItemShopAvailabilityDifficulty", "{0}.6f=.LyC.:fe.FB5.TYf", "{0}.PlayerStateData.DifficultyState.Settings.ItemShopAvailability.ItemShopAvailabilityDifficulty" ] },
        { "DIFFICULTY_INVENTORY_STACK_LIMITS", [ "6f=.LyC.:fe.kZ5.?SS", "PlayerStateData.DifficultyState.Settings.InventoryStackLimits.InventoryStackLimitsDifficulty", "{0}.6f=.LyC.:fe.kZ5.?SS", "{0}.PlayerStateData.DifficultyState.Settings.InventoryStackLimits.InventoryStackLimitsDifficulty" ] },
        { "DIFFICULTY_DAMAGE_GIVEN", [ "6f=.LyC.:fe.PYQ.mum", "PlayerStateData.DifficultyState.Settings.DamageGiven.DamageGivenDifficulty", "{0}.6f=.LyC.:fe.PYQ.mum", "{0}.PlayerStateData.DifficultyState.Settings.DamageGiven.DamageGivenDifficulty" ] },
        { "DIFFICULTY_GROUND_COMBAT_TIMERS", [ "6f=.LyC.:fe.jGh.ZbV", "PlayerStateData.DifficultyState.Settings.GroundCombatTimers.CombatTimerDifficultyOption", "{0}.6f=.LyC.:fe.jGh.ZbV", "{0}.PlayerStateData.DifficultyState.Settings.GroundCombatTimers.CombatTimerDifficultyOption" ] },
        { "DIFFICULTY_SPACE_COMBAT_TIMERS", [ "6f=.LyC.:fe.Od7.ZbV", "PlayerStateData.DifficultyState.Settings.SpaceCombatTimers.CombatTimerDifficultyOption", "{0}.6f=.LyC.:fe.Od7.ZbV", "{0}.PlayerStateData.DifficultyState.Settings.SpaceCombatTimers.CombatTimerDifficultyOption" ] },
        { "DIFFICULTY_CREATURE_HOSTILITY", [ "6f=.LyC.:fe.BbG.1c;", "PlayerStateData.DifficultyState.Settings.CreatureHostility.CreatureHostilityDifficulty", "{0}.6f=.LyC.:fe.BbG.1c;", "{0}.PlayerStateData.DifficultyState.Settings.CreatureHostility.CreatureHostilityDifficulty" ] },
        { "DIFFICULTY_INVENTORIES_ALWAYS_IN_RANGE", [ "6f=.LyC.:fe.pS0", "PlayerStateData.DifficultyState.Settings.InventoriesAlwaysInRange", "{0}.6f=.LyC.:fe.pS0", "{0}.PlayerStateData.DifficultyState.Settings.InventoriesAlwaysInRange" ] },
        { "DIFFICULTY_WARP_DRIVE_REQUIREMENTS", [ "6f=.LyC.:fe.aw9", "PlayerStateData.DifficultyState.Settings.WarpDriveRequirements", "{0}.6f=.LyC.:fe.aw9", "{0}.PlayerStateData.DifficultyState.Settings.WarpDriveRequirements" ] },
        { "DIFFICULTY_BASE_AUTO_POWER", [ "6f=.LyC.:fe.uo4", "PlayerStateData.DifficultyState.Settings.BaseAutoPower", "{0}.6f=.LyC.:fe.uo4", "{0}.PlayerStateData.DifficultyState.Settings.BaseAutoPower" ] },
        { "DIFFICULTY_REPUTATION_GAIN", [ "6f=.LyC.:fe.vo>.S@3", "PlayerStateData.DifficultyState.Settings.ReputationGain.ReputationGainDifficulty", "{0}.6f=.LyC.:fe.vo>.S@3", "{0}.PlayerStateData.DifficultyState.Settings.ReputationGain.ReputationGainDifficulty" ] },
        //{ "GAME_MODE", [ "", "", "{0}.idA", "{0}.GameMode" ] },
        { "FREIGHTER_POSITION", [ "6f=.lpm[*]", "PlayerStateData.FreighterMatrixPos[*]", "{0}.6f=.lpm[*]", "{0}.PlayerStateData.FreighterMatrixPos[*]" ] },
        { "PERSISTENT_PLAYER_BASE_ALL", [ "6f=.F?0[*]", "PlayerStateData.PersistentPlayerBases[*]", "{0}.6f=.F?0[*]", "{0}.PlayerStateData.PersistentPlayerBases[*]"] },
        { "PERSISTENT_PLAYER_BASE_ALL_TYPES", [ "6f=.F?0[*].peI.DPp", "PlayerStateData.PersistentPlayerBases[*].BaseType.PersistentBaseTypes", "{0}.6f=.F?0[*].peI.DPp", "{0}.PlayerStateData.PersistentPlayerBases[*].BaseType.PersistentBaseTypes"] },
        { "SETTLEMENT_ALL_OWNER_LID", [ "6f=.GQA[*].3?K.f5Q", "PlayerStateData.SettlementStatesV2[*].Owner.LID", "{0}.6f=.GQA[*].3?K.f5Q", "{0}.PlayerStateData.SettlementStatesV2[*].Owner.LID"] },
        { "TRANSFER_UID_SETTLEMENT", [ "6f=.GQA..[?(@.K7E == '{0}')]", "PlayerStateData.SettlementStatesV2..[?(@.UID == '{0}')]", "{0}.6f=.GQA..[?(@.K7E == '{{0}}')]", "{0}.PlayerStateData.SettlementStatesV2..[?(@.UID == '{{0}}')]"] },




        //{ "PERSISTENT_PLAYER_BASE_OWNERSHIP_EXPRESSION_PTK", [ "@.3?K.D6b == '' || @.3?K.D6b == '{0}'", "@.Owner.PTK == '' || @.Owner.PTK == '{0}'" ] }, // only with valid platform
        //{ "PERSISTENT_PLAYER_BASE_OWNERSHIP_EXPRESSION_LID", [ "@.3?K.f5Q != ''", "@.Owner.LID != ''" ] }, // only if set
        
        //{ "SETTLEMENT_OWNERSHIP", [ "6f=.GQA[?({{0}})].3?K.{0}", "PlayerStateData.SettlementStatesV2[?({{0}})].Owner.{0}", "vLc.6f=.GQA[?({{0}})].3?K.{0}", "BaseContext.PlayerStateData.SettlementStatesV2[?({{0}})].Owner.{0}" ] },
        //{ "SETTLEMENT_OWNERSHIP_EXPRESSION_PTK", [ "@.3?K.D6b == '{0}'", "@.Owner.PTK == '{0}'" ] }, // only with valid platform
        //{ "SETTLEMENT_OWNERSHIP_EXPRESSION_LID", [ "@.3?K.f5Q != ''", "@.Owner.LID != ''" ] }, // only if set
        //{ "SETTLEMENT_WITH_UID", [ "6f=.GQA..[?(@.K7E == '{0}')]", "PlayerStateData.SettlementStatesV2..[?(@.UID == '{0}')]", "vLc.6f=.GQA..[?(@.K7E == '{0}')]", "BaseContext.PlayerStateData.SettlementStatesV2..[?(@.UID == '{0}')]" ] },

        // intersection expressions
        { "INTERSECTION_PERSISTENT_PLAYER_BASE_OWNERSHIP", [ "6f=.F?0[?({{0}})].3?K.{0}", "PlayerStateData.PersistentPlayerBases[?({{0}})].Owner.{0}", "{0}.6f=.F?0[?({{{{0}}}})].3?K.{{0}}", "{0}.PlayerStateData.PersistentPlayerBases[?({{{{0}}}})].Owner.{{0}}" ] },
        { "INTERSECTION_PERSISTENT_PLAYER_BASE_OWNERSHIP_EXPRESSION_TYPE", [ "@.peI.DPp == '{0}' || @.peI.DPp == '{1}'", "@.BaseType.PersistentBaseTypes == '{0}' || @.BaseType.PersistentBaseTypes == '{1}'" ] },
        { "INTERSECTION_PERSISTENT_PLAYER_BASE_OWNERSHIP_EXPRESSION_UID", [ "@.3?K.K7E != ''", "@.Owner.UID != ''" ] },



        //{ "UID_DISCOVERY_VALUE", [ "fDu.ETO.OsQ.?fB[?({{0}})].ksu.{0}", "DiscoveryManagerData.DiscoveryData-v1.Store.Record[?({{0}})].OWS.{0}" ] },
        //{ "EXPRESSION_DISCOVERY_PTK", [ "@.ksu.D6b == '' || @.ksu.D6b == '{0}'", "@.OWS.PTK == '' || @.OWS.PTK == '{0}'" ] }, // only with valid platform
        //{ "EXPRESSION_DISCOVERY_EXPRESSION_LID", [ "@.ksu.f5Q != ''", "@.OWS.LID != ''" ] }, // only if set


        // relative from somewhere
        { "RELATIV_BASE_GALACTIC_ADDRESS", [ "oZw", "GalacticAddress" ] },
        { "RELATIV_BASE_NAME", [ "NKm", "Name" ] },
        { "RELATIV_BASE_OWNER", [ "3?K", "Owner" ] },
        { "RELATIV_BASE_POSITION_0", [ "wMC[0]", "Position[0]" ] },
        { "RELATIV_BASE_POSITION_1", [ "wMC[1]", "Position[1]" ] },
        { "RELATIV_BASE_POSITION_2", [ "wMC[2]", "Position[2]" ] },
        //{ "R_BASE_TYPE", [ "peI.DPp", "BaseType.PersistentBaseTypes" ] },
        { "RELATIV_OWNER_LID", [ "f5Q", "LID" ] },
        { "RELATIV_OWNER_PTK", [ "D6b", "PTK" ] },
        { "RELATIV_OWNER_UID", [ "K7E", "UID" ] },
        { "RELATIV_OWNER_USN", [ "V?:", "USN" ] },
        { "RELATIV_SONG_AUTHOR_ID", [ "m7b", "AuthorOnlineID" ] },
        { "RELATIV_SONG_AUTHOR_USERNAME", [ "4ha", "AuthorUsername" ] },
        { "RELATIV_SONG_AUTHOR_PLATFORM", [ "d2f", "AuthorPlatform" ] },


                        //{ "UID_DISCOVERY_VALUE", [ "fDu.ETO.OsQ.?fB[?({{0}})].ksu.{0}", "DiscoveryManagerData.DiscoveryData-v1.Store.Record[?({{0}})].OWS.{0}" ] },
        //{ "UID_DISCOVERY_EXPRESSION_PTK", [ "@.ksu.D6b == '' || @.ksu.D6b == '{0}'", "@.OWS.PTK == '' || @.OWS.PTK == '{0}'" ] }, // only with valid platform
        //{ "UID_DISCOVERY_EXPRESSION_LID", [ "@.ksu.f5Q != ''", "@.OWS.LID != ''" ] }, // only if set
        //{ "UID_DISCOVERY_TRANSFER", [ "fDu.ETO.OsQ.?fB..[?(@.K7E == '{0}')]", "DiscoveryManagerData.DiscoveryData-v1.Store.Record..[?(@.UID == '{0}')]" ] },














        // absolut from root
        { "ACTIVE_CONTEXT", [ "", "", "XTp", "ActiveContext" ] },
        { "DIFFICULTY_ACTIVE_SURVIVAL_BARS", [ "6f=.LyC.:fe.tEx.ZeS", "PlayerStateData.DifficultyState.Settings.ActiveSurvivalBars.ActiveSurvivalBarsDifficulty", "vLc.6f=.LyC.:fe.tEx.ZeS", "BaseContext.PlayerStateData.DifficultyState.Settings.ActiveSurvivalBars.ActiveSurvivalBarsDifficulty" ] },
        { "DIFFICULTY_HAZARD_DRAIN", [ "6f=.LyC.:fe.bGK.ORx", "PlayerStateData.DifficultyState.Settings.HazardDrain.HazardDrainDifficulty", "vLc.6f=.LyC.:fe.bGK.ORx", "BaseContext.PlayerStateData.DifficultyState.Settings.HazardDrain.HazardDrainDifficulty" ] },
        { "DIFFICULTY_ENERGY_DRAIN", [ "6f=.LyC.:fe.A:s.Dn>", "PlayerStateData.DifficultyState.Settings.EnergyDrain.EnergyDrainDifficulty", "vLc.6f=.LyC.:fe.A:s.Dn>", "BaseContext.PlayerStateData.DifficultyState.Settings.EnergyDrain.EnergyDrainDifficulty" ] },
        { "DIFFICULTY_SUBSTANCE_COLLECTION", [ "6f=.LyC.:fe.jH@.9JJ", "PlayerStateData.DifficultyState.Settings.SubstanceCollection.SubstanceCollectionDifficulty", "vLc.6f=.LyC.:fe.jH@.9JJ", "BaseContext.PlayerStateData.DifficultyState.Settings.SubstanceCollection.SubstanceCollectionDifficulty" ] },
        { "DIFFICULTY_SPRINTING_COST", [ "6f=.LyC.:fe.l29.LT:", "PlayerStateData.DifficultyState.Settings.SprintingCost.SprintingCostDifficulty", "vLc.6f=.LyC.:fe.l29.LT:", "BaseContext.PlayerStateData.DifficultyState.Settings.SprintingCost.SprintingCostDifficulty" ] },
        { "DIFFICULTY_SCANNER_RECHARGE", [ "6f=.LyC.:fe.Lf?.gFS", "PlayerStateData.DifficultyState.Settings.ScannerRecharge.ScannerRechargeDifficulty", "vLc.6f=.LyC.:fe.Lf?.gFS", "BaseContext.PlayerStateData.DifficultyState.Settings.ScannerRecharge.ScannerRechargeDifficulty" ] },
        { "DIFFICULTY_DAMAGE_RECEIVED", [ "6f=.LyC.:fe.hXp.cYk", "PlayerStateData.DifficultyState.Settings.DamageReceived.DamageReceivedDifficulty", "vLc.6f=.LyC.:fe.hXp.cYk", "BaseContext.PlayerStateData.DifficultyState.Settings.DamageReceived.DamageReceivedDifficulty" ] },
        { "DIFFICULTY_BREAK_TECH_ON_DAMAGE", [ "6f=.LyC.:fe.gd>.ef4", "PlayerStateData.DifficultyState.Settings.BreakTechOnDamage.BreakTechOnDamageProbability", "vLc.6f=.LyC.:fe.gd>.ef4", "BaseContext.PlayerStateData.DifficultyState.Settings.BreakTechOnDamage.BreakTechOnDamageProbability" ] },
        { "DIFFICULTY_DEATH_CONSEQUENCES", [ "6f=.LyC.:fe.n7p.q2@", "PlayerStateData.DifficultyState.Settings.DeathConsequences.DeathConsequencesDifficulty", "vLc.6f=.LyC.:fe.n7p.q2@", "BaseContext.PlayerStateData.DifficultyState.Settings.DeathConsequences.DeathConsequencesDifficulty" ] },
        { "DIFFICULTY_CHARGING_REQUIREMENTS", [ "6f=.LyC.:fe.nhq.428", "PlayerStateData.DifficultyState.Settings.ChargingRequirements.ChargingRequirementsDifficulty", "vLc.6f=.LyC.:fe.nhq.428", "BaseContext.PlayerStateData.DifficultyState.Settings.ChargingRequirements.ChargingRequirementsDifficulty" ] },
        { "DIFFICULTY_FUEL_USE", [ "6f=.LyC.:fe.jnM.Eg1", "PlayerStateData.DifficultyState.Settings.FuelUse.FuelUseDifficulty", "vLc.6f=.LyC.:fe.jnM.Eg1", "BaseContext.PlayerStateData.DifficultyState.Settings.FuelUse.FuelUseDifficulty" ] },
        { "DIFFICULTY_LAUNCH_FUEL_COST", [ "6f=.LyC.:fe.A9D.iqY", "PlayerStateData.DifficultyState.Settings.LaunchFuelCost.LaunchFuelCostDifficulty", "vLc.6f=.LyC.:fe.A9D.iqY", "BaseContext.PlayerStateData.DifficultyState.Settings.LaunchFuelCost.LaunchFuelCostDifficulty" ] },
        { "DIFFICULTY_CRAFTING_IS_FREE", [ "6f=.LyC.:fe.?Dt", "PlayerStateData.DifficultyState.Settings.CraftingIsFree", "vLc.6f=.LyC.:fe.?Dt", "BaseContext.PlayerStateData.DifficultyState.Settings.CraftingIsFree" ] },
        { "DIFFICULTY_CURRENCY_COST", [ "6f=.LyC.:fe.tsk.Ubk", "PlayerStateData.DifficultyState.Settings.CurrencyCost.CurrencyCostDifficulty", "vLc.6f=.LyC.:fe.tsk.Ubk", "BaseContext.PlayerStateData.DifficultyState.Settings.CurrencyCost.CurrencyCostDifficulty" ] },
        { "DIFFICULTY_ITEM_SHOP_AVAILABILITY", [ "6f=.LyC.:fe.FB5.TYf", "PlayerStateData.DifficultyState.Settings.ItemShopAvailability.ItemShopAvailabilityDifficulty", "vLc.6f=.LyC.:fe.FB5.TYf", "BaseContext.PlayerStateData.DifficultyState.Settings.ItemShopAvailability.ItemShopAvailabilityDifficulty" ] },
        { "DIFFICULTY_INVENTORY_STACK_LIMITS", [ "6f=.LyC.:fe.kZ5.?SS", "PlayerStateData.DifficultyState.Settings.InventoryStackLimits.InventoryStackLimitsDifficulty", "vLc.6f=.LyC.:fe.kZ5.?SS", "BaseContext.PlayerStateData.DifficultyState.Settings.InventoryStackLimits.InventoryStackLimitsDifficulty" ] },
        { "DIFFICULTY_DAMAGE_GIVEN", [ "6f=.LyC.:fe.PYQ.mum", "PlayerStateData.DifficultyState.Settings.DamageGiven.DamageGivenDifficulty", "vLc.6f=.LyC.:fe.PYQ.mum", "BaseContext.PlayerStateData.DifficultyState.Settings.DamageGiven.DamageGivenDifficulty" ] },
        { "DIFFICULTY_GROUND_COMBAT_TIMERS", [ "6f=.LyC.:fe.jGh.ZbV", "PlayerStateData.DifficultyState.Settings.GroundCombatTimers.CombatTimerDifficultyOption", "vLc.6f=.LyC.:fe.jGh.ZbV", "BaseContext.PlayerStateData.DifficultyState.Settings.GroundCombatTimers.CombatTimerDifficultyOption" ] },
        { "DIFFICULTY_SPACE_COMBAT_TIMERS", [ "6f=.LyC.:fe.Od7.ZbV", "PlayerStateData.DifficultyState.Settings.SpaceCombatTimers.CombatTimerDifficultyOption", "vLc.6f=.LyC.:fe.Od7.ZbV", "BaseContext.PlayerStateData.DifficultyState.Settings.SpaceCombatTimers.CombatTimerDifficultyOption" ] },
        { "DIFFICULTY_CREATURE_HOSTILITY", [ "6f=.LyC.:fe.BbG.1c;", "PlayerStateData.DifficultyState.Settings.CreatureHostility.CreatureHostilityDifficulty", "vLc.6f=.LyC.:fe.BbG.1c;", "BaseContext.PlayerStateData.DifficultyState.Settings.CreatureHostility.CreatureHostilityDifficulty" ] },
        { "DIFFICULTY_INVENTORIES_ALWAYS_IN_RANGE", [ "6f=.LyC.:fe.pS0", "PlayerStateData.DifficultyState.Settings.InventoriesAlwaysInRange", "vLc.6f=.LyC.:fe.pS0", "BaseContext.PlayerStateData.DifficultyState.Settings.InventoriesAlwaysInRange" ] },
        { "DIFFICULTY_WARP_DRIVE_REQUIREMENTS", [ "6f=.LyC.:fe.aw9", "PlayerStateData.DifficultyState.Settings.WarpDriveRequirements", "vLc.6f=.LyC.:fe.aw9", "BaseContext.PlayerStateData.DifficultyState.Settings.WarpDriveRequirements" ] },
        { "DIFFICULTY_BASE_AUTO_POWER", [ "6f=.LyC.:fe.uo4", "PlayerStateData.DifficultyState.Settings.BaseAutoPower", "vLc.6f=.LyC.:fe.uo4", "BaseContext.PlayerStateData.DifficultyState.Settings.BaseAutoPower" ] },
        { "DIFFICULTY_REPUTATION_GAIN", [ "6f=.LyC.:fe.vo>.S@3", "PlayerStateData.DifficultyState.Settings.ReputationGain.ReputationGainDifficulty", "vLc.6f=.LyC.:fe.vo>.S@3", "BaseContext.PlayerStateData.DifficultyState.Settings.ReputationGain.ReputationGainDifficulty" ] },
        { "GAME_MODE_MAIN", [ "", "", "vLc.idA", "BaseContext.GameMode" ] },
        { "GAME_MODE_SEASON", [ "", "", "2YS.idA", "ExpeditionContext.GameMode" ] },
        { "GAME_VERSION_230", [ "6f=..Xf4", "PlayerStateData..CurrentPos" ] },
        { "GAME_VERSION_260", [ "rnc.Ovv", "SpawnStateData.AbandonedFreighterPositionInSystem" ] },
        { "GAME_VERSION_300", [ "6f=.ux@", "PlayerStateData.PreviousUniverseAddress" ] },
        { "GAME_VERSION_320", [ "6f=.Mcl", "PlayerStateData.Pets" ] },
        { "GAME_VERSION_340", [ "6f=.Rol.Whh", "PlayerStateData.SeasonData.MainMissionTitle" ] },
        { "GAME_VERSION_350", [ "6f=.8iI", "PlayerStateData.ByteBeatLibrary" ] },
        { "GAME_VERSION_351", [ "6f=.8iI.ON4..m7b", "PlayerStateData.ByteBeatLibrary.MySongs..AuthorOnlineID" ] },
        { "GAME_VERSION_370", [ "6f=.Rol.qs?",  "PlayerStateData.SeasonData.SandwormOverrides" ] },
        { "GAME_VERSION_384", [ "6f=.Agx",  "PlayerStateData.VehicleAIControlEnabled" ] },
        { "GAME_VERSION_390", [ "6f=.Sd6", "PlayerStateData.NextLoadSpawnsWithFreshStart" ] },
        { "GAME_VERSION_425", [ "6f=.Rol.XEk", "CommonStateData.SeasonData.SeasonStartMusicOverride" ] },
        { "PLATFORM", [ "8>q", "Platform" ] },
        { "SAVE_NAME", [ "6f=.Pk4", "PlayerStateData.SaveName", "<h0.Pk4", "CommonStateData.SaveName" ] },
        { "SAVE_SUMMARY", [ "6f=.n:R", "PlayerStateData.SaveSummary", "vLc.6f=.n:R", "BaseContext.PlayerStateData.SaveSummary" ] }, // TODO replace
        { "SAVE_SUMMARY_MAIN", [ "6f=.n:R", "PlayerStateData.SaveSummary", "vLc.6f=.n:R", "BaseContext.PlayerStateData.SaveSummary" ] },
        { "SAVE_SUMMARY_SEASON", [ "6f=.n:R", "PlayerStateData.SaveSummary", "2YS.6f=.n:R", "ExpeditionContext.PlayerStateData.SaveSummary" ] },
        { "SEASON_ID", [ "6f=.Rol.gou", "PlayerStateData.SeasonData.SeasonId", "<h0.Rol.gou", "CommonStateData.SeasonData.SeasonId" ] },
        { "TOTAL_PLAY_TIME", [ "6f=.Lg8", "PlayerStateData.TotalPlayTime", "<h0.Lg8", "CommonStateData.TotalPlayTime" ] },
        { "VERSION", [ "F2P", "Version" ] },

        { "BASE_CONTEXT", [ "", "", "vLc", "BaseContext" ] },
        { "EXPEDITION_CONTEXT", [ "", "", "2YS", "ExpeditionContext" ] },

        // relative from somewhere
        { "BASE_GALACTIC_ADDRESS", [ "oZw", "GalacticAddress" ] },
        { "BASE_NAME", [ "NKm", "Name" ] },
        { "BASE_OWNER", [ "3?K", "Owner" ] },
        { "BASE_POSITION_0", [ "wMC[0]", "Position[0]" ] },
        { "BASE_POSITION_1", [ "wMC[1]", "Position[1]" ] },
        { "BASE_POSITION_2", [ "wMC[2]", "Position[2]" ] },
        { "BASE_TYPE", [ "peI.DPp", "BaseType.PersistentBaseTypes" ] },
        { "OWNER_LID", [ "f5Q", "LID" ] },
        { "OWNER_PTK", [ "D6b", "PTK" ] },
        { "OWNER_UID", [ "K7E", "UID" ] },
        { "OWNER_USN", [ "V?:", "USN" ] },
        { "SONG_AUTHOR_ID", [ "m7b", "AuthorOnlineID" ] },
        { "SONG_AUTHOR_USERNAME", [ "4ha", "AuthorUsername" ] },
        { "SONG_AUTHOR_PLATFORM", [ "d2f", "AuthorPlatform" ] },

        // select multiple
        { "DISCOVERY_DATA_OWNERSHIP", [ "fDu.ETO.OsQ.?fB[?({{0}})].ksu.{0}", "DiscoveryManagerData.DiscoveryData-v1.Store.Record[?({{0}})].OWS.{0}" ] },
        { "DISCOVERY_DATA_OWNERSHIP_EXPRESSION_PTK", [ "@.ksu.D6b == '' || @.ksu.D6b == '{0}'", "@.OWS.PTK == '' || @.OWS.PTK == '{0}'" ] }, // only with valid platform
        { "DISCOVERY_DATA_OWNERSHIP_EXPRESSION_LID", [ "@.ksu.f5Q != ''", "@.OWS.LID != ''" ] }, // only if set
        { "DISCOVERY_DATA_WITH_UID", [ "fDu.ETO.OsQ.?fB..[?(@.K7E == '{0}')]", "DiscoveryManagerData.DiscoveryData-v1.Store.Record..[?(@.UID == '{0}')]" ] },
        { "FREIGHTER_POSITION", [ "6f=.lpm[*]", "PlayerStateData.FreighterMatrixPos[*]", "vLc.6f=.lpm[*]", "BaseContext.PlayerStateData.FreighterMatrixPos[*]" ] },
        { "MY_SONGS_WITH_UID", [ "6f=.8iI.ON4[?(@.m7b == '{0}')]", "PlayerStateData.ByteBeatLibrary.MySongs[?(@.AuthorOnlineID == '{0}')]", "<h0.8iI.ON4[?(@.m7b == '{0}')]", "CommonStateData.ByteBeatLibrary.MySongs[?(@.AuthorOnlineID == '{0}')]" ] },
        { "PERSISTENT_PLAYER_BASE_ALL", [ "6f=.F?0[*]", "PlayerStateData.PersistentPlayerBases[*]", "vLc.6f=.F?0[*]", "BaseContext.PlayerStateData.PersistentPlayerBases[*]" ] },
        { "PERSISTENT_PLAYER_BASE_ALL_TYPES", [ "6f=.F?0[*].peI.DPp", "PlayerStateData.PersistentPlayerBases[*].BaseType.PersistentBaseTypes", "vLc.6f=.F?0[*].peI.DPp", "BaseContext.PlayerStateData.PersistentPlayerBases[*].BaseType.PersistentBaseTypes" ] },
        { "PERSISTENT_PLAYER_BASE_OWNERSHIP", [ "6f=.F?0[?({{0}})].3?K.{0}", "PlayerStateData.PersistentPlayerBases[?({{0}})].Owner.{0}", "vLc.6f=.F?0[?({{0}})].3?K.{0}", "BaseContext.PlayerStateData.PersistentPlayerBases[?({{0}})].Owner.{0}" ] },
        { "PERSISTENT_PLAYER_BASE_OWNERSHIP_EXPRESSION_TYPE", [ "@.peI.DPp == '{0}' || @.peI.DPp == '{1}'", "@.BaseType.PersistentBaseTypes == '{0}' || @.BaseType.PersistentBaseTypes == '{1}'" ] }, // only with own base
        { "PERSISTENT_PLAYER_BASE_OWNERSHIP_EXPRESSION_PTK", [ "@.3?K.D6b == '' || @.3?K.D6b == '{0}'", "@.Owner.PTK == '' || @.Owner.PTK == '{0}'" ] }, // only with valid platform
        { "PERSISTENT_PLAYER_BASE_OWNERSHIP_EXPRESSION_LID", [ "@.3?K.f5Q != ''", "@.Owner.LID != ''" ] }, // only if set
        { "SETTLEMENT_ALL_OWNER_LID", [ "6f=.GQA[*].3?K.f5Q", "PlayerStateData.SettlementStatesV2[*].Owner.LID", "vLc.6f=.GQA[*].3?K.f5Q", "BaseContext.PlayerStateData.SettlementStatesV2[*].Owner.LID" ] },
        { "SETTLEMENT_OWNERSHIP", [ "6f=.GQA[?({{0}})].3?K.{0}", "PlayerStateData.SettlementStatesV2[?({{0}})].Owner.{0}", "vLc.6f=.GQA[?({{0}})].3?K.{0}", "BaseContext.PlayerStateData.SettlementStatesV2[?({{0}})].Owner.{0}" ] },
        { "SETTLEMENT_OWNERSHIP_EXPRESSION_PTK", [ "@.3?K.D6b == '{0}'", "@.Owner.PTK == '{0}'" ] }, // only with valid platform
        { "SETTLEMENT_OWNERSHIP_EXPRESSION_LID", [ "@.3?K.f5Q != ''", "@.Owner.LID != ''" ] }, // only if set
        { "SETTLEMENT_WITH_UID", [ "6f=.GQA..[?(@.K7E == '{0}')]", "PlayerStateData.SettlementStatesV2..[?(@.UID == '{0}')]", "vLc.6f=.GQA..[?(@.K7E == '{0}')]", "BaseContext.PlayerStateData.SettlementStatesV2..[?(@.UID == '{0}')]" ] },






        // template
        { "", [ "", "", "", "" ] },
        { "", [ "", "", "", "" ] },
        { "", [ "", "", "", "" ] },
    };
    internal static readonly string[] JSONPATH_CONTEXT_OBFUSCATED = ["", "2YS", "vLc", "", ""]; // SaveContextQueryEnum
    internal static readonly string[] JSONPATH_CONTEXT_PLAINTEXT = ["", "ExpeditionContext", "BaseContext", "", ""];

    internal const int OFFSET_GAMEMODE = 512;
    internal const int OFFSET_INDEX = 2;
    internal const int OFFSET_SEASON = 128;

    internal const int OFFSET_GAMEMODE_SEASONAL = OFFSET_GAMEMODE * (int)(PresetGameModeEnum.Seasonal);
    internal const int OFFSET_MULTIPLICATION_GAMEMODE_SEASON = OFFSET_GAMEMODE * OFFSET_SEASON;

    internal const uint SAVE_FORMAT_1 = 0x7D0; // 2000 (1.0) // not used but for completeness
    internal const uint SAVE_FORMAT_2 = 0x7D1; // 2001 (1.1)
    internal const uint SAVE_FORMAT_3 = 0x7D2; // 2002 (3.6)

    internal const int SAVE_RENAMING_LENGTH_MANIFEST = 0x80; // 128
    internal const int SAVE_RENAMING_LENGTH_INGAME = 0x2A; // 42

    internal const uint SAVE_STREAMING_HEADER = 0xFEEDA1E5; // 4276986341
    internal const int SAVE_STREAMING_HEADER_TOTAL_LENGTH = 0x10; // 16
    internal const int SAVE_STREAMING_CHUNK_MAX_LENGTH = 0x80000; // 524288

    internal const int THRESHOLD_GAMEMODE_NORMAL = THRESHOLD_VANILLA + ((int)(PresetGameModeEnum.Normal) * OFFSET_GAMEMODE);
    internal const int THRESHOLD_GAMEMODE_CREATIVE = THRESHOLD_VANILLA + ((int)(PresetGameModeEnum.Creative) * OFFSET_GAMEMODE);
    internal const int THRESHOLD_GAMEMODE_SURVIVAL = THRESHOLD_VANILLA + ((int)(PresetGameModeEnum.Survival) * OFFSET_GAMEMODE);
    internal const int THRESHOLD_GAMEMODE_PERMADEATH = THRESHOLD_VANILLA + ((int)(PresetGameModeEnum.Permadeath) * OFFSET_GAMEMODE);
    internal const int THRESHOLD_GAMEMODE_SEASONAL = THRESHOLD_VANILLA + ((int)(PresetGameModeEnum.Seasonal) * OFFSET_GAMEMODE);
    internal const int THRESHOLD_VANILLA = 4098;
    internal const int THRESHOLD_VANILLA_GAMEMODE = THRESHOLD_VANILLA + OFFSET_GAMEMODE;
    internal const int THRESHOLD_WAYPOINT = 4140;
    internal const int THRESHOLD_WAYPOINT_GAMEMODE = THRESHOLD_WAYPOINT + OFFSET_GAMEMODE;
}
