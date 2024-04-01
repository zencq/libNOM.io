using System.Collections.Concurrent;

using libNOM.io.Interfaces;
using libNOM.io.Settings;

using Newtonsoft.Json.Linq;

namespace libNOM.io;


// This partial class contains initialization related code.
public abstract partial class Platform : IPlatform, IEquatable<Platform>
{
    #region Property

    // public //

    public DirectoryInfo Location { get; protected set; }

    public PlatformSettings Settings { get; protected set; }

    // protected //

    protected int AnchorFileIndex { get; set; } = -1;

    #endregion

    #region Getter

    /// <summary>
    /// Gets the index of the matching anchor.
    /// </summary>
    /// <param name="directory"></param>
    /// <returns></returns>
    protected int GetAnchorFileIndex(DirectoryInfo? directory)
    {
        if (directory is not null)
            for (var i = 0; i < PlatformAnchorFilePattern.Length; i++)
                if (directory.GetFiles(PlatformAnchorFilePattern[i]).Length != 0)
                    return i;

        return -1;
    }

    #endregion

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

    protected virtual void InitializePlatformSpecific() { }

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

        var tasks = Enumerable.Range(0, Constants.OFFSET_INDEX + COUNT_SAVES_TOTAL).Select((metaIndex) => Task.Run(() =>
        {
            switch (metaIndex)
            {
                case 0:
                    AccountContainer = InitializeContainer(metaIndex);
                    break;
                case 1:
                    break;
                default:
                    bag.Add(InitializeContainer(metaIndex));
                    break;
            }
        }));
        Task.WaitAll(tasks.ToArray());

        return bag;
    }

    private protected Container InitializeContainer(int metaIndex) => InitializeContainer(metaIndex, null);

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

    public void Load(Container container)
    {
        if (container.IsBackup)
            LoadBackupContainer(container);
        else
            LoadSaveContainer(container);
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
        if (IsUpdateNecessary(container.Extra.GameMode, force))
            container.GameMode = Meta.GameMode.Get(json);

        if (IsUpdateNecessary(container.SaveVersion, force))
            container.SaveVersion = Meta.SaveVersion.Get(json);

        if (IsUpdateNecessary((int)(container.Extra.TotalPlayTime), force))
            container.TotalPlayTime = Meta.TotalPlayTime.Get(json);

        // Finally all remaining values that depend on others.
        if (container.GameMode == PresetGameModeEnum.Seasonal && IsUpdateNecessary(container.Extra.Season, force)) // needs GameMode
            container.Season = Meta.Season.Get(json);

        if (IsUpdateNecessary(container.Extra.BaseVersion, force))
            container.BaseVersion = Meta.BaseVersion.Calculate(container); // needs SaveVersion and GameMode and Season

        if (IsUpdateNecessary((int)(container.GameVersion), force))
            container.GameVersion = Meta.GameVersion.Get(container.BaseVersion, json); // needs BaseVersion

        if (IsUpdateNecessary((int)(container.Extra.DifficultyPreset), force))
            container.Difficulty = Meta.DifficultyPreset.Get(container, json); // needs GameMode and GameVersion

        if (container.IsVersion400Waypoint) // needs GameVersion
        {
            if (IsUpdateNecessary(container.Extra.SaveName, force))
                container.SaveName = Meta.SaveName.Get(json);

            if (IsUpdateNecessary(container.Extra.SaveSummary, force))
                container.SaveSummary = Meta.SaveSummary.Get(json);
        }
    }

    private static bool IsUpdateNecessary(int property, bool force) => force || property <= 0;

    private static bool IsUpdateNecessary(string property, bool force) => force || string.IsNullOrEmpty(property);

    /// <summary>
    /// Updates the specified container with information from the meta file.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="disk"></param>
    /// <param name="decompressed"></param>
    protected abstract void UpdateContainerWithMetaInformation(Container container, ReadOnlySpan<byte> disk, ReadOnlySpan<uint> decompressed);

    protected void UpdateContainerWithWaypointMetaInformation(Container container, ReadOnlySpan<byte> disk)
    {
        if (disk.Length == META_LENGTH_TOTAL_WAYPOINT)
            container.Extra = container.Extra with
            {
                SaveName = disk.Slice(META_LENGTH_KNOWN, Constants.SAVE_RENAMING_LENGTH_MANIFEST).GetStringUntilTerminator(),
                SaveSummary = disk.Slice(META_LENGTH_KNOWN + (Constants.SAVE_RENAMING_LENGTH_MANIFEST * 1), Constants.SAVE_RENAMING_LENGTH_MANIFEST).GetStringUntilTerminator(),
                DifficultyPreset = disk[META_LENGTH_KNOWN + (Constants.SAVE_RENAMING_LENGTH_MANIFEST * 2)],
            };
    }

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
