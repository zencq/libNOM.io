namespace libNOM.io;


/// <summary>
/// Implementation for the Nintendo Switch platform.
/// </summary>
// This partial class contains some general code.
public partial class PlatformSwitch : Platform
{
    #region Constant

    internal static readonly string[] ANCHOR_FILE_PATTERN = ["manifest??.hg", "savedata??.hg"];

    protected const uint META_HEADER = 0xCA55E77E;
    protected override int META_LENGTH_KNOWN_VANILLA => 0x28; // 40
    internal override int META_LENGTH_TOTAL_VANILLA => 0x64; // 100
    internal override int META_LENGTH_TOTAL_WAYPOINT => 0x164; // 356
    internal override int META_LENGTH_TOTAL_WORLDS => 0x174; // 372

    #endregion
}
