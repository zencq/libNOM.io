using CommunityToolkit.Diagnostics;
using LazyCache;
using libNOM.io.Interfaces;
using libNOM.map;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace libNOM.io;


/// <summary>
/// Base for all platforms that just hook into the methods needed.
/// </summary>
public abstract class Platform : IPlatform, IEquatable<Platform>
{
    #region Constant

    internal virtual int COUNT_SLOTS => 15; // { get; }
    internal virtual int COUNT_SAVES_PER_SLOT => 2; // { get; }
    internal int COUNT_SAVES_TOTAL => COUNT_SLOTS * COUNT_SAVES_PER_SLOT; // { get; }

    protected abstract int META_LENGTH_TOTAL_VANILLA { get; }
    protected abstract int META_LENGTH_TOTAL_WAYPOINT { get; }

    #endregion

    #region Field

    protected readonly IAppCache _cache = new CachingService();
    protected readonly LazyCacheEntryOptions _options = new();
    protected readonly FileSystemWatcher _watcher = new();

    #endregion

    #region Property

    #region Container

    protected Container AccountContainer { get; set; }

    protected List<Container> SaveContainerCollection { get; } = new();

    #endregion

    #region Configuration

    public DirectoryInfo Location { get; protected set; }

    protected int AnchorFileIndex { get; set; }

    public PlatformSettings Settings { get; protected set; }

    #endregion

    #region Flags

    public abstract bool CanCreate { get; }

    public abstract bool CanRead { get; }

    public abstract bool CanUpdate { get; }

    public abstract bool CanDelete { get; }

    public bool Exists => Location?.Exists == true; // { get; }

    public virtual bool HasAccountData => AccountContainer.Exists && AccountContainer.IsCompatible; // { get; }

    public abstract bool HasModding { get; }

    public bool IsLoaded => SaveContainerCollection.Any(); // { get; }

    public abstract bool IsPersonalComputerPlatform { get; }

    public bool IsRunning // { get; }
    {
        get
        {
            if (string.IsNullOrEmpty(PlatformProcess))
                return false;

            try
            {
                // First we get the file name of the process as name as it is different on Windows and macOS.
                var processName = Path.GetFileNameWithoutExtension(PlatformProcess);
                // Then we still need to check the MainModule to get the correct process as Steam (Windows) and Microsoft have the same name.
                var process = Process.GetProcessesByName(processName).FirstOrDefault(i => i.MainModule?.FileName?.EndsWith(PlatformProcess, StringComparison.Ordinal) == true);
                return process is not null && !process.HasExited;
            }
            // Throws Win32Exception if the implementing programm only targets x86 as the game is a x64 process.
            catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
            {
                return false;
            }
        }
    }

    public virtual bool IsValid => PlatformAnchorFileRegex.ContainsIndex(AnchorFileIndex); // { get; }

    public abstract bool RestartToApply { get; }

    #endregion

    #region Platform Indicator

    protected abstract string[] PlatformAnchorFileGlob { get; }

    protected abstract Regex[] PlatformAnchorFileRegex { get; }

    protected abstract string? PlatformArchitecture { get; }

    public abstract PlatformEnum PlatformEnum { get; }

    protected abstract string? PlatformProcess { get; }

    protected abstract string PlatformToken { get; }

    public UserIdentificationData PlatformUserIdentification { get; } = new();

    #endregion

    #endregion

    #region Getter

    #region Container

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

    /// <summary>
    /// Gets all <see cref="Container"/> affected by one cache eviction.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    protected virtual IEnumerable<Container> GetCacheEvictionContainers(string name)
    {
        return SaveContainerCollection.Where(c => c.DataFile?.Name.Equals(name, StringComparison.OrdinalIgnoreCase) == true);
    }

    public IEnumerable<Container> GetExistingContainers()
    {
        return SaveContainerCollection.Where(c => c.Exists);
    }

    public IEnumerable<Container> GetLoadedContainers()
    {
        return SaveContainerCollection.Where(c => c.IsLoaded);
    }

    public IEnumerable<Container> GetSlotContainers(int slotIndex)
    {
        return SaveContainerCollection.Where(c => c.SlotIndex == slotIndex);
    }

    public IEnumerable<Container> GetUnsyncedContainers()
    {
        return SaveContainerCollection.Where(c => c.IsLoaded && !c.IsSynced);
    }

    public IEnumerable<Container> GetWatcherContainers()
    {
        return SaveContainerCollection.Where(c => c.HasWatcherChange);
    }

    #endregion

    public int GetMaximumSlots() => COUNT_SLOTS;

    protected int GetMetaSize(Container container)
    {
        var size = (int)(container.Extra.Size);
        return (size == META_LENGTH_TOTAL_WAYPOINT || size == META_LENGTH_TOTAL_VANILLA) ? size : (container.Is400Waypoint ? META_LENGTH_TOTAL_WAYPOINT : META_LENGTH_TOTAL_VANILLA);
    }

    #endregion

    #region Path

    public string GetBackupPath()
    {
        return Path.GetFullPath(Settings.Backup);
    }

    public string GetDownloadPath()
    {
        return Path.GetFullPath(Settings.Download);
    }

    #endregion

    #region Setter

    /// <summary>
    /// Updates the instance with a new configuration.
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
            AccountContainer = null;
            SaveContainerCollection.Clear();

