using System.Text.RegularExpressions;

namespace libNOM.io;


public partial class PlatformSwitch : Platform
{
    #region Constant

    #region Platform Specific

    protected const uint META_HEADER = 0xCA55E77E;
    protected const int META_LENGTH_KNOWN = 0x20; // 32
    protected override int META_LENGTH_TOTAL_VANILLA => 0x64; // 100
    protected override int META_LENGTH_TOTAL_WAYPOINT => 0x164; // 356

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

    protected override string? PlatformArchitecture { get; } = "NX1|Final";

    public override PlatformEnum PlatformEnum { get; } = PlatformEnum.Switch;

    protected override string? PlatformProcess { get; } = null; // console platform has no PC process

    protected override string PlatformToken { get; } = "NS";

    #endregion

    #endregion

    #region Getter

    #region Container

    protected override IEnumerable<Container> GetCacheEvictionContainers(string name)
    {
        return SaveContainerCollection.Where(c => c.MetaFile?.Name.Equals(name, StringComparison.OrdinalIgnoreCase) == true);
    }

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

    private protected override Container CreateContainer(int metaIndex, PlatformExtra? extra)
    {
        var data = new FileInfo(Path.Combine(Location!.FullName, $"savedata{metaIndex:D2}.hg"));
        var meta = new FileInfo(Path.Combine(Location.FullName, $"manifest{metaIndex:D2}.hg"));

        return new Container(metaIndex)
        {
            DataFile = data,
            Extra = new()
            {
                /// Additional values will be set in <see cref="DecryptMeta"/>.
                Size = meta.Exists ? (uint)(meta.Length) : 0,
                SizeDisk = data.Exists ? (uint)(data.Length) : 0,
            },
            MetaFile = meta,
        };
    }

    #endregion

    #region Load

    protected override void UpdateContainerWithMetaInformation(Container container, byte[] raw, uint[] converted)
    {
        container.Extra = new()
        {
#if NETSTANDARD2_0
            Bytes = raw.Skip(META_LENGTH_KNOWN).ToArray(),
#else
            Bytes = raw[META_LENGTH_KNOWN..],
#endif

            Size = (uint)(raw.Length),
            SizeDecompressed = converted[2],
            LastWriteTime = DateTimeOffset.FromUnixTimeSeconds(converted[4]).ToLocalTime(),
            BaseVersion = (int)(converted[5]),
            GameMode = BitConverter.ToInt16(raw, 6 * sizeof(uint)),
            Season = BitConverter.ToInt16(raw, 6 * sizeof(uint) + 2),
            TotalPlayTime = converted[7],
        };

        if (container.Extra.Size == 0)
            container.Extra.Size = (uint)(raw.Length);
    }

    protected override void UpdateContainerWithDataInformation(Container container, byte[] raw, byte[] converted)
    {
        if (container.Extra.SizeDecompressed == 0)
            container.Extra.SizeDecompressed = (uint)(converted.Length);

        if (container.Extra.SizeDisk == 0)
            container.Extra.SizeDisk = (uint)(raw.Length);
    }

    #endregion

    #region Write

    protected override byte[] CreateMeta(Container container, byte[] data)
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

        var buffer = new byte[GetMetaSize(container)];
        var unixSeconds = (uint)(container.LastWriteTime!.Value.ToUniversalTime().ToUnixTimeSeconds());

        using var writer = new BinaryWriter(new MemoryStream(buffer));
        writer.Write(META_HEADER); // 4
        writer.Write(Globals.Constants.SAVE_FORMAT_3); // 4
        writer.Write(container.Extra.SizeDecompressed); // 4

        if (container.IsAccount)
        {
            // For account data rewrite most of the SwitchContainer as it contains data from another regular save.
            writer.Write(container.MetaIndex); // 4
            writer.Write(unixSeconds); // 4
            writer.Write(container.Extra.BaseVersion); // 4
            writer.Write(container.Extra.GameMode); // 4
            writer.Write(container.Extra.TotalPlayTime); // 4
        }
        else
        {
            writer.Write(container.MetaIndex); // 4
            writer.Write(unixSeconds); // 4
            writer.Write(container.Extra.BaseVersion); // 4
            writer.Write(container.Is400Waypoint && container.GameModeEnum < PresetGameModeEnum.Permadeath ? (short)(PresetGameModeEnum.Normal) : container.Extra.GameMode); // 2
            writer.Write(container.Extra.Season); // 2
            writer.Write(container.Extra.TotalPlayTime); // 4
        }
        writer.Write(container.Extra.Bytes ?? Array.Empty<byte>()); // 68 or 336

        return EncryptMeta(container, data, CompressMeta(container, data, buffer));
    }

    #endregion

    // // File Operation

    #region Transfer

    protected override void CreatePlatformExtra(Container destination, Container source)
    {
        destination.Extra = new PlatformExtra
        {
            Bytes = new byte[(source.Is400Waypoint ? META_LENGTH_TOTAL_WAYPOINT : META_LENGTH_TOTAL_VANILLA) - META_LENGTH_KNOWN],
            BaseVersion = source.Extra.BaseVersion,
            TotalPlayTime = source.Extra.TotalPlayTime,
        };
    }

    #endregion
}
