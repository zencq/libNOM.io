using CommunityToolkit.HighPerformance;
using System.Text.RegularExpressions;

namespace libNOM.io;


public partial class PlatformSwitch : Platform
{
    #region Constant

    #region Platform Specific

    protected const uint META_HEADER = 0xCA55E77E;

    protected const int META_LENGTH_KNOWN = 0x28; // 40
    protected override int META_LENGTH_TOTAL_VANILLA => 0x64; // 100
    protected override int META_LENGTH_TOTAL_WAYPOINT => 0x164; // 356

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

    #region Directory Data

    public static readonly string[] ANCHOR_FILE_GLOB = new[] { "manifest*.hg", "savedata*.hg" };
#if NETSTANDARD2_0_OR_GREATER || NET6_0
    public static readonly Regex[] ANCHOR_FILE_REGEX = new Regex[] { AnchorFileRegex0, AnchorFileRegex1 };
#else
    public static readonly Regex[] ANCHOR_FILE_REGEX = new Regex[] { AnchorFileRegex0(), AnchorFileRegex1() };
#endif

    #endregion

    #endregion

    #region Property

    #region Configuration

    // public //

    public override PlatformEnum PlatformEnum { get; } = PlatformEnum.Switch;

    // protected //

    protected override string[] PlatformAnchorFileGlob { get; } = ANCHOR_FILE_GLOB;

    protected override Regex[] PlatformAnchorFileRegex { get; } = ANCHOR_FILE_REGEX;

    protected override string? PlatformArchitecture { get; } = "NX1|Final";


    protected override string? PlatformProcessPath { get; } = null;

    protected override string PlatformToken { get; } = "NS";

    #endregion

    #region Flags

    // public //

    public override bool HasModding { get; } = false;

    public override bool IsValid => AnchorFileIndex == 0; // { get; }

    public override bool RestartToApply { get; } = true;

    // protected //

    protected override bool CanCreate { get; } = true;

    protected override bool CanRead { get; } = true;

    protected override bool CanUpdate { get; } = true;

    protected override bool CanDelete { get; } = true;

    protected override bool IsConsolePlatform { get; } = true;

    #endregion

    #endregion

    #region Getter

    #region Container

