using Newtonsoft.Json.Linq;

namespace libNOM.io.Meta;


internal static class GameVersion
{
    #region Getter

    /// <summary>
    /// Gets the game version for account data based on the read meta.
    /// </summary>
    /// <param name="platform"></param>
    /// <param name="length"></param>
    /// <param name="format"></param>
    /// <returns></returns>
    internal static GameVersionEnum Get(Platform platform, int length, uint format)
    {
        if (length == platform.META_LENGTH_TOTAL_WAYPOINT)
            return GameVersionEnum.Waypoint; // Constants.META_FORMAT_3

        if (length == platform.META_LENGTH_TOTAL_VANILLA)
            return format switch
            {
                Constants.META_FORMAT_1 => GameVersionEnum.Vanilla,
                Constants.META_FORMAT_2 => GameVersionEnum.Foundation,
                Constants.META_FORMAT_3 => GameVersionEnum.Frontiers,
                _ => GameVersionEnum.Unknown,
            };

        return GameVersionEnum.Unknown;
    }

    /// <summary>
    /// Gets the game version for and purely from the specified baseVersion.
    /// </summary>
    /// <param name="baseVersion"></param>
    /// <returns></returns>
    internal static GameVersionEnum Get(int baseVersion) => baseVersion switch
    {
        >= 4149 => GameVersionEnum.OmegaWithV2, // 4.52 (Microsoft)
        >= 4147 => GameVersionEnum.Omega, // 4.50
        >= 4146 => GameVersionEnum.Echoes, // 4.40
        >= 4144 => GameVersionEnum.Interceptor, // (4.30/4.25), 4.20
        >= 4143 => GameVersionEnum.Fractal, // 4.10
        >= 4142 => GameVersionEnum.WaypointWithSuperchargedSlots, // 4.05
        >= 4141 => GameVersionEnum.WaypointWithAgileStat, // 4.04
        >= 4140 => GameVersionEnum.Waypoint, // 4.00
        >= 4139 => GameVersionEnum.Endurance, // 3.94
        >= 4138 => GameVersionEnum.Outlaws, // (3.90), 3.85
        >= 4137 => GameVersionEnum.SentinelWithWeaponResource, // (3.84), 3.81
        >= 4136 => GameVersionEnum.Sentinel, // 3.80
        >= 4135 => GameVersionEnum.Frontiers, // (3.70), 3.60
        >= 4129 => GameVersionEnum.Expeditions, // (3.51, 3.50, 3.40), 3.30
        >= 4127 => GameVersionEnum.NextGeneration, // (3.20), 3.10
        >= 4126 => GameVersionEnum.Crossplay, // (3.00, 2.60), 2.50
        >= 4125 => GameVersionEnum.ExoMech, // 2.40
        >= 4124 => GameVersionEnum.SynthesisWithJetpack, // (2.30), 2.26
        >= 4122 => GameVersionEnum.Synthesis, // 2.20
        >= 4119 => GameVersionEnum.BeyondWithVehicleCam, // 2.11
        _ => GameVersionEnum.Unknown,
    };

    /// <summary>
    /// Gets the game version for the specified baseVersion.
    /// </summary>
    /// <param name="baseVersion"></param>
    /// <param name="jsonObject"></param>
    /// <returns></returns>
    internal static GameVersionEnum Get(int baseVersion, JObject jsonObject) => baseVersion switch
    {
        >= 4149 => GameVersionEnum.OmegaWithV2, // 4.52 (Microsoft)
        >= 4147 => GameVersionEnum.Omega, // 4.50
        >= 4146 => GameVersionEnum.Echoes, // 4.40
        >= 4144 => GetSingularity(jsonObject) ?? GameVersionEnum.Interceptor, // 4.30/4.25, 4.20
        >= 4143 => GameVersionEnum.Fractal, // 4.10
        >= 4142 => GameVersionEnum.WaypointWithSuperchargedSlots, // 4.05
        >= 4141 => GameVersionEnum.WaypointWithAgileStat, // 4.04
        >= 4140 => GameVersionEnum.Waypoint, // 4.00
        >= 4139 => GameVersionEnum.Endurance, // 3.94
        >= 4138 => GetLeviathan(jsonObject) ?? GameVersionEnum.Outlaws, // 3.90, 3.85
        >= 4137 => GetSentinelWithVehicleAI(jsonObject) ?? GameVersionEnum.SentinelWithWeaponResource, // 3.84, 3.81
        >= 4136 => GameVersionEnum.Sentinel, // 3.80
        >= 4135 => GetEmergence(jsonObject) ?? GameVersionEnum.Frontiers, // 3.70, 3.60
        >= 4129 => GetPrismsWithByteBeatAuthor(jsonObject) ?? GetPrisms(jsonObject) ?? GetBeachhead(jsonObject) ?? GameVersionEnum.Expeditions, // 3.51, 3.50, 3.40, 3.30
        >= 4127 => GetCompanions(jsonObject) ?? GameVersionEnum.NextGeneration, // 3.20, 3.10
        >= 4126 => GetOrigins(jsonObject) ?? GetDesolation(jsonObject) ?? GameVersionEnum.Crossplay, // 3.00, 2.60, 2.50
        >= 4125 => GameVersionEnum.ExoMech, // 2.40
        >= 4124 => GetLivingShip(jsonObject) ?? GameVersionEnum.SynthesisWithJetpack, // 2.30, 2.26
        >= 4122 => GameVersionEnum.Synthesis, // 2.20
        >= 4119 => GameVersionEnum.BeyondWithVehicleCam, // 2.11
        _ => GameVersionEnum.Unknown,
    };