            DisableWatcher();
        }
        else if (Settings.LoadingStrategy > LoadingStrategyEnum.Empty && oldStrategy == LoadingStrategyEnum.Empty)
        {
            GeneratePlatformData();
        }
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
    /// <exception cref="ArgumentNullException"/>
    protected virtual void InitializeComponent(DirectoryInfo? directory, PlatformSettings? platformSettings)
    {
        // Does not make sense to continue without a directory.
        Guard.IsNotNull(directory);

        AnchorFileIndex = GetAnchorFileIndex(directory);
        Location = directory;
        Settings = platformSettings ?? new();

        // Stop if not valid/no anchor file found.
        if (!IsValid)
            return;

        // Watcher
        _options.RegisterPostEvictionCallback(OnCacheEviction);
        _options.SetAbsoluteExpiration(TimeSpan.FromMilliseconds(Globals.Constants.CACHE_EXPIRATION), ExpirationMode.ImmediateEviction);

        _watcher.Changed += OnWatcherEvent;
        _watcher.Created += OnWatcherEvent;
        _watcher.Deleted += OnWatcherEvent;
        _watcher.Renamed += OnWatcherEvent;

        _watcher.Filter = PlatformAnchorFileGlob[AnchorFileIndex];
        _watcher.Path = Location.FullName;

        // Loading
        GeneratePlatformData();
    }

    /// <summary>
    /// Gets the index of the matching anchor file.
    /// </summary>
    /// <param name="directory"></param>
    /// <returns></returns>
    protected int GetAnchorFileIndex(DirectoryInfo directory)
    {
        for (var i = 0; i < PlatformAnchorFileRegex.Length; i++)
        {
            if (directory.GetFiles().Any(f => PlatformAnchorFileRegex[i].IsMatch(f.Name)))
                return i;
        }
        return -1;
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

    // // System Interface

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

    // // Read / Write

    #region Generate

    /// <summary>
    /// Generates all necessary data.
    /// </summary>
    private void GeneratePlatformData()
    {
        if (Settings.LoadingStrategy == LoadingStrategyEnum.Empty)
            return;

        var collection = GenerateContainerCollection();

        SaveContainerCollection.Clear();
        SaveContainerCollection.AddRange(collection);
        SaveContainerCollection.Sort();

        UpdateUserIdentification();
        EnableWatcher();
    }

    /// <summary>
    /// Generates a collection with a <see cref="Container"/> for each possible file.
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerable<Container> GenerateContainerCollection()
    {
        var bag = new ConcurrentBag<Container>();

        var tasks = Enumerable.Range(0, Globals.Constants.OFFSET_INDEX + COUNT_SAVES_TOTAL).Select((metaIndex) =>
        {
            return Task.Run(() =>
            {
                if (metaIndex == 0)
                {
                    AccountContainer = CreateContainer(metaIndex);
                    BuildContainerFull(AccountContainer);
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
    /// Creates a <see cref="Container"/> with general and platform specific data.
    /// </summary>
    /// <param name="metaIndex"></param>
    /// <param name="extra">An optional object with additional data if necessary.</param>
    /// <returns></returns>
    private protected abstract Container CreateContainer(int metaIndex, PlatformExtra? extra);

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
            ProcessContainerData(container, binary.GetString());
    }

    /// <summary>
    /// Deserializes the read data of a <see cref="Container"/> into a JSON object.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="binary"></param>
    /// <returns></returns>
    protected virtual JObject? DeserializeContainer(Container container, byte[] binary)
    {
        JObject? jsonObject;
        try
        {
            jsonObject = binary.GetJson();
        }
        catch (Exception ex) when (ex is JsonReaderException or JsonSerializationException)
        {
            container.IncompatibilityException = ex;
            container.IncompatibilityTag = Globals.Constants.INCOMPATIBILITY_002;
            return null;
        }
        if (jsonObject is null)
        {
            container.IncompatibilityTag = Globals.Constants.INCOMPATIBILITY_003;
            return null;
        }

        if (Settings.UseMapping)
        {
            container.UnknownKeys = Mapping.Deobfuscate(jsonObject);
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

        if (!Directory.Exists(Settings.Backup))
            return;

        foreach (var file in Directory.GetFiles(Settings.Backup, $"backup.{PlatformEnum}.{container.MetaIndex:D2}.*.*.zip"))
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
                    IsBackup = true,
                    LastWriteTime = DateTimeOffset.ParseExact($"{parts[3]}", Globals.Constants.FILE_TIMESTAMP_FORMAT, CultureInfo.InvariantCulture),
                    GameVersionEnum = (GameVersionEnum)(System.Convert.ToInt32(parts[4])),
                });
            }
            catch (FormatException)
            {
                continue;
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
    protected virtual byte[] LoadContainer(Container container)
    {
        // Any incompatibility will be set again while loading.
        container.ClearIncompatibility();

        if (container.Exists)
        {
            var meta = LoadMeta(container);
            var data = LoadData(container, meta);
            if (data.IsNullOrEmpty())
            {
                container.IncompatibilityTag = Globals.Constants.INCOMPATIBILITY_001;
            }
            else
            {
                return data;
            }
        }

        container.IncompatibilityTag ??= Globals.Constants.INCOMPATIBILITY_006;
        return Array.Empty<byte>();
    }

    /// <inheritdoc cref="LoadMeta(Container, byte[])"/>
    protected uint[] LoadMeta(Container container)
    {
        // 1. Read
        return LoadMeta(container, ReadMeta(container));
    }

    /// <summary>
    /// Loads the meta file into a processable format including reading, decrypting, and decompressing.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="read">Already read contents of the meta file.</param>
    /// <returns></returns>
    protected uint[] LoadMeta(Container container, byte[] read)
    {
        // 2. Decrypt
        // 3. Decompress
        var result = DecompressMeta(container, DecryptMeta(container, read));
        // 4. Update Container Information
        UpdateContainerWithMetaInformation(container, read, result);

        return result;
    }

    /// <summary>
    /// Reads the contents of the meta file into a byte array.
    /// </summary>
    /// <param name="container"></param>
    /// <returns></returns>
    protected virtual byte[] ReadMeta(Container container)
    {
        if (container.MetaFile?.Exists != true)
            return Array.Empty<byte>();

        return File.ReadAllBytes(container.MetaFile.FullName);
    }

    /// <summary>
    /// Decrypts the meta file.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="meta"></param>
    /// <returns></returns>
    protected virtual uint[] DecryptMeta(Container container, byte[] meta)
    {
        return meta.GetUInt32();
    }

    /// <summary>
    /// Decompresses the meta file.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="meta"></param>
    /// <returns></returns>
    protected virtual uint[] DecompressMeta(Container container, uint[] meta)
    {
        return meta;
    }

    protected abstract void UpdateContainerWithMetaInformation(Container container, byte[] raw, uint[] converted);

    /// <inheritdoc cref="LoadData(Container, uint[], byte[])"/>
    protected virtual byte[] LoadData(Container container, uint[] meta)
    {
        // 1. Read
        return LoadData(container, meta, ReadData(container));
    }

    /// <summary>
    /// Loads the data file into a processable format including reading, decrypting, and decompressing.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="meta">Processed contents of the meta file.</param>
    /// <param name="read">Already read contents of the data file.</param>
    /// <returns></returns>
    protected virtual byte[] LoadData(Container container, uint[] meta, byte[] read)
    {
        // 2. Decrypt
        // 3. Decompress
        var result = DecompressData(container, meta, DecryptData(container, meta, read));
        // 4. Update Container Information
        UpdateContainerWithDataInformation(container, read, result);

        return result;
    }

    /// <summary>
    /// Reads the contents of the data file into a byte array.
    /// </summary>
    /// <param name="container"></param>
    /// <returns></returns>
    protected virtual byte[] ReadData(Container container)
    {
        if (container.DataFile?.Exists != true)
            return Array.Empty<byte>();

        return File.ReadAllBytes(container.DataFile.FullName);
    }

    /// <summary>
    /// Decrypts the data file.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="meta"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    protected virtual byte[] DecryptData(Container container, uint[] meta, byte[] data)
    {
        return data;
    }

    /// <summary>
    /// Decompresses the data file.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="meta"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    protected virtual byte[] DecompressData(Container container, uint[] meta, byte[] data)
    {
#if NETSTANDARD2_0
        // No compression for account data and before Frontiers.
        if (!container.IsSave || !data.Any() || data.Take(4).GetUInt32().FirstOrDefault() != Globals.Constants.HEADER_SAVE_STREAMING_CHUNK)
            return data;
#else
        // No compression for account data and before Frontiers.
        if (!container.IsSave || !data.Any() || data[..4].GetUInt32().FirstOrDefault() != Globals.Constants.HEADER_SAVE_STREAMING_CHUNK)
            return data;
#endif
        var concurrent = new ConcurrentDictionary<int, byte[]>();
        var headers = new List<(int Offset, int SizeCompressed, int SizeDecompressed)>();
        var offset = 0;

        while (offset < data.Length)
        {
#if NETSTANDARD2_0
            var chunkHeader = data.Skip(offset).Take(Globals.Constants.SAVE_STREAMING_HEADER_SIZE).GetUInt32();
#else
            var chunkHeader = data[offset..(offset + Globals.Constants.SAVE_STREAMING_HEADER_SIZE)].GetUInt32();
#endif
            var sizeCompressed = (int)(chunkHeader[1]);

            offset += Globals.Constants.SAVE_STREAMING_HEADER_SIZE;
            headers.Add((offset, sizeCompressed, (int)(chunkHeader[2])));
            offset += sizeCompressed;
        }
        Parallel.ForEach(headers, (header) =>
        {
#if NETSTANDARD2_0
            var source = data.Skip(header.Offset).Take(header.SizeCompressed).ToArray();
            _ = Globals.LZ4.Decode(source, out byte[] target, header.SizeDecompressed);
#else
            _ = Globals.LZ4.Decode(data[header.Offset..(header.Offset + header.SizeCompressed)], out byte[] target, header.SizeDecompressed);
#endif
            concurrent[header.Offset] = target;
        });

        IEnumerable<byte> result = Array.Empty<byte>();

        foreach (var decompressedData in concurrent.OrderBy(i => i.Key).Select(j => j.Value))
            result = result.Concat(decompressedData);

        return result.ToArray();
    }

    protected abstract void UpdateContainerWithDataInformation(Container container, byte[] raw, byte[] converted);

    #endregion

    #region Process

    /// <inheritdoc cref="ProcessContainerData(Container, JObject)"/>
    private static void ProcessContainerData(Container container, string json)
    {
        // Values relevant for AccountData first.
        container.SaveVersion = container.SaveVersion == 0 ? Json.GetVersion(json) : container.SaveVersion;

        container.SaveName = string.IsNullOrEmpty(container.SaveName) ? Json.GetSaveName(json) : container.SaveName;
        container.SaveSummary = string.IsNullOrEmpty(container.SaveSummary) ? Json.GetSaveSummary(json) : container.SaveSummary;
        container.TotalPlayTime = container.TotalPlayTime == 0 ? Json.GetTotalPlayTime(json) : container.TotalPlayTime;
        // Works after Version is set.
        container.GameModeEnum = container.GameModeEnum == PresetGameModeEnum.Unspecified ? Json.GetGameModeEnum(container, json) ?? PresetGameModeEnum.Unspecified : container.GameModeEnum;
        container.SeasonEnum = container.SeasonEnum == SeasonEnum.None ? Json.GetSeasonEnum(container) : container.SeasonEnum;
        // Works after Version and GameModeEnum and SeasonEnum are set.
        container.BaseVersion = container.BaseVersion == 0 ? Calculate.CalculateBaseVersion(container.SaveVersion, container.GameModeEnum, container.SeasonEnum) : container.BaseVersion;
        // Works after BaseVersion is set.
        container.GameVersionEnum = Json.GetGameVersionEnum(container, json);
    }

    /// <summary>
    /// Processes the read JSON object and fills the properties of the container.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="jsonObject"></param>
    private void ProcessContainerData(Container container, JObject jsonObject)
    {
        // Values relevant for AccountData first.
        container.SaveVersion = container.SaveVersion == 0 ? Json.GetVersion(jsonObject) : container.SaveVersion;

        // No need to do these things for AccountData.
        if (container.IsSave)
        {
            container.SaveName = string.IsNullOrEmpty(container.SaveName) ? Json.GetSaveName(jsonObject) : container.SaveName;
            container.SaveSummary = string.IsNullOrEmpty(container.SaveSummary) ? Json.GetSaveSummary(jsonObject) : container.SaveSummary;
            container.TotalPlayTime = container.TotalPlayTime == 0 ? Json.GetTotalPlayTime(jsonObject) : container.TotalPlayTime;
            // Works after Version is set.
            container.GameModeEnum = container.GameModeEnum == PresetGameModeEnum.Unspecified ? Json.GetGameModeEnum(container, jsonObject) ?? PresetGameModeEnum.Unspecified : container.GameModeEnum;
            container.SeasonEnum = container.SeasonEnum == SeasonEnum.None ? Json.GetSeasonEnum(container) : container.SeasonEnum;
            // Works after Version and GameModeEnum and SeasonEnum are set.
            container.BaseVersion = container.BaseVersion == 0 ? Calculate.CalculateBaseVersion(container.SaveVersion, container.GameModeEnum, container.SeasonEnum) : container.BaseVersion;
            // Works after BaseVersion is set.
            container.GameVersionEnum = Json.GetGameVersionEnum(container, jsonObject);

            container.UserIdentification = GetUserIdentification(jsonObject);
            UpdateUserIdentification();
        }

        container.SetJsonObject(jsonObject);

        // If we are in here, the container is in sync (again).
        container.IsSynced = true;
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

            var binary = LoadData(container, LoadMeta(container, meta), data);
            if (binary.IsNullOrEmpty())
            {
                container.IncompatibilityTag = Globals.Constants.INCOMPATIBILITY_001;
                return;
            }

            // Process
            if (DeserializeContainer(container, binary) is JObject jsonObject)
                Rebuild(container, jsonObject);
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
        Guard.IsNotNull(jsonObject);

        ProcessContainerData(container, jsonObject);
    }

    public void Reload(Container container)
    {
        if (container.IsLoaded)
        {
            BuildContainerFull(container);
        }
        else
        {
            BuildContainerHollow(container);
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

    /// <summary>
    /// Writes the specified container to drive in the most basic manner.
    /// </summary>
    /// <param name="container"></param>
    internal void JustWrite(Container container)
    {
        var data = CreateData(container);
        var meta = CreateMeta(container, data);

        WriteMeta(container, meta);
        WriteData(container, data);
    }

    /// <summary>
    /// Creates binary data file content ready to write to disk.
    /// </summary>
    /// <param name="container"></param>
    /// <returns></returns>
    protected virtual byte[] CreateData(Container container)
    {
        var plain = container.GetJsonObject()!.GetBytes(Settings.UseMapping);
        var encrypted = EncryptData(container, CompressData(container, plain));

        container.Extra = container.Extra with
        {
            SizeDecompressed = (uint)(plain.Length),
            SizeDisk = (uint)(encrypted.Length),
        };

        return encrypted;
    }


    /// <summary>
    /// Compresses the data file content.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    protected virtual byte[] CompressData(Container container, byte[] data)
    {
        if (!container.IsSave || !container.Is360Frontiers)
            return data;

        var concurrent = new ConcurrentDictionary<int, byte[]>();
        var offsets = new List<int>();
        var position = 0;

        while (position < data.Length)
        {
            offsets.Add(position);
            position += Globals.Constants.SAVE_STREAMING_CHUNK_SIZE;
        }
        Parallel.ForEach(offsets, (offset) =>
        {
#if NETSTANDARD2_0
            var source = data.Skip(offset).Take(Globals.Constants.SAVE_STREAMING_CHUNK_SIZE).ToArray();
#else
            var end = offset + Math.Min(Globals.Constants.SAVE_STREAMING_CHUNK_SIZE, data.Length - offset);
            var source = data[offset..end];
#endif
            _ = Globals.LZ4.Encode(source, out byte[] target);
            var chunkHeader = new uint[]
            {
                Globals.Constants.HEADER_SAVE_STREAMING_CHUNK,
                (uint)(target.Length),
                (uint)(source.Length),
                0,
            };
            concurrent[offset] = chunkHeader.GetBytes().Concat(target).ToArray();
        });

        IEnumerable<byte>? result = Array.Empty<byte>();

        foreach (var compressedData in concurrent.OrderBy(i => i.Key).Select(j => j.Value))
            result = result.Concat(compressedData);

        return result.ToArray();
    }

    /// <summary>
    /// Encrypts the data file content.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    protected virtual byte[] EncryptData(Container container, byte[] data)
    {
        return data;
    }

    /// <summary>
    /// Writes the data file content to disk.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="data"></param>
    protected virtual void WriteData(Container container, byte[] data)
    {
        File.WriteAllBytes(container.DataFile!.FullName, data);
    }

    /// <summary>
    /// Creates binary meta file content ready to write to disk.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="data"></param>
    /// <param name="decompressedSize"></param>
    /// <returns></returns>
    protected abstract byte[] CreateMeta(Container container, byte[] data);

    /// <summary>
    /// Compresses the meta file content.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="data"></param>
    /// <param name="meta"></param>
    /// <returns></returns>
    protected virtual byte[] CompressMeta(Container container, byte[] data, byte[] meta)
    {
        return meta;
    }

    /// <summary>
    /// Encrypts the meta file content.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="data"></param>
    /// <param name="meta"></param>
    /// <returns></returns>
    protected virtual byte[] EncryptMeta(Container container, byte[] data, byte[] meta)
    {
        return meta;
    }

    /// <summary>
    /// Writes the meta file content to disk.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="meta"></param>
    protected virtual void WriteMeta(Container container, byte[] meta)
    {
        File.WriteAllBytes(container.MetaFile!.FullName, meta);
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
        var name = $"backup.{PlatformEnum}.{container.MetaIndex:D2}.{createdAt.ToString(Globals.Constants.FILE_TIMESTAMP_FORMAT)}.{container.GameVersionEnum.Numerate()}.zip".ToLowerInvariant();
        var path = Path.Combine(Settings.Backup, name);

        Directory.CreateDirectory(Settings.Backup);
        using (var zip = ZipFile.Open(path, ZipArchiveMode.Update))
        {
            _ = zip.CreateEntryFromFile(container.DataFile.FullName, "data");
            if (container.MetaFile?.Exists == true)
            {
                _ = zip.CreateEntryFromFile(container.MetaFile.FullName, "meta");
            }
        }

        // Create new backup container.
        var backup = new Container(container.MetaIndex)
        {
            DataFile = new(path),
            IsBackup = true,
            LastWriteTime = createdAt,
            GameVersionEnum = container.GameVersionEnum,
        };
        container.BackupCollection.Add(backup);

        // Remove the oldest backups above the maximum count.
        var outdated = container.BackupCollection.OrderByDescending(b => b.LastWriteTime).Skip(Settings.MaxBackupCount);
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
        if (container is null)
            ThrowHelper.ThrowInvalidOperationException($"{backup} is not in the collection.");

        ProcessContainerData(container, backup.GetJsonObject()!);
        container.IsSynced = false;

        container.BackupRestoredCallback.Invoke();
    }

    #endregion

    #region Copy

    public void Copy(Container source, Container destination) => Copy(new[] { (Source: source, Destination: destination) }, true);

    protected void Copy(Container source, Container destination, bool write) => Copy(new[] { (Source: source, Destination: destination) }, write);

    public void Copy(IEnumerable<(Container Source, Container Destination)> containerOperation) => Copy(containerOperation, true);

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

                // Due to this CanCreate can be true.
                if (!Destination.Exists)
                {
                    CopyPlatformExtra(Destination, Source);
                }

                // Faking relevant properties to force it to Write().
                Destination.Exists = true;
                Destination.IsSynced = false;

                // Properties requied to properly build the container below.
                Destination.SaveVersion = Source.SaveVersion;
                Destination.SaveName = Source.SaveName;
                Destination.SaveSummary = Source.SaveSummary;
                Destination.TotalPlayTime = Source.TotalPlayTime;
                Destination.GameModeEnum = Source.GameModeEnum;
                Destination.SeasonEnum = Source.SeasonEnum;
                Destination.BaseVersion = Source.BaseVersion;
                Destination.GameVersionEnum = Source.GameVersionEnum;

                Destination.SetJsonObject(Source.GetJsonObject());

                // This "if" is not really useful in this method but properly implemented nonetheless.
                if (write)
                {
                    Write(Destination, Source.LastWriteTime ?? DateTimeOffset.Now);
                    BuildContainerFull(Destination);
                }
            }
            //else
            //    continue;
        }

        UpdateUserIdentification();
    }

    /// <summary>
    /// Creates a new platform extra based on the source container.
    /// </summary>
    /// <param name="destination"></param>
    /// <param name="source"></param>
    /// <returns></returns>
    protected virtual void CopyPlatformExtra(Container destination, Container source)
    {
        destination.Extra = new()
        {
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
            container.IncompatibilityTag = Globals.Constants.INCOMPATIBILITY_006;
        }

        EnableWatcher();
    }

    #endregion

    #region Move

    public void Move(Container source, Container destination) => Move(new[] { (Source: source, Destination: destination) }, true);

    protected void Move(Container source, Container destination, bool write) => Move(new[] { (Source: source, Destination: destination) }, write);

    public void Move(IEnumerable<(Container Source, Container Destination)> containerOperationData) => Move(containerOperationData, true);

    protected virtual void Move(IEnumerable<(Container Source, Container Destination)> containerOperationData, bool write)
    {
        Copy(containerOperationData, write);
        Delete(containerOperationData.Select(i => i.Source), write);
    }

    #endregion

    #region Swap

    public void Swap(Container source, Container destination) => Swap(new[] { (Source: source, Destination: destination) }, true);

    protected void Swap(Container source, Container destination, bool write) => Swap(new[] { (Source: source, Destination: destination) }, write);

    public void Swap(IEnumerable<(Container Source, Container Destination)> containerOperationData) => Swap(containerOperationData, true);

    protected virtual void Swap(IEnumerable<(Container Source, Container Destination)> containerOperationData, bool write)
    {
        // Make sure everything can be swapped.
        foreach (var (Source, Destination) in containerOperationData.Where(i => i.Source.Exists && i.Destination.Exists))
        {
            if (!Source.IsLoaded)
                BuildContainerFull(Source);

            if (!Destination.IsLoaded)
                BuildContainerFull(Destination);

            if (!Source.IsCompatible || !Destination.IsCompatible)
                throw new InvalidOperationException($"Cannot swap as at least one container is not compatible: {Source.IncompatibilityTag} / {Destination.IncompatibilityTag}");
        }

        foreach (var (Source, Destination) in containerOperationData)
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
                    BuildContainerFull(Destination);

                    // Write Destination to Source.
                    Source.SetJsonObject(jsonObject);
                    Write(Source, writeTime ?? DateTimeOffset.Now);
                    BuildContainerFull(Source);
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

    public ContainerTransferData PrepareTransferSource(int sourceSlot)
    {
        var data = new ContainerTransferData
        {
            Containers = GetSlotContainers(sourceSlot),
            TransferBase = true,
            TransferBaseReadonly = new(),
            TransferBaseUserDecision = new(),
            TransferBytebeat = true,
            TransferDiscovery = true,
            TransferSettlement = true,
        };

        foreach (var container in data.Containers)
        {
            if (!container.Exists)
                continue;

            if (!container.IsLoaded)
                BuildContainerFull(container);

            var jsonObject = container.GetJsonObject()!;
            var usesMapping = jsonObject.UsesMapping();

            foreach (var playerBase in jsonObject.SelectTokens(usesMapping ? "PlayerStateData.PersistentPlayerBases[*]" : "6f=.F?0[*]"))
            {
                var baseType = playerBase.SelectToken(usesMapping ? "BaseType.PersistentBaseTypes" : "peI.DPp")?.Value<string>();

                var isFreighterBase = baseType == PersistentBaseTypesEnum.FreighterBase.ToString();
                var isHomePlanetBase = baseType == PersistentBaseTypesEnum.HomePlanetBase.ToString();

                var name = playerBase.SelectToken(usesMapping ? "Name" : "NKm")?.Value<string>();
                if (string.IsNullOrEmpty(name))
                {
                    if (isFreighterBase)
                    {
                        name = "Freighter Base";
                    }
                    else if (isHomePlanetBase)
                    {
                        name = "Unnamed Planet Base";
                    }
                    else
                    {
                        name = "Unknown Base Type";
                    }
                }

                var isOwnBase = isHomePlanetBase || isFreighterBase;
                var identifier = CreateBaseIdentifier((JObject)(playerBase));

                data.TransferBaseReadonly[identifier] = isOwnBase;
                data.TransferBaseUserDecision[identifier] = new() { DoTransfer = isOwnBase, Name = name! };
            }
        }

        UpdateUserIdentification();
        data.UserIdentification = PlatformUserIdentification;
        return data;
    }

    /// <summary>
    /// Creates an unique identifer for bases based on its location.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <returns></returns>
    protected static string CreateBaseIdentifier(JObject jsonObject)
    {
        var usesMapping = jsonObject.ContainsKey("GalacticAddress"); // variant of UsesMappping()

        // Indirect cast from double to int.
        var address = jsonObject[usesMapping ? "GalacticAddress" : "oZw"];
        var x = jsonObject.SelectToken(usesMapping ? "Position[0]" : "wMC[0]")!.Value<int>();
        var y = jsonObject.SelectToken(usesMapping ? "Position[1]" : "wMC[1]")!.Value<int>();
        var z = jsonObject.SelectToken(usesMapping ? "Position[2]" : "wMC[2]")!.Value<int>();

        return $"{address}{x:+0;-#}{y:+0;-#}{z:+0;-#}";
    }

    public void PrepareTransferDestination(int destinationSlot)
    {
        foreach (var container in GetSlotContainers(destinationSlot))
        {
            if (!container.Exists)
                continue;

            if (!container.IsLoaded)
                BuildContainerFull(container);
        }

        UpdateUserIdentification();
    }

    public void Transfer(ContainerTransferData sourceTransferData, int destinationSlot) => Transfer(sourceTransferData, destinationSlot, true);

    /// <inheritdoc cref="Transfer(ContainerTransferData, int)"/>
    /// <param name="write"></param>
    /// <exception cref="InvalidOperationException"></exception>
    protected virtual void Transfer(ContainerTransferData sourceTransferData, int destinationSlot, bool write)
    {
        if (!sourceTransferData.UserIdentification.IsComplete() || !PlatformUserIdentification.IsComplete())
            throw new InvalidOperationException("Cannot transfer as at least one user identification is not complete.");

        foreach (var (Source, Destination) in sourceTransferData.Containers.Zip(GetSlotContainers(destinationSlot), (Source, Destination) => (Source, Destination)))
        {
            if (!Source.Exists)
            {
                Delete(Destination, write);
            }
            else if (Destination.Exists || !Destination.Exists && CanCreate)
            {
                if (!Source.IsLoaded)
                    BuildContainerFull(Source);

                if (!Source.IsCompatible)
                    throw new InvalidOperationException($"Cannot transfer as the source container is not compatible: {Source.IncompatibilityTag}");

                // Due to this CanCreate can be true.
                if (!Destination.Exists)
                {
                    CreatePlatformExtra(Destination, Source);
                }

                // Faking relevant properties to force it to Write().
                Destination.Exists = true;
                Destination.IsSynced = false;

                // Properties requied to properly build the container below.
                Destination.SaveVersion = Source.SaveVersion;
                Destination.SaveName = Source.SaveName;
                Destination.SaveSummary = Source.SaveSummary;
                Destination.TotalPlayTime = Source.TotalPlayTime;
                Destination.GameModeEnum = Source.GameModeEnum;
                Destination.SeasonEnum = Source.SeasonEnum;
                Destination.BaseVersion = Source.BaseVersion;
                Destination.GameVersionEnum = Source.GameVersionEnum;

                Destination.SetJsonObject(Source.GetJsonObject());
                TransferOwnership(Destination, sourceTransferData);

                if (write)
                {
                    Write(Destination, Source.LastWriteTime ?? DateTimeOffset.Now);
                    BuildContainerFull(Destination);
                }
            }
            //else
            //    continue;
        }
    }

    /// <summary>
    /// Creates an empty platform extra for the transfer target.
    /// </summary>
    /// <param name="destination"></param>
    /// <param name="source"></param>
    /// <returns></returns>
    protected abstract void CreatePlatformExtra(Container destination, Container source);

    /// <summary>
    /// Transfers ownerships to new container according to the prepared data.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="sourceTransferData"></param>
    protected void TransferOwnership(Container container, ContainerTransferData sourceTransferData)
    {
        var jsonObject = container.GetJsonObject()!;
        var usesMapping = jsonObject.UsesMapping();

        // Change token for Platform.
        jsonObject[usesMapping ? "Platform" : "8>q"] = PlatformArchitecture;

        if (sourceTransferData.TransferDiscovery) // 1.0
            TransferGeneralOwnership(jsonObject, usesMapping ? $"DiscoveryManagerData.DiscoveryData-v1.Store.Record..[?(@.UID == '{sourceTransferData.UserIdentification!.UID}')]" : $"fDu.ETO.OsQ.?fB..[?(@.K7E == '{sourceTransferData.UserIdentification!.UID}')]");

        if (sourceTransferData.TransferBase) // 1.1
            TransferBaseOwnership(jsonObject, sourceTransferData);

        if (container.Is351PrismsWithBytebeatAuthor && sourceTransferData.TransferBytebeat) // 3.51
            TransferBytebeatOwnership(jsonObject, sourceTransferData);

        if (container.Is360Frontiers && sourceTransferData.TransferSettlement) // 3.6
            TransferGeneralOwnership(jsonObject, usesMapping ? $"PlayerStateData.SettlementStatesV2..[?(@.UID == '{sourceTransferData.UserIdentification!.UID}')]" : $"6f=.GQA..[?(@.K7E == '{sourceTransferData.UserIdentification!.UID}')]");

        container.SetJsonObject(jsonObject);
    }

    /// <summary>
    /// Transfers ownerships of all selected bases.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <param name="sourceTransferData"></param>
    protected void TransferBaseOwnership(JObject jsonObject, ContainerTransferData sourceTransferData)
    {
        var usesMapping = jsonObject.UsesMapping();

        foreach (var playerBase in jsonObject.SelectTokens(usesMapping ? "PlayerStateData.PersistentPlayerBases[*]" : "6f=.F?0[*]"))
        {
            var identifier = CreateBaseIdentifier((JObject)(playerBase));

            if (sourceTransferData.TransferBaseReadonly[identifier] && sourceTransferData.TransferBaseUserDecision[identifier].DoTransfer)
                TransferGeneralOwnership((JObject)(playerBase[usesMapping ? "Owner" : "3?K"]!));
        }
    }

    /// <summary>
    /// Transfers ownerships of the ByteBeat library.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <param name="sourceTransferData"></param>
    protected void TransferBytebeatOwnership(JObject jsonObject, ContainerTransferData sourceTransferData)
    {
        var uid = sourceTransferData.UserIdentification!.UID;

        if (PlatformUserIdentification is null || uid is null)
            return;

        var usesMapping = jsonObject.UsesMapping();

        foreach (var token in jsonObject.SelectTokens(usesMapping ? $"PlayerStateData.MySongs[?(@.AuthorOnlineID == '{uid}')]" : $"6f=.ON4[?(@.m7b == '{uid}')]"))
        {
            token[usesMapping ? "AuthorOnlineID" : "m7b"] = PlatformUserIdentification.UID;
            token[usesMapping ? "AuthorUsername" : "4ha"] = PlatformUserIdentification.USN;
            token[usesMapping ? "AuthorPlatform" : "d2f"] = PlatformToken;
        }
    }

    /// <summary>
    /// Generic method that transfers ownerships according to the specified path.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <param name="path"></param>
    protected void TransferGeneralOwnership(JObject jsonObject, string path)
    {
        foreach (var token in jsonObject.SelectTokens(path))
            TransferGeneralOwnership((JObject)(token));
    }

    /// <summary>
    /// Transfers ownerships in the specified JSON token.
    /// </summary>
    /// <param name="jsonObject"></param>
    protected void TransferGeneralOwnership(JObject jsonObject)
    {
        if (PlatformUserIdentification is null)
            return;

        // Determine once.
        string uid;
        string lid;
        string usn;
        string ptk;
        if (jsonObject.ContainsKey("UID")) // variant of UsesMappping()
        {
            uid = "UID";
            lid = "LID";
            usn = "USN";
            ptk = "PTK";
        }
        else
        {
            uid = "K7E";
            lid = "f5Q";
            usn = "V?:";
            ptk = "D6b";
        }

        // Only UID is guaranteed.
        jsonObject[uid] = PlatformUserIdentification.UID;

        // Replace LID, PTK, and USN if it is not empty.
        if (!string.IsNullOrEmpty(jsonObject[lid]?.Value<string>()))
            jsonObject[lid] = PlatformUserIdentification.LID;

        if (!string.IsNullOrEmpty(jsonObject[usn]?.Value<string>()))
            jsonObject[usn] = PlatformUserIdentification.USN;

        if (!string.IsNullOrEmpty(jsonObject[ptk]?.Value<string>()))
            jsonObject[ptk] = PlatformToken;
    }

    #endregion

    // // FileSystemWatcher

    #region FileSystemWatcher

    /// <summary>
    /// Enables the <see cref="FileSystemWatcher"/> if allowed in set settings.
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
    /// Gets called when evicted from cache.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="reason"></param>
    /// <param name="state"></param>
    protected virtual void OnCacheEviction(object key, object value, EvictionReason reason, object state)
    {
        /** Save Streaming (GOG.com, Steam)
         *
         * save.hg.stream (Created)
         * save.hg.stream (Changed)
         * mf_save.hg (Created)
         * mf_save.hg (Changed)
         * save.hg (Renamed)
         *
         * save.hg.stream (Created)
         * save.hg.stream (Changed)
         * mf_save.hg (Changed)
         * mf_save.hg (Changed)
         * save.hg (Deleted)
         * save.hg (Renamed)
         *
         * save.hg (Deleted)
         * mf_save.hg (Deleted)
         */
        /** Previous Format (GOG.com, Steam)
         * save.hg (Created)
         * mf_save.hg (Created)
         * save.hg (Changed)
         * mf_save.hg (Changed)
         *
         * save.hg (Changed)
         * mf_save.hg (Changed)
         * save.hg (Changed)
         * mf_save.hg (Changed)
         *
         * save.hg (Deleted)
         * mf_save.hg (Deleted)
         */
        /** Microsoft
         * containers.index (Deleted)
         * containers.index (Created)
         */
        /** Editor (All)
         * save.hg (Changed)
         */

        if (reason is not EvictionReason.Expired and not EvictionReason.TokenExpired)
            return;

        // Choose what actually happend based on the combined change types combinations listed at the beginning of this method.
        var changeType = (WatcherChangeTypes)(value) switch
        {
            WatcherChangeTypes.Renamed => WatcherChangeTypes.Created, // Save Streaming
            WatcherChangeTypes.Deleted | WatcherChangeTypes.Renamed => WatcherChangeTypes.Changed, // Save Streaming
            WatcherChangeTypes.Created | WatcherChangeTypes.Changed => WatcherChangeTypes.Created, // Previous Format
            WatcherChangeTypes.Deleted | WatcherChangeTypes.Created => WatcherChangeTypes.Changed, // Microsoft
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
        var usesMapping = jsonObject.UsesMapping();

        key = key switch
        {
            "LID" => usesMapping ? "LID" : "f5Q",
            "UID" => usesMapping ? "UID" : "K7E",
            "USN" => usesMapping ? "USN" : "V?:",
            _ => string.Empty,
        };
        if (string.IsNullOrEmpty(key))
            return string.Empty;

        var byBase = GetUserIdentificationByBase(jsonObject, key);
        var bySettlement = GetUserIdentificationBySettlement(jsonObject, key);

        var result = byBase.Concat(bySettlement).MostCommon();
        if (result is not null)
            return result;

        var byDiscovery = GetUserIdentificationByDiscovery(jsonObject, key);
        return byDiscovery.MostCommon() ?? string.Empty;
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
        var usesMapping = jsonObject.UsesMapping();

        var path = usesMapping ? $"PlayerStateData.PersistentPlayerBases[?({{0}})].Owner.{key}" : $"6f=.F?0[?({{0}})].3?K.{key}";
        var expressions = new[]
        {
            usesMapping ? $"@.BaseType.PersistentBaseTypes == '{PersistentBaseTypesEnum.HomePlanetBase}' || @.BaseType.PersistentBaseTypes == '{PersistentBaseTypesEnum.FreighterBase}'" : $"@.peI.DPp == '{PersistentBaseTypesEnum.HomePlanetBase}' || @.peI.DPp == '{PersistentBaseTypesEnum.FreighterBase}'", // only with own base
            usesMapping ? $"@.Owner.PTK == '' || @.Owner.PTK == '{PlatformToken}'" : $"@.3?K.D6b == '' || @.3?K.D6b == '{PlatformToken}'", // only with valid platform
            usesMapping ? $"@.Owner.LID != ''" : $"@.3?K.f5Q != ''", // only if set
        };

        return GetUserIdentificationIntersection(jsonObject, path, expressions);
    }

    /// <summary>
    /// Gets the <see cref="UserIdentificationData"/> information for the specified property key from discoveries.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    protected virtual IEnumerable<string> GetUserIdentificationByDiscovery(JObject jsonObject, string key)
    {
        var usesMapping = jsonObject.UsesMapping();

        var path = usesMapping ? $"DiscoveryManagerData.DiscoveryData-v1.Store.Record[?({{0}})].OWS.{key}" : $"fDu.ETO.OsQ.?fB[?({{0}})].ksu.{key}";
        var expressions = new[]
        {
            usesMapping ? $"@.OWS.PTK == '' || @.OWS.PTK == '{PlatformToken}'" : $"@.ksu.D6b == '' || @.ksu.D6b == '{PlatformToken}'", // only with valid platform
            usesMapping ? $"@.OWS.LID != ''" : $"@.ksu.f5Q != ''", // only if set
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
        var usesMapping = jsonObject.UsesMapping();

        var path = usesMapping ? $"PlayerStateData.SettlementStatesV2[?({{0}})].Owner.{key}" : $"6f=.GQA[?({{0}})].3?K.{key}";
        var expressions = new[]
        {
            usesMapping ? $"@.Owner.PTK == '{PlatformToken}'" : $"@.3?K.D6b == '{PlatformToken}'", // only with valid platform
            usesMapping ? $"@.Owner.LID != ''" : $"@.3?K.f5Q != ''", // only if set
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
        if (expressions.Length == 0)
            return Array.Empty<string>();

        IEnumerable<string> result = null!;
        foreach (var expression in expressions)
        {
            var query = (IEnumerable<string>)(jsonObject.SelectTokens(string.Format(path, expression)).Select(i => i.Value<string>()).Where(j => !string.IsNullOrWhiteSpace(j)));
            result = result is null ? query : result.Intersect(query);
        }
        return result;
    }

    /// <summary>
    /// Updates the <see cref="UserIdentificationData"/> with data from all loaded containers.
    /// </summary>
    protected void UpdateUserIdentification()
    {
        // TODO check calls
        PlatformUserIdentification.LID = GetUserIdentificationPropertyValue(nameof(UserIdentificationData.LID));
        PlatformUserIdentification.PTK = PlatformToken;
        PlatformUserIdentification.UID = GetUserIdentificationPropertyValue(nameof(UserIdentificationData.UID));
        PlatformUserIdentification.USN = GetUserIdentificationPropertyValue(nameof(UserIdentificationData.USN));
    }

    /// <summary>
    /// Collects the <see cref="UserIdentificationData"/> for the platform by searching through all containers.
    /// </summary>
    /// <param name="propertyName"></param>
    /// <returns></returns>
    protected string? GetUserIdentificationPropertyValue(string propertyName)
    {
        return (propertyName switch
        {
            nameof(UserIdentificationData.LID) => SaveContainerCollection.Select(i => i.UserIdentification?.LID),
            nameof(UserIdentificationData.UID) => SaveContainerCollection.Select(i => i.UserIdentification?.UID),
            nameof(UserIdentificationData.USN) => SaveContainerCollection.Select(i => i.UserIdentification?.USN),
            _ => Array.Empty<string?>(),
        }).Where(j => !string.IsNullOrWhiteSpace(j)).MostCommon();
    }

    #endregion
}
