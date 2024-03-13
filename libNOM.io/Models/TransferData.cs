namespace libNOM.io.Models;


/// <summary>
/// Holds information to transfer saves to another platform instance.
/// </summary>
public record class TransferData(IEnumerable<Container> Containers, bool TransferBase, Dictionary<string, UserDecision> TransferBaseUserDecision, bool TransferByteBeat, bool TransferDiscovery, bool TransferSettlement, UserIdentification UserIdentification);
