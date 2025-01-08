namespace libNOM.io;


/// <summary>
/// Implementation for the Microsoft platform.
/// </summary>
// This partial class contains property related code.
public partial class PlatformMicrosoft : Platform
{
    #region Flags

    // public //

    public override bool CanCreate { get; } = true;

    public override bool CanRead { get; } = true;

    public override bool CanUpdate { get; } = true;

    public override bool CanDelete { get; } = true;

    public override bool Exists => base.Exists && _containersindex.Exists; // { get; }

    public override bool HasModding { get; } = false;

    public override RestartRequirementEnum RestartToApply { get; } = RestartRequirementEnum.Always;

    // protected //

    protected override bool IsConsolePlatform { get; } = false;

    #endregion

    #region Platform Indicator

    // public //

    public override PlatformEnum PlatformEnum { get; } = PlatformEnum.Microsoft;

    // protected //

    protected override string[] PlatformAnchorFilePattern { get; } = ANCHOR_FILE_PATTERN;

    protected override string? PlatformArchitecture { get; } = "XB1|Final";

    // Looks like "C:\\Program Files\\WindowsApps\\HelloGames.NoMansSky_4.38.0.0_x64__bs190hzg1sesy\\Binaries\\NMS.exe"
    protected override string? PlatformProcessPath { get; } = @"bs190hzg1sesy\Binaries\NMS.exe";

    protected override string PlatformToken { get; } = "XB";

    #endregion
}
