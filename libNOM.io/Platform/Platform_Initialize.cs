using System.Collections.Concurrent;

using libNOM.io.Settings;

using Newtonsoft.Json.Linq;

namespace libNOM.io;


// This partial class contains initialization related code.
public abstract partial class Platform : IPlatform, IEquatable<Platform>
{
    #region Constructor

#pragma warning disable CS8618 // Non-nullable property 'Settings' must contain a non-null value when exiting constructor. Property 'Settings' is set in InitializeComponent.
    public Platform() => InitializeComponent(null, null);

    public Platform(string? path) => InitializeComponent(string.IsNullOrWhiteSpace(path) ? null : new(path), null);

    public Platform(string? path, PlatformSettings? platformSettings) => InitializeComponent(string.IsNullOrWhiteSpace(path) ? null : new(path), platformSettings);

    public Platform(PlatformSettings? platformSettings) => InitializeComponent(null, platformSettings);

    public Platform(DirectoryInfo? directory) => InitializeComponent(directory, null);

    public Platform(DirectoryInfo? directory, PlatformSettings? platformSettings) => InitializeComponent(directory, platformSettings);
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor.

    #endregion

    #region Initialize

    /// <summary>
    /// Workaround to be able to decide when inherited classes initialize their components.
    /// </summary>
    /// <param name="directory"></param>
    /// <param name="platformSettings"></param>
    private void InitializeComponent(DirectoryInfo? directory, PlatformSettings? platformSettings)
    {
        AnchorFileIndex = GetAnchorFileIndex(directory);
        Location = directory!; // force with ! even if null as it would be invalid anyway
        Settings = platformSettings ?? new();

        // Stop if no directory or no anchor found.
        if (!IsValid)
            return;

        InitializeWatcher();

        InitializePlatformSpecific();
        InitializePlatform();

        if (!Settings.Trace)
            return;

        InitializeTrace();
    }

    protected virtual void InitializePlatformSpecific() { } // empty as not used by all platforms

    /// <summary>
    /// Generates all related containers as well as the user identification.
    /// </summary>
    private void InitializePlatform()
    {
        if (Settings.LoadingStrategy == LoadingStrategyEnum.Empty)
            return;

        SaveContainerCollection.Clear();
        SaveContainerCollection.AddRange(GenerateContainerCollection());
        SaveContainerCollection.Sort();

        UpdateUserIdentification();
        EnableWatcher();
    }

    /// <summary>
    /// Generates a <see cref="Container"/> collection with an entry for each possible file.
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerable<Container> GenerateContainerCollection()
    {
        var bag = new ConcurrentBag<Container>();

        var tasks = Enumerable.Range(0, Constants.OFFSET_INDEX + MAX_SAVE_TOTAL).Select((metaIndex) => Task.Run(() =>
        {
            switch (metaIndex)
            {
                case 0:
                    AccountContainer = InitializeContainer(metaIndex, null);
                    break;
                case 1:
                    break;
                default:
                    bag.Add(InitializeContainer(metaIndex, null));
                    break;
            }
        }));
        Task.WaitAll([.. tasks]);

        return bag;
    }

    private protected Container InitializeContainer(int metaIndex, ContainerExtra? extra)
    {
        var container = CreateContainer(metaIndex, extra);

        if (container.IsSave && Settings.LoadingStrategy < LoadingStrategyEnum.Full)
            BuildContainerHollow(container);
        else
            BuildContainerFull(container); // account data always full

        GenerateBackupCollection(container);

        return container;
    }

    /// <summary>
    /// Creates a <see cref="Container"/> with basic data.
    /// </summary>
    /// <param name="metaIndex"></param>
    /// <param name="extra">An optional object with additional data necessary for proper creation.</param>
    /// <returns></returns>
    private protected abstract Container CreateContainer(int metaIndex, ContainerExtra? extra); // private protected as PlatformExtra is internal

