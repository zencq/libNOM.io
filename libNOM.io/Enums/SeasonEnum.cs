namespace libNOM.io.Enums;


/// <summary>
/// Specifies all known Expeditions incl. a placeholder for the next one.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1069:Enums values should not be duplicated", Justification = "Pioneers should be 1 but 0 is used in Version calculation")]
public enum SeasonEnum
{
    Unspecified = 0,
    Pioneers = 0,
    Beachhead = 2,
    Cartographers = 3,
    Emergence = 4,
    PioneersRedux = 5,
    BeachheadRedux = 6,
    CartographersRedux = 7,
    EmergenceRedux = 8,
    Exobiology = 9,
    Blighted = 10,
    Future,
}
