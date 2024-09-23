using System.ComponentModel;

namespace libNOM.io.Enums;


/// <summary>
/// Specifies all versions of big game updates and smaller ones if necessary for specific features.
/// </summary>
// EXTERNAL RELEASE: Add new game version.
public enum GameVersionEnum
{
    Unknown,
    Vanilla = 100,
    Foundation = 110,
    PathFinder = 120,
    AtlasRises = 130,
    Next = 150,
    Abyss = 170,
    Visions = 175,
    Beyond = 200,
    [Description(nameof(Beyond))]
    BeyondWithVehicleCam = 211,
    Synthesis = 220,
    [Description(nameof(Synthesis))]
    SynthesisWithJetpack = 226,
    LivingShip = 230,
    ExoMech = 240,
    Crossplay = 251,
    Desolation = 260,
    Origins = 300,
    NextGeneration = 310,
    Companions = 320,
    Expeditions = 330,
    Beachhead = 340,
    Prisms = 350,
    [Description(nameof(Prisms))]
    PrismsWithByteBeatAuthor = 351,
    Frontiers = 360,
    Emergence = 370,
    Sentinel = 380,
    [Description(nameof(Sentinel))]
    SentinelWithWeaponResource = 381,
    [Description(nameof(Sentinel))]
    SentinelWithVehicleAI = 384,
    Outlaws = 385,
    Leviathan = 390,
    Endurance = 394,
    Waypoint = 400,
    [Description(nameof(Waypoint))]
    WaypointWithAgileStat = 404,
    [Description(nameof(Waypoint))]
    WaypointWithSuperchargedSlots = 405,
    Fractal = 410,
    Interceptor = 420,
    Mac = 425,
    Singularity = 430,
    Echoes = 440,
    Omega = 450,
    [Description(nameof(Omega))]
    OmegaWithMicrosoftV2 = 452,
    Orbital = 460,
    Adrift = 470,
    WorldsPartI = 500,
    Aquarius = 510,
}
