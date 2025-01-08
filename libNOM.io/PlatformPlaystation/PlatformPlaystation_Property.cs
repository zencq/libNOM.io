namespace libNOM.io;


/// <summary>
/// Implementation for the Sony PlayStation platform.
/// </summary>
// This partial class contains property related code.
public partial class PlatformPlaystation : Platform
{
    #region Flags

    // public //

    public override bool CanCreate { get; } = true;

    public override bool CanRead { get; } = true;

    public override bool CanUpdate { get; } = true;

    public override bool CanDelete { get; } = true;

    public override bool HasAccountData => _usesSaveStreaming && base.HasAccountData; // { get; }

    public override bool HasModding { get; } = false;

    public override RestartRequirementEnum RestartToApply { get; } = RestartRequirementEnum.Always;

    // protected //

    protected override bool IsConsolePlatform { get; } = true;

    #endregion

    #region Platform Indicator

    // public //

    public override PlatformEnum PlatformEnum { get; } = PlatformEnum.Playstation;

    // protected //

    protected override string[] PlatformAnchorFilePattern { get; } = ANCHOR_FILE_PATTERN;

    protected override string? PlatformArchitecture { get; } = "PS4|Final";

    protected override string? PlatformProcessPath { get; } = null;

    protected override string PlatformToken { get; } = "PS";

    #endregion
}
