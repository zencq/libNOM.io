using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Text.RegularExpressions;

using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;

using LazyCache;
using libNOM.io.Interfaces;
using libNOM.map;

using Microsoft.Extensions.Caching.Memory;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace libNOM.io;


/// <summary>
/// Abstract base for all platforms which just hook into the methods they need.
/// </summary>
public abstract class Platform : IPlatform, IEquatable<Platform>
{
    #region Constant

    internal virtual int COUNT_SAVE_SLOTS { get; } = 15; // overrideable for compatibility with old PlayStation format
    internal virtual int COUNT_SAVES_PER_SLOT { get; } = 2;
    internal int COUNT_SAVES_TOTAL => COUNT_SAVE_SLOTS * COUNT_SAVES_PER_SLOT; // { get; }

    protected abstract int META_LENGTH_TOTAL_VANILLA { get; }
    protected abstract int META_LENGTH_TOTAL_WAYPOINT { get; }

    #endregion

    #region Field

    protected readonly IAppCache _cache = new CachingService();
    protected int _preparedForTransfer = -1;
    protected readonly LazyCacheEntryOptions _options = new();
    protected readonly FileSystemWatcher _watcher = new();

    #endregion

    #region Property

    #region Container

    protected Container AccountContainer { get; set; }

    protected List<Container> SaveContainerCollection { get; } = [];

    #endregion

    #region Configuration

    // public //

    public DirectoryInfo Location { get; protected set; }

    public abstract PlatformEnum PlatformEnum { get; }

    public UserIdentificationData PlatformUserIdentification { get; } = new();

    public PlatformSettings Settings { get; protected set; }

    // protected //

    protected int AnchorFileIndex { get; set; } = -1;

    protected abstract string[] PlatformAnchorFileGlob { get; }

    protected abstract Regex[] PlatformAnchorFileRegex { get; }

    protected abstract string? PlatformArchitecture { get; }

    protected abstract string? PlatformProcessPath { get; }

    protected abstract string PlatformToken { get; }

    #endregion

    #region Flags

    // public //

    public abstract bool CanCreate { get; }

    public abstract bool CanRead { get; }

    public abstract bool CanUpdate { get; }

    public abstract bool CanDelete { get; }

    public virtual bool Exists => Location?.Exists == true; // { get; }

    public virtual bool HasAccountData => AccountContainer.Exists && AccountContainer.IsCompatible; // { get; }

    public abstract bool HasModding { get; }

    public bool IsLoaded => SaveContainerCollection.Count != 0; // { get; }

    public bool IsRunning // { get; }
    {
        get
        {
            if (IsConsolePlatform || string.IsNullOrEmpty(PlatformProcessPath))
                return false;

            try
            {
                // First we get the file name of the process as it is different on Windows and macOS.
                var processName = Path.GetFileNameWithoutExtension(PlatformProcessPath);
                // Then we still need to check the MainModule to get the correct process as Steam (Windows) and Microsoft have the same name.
                var process = Process.GetProcessesByName(processName).FirstOrDefault(i => i.MainModule?.FileName?.EndsWith(PlatformProcessPath, StringComparison.Ordinal) == true);
                return process is not null && !process.HasExited;
            }
            // Throws Win32Exception if the implementing program only targets x86 as the game is a x64 process.
            catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
            {
                return false;
            }
        }
    }

    public virtual bool IsValid => PlatformAnchorFileRegex.ContainsIndex(AnchorFileIndex); // { get; }

    public abstract bool RestartToApply { get; }

    // protected //

    protected abstract bool IsConsolePlatform { get; }

    #endregion

    #endregion

    #region Getter

    // public //

    public int GetMaximumSlots() => COUNT_SAVE_SLOTS;

    // protected //

    protected int GetMetaSize(Container container)
    {
        return container.MetaFormat switch
        {
            MetaFormatEnum.Waypoint => META_LENGTH_TOTAL_WAYPOINT,
            _ => META_LENGTH_TOTAL_VANILLA,
        };
    }

    // private //

    /// <summary>
    /// Creates an unique identifier for bases based on its location.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <returns></returns>
#if !NETSTANDARD2_0
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0057: Use range operator", Justification = "The range operator is not supported in netstandard2.0 and Slice() has no performance penalties.")]
#endif
    private static string GetBaseIdentifier(JObject jsonObject)
    {
#if NETSTANDARD2_0_OR_GREATER
        var galacticAddress = jsonObject.GetValue<string>("BASE_GALACTIC_ADDRESS")!;
        var galacticInteger = galacticAddress.StartsWith("0x") ? long.Parse(galacticAddress.Substring(2), NumberStyles.HexNumber) : long.Parse(galacticAddress);
#else
        ReadOnlySpan<char> galacticAddress = jsonObject.GetValue<string>("BASE_GALACTIC_ADDRESS");
        var galacticInteger = galacticAddress.StartsWith("0x") ? long.Parse(galacticAddress.Slice(2), NumberStyles.HexNumber) : long.Parse(galacticAddress);
#endif

        var positionX = jsonObject.GetValue<int>("BASE_POSITION_0");
        var positionY = jsonObject.GetValue<int>("BASE_POSITION_1");
        var positionZ = jsonObject.GetValue<int>("BASE_POSITION_2");

        return $"{galacticInteger}{positionX:+000000;-000000}{positionY:+000000;-000000}{positionZ:+000000;-000000}";
    }

    // //

    #region Container

    // public //

    public Container GetAccountContainer()
    {
        return AccountContainer;
    }

    public Container? GetSaveContainer(int collectionIndex)
    {
        if (SaveContainerCollection.ContainsIndex(collectionIndex))
            return SaveContainerCollection[collectionIndex];

        return null;
    }

    public IEnumerable<Container> GetExistingContainers()
    {
        return SaveContainerCollection.Where(i => i.Exists);
    }

    public IEnumerable<Container> GetLoadedContainers()
    {
        return SaveContainerCollection.Where(i => i.IsLoaded);
    }

    public IEnumerable<Container> GetSlotContainers(int slotIndex)
    {
        return SaveContainerCollection.Where(i => i.SlotIndex == slotIndex);
    }

    public IEnumerable<Container> GetUnsyncedContainers()
    {
        return SaveContainerCollection.Where(i => i.IsLoaded && !i.IsSynced);
    }

    public IEnumerable<Container> GetWatcherContainers()
    {
        return SaveContainerCollection.Where(i => i.HasWatcherChange);
    }

    // protected //

    /// <summary>
    /// Gets all <see cref="Container"/> affected by one cache eviction.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    protected virtual IEnumerable<Container> GetCacheEvictionContainers(string name)
    {
        return SaveContainerCollection.Where(i => i.DataFile?.Name.Equals(name, StringComparison.OrdinalIgnoreCase) == true);
    }

    #endregion

    #region Path

    // public //

    public string GetBackupPath()
    {
        return Path.GetFullPath(Settings.Backup);
    }

    public string GetDownloadPath()
    {
        return Path.GetFullPath(Settings.Download);
    }

    // protected //

    /// <summary>
    /// Gets the index of the matching anchor.
    /// </summary>
    /// <param name="directory"></param>
    /// <returns></returns>
    protected int GetAnchorFileIndex(DirectoryInfo? directory)
    {
        if (directory is not null)
        {
            for (var i = 0; i < PlatformAnchorFileRegex.Length; i++)
            {
                if (directory.GetFiles().Any(j => PlatformAnchorFileRegex[i].IsMatch(j.Name)))
                    return i;
            }
        }
        return -1;
    }

