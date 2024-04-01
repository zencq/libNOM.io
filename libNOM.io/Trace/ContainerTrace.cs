using libNOM.io.Interfaces;

namespace libNOM.io.Trace;


public record class ContainerTrace
{
    #region Global

    public int? BaseVersion { get; internal set; }

    public PresetGameModeEnum? GameMode { get; internal set; }

    public int? SaveVersion { get; internal set; }

    public StoragePersistentSlotEnum? PersistentStorageSlot { get; internal set; }

    public UserIdentification? UserIdentification { get; internal set; }

    public uint? MetaLength { get; internal set; }

    public uint? SizeDecompressed { get; internal set; }

    public uint? SizeDisk { get; internal set; }

    public IPlatform? Platform { get; internal set; }

    #endregion

    #region Microsoft

    public string? MicrosoftIdentifier1 { get; internal set; }

    public string? MicrosoftIdentifier2 { get; internal set; }

    public string? MicrosoftSyncTime { get; internal set; }

    public byte? MicrosoftExtension { get; internal set; }

    public byte? MicrosoftSyncState { get; internal set; }

    public Guid? MicrosoftDirectoryGuid { get; internal set; }

    public long? MicrosoftSize { get; internal set; }

    #endregion

    #region Playstation

    public int? PlaystationOffset { get; internal set; }

    #endregion
}
