using libNOM.io.Settings;

namespace libNOM.io;


// This partial class contains initialization related code.
public partial class PlatformPlaystation : Platform
{
    // //

    #region Constructor

    /// <summary>
    /// Special case for <see cref="PlatformCollection.AnalyzeFile(string)"/> to be able to use selected methods with an empty initialization.
    /// </summary>
    /// <param name="usesSaveWizard"></param>
    internal PlatformPlaystation(bool usesSaveWizard, PlatformSettings? platformSettings) : base(platformSettings)
    {
        _usesSaveStreaming = true;
        _usesSaveWizard = usesSaveWizard;
    }

    public PlatformPlaystation() : base() { }

    public PlatformPlaystation(string? path) : base(path) { }

    public PlatformPlaystation(string? path, PlatformSettings? platformSettings) : base(path, platformSettings) { }

    public PlatformPlaystation(PlatformSettings? platformSettings) : base(platformSettings) { }

    public PlatformPlaystation(DirectoryInfo? directory) : base(directory) { }

    public PlatformPlaystation(DirectoryInfo? directory, PlatformSettings? platformSettings) : base(directory, platformSettings) { }

    #endregion

    #region Initialize

    protected override void InitializePlatformSpecific()
    {
        if (AnchorFileIndex == 1) // memory.dat
        {
            _memorydat = new FileInfo(Path.Combine(Location.FullName, "memory.dat"));
            _lastWriteTime = _memorydat.LastWriteTime;
        }
        else
        {
            _usesSaveStreaming = true;
        }

        // Get first file that is not account data if not _memorydat.
        var f = _memorydat ?? Location.EnumerateFiles(PlatformAnchorFilePattern[AnchorFileIndex]).FirstOrDefault(i => !i.Name.Contains("00"));
        if (f is not null)
        {
            using var reader = new BinaryReader(File.Open(f.FullName, FileMode.Open, FileAccess.Read, FileShare.Read));
            _usesSaveWizard = reader.ReadBytes(SAVEWIZARD_HEADER_BINARY.Length).SequenceEqual(SAVEWIZARD_HEADER_BINARY);
        }
    }

    private protected override Container CreateContainer(int metaIndex, ContainerExtra? _)
    {
        if (_usesSaveStreaming)
        {
            var data = new FileInfo(Path.Combine(Location.FullName, $"savedata{metaIndex:D2}.hg"));
            var meta = new FileInfo(Path.Combine(Location.FullName, $"manifest{metaIndex:D2}.hg"));

            // AccountData may have an additional meta/manifest file (for SaveWizard only).
            // Otherwise the meta data are at the beginning of the data file itself (for SaveWizard only).
            return new Container(metaIndex, this)
            {
                DataFile = data,
                MetaFile = metaIndex == 0 ? (meta.Exists ? meta : null) : (_usesSaveWizard ? data : null),
                /// Additional values will be set in <see cref="UpdateContainerWithMetaInformation"/> and <see cref="UpdateContainerWithDataInformation"/>.
                Extra = new()
                {
                    LastWriteTime = data.LastWriteTime,
                },
            };
        }

        return new Container(metaIndex, this)
        {
            DataFile = _memorydat,
            MetaFile = _memorydat,
            /// Additional values will be set in <see cref="UpdateContainerWithMetaInformation"/> and <see cref="UpdateContainerWithDataInformation"/>.
            Extra = new(),
        };
    }

    #endregion

    #region Process

    protected override void UpdateContainerWithMetaInformation(Container container, ReadOnlySpan<byte> disk, ReadOnlySpan<uint> decompressed)
    {
        if (_usesSaveStreaming)
        {
            if (container.IsAccount && container.MetaFile?.Exists == true)
            {
                container.Extra = GetAccountStreamingExtra(container, disk, decompressed);
            }
            else if (_usesSaveWizard)
            {
                container.Extra = GetWizardStreamingExtra(container, decompressed);
            }
        }
        else
        {
            container.Extra = GetLegacyExtra(container, decompressed);
        }
    }