    public void Load(IContainer container)
    {
        var nonIContainer = container.ToContainer();
        if (nonIContainer.IsBackup)
            LoadBackupContainer(nonIContainer);
        else
            LoadSaveContainer(nonIContainer);
    }

    /// <summary>
    /// Loads data of the specified save.
    /// </summary>
    /// <param name="container"></param>
    private void LoadSaveContainer(Container container)
    {
        if (Settings.LoadingStrategy < LoadingStrategyEnum.Current)
            Settings = Settings with { LoadingStrategy = LoadingStrategyEnum.Current };

        // Unloads data by removing the reference to the JSON object.
        if (Settings.LoadingStrategy == LoadingStrategyEnum.Current && container.IsSave)
        {
            var loadedContainers = SaveContainerCollection.Where(i => i.IsLoaded && !i.Equals(container));
            foreach (var loadedContainer in loadedContainers)
                loadedContainer.SetJsonObject(null);
        }

        BuildContainerFull(container);
    }

    /// <summary>
    /// Builds a <see cref="Container"/> by loading from disk and processing it by deserializing the data.
    /// </summary>
    /// <param name="container"></param>
    protected void BuildContainerFull(Container container)
    {
        var binary = LoadContainer(container);

        if (container.IsCompatible && Deserialize(container, binary) is JObject jsonObject)
            UpdateContainerWithJsonInformation(container, jsonObject);
    }

    /// <summary>
    /// Builds a <see cref="Container"/> by loading from disk and processing it by extracting from the string representation.
    /// </summary>
    /// <param name="container"></param>
    protected void BuildContainerHollow(Container container)
    {
        var binary = LoadContainer(container);

        if (container.IsCompatible)
            UpdateContainerWithJsonInformation(container, binary.GetString(), false);
    }

    #endregion

    #region Process

    // Json

    /// <summary>
    /// Processes the read JSON object and fills the properties of the container.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="jsonObject"></param>
    private void UpdateContainerWithJsonInformation(Container container, JObject jsonObject)
    {
        // Setting the JSON will enable all properties.
        container.SetJsonObject(jsonObject);

        // No need to do this for AccountData.
        if (container.IsSave)
        {
            container.UserIdentification = GetUserIdentification(jsonObject);
            UpdateUserIdentification();
        }

        // If we are in here, the container is in sync (again).
        container.IsSynced = true;
    }

    /// <inheritdoc cref="UpdateContainerWithJsonInformation(Container, JObject)"/>
    /// <param name="json"></param>
    /// <param name="force"></param>
    private static void UpdateContainerWithJsonInformation(Container container, string json, bool force)
    {
        // Independent values first.
        if (container.Extra.GameMode.IsUpdateNecessary(force))
            container.GameMode = Meta.GameMode.Get(json);

        if (container.SaveVersion.IsUpdateNecessary(force))
            container.SaveVersion = Meta.SaveVersion.Get(json);

        if (container.Extra.TotalPlayTime.IsUpdateNecessary(force))
            container.TotalPlayTime = Meta.TotalPlayTime.Get(json);

        // Finally all remaining values that depend on others.
        if (container.GameMode == PresetGameModeEnum.Seasonal && container.Extra.Season.IsUpdateNecessary(force)) // needs GameMode
            container.Season = Meta.Season.Get(json);

        if (container.Extra.BaseVersion.IsUpdateNecessary(force))
            container.BaseVersion = Meta.BaseVersion.Calculate(container); // needs SaveVersion and GameMode and Season

        if (((int)(container.GameVersion)).IsUpdateNecessary(force))
            container.GameVersion = Meta.GameVersion.Get(container.BaseVersion, json); // needs BaseVersion

        if (container.Extra.DifficultyPreset.IsUpdateNecessary(force))
            container.Difficulty = Meta.DifficultyPreset.Get(container, json); // needs GameMode and GameVersion

        // Extend with values added in later versions.
        if (container.IsVersion400Waypoint) // needs GameVersion
            UpdateContainerWithWaypointJsonInformation(container, json, force);

        if (container.IsVersion450Omega) // needs GameVersion
            UpdateContainerWithOmegaMetaInformation(container, json);
    }