    /// <inheritdoc cref="Get(int, JObject)"/>
    /// <param name="json"></param>
    internal static GameVersionEnum Get(int baseVersion, string json) => baseVersion switch
    {
        >= 4149 => GameVersionEnum.OmegaWithV2, // 4.52 (Microsoft)
        >= 4147 => GameVersionEnum.Omega, // 4.50
        >= 4146 => GameVersionEnum.Echoes, // 4.40
        >= 4144 => GetSingularity(json) ?? GameVersionEnum.Interceptor, // 4.30/4.25, 4.20
        >= 4143 => GameVersionEnum.Fractal, // 4.10
        >= 4142 => GameVersionEnum.WaypointWithSuperchargedSlots, // 4.05
        >= 4141 => GameVersionEnum.WaypointWithAgileStat, // 4.04
        >= 4140 => GameVersionEnum.Waypoint, // 4.00
        >= 4139 => GameVersionEnum.Endurance, // 3.94
        >= 4138 => GetLeviathan(json) ?? GameVersionEnum.Outlaws, // 3.90, 3.85
        >= 4137 => GetSentinelWithVehicleAI(json) ?? GameVersionEnum.SentinelWithWeaponResource, // 3.84, 3.81
        >= 4136 => GameVersionEnum.Sentinel, // 3.80
        >= 4135 => GetEmergence(json) ?? GameVersionEnum.Frontiers, // 3.70, 3.60
        >= 4129 => GetPrismsWithByteBeatAuthor(json) ?? GetPrisms(json) ?? GetBeachhead(json) ?? GameVersionEnum.Expeditions, // 3.51, 3.50, 3.40, 3.30
        >= 4127 => GetCompanions(json) ?? GameVersionEnum.NextGeneration, // 3.20, 3.10
        >= 4126 => GetOrigins(json) ?? GetDesolation(json) ?? GameVersionEnum.Crossplay, // 3.00, 2.60, 2.50
        >= 4125 => GameVersionEnum.ExoMech, // 2.40
        >= 4124 => GetLivingShip(json) ?? GameVersionEnum.SynthesisWithJetpack, // 2.30, 2.26
        >= 4122 => GameVersionEnum.Synthesis, // 2.20
        >= 4119 => GameVersionEnum.BeyondWithVehicleCam, // 2.11
        _ => GameVersionEnum.Unknown,
    };

    #endregion

    #region GetEnumIf

    private static GameVersionEnum? GetEnumIfAny(JObject jsonObject, string pathIdentifier, GameVersionEnum gameVersion)
    {
        // Only relevant for pre-Omega saves and therefore context does not matter.
        var result = jsonObject.SelectTokens(Json.GetPath(pathIdentifier, jsonObject));
        return result.Any() ? gameVersion : null;
    }

    private static GameVersionEnum? GetEnumIfContains(string json, string obfuscated, string plaintext, GameVersionEnum gameVersion)
    {
        var result = json.Contains($"\"{obfuscated}\":") || json.Contains($"\"{plaintext}\":");
        return result ? gameVersion : null;
    }

    private static GameVersionEnum? GetEnumIfNotNull(JObject jsonObject, string pathIdentifier, GameVersionEnum gameVersion)
    {
        // Only relevant for pre-Omega saves and therefore context does not matter.
        var result = jsonObject.SelectToken(Json.GetPath(pathIdentifier, jsonObject));
        return result is not null ? gameVersion : null;
    }

