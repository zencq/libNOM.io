namespace libNOM.io.Data;


/// <summary>
/// Holds information about the users identity.
/// </summary>
public record class UserIdentificationData
{
    #region Property

    public string? LID { get; set; }

    public string? UID { get; set; }

    public string? USN { get; set; }

    public string? PTK { get; set; }

    #endregion

    /// <summary>
    /// Checks whether this information are complete.
    /// </summary>
    /// <returns></returns>
    public bool IsComplete()
    {
        return new[] { LID, UID, USN, PTK }.All(property => !string.IsNullOrEmpty(property));
    }
}
