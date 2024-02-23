namespace libNOM.io.Models;


/// <summary>
/// Holds information about a single user decision whether to transfer a base.
/// </summary>
public record class UserDecision(bool DoTransfer, string Name);