    #endregion

    #endregion

    #region Setter

    // public //

    /// <summary>
    /// Updates the instance with a new configuration. If null is passed, the settings will be reset to default.
    /// </summary>
    /// <param name="platformSettings"></param>
    public void SetSettings(PlatformSettings? platformSettings)
    {
        // Cache old strategy first to be able to properly react to the change.
        var oldStrategy = Settings.LoadingStrategy;

        // Update.
        Settings = platformSettings ?? new();

        // Set new loadingStrategy and trigger collection operations.
        if (Settings.LoadingStrategy == LoadingStrategyEnum.Empty && oldStrategy > LoadingStrategyEnum.Empty)
        {
            // Clear container by removing its reference.
            AccountContainer = null!;
            SaveContainerCollection.Clear();

            DisableWatcher();
        }
        else if (Settings.LoadingStrategy > LoadingStrategyEnum.Empty && oldStrategy == LoadingStrategyEnum.Empty)
        {
            GeneratePlatformData();
        }
    }

    // private //

    private static void SetValueIfNullOrEmpty(JObject jsonObject, JToken value, string pathIdentifier)
    {
        if (!string.IsNullOrEmpty(jsonObject.GetValue<string>(pathIdentifier)))
            jsonObject.SetValue(value, pathIdentifier);
    }

    #endregion

    // //

    #region Constructor

#pragma warning disable CS8618 // Non-nullable property 'Settings' must contain a non-null value when exiting constructor. Property 'Settings' is set in InitializeComponent.
    public Platform()
    {
        InitializeComponent(null, null);
    }

    public Platform(string path)
    {
        InitializeComponent(new(path), null);
    }

    public Platform(string path, PlatformSettings platformSettings)
    {
        InitializeComponent(new(path), platformSettings);
    }

    public Platform(DirectoryInfo directory)
    {
        InitializeComponent(directory, null);
    }

