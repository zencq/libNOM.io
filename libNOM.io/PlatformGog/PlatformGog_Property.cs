namespace libNOM.io;


/// <summary>
/// Implementation for the GOG.com platform.
/// </summary>
// This partial class contains all property related code.
public partial class PlatformGog : PlatformSteam
{
    #region Platform Indicator

    // public //

    public override PlatformEnum PlatformEnum { get; } = PlatformEnum.Gog;

    // protected //

    protected override string? PlatformArchitecture { get; } = "Win|Final";

    protected override string? PlatformProcessPath { get; } = @"GOG Galaxy\Games\No Man's Sky\Binaries\NMS.exe";

    protected override string PlatformToken { get; } = "GX";

    #endregion
}
