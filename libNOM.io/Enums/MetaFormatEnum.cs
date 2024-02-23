namespace libNOM.io.Enums;


/// <summary>
/// Specifies the possible formats the meta/manifest file can have.
/// </summary>
internal enum MetaFormatEnum : uint
{
    Unknown,
    Vanilla,
    Foundation, // 1.10
    Frontiers, // 3.60
    Waypoint, // 4.00
}
