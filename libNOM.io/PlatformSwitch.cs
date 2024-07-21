using CommunityToolkit.HighPerformance;

using libNOM.io.Settings;

namespace libNOM.io;


/// <summary>
/// Implementation for the Nintendo Switch platform.
/// </summary>
// This partial class contains all related code.
public partial class PlatformSwitch : Platform
{
    #region Constant

    internal static readonly string[] ANCHOR_FILE_PATTERN = ["manifest??.hg", "savedata??.hg"];

    protected const uint META_HEADER = 0xCA55E77E;
    protected override int META_LENGTH_KNOWN_VANILLA => 0x28; // 40
    internal override int META_LENGTH_TOTAL_VANILLA => 0x64; // 100
    internal override int META_LENGTH_TOTAL_WAYPOINT => 0x164; // 356
    internal override int META_LENGTH_TOTAL_WORLDS => META_LENGTH_TOTAL_WAYPOINT; // no changes for this platform

    #endregion

    // Property

    #region Flags

    // public //

    public override bool CanCreate { get; } = true;

    public override bool CanRead { get; } = true;

    public override bool CanUpdate { get; } = true;

    public override bool CanDelete { get; } = true;

    public override bool HasModding { get; } = false;

    public override bool IsValid => AnchorFileIndex == 0; // { get; }

    public override RestartRequirementEnum RestartToApply { get; } = RestartRequirementEnum.Always;

    // protected //

    protected override bool IsConsolePlatform { get; } = true;

    #endregion

    #region Platform Indicator

    // public //

    public override PlatformEnum PlatformEnum { get; } = PlatformEnum.Switch;

    // protected //

    protected override string[] PlatformAnchorFilePattern { get; } = ANCHOR_FILE_PATTERN;

    protected override string? PlatformArchitecture { get; } = "NX1|Final";

    protected override string? PlatformProcessPath { get; } = null;

    protected override string PlatformToken { get; } = "NS";

    #endregion

    // Accessor

    #region Getter

    protected override IEnumerable<Container> GetCacheEvictionContainers(string name)
    {
        return SaveContainerCollection.Where(i => i.MetaFile?.Name.Equals(name, StringComparison.OrdinalIgnoreCase) == true);
    }

    #endregion

    // Initialize

    #region Constructor

    public PlatformSwitch() : base() { }

    public PlatformSwitch(string? path) : base(path) { }

    public PlatformSwitch(string? path, PlatformSettings? platformSettings) : base(path, platformSettings) { }

    public PlatformSwitch(PlatformSettings? platformSettings) : base(platformSettings) { }

    public PlatformSwitch(DirectoryInfo? directory) : base(directory) { }

    public PlatformSwitch(DirectoryInfo? directory, PlatformSettings? platformSettings) : base(directory, platformSettings) { }

    #endregion

    #region Initialize

    private protected override Container CreateContainer(int metaIndex, ContainerExtra? _)
    {
        return new Container(metaIndex, this)
        {
            DataFile = new FileInfo(Path.Combine(Location.FullName, $"savedata{metaIndex:D2}.hg")),
            MetaFile = new FileInfo(Path.Combine(Location.FullName, $"manifest{metaIndex:D2}.hg")),
            /// Additional values will be set in <see cref="UpdateContainerWithMetaInformation"/> and <see cref="Platform.UpdateContainerWithDataInformation"/>.
            Extra = new(),
        };
    }

    #endregion

    #region Process

    protected override void UpdateContainerWithMetaInformation(Container container, ReadOnlySpan<byte> disk, ReadOnlySpan<uint> decompressed)
    {
        /**
          0. META HEADER          (  4)
          1. META FORMAT          (  4)
          2. DECOMPRESSED SIZE    (  4)
          3. META INDEX           (  4)
          4. TIMESTAMP            (  4)
          5. BASE VERSION         (  4)
          6. GAME MODE            (  2)
          6. SEASON               (  2)
          7. TOTAL PLAY TIME      (  4)
          8. EMPTY                (  8)

         10. EMPTY                ( 60)
                                  (100)

         10. SAVE NAME            (128) // may contain additional junk data after null terminator
         42. SAVE SUMMARY         (128) // may contain additional junk data after null terminator
         74. DIFFICULTY PRESET    (  4)
         75. EMPTY                ( 56)
                                  (356)
         */
        if (disk.IsEmpty())
            return;

        container.Extra = container.Extra with
        {
            Bytes = container.IsAccount ? disk.ToArray() : disk[META_LENGTH_KNOWN_VANILLA..].ToArray(),
            MetaLength = (uint)(disk.Length),
            SizeDecompressed = decompressed[2],
        };

        if (container.IsAccount)
        {
            container.GameVersion = Meta.GameVersion.Get(this, disk.Length, Constants.META_FORMAT_3);
        }
        if (container.IsSave)
        {
            // Vanilla data always available.
            container.Extra = container.Extra with
            {
                LastWriteTime = DateTimeOffset.FromUnixTimeSeconds(decompressed[4]).ToLocalTime(),
                BaseVersion = (int)(decompressed[5]),
                GameMode = disk.Cast<ushort>(24),
                Season = disk.Cast<ushort>(26),
                TotalPlayTime = decompressed[7],
            };

            // Extended data since Waypoint.
            UpdateContainerWithWaypointMetaInformation(container, disk);

            // GameVersion with BaseVersion only is not 100% accurate but good enough to calculate SaveVersion.
            container.SaveVersion = Meta.SaveVersion.Calculate(container, Meta.GameVersion.Get(container.Extra.BaseVersion));
        }
    }

    #endregion

    // //

    #region Write

    protected override Span<uint> CreateMeta(Container container, ReadOnlySpan<byte> data)
    {
        byte[] buffer;

        if (container.IsAccount)
        {
            buffer = container.Extra.Bytes ?? CreateMetaBuffer(container);

            // Overwrite only SizeDecompressed.
            using var writer = new BinaryWriter(new MemoryStream(buffer));

            writer.Seek(0x8, SeekOrigin.Begin);
            writer.Write(container.Extra.SizeDecompressed); // 4
        }
        else
        {
            buffer = CreateMetaBuffer(container);

            using var writer = new BinaryWriter(new MemoryStream(buffer));

            writer.Write(META_HEADER); // 4
            writer.Write(Constants.META_FORMAT_3); // 4
            writer.Write(container.Extra.SizeDecompressed); // 4
            writer.Write(container.MetaIndex); // 4
            writer.Write((uint)(container.LastWriteTime!.Value.ToUniversalTime().ToUnixTimeSeconds())); // 4
            writer.Write(container.BaseVersion); // 4
            writer.Write((ushort)(container.GameMode)); // 2
            writer.Write((ushort)(container.Season)); // 2
            writer.Write(container.TotalPlayTime); // 4

            // Skip EMPTY.
            writer.Seek(0x8, SeekOrigin.Current); // 8

            // Append buffered bytes that follow META_LENGTH_KNOWN_VANILLA.
            writer.Write(container.Extra.Bytes ?? []); // Extra.Bytes is 60 or 272

            OverwriteWaypointMeta(writer, container);
        }

        return buffer.AsSpan().Cast<byte, uint>();
    }

    #endregion
}
