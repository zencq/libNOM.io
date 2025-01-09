namespace libNOM.io;


/// <summary>
/// Implementation for the Nintendo Switch platform.
/// </summary>
// This partial class contains FileSystemWatcher related code.
public partial class PlatformSwitch : Platform
{
    // Accessor

    #region Getter

    protected override IEnumerable<Container> GetCacheEvictionContainers(string name)
    {
        return SaveContainerCollection.Where(i => i.MetaFile?.Name.Equals(name, StringComparison.OrdinalIgnoreCase) == true);
    }

    #endregion
}
