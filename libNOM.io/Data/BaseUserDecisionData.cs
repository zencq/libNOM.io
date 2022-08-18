namespace libNOM.io.Data;


/// <summary>
/// Holds information for a single user decision for transferring bases.
/// </summary>
public record class BaseUserDecisionData
{
    public bool DoTransfer { get; set; }

#if NETSTANDARD2_0_OR_GREATER
    public string Name { get; internal set; } = null!;
#else
    public string Name { get; init; } = null!;
#endif
}
