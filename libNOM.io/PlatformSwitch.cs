using CommunityToolkit.Diagnostics;
using System.Text.RegularExpressions;

namespace libNOM.io;


#region Container

internal record class PlatformExtraSwitch
{
    internal uint BaseVersion;

    internal uint GameMode;

    internal uint MetaIndex;

    internal byte[] MetaTail = null!;

    internal uint TotalPlayTime;
}

public partial class Container
{
    internal PlatformExtraSwitch? Switch { get; set; }
}

#endregion

public partial class PlatformSwitch : Platform
{
    #region Constant

    #region Platform Specific

    private const uint META_HEADER = 0xCA55E77E;
    private const int META_KNOWN = 0x20; // 32
    private const int META_SIZE = 0x64; // 100
    private const int META_SIZE_WAYPOINT = 0x164; // 256

    #endregion

    #region Directory Data

    public static readonly string[] ANCHOR_FILE_GLOB = new[] { "manifest*.hg", "savedata*.hg" };
#if NETSTANDARD2_0_OR_GREATER || NET6_0
    public static readonly Regex[] ANCHOR_FILE_REGEX = new Regex[] { AnchorFileRegex0!, AnchorFileRegex1! };
#else
    public static readonly Regex[] ANCHOR_FILE_REGEX = new Regex[] { AnchorFileRegex0(), AnchorFileRegex1() };
#endif

    #endregion

    #region Generated Regex

#if NETSTANDARD2_0_OR_GREATER || NET6_0
    private static readonly Regex AnchorFileRegex0 = new("manifest\\d{2}\\.hg", RegexOptions.Compiled);
    private static readonly Regex AnchorFileRegex1 = new("savedata\\d{2}\\.hg", RegexOptions.Compiled);
#else
    [GeneratedRegex("manifest\\d{2}\\.hg", RegexOptions.Compiled)]
    private static partial Regex AnchorFileRegex0();

    [GeneratedRegex("savedata\\d{2}\\.hg", RegexOptions.Compiled)]
    private static partial Regex AnchorFileRegex1();
#endif

    #endregion

    #endregion

    #region Property

    #region Flags

    public override bool CanCreate { get; } = true;

    public override bool CanRead { get; } = true;

    public override bool CanUpdate { get; } = true;

    public override bool CanDelete { get; } = true;

    public override bool HasModding { get; } = false;

    public override bool IsPersonalComputerPlatform { get; } = false;

    public override bool IsValid => AnchorFileIndex == 0; // { get; } // seconds index is only there to have the actual save data filename to get the MetaIndex in AnalyzeFile 

    public override bool RestartToApply { get; } = false;

    #endregion

    #region Platform Indicator

    protected override string[] PlatformAnchorFileGlob { get; } = ANCHOR_FILE_GLOB;

    protected override Regex[] PlatformAnchorFileRegex { get; } = ANCHOR_FILE_REGEX;

    protected override string PlatformArchitecture { get; } = "NX1|Final";

    public override PlatformEnum PlatformEnum { get; } = PlatformEnum.Switch;

    protected override string? PlatformProcess { get; } = null; // console platform has no PC process

    protected override string PlatformToken { get; } = "NS";

    #endregion

    #endregion

    // //

    #region Constructor

    public PlatformSwitch(string path) : base(path) { }

    public PlatformSwitch(string path, PlatformSettings platformSettings) : base(path, platformSettings) { }

    public PlatformSwitch(DirectoryInfo directory) : base(directory) { }

    public PlatformSwitch(DirectoryInfo directory, PlatformSettings platformSettings) : base(directory, platformSettings) { }

    #endregion

    // //

    // // Read / Write

    #region Generate

    protected override Container CreateContainer(int metaIndex, object? extra)
    {
        return new Container(metaIndex)
        {
            DataFile = new FileInfo(Path.Combine(Location!.FullName, $"savedata{metaIndex:D2}.hg")),
            MetaFile = new FileInfo(Path.Combine(Location!.FullName, $"manifest{metaIndex:D2}.hg")),
        };
    }

    #endregion

    #region Load

