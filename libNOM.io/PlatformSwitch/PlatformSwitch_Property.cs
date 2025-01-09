namespace libNOM.io;


/// <summary>
/// Implementation for the Nintendo Switch platform.
/// </summary>
// This partial class contains property related code.
public partial class PlatformSwitch : Platform
{
    #region Flags

    // public //

    public override bool CanCreate { get; } = true;

    public override bool CanRead { get; } = true;

    public override bool CanUpdate { get; } = true;

    public override bool CanDelete { get; } = true;

    public override bool HasModding { get; } = false;

    public override bool IsValid => AnchorFileIndex == 0; // { get; }

    public override RestartRequirementEnum RestartToApply { get; } = RestartRequirementEnum.Always;

    // protected //

    protected override bool IsConsolePlatform { get; } = true;

    #endregion

    #region Platform Indicator

    // public //

    public override PlatformEnum PlatformEnum { get; } = PlatformEnum.Switch;

    // protected //

    protected override string[] PlatformAnchorFilePattern { get; } = ANCHOR_FILE_PATTERN;

    protected override string? PlatformArchitecture { get; } = "NX1|Final";

    protected override string? PlatformProcessPath { get; } = null;

    protected override string PlatformToken { get; } = "NS";

    #endregion
}
