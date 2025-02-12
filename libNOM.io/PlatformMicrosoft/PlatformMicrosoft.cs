using System.Text;

namespace libNOM.io;


/// <summary>
/// Implementation for the Microsoft platform.
/// </summary>
// This partial class contains some general code.
public partial class PlatformMicrosoft : Platform
{
    #region Constant

    internal const string ACCOUNT_PATTERN = "*_29070100B936489ABCE8B9AF3980429C";

    internal static readonly string[] ANCHOR_FILE_PATTERN = ["containers.index"];

    public static readonly string PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", "HelloGames.NoMansSky_bs190hzg1sesy", "SystemAppData", "wgs");

    protected override int META_LENGTH_AFTER_VANILLA => 0x14; // 20
    internal override int META_LENGTH_TOTAL_VANILLA => 0x18; // 24
    internal override int META_LENGTH_TOTAL_WAYPOINT => 0x118; // 280
    internal override int META_LENGTH_TOTAL_WORLDS_PART_I => 0x128; // 296

    private const int BLOBCONTAINER_HEADER = 0x4; // 4
    private const int BLOBCONTAINER_COUNT = 0x2; // 2
    private const int BLOBCONTAINER_IDENTIFIER_LENGTH = 0x80; // 128
    private const int BLOBCONTAINER_TOTAL_LENGTH = sizeof(int) + sizeof(int) + BLOBCONTAINER_COUNT * (BLOBCONTAINER_IDENTIFIER_LENGTH + 2 * 0x10); // 328

    private const int CONTAINERSINDEX_HEADER = 0xE; // 14
    private const long CONTAINERSINDEX_FOOTER = 0x10000000; // 268,435,456
    private const int CONTAINERSINDEX_OFFSET_BLOBCONTAINER_LIST = 0xC8; // 200

    internal static readonly byte[] HGSAVEV2_HEADER = [.. Encoding.ASCII.GetBytes("HGSAVEV2"), 0x00];
    internal const int HGSAVEV2_HEADER_LENGTH = 0x8; // 8
    internal const int HGSAVEV2_CHUNK_LENGTH_MAX = 0x100000; // 1,048,576

    #endregion

    #region Field

    private string _accountGuid = null!; // will be set when containers.index is parsed
    private FileInfo _containersindex = null!; // will be set if valid
    private DateTimeOffset _lastWriteTime; // will be set when containers.index is parsed to store global timestamp
    private string _processIdentifier = null!; // will be set when containers.index is parsed
    private ContainerExtra? _settingsContainer; // will be set when containers.index is parsed and exists

    #endregion
}
