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
    /// Whether saves can be created out of thin air.
    /// </summary>
    public bool CanCreate { get; }

    /// <summary>
    /// Whether saves can be read.
    /// </summary>
    public bool CanRead { get; }

    /// <summary>
    /// Whether existing saves be updated.
    /// </summary>
    public bool CanUpdate { get; }

    /// <summary>
    /// Whether saves can be deleted.
    /// </summary>
    public bool CanDelete { get; }

    /// <summary>
    /// Whether the location for the platform exists.
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
    /// Whether the game is currently running on this platform (only if not a console).
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

    /// <summary>
    /// Defines the platform via enum.
    /// </summary>
    public PlatformEnum PlatformEnum { get; }

    /// <summary>
    /// Identification of the user including username and some ids.
    /// </summary>
    public UserIdentification PlatformUserIdentification { get; }

    #endregion

    #endregion

    // //

    #region Getter

    /// <summary>
    /// Gets the <see cref="Container"/> with the account data.
    /// </summary>
    /// <returns></returns>
    public Container? GetAccountContainer();

    /// <summary>
    /// Gets a specific <see cref="Container"/> with save data.
    /// </summary>
    /// <param name="index">The CollectionIndex of the save to get.</param>
    /// <returns></returns>
    public Container? GetSaveContainer(int index);

    /// <summary>
    /// Gets all possible <see cref="Container"/> with save data, that then can be filtered further.
    /// Here are some examples:
    /// <code>Where(i => i.Exists)</code> to get those that are actually exist.
    /// <code>Where(i => i.IsLoaded)</code> to get those that are actually loaded.
    /// <code>Where(i => i.SlotIndex == slotIndex)</code> to get those of the specified slot.
    /// <code>Where(i => i.IsLoaded && !i.IsSynced)</code> to get those that have unsaved changes.
    /// <code>Where(i => i.HasWatcherChange)</code> to get those that have unresolved changes detected by the FileSystemWatcher.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Container> GetSaveContainers();

    #endregion

    #region Setter

    /// <summary>
    /// Updates the instance with a new settings configuration.
    /// </summary>
    /// <param name="platformSettings"></param>
    public void SetSettings(PlatformSettings platformSettings);

    #endregion

    // // Read / Write

    #region Load

    /// <summary>
    /// Loads data of a <see cref= "Container"/> in consideration of the loading strategy.
    /// </summary>
    /// <param name="container"></param>
    public void Load(Container container);

    /// <summary>
    /// Fully reloads the specified <see cref="Container"/> and resets the data to those currently on the drive.
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
    /// Uses a pair of <see cref="Container"/>s to copy from one location to the other.
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
    /// Deletes all files of all <see cref="Container"/>s in the specified enumerable.
    /// </summary>
    /// <param name="containers"></param>
    public void Delete(IEnumerable<Container> containers);

    #endregion

    #region Move

    /// <summary>
    /// Uses a pair of <see cref="Container"/>s to move one location to the other.
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
    /// Uses a pair of <see cref="Container"/>s to swap their locations.
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
    /// Gets all necessary data from the specified slot in the source.
    /// </summary>
    /// <param name="sourceSlotIndex"></param>
    /// <returns></returns>
    public TransferData GetSourceTransferData(int sourceSlotIndex);

    /// <summary>
    /// Transfers a specified slot to another account or platform according to the prepared <see cref="TransferData"/>.
    /// Works similar to copy but with additional ownership transfer.
    /// </summary>
    /// <param name="sourceTransferData"></param>
    /// <param name="destinationSlotIndex"></param>
    public void Transfer(TransferData sourceTransferData, int destinationSlotIndex);

    #endregion

    // // FileSystemWatcher

    #region FileSystemWatcher

    /// <summary>
    /// Resolves automatic decisions or those made by the user by reloading the container or mark as not synced.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="execute"></param>
    public void OnWatcherDecision(Container container, bool execute);

    #endregion
}
