using Newtonsoft.Json.Linq;

namespace libNOM.io.Interfaces;


public interface IPlatform
{
    #region Property

    #region Configuration

    /// <summary>
    /// Directory this platform is in.
    /// </summary>
    public DirectoryInfo? Location { get; }

    /// <summary>
    /// Settings used for this platform.
    /// </summary>
    public PlatformSettings Settings { get; }

    #endregion

    #region Flags

    /// <summary>
    /// Whether a save file can be created out of thin air.
    /// </summary>
    public bool CanCreate { get; }

    /// <summary>
    /// Whether a save file can be read.
    /// </summary>
    public bool CanRead { get; }

    /// <summary>
    /// Whether a save file can be written.
    /// </summary>
    public bool CanUpdate { get; }

    /// <summary>
    /// Whether a save file can be deleted.
    /// </summary>
    public bool CanDelete { get; }

    /// <summary>
    /// Whether the platform and the location it is in exists .
    /// </summary>
    public bool Exists { get; }

    /// <summary>
    /// Whether account data were found for this platform.
    /// </summary>
    public bool HasAccountData { get; }

    /// <summary>
    /// Whether the platform is capable of modding.
    /// </summary>
    public bool HasModding { get; }

    /// <summary>
    /// Whether any save files are found and added to the internal collection.
    /// </summary>
    public bool IsLoaded { get; }

    /// <summary>
    /// Whether this platform is on personal computer or not (console otherwise).
    /// </summary>
    public bool IsPersonalComputerPlatform { get; }

    /// <summary>
    /// Whether the game is currently running on this platform.
    /// </summary>
    public bool IsRunning { get; }

    /// <summary>
    /// Whether a valid anchor file was found.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Whether modifying save data requires a restart of the game to see the changes.
    /// For account data this is always necessary.
    /// </summary>
    public bool RestartToApply { get; }

    #endregion

    #region Platform Indicator

    public PlatformEnum PlatformEnum { get; }

    public UserIdentificationData PlatformUserIdentification { get; }

    #endregion

    #endregion

    #region Getter

    #region Container

    /// <summary>
    /// Gets the <see cref="Container"/> with the account data from this platform.
    /// </summary>
    /// <returns></returns>
    public Container? GetAccountContainer();

    /// <summary>
    /// Gets a single <see cref="Container"/> with save data from this platform.
    /// </summary>
    /// <param name="collectionIndex"></param>
    /// <returns></returns>
    public Container? GetSaveContainer(int collectionIndex);

    /// <summary>
    /// Gets all <see cref="Container"/> that exist.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Container> GetExistingContainers();

    /// <summary>
    /// Gets all <see cref="Container"/> that are currently loaded.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Container> GetLoadedContainers();

    /// <summary>
    /// Gets all <see cref="Container"/> for the specified slot.
    /// </summary>
    /// <param name="slotIndex"></param>
    /// <returns></returns>
    public IEnumerable<Container> GetSlotContainers(int slotIndex);

    /// <summary>
    /// Gets all <see cref="Container"/> that are loaded but unsynced.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Container> GetUnsyncedContainers();

    /// <summary>
    /// Gets all <see cref="Container"/> with unresolved changes by the <see cref="FileSystemWatcher"/>.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Container> GetWatcherContainers();

    #endregion

    #region Path

    /// <summary>
    /// Gets the absolute path to the backup directory.
    /// </summary>
    /// <returns></returns>
    public string GetBackupPath();

    /// <summary>
    /// Gets absolute path to the download directory.
    /// </summary>
    /// <returns></returns>
    public string GetDownloadPath();

    #endregion

    /// <summary>
    /// Returns the maximum number of possible save slots.
    /// </summary>
    /// <returns></returns>
    public int GetMaximumSlots();

    #endregion

    #region Setter

    /// <summary>
    /// Updates the instance with a new configuration.
    /// </summary>
    /// <param name="platformSettings"></param>
    public void SetSettings(PlatformSettings platformSettings);

    #endregion

    // // Read / Write

    #region Reload

    /// <summary>
    /// Loads data of a <see cref= "Container"/> in consideration of the loading strategy.
    /// </summary>
    /// <param name="container"></param>
    public void Load(Container container);

