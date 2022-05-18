namespace libNOM.io.Data;


/// <summary>
/// Holds information for a single user decision for transferring bases.
/// </summary>
public record class BaseUserDecisionData
{
    public bool DoTransfer { get; set; }

#if NET47_OR_GREATER || NETSTANDARD2_0_OR_GREATER
    public string Name { get; internal set; } = null!;
#elif NET5_0_OR_GREATER
    public string Name { get; init; } = null!;
#endif
}
