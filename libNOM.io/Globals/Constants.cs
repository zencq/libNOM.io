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
        // Absolut from root, common
        { "A_ACTIVE_CONTEXT", [ "", "", "XTp", "ActiveContext" ] },
        { "A_GAME_VERSION_230", new[] { "6f=..Xf4", "PlayerStateData..CurrentPos" } },
        { "A_GAME_VERSION_260", new[] { "rnc.Ovv", "SpawnStateData.AbandonedFreighterPositionInSystem" } },
        { "A_GAME_VERSION_300", new[] { "6f=.ux@", "PlayerStateData.PreviousUniverseAddress" } },
        { "A_GAME_VERSION_320", new[] { "6f=.Mcl", "PlayerStateData.Pets" } },
        { "A_GAME_VERSION_340", new[] { "6f=.Rol.Whh", "PlayerStateData.SeasonData.MainMissionTitle" } },
        { "A_GAME_VERSION_350", new[] { "6f=.8iI", "PlayerStateData.ByteBeatLibrary" } },
        { "A_GAME_VERSION_351", new[] { "6f=.8iI.ON4..m7b", "PlayerStateData.ByteBeatLibrary.MySongs..AuthorOnlineID" } },
        { "A_GAME_VERSION_370", new[] { "6f=.Rol.qs?",  "PlayerStateData.SeasonData.SandwormOverrides" } },
        { "A_GAME_VERSION_384", new[] { "6f=.Agx",  "PlayerStateData.VehicleAIControlEnabled" } },
        { "A_GAME_VERSION_390", new[] { "6f=.Sd6", "PlayerStateData.NextLoadSpawnsWithFreshStart" } },
        { "A_GAME_VERSION_425", new[] { "6f=.Rol.XEk", "PlayerStateData.SeasonData.SeasonStartMusicOverride" } },
        { "A_PLATFORM", new[] { "8>q", "Platform" } },
        { "A_SAVE_NAME", new[] { "6f=.Pk4", "PlayerStateData.SaveName", "<h0.Pk4", "CommonStateData.SaveName" } },
        { "A_SEASON_ID", new[] { "6f=.Rol.gou", "PlayerStateData.SeasonData.SeasonId", "<h0.Rol.gou", "CommonStateData.SeasonData.SeasonId" } },
        { "A_TOTAL_PLAY_TIME", new[] { "6f=.Lg8", "PlayerStateData.TotalPlayTime", "<h0.Lg8", "CommonStateData.TotalPlayTime" } },
        { "A_UID_BYTEBEAT_TRANSFER", new[] { "6f=.8iI.ON4[?(@.m7b == '{0}')]", "PlayerStateData.ByteBeatLibrary.MySongs[?(@.AuthorOnlineID == '{0}')]", "<h0.8iI.ON4[?(@.m7b == '{0}')]", "CommonStateData.ByteBeatLibrary.MySongs[?(@.AuthorOnlineID == '{0}')]" } },
        { "A_UID_DISCOVERY_VALUE", new[] { "fDu.ETO.OsQ.?fB[?({{0}})].ksu.{0}", "DiscoveryManagerData.DiscoveryData-v1.Store.Record[?({{0}})].OWS.{0}" } },
        { "A_UID_DISCOVERY_EXPRESSION_PTK", new[] { "@.ksu.D6b == '' || @.ksu.D6b == '{0}'", "@.OWS.PTK == '' || @.OWS.PTK == '{0}'" } }, // only with valid platform
        { "A_UID_DISCOVERY_EXPRESSION_LID", new[] { "@.ksu.f5Q != ''", "@.OWS.LID != ''" } }, // only if set
        { "A_UID_DISCOVERY_TRANSFER", new[] { "fDu.ETO.OsQ.?fB..[?(@.K7E == '{0}')]", "DiscoveryManagerData.DiscoveryData-v1.Store.Record..[?(@.UID == '{0}')]" } },
        { "A_VERSION", new[] { "F2P", "Version" } },

        // absolut from root, Context depended
        { "C_DIFFICULTY_ACTIVE_SURVIVAL_BARS", new[] { "6f=.LyC.:fe.tEx.ZeS", "PlayerStateData.DifficultyState.Settings.ActiveSurvivalBars.ActiveSurvivalBarsDifficulty", "{0}.6f=.LyC.:fe.tEx.ZeS", "{0}.PlayerStateData.DifficultyState.Settings.ActiveSurvivalBars.ActiveSurvivalBarsDifficulty" } },
        { "C_DIFFICULTY_HAZARD_DRAIN", new[] { "6f=.LyC.:fe.bGK.ORx", "PlayerStateData.DifficultyState.Settings.HazardDrain.HazardDrainDifficulty", "{0}.6f=.LyC.:fe.bGK.ORx", "{0}.PlayerStateData.DifficultyState.Settings.HazardDrain.HazardDrainDifficulty" } },
        { "C_DIFFICULTY_ENERGY_DRAIN", new[] { "6f=.LyC.:fe.A:s.Dn>", "PlayerStateData.DifficultyState.Settings.EnergyDrain.EnergyDrainDifficulty", "{0}.6f=.LyC.:fe.A:s.Dn>", "{0}.PlayerStateData.DifficultyState.Settings.EnergyDrain.EnergyDrainDifficulty" } },
        { "C_DIFFICULTY_SUBSTANCE_COLLECTION", new[] { "6f=.LyC.:fe.jH@.9JJ", "PlayerStateData.DifficultyState.Settings.SubstanceCollection.SubstanceCollectionDifficulty", "{0}.6f=.LyC.:fe.jH@.9JJ", "{0}.PlayerStateData.DifficultyState.Settings.SubstanceCollection.SubstanceCollectionDifficulty" } },
        { "C_DIFFICULTY_SPRINTING_COST", new[] { "6f=.LyC.:fe.l29.LT:", "PlayerStateData.DifficultyState.Settings.SprintingCost.SprintingCostDifficulty", "{0}.6f=.LyC.:fe.l29.LT:", "{0}.PlayerStateData.DifficultyState.Settings.SprintingCost.SprintingCostDifficulty" } },
        { "C_DIFFICULTY_SCANNER_RECHARGE", new[] { "6f=.LyC.:fe.Lf?.gFS", "PlayerStateData.DifficultyState.Settings.ScannerRecharge.ScannerRechargeDifficulty", "{0}.6f=.LyC.:fe.Lf?.gFS", "{0}.PlayerStateData.DifficultyState.Settings.ScannerRecharge.ScannerRechargeDifficulty" } },
        { "C_DIFFICULTY_DAMAGE_RECEIVED", new[] { "6f=.LyC.:fe.hXp.cYk", "PlayerStateData.DifficultyState.Settings.DamageReceived.DamageReceivedDifficulty", "{0}.6f=.LyC.:fe.hXp.cYk", "{0}.PlayerStateData.DifficultyState.Settings.DamageReceived.DamageReceivedDifficulty" } },
        { "C_DIFFICULTY_BREAK_TECH_ON_DAMAGE", new[] { "6f=.LyC.:fe.gd>.ef4", "PlayerStateData.DifficultyState.Settings.BreakTechOnDamage.BreakTechOnDamageProbability", "{0}.6f=.LyC.:fe.gd>.ef4", "{0}.PlayerStateData.DifficultyState.Settings.BreakTechOnDamage.BreakTechOnDamageProbability" } },
        { "C_DIFFICULTY_DEATH_CONSEQUENCES", new[] { "6f=.LyC.:fe.n7p.q2@", "PlayerStateData.DifficultyState.Settings.DeathConsequences.DeathConsequencesDifficulty", "{0}.6f=.LyC.:fe.n7p.q2@", "{0}.PlayerStateData.DifficultyState.Settings.DeathConsequences.DeathConsequencesDifficulty" } },
        { "C_DIFFICULTY_CHARGING_REQUIREMENTS", new[] { "6f=.LyC.:fe.nhq.428", "PlayerStateData.DifficultyState.Settings.ChargingRequirements.ChargingRequirementsDifficulty", "{0}.6f=.LyC.:fe.nhq.428", "{0}.PlayerStateData.DifficultyState.Settings.ChargingRequirements.ChargingRequirementsDifficulty" } },
        { "C_DIFFICULTY_FUEL_USE", new[] { "6f=.LyC.:fe.jnM.Eg1", "PlayerStateData.DifficultyState.Settings.FuelUse.FuelUseDifficulty", "{0}.6f=.LyC.:fe.jnM.Eg1", "{0}.PlayerStateData.DifficultyState.Settings.FuelUse.FuelUseDifficulty" } },
        { "C_DIFFICULTY_LAUNCH_FUEL_COST", new[] { "6f=.LyC.:fe.A9D.iqY", "PlayerStateData.DifficultyState.Settings.LaunchFuelCost.LaunchFuelCostDifficulty", "{0}.6f=.LyC.:fe.A9D.iqY", "{0}.PlayerStateData.DifficultyState.Settings.LaunchFuelCost.LaunchFuelCostDifficulty" } },
        { "C_DIFFICULTY_CRAFTING_IS_FREE", new[] { "6f=.LyC.:fe.?Dt", "PlayerStateData.DifficultyState.Settings.CraftingIsFree", "{0}.6f=.LyC.:fe.?Dt", "{0}.PlayerStateData.DifficultyState.Settings.CraftingIsFree" } },
        { "C_DIFFICULTY_CURRENCY_COST", new[] { "6f=.LyC.:fe.tsk.Ubk", "PlayerStateData.DifficultyState.Settings.CurrencyCost.CurrencyCostDifficulty", "{0}.6f=.LyC.:fe.tsk.Ubk", "{0}.PlayerStateData.DifficultyState.Settings.CurrencyCost.CurrencyCostDifficulty" } },
        { "C_DIFFICULTY_ITEM_SHOP_AVAILABILITY", new[] { "6f=.LyC.:fe.FB5.TYf", "PlayerStateData.DifficultyState.Settings.ItemShopAvailability.ItemShopAvailabilityDifficulty", "{0}.6f=.LyC.:fe.FB5.TYf", "{0}.PlayerStateData.DifficultyState.Settings.ItemShopAvailability.ItemShopAvailabilityDifficulty" } },
        { "C_DIFFICULTY_INVENTORY_STACK_LIMITS", new[] { "6f=.LyC.:fe.kZ5.?SS", "PlayerStateData.DifficultyState.Settings.InventoryStackLimits.InventoryStackLimitsDifficulty", "{0}.6f=.LyC.:fe.kZ5.?SS", "{0}.PlayerStateData.DifficultyState.Settings.InventoryStackLimits.InventoryStackLimitsDifficulty" } },
        { "C_DIFFICULTY_DAMAGE_GIVEN", new[] { "6f=.LyC.:fe.PYQ.mum", "PlayerStateData.DifficultyState.Settings.DamageGiven.DamageGivenDifficulty", "{0}.6f=.LyC.:fe.PYQ.mum", "{0}.PlayerStateData.DifficultyState.Settings.DamageGiven.DamageGivenDifficulty" } },
        { "C_DIFFICULTY_GROUND_COMBAT_TIMERS", new[] { "6f=.LyC.:fe.jGh.ZbV", "PlayerStateData.DifficultyState.Settings.GroundCombatTimers.CombatTimerDifficultyOption", "{0}.6f=.LyC.:fe.jGh.ZbV", "{0}.PlayerStateData.DifficultyState.Settings.GroundCombatTimers.CombatTimerDifficultyOption" } },
        { "C_DIFFICULTY_SPACE_COMBAT_TIMERS", new[] { "6f=.LyC.:fe.Od7.ZbV", "PlayerStateData.DifficultyState.Settings.SpaceCombatTimers.CombatTimerDifficultyOption", "{0}.6f=.LyC.:fe.Od7.ZbV", "{0}.PlayerStateData.DifficultyState.Settings.SpaceCombatTimers.CombatTimerDifficultyOption" } },
        { "C_DIFFICULTY_CREATURE_HOSTILITY", new[] { "6f=.LyC.:fe.BbG.1c;", "PlayerStateData.DifficultyState.Settings.CreatureHostility.CreatureHostilityDifficulty", "{0}.6f=.LyC.:fe.BbG.1c;", "{0}.PlayerStateData.DifficultyState.Settings.CreatureHostility.CreatureHostilityDifficulty" } },
        { "C_DIFFICULTY_INVENTORIES_ALWAYS_IN_RANGE", new[] { "6f=.LyC.:fe.pS0", "PlayerStateData.DifficultyState.Settings.InventoriesAlwaysInRange", "{0}.6f=.LyC.:fe.pS0", "{0}.PlayerStateData.DifficultyState.Settings.InventoriesAlwaysInRange" } },
        { "C_DIFFICULTY_WARP_DRIVE_REQUIREMENTS", new[] { "6f=.LyC.:fe.aw9", "PlayerStateData.DifficultyState.Settings.WarpDriveRequirements", "{0}.6f=.LyC.:fe.aw9", "{0}.PlayerStateData.DifficultyState.Settings.WarpDriveRequirements" } },
        { "C_DIFFICULTY_BASE_AUTO_POWER", new[] { "6f=.LyC.:fe.uo4", "PlayerStateData.DifficultyState.Settings.BaseAutoPower", "{0}.6f=.LyC.:fe.uo4", "{0}.PlayerStateData.DifficultyState.Settings.BaseAutoPower" } },
        { "C_DIFFICULTY_REPUTATION_GAIN", new[] { "6f=.LyC.:fe.vo>.S@3", "PlayerStateData.DifficultyState.Settings.ReputationGain.ReputationGainDifficulty", "{0}.6f=.LyC.:fe.vo>.S@3", "{0}.PlayerStateData.DifficultyState.Settings.ReputationGain.ReputationGainDifficulty" } },
        { "C_GAME_MODE", new[] { "", "", "{0}.idA", "{0}.GameMode" } },
        { "C_FREIGHTER_POSITION", new[] { "6f=.lpm[*]", "PlayerStateData.FreighterMatrixPos[*]", "{0}.6f=.lpm[*]", "{0}.PlayerStateData.FreighterMatrixPos[*]" } },

        { "M_PERSISTENT_PLAYER_BASE_ALL", new[] { "6f=.F?0[*]", "PlayerStateData.PersistentPlayerBases[*]", "vLc.6f=.F?0[*]", "BaseContext.PlayerStateData.PersistentPlayerBases[*]" } },
        { "M_PERSISTENT_PLAYER_BASE_ALL_TYPES", new[] { "6f=.F?0[*].peI.DPp", "PlayerStateData.PersistentPlayerBases[*].BaseType.PersistentBaseTypes", "vLc.6f=.F?0[*].peI.DPp", "BaseContext.PlayerStateData.PersistentPlayerBases[*].BaseType.PersistentBaseTypes" } },
        { "M_PERSISTENT_PLAYER_BASE_OWNERSHIP", new[] { "6f=.F?0[?({{0}})].3?K.{0}", "PlayerStateData.PersistentPlayerBases[?({{0}})].Owner.{0}", "vLc.6f=.F?0[?({{0}})].3?K.{0}", "BaseContext.PlayerStateData.PersistentPlayerBases[?({{0}})].Owner.{0}" } },
        { "M_PERSISTENT_PLAYER_BASE_OWNERSHIP_EXPRESSION_TYPE", new[] { "@.peI.DPp == '{0}' || @.peI.DPp == '{1}'", "@.BaseType.PersistentBaseTypes == '{0}' || @.BaseType.PersistentBaseTypes == '{1}'" } }, // only with own base
        { "M_PERSISTENT_PLAYER_BASE_OWNERSHIP_EXPRESSION_PTK", new[] { "@.3?K.D6b == '' || @.3?K.D6b == '{0}'", "@.Owner.PTK == '' || @.Owner.PTK == '{0}'" } }, // only with valid platform
        { "M_PERSISTENT_PLAYER_BASE_OWNERSHIP_EXPRESSION_LID", new[] { "@.3?K.f5Q != ''", "@.Owner.LID != ''" } }, // only if set
                { "M_SETTLEMENT_ALL_OWNER_LID", new[] { "6f=.GQA[*].3?K.f5Q", "PlayerStateData.SettlementStatesV2[*].Owner.LID", "vLc.6f=.GQA[*].3?K.f5Q", "BaseContext.PlayerStateData.SettlementStatesV2[*].Owner.LID" } },
        { "M_SETTLEMENT_OWNERSHIP", new[] { "6f=.GQA[?({{0}})].3?K.{0}", "PlayerStateData.SettlementStatesV2[?({{0}})].Owner.{0}", "vLc.6f=.GQA[?({{0}})].3?K.{0}", "BaseContext.PlayerStateData.SettlementStatesV2[?({{0}})].Owner.{0}" } },
        { "M_SETTLEMENT_OWNERSHIP_EXPRESSION_PTK", new[] { "@.3?K.D6b == '{0}'", "@.Owner.PTK == '{0}'" } }, // only with valid platform
        { "M_SETTLEMENT_OWNERSHIP_EXPRESSION_LID", new[] { "@.3?K.f5Q != ''", "@.Owner.LID != ''" } }, // only if set
        { "M_SETTLEMENT_WITH_UID", new[] { "6f=.GQA..[?(@.K7E == '{0}')]", "PlayerStateData.SettlementStatesV2..[?(@.UID == '{0}')]", "vLc.6f=.GQA..[?(@.K7E == '{0}')]", "BaseContext.PlayerStateData.SettlementStatesV2..[?(@.UID == '{0}')]" } },

        // Relative from somewhere
        { "R_BASE_GALACTIC_ADDRESS", new[]{ "oZw", "GalacticAddress" } },
        { "R_BASE_NAME", new[] { "NKm", "Name" } },
        { "R_BASE_OWNER", new[] { "3?K", "Owner" } },
        { "R_BASE_POSITION_0", new[]{ "wMC[0]", "Position[0]" } },
        { "R_BASE_POSITION_1", new[]{ "wMC[1]", "Position[1]" } },
        { "R_BASE_POSITION_2", new[]{ "wMC[2]", "Position[2]" } },
        { "R_BASE_TYPE", new[] { "peI.DPp", "BaseType.PersistentBaseTypes" } },
        { "R_OWNER_LID", new[] { "f5Q", "LID" } },
        { "R_OWNER_PTK", new[] { "D6b", "PTK" } },
        { "R_OWNER_UID", new[] { "K7E", "UID" } },
        { "R_OWNER_USN", new[] { "V?:", "USN" } },
        { "R_SONG_AUTHOR_ID", new[] { "m7b", "AuthorOnlineID" } },
        { "R_SONG_AUTHOR_USERNAME", new[] { "4ha", "AuthorUsername" } },
        { "R_SONG_AUTHOR_PLATFORM", new[] { "d2f", "AuthorPlatform" } },

        // select Multiple











        // absolut from root
        { "ACTIVE_CONTEXT", [ "", "", "XTp", "ActiveContext" ] },
        { "DIFFICULTY_ACTIVE_SURVIVAL_BARS", new[] { "6f=.LyC.:fe.tEx.ZeS", "PlayerStateData.DifficultyState.Settings.ActiveSurvivalBars.ActiveSurvivalBarsDifficulty", "vLc.6f=.LyC.:fe.tEx.ZeS", "BaseContext.PlayerStateData.DifficultyState.Settings.ActiveSurvivalBars.ActiveSurvivalBarsDifficulty" } },
        { "DIFFICULTY_HAZARD_DRAIN", new[] { "6f=.LyC.:fe.bGK.ORx", "PlayerStateData.DifficultyState.Settings.HazardDrain.HazardDrainDifficulty", "vLc.6f=.LyC.:fe.bGK.ORx", "BaseContext.PlayerStateData.DifficultyState.Settings.HazardDrain.HazardDrainDifficulty" } },
        { "DIFFICULTY_ENERGY_DRAIN", new[] { "6f=.LyC.:fe.A:s.Dn>", "PlayerStateData.DifficultyState.Settings.EnergyDrain.EnergyDrainDifficulty", "vLc.6f=.LyC.:fe.A:s.Dn>", "BaseContext.PlayerStateData.DifficultyState.Settings.EnergyDrain.EnergyDrainDifficulty" } },
        { "DIFFICULTY_SUBSTANCE_COLLECTION", new[] { "6f=.LyC.:fe.jH@.9JJ", "PlayerStateData.DifficultyState.Settings.SubstanceCollection.SubstanceCollectionDifficulty", "vLc.6f=.LyC.:fe.jH@.9JJ", "BaseContext.PlayerStateData.DifficultyState.Settings.SubstanceCollection.SubstanceCollectionDifficulty" } },
        { "DIFFICULTY_SPRINTING_COST", new[] { "6f=.LyC.:fe.l29.LT:", "PlayerStateData.DifficultyState.Settings.SprintingCost.SprintingCostDifficulty", "vLc.6f=.LyC.:fe.l29.LT:", "BaseContext.PlayerStateData.DifficultyState.Settings.SprintingCost.SprintingCostDifficulty" } },
        { "DIFFICULTY_SCANNER_RECHARGE", new[] { "6f=.LyC.:fe.Lf?.gFS", "PlayerStateData.DifficultyState.Settings.ScannerRecharge.ScannerRechargeDifficulty", "vLc.6f=.LyC.:fe.Lf?.gFS", "BaseContext.PlayerStateData.DifficultyState.Settings.ScannerRecharge.ScannerRechargeDifficulty" } },
        { "DIFFICULTY_DAMAGE_RECEIVED", new[] { "6f=.LyC.:fe.hXp.cYk", "PlayerStateData.DifficultyState.Settings.DamageReceived.DamageReceivedDifficulty", "vLc.6f=.LyC.:fe.hXp.cYk", "BaseContext.PlayerStateData.DifficultyState.Settings.DamageReceived.DamageReceivedDifficulty" } },
        { "DIFFICULTY_BREAK_TECH_ON_DAMAGE", new[] { "6f=.LyC.:fe.gd>.ef4", "PlayerStateData.DifficultyState.Settings.BreakTechOnDamage.BreakTechOnDamageProbability", "vLc.6f=.LyC.:fe.gd>.ef4", "BaseContext.PlayerStateData.DifficultyState.Settings.BreakTechOnDamage.BreakTechOnDamageProbability" } },
        { "DIFFICULTY_DEATH_CONSEQUENCES", new[] { "6f=.LyC.:fe.n7p.q2@", "PlayerStateData.DifficultyState.Settings.DeathConsequences.DeathConsequencesDifficulty", "vLc.6f=.LyC.:fe.n7p.q2@", "BaseContext.PlayerStateData.DifficultyState.Settings.DeathConsequences.DeathConsequencesDifficulty" } },
        { "DIFFICULTY_CHARGING_REQUIREMENTS", new[] { "6f=.LyC.:fe.nhq.428", "PlayerStateData.DifficultyState.Settings.ChargingRequirements.ChargingRequirementsDifficulty", "vLc.6f=.LyC.:fe.nhq.428", "BaseContext.PlayerStateData.DifficultyState.Settings.ChargingRequirements.ChargingRequirementsDifficulty" } },
        { "DIFFICULTY_FUEL_USE", new[] { "6f=.LyC.:fe.jnM.Eg1", "PlayerStateData.DifficultyState.Settings.FuelUse.FuelUseDifficulty", "vLc.6f=.LyC.:fe.jnM.Eg1", "BaseContext.PlayerStateData.DifficultyState.Settings.FuelUse.FuelUseDifficulty" } },
        { "DIFFICULTY_LAUNCH_FUEL_COST", new[] { "6f=.LyC.:fe.A9D.iqY", "PlayerStateData.DifficultyState.Settings.LaunchFuelCost.LaunchFuelCostDifficulty", "vLc.6f=.LyC.:fe.A9D.iqY", "BaseContext.PlayerStateData.DifficultyState.Settings.LaunchFuelCost.LaunchFuelCostDifficulty" } },
        { "DIFFICULTY_CRAFTING_IS_FREE", new[] { "6f=.LyC.:fe.?Dt", "PlayerStateData.DifficultyState.Settings.CraftingIsFree", "vLc.6f=.LyC.:fe.?Dt", "BaseContext.PlayerStateData.DifficultyState.Settings.CraftingIsFree" } },
        { "DIFFICULTY_CURRENCY_COST", new[] { "6f=.LyC.:fe.tsk.Ubk", "PlayerStateData.DifficultyState.Settings.CurrencyCost.CurrencyCostDifficulty", "vLc.6f=.LyC.:fe.tsk.Ubk", "BaseContext.PlayerStateData.DifficultyState.Settings.CurrencyCost.CurrencyCostDifficulty" } },
        { "DIFFICULTY_ITEM_SHOP_AVAILABILITY", new[] { "6f=.LyC.:fe.FB5.TYf", "PlayerStateData.DifficultyState.Settings.ItemShopAvailability.ItemShopAvailabilityDifficulty", "vLc.6f=.LyC.:fe.FB5.TYf", "BaseContext.PlayerStateData.DifficultyState.Settings.ItemShopAvailability.ItemShopAvailabilityDifficulty" } },
        { "DIFFICULTY_INVENTORY_STACK_LIMITS", new[] { "6f=.LyC.:fe.kZ5.?SS", "PlayerStateData.DifficultyState.Settings.InventoryStackLimits.InventoryStackLimitsDifficulty", "vLc.6f=.LyC.:fe.kZ5.?SS", "BaseContext.PlayerStateData.DifficultyState.Settings.InventoryStackLimits.InventoryStackLimitsDifficulty" } },
        { "DIFFICULTY_DAMAGE_GIVEN", new[] { "6f=.LyC.:fe.PYQ.mum", "PlayerStateData.DifficultyState.Settings.DamageGiven.DamageGivenDifficulty", "vLc.6f=.LyC.:fe.PYQ.mum", "BaseContext.PlayerStateData.DifficultyState.Settings.DamageGiven.DamageGivenDifficulty" } },
        { "DIFFICULTY_GROUND_COMBAT_TIMERS", new[] { "6f=.LyC.:fe.jGh.ZbV", "PlayerStateData.DifficultyState.Settings.GroundCombatTimers.CombatTimerDifficultyOption", "vLc.6f=.LyC.:fe.jGh.ZbV", "BaseContext.PlayerStateData.DifficultyState.Settings.GroundCombatTimers.CombatTimerDifficultyOption" } },
        { "DIFFICULTY_SPACE_COMBAT_TIMERS", new[] { "6f=.LyC.:fe.Od7.ZbV", "PlayerStateData.DifficultyState.Settings.SpaceCombatTimers.CombatTimerDifficultyOption", "vLc.6f=.LyC.:fe.Od7.ZbV", "BaseContext.PlayerStateData.DifficultyState.Settings.SpaceCombatTimers.CombatTimerDifficultyOption" } },
        { "DIFFICULTY_CREATURE_HOSTILITY", new[] { "6f=.LyC.:fe.BbG.1c;", "PlayerStateData.DifficultyState.Settings.CreatureHostility.CreatureHostilityDifficulty", "vLc.6f=.LyC.:fe.BbG.1c;", "BaseContext.PlayerStateData.DifficultyState.Settings.CreatureHostility.CreatureHostilityDifficulty" } },
        { "DIFFICULTY_INVENTORIES_ALWAYS_IN_RANGE", new[] { "6f=.LyC.:fe.pS0", "PlayerStateData.DifficultyState.Settings.InventoriesAlwaysInRange", "vLc.6f=.LyC.:fe.pS0", "BaseContext.PlayerStateData.DifficultyState.Settings.InventoriesAlwaysInRange" } },
        { "DIFFICULTY_WARP_DRIVE_REQUIREMENTS", new[] { "6f=.LyC.:fe.aw9", "PlayerStateData.DifficultyState.Settings.WarpDriveRequirements", "vLc.6f=.LyC.:fe.aw9", "BaseContext.PlayerStateData.DifficultyState.Settings.WarpDriveRequirements" } },
        { "DIFFICULTY_BASE_AUTO_POWER", new[] { "6f=.LyC.:fe.uo4", "PlayerStateData.DifficultyState.Settings.BaseAutoPower", "vLc.6f=.LyC.:fe.uo4", "BaseContext.PlayerStateData.DifficultyState.Settings.BaseAutoPower" } },
        { "DIFFICULTY_REPUTATION_GAIN", new[] { "6f=.LyC.:fe.vo>.S@3", "PlayerStateData.DifficultyState.Settings.ReputationGain.ReputationGainDifficulty", "vLc.6f=.LyC.:fe.vo>.S@3", "BaseContext.PlayerStateData.DifficultyState.Settings.ReputationGain.ReputationGainDifficulty" } },
        { "GAME_MODE_MAIN", new[] { "", "", "vLc.idA", "BaseContext.GameMode" } },
        { "GAME_MODE_SEASON", new[] { "", "", "2YS.idA", "ExpeditionContext.GameMode" } },
        { "GAME_VERSION_230", new[] { "6f=..Xf4", "PlayerStateData..CurrentPos" } },
        { "GAME_VERSION_260", new[] { "rnc.Ovv", "SpawnStateData.AbandonedFreighterPositionInSystem" } },
        { "GAME_VERSION_300", new[] { "6f=.ux@", "PlayerStateData.PreviousUniverseAddress" } },
        { "GAME_VERSION_320", new[] { "6f=.Mcl", "PlayerStateData.Pets" } },
        { "GAME_VERSION_340", new[] { "6f=.Rol.Whh", "PlayerStateData.SeasonData.MainMissionTitle" } },
        { "GAME_VERSION_350", new[] { "6f=.8iI", "PlayerStateData.ByteBeatLibrary" } },
        { "GAME_VERSION_351", new[] { "6f=.8iI.ON4..m7b", "PlayerStateData.ByteBeatLibrary.MySongs..AuthorOnlineID" } },
        { "GAME_VERSION_370", new[] { "6f=.Rol.qs?",  "PlayerStateData.SeasonData.SandwormOverrides" } },
        { "GAME_VERSION_384", new[] { "6f=.Agx",  "PlayerStateData.VehicleAIControlEnabled" } },
        { "GAME_VERSION_390", new[] { "6f=.Sd6", "PlayerStateData.NextLoadSpawnsWithFreshStart" } },
        { "GAME_VERSION_425", new[] { "6f=.Rol.XEk", "CommonStateData.SeasonData.SeasonStartMusicOverride" } },
        { "PLATFORM", new[] { "8>q", "Platform" } },
        { "SAVE_NAME", new[] { "6f=.Pk4", "PlayerStateData.SaveName", "<h0.Pk4", "CommonStateData.SaveName" } },
        { "SAVE_SUMMARY", new[] { "6f=.n:R", "PlayerStateData.SaveSummary", "vLc.6f=.n:R", "BaseContext.PlayerStateData.SaveSummary" } }, // TODO replace
        { "SAVE_SUMMARY_MAIN", new[] { "6f=.n:R", "PlayerStateData.SaveSummary", "vLc.6f=.n:R", "BaseContext.PlayerStateData.SaveSummary" } },
        { "SAVE_SUMMARY_SEASON", new[] { "6f=.n:R", "PlayerStateData.SaveSummary", "2YS.6f=.n:R", "ExpeditionContext.PlayerStateData.SaveSummary" } },
        { "SEASON_ID", new[] { "6f=.Rol.gou", "PlayerStateData.SeasonData.SeasonId", "<h0.Rol.gou", "CommonStateData.SeasonData.SeasonId" } },
        { "TOTAL_PLAY_TIME", new[] { "6f=.Lg8", "PlayerStateData.TotalPlayTime", "<h0.Lg8", "CommonStateData.TotalPlayTime" } },
        { "VERSION", new[] { "F2P", "Version" } },

        { "BASE_CONTEXT", new[] { "", "", "vLc", "BaseContext" } },
        { "EXPEDITION_CONTEXT", new[] { "", "", "2YS", "ExpeditionContext" } },

        // relative from somewhere
        { "BASE_GALACTIC_ADDRESS", new[]{ "oZw", "GalacticAddress" } },
        { "BASE_NAME", new[] { "NKm", "Name" } },
        { "BASE_OWNER", new[] { "3?K", "Owner" } },
        { "BASE_POSITION_0", new[]{ "wMC[0]", "Position[0]" } },
        { "BASE_POSITION_1", new[]{ "wMC[1]", "Position[1]" } },
        { "BASE_POSITION_2", new[]{ "wMC[2]", "Position[2]" } },
        { "BASE_TYPE", new[] { "peI.DPp", "BaseType.PersistentBaseTypes" } },
        { "OWNER_LID", new[] { "f5Q", "LID" } },
        { "OWNER_PTK", new[] { "D6b", "PTK" } },
        { "OWNER_UID", new[] { "K7E", "UID" } },
        { "OWNER_USN", new[] { "V?:", "USN" } },
        { "SONG_AUTHOR_ID", new[] { "m7b", "AuthorOnlineID" } },
        { "SONG_AUTHOR_USERNAME", new[] { "4ha", "AuthorUsername" } },
        { "SONG_AUTHOR_PLATFORM", new[] { "d2f", "AuthorPlatform" } },

        // select multiple
        { "DISCOVERY_DATA_OWNERSHIP", new[] { "fDu.ETO.OsQ.?fB[?({{0}})].ksu.{0}", "DiscoveryManagerData.DiscoveryData-v1.Store.Record[?({{0}})].OWS.{0}" } },
        { "DISCOVERY_DATA_OWNERSHIP_EXPRESSION_PTK", new[] { "@.ksu.D6b == '' || @.ksu.D6b == '{0}'", "@.OWS.PTK == '' || @.OWS.PTK == '{0}'" } }, // only with valid platform
        { "DISCOVERY_DATA_OWNERSHIP_EXPRESSION_LID", new[] { "@.ksu.f5Q != ''", "@.OWS.LID != ''" } }, // only if set
        { "DISCOVERY_DATA_WITH_UID", new[] { "fDu.ETO.OsQ.?fB..[?(@.K7E == '{0}')]", "DiscoveryManagerData.DiscoveryData-v1.Store.Record..[?(@.UID == '{0}')]" } },
        { "FREIGHTER_POSITION", new[] { "6f=.lpm[*]", "PlayerStateData.FreighterMatrixPos[*]", "vLc.6f=.lpm[*]", "BaseContext.PlayerStateData.FreighterMatrixPos[*]" } },
        { "MY_SONGS_WITH_UID", new[] { "6f=.8iI.ON4[?(@.m7b == '{0}')]", "PlayerStateData.ByteBeatLibrary.MySongs[?(@.AuthorOnlineID == '{0}')]", "<h0.8iI.ON4[?(@.m7b == '{0}')]", "CommonStateData.ByteBeatLibrary.MySongs[?(@.AuthorOnlineID == '{0}')]" } },
        { "PERSISTENT_PLAYER_BASE_ALL", new[] { "6f=.F?0[*]", "PlayerStateData.PersistentPlayerBases[*]", "vLc.6f=.F?0[*]", "BaseContext.PlayerStateData.PersistentPlayerBases[*]" } },
        { "PERSISTENT_PLAYER_BASE_ALL_TYPES", new[] { "6f=.F?0[*].peI.DPp", "PlayerStateData.PersistentPlayerBases[*].BaseType.PersistentBaseTypes", "vLc.6f=.F?0[*].peI.DPp", "BaseContext.PlayerStateData.PersistentPlayerBases[*].BaseType.PersistentBaseTypes" } },
        { "PERSISTENT_PLAYER_BASE_OWNERSHIP", new[] { "6f=.F?0[?({{0}})].3?K.{0}", "PlayerStateData.PersistentPlayerBases[?({{0}})].Owner.{0}", "vLc.6f=.F?0[?({{0}})].3?K.{0}", "BaseContext.PlayerStateData.PersistentPlayerBases[?({{0}})].Owner.{0}" } },
        { "PERSISTENT_PLAYER_BASE_OWNERSHIP_EXPRESSION_TYPE", new[] { "@.peI.DPp == '{0}' || @.peI.DPp == '{1}'", "@.BaseType.PersistentBaseTypes == '{0}' || @.BaseType.PersistentBaseTypes == '{1}'" } }, // only with own base
        { "PERSISTENT_PLAYER_BASE_OWNERSHIP_EXPRESSION_PTK", new[] { "@.3?K.D6b == '' || @.3?K.D6b == '{0}'", "@.Owner.PTK == '' || @.Owner.PTK == '{0}'" } }, // only with valid platform
        { "PERSISTENT_PLAYER_BASE_OWNERSHIP_EXPRESSION_LID", new[] { "@.3?K.f5Q != ''", "@.Owner.LID != ''" } }, // only if set
        { "SETTLEMENT_ALL_OWNER_LID", new[] { "6f=.GQA[*].3?K.f5Q", "PlayerStateData.SettlementStatesV2[*].Owner.LID", "vLc.6f=.GQA[*].3?K.f5Q", "BaseContext.PlayerStateData.SettlementStatesV2[*].Owner.LID" } },
        { "SETTLEMENT_OWNERSHIP", new[] { "6f=.GQA[?({{0}})].3?K.{0}", "PlayerStateData.SettlementStatesV2[?({{0}})].Owner.{0}", "vLc.6f=.GQA[?({{0}})].3?K.{0}", "BaseContext.PlayerStateData.SettlementStatesV2[?({{0}})].Owner.{0}" } },
        { "SETTLEMENT_OWNERSHIP_EXPRESSION_PTK", new[] { "@.3?K.D6b == '{0}'", "@.Owner.PTK == '{0}'" } }, // only with valid platform
        { "SETTLEMENT_OWNERSHIP_EXPRESSION_LID", new[] { "@.3?K.f5Q != ''", "@.Owner.LID != ''" } }, // only if set
        { "SETTLEMENT_WITH_UID", new[] { "6f=.GQA..[?(@.K7E == '{0}')]", "PlayerStateData.SettlementStatesV2..[?(@.UID == '{0}')]", "vLc.6f=.GQA..[?(@.K7E == '{0}')]", "BaseContext.PlayerStateData.SettlementStatesV2..[?(@.UID == '{0}')]" } },








        // template
        { "", new[] { "", "", "", "" } },
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