    protected override IEnumerable<Container> GetCacheEvictionContainers(string name)
    {
        return SaveContainerCollection.Where(i => i.MetaFile?.Name.Equals(name, StringComparison.OrdinalIgnoreCase) == true);
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

    // // Read / Write

    #region Generate

    private protected override Container CreateContainer(int metaIndex, PlatformExtra? extra)
    {
        var data = new FileInfo(Path.Combine(Location.FullName, $"savedata{metaIndex:D2}.hg"));
        var meta = new FileInfo(Path.Combine(Location.FullName, $"manifest{metaIndex:D2}.hg"));

        return new Container(metaIndex)
        {
            DataFile = data,
            MetaFile = meta,
            /// Additional values will be set in <see cref="UpdateContainerWithMetaInformation"/> and <see cref="UpdateContainerWithDataInformation"/>.
            Extra = new(),
        };
    }

    #endregion

    #region Load

    protected override void UpdateContainerWithMetaInformation(Container container, ReadOnlySpan<byte> raw, ReadOnlySpan<uint> converted)
    {
        //  0. META HEADER          (  4)
        //  1. META FORMAT          (  4)
        //  2. DECOMPRESSED SIZE    (  4)
        //  3. META INDEX           (  4)
        //  4. TIMESTAMP            (  4)
        //  5. BASE VERSION         (  4)
        //  6. GAME MODE            (  2)
        //  6. SEASON               (  2)
        //  7. TOTAL PLAY TIME      (  4)
        //  8. EMPTY                (  8)

        // 10. EMPTY                ( 60)
        //                          (100)

        // 10. SAVE NAME            (128) // may contain additional junk data after null terminator
        // 42. SAVE SUMMARY         (128) // may contain additional junk data after null terminator
        // 74. DIFFICULTY PRESET    (  1)
        // 74. EMPTY                ( 59) // may contain additional junk data
        //                          (356)

        // Vanilla data always available.
        container.Extra = container.Extra with
        {
            Bytes = raw.Slice(META_LENGTH_KNOWN).ToArray(),
            Size = (uint)(raw.Length),
            SizeDecompressed = converted[2],
            LastWriteTime = DateTimeOffset.FromUnixTimeSeconds(converted[4]).ToLocalTime(),
            BaseVersion = (int)(converted[5]),
            GameMode = raw.Cast<ushort>(24),
            Season = raw.Cast<ushort>(26),
            TotalPlayTime = converted[7],
        };

        // Extended data since Waypoint.
        if (raw.Length == META_LENGTH_TOTAL_WAYPOINT)
        {
            container.Extra = container.Extra with
            {
                SaveName = converted.Slice(10, 32).GetSaveRenamingString(),
                SaveSummary = converted.Slice(42, 32).GetSaveRenamingString(),
                DifficultyPreset = raw[296],
            };
        }

        container.SaveVersion = Calculate.CalculateSaveVersion(container);
    }

    #endregion

    #region Write

    protected override Span<uint> CreateMeta(Container container, ReadOnlySpan<byte> data)
    {
        var buffer = new byte[GetMetaSize(container)];
        var unixSeconds = (uint)(container.LastWriteTime!.Value.ToUniversalTime().ToUnixTimeSeconds());

        using var writer = new BinaryWriter(new MemoryStream(buffer));
        writer.Write(META_HEADER); // 4
        writer.Write(Constants.SAVE_FORMAT_3); // 4
        writer.Write(container.Extra.SizeDecompressed); // 4

        if (container.IsAccount)
        {
            // Rewrite most of the extra as it contains data from another regular save.
            writer.Write(container.MetaIndex); // 4
            writer.Write(unixSeconds); // 4
            writer.Write(container.Extra.BaseVersion); // 4
            writer.Write(container.Extra.GameMode); // 4
            writer.Write(container.Extra.Season); // 4
            writer.Write(container.Extra.TotalPlayTime); // 4
        }
        else
        {
            writer.Write(container.MetaIndex); // 4
            writer.Write(unixSeconds); // 4
            writer.Write(container.BaseVersion); // 4
            writer.Write((ushort)(container.GameMode)); // 2
            writer.Write((ushort)(container.Season)); // 2
            writer.Write(container.TotalPlayTime); // 4

            // Extended data since Waypoint.
            if (container.MetaFormat >= MetaFormatEnum.Waypoint)
            {
                // Append cached bytes and overwrite afterwards.
                writer.Write(container.Extra.Bytes ?? Array.Empty<byte>()); // 272

                writer.Seek(META_LENGTH_KNOWN, SeekOrigin.Begin);
                writer.Write(container.SaveName.GetSaveRenamingBytes()); // 128

                writer.Seek(META_LENGTH_KNOWN + Constants.SAVE_RENAMING_LENGTH, SeekOrigin.Begin);
                writer.Write(container.SaveSummary.GetSaveRenamingBytes()); // 128

                writer.Seek(META_LENGTH_KNOWN + 2 * Constants.SAVE_RENAMING_LENGTH, SeekOrigin.Begin);
                writer.Write((byte)(container.GameDifficulty)); // 1
            }
            else
            {
                writer.Write(container.Extra.Bytes ?? Array.Empty<byte>()); // 60
            }
        }

        return buffer.AsSpan().Cast<byte, uint>();
    }

    #endregion

    // // File Operation

    // TODO Transfer Refactoring

    #region Transfer

    protected override void CreatePlatformExtra(Container destination, Container source)
    {
        destination.Extra = new PlatformExtra
        {
            Bytes = new byte[(source.IsVersion400Waypoint ? META_LENGTH_TOTAL_WAYPOINT : META_LENGTH_TOTAL_VANILLA) - META_LENGTH_KNOWN],
            BaseVersion = source.Extra.BaseVersion,
            TotalPlayTime = source.Extra.TotalPlayTime,
        };
    }

    #endregion
}
