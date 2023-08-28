namespace libNOM.io.Enums;


/// <summary>
/// Specifies all known expeditions including a placeholder for future ones.
/// </summary>
public enum SeasonEnum : ushort
{
    None = 0,
    Pioneers = None, // 1st
    Beachhead = 2, // 2nd
    Cartographers, // 3rd
    Emergence, // 4th
    PioneersRedux,
    BeachheadRedux,
    CartographersRedux,
    EmergenceRedux,
    Exobiology, // 5th
    Blighted, // 6th
    Leviathan, // 7th
    Polestar, // 8th
    ExobiologyRedux,
    BlightedRedux,
    LeviathanRedux,
    PolestarRedux,
    Utopia, // 9th
    Singularity, // 10th
    Voyagers, // 11th
    Future,
}
