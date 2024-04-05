namespace libNOM.io.Trace;


public record class PlatformTrace
{
    #region Global

    public string? DirectoryUID { get; internal set; }

    #endregion

    #region Microsoft

    public int? MicrosoftContainersIndexHeader { get; internal set; }

    public string? MicrosoftContainersIndexProcessIdentifier { get; internal set; }

    public DateTimeOffset? MicrosoftContainersIndexTimestamp { get; internal set; }

    public int? MicrosoftContainersIndexSyncState { get; internal set; }

    public string? MicrosoftContainersIndexAccountGuid { get; internal set; }

    public long? MicrosoftContainersIndexFooter { get; internal set; }

    #endregion
}
