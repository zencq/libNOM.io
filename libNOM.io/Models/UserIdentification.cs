namespace libNOM.io.Models;


/// <summary>
/// Holds information about the users identity.
/// </summary>
public record class UserIdentification
{
    #region Property

    public string? LID { get; set; }

    public string? UID { get; set; }

    public string? USN { get; set; }

    public string? PTK { get; set; }

    #endregion

    /// <summary>
    /// Checks whether the data are complete.
    /// </summary>
    /// <returns></returns>
    public bool IsComplete() => new[] { LID, UID, USN, PTK }.All(i => !string.IsNullOrEmpty(i));
}
