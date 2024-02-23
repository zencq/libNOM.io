namespace libNOM.io.Models;


/// <summary>
/// Holds information to transfer saves to another platform instance.
/// </summary>
public record class ContainerTransferData
{
#if NETSTANDARD2_0_OR_GREATER || NET6_0
    /// <summary>
    /// Which containers are about the be transferred.
    /// </summary>
    public IEnumerable<Container> Containers { get; set; } = null!;

    /// <summary>
    /// Whether to transfer ownership of bases.
    /// </summary>
    public bool TransferBase { get; set; } = true;

    /// <summary>
    /// Decisions of the user which bases should be transferred (e.g. to avoid issues with uploaded ones).
    /// </summary>
    public Dictionary<string, BaseUserDecisionData> TransferBaseUserDecision { get; set; } = [];

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
#else

    /// <summary>
    /// Which containers are about the be transferred.
    /// </summary>
    public required IEnumerable<Container> Containers { get; set; }

    /// <summary>
    /// Whether to transfer ownership of bases.
    /// </summary>
    public bool TransferBase { get; set; } = true;

    /// <summary>
    /// Decisions of the user which bases should be transferred (e.g. to avoid issues with uploaded ones).
    /// </summary>
    public Dictionary<string, BaseUserDecisionData> TransferBaseUserDecision { get; set; } = [];

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
    public required UserIdentificationData UserIdentification { get; set; }
#endif
}
