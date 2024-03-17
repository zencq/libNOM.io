﻿namespace libNOM.io;


/// <summary>
/// Implementation for the Sony PlayStation platform.
/// </summary>
// This partial class contains some general code.
public partial class PlatformPlaystation : Platform
{
    #region Constant

    internal static readonly string[] ANCHOR_FILE_PATTERN = ["savedata??.hg", "memory.dat"];

    protected override int COUNT_SAVE_SLOTS => _usesSaveStreaming ? base.COUNT_SAVE_SLOTS : 5;

    protected const uint META_HEADER = 0xCA55E77E;
    internal override int META_LENGTH_TOTAL_VANILLA => _usesSaveWizard ? 0x30 : 0x20; // 48 : 32
    internal override int META_LENGTH_TOTAL_WAYPOINT => _usesSaveWizard ? 0x70 : 0x0; // 112 : 0 // actually _FRONTIERS would be more accurate as there was no changed in Waypoint for PlayStation but reusing it as it has to be implemented anyway and would have no use otherwise

    private const uint MEMORYDAT_LENGTH_ACCOUNTDATA = 0x40000U;
    private const uint MEMORYDAT_LENGTH_CONTAINER = 0x300000U;
    private const uint MEMORYDAT_LENGTH_TOTAL = 0x2000000U; // 32 MB
    private const uint MEMORYDAT_LENGTH_TOTAL_SAVEWIZARD = 0x3000000U; // 48 MB // ten uncompressed saves may exceed the default length
    private int MEMORYDAT_META_INDEX_OFFSET => _usesSaveWizard ? 8 : 3;
    private int MEMORYDAT_META_INDEX_LENGTH => _usesSaveWizard ? 7 : 2;
    private int MEMORYDAT_OFFSET_META => _usesSaveWizard ? 0x40 : 0x0; // 64 : 0
    private int MEMORYDAT_OFFSET_DATA => _usesSaveWizard ? 0x1040 : 0x20000; // 4160 : 131072
    private const uint MEMORYDAT_OFFSET_DATA_ACCOUNTDATA = 0x20000U;
    private const uint MEMORYDAT_OFFSET_DATA_CONTAINER = 0xE0000U;

#pragma warning disable IDE0051 // Remove unused private member
    internal const string SAVEWIZARD_HEADER = "NOMANSKY";
    internal static readonly byte[] SAVEWIZARD_HEADER_BINARY = SAVEWIZARD_HEADER.GetUTF8Bytes();
    private const int SAVEWIZARD_VERSION_1 = 1; // not used but for completeness
    private const int SAVEWIZARD_VERSION_2 = 2;
#pragma warning restore IDE0051

    #endregion

    #region Field

    private DateTimeOffset? _lastWriteTime; // will be set to track _memorydat timestamp
    private FileInfo? _memorydat; // will be set if _usesSaveStreaming is false
    private bool _usesSaveStreaming; // will be set to indicate whether save streaming is used
    private bool _usesSaveWizard; // will be set to indicate whether SaveWizard is used

    #endregion

    #region Property

    #region Flags

    // public //

    public override bool CanCreate { get; } = true;

    public override bool CanRead { get; } = true;

    public override bool CanUpdate { get; } = true;

    public override bool CanDelete { get; } = true;

    public override bool HasAccountData => _usesSaveStreaming && base.HasAccountData; // { get; }

    public override bool HasModding { get; } = false;

    public override bool RestartToApply { get; } = true;

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

    #endregion
}