    /// <summary>
    /// Rebuilds the container with data from the specified JSON object.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="jsonObject"></param>
    /// <exception cref="ArgumentNullException"/>
    public void Rebuild(Container container, JObject jsonObject);

    /// <summary>
    /// Fully reloads of the specified <see cref="Container"/> and resets the data with those currently on the drive.
    /// </summary>
    /// <param name="container"></param>
    public void Reload(Container container);

    #endregion

    #region Write

    /// <summary>
    /// Writes the data of the specified container to the drive and sets the timestamp to now.
    /// </summary>
    /// <param name="container"></param>
    public void Write(Container container);

    /// <summary>
    /// Writes the data of the specified container to the drive and sets the timestamp to the specified value.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="writeTime"></param>
    public void Write(Container container, DateTimeOffset writeTime);

    #endregion

    // // File Operation

    #region Backup

    /// <summary>
    /// Creates a backup file of the specified <see cref="Container"/>.
    /// </summary>
    /// <param name="container"></param>
    public void Backup(Container container);

    /// <summary>
    /// Restores a backup container by extracting the archive content and reloading the data.
    /// </summary>
    /// <param name="backup"></param>
    public void Restore(Container backup);

    #endregion

    #region Copy

    /// <summary>
    /// Uses a pair of <see cref="Container"/> to copy from one location to the other.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    public void Copy(Container source, Container destination);

    /// <summary>
    /// Uses an enumerable of <see cref="Container"/> pairs to copy each from one location to the other.
    /// </summary>
    /// <param name="operationData"></param>
    public void Copy(IEnumerable<(Container Source, Container Destination)> operationData);

    #endregion

    #region Delete

    /// <summary>
    /// Deletes all files of a single <see cref="Container"/>.
    /// </summary>
    /// <param name="container"></param>
    public void Delete(Container container);

    /// <summary>
    /// Deletes all files of all <see cref="Container"/> in the specified enumerable.
    /// </summary>
    /// <param name="containers"></param>
    public void Delete(IEnumerable<Container> containers);

    #endregion

    #region Move

    /// <summary>
    /// Uses a pair of <see cref="Container"/> to move one location to the other.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    public void Move(Container source, Container destination);

    /// <summary>
    /// Uses an enumerable of <see cref="Container"/> pairs to move each from one location to the other.
    /// </summary>
    /// <param name="operationData"></param>
    public void Move(IEnumerable<(Container Source, Container Destination)> operationData);

    #endregion

    #region Swap

    /// <summary>
    /// Uses a pair of <see cref="Container"/> to swap their locations.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    public void Swap(Container source, Container destination);

    /// <summary>
    /// Uses an enumerable of <see cref="Container"/> pairs to swap their respective locations.
    /// </summary>
    /// <param name="operationData"></param>
    public void Swap(IEnumerable<(Container Source, Container Destination)> operationData);

    #endregion

    #region Transfer

    /// <summary>
    /// Prepares the specified slot for transfer.
    /// </summary>
    /// <param name="sourceSlot"></param>
    /// <returns></returns>
    public ContainerTransferData PrepareTransferSource(int sourceSlot);

    /// <summary>
    /// Ensures that the destination is prepared for the incoming <see cref="Transfer(ContainerTransferData, int)"/>.
    /// </summary>
    /// <param name="destinationSlot"></param>
    public void PrepareTransferDestination(int destinationSlot);

    /// <summary>
    /// Transfers a specified slot to another account or platform according to the prepared <see cref="ContainerTransferData"/>.
    /// Works similar to copy but with additional ownership transfer.
    /// </summary>
    /// <param name="sourceTransferData"></param>
    /// <param name="destinationSlot"></param>
    public void Transfer(ContainerTransferData sourceTransferData, int destinationSlot);

    #endregion

    // // FileSystemWatcher

    #region FileSystemWatcher

    /// <summary>
    /// Resolves automatic decions or those made by the user by reloading the container or mark as unsynced.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="execute"></param>
    public void OnWatcherDecision(Container container, bool execute);

    #endregion
}
