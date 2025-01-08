using libNOM.io.Services;

namespace libNOM.io;


/// <summary>
/// Implementation for the Steam platform.
/// </summary>
// This partial class contains property related code.
public partial class PlatformSteam : Platform
{
    #region Field

    private SteamService? _steamService; // will be set if SteamService is accessed

    #endregion

    // //

    #region Flags

    // public //

    public override bool CanCreate { get; } = true;

    public override bool CanRead { get; } = true;

    public override bool CanUpdate { get; } = true;

    public override bool CanDelete { get; } = true;

    public override bool HasModding { get; } = true;

    public override RestartRequirementEnum RestartToApply { get; } = RestartRequirementEnum.AccountOnly;

    // protected //

    protected override bool IsConsolePlatform { get; } = false;

    #endregion

    #region Platform Configuration

    private SteamService SteamService => _steamService ??= new(); // { get; }

    #endregion

    #region Platform Indicator

    // public //

    public override PlatformEnum PlatformEnum { get; } = PlatformEnum.Steam;

    // protected //

    protected override string[] PlatformAnchorFilePattern { get; } = ANCHOR_FILE_PATTERN;

    // On SteamDeck (with Proton) the Windows architecture is also used.
    protected override string? PlatformArchitecture => Common.IsWindowsOrLinux() ? "Win|Final" : (Common.IsMac() ? "Mac|Final" : null); // { get; }

    // Same as the architecture but for the process.
    protected override string? PlatformProcessPath => Common.IsWindowsOrLinux() ? @"steamapps\common\No Man's Sky\Binaries\NMS.exe" : (Common.IsMac() ? @"steamapps/common/No Man's Sky/No Man's Sky.app/Contents/MacOS/No Man's Sky" : null); // { get; }

    protected override string PlatformToken { get; } = "ST";

    #endregion
}