    private ContainerExtra GetAccountStreamingExtra(Container container, ReadOnlySpan<byte> disk, ReadOnlySpan<uint> decompressed)
    {
        return container.Extra with
        {
            Bytes = disk.ToArray(),
            MetaLength = decompressed[2],
            SizeDecompressed = decompressed[2],
            SizeDisk = decompressed[2],

            PlaystationOffset = 0,
        };
    }

    private ContainerExtra GetWizardStreamingExtra(Container container, ReadOnlySpan<uint> decompressed)
    {
        /**
          0. META HEADER          (  8) // here the same structure as used at the beginning of the memory.dat
          2. CONST (2)            (  4)
          3. META OFFSET          (  4)
          4. CONST (1)            (  4)
          5. COMPRESSED SIZE      (  4)
          6. EMPTY                ( 40)
         16. EMPTY                (  4) // here the same structure as the old memory.dat format starts but with many empty values
         17. META FORMAT          (  4)
         18. EMPTY                ( 20)
         23. DECOMPRESSED SIZE    (  4)
         24. EMPTY                (  4)
         25. CONST (1)            (  4)
         26. EMPTY                (  8)
                                  (112)
         */
        return container.Extra with
        {
            Bytes = new byte[decompressed[23]],
            MetaLength = (uint)(META_LENGTH_TOTAL_WAYPOINT),
            SizeDecompressed = decompressed[23],
            SizeDisk = decompressed[5],

            PlaystationOffset = META_LENGTH_TOTAL_WAYPOINT,
        };
    }

    private ContainerExtra GetLegacyExtra(Container container, ReadOnlySpan<uint> decompressed)
    {
        /**
          0. META HEADER          ( 4)
          1. META FORMAT          ( 4)
          2. COMPRESSED SIZE      ( 4)
          3. CHUNK OFFSET         ( 4)
          4. CHUNK SIZE           ( 4)
          5. META INDEX           ( 4)
          6. TIMESTAMP            ( 4)
          7. DECOMPRESSED SIZE    ( 4)
                                  (32)

          8. SAVEWIZARD OFFSET    ( 4)
          9. CONST (1)            ( 4)
         10. EMPTY                ( 8)
                                  (48)
         */
        if (decompressed.IsEmpty || decompressed[3] == 0)
        {
            container.Exists = false; // force false to overwrite existing of DataFile
            return container.Extra;
        }

        return container.Extra with
        {
            Bytes = new byte[decompressed[MEMORYDAT_META_INDEX_LENGTH]], // either COMPRESSED SIZE or DECOMPRESSED SIZE depending on SaveWizard usage
            MetaLength = (uint)(META_LENGTH_TOTAL_VANILLA),
            SizeDisk = decompressed[2],
            SizeDecompressed = decompressed[7],
            LastWriteTime = DateTimeOffset.FromUnixTimeSeconds(decompressed[6]).ToLocalTime(),

            PlaystationOffset = (int)(decompressed[MEMORYDAT_META_INDEX_OFFSET]), // either CHUNK OFFSET or SAVEWIZARD OFFSET depending on SaveWizard usage
        };
    }

    // Data

    protected override void UpdateContainerWithDataInformation(Container container, ReadOnlySpan<byte> disk, ReadOnlySpan<byte> decompressed)
    {
        // Sizes other than for AccountData need to be set directly in CompressData() as the compressed data wont be returned if _usesSaveWizard.
        if (container.IsAccount)
            container.Extra = container.Extra with
            {
                SizeDecompressed = (uint)(decompressed.Length),
                SizeDisk = (uint)(disk.Length),
            };
        // Save memory by only storing it when necessary.
        else if (!_usesSaveStreaming)
            container.Extra = container.Extra with
            {
                Bytes = disk.ToArray(),
            };
    }

    #endregion
}
