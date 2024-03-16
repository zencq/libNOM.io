using System.Text;

namespace libNOM.io;


public partial class PlatformMicrosoft : Platform
{
    #region Constant

    internal const string ACCOUNT_PATTERN = "*_29070100B936489ABCE8B9AF3980429C";

    internal static readonly string[] ANCHOR_FILE_PATTERN = ["containers.index"];

    internal static readonly string PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", "HelloGames.NoMansSky_bs190hzg1sesy", "SystemAppData", "wgs");

    protected override int META_LENGTH_KNOWN => 0x14; // 20
    internal override int META_LENGTH_TOTAL_VANILLA => 0x18; // 24
    internal override int META_LENGTH_TOTAL_WAYPOINT => 0x118; // 280

    private const int BLOBCONTAINER_HEADER = 0x4; // 4
    private const int BLOBCONTAINER_COUNT = 0x2; // 2
    private const int BLOBCONTAINER_IDENTIFIER_LENGTH = 0x80; // 128
    private const int BLOBCONTAINER_TOTAL_LENGTH = sizeof(int) + sizeof(int) + BLOBCONTAINER_COUNT * (BLOBCONTAINER_IDENTIFIER_LENGTH + 2 * 0x10); // 328

    private const int CONTAINERSINDEX_HEADER = 0xE; // 14
    private const long CONTAINERSINDEX_FOOTER = 0x10000000; // 268.435.456
    private const int CONTAINERSINDEX_OFFSET_BLOBCONTAINER_LIST = 0xC8; // 200

    internal static readonly byte[] SAVE_V2_HEADER = [.. Encoding.ASCII.GetBytes("HGSAVEV2"), 0x00];
    internal const int SAVE_V2_HEADER_PARTIAL_LENGTH = 0x8; // 8
    internal const int SAVE_V2_CHUNK_MAX_LENGTH = 0x100000; // 1.048.576

    #endregion

    #region Field

    private string _accountGuid = null!; // will be set when containers.index is parsed
    private FileInfo _containersindex = null!; // will be set if valid
    private DateTimeOffset _lastWriteTime; // will be set when containers.index is parsed to store global timestamp
    private string _processIdentifier = null!; // will be set when containers.index is parsed
    private PlatformExtra? _settingsContainer; // will be set when containers.index is parsed and exists

    #endregion

    #region Property

    #region Flags

    // public //

    public override bool CanCreate { get; } = true;

    public override bool CanRead { get; } = true;

    public override bool CanUpdate { get; } = true;

    public override bool CanDelete { get; } = true;

    public override bool Exists => base.Exists && _containersindex.Exists; // { get; }

    public override bool HasModding { get; } = false;

    public override bool RestartToApply { get; } = true;

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

    #endregion
}