    protected static void UpdateContainerWithWaypointJsonInformation(Container container, string json, bool force)
    {
        if (container.Extra.SaveName.IsUpdateNecessary(force))
            container.SaveName = Meta.SaveName.Get(json);

        if (container.Extra.SaveSummary.IsUpdateNecessary(force))
            container.SaveSummary = Meta.SaveSummary.Get(json);
    }

    protected static void UpdateContainerWithOmegaMetaInformation(Container container, string json)
    {
        container.CanSwitchContext = Meta.Context.CanSwitch(json);

        container.ActiveContext = Meta.Context.GetActive(json); // needs CanSwitchContext
    }

    // Meta

    /// <summary>
    /// Updates the specified container with information from the meta file.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="disk"></param>
    /// <param name="decompressed"></param>
    protected virtual void UpdateContainerWithMetaInformation(Container container, ReadOnlySpan<byte> disk, ReadOnlySpan<uint> decompressed)
    {
        if (container.IsAccount)
            UpdateAccountContainerWithMetaInformation(container, disk, decompressed);
        else
            UpdateSaveContainerWithMetaInformation(container, disk, decompressed);
    }

    protected virtual void UpdateAccountContainerWithMetaInformation(Container container, ReadOnlySpan<byte> disk, ReadOnlySpan<uint> decompressed) { } // empty as not used by all platforms

    protected virtual void UpdateSaveContainerWithMetaInformation(Container container, ReadOnlySpan<byte> disk, ReadOnlySpan<uint> decompressed) { } // empty as not used by all platforms

    protected void UpdateSaveContainerWithWaypointMetaInformation(Container container, ReadOnlySpan<byte> disk)
    {
        container.Extra = container.Extra with
        {
            SaveName = disk.Slice(META_LENGTH_BEFORE_NAME, Constants.SAVE_RENAMING_LENGTH_MANIFEST).GetStringUntilTerminator(),
            SaveSummary = disk.Slice(META_LENGTH_BEFORE_SUMMARY, Constants.SAVE_RENAMING_LENGTH_MANIFEST).GetStringUntilTerminator(),
            DifficultyPreset = disk[META_LENGTH_BEFORE_DIFFICULTY_PRESET], // just a single byte to be able to use a common method for all platforms
        };
    }

    protected virtual void UpdateSaveContainerWithWorldsMetaInformation(Container container, ReadOnlySpan<byte> disk, ReadOnlySpan<uint> decompressed)
    {
        container.Extra = container.Extra with
        {
            SaveName = disk.Slice(META_LENGTH_BEFORE_NAME, Constants.SAVE_RENAMING_LENGTH_MANIFEST).GetStringUntilTerminator(),
            SaveSummary = disk.Slice(META_LENGTH_BEFORE_SUMMARY, Constants.SAVE_RENAMING_LENGTH_MANIFEST).GetStringUntilTerminator(),
            DifficultyPreset = disk[META_LENGTH_BEFORE_DIFFICULTY_PRESET], // keep it a single byte to get the correct value if migrated but not updated
            LastWriteTime = DateTimeOffset.FromUnixTimeSeconds(decompressed[META_LENGTH_BEFORE_TIMESTAMP / 4]).ToLocalTime(),
        };
    }

    // Data

    /// <summary>
    /// Updates the specified container with information from the data file.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="disk"></param>
    /// <param name="decompressed"></param>
    protected virtual void UpdateContainerWithDataInformation(Container container, ReadOnlySpan<byte> disk, ReadOnlySpan<byte> decompressed)
    {
        container.Extra = container.Extra with
        {
            SizeDecompressed = (uint)(decompressed.Length),
            SizeDisk = (uint)(disk.Length),
        };
    }

    #endregion
}