    public Platform(DirectoryInfo directory, PlatformSettings platformSettings)
    {
        InitializeComponent(directory, platformSettings);
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor.

    /// <summary>
    /// Workaround to be able to decide when inherited classes initialize their components.
    /// </summary>
    /// <param name="directory"></param>
    /// <param name="platformSettings"></param>
    protected virtual void InitializeComponent(DirectoryInfo? directory, PlatformSettings? platformSettings)
    {
        AnchorFileIndex = GetAnchorFileIndex(directory);
        Location = directory!; // force with ! even if null as it would be invalid anyway
        Settings = platformSettings ?? new();

        // Stop if no directory or no anchor found.
        if (!IsValid)
            return;

        // Watcher
        _options.RegisterPostEvictionCallback(OnCacheEviction);
        _options.SetAbsoluteExpiration(TimeSpan.FromMilliseconds(Constants.CACHE_EXPIRATION), ExpirationMode.ImmediateEviction);

        _watcher.Changed += OnWatcherEvent;
        _watcher.Created += OnWatcherEvent;
        _watcher.Deleted += OnWatcherEvent;
        _watcher.Renamed += OnWatcherEvent;

        _watcher.Filter = PlatformAnchorFileGlob[AnchorFileIndex];
        _watcher.Path = Location.FullName;

        // Loading
        GeneratePlatformData();
    }

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
        throw new NotImplementedException();
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

    // // Read / Write

    #region Generate

    /// <summary>
    /// Generates all related containers as well as the user identification.
    /// </summary>
    private void GeneratePlatformData()
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

        var tasks = Enumerable.Range(0, Constants.OFFSET_INDEX + COUNT_SAVES_TOTAL).Select((metaIndex) =>
        {
            return Task.Run(() =>
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
                    {
                        BuildContainerHollow(container);
                    }
                    else
                    {
                        BuildContainerFull(container);
                    }
                    GenerateBackupCollection(container);
                    bag.Add(container);
                }
            });
        });
        Task.WaitAll(tasks.ToArray());

        return bag;
    }

    /// <inheritdoc cref="CreateContainer(int, PlatformExtra?)"/>
    internal Container CreateContainer(int metaIndex)
    {
        return CreateContainer(metaIndex, null);
    }

    /// <summary>
    /// Creates a <see cref="Container"/> with basic data.
    /// </summary>
    /// <param name="metaIndex"></param>
    /// <param name="extra">An optional object with additional data necessary for proper creation.</param>
    /// <returns></returns>
    private protected abstract Container CreateContainer(int metaIndex, PlatformExtra? extra); // private protected as PlatformExtra is internal

    /// <summary>
    /// Builds a <see cref="Container"/> by loading from disk and processing it by deserializing the data.
    /// </summary>
    /// <param name="container"></param>
    protected void BuildContainerFull(Container container)
    {
        var binary = LoadContainer(container);

        if (container.IsCompatible && DeserializeContainer(container, binary) is JObject jsonObject)
            ProcessContainerData(container, jsonObject);
    }

    /// <summary>
    /// Builds a <see cref="Container"/> by loading from disk and processing it by extracting from the string representation.
    /// </summary>
    /// <param name="container"></param>
    protected void BuildContainerHollow(Container container)
    {
        var binary = LoadContainer(container);

        if (container.IsCompatible)
            ProcessContainerData(container, binary.GetString(), false);
    }

    /// <summary>
    /// Deserializes the read data of a <see cref="Container"/> into a JSON object.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="binary"></param>
    /// <returns></returns>
    protected virtual JObject? DeserializeContainer(Container container, ReadOnlySpan<byte> binary)
    {
        JObject? jsonObject;
        try
        {
            jsonObject = binary.GetJson();
        }
        catch (Exception ex) when (ex is JsonReaderException or JsonSerializationException)
        {
            container.IncompatibilityException = ex;
            container.IncompatibilityTag = Constants.INCOMPATIBILITY_002;
            return null;
        }
        if (jsonObject is null)
        {
            container.IncompatibilityTag = Constants.INCOMPATIBILITY_003;
            return null;
        }

        container.UsesMapping = Settings.UseMapping;
        if (Settings.UseMapping)
        {
            container.UnknownKeys = Mapping.Deobfuscate(jsonObject);
        }
        else
        {
            // Do deliver a consistent experience, make sure the file is obfuscated if the setting is set to false.
            Mapping.Obfuscate(jsonObject);
        }

        return jsonObject;
    }

    /// <summary>
    /// Generates a collection with all backups of the specified <see cref="Container"/> that matches the MetaIndex and this <see cref="Platform"/>.
    /// </summary>
    /// <param name="container"></param>
    protected void GenerateBackupCollection(Container container)
    {
        container.BackupCollection.Clear();

        // No directory, no backups.
        if (!Directory.Exists(Settings.Backup))
            return;

        foreach (var file in Directory.GetFiles(Settings.Backup, $"backup.{PlatformEnum}.{container.MetaIndex:D2}.*.*.zip".ToLowerInvariant()))
        {
            var parts = Path.GetFileNameWithoutExtension(file).Split('.');

            // The filename of a backup needs to have the following format: "backup.{PlatformEnum}.{MetaIndex}.{CreatedAt}.{VersionEnum}" + ".zip"
            if (parts.Length < 5)
                continue;

            try
            {
                container.BackupCollection.Add(new(container.MetaIndex)
                {
                    DataFile = new(file),
                    GameVersion = (GameVersionEnum)(System.Convert.ToInt32(parts[4])),
                    IsBackup = true,
                    LastWriteTime = DateTimeOffset.ParseExact($"{parts[3]}", Constants.FILE_TIMESTAMP_FORMAT, CultureInfo.InvariantCulture),
                });
            }
            catch (FormatException)
            {
                // Ignore.
            }
        }
    }

    #endregion

    #region Load

    /// <summary>
    /// Loads the save data of a <see cref="Container"/> into a processable format using meta data.
    /// </summary>
    /// <param name="container"></param>
    /// <returns></returns>
    protected virtual ReadOnlySpan<byte> LoadContainer(Container container)
    {
        // Any incompatibility will be set again while loading.
        container.ClearIncompatibility();

        if (container.Exists)
        {
            // Loads all meta information into the extra property.
            LoadMeta(container);

            var data = LoadData(container);
            if (data.IsEmpty())
            {
                container.IncompatibilityTag = Constants.INCOMPATIBILITY_001;
            }
            else
            {
                return data;
            }
        }

        container.IncompatibilityTag ??= Constants.INCOMPATIBILITY_006;
        return [];
    }

    /// <inheritdoc cref="LoadMeta(Container, Span{byte})"/>
    protected void LoadMeta(Container container)
    {
        // 1. Read
        LoadMeta(container, ReadMeta(container));
    }

    /// <summary>
    /// Loads the meta file into a processable format including reading, decrypting, and decompressing.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="read">Already read content of the meta file.</param>
    /// <returns></returns>
    protected void LoadMeta(Container container, Span<byte> read)
    {
        // 2. Decrypt
        // 3. Decompress
        var result = read.IsEmpty() ? [] : DecompressMeta(container, DecryptMeta(container, read));
        // 4. Update Container Information
        UpdateContainerWithMetaInformation(container, result.AsBytes(), result);
    }

    /// <summary>
    /// Reads the content of the meta file.
    /// </summary>
    /// <param name="container"></param>
    /// <returns></returns>
    protected virtual Span<byte> ReadMeta(Container container)
    {
        if (container.MetaFile?.Exists != true)
            return [];

        return File.ReadAllBytes(container.MetaFile!.FullName);
    }

    /// <summary>
    /// Decrypts the read content of the meta file.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="meta"></param>
    /// <returns></returns>
    protected virtual Span<uint> DecryptMeta(Container container, Span<byte> meta)
    {
        return meta.Cast<byte, uint>();
    }

    /// <summary>
    /// Decompresses the read and decrypted content of the meta file.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="meta"></param>
    /// <returns></returns>
    protected virtual ReadOnlySpan<uint> DecompressMeta(Container container, ReadOnlySpan<uint> meta)
    {
        return meta;
    }

    /// <summary>
    /// Updates the specified container with information from the meta file.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="disk"></param>
    /// <param name="decompressed"></param>
    protected abstract void UpdateContainerWithMetaInformation(Container container, ReadOnlySpan<byte> disk, ReadOnlySpan<uint> decompressed);

    /// <inheritdoc cref="LoadData(Container, ReadOnlySpan{byte})"/>
    protected virtual ReadOnlySpan<byte> LoadData(Container container)
    {
        // 1. Read
        return LoadData(container, ReadData(container));
    }

    /// <summary>
    /// Loads the data file into a processable format including reading, decrypting, and decompressing.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="read"></param>
    /// <returns></returns>
    protected ReadOnlySpan<byte> LoadData(Container container, ReadOnlySpan<byte> read)
    {
        if (read.IsEmpty())
            return read;

        // 2. Decrypt
        // 3. Decompress
        var result = DecompressData(container, DecryptData(container, read));
        // 4. Update Container Information
        UpdateContainerWithDataInformation(container, read, result);

        return result;
    }

    /// <summary>
    /// Reads the content of the data file.
    /// </summary>
    /// <param name="container"></param>
    /// <returns></returns>
    protected virtual ReadOnlySpan<byte> ReadData(Container container)
    {
        if (container.DataFile?.Exists != true)
            return [];

        return File.ReadAllBytes(container.DataFile!.FullName);
    }

    /// <summary>
    /// Decrypts the read content of the data file.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    protected virtual ReadOnlySpan<byte> DecryptData(Container container, ReadOnlySpan<byte> data)
    {
        return data;
    }

    /// <summary>
    /// Decompresses the read and decrypted content of the data file.
    /// </summary>
    /// <param name="container"></param
    /// <param name="data"></param>
    /// <returns></returns>
    protected virtual ReadOnlySpan<byte> DecompressData(Container container, ReadOnlySpan<byte> data)
    {
        if (container.IsAccount || data.Cast<uint>(0) != Constants.SAVE_STREAMING_HEADER) // no compression before Frontiers
            return data;

        var offset = 0;
        ReadOnlySpan<byte> result = [];

        while (offset < data.Length)
        {
            var chunkHeader = data.Slice(offset, Constants.SAVE_STREAMING_HEADER_TOTAL_LENGTH).Cast<byte, uint>();
            var sizeCompressed = (int)(chunkHeader[1]);

            offset += Constants.SAVE_STREAMING_HEADER_TOTAL_LENGTH;
            _ = LZ4.Decode(data.Slice(offset, sizeCompressed), out var target, (int)(chunkHeader[2]));
            offset += sizeCompressed;

            result = result.Concat(target);
        }

        return result;
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

    #region Process

    /// <summary>
    /// Processes the read JSON object and fills the properties of the container.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="jsonObject"></param>
    /// <param name="force"></param>
    private void ProcessContainerData(Container container, JObject jsonObject)
    {
        // Setting the JSON will enable all properties.
        container.SetJsonObject(jsonObject);

        // No need to do these things for AccountData.
        if (container.IsSave)
        {
            container.UserIdentification = GetUserIdentification(jsonObject);
            UpdateUserIdentification();
        }

        // If we are in here, the container is in sync (again).
        container.IsSynced = true;
    }

    /// <inheritdoc cref="ProcessContainerData(Container, JObject, bool)"/>
    private static void ProcessContainerData(Container container, string json, bool force)
    {
        // Values relevant for AccountData first.
        if (container.SaveVersion == 0 || force)
            container.SaveVersion = Helper.SaveVersion.Get(json);

        // Then all independent values.
        if (container.Extra.GameMode == 0 || force)
            container.GameMode = Helper.GameMode.Get(json);

        if (container.TotalPlayTime == 0 || force)
            container.TotalPlayTime = Helper.TotalPlayTime.Get(json);

        // Finally all remaining values that depend on others.
        if (container.GameMode == PresetGameModeEnum.Seasonal && container.Season == SeasonEnum.None || force)
            container.Season = Helper.Season.Get(json); // needs GameMode

        if (container.BaseVersion == 0 || force)
            container.BaseVersion = Helper.BaseVersion.Calculate(container); // needs SaveVersion and GameMode and Season

        if (container.GameVersion == GameVersionEnum.Unknown)
            container.GameVersion = Helper.GameVersion.Get(container.BaseVersion, json); // needs BaseVersion

        if (container.GameDifficulty == DifficultyPresetTypeEnum.Invalid || force)
            container.GameDifficulty = Helper.DifficultyPreset.Get(container, json); // needs GameMode and GameVersion

        if (container.IsVersion400Waypoint) // needs GameVersion
        {
            if (string.IsNullOrEmpty(container.SaveName) || force)
                container.SaveName = Helper.SaveName.Get(json);

            if (string.IsNullOrEmpty(container.SaveSummary) || force)
                container.SaveSummary = Helper.SaveSummary.Get(json);
        }
    }

    #endregion

    #region Reload

    public void Load(Container container)
    {
        if (container.IsBackup)
        {
            LoadBackupContainer(container);
        }
        else
        {
            LoadSaveContainer(container);
        }
    }

    /// <summary>
    /// Loads data of the specified backup.
    /// </summary>
    /// <param name="container"></param>
    /// <exception cref="ArgumentException"/>
    private void LoadBackupContainer(Container container)
    {
        Guard.IsTrue(container.Exists);
        Guard.IsTrue(container.IsBackup);

        // Load
        container.ClearIncompatibility();

        using var zipArchive = ZipFile.Open(container.DataFile!.FullName, ZipArchiveMode.Read);
        if (zipArchive.ReadEntry("data", out var data))
        {
            _ = zipArchive.ReadEntry("meta", out var meta);

            // Loads all meta information into the extra property.
            LoadMeta(container, meta);

            var binary = LoadData(container, data);
            if (binary.IsEmpty())
            {
                container.IncompatibilityTag = Constants.INCOMPATIBILITY_001;
                return;
            }

            // Process
            if (DeserializeContainer(container, binary) is JObject jsonObject)
                ProcessContainerData(container, jsonObject);
        }
    }

    /// <summary>
    /// Loads data of the specified save.
    /// </summary>
    /// <param name="container"></param>
    private void LoadSaveContainer(Container container)
    {
        if (Settings.LoadingStrategy < LoadingStrategyEnum.Current)
            Settings = Settings with { LoadingStrategy = LoadingStrategyEnum.Current };

        if (Settings.LoadingStrategy == LoadingStrategyEnum.Current && container.IsSave)
        {
            var loadedContainers = GetLoadedContainers().Where(i => !i.Equals(container));
            foreach (var loadedContainer in loadedContainers)
            {
                // Unloads data by removing the reference to the JSON object.
                loadedContainer.SetJsonObject(null);
            }
        }

        BuildContainerFull(container);
    }

    public void Rebuild(Container container, JObject jsonObject)
    {
        ProcessContainerData(container, jsonObject);
    }

    /// <summary>
    /// Rebuilds a <see cref="Container"/> by loading from disk and processing it by deserializing the data.
    /// </summary>
    /// <param name="container"></param>
    protected void RebuildContainerFull(Container container)
    {
        var binary = LoadContainer(container);

        if (container.IsCompatible && DeserializeContainer(container, binary) is JObject jsonObject)
            ProcessContainerData(container, jsonObject);
    }

    /// <summary>
    /// Rebuilds a <see cref="Container"/> by loading from disk and processing it by extracting from the string representation.
    /// </summary>
    /// <param name="container"></param>
    protected void RebuildContainerHollow(Container container)
    {
        var binary = LoadContainer(container);

        if (container.IsCompatible)
            ProcessContainerData(container, binary.GetString(), true);
    }

    public void Reload(Container container)
    {
        if (container.IsLoaded)
        {
            RebuildContainerFull(container);
        }
        else
        {
            RebuildContainerHollow(container);
        }
    }

    #endregion

    #region Write

    public void Write(Container container) => Write(container, DateTimeOffset.Now.LocalDateTime);

    public virtual void Write(Container container, DateTimeOffset writeTime)
    {
        if (!CanUpdate || !container.IsLoaded)
            return;

        DisableWatcher();

        // In case LastWriteTime is written inside meta set it before writing.
        if (Settings.SetLastWriteTime)
        {
            container.LastWriteTime = writeTime;
        }

        if (Settings.WriteAlways || !container.IsSynced)
        {
            container.Exists = true;
            container.IsSynced = true;

            JustWrite(container);
        }

        // To ensure the timestamp will be the same the next time, the file times are always set to the currently saved one.
        if (container.LastWriteTime is not null)
        {
            if (container.DataFile is not null)
            {
                File.SetCreationTime(container.DataFile.FullName, container.LastWriteTime!.Value.LocalDateTime);
                File.SetLastWriteTime(container.DataFile.FullName, container.LastWriteTime!.Value.LocalDateTime);
            }
            if (container.MetaFile is not null)
            {
                File.SetCreationTime(container.MetaFile.FullName, container.LastWriteTime!.Value.LocalDateTime);
                File.SetLastWriteTime(container.MetaFile.FullName, container.LastWriteTime!.Value.LocalDateTime);
            }
        }

        EnableWatcher();

        // Always refresh in case something above was executed.
        container.RefreshFileInfo();
        container.WriteCallback.Invoke();
    }

    internal void JustWrite(Container container)
    {
        var data = PrepareData(container);
        var meta = PrepareMeta(container, data);

        WriteMeta(container, meta);
        WriteData(container, data);
    }

    /// <summary>
    /// Prepares the ready to write to disk binary data file content.
    /// </summary>
    /// <param name="container"></param>
    /// <returns></returns>
    protected ReadOnlySpan<byte> PrepareData(Container container)
    {
        // 1. Create
        var plain = CreateData(container);
        // 2. Compress
        // 3. Encrypt
        var encrypted = EncryptData(container, CompressData(container, plain));
        // 4. Update Container Information
        UpdateContainerWithDataInformation(container, encrypted, plain);

        return encrypted;
    }

    /// <summary>
    /// Creates binary data file content from the JSON object.
    /// </summary>
    /// <param name="container"></param>
    /// <returns></returns>
    protected virtual ReadOnlySpan<byte> CreateData(Container container)
    {
        return container.GetJsonObject().GetBytes();
    }

    /// <summary>
    /// Compresses the created data file content.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    protected virtual ReadOnlySpan<byte> CompressData(Container container, ReadOnlySpan<byte> data)
    {
        if (!container.IsSave || !container.IsVersion360Frontiers)
            return data;

        var position = 0;
        ReadOnlySpan<byte> result = Array.Empty<byte>();

        while (position < data.Length)
        {
            var source = data.Slice(position, Math.Min(Constants.SAVE_STREAMING_CHUNK_MAX_LENGTH, data.Length - position));
            _ = LZ4.Encode(source, out var target);
            position += Constants.SAVE_STREAMING_CHUNK_MAX_LENGTH;

            var chunkHeader = new ReadOnlySpan<uint>(
            [
                Constants.SAVE_STREAMING_HEADER,
                (uint)(target.Length),
                (uint)(source.Length),
                0,
            ]);

            result = result.Concat(chunkHeader.Cast<uint, byte>()).Concat(target);
        }

        return result;
    }

    /// <summary>
    /// Encrypts the created and compressed data file content.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    protected virtual ReadOnlySpan<byte> EncryptData(Container container, ReadOnlySpan<byte> data)
    {
        return data;
    }

    /// <summary>
    /// Writes the final data file content to disk.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="data"></param>
    protected virtual void WriteData(Container container, ReadOnlySpan<byte> data)
    {
        if (container.DataFile is not null)
            File.WriteAllBytes(container.DataFile.FullName, data.ToArray());
    }

    /// <summary>
    /// Prepares the ready to write to disk binary meta file content.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    protected ReadOnlySpan<byte> PrepareMeta(Container container, ReadOnlySpan<byte> data)
    {
        // 1. Create
        var plain = CreateMeta(container, data);
        // 2. Compress
        // 3. Encrypt
        var encrypted = EncryptMeta(container, data, CompressMeta(container, data, plain.AsBytes()));
        // 4. Update Container Information
        UpdateContainerWithMetaInformation(container, encrypted, plain);

        return encrypted;
    }

    /// <summary>
    /// Creates binary meta file content with information from the <see cref="Container"/> and the JSON object.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    protected abstract Span<uint> CreateMeta(Container container, ReadOnlySpan<byte> data);

    /// <summary>
    /// Compresses the created meta file content.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="data"></param>
    /// <param name="meta"></param>
    /// <returns></returns>
    protected virtual Span<byte> CompressMeta(Container container, ReadOnlySpan<byte> data, Span<byte> meta)
    {
        return meta;
    }

    /// <summary>
    /// Encrypts the created and compressed meta file content.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="data"></param>
    /// <param name="meta"></param>
    /// <returns></returns>
    protected virtual ReadOnlySpan<byte> EncryptMeta(Container container, ReadOnlySpan<byte> data, Span<byte> meta)
    {
        return meta;
    }

    /// <summary>
    /// Writes the final meta file content to disk.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="meta"></param>
    protected virtual void WriteMeta(Container container, ReadOnlySpan<byte> meta)
    {
        if (container.MetaFile is not null)
            File.WriteAllBytes(container.MetaFile.FullName, meta.ToArray());
    }

    #endregion

    // // File Operation

    #region Backup

    public void Backup(Container container)
    {
        // Does not make sense without the data file.
        Guard.IsNotNull(container.DataFile);
        Guard.IsTrue(container.DataFile.Exists);

        var createdAt = DateTime.Now;
        var name = $"backup.{PlatformEnum}.{container.MetaIndex:D2}.{createdAt.ToString(Constants.FILE_TIMESTAMP_FORMAT)}.{(uint)(container.GameVersion)}.zip".ToLowerInvariant();
        var path = Path.Combine(Settings.Backup, name);

        Directory.CreateDirectory(Settings.Backup);
        using (var zipArchive = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            _ = zipArchive.CreateEntryFromFile(container.DataFile.FullName, "data");
            if (container.MetaFile?.Exists == true)
            {
                _ = zipArchive.CreateEntryFromFile(container.MetaFile.FullName, "meta");
            }
        }

        // Create new backup container.
        var backup = new Container(container.MetaIndex)
        {
            DataFile = new(path),
            GameVersion = container.GameVersion,
            IsBackup = true,
            LastWriteTime = createdAt,
        };
        container.BackupCollection.Add(backup);

        // Remove the oldest backups above the maximum count.
        var outdated = container.BackupCollection.OrderByDescending(i => i.LastWriteTime).Skip(Settings.MaxBackupCount);
        if (outdated.Any())
        {
            Delete(outdated);
            foreach (var item in outdated)
            {
                container.BackupCollection.Remove(item);
            }
        }

        container.BackupCreatedCallback.Invoke(backup);
    }

    public void Restore(Container backup)
    {
        Guard.IsTrue(backup.Exists);
        Guard.IsTrue(backup.IsBackup);

        if (!backup.IsLoaded)
        {
            LoadBackupContainer(backup);
        }

        if (!backup.IsCompatible)
            ThrowHelper.ThrowInvalidOperationException(backup.IncompatibilityException?.Message ?? backup.IncompatibilityTag ?? $"{backup} is incompatible.");

        var container = GetSaveContainer(backup.CollectionIndex);
        Rebuild(container!, backup.GetJsonObject());

        // Set IsSynced to false as ProcessContainerData set it to true but it is not compared to the state on disk.
        container!.IsSynced = false;
        container!.BackupRestoredCallback.Invoke();
    }

    #endregion

    #region Copy

    public void Copy(Container source, Container destination) => Copy(new[] { (Source: source, Destination: destination) }, true);

    protected void Copy(Container source, Container destination, bool write) => Copy(new[] { (Source: source, Destination: destination) }, write);

    public void Copy(IEnumerable<(Container Source, Container Destination)> operationData) => Copy(operationData, true);

    protected virtual void Copy(IEnumerable<(Container Source, Container Destination)> operationData, bool write)
    {
        foreach (var (Source, Destination) in operationData)
        {
            if (!Source.Exists)
            {
                Delete(Destination, write);
            }
            else if (Destination.Exists || (!Destination.Exists && CanCreate))
            {
                if (!Source.IsLoaded)
                    BuildContainerFull(Source);

                if (!Source.IsCompatible)
                    throw new InvalidOperationException($"Cannot copy as the source container is not compatible: {Source.IncompatibilityTag}");

                Destination.SetJsonObject(Source.GetJsonObject());
                Destination.ClearIncompatibility();

                // Due to this CanCreate can be true.
                CopyPlatformExtra(Destination, Source);

                // Faking relevant properties to force it to Write().
                Destination.Exists = true;

                // Additional properties required to properly rebuild the container.
                Destination.GameVersion = Source.GameVersion;
                Destination.SaveVersion = Source.SaveVersion;

                // This "if" is not really useful in this method but properly implemented nonetheless.
                if (write)
                {
                    Write(Destination, Source.LastWriteTime ?? DateTimeOffset.Now);
                    RebuildContainerFull(Destination);
                }
            }
            //else
            //    continue;
        }

        UpdateUserIdentification();
    }

    /// <summary>
    /// Copies the platform extra from the source container.
    /// </summary>
    /// <param name="destination"></param>
    /// <param name="source"></param>
    /// <returns></returns>
    protected virtual void CopyPlatformExtra(Container destination, Container source)
    {
        // Overwrite all general values but keep platform specific stuff unchanged.
        destination.Extra = destination.Extra with
        {
            MetaFormat = source.Extra.MetaFormat,
            Bytes = source.Extra.Bytes,
            Size = source.Extra.Size,
            SizeDecompressed = source.Extra.SizeDecompressed,
            SizeDisk = source.Extra.SizeDisk,
            LastWriteTime = source.Extra.LastWriteTime,
            BaseVersion = source.Extra.BaseVersion,
            GameMode = source.Extra.GameMode,
            Season = source.Extra.Season,
            TotalPlayTime = source.Extra.TotalPlayTime,
            SaveName = source.Extra.SaveName,
            SaveSummary = source.Extra.SaveSummary,
            DifficultyPreset = source.Extra.DifficultyPreset,
        };
    }

    #endregion

    #region Delete

    public void Delete(Container container) => Delete(new[] { container }, true);

    protected void Delete(Container container, bool write) => Delete(new[] { container }, write);

    public void Delete(IEnumerable<Container> containers) => Delete(containers, true);

    protected virtual void Delete(IEnumerable<Container> containers, bool write)
    {
        Guard.IsTrue(CanDelete);

        DisableWatcher();

        foreach (var container in containers)
        {
            if (write)
            {
                if (container.DataFile?.Exists == true)
                {
                    try
                    {
                        File.Delete(container.DataFile.FullName);
                    }
                    catch (Exception ex) when (ex is IOException or NotSupportedException or PathTooLongException or UnauthorizedAccessException)
                    {
                        // Nothing to do.
                    }
                }

                if (container.MetaFile?.Exists == true)
                {
                    try
                    {
                        File.Delete(container.MetaFile.FullName);
                    }
                    catch (Exception ex) when (ex is IOException or NotSupportedException or PathTooLongException or UnauthorizedAccessException)
                    {
                        // Nothing to do.
                    }
                }
            }

            container.Reset();
            container.IncompatibilityTag = Constants.INCOMPATIBILITY_006;
        }

        EnableWatcher();
    }

    #endregion

    #region Move

    public void Move(Container source, Container destination) => Move(new[] { (Source: source, Destination: destination) }, true);

    protected void Move(Container source, Container destination, bool write) => Move(new[] { (Source: source, Destination: destination) }, write);

    public void Move(IEnumerable<(Container Source, Container Destination)> operationData) => Move(operationData, true);

    protected virtual void Move(IEnumerable<(Container Source, Container Destination)> operationData, bool write)
    {
        Copy(operationData, write);
        Delete(operationData.Select(i => i.Source), write);
    }

    #endregion

    #region Swap

    public void Swap(Container source, Container destination) => Swap(new[] { (Source: source, Destination: destination) }, true);

    protected void Swap(Container source, Container destination, bool write) => Swap(new[] { (Source: source, Destination: destination) }, write);

    public void Swap(IEnumerable<(Container Source, Container Destination)> operationData) => Swap(operationData, true);

    protected virtual void Swap(IEnumerable<(Container Source, Container Destination)> operationData, bool write)
    {
        // Make sure everything can be swapped.
        foreach (var (Source, Destination) in operationData.Where(i => i.Source.Exists && i.Destination.Exists))
        {
            if (!Source.IsLoaded)
                BuildContainerFull(Source);

            if (!Destination.IsLoaded)
                BuildContainerFull(Destination);

            if (!Source.IsCompatible || !Destination.IsCompatible)
                throw new InvalidOperationException($"Cannot swap as at least one container is not compatible: {Source.IncompatibilityTag} / {Destination.IncompatibilityTag}");
        }

        foreach (var (Source, Destination) in operationData)
        {
            if (Source.Exists)
            {
                // Source and Destination exists. Swap.
                if (Destination.Exists)
                {
                    // Cache.
                    var jsonObject = Destination.GetJsonObject();
                    var writeTime = Destination.LastWriteTime;

                    // Write Source to Destination.
                    Destination.SetJsonObject(Source.GetJsonObject());
                    Write(Destination, Source.LastWriteTime ?? DateTimeOffset.Now);
                    RebuildContainerFull(Destination);

                    // Write Destination to Source.
                    Source.SetJsonObject(jsonObject);
                    Write(Source, writeTime ?? DateTimeOffset.Now);
                    RebuildContainerFull(Source);
                }
                // Source exists only. Move to Destination.
                else
                {
                    Move(Source, Destination);
                }
            }
            // Destination exists only. Move to Source.
            else if (Destination.Exists)
            {
                Move(Destination, Source);
            }
        }

        UpdateUserIdentification();
    }

    #endregion

    #region Transfer

    public ContainerTransferData PrepareTransferSource(int sourceSlotIndex)
    {

        // If user identification is not complete, load saves until it is.
        foreach (var container in SaveContainerCollection.Where(i => i.Exists && !i.IsLoaded))
        {
            // Faking while-loop by checking first.
            if (PlatformUserIdentification.IsComplete())
                break;

            BuildContainerFull(container);
        }

        var sourceTransferData = new ContainerTransferData
        {
            Containers = GetSlotContainers(sourceSlotIndex),
            UserIdentification = PlatformUserIdentification,
        };

        foreach (var container in sourceTransferData.Containers)
        {
            if (!container.Exists)
                continue;

            if (!container.IsLoaded)
                BuildContainerFull(container);

            var jsonObject = container.GetJsonObject();
            var usesMapping = jsonObject.UsesMapping();

            // TODO JSONPath
            var path = usesMapping ? $"PlayerStateData.PersistentPlayerBases[?({{0}})]" : $"6f=.F?0[?({{0}})]";
            var expressions = new[]
            {
                usesMapping ? $"@.BaseType.PersistentBaseTypes == '{PersistentBaseTypesEnum.HomePlanetBase}' || @.BaseType.PersistentBaseTypes == '{PersistentBaseTypesEnum.FreighterBase}'" : $"@.peI.DPp == '{PersistentBaseTypesEnum.HomePlanetBase}' || @.peI.DPp == '{PersistentBaseTypesEnum.FreighterBase}'", // only with own base
                usesMapping ? $"@.Owner.UID == '{PlatformUserIdentification.UID}'" : $"@.3?K.K7E == '{PlatformUserIdentification.UID}'",
            };

            foreach (var persistentPlayerBase in Json.SelectTokensWithIntersection(jsonObject, path, expressions).Cast<JObject>())
            {
                var baseName = persistentPlayerBase.GetValue<string>("BASE_NAME");
                var baseType = persistentPlayerBase.GetValue<string>("BASE_TYPE");

                if (string.IsNullOrEmpty(baseName))
                {
                    if (baseType == PersistentBaseTypesEnum.FreighterBase.ToString())
                    {
                        baseName = "Freighter Base";
                    }
                    else if (baseType == PersistentBaseTypesEnum.HomePlanetBase.ToString())
                    {
                        baseName = "Unnamed Planet Base";
                    }
                    else
                    {
                        baseName = "Unnamed Base";
                    }
                }

                sourceTransferData.TransferBaseUserDecision[GetBaseIdentifier(persistentPlayerBase)] = new() { DoTransfer = true, Name = baseName! };
            }
        }

        UpdateUserIdentification();

        return sourceTransferData with
        {
            UserIdentification = PlatformUserIdentification,
        };
    }

    public void PrepareTransferDestination(int destinationSlotIndex)
    {
        // Load destination as they are needed anyway.
        foreach (var container in GetSlotContainers(destinationSlotIndex))
        {
            if (container.Exists && !container.IsLoaded)
                BuildContainerFull(container);
        }

        // If user identification is not complete, load saves until it is.
        foreach (var container in SaveContainerCollection.Where(i => i.Exists && !i.IsLoaded))
        {
            // Faking while-loop by checking first.
            if (PlatformUserIdentification.IsComplete())
                break;

            BuildContainerFull(container);
        }

        _preparedForTransfer = destinationSlotIndex;
    }

    public void Transfer(ContainerTransferData sourceTransferData, int destinationSlotIndex) => Transfer(sourceTransferData, destinationSlotIndex, true);

    /// <inheritdoc cref="Transfer(ContainerTransferData, int)"/>
    /// <param name="write"></param>
    /// <exception cref="InvalidOperationException"></exception>
    protected virtual void Transfer(ContainerTransferData sourceTransferData, int destinationSlotIndex, bool write)
    {
        if (_preparedForTransfer != destinationSlotIndex)
            PrepareTransferDestination(destinationSlotIndex);

        if (!sourceTransferData.UserIdentification.IsComplete() || !PlatformUserIdentification.IsComplete())
            throw new InvalidOperationException("Cannot transfer as at least one user identification is not complete.");

        foreach (var (Source, Destination) in sourceTransferData.Containers.Zip(GetSlotContainers(destinationSlotIndex), (Source, Destination) => (Source, Destination)))
        {
            if (!Source.Exists)
            {
                Delete(Destination, write);
            }
            else if (Destination.Exists || !Destination.Exists && CanCreate)
            {
                if (!Source.IsCompatible)
                    throw new InvalidOperationException($"Cannot transfer as the source container is not compatible: {Source.IncompatibilityTag}");

                Destination.SetJsonObject(Source.GetJsonObject());
                Destination.ClearIncompatibility();

                // Due to this CanCreate can be true.
                CreatePlatformExtra(Destination, Source);

                // Faking relevant properties to force it to Write().
                Destination.Exists = true;

                // Additional properties required to properly rebuild the container.
                Destination.GameVersion = Source.GameVersion;
                Destination.SaveVersion = Source.SaveVersion;

                TransferOwnership(Destination, sourceTransferData);

                // This "if" is not really useful in this method but properly implemented nonetheless.
                if (write)
                {
                    Write(Destination, Source.LastWriteTime ?? DateTimeOffset.Now);
                    RebuildContainerFull(Destination);
                }
            }
            //else
            //    continue;
        }
    }

    /// <summary>
    /// Creates the platform extra for the destination container.
    /// </summary>
    /// <param name="destination"></param>
    /// <param name="source"></param>
    /// <returns></returns>
    protected virtual void CreatePlatformExtra(Container destination, Container source)
    {
        CopyPlatformExtra(destination, source);

        // Reset bytes as from another platform would not be right.
        destination.Extra = destination.Extra with
        {
            Bytes = null,
        };
    }

    /// <summary>
    /// Transfers ownerships to new container according to the prepared data.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="sourceTransferData"></param>
    protected void TransferOwnership(Container container, ContainerTransferData sourceTransferData)
    {
        var jsonObject = container.GetJsonObject();

        // Change token for Platform.
        jsonObject.SetValue(PlatformArchitecture, "PLATFORM");

        // TODO check both contexts

        if (sourceTransferData.TransferDiscovery) // 1.0
            TransferGeneralOwnership(jsonObject, Json.GetPaths("DISCOVERY_DATA_WITH_UID", jsonObject, sourceTransferData.UserIdentification!.UID)[0]);

        if (container.IsVersion(GameVersionEnum.Foundation) && sourceTransferData.TransferBase) // 1.1
            TransferBaseOwnership(jsonObject, sourceTransferData);

        if (container.IsVersion351PrismsWithBytebeatAuthor && sourceTransferData.TransferBytebeat) // 3.51
            TransferBytebeatOwnership(jsonObject, sourceTransferData);

        if (container.IsVersion360Frontiers && sourceTransferData.TransferSettlement) // 3.6
            TransferGeneralOwnership(jsonObject, Json.GetPaths("SETTLEMENT_WITH_UID", jsonObject, sourceTransferData.UserIdentification!.UID)[0]);
    }

    /// <summary>
    /// Generic method that transfers ownerships according to the specified path.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <param name="path"></param>
    protected void TransferGeneralOwnership(JObject jsonObject, string path)
    {
        foreach (var ownership in jsonObject.SelectTokens(path))
            TransferGeneralOwnership((JObject)(ownership));
    }

    /// <summary>
    /// Transfers ownerships in the specified JSON token.
    /// </summary>
    /// <param name="jsonObject"></param>
    protected void TransferGeneralOwnership(JObject jsonObject)
    {
        // Only UID is guaranteed.
        jsonObject.SetValue(PlatformUserIdentification.UID, "OWNER_UID");

        // Replace LID, PTK, and USN if it is not empty.
        SetValueIfNullOrEmpty(jsonObject, PlatformUserIdentification.LID, "OWNER_LID");
        SetValueIfNullOrEmpty(jsonObject, PlatformUserIdentification.USN, "OWNER_USN");
        SetValueIfNullOrEmpty(jsonObject, PlatformToken, "OWNER_PTK");
    }

    /// <summary>
    /// Transfers ownerships of all selected bases.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <param name="sourceTransferData"></param>
    protected void TransferBaseOwnership(JObject jsonObject, ContainerTransferData sourceTransferData)
    {
        foreach (var persistentPlayerBase in jsonObject.SelectTokens(Json.GetPaths("PERSISTENT_PLAYER_BASE_ALL", jsonObject)[0]).Cast<JObject>())
        {
            var identifier = GetBaseIdentifier(persistentPlayerBase);

            if (sourceTransferData.TransferBaseUserDecision.TryGetValue(identifier, out var userDecision) && userDecision.DoTransfer)
                TransferGeneralOwnership((JObject)(persistentPlayerBase[Json.GetPaths("BASE_OWNER", jsonObject)[0]]!));
        }
    }

    /// <summary>
    /// Transfers ownerships of the ByteBeat library.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <param name="sourceTransferData"></param>
    protected void TransferBytebeatOwnership(JObject jsonObject, ContainerTransferData sourceTransferData)
    {
        foreach (var mySong in jsonObject.SelectTokens(Json.GetPaths("MY_SONGS_WITH_UID", jsonObject, sourceTransferData.UserIdentification!.UID)[0]).Cast<JObject>())
        {
            // TODO set value w/o jsonObject -> GetPaths returns all -> try and find
            mySong.SetValue(PlatformUserIdentification.UID, "SONG_AUTHOR_ID");
            mySong.SetValue(PlatformUserIdentification.USN, "SONG_AUTHOR_USERNAME");
            mySong.SetValue(PlatformToken, "SONG_AUTHOR_PLATFORM");
        }
    }

    #endregion

    // // FileSystemWatcher

    #region FileSystemWatcher

    /// <summary>
    /// Enables the <see cref="FileSystemWatcher"/> if settings allowing it.
    /// </summary>
    protected void EnableWatcher()
    {
        _watcher.EnableRaisingEvents = Settings.Watcher;
    }

    /// <summary>
    /// Disables the <see cref="FileSystemWatcher"/>.
    /// </summary>
    protected void DisableWatcher()
    {
        _watcher.EnableRaisingEvents = false;
    }

    /// <summary>
    /// Gets called on a watcher event and adds the new change type to the cache.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="e"></param>
    protected void OnWatcherEvent(object source, FileSystemEventArgs e)
    {
        // Workaround to update the value and keep the immediate eviction.
        var contains = _cache.TryGetValue(e.Name, out Lazy<WatcherChangeTypes> lazyType);
        if (contains)
        {
            _cache.Remove(e.Name);
            _cache.GetOrAdd(e.Name, () => (lazyType.Value | e.ChangeType), _options);
        }
        else
        {
            _cache.GetOrAdd(e.Name, () => (e.ChangeType), _options);
        }
    }

    /// <summary>
    /// Gets called when something gets evicted from cache.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="reason"></param>
    /// <param name="state"></param>
    protected virtual void OnCacheEviction(object key, object value, EvictionReason reason, object state)
    {
        /** Vanilla WatcherChangeTypes

        Created by game or an editor:
         * save.hg (Created)
         * mf_save.hg (Created)
         * save.hg (Changed)
         * mf_save.hg (Changed)

        Changed by game or an editor:
         * save.hg (Changed)
         * mf_save.hg (Changed)
         * save.hg (Changed)
         * mf_save.hg (Changed)
        
        Deleted by game or an editor:
         * save.hg (Deleted)
         * mf_save.hg (Deleted)
         */

        /** Save Streaming WatcherChangeTypes

        Created by game:
         * save.hg.stream (Created)
         * save.hg.stream (Changed)
         * mf_save.hg (Created)
         * mf_save.hg (Changed)
         * save.hg (Renamed)

        Changed by game:
         * save.hg.stream (Created)
         * save.hg.stream (Changed)
         * mf_save.hg (Changed)
         * mf_save.hg (Changed)
         * save.hg (Deleted)
         * save.hg (Renamed)

        Deleted by game:
         * save.hg (Deleted)
         * mf_save.hg (Deleted)

        All changes by an editor:
         * save.hg (Changed)
         * mf_save.hg (Changed)
         */

        if (reason is not EvictionReason.Expired and not EvictionReason.TokenExpired)
            return;

        // Choose what actually happend based on the combined change types combinations listed at the beginning of this method.
        var changeType = (WatcherChangeTypes)(value) switch
        {
            WatcherChangeTypes.Renamed => WatcherChangeTypes.Created, // Save Streaming
            WatcherChangeTypes.Deleted | WatcherChangeTypes.Renamed => WatcherChangeTypes.Changed, // Save Streaming
            WatcherChangeTypes.Created | WatcherChangeTypes.Changed => WatcherChangeTypes.Created, // Vanilla
            _ => (WatcherChangeTypes)(value),
        };
        foreach (var container in GetCacheEvictionContainers((string)(key)))
        {
            container.SetWatcherChange(changeType);
            if (container.IsSynced)
            {
                OnWatcherDecision(container, true);
            }
        }
    }

    public void OnWatcherDecision(Container container, bool execute)
    {
        Guard.IsNotNull(container);

        if (execute)
        {
            Reload(container);

            // Only when executed to keep old timestamps.
            container.RefreshFileInfo();
        }
        else
        {
            container.IsSynced = false;
        }
        container.ResolveWatcherChange();

        // Invoke as it was written but from the outside.
        container.WriteCallback.Invoke();
    }

    #endregion

    // // User Identification

    #region UserIdentification

    /// <summary>
    /// Updates the <see cref="UserIdentificationData"/> with data from all loaded containers.
    /// </summary>
    protected void UpdateUserIdentification()
    {
        PlatformUserIdentification.LID = SaveContainerCollection.Select(i => i.UserIdentification?.LID).MostCommon();
        PlatformUserIdentification.PTK = PlatformToken;
        PlatformUserIdentification.UID = SaveContainerCollection.Select(i => i.UserIdentification?.UID).MostCommon();
        PlatformUserIdentification.USN = SaveContainerCollection.Select(i => i.UserIdentification?.USN).MostCommon();
    }

    /// <summary>
    /// Gets the <see cref="UserIdentificationData"/> for this platform.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <returns></returns>
    protected UserIdentificationData GetUserIdentification(JObject jsonObject)
    {
        return new UserIdentificationData
        {
            LID = GetUserIdentification(jsonObject, "LID"),
            UID = GetUserIdentification(jsonObject, "UID"),
            USN = GetUserIdentification(jsonObject, "USN"),
            PTK = PlatformToken,
        };
    }

    /// <summary>
    /// Gets the <see cref="UserIdentificationData"/> information for the specified property key.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    protected virtual string GetUserIdentification(JObject jsonObject, string key)
    {
        key = key switch
        {
            "LID" => Json.GetPaths("OWNER_LID", jsonObject)[0],
            "UID" => Json.GetPaths("OWNER_UID", jsonObject)[0],
            "USN" => Json.GetPaths("OWNER_USN", jsonObject)[0],
            _ => string.Empty,
        };
        if (string.IsNullOrEmpty(key))
            return string.Empty;

        // ByBase is most reliable due to the BaseType, then BySettlement is second as it is still something you own, and ByDiscovery as last resort which can be a mess.
        return GetUserIdentificationByBase(jsonObject, key).MostCommon() ?? GetUserIdentificationBySettlement(jsonObject, key).MostCommon() ?? GetUserIdentificationByDiscovery(jsonObject, key).MostCommon() ?? string.Empty;
    }

    /// <summary>
    /// Gets the <see cref="UserIdentificationData"/> information for the specified property key from discoveries.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    protected virtual IEnumerable<string> GetUserIdentificationByDiscovery(JObject jsonObject, string key)
    {
        // TODO check {{0}}
        var path = Json.GetPaths("DISCOVERY_DATA_OWNERSHIP", jsonObject, key)[0];
        var expressions = new[]
        {
            Json.GetPaths("DISCOVERY_DATA_OWNERSHIP_EXPRESSION_PTK", jsonObject, PlatformToken)[0],
            Json.GetPaths("DISCOVERY_DATA_OWNERSHIP_EXPRESSION_LID", jsonObject)[0],
        };

        return GetUserIdentificationIntersection(jsonObject, path, expressions);
    }

    /// <summary>
    /// Gets the <see cref="UserIdentificationData"/> information for the specified property key from bases.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    /// <seealso href="https://stackoverflow.com/a/38256828"/>
    protected virtual IEnumerable<string> GetUserIdentificationByBase(JObject jsonObject, string key)
    {
        // TODO check {{0}}
        var path = Json.GetPaths("PERSISTENT_PLAYER_BASE_OWNERSHIP", jsonObject, key)[0];
        var expressions = new[]
        {
            Json.GetPaths("PERSISTENT_PLAYER_BASE_OWNERSHIP_EXPRESSION_TYPE", jsonObject, PersistentBaseTypesEnum.HomePlanetBase, PersistentBaseTypesEnum.FreighterBase)[0],
            Json.GetPaths("PERSISTENT_PLAYER_BASE_OWNERSHIP_EXPRESSION_PTK", jsonObject, PlatformToken)[0],
            Json.GetPaths("PERSISTENT_PLAYER_BASE_OWNERSHIP_EXPRESSION_LID", jsonObject)[0],
        };

        return GetUserIdentificationIntersection(jsonObject, path, expressions);
    }

    /// <summary>
    /// Gets the <see cref="UserIdentificationData"/> information for the specified property key from settlements.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    protected virtual IEnumerable<string> GetUserIdentificationBySettlement(JObject jsonObject, string key)
    {
        // TODO check {{0}}
        var path = Json.GetPaths("SETTLEMENT_OWNERSHIP", jsonObject, key)[0];
        var expressions = new[]
        {
            Json.GetPaths("SETTLEMENT_OWNERSHIP_EXPRESSION_PTK", jsonObject, PlatformToken)[0],
            Json.GetPaths("SETTLEMENT_OWNERSHIP_EXPRESSION_LID", jsonObject)[0],
        };

        return GetUserIdentificationIntersection(jsonObject, path, expressions);
    }

    /// <summary>
    /// Intersects the result of the specified expressions.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <param name="path"></param>
    /// <param name="expressions"></param>
    /// <returns></returns>
    protected static IEnumerable<string> GetUserIdentificationIntersection(JObject jsonObject, string path, params string[] expressions)
    {
        return (IEnumerable<string>)(Json.SelectTokensWithIntersection(jsonObject, path, expressions).Select(i => i.Value<string>()).Where(j => !string.IsNullOrWhiteSpace(j)));
    }

    #endregion
}
