namespace libNOM.io.Data;


/// <summary>
/// Holds information to transfer saves to another platform instance.
/// </summary>
public record class ContainerTransferData
{
    /// <summary>
    /// Which containers are about the be transferred.
    /// </summary>
    public IEnumerable<Container> Containers { get; set; } = null!;

    /// <summary>
    /// Whether to transfer ownership of bases.
    /// </summary>
    public bool TransferBase { get; set; } = true;

    /// <summary>
    /// Data about all bases in <see cref="Containers"/> which should not be modified after preperation.
    /// </summary>
    public Dictionary<string, bool> TransferBaseReadonly { get; set; } = new();

    /// <summary>
    /// Decisions of the user which bases should be transferred to avoid issues with uploaded ones.
    /// </summary>
    public Dictionary<string, BaseUserDecisionData> TransferBaseUserDecision { get; set; } = new();

    /// <summary>
    /// Whether to transfer ownership of the ByteBeat library.
    /// </summary>
    public bool TransferBytebeat { get; set; } = true;

    /// <summary>
    /// Whether to transfer ownership of discoveries.
    /// </summary>
    public bool TransferDiscovery { get; set; } = true;

    /// <summary>
    /// Whether to transfer ownership of the settlement.
    /// </summary>
    public bool TransferSettlement { get; set; } = true;

    /// <summary>
    /// Identification of the user in the source platform.
    /// </summary>
    public UserIdentificationData UserIdentification { get; set; } = null!;
}