    #endregion

    #region Get Version by JSON Indicator

    // GAME_VERSION_430 (PlayerStateData.SeasonData.Stages[*].Milestones[*].GreyIfCantStart) is only used in actual expedition saves.
    // This uses actually VersionEnum.Mac but it made most of the preparation for Singularity and therefore we already use this here.
    private static GameVersionEnum? GetSingularity(JObject jsonObject) => GetEnumIfNotNull(jsonObject, "GAME_VERSION_425", GameVersionEnum.Singularity);
    private static GameVersionEnum? GetSingularity(string json) => GetEnumIfContains(json, "XEk", "SeasonStartMusicOverride", GameVersionEnum.Singularity);

    private static GameVersionEnum? GetLeviathan(JObject jsonObject) => GetEnumIfNotNull(jsonObject, "GAME_VERSION_390", GameVersionEnum.Leviathan);
    private static GameVersionEnum? GetLeviathan(string json) => GetEnumIfContains(json, "Sd6", "NextLoadSpawnsWithFreshStart", GameVersionEnum.Leviathan);

    private static GameVersionEnum? GetSentinelWithVehicleAI(JObject jsonObject) => GetEnumIfNotNull(jsonObject, "GAME_VERSION_384", GameVersionEnum.SentinelWithVehicleAI);
    private static GameVersionEnum? GetSentinelWithVehicleAI(string json) => GetEnumIfContains(json, "Agx", "VehicleAIControlEnabled", GameVersionEnum.SentinelWithVehicleAI);

    private static GameVersionEnum? GetEmergence(JObject jsonObject) => GetEnumIfAny(jsonObject, "GAME_VERSION_370", GameVersionEnum.Emergence);
    private static GameVersionEnum? GetEmergence(string json) => GetEnumIfContains(json, "qs?", "SandwormOverrides", GameVersionEnum.Emergence);

    private static GameVersionEnum? GetPrismsWithByteBeatAuthor(JObject jsonObject) => GetEnumIfAny(jsonObject, "GAME_VERSION_351", GameVersionEnum.PrismsWithByteBeatAuthor);
    private static GameVersionEnum? GetPrismsWithByteBeatAuthor(string json) => GetEnumIfContains(json, "m7b", "AuthorOnlineID", GameVersionEnum.PrismsWithByteBeatAuthor);

    private static GameVersionEnum? GetPrisms(JObject jsonObject) => GetEnumIfNotNull(jsonObject, "GAME_VERSION_350", GameVersionEnum.Prisms);
    private static GameVersionEnum? GetPrisms(string json) => GetEnumIfContains(json, "8iI", "ByteBeatLibrary", GameVersionEnum.Prisms);

    private static GameVersionEnum? GetBeachhead(JObject jsonObject) => GetEnumIfNotNull(jsonObject, "GAME_VERSION_340", GameVersionEnum.Beachhead);
    private static GameVersionEnum? GetBeachhead(string json) => GetEnumIfContains(json, "Whh", "MainMissionTitle", GameVersionEnum.Beachhead);

    private static GameVersionEnum? GetCompanions(JObject jsonObject) => GetEnumIfNotNull(jsonObject, "GAME_VERSION_320", GameVersionEnum.Companions);
    private static GameVersionEnum? GetCompanions(string json) => GetEnumIfContains(json, "Mcl", "Pets", GameVersionEnum.Companions);

    private static GameVersionEnum? GetOrigins(JObject jsonObject) => GetEnumIfNotNull(jsonObject, "GAME_VERSION_300", GameVersionEnum.Origins);
    private static GameVersionEnum? GetOrigins(string json) => GetEnumIfContains(json, "ux@", "PreviousUniverseAddress", GameVersionEnum.Origins);

    private static GameVersionEnum? GetDesolation(JObject jsonObject) => GetEnumIfNotNull(jsonObject, "GAME_VERSION_260", GameVersionEnum.Desolation);
    private static GameVersionEnum? GetDesolation(string json) => GetEnumIfContains(json, "Ovv", "AbandonedFreighterPositionInSystem", GameVersionEnum.Desolation);

    private static GameVersionEnum? GetLivingShip(JObject jsonObject) => GetEnumIfAny(jsonObject, "GAME_VERSION_230", GameVersionEnum.LivingShip);
    private static GameVersionEnum? GetLivingShip(string json) => GetEnumIfContains(json, "Xf4", "CurrentPos", GameVersionEnum.LivingShip);

    #endregion
}
