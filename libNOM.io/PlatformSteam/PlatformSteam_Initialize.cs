using libNOM.io.Settings;

namespace libNOM.io;


/// <summary>
/// Implementation for the Steam platform.
/// </summary>
// This partial class contains initialization related code.
public partial class PlatformSteam : Platform
{
    #region Constructor

    public PlatformSteam() : base() { }

    public PlatformSteam(string? path) : base(path) { }

    public PlatformSteam(string? path, PlatformSettings? platformSettings) : base(path, platformSettings) { }

    public PlatformSteam(PlatformSettings? platformSettings) : base(platformSettings) { }

    public PlatformSteam(DirectoryInfo? directory) : base(directory) { }

    public PlatformSteam(DirectoryInfo? directory, PlatformSettings? platformSettings) : base(directory, platformSettings) { }

    #endregion

    #region Initialize

    protected override void InitializeWatcher()
    {
        base.InitializeWatcher();

        // Files can have 0 or 1 or 2 numbers in its name.
#if NETSTANDARD2_0_OR_GREATER
        _watcher.Filter = PlatformAnchorFilePattern[AnchorFileIndex].Replace("??", "*");
#else
        _watcher.Filters.Add(PlatformAnchorFilePattern[AnchorFileIndex].Replace("??", "?"));
        _watcher.Filters.Add(PlatformAnchorFilePattern[AnchorFileIndex].Replace("??", string.Empty));
#endif
    }

    /// <seealso href="https://help.steampowered.com/en/faqs/view/2816-BE67-5B69-0FEC"/>
    protected override void InitializePlatformSpecific()
    {
        // Extract UID from directory name if possible.
        if (Location.Name.Length == 20 && Location.Name.StartsWith(ACCOUNT_PATTERN[..^1]) && Location.Name[(ACCOUNT_PATTERN.Length - 1)..].All(char.IsDigit))
            _uid = Location.Name[3..]; // remove "st_"
    }

    private protected override Container CreateContainer(int metaIndex, ContainerExtra? _)
    {
        var name = metaIndex == 0 ? "accountdata.hg" : $"save{(metaIndex == Constants.OFFSET_INDEX ? string.Empty : metaIndex - 1)}.hg";
        var data = new FileInfo(Path.Combine(Location.FullName, name));

        return new Container(metaIndex, this)
        {
            DataFile = data,
            MetaFile = new FileInfo(Path.Combine(Location.FullName, $"mf_{name}")),
            /// Additional values will be set in <see cref="UpdateContainerWithMetaInformation"/> and <see cref="Platform.UpdateContainerWithDataInformation"/>.
            Extra = new()
            {
                LastWriteTime = data.LastWriteTime,
            },
        };
    }

    #endregion

    #region Process

    protected override void UpdateContainerWithMetaInformation(Container container, ReadOnlySpan<byte> disk, ReadOnlySpan<uint> decompressed)
    {
        /**
          0. META HEADER          (  4)
          1. META FORMAT          (  4)
          2. SPOOKY HASH          ( 16) // META_FORMAT_1
          6. SHA256 HASH          ( 32) // META_FORMAT_1
         14. DECOMPRESSED SIZE    (  4) // META_FORMAT_3
         15. COMPRESSED SIZE      (  4) // META_FORMAT_3
         16. PROFILE HASH         (  4) // META_FORMAT_0
         17. BASE VERSION         (  4) // META_FORMAT_3
         18. GAME MODE            (  2) // META_FORMAT_3
         18. SEASON               (  2) // META_FORMAT_3
          19. TOTAL PLAY TIME       (  8) // META_FORMAT_4
         20. EMPTY                (  8)

         22. EMPTY                ( 16) // META_FORMAT_1
                                  (104)

         22. SAVE NAME            (128) // META_FORMAT_3 // may contain additional junk data after null terminator
         54. SAVE SUMMARY         (128) // META_FORMAT_3 // may contain additional junk data after null terminator

         86. DIFFICULTY PRESET    (  1) // META_FORMAT_2
         86. EMPTY                ( 15) // META_FORMAT_2 // may contain additional junk data
                                  (360)

         86. DIFFICULTY PRESET    (  4) // META_FORMAT_3
         87. SLOT IDENTIFIER      (  8) // META_FORMAT_3
         89. TIMESTAMP            (  4) // META_FORMAT_3
         90. META FORMAT          (  4) // META_FORMAT_3
         91. EMPTY                ( 20) // META_FORMAT_3
                                  (384)
         */

        // Do not write wrong data in case a step before failed.
        if (decompressed.TryGetValue(0, out var header) && header == META_HEADER)
        {
            // Vanilla metadata always available but not always set depending on the META_FORMAT.
            container.Extra = container.Extra with
            {
                Bytes = disk[META_LENGTH_KNOWN_VANILLA..].ToArray(),
                SizeDecompressed = decompressed[14],
                BaseVersion = (int)(decompressed[17]),
                GameMode = disk.Cast<ushort>(72),
                Season = disk.Cast<ushort>(74),
                TotalPlayTime = disk.Cast<ulong>(76),
            };

            base.UpdateContainerWithMetaInformation(container, disk, decompressed);
        }

        // Size is save to write always.
        container.Extra = container.Extra with { MetaLength = (uint)(disk.Length) };
    }

    protected override void UpdateAccountContainerWithMetaInformation(Container container, ReadOnlySpan<byte> disk, ReadOnlySpan<uint> decompressed)
    {
        container.GameVersion = Meta.GameVersion.Get(this, disk.Length, decompressed[1]);
    }

    protected override void UpdateSaveContainerWithMetaInformation(Container container, ReadOnlySpan<byte> disk, ReadOnlySpan<uint> decompressed)
    {
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
