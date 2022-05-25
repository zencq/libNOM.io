using System.ComponentModel;

namespace libNOM.io.Enums;


/// <summary>
/// Specifies all versions of big game updates and smaller ones if necessary for specific features.
/// </summary>
public enum VersionEnum
{
    Unknown = 0,
    Vanilla = 100,
    Foundation = 110,
    PathFinder = 120,
    AtlasRises = 130,
    Next = 150,
    Abyss = 170,
    Visions = 175,
    Beyond = 200,
    [Description("Beyond")]
    BeyondWithVehicleCam = 211,
    Synthesis = 220,
    [Description("Synthesis")]
    SynthesisWithJetpack = 226,
    LivingShip = 230,
    ExoMech = 240,
    Crossplay = 250,
    Desolation = 260,
    Origins = 300,
    NextGeneration = 310,
    Companions = 320,
    Expeditions = 330,
    Beachhead = 340,
    Prisms = 350,
    [Description("Prisms")]
    PrismsWithBytebeatAuthor = 351,
    Frontiers = 360,
    Emergence = 370,
    Sentinel = 380,
    [Description("Sentinel")]
    SentinelWithWeaponResource = 381,
    [Description("Sentinel")]
    SentinelWithVehicleAI = 384,
    Outlaws = 385,
    Leviathan = 390,
}