    protected override uint[] DecryptMeta(Container container, byte[] meta)
    {
        var metaInt = base.DecryptMeta(container, meta);

        container.LastWriteTime = DateTimeOffset.FromUnixTimeSeconds(metaInt[4]).ToLocalTime();
        container.Switch = new()
        {
            BaseVersion = metaInt[5],
            GameMode = metaInt[6],
            MetaIndex = metaInt[3],
#if NETSTANDARD2_0
            MetaTail = meta.Skip(META_KNOWN).ToArray(),
#else
            MetaTail = meta[META_KNOWN..],
#endif
            TotalPlayTime = metaInt[7],
        };

        return metaInt;
    }

    protected override byte[] DecompressData(Container container, uint[] meta, byte[] data)
    {
        // No compression for account data.
        if (!container.IsSave)
            return data;

        return DecompressSaveStreamingData(data);
    }

    #endregion

    #region Write

    protected override byte[] CompressData(Container container, byte[] data)
    {
        if (!container.IsSave)
            return data;

        return CompressSaveStreamingData(data);
    }

    protected override byte[] CreateMeta(Container container, byte[] data, int decompressedSize)
    {
        //  0. META HEADER          (  4)
        //  1. META FORMAT          (  4)
        //  2. DECOMPRESSED SIZE    (  4)
        //  3. META INDEX           (  4)
        //  4. TIMESTAMP            (  4)
        //  5. SAVE VERSION         (  4)
        //  6. GAME MODE            (  2)
        //  6. SEASON               (  2)
        //  7. TOTAL PLAY TIME      (  4)
        //  8. UNKNOWN              ( 68)
        //                          (100)

        //  8. UNKNOWN              (324) // Waypoint
        //                          (356)

        // META_KNOWN and Steam.MetaTail are using uint and therefore need to be multiplied by 4 to get the actual buffer size.
        var bufferSize = container.Switch?.MetaTail is not null ? (META_KNOWN + container.Switch!.MetaTail.Length) * 4 : (container.IsWaypoint ? META_SIZE_WAYPOINT : META_SIZE);
        var buffer = new byte[bufferSize];
        var unixSeconds = (uint)(container.LastWriteTime.ToUniversalTime().ToUnixTimeSeconds());

        using var writer = new BinaryWriter(new MemoryStream(buffer));
        writer.Write(META_HEADER); // 4
        writer.Write(Globals.Constant.SAVE_FORMAT_3); // 4
        writer.Write(decompressedSize); // 4

        if (!container.IsSave)
        {
            // For account data rewrite most of the SwitchContainer as it contains data from another regular save.
            writer.Write(container.Switch!.MetaIndex); // 4
            writer.Write(unixSeconds); // 4
            writer.Write(container.Switch!.BaseVersion); // 4
            writer.Write(container.Switch!.GameMode); // 4
            writer.Write(container.Switch!.TotalPlayTime); // 4
        }
        else
        {
            writer.Write(container.MetaIndex); // 4
            writer.Write(unixSeconds); // 4
            writer.Write(container.BaseVersion); // 4
            writer.Write((ushort)(container.GameModeEnum ?? 0)); // 2
            writer.Write((ushort)(container.SeasonEnum)); // 2
            writer.Write((uint)(container.TotalPlayTime)); // 4
        }
        writer.Write(container.Switch!.MetaTail ?? Array.Empty<byte>()); // 68 or 336

        return EncryptMeta(container, data, CompressMeta(container, data, buffer));
    }

    #endregion

    // // File Operation

    #region Copy

    protected override bool GuardPlatformExtra(Container source)
    {
        return source.Switch is null;
    }

    protected override void CopyPlatformExtra(Container destination, Container source)
    {
        destination.Switch = new PlatformExtraSwitch
        {
            BaseVersion = source.Switch!.BaseVersion,
            GameMode = source.Switch!.GameMode,
            MetaIndex = source.Switch!.MetaIndex,
            MetaTail = source.Switch!.MetaTail,
            TotalPlayTime = source.Switch!.TotalPlayTime,
        };
    }

    #endregion

    #region Transfer

    protected override void CreatePlatformExtra(Container destination, Container source)
    {
        destination.Switch = new PlatformExtraSwitch
        {
            BaseVersion = (uint)(source.BaseVersion),
            MetaIndex = (uint)(source.MetaIndex),
            MetaTail = new byte[(source.IsWaypoint ? META_SIZE_WAYPOINT : META_SIZE) - META_KNOWN],
            TotalPlayTime = (uint)(source.TotalPlayTime),
        };
    }

    #endregion
}
