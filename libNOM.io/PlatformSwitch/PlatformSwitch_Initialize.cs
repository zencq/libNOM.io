using libNOM.io.Settings;

namespace libNOM.io;


/// <summary>
/// Implementation for the Nintendo Switch platform.
/// </summary>
// This partial class contains initialization related code.
public partial class PlatformSwitch : Platform
{
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

         75. SLOT IDENTIFIER      (  8)
         77. TIMESTAMP            (  4)
         78. META FORMAT          (  4)
         79. EMPTY                ( 56)
                                  (372)
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
            container.GameVersion = Meta.GameVersion.Get(this, disk.Length, Constants.META_FORMAT_2);
        }
        else if (container.IsSave)
            UpdateSaveContainerWithMetaInformation(container, disk, decompressed);
    }

    protected void UpdateSaveContainerWithMetaInformation(Container container, ReadOnlySpan<byte> disk, ReadOnlySpan<uint> decompressed)
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

        // Extended metadata since Waypoint 4.00.
        if (disk.Length == META_LENGTH_TOTAL_WAYPOINT)
            UpdateSaveContainerWithWaypointMetaInformation(container, disk);

        // Extended metadata since Worlds Part I 5.00.
        if (disk.Length == META_LENGTH_TOTAL_WORLDS)
            UpdateSaveContainerWithWorldsPart1MetaInformation(container, disk, decompressed);

        // GameVersion with BaseVersion only is not 100% accurate but good enough to calculate SaveVersion.
        container.SaveVersion = Meta.SaveVersion.Calculate(container, Meta.GameVersion.Get(container.Extra.BaseVersion));
    }

    #endregion
}
