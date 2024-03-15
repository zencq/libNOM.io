using System.Collections.Concurrent;

using libNOM.io.Interfaces;

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

    public Platform(string path) => InitializeComponent(new(path), null);

    public Platform(string path, PlatformSettings platformSettings) => InitializeComponent(new(path), platformSettings);

    public Platform(DirectoryInfo directory) => InitializeComponent(directory, null);

    public Platform(DirectoryInfo directory, PlatformSettings platformSettings) => InitializeComponent(directory, platformSettings);
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor.

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
    }

    #endregion

    #region Initialize

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
            if (metaIndex == 0)
            {
                AccountContainer = CreateContainer(metaIndex);
                BuildContainerFull(AccountContainer); // always full
            }
            else if (metaIndex > 1) // skip index 1
            {
                var container = CreateContainer(metaIndex);

                if (Settings.LoadingStrategy < LoadingStrategyEnum.Full)
                    BuildContainerHollow(container);
                else
                    BuildContainerFull(container);

                GenerateBackupCollection(container);
                bag.Add(container);
            }
        }));
        Task.WaitAll(tasks.ToArray());

        return bag;
    }

    /// <inheritdoc cref="CreateContainer(int, PlatformExtra?)"/>
    private Container CreateContainer(int metaIndex) => CreateContainer(metaIndex, null);

    /// <summary>
    /// Creates a <see cref="Container"/> with basic data.
    /// </summary>
    /// <param name="metaIndex"></param>
    /// <param name="extra">An optional object with additional data necessary for proper creation.</param>
    /// <returns></returns>
    private protected abstract Container CreateContainer(int metaIndex, PlatformExtra? extra); // private protected as PlatformExtra is internal

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
    private static void UpdateContainerWithJsonInformation(Container container, string json, bool force)
    {
        // Values relevant for AccountData first.
        if (container.SaveVersion <= 0 || force)
            container.SaveVersion = Meta.SaveVersion.Get(json);

        // Then all independent values.
        if (container.Extra.GameMode == 0 || force)
            container.GameMode = Meta.GameMode.Get(json);

        if (container.TotalPlayTime == 0 || force)
            container.TotalPlayTime = Meta.TotalPlayTime.Get(json);

        // Finally all remaining values that depend on others.
        if (container.GameMode == PresetGameModeEnum.Seasonal && container.Season == SeasonEnum.None || force)
            container.Season = Meta.Season.Get(json); // needs GameMode

        if (container.BaseVersion <= 0 || force)
            container.BaseVersion = Meta.BaseVersion.Calculate(container); // needs SaveVersion and GameMode and Season

        if (container.GameVersion == GameVersionEnum.Unknown)
            container.GameVersion = Meta.GameVersion.Get(container.BaseVersion, json); // needs BaseVersion

        if (container.Difficulty == DifficultyPresetTypeEnum.Invalid || force)
            container.Difficulty = Meta.DifficultyPreset.Get(container, json); // needs GameMode and GameVersion

        if (container.IsVersion400Waypoint) // needs GameVersion
        {
            if (string.IsNullOrEmpty(container.SaveName) || force)
                container.SaveName = Meta.SaveName.Get(json);

            if (string.IsNullOrEmpty(container.SaveSummary) || force)
                container.SaveSummary = Meta.SaveSummary.Get(json);
        }
    }

    /// <summary>
    /// Updates the specified container with information from the meta file.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="disk"></param>
    /// <param name="decompressed"></param>
    protected abstract void UpdateContainerWithMetaInformation(Container container, ReadOnlySpan<byte> disk, ReadOnlySpan<uint> decompressed);

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
