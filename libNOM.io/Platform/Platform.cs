namespace libNOM.io;


/// <summary>
/// Abstract base for all platforms which just hook into the methods they need.
/// </summary>
// This partial class contains some general code.
public abstract partial class Platform : IPlatform, IEquatable<Platform>
{
    #region Constant

    protected virtual int MAX_SAVE_SLOTS { get; } = Constants.MAX_SAVE_SLOTS; // overridable for compatibility with old PlayStation format
    protected virtual int MAX_SAVE_PER_SLOT { get; } = Constants.MAX_SAVE_PER_SLOT; // overridable in case it will be necessary in the future
    internal int MAX_SAVE_TOTAL => MAX_SAVE_SLOTS * MAX_SAVE_PER_SLOT; // { get; } // compute here in case one of the values has been overridden

    protected virtual int META_LENGTH_KNOWN_VANILLA { get; } = -1; // all metadata at the beginning of a file before the first extension in Waypoint
    protected virtual int META_LENGTH_KNOWN_NAME => META_LENGTH_KNOWN_VANILLA + Constants.SAVE_RENAMING_LENGTH_MANIFEST; // { get; } // ? + 128
    protected virtual int META_LENGTH_KNOWN_SUMMARY => META_LENGTH_KNOWN_NAME + Constants.SAVE_RENAMING_LENGTH_MANIFEST; // { get; } // ? + 128 // ? + 256
    protected virtual int META_LENGTH_KNOWN_IDENTIFIER => META_LENGTH_KNOWN_SUMMARY + 4 + 8; // { get; } // ? + 4 + 8 // ? + 268

    internal abstract int META_LENGTH_TOTAL_VANILLA { get; }
    internal abstract int META_LENGTH_TOTAL_WAYPOINT { get; }
    internal abstract int META_LENGTH_TOTAL_WORLDS_PART_I { get; }
    internal abstract int META_LENGTH_TOTAL_WORLDS_PART_II { get; }

    #endregion

    #region IEquatable

    public override bool Equals(object? obj)
    {
        return Equals(obj as Platform);
    }

    public bool Equals(Platform? other)
    {
        return (this.PlatformEnum, this.PlatformUserIdentification.UID, this.Location?.FullName) == (other?.PlatformEnum, other?.PlatformUserIdentification.UID, other?.Location?.FullName);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode() + PlatformEnum.GetHashCode() + Location.GetHashCode();
    }

    #endregion

    #region ToString

    public override string ToString()
    {
        if (string.IsNullOrEmpty(PlatformUserIdentification.UID))
            return $"{PlatformEnum}";

        return $"{PlatformEnum} {PlatformUserIdentification.UID}";
    }

    #endregion
}
