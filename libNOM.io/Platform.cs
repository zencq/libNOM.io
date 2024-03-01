using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;

using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;

using LazyCache;

using libNOM.io.Interfaces;

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

    protected virtual int COUNT_SAVE_SLOTS { get; } = 15; // overrideable for compatibility with old PlayStation format
    private int COUNT_SAVES_PER_SLOT { get; } = 2;
    internal int COUNT_SAVES_TOTAL => COUNT_SAVE_SLOTS * COUNT_SAVES_PER_SLOT; // { get; }

    protected virtual int META_LENGTH_KNOWN { get; } = -1; // in fact, everything is known, but named as such because everything after that can contain additional junk data
    internal abstract int META_LENGTH_TOTAL_VANILLA { get; }
    internal abstract int META_LENGTH_TOTAL_WAYPOINT { get; }

    #endregion

    #region Field

    protected readonly IAppCache _cache = new CachingService();
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

    public PlatformSettings Settings { get; protected set; }

    // protected //

    protected int AnchorFileIndex { get; set; } = -1;

    #endregion

    #region Flags

    // public //

    public abstract bool CanCreate { get; }

    public abstract bool CanRead { get; }

    public abstract bool CanUpdate { get; }

    public abstract bool CanDelete { get; }

    public virtual bool Exists => Location?.Exists ?? false; // { get; }

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

    public virtual bool IsValid => PlatformAnchorFilePattern.ContainsIndex(AnchorFileIndex); // { get; }

    public abstract bool RestartToApply { get; }

    // protected //

    protected abstract bool IsConsolePlatform { get; }

    #endregion

    #region Platform Indicator

    // public //

    public abstract PlatformEnum PlatformEnum { get; }

    public UserIdentification PlatformUserIdentification { get; } = new();

    // protected //

    protected abstract string[] PlatformAnchorFilePattern { get; }

    protected abstract string? PlatformArchitecture { get; }

    protected abstract string? PlatformProcessPath { get; }

    protected abstract string PlatformToken { get; }

    #endregion

    #endregion

    // //

    #region Getter

    // public //

    public Container GetAccountContainer() => AccountContainer;

    public IEnumerable<Container> GetSaveContainers() => SaveContainerCollection;

    // protected //

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

    /// <summary>
    /// Gets all <see cref="Container"/> affected by one cache eviction.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    protected virtual IEnumerable<Container> GetCacheEvictionContainers(string name)
    {
        return SaveContainerCollection.Where(i => i.DataFile?.Name.Equals(name, StringComparison.OrdinalIgnoreCase) == true);
    }

    // private //

    private static IEnumerable<SaveContextQueryEnum> GetContexts(JObject jsonObject)
    {
        // Check first whether there can be context keys.
        if (Constants.JSONPATH["ACTIVE_CONTEXT"].Any(jsonObject.ContainsKey))
        {
            // Then return all contexts that are in the specified JSON.
            if (Constants.JSONPATH["BASE_CONTEXT"].Any(jsonObject.ContainsKey))
                yield return SaveContextQueryEnum.Main;
            if (Constants.JSONPATH["EXPEDITION_CONTEXT"].Any(jsonObject.ContainsKey))
                yield return SaveContextQueryEnum.Season;
        }
        else
            yield return SaveContextQueryEnum.DontCare;
    }

    #endregion

    #region Setter

    /// <summary>
    /// Updates the instance with a new configuration. If null is passed, the settings will be reset to default.
    /// </summary>
    /// <param name="platformSettings"></param>
    public void SetSettings(PlatformSettings? platformSettings)
    {
        // Cache old values first to be able to properly react to the change.
        var oldMapping = Settings.UseMapping;
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
            GeneratePlatformData(); // calls EnableWatcher()

        // Ensure mapping is updated in the containers.
        if (Settings.UseMapping != oldMapping)
            foreach (var container in SaveContainerCollection)
                container.SetJsonObject(container.GetJsonObject());
    }

    #endregion

    // //

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

        _watcher.Filter = PlatformAnchorFilePattern[AnchorFileIndex];
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
                        BuildContainerHollow(container);
                    else
                        BuildContainerFull(container);

                    GenerateBackupCollection(container);
                    bag.Add(container);
                }
            });
        });
        Task.WaitAll(tasks.ToArray());

        return bag;
    }

    /// <inheritdoc cref="CreateContainer(int, PlatformExtra?)"/>
    internal Container CreateContainer(int metaIndex) => CreateContainer(metaIndex, null);

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
                container.BackupCollection.Add(new(container.MetaIndex, this)
                {
                    DataFile = new(file),
                    GameVersion = (GameVersionEnum)(System.Convert.ToInt32(parts[4])),
                    IsBackup = true,
                    LastWriteTime = DateTimeOffset.ParseExact($"{parts[3]}", Constants.FILE_TIMESTAMP_FORMAT, CultureInfo.InvariantCulture),
                });
            }
            catch (FormatException) { } // ignore
        }
    }

    #endregion

    #region Load

    public void Load(Container container)
    {
        if (container.IsBackup)
            LoadBackupContainer(container);
        else
            LoadSaveContainer(container);
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
                container.IncompatibilityTag = Constants.INCOMPATIBILITY_001;
            else if (DeserializeContainer(container, binary) is JObject jsonObject)
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
            // Unloads data by removing the reference to the JSON object.
            var loadedContainers = SaveContainerCollection.Where(i => i.IsLoaded && !i.Equals(container));
            foreach (var loadedContainer in loadedContainers)
                loadedContainer.SetJsonObject(null);
        }

        BuildContainerFull(container);
    }

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
                container.IncompatibilityTag = Constants.INCOMPATIBILITY_001;
            else
                return data;
        }

        container.IncompatibilityTag ??= Constants.INCOMPATIBILITY_006;
        return [];
    }

    public void Reload(Container container)
    {
        if (container.IsLoaded)
            RebuildContainerFull(container);
        else
            RebuildContainerHollow(container);
    }

    public void Rebuild(Container container, JObject jsonObject) => ProcessContainerData(container, jsonObject);

    /// <summary>
    /// Rebuilds a <see cref="Container"/> by loading from disk and processing it by deserializing the data.
    /// </summary>
    /// <param name="container"></param>
    protected void RebuildContainerFull(Container container) => BuildContainerFull(container);

    /// <summary>
    /// Rebuilds a <see cref="Container"/> by loading from disk and processing it by extracting from the string representation.
    /// </summary>
    /// <param name="container"></param>
    protected void RebuildContainerHollow(Container container)
    {
        var binary = LoadContainer(container);

        if (container.IsCompatible)
            ProcessContainerData(container, binary.GetString(), true); // force
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
        return container.MetaFile?.ReadAllBytes() ?? [];
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
        return container.DataFile?.ReadAllBytes() ?? [];
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
        return jsonObject;
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
            container.SaveVersion = Meta.SaveVersion.Get(json);

        // Then all independent values.
        if (container.Extra.GameMode == 0 || force)
            container.GameMode = Meta.GameMode.Get(json);

        if (container.TotalPlayTime == 0 || force)
            container.TotalPlayTime = Meta.TotalPlayTime.Get(json);

        // Finally all remaining values that depend on others.
        if (container.GameMode == PresetGameModeEnum.Seasonal && container.Season == SeasonEnum.None || force)
            container.Season = Meta.Season.Get(json); // needs GameMode

        if (container.BaseVersion == 0 || force)
            container.BaseVersion = Meta.BaseVersion.Calculate(container); // needs SaveVersion and GameMode and Season

        if (container.GameVersion == GameVersionEnum.Unknown)
            container.GameVersion = Meta.GameVersion.Get(container.BaseVersion, json); // needs BaseVersion

        if (container.GameDifficulty == DifficultyPresetTypeEnum.Invalid || force)
            container.GameDifficulty = Meta.DifficultyPreset.Get(container, json); // needs GameMode and GameVersion

        if (container.IsVersion400Waypoint) // needs GameVersion
        {
            if (string.IsNullOrEmpty(container.SaveName) || force)
                container.SaveName = Meta.SaveName.Get(json);

            if (string.IsNullOrEmpty(container.SaveSummary) || force)
                container.SaveSummary = Meta.SaveSummary.Get(json);
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
            container.LastWriteTime = writeTime;

        if (Settings.WriteAlways || !container.IsSynced)
        {
            JustWrite(container);

            container.Exists = true;
            container.IsSynced = true;
        }

        // To ensure the timestamp will be the same the next time, the file times are always set to the currently saved one.
        container.DataFile?.SetFileTime(container.LastWriteTime);
        container.MetaFile?.SetFileTime(container.LastWriteTime);

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
        ReadOnlySpan<byte> result = [];

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
        container.DataFile?.WriteAllBytes(data);
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
    /// Adds meta data that were added with Waypoint.
    /// Contrary to the leading data, this is the same for all platforms.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="writer"></param>
    protected void AddWaypointMeta(BinaryWriter writer, Container container)
    {
        // Always append cached bytes but overwrite afterwards if Waypoint.
        writer.Write(container.Extra.Bytes ?? []); // length depends on platform

        if (container.MetaFormat >= MetaFormatEnum.Waypoint)
        {
            writer.Seek(META_LENGTH_KNOWN, SeekOrigin.Begin);
            writer.Write(container.SaveName.GetBytesWithTerminator()); // 128

            writer.Seek(META_LENGTH_KNOWN + (Constants.SAVE_RENAMING_LENGTH_MANIFEST), SeekOrigin.Begin);
            writer.Write(container.SaveSummary.GetBytesWithTerminator()); // 128

            writer.Seek(META_LENGTH_KNOWN + (Constants.SAVE_RENAMING_LENGTH_MANIFEST * 2), SeekOrigin.Begin);
            writer.Write((byte)(container.GameDifficulty)); // 1
        }
    }

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
        container.MetaFile?.WriteAllBytes(meta);
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

        Directory.CreateDirectory(Settings.Backup); // ensure directory exists
        using (var zipArchive = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            _ = zipArchive.CreateEntryFromFile(container.DataFile.FullName, "data");
            if (container.MetaFile?.Exists == true)
                _ = zipArchive.CreateEntryFromFile(container.MetaFile.FullName, "meta");
        }

        // Create new backup container.
        var backup = new Container(container.MetaIndex, this)
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
                container.BackupCollection.Remove(item);
        }

        container.BackupCreatedCallback.Invoke(backup);
    }

    public void Restore(Container backup)
    {
        // Does not make sense without it being an existing backup.
        Guard.IsTrue(backup.Exists);
        Guard.IsTrue(backup.IsBackup);

        if (!backup.IsLoaded)
            LoadBackupContainer(backup);

        if (!backup.IsCompatible)
            ThrowHelper.ThrowInvalidOperationException(backup.IncompatibilityException?.Message ?? backup.IncompatibilityTag ?? $"{backup} is incompatible.");

        var container = SaveContainerCollection.First(i => i.CollectionIndex == backup.CollectionIndex);
        Rebuild(container!, backup.GetJsonObject());

        // Set IsSynced to false as ProcessContainerData set it to true but it is not compared to the state on disk.
        container!.IsSynced = false;
        container!.BackupRestoredCallback.Invoke();
    }

    #endregion

    #region Copy

    public void Copy(Container source, Container destination) => Copy([(Source: source, Destination: destination)], true);

    public void Copy(IEnumerable<(Container Source, Container Destination)> operationData) => Copy(operationData, true);

    protected virtual void Copy(IEnumerable<(Container Source, Container Destination)> operationData, bool write)
    {
        foreach (var (Source, Destination) in operationData)
            if (!Source.Exists)
            {
                Delete(Destination, write);
            }
            else if (Destination.Exists || (!Destination.Exists && CanCreate))
            {
                if (!Source.IsLoaded)
                    BuildContainerFull(Source);

                if (!Source.IsCompatible)
                    ThrowHelper.ThrowInvalidOperationException($"Cannot copy as the source container is not compatible: {Source.IncompatibilityTag}");

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
                    Write(Destination, Source.LastWriteTime ?? DateTimeOffset.Now);
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

    public void Delete(Container container) => Delete([container], true);

    protected void Delete(Container container, bool write) => Delete([container], write);

    public void Delete(IEnumerable<Container> containers) => Delete(containers, true);

    protected virtual void Delete(IEnumerable<Container> containers, bool write)
    {
        Guard.IsTrue(CanDelete);

        DisableWatcher();

        foreach (var container in containers)
        {
            if (write)
            {
                try
                {
                    container.DataFile?.Delete();
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { } // nothing to do
                try
                {
                    container.MetaFile?.Delete();
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { } // nothing to do
            }

            container.Reset();
            container.IncompatibilityTag = Constants.INCOMPATIBILITY_006;
        }

        EnableWatcher();
    }

    #endregion

    #region Move

    public void Move(Container source, Container destination) => Move([(Source: source, Destination: destination)], true);

    protected void Move(Container source, Container destination, bool write) => Move([(Source: source, Destination: destination)], write);

    public void Move(IEnumerable<(Container Source, Container Destination)> operationData) => Move(operationData, true);

    protected virtual void Move(IEnumerable<(Container Source, Container Destination)> operationData, bool write)
    {
        Copy(operationData, write);
        Delete(operationData.Select(i => i.Source), write);
    }

    #endregion

    #region Swap

    public void Swap(Container source, Container destination) => Swap([(Source: source, Destination: destination)], true);

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
                ThrowHelper.ThrowInvalidOperationException($"Cannot swap as at least one container is not compatible: {Source.IncompatibilityTag} >> {Destination.IncompatibilityTag}");
        }

        foreach (var (Source, Destination) in operationData)
        {
            if (Source.Exists)
            {
                // Source and Destination exists. Swap.
                if (Destination.Exists)
                {
                    // Keep a copy to be able to set Source correctly after Destination is done.
                    var copy = Common.DeepCopy(Destination);

                    // Write Source to Destination.
                    Destination.SetJsonObject(Source.GetJsonObject());
                    CopyPlatformExtra(Destination, Source);
                    Write(Destination, Source.LastWriteTime ?? DateTimeOffset.Now);
                    RebuildContainerFull(Destination);

                    // Write Destination to Source.
                    Source.SetJsonObject(copy.GetJsonObject());
                    CopyPlatformExtra(Source, copy);
                    Write(Source, copy.LastWriteTime ?? DateTimeOffset.Now);
                    RebuildContainerFull(Source);
                }
                // Source exists only. Move to Destination.
                else
                    Move(Source, Destination, write);
            }
            // Destination exists only. Move to Source.
            else if (Destination.Exists)
                Move(Destination, Source, write);
        }

        UpdateUserIdentification();
    }

    #endregion

    #region Transfer

    public TransferData GetSourceTransferData(int sourceSlotIndex)
    {
        PrepareUserIdentification();

        var sourceTransferData = new TransferData(SaveContainerCollection.Where(i => i.SlotIndex == sourceSlotIndex), true, [], true, true, true, PlatformUserIdentification);

        foreach (var container in sourceTransferData.Containers)
        {
            if (!container.Exists)
                continue;

            if (!container.IsLoaded)
                BuildContainerFull(container);

            var jsonObject = container.GetJsonObject();

                var expressions = new[]
                {
                Json.GetPath("INTERSECTION_PERSISTENT_PLAYER_BASE_OWNERSHIP_EXPRESSION_TYPE_OR_TYPE", jsonObject),
                Json.GetPath("INTERSECTION_PERSISTENT_PLAYER_BASE_OWNERSHIP_EXPRESSION_THIS_UID", jsonObject, PlatformUserIdentification.UID),
                };

            foreach (var context in GetContexts(jsonObject))
            {
                var path = Json.GetPath("INTERSECTION_PERSISTENT_PLAYER_BASE_FOR_TRANSFER", jsonObject, context);
                foreach (var persistentPlayerBase in jsonObject.SelectTokensWithIntersection<JObject>(expressions.Select(i => string.Format(path, i))))
                {
                    var name = persistentPlayerBase.GetValue<string>("RELATIVE_BASE_NAME");
                    if (string.IsNullOrEmpty(name))
                        name = EnumExtensions.Parse<PersistentBaseTypesEnum>(persistentPlayerBase.GetValue<string>("RELATIVE_BASE_OWNER")) switch
                        {
                            PersistentBaseTypesEnum.FreighterBase => "Freighter Base",
                            PersistentBaseTypesEnum.HomePlanetBase => "Unnamed Planet Base",
                            _ => "Unnamed Base",
                        };

                    sourceTransferData.TransferBaseUserDecision[GetBaseIdentifier(persistentPlayerBase)] = new(context, name!, true);
                }
            }
        }

        UpdateUserIdentification();

        return sourceTransferData with { UserIdentification = PlatformUserIdentification };
    }

    private void PrepareUserIdentification()
    {
        // If user identification is not complete, load saves until it is.
        foreach (var container in SaveContainerCollection.Where(i => i.Exists && !i.IsLoaded))
        {
            // Faking while-loop by checking first.
            if (PlatformUserIdentification.IsComplete())
                break;

            BuildContainerFull(container);
        }
    }

    /// <summary>
    /// Creates an unique identifier for bases based on its location.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <returns></returns>
    private static string GetBaseIdentifier(JObject jsonObject)
    {
#if NETSTANDARD2_0
        var galacticAddress = jsonObject.GetValue<string>("RELATIVE_BASE_GALACTIC_ADDRESS")!;
        var galacticInteger = galacticAddress.StartsWith("0x") ? long.Parse(galacticAddress.Substring(2), NumberStyles.HexNumber) : long.Parse(galacticAddress);
#else
        ReadOnlySpan<char> galacticAddress = jsonObject.GetValue<string>("RELATIVE_BASE_GALACTIC_ADDRESS");
        var galacticInteger = galacticAddress.StartsWith("0x") ? long.Parse(galacticAddress[2..], NumberStyles.HexNumber) : long.Parse(galacticAddress);
#endif

        var positionX = jsonObject.GetValue<int>("RELATIVE_BASE_POSITION_0");
        var positionY = jsonObject.GetValue<int>("RELATIVE_BASE_POSITION_1");
        var positionZ = jsonObject.GetValue<int>("RELATIVE_BASE_POSITION_2");

        return $"{galacticInteger}{positionX:+000000;-000000}{positionY:+000000;-000000}{positionZ:+000000;-000000}";
    }

    /// <summary>
    /// Ensures that the destination is prepared for the incoming <see cref="Transfer(TransferData, int)"/>.
    /// Mainly to lookup the user identification.
    /// </summary>
    /// <param name="destinationSlotIndex"></param>
    protected void PrepareTransferDestination(int destinationSlotIndex)
    {
        // Load destination as they are needed anyway.
        foreach (var container in SaveContainerCollection.Where(i => i.SlotIndex == destinationSlotIndex))
            if (container.Exists && !container.IsLoaded)
                BuildContainerFull(container);

        PrepareUserIdentification();
    }

    public void Transfer(TransferData sourceTransferData, int destinationSlotIndex) => Transfer(sourceTransferData, destinationSlotIndex, true);

    /// <inheritdoc cref="Transfer(TransferData, int)"/>
    /// <param name="write"></param>
    /// <exception cref="InvalidOperationException"></exception>
    protected virtual void Transfer(TransferData sourceTransferData, int destinationSlotIndex, bool write)
    {
        PrepareTransferDestination(destinationSlotIndex);

        if (!sourceTransferData.UserIdentification.IsComplete() || !PlatformUserIdentification.IsComplete())
            ThrowHelper.ThrowInvalidOperationException("Cannot transfer as at least one user identification is not complete.");

        foreach (var (Source, Destination) in sourceTransferData.Containers.Zip(SaveContainerCollection.Where(i => i.SlotIndex == destinationSlotIndex), (Source, Destination) => (Source, Destination)))
            if (!Source.Exists)
            {
                Delete(Destination, write);
            }
            else if (Destination.Exists || (!Destination.Exists && CanCreate))
            {
                if (!Source.IsCompatible)
                    ThrowHelper.ThrowInvalidOperationException($"Cannot copy as the source container is not compatible: {Source.IncompatibilityTag}");

                Destination.SetPlatform(this);
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
                    Write(Destination, Source.LastWriteTime ?? DateTimeOffset.Now);
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

        // Reset bytes as from another platform it would not be right.
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
    protected void TransferOwnership(Container container, TransferData sourceTransferData)
    {
        var jsonObject = container.GetJsonObject();

        // Change token for Platform.
        jsonObject.SetValue(PlatformArchitecture, "PLATFORM");

        if (sourceTransferData.TransferDiscovery) // 1.0
            TransferGeneralOwnership(jsonObject, sourceTransferData, SaveContextQueryEnum.DontCare, "TRANSFER_UID_DISCOVERY");

        if (container.IsVersion(GameVersionEnum.Foundation) && sourceTransferData.TransferBase) // 1.1
            foreach (var context in GetContexts(jsonObject))
                TransferBaseOwnership(jsonObject, sourceTransferData, context);

        if (container.IsVersion351PrismsWithBytebeatAuthor && sourceTransferData.TransferBytebeat) // 3.51
            TransferBytebeatOwnership(jsonObject, sourceTransferData);

        if (container.IsVersion360Frontiers && sourceTransferData.TransferSettlement) // 3.6
            foreach (var context in GetContexts(jsonObject))
                TransferGeneralOwnership(jsonObject, sourceTransferData, context, "TRANSFER_UID_SETTLEMENT");
    }

    /// <summary>
    /// Generic method that transfers ownerships according to the specified path.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <param name="sourceTransferData"></param>
    /// <param name="context"></param>
    /// <param name="pathIdentifier"></param>
    protected void TransferGeneralOwnership(JObject jsonObject, TransferData sourceTransferData, SaveContextQueryEnum context, string pathIdentifier)
    {
        var path = Json.GetPath(pathIdentifier, jsonObject, context, sourceTransferData.UserIdentification.UID);
        foreach (var ownership in jsonObject.SelectTokens(path).Cast<JObject>())
            TransferGeneralOwnership(ownership);
    }

    /// <summary>
    /// Transfers ownerships in the specified JSON token.
    /// </summary>
    /// <param name="jsonObject"></param>
    protected void TransferGeneralOwnership(JObject jsonObject)
    {
        // Only UID is guaranteed.
        jsonObject.SetValue(PlatformUserIdentification.UID, "RELATIVE_OWNER_UID");

        // Replace LID, PTK, and USN if it is not empty.
        jsonObject.SetValueIfNotNullOrEmpty(PlatformUserIdentification.LID, "RELATIVE_OWNER_LID");
        jsonObject.SetValueIfNotNullOrEmpty(PlatformUserIdentification.USN, "RELATIVE_OWNER_USN");
        jsonObject.SetValueIfNotNullOrEmpty(PlatformToken, "RELATIVE_OWNER_PTK");
    }

    /// <summary>
    /// Transfers ownerships of all selected bases.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <param name="sourceTransferData"></param>
    /// <param name="context"></param>
    protected void TransferBaseOwnership(JObject jsonObject, TransferData sourceTransferData, SaveContextQueryEnum context)
    {
        var path = Json.GetPath("TRANSFER_UID_BASE", jsonObject, context);
        foreach (var persistentPlayerBase in jsonObject.SelectTokens(path).Cast<JObject>())
            if (sourceTransferData.TransferBaseUserDecision.TryGetValue(GetBaseIdentifier(persistentPlayerBase), out var userDecision) && userDecision.DoTransfer)
                TransferGeneralOwnership(persistentPlayerBase.GetValue<JObject>("RELATIVE_BASE_OWNER")!);
    }

    /// <summary>
    /// Transfers ownerships of the ByteBeat library.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <param name="sourceTransferData"></param>
    protected void TransferBytebeatOwnership(JObject jsonObject, TransferData sourceTransferData)
    {
        var path = Json.GetPath("TRANSFER_UID_BYTEBEAT", jsonObject, sourceTransferData.UserIdentification.UID);
        foreach (var mySong in jsonObject.SelectTokens(path).Cast<JObject>())
        {
            mySong.SetValueIfNotNullOrEmpty(PlatformUserIdentification.UID, "RELATIVE_SONG_AUTHOR_ID");
            mySong.SetValueIfNotNullOrEmpty(PlatformUserIdentification.USN, "RELATIVE_SONG_AUTHOR_USERNAME");
            mySong.SetValueIfNotNullOrEmpty(PlatformToken, "RELATIVE_SONG_AUTHOR_PLATFORM");
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
        if (_cache.TryGetValue(e.Name, out Lazy<WatcherChangeTypes> lazyType))
        {
            _cache.Remove(e.Name);
            _cache.GetOrAdd(e.Name, () => (lazyType.Value | e.ChangeType), _options);
        }
        else
            _cache.GetOrAdd(e.Name, () => (e.ChangeType), _options);
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

        // Choose what actually happened based on the combined change types combinations listed at the beginning of this method.
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
                OnWatcherDecision(container, true);
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
            container.IsSynced = false;

        container.ResolveWatcherChange();

        // Invoke as it was written but from the outside.
        container.WriteCallback.Invoke();
    }

    #endregion

    // // User Identification

    #region UserIdentification

    /// <summary>
    /// Updates the <see cref="UserIdentification"/> with data from all loaded containers.
    /// </summary>
    protected void UpdateUserIdentification()
    {
        PlatformUserIdentification.LID = SaveContainerCollection.Select(i => i.UserIdentification?.LID).MostCommon();
        PlatformUserIdentification.PTK = PlatformToken;
        PlatformUserIdentification.UID = SaveContainerCollection.Select(i => i.UserIdentification?.UID).MostCommon();
        PlatformUserIdentification.USN = SaveContainerCollection.Select(i => i.UserIdentification?.USN).MostCommon();
    }

    /// <summary>
    /// Gets the <see cref="UserIdentification"/> for this platform.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <returns></returns>
    protected UserIdentification GetUserIdentification(JObject jsonObject)
    {
        return new UserIdentification
        {
            LID = GetUserIdentification(jsonObject, "LID"),
            UID = GetUserIdentification(jsonObject, "UID"),
            USN = GetUserIdentification(jsonObject, "USN"),
            PTK = PlatformToken,
        };
    }

    /// <summary>
    /// Gets the <see cref="UserIdentification"/> information for the specified property key.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    protected virtual string GetUserIdentification(JObject jsonObject, string key)
    {
        // Utilize GetPath() to get the right obfuscation state of the key.
        key = key switch
        {
            "LID" => Json.GetPath("RELATIVE_OWNER_LID", jsonObject),
            "UID" => Json.GetPath("RELATIVE_OWNER_UID", jsonObject),
            "USN" => Json.GetPath("RELATIVE_OWNER_USN", jsonObject),
            _ => string.Empty,
        };
        if (string.IsNullOrEmpty(key))
            return string.Empty;

        // ByBase is most reliable due to the BaseType, then BySettlement is second as it is still something you own, and ByDiscovery as last resort which can be a mess.
        return GetUserIdentificationByBase(jsonObject, key).MostCommon() ?? GetUserIdentificationBySettlement(jsonObject, key).MostCommon() ?? GetUserIdentificationByDiscovery(jsonObject, key).MostCommon() ?? string.Empty;
    }

    /// <summary>
    /// Gets the <see cref="UserIdentification"/> information for the specified property key from bases.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    /// <seealso href="https://stackoverflow.com/a/38256828"/>
    protected virtual string? GetUserIdentificationByBase(JObject jsonObject, string key)
    {
        var expressions = GetIntersectionExpressionsByBase(jsonObject);
        var result = new List<string>();

        foreach (var context in GetContexts(jsonObject))
        {
            var path = Json.GetPath("INTERSECTION_PERSISTENT_PLAYER_BASE_OWNERSHIP_KEY", jsonObject, context, key);
            result.AddRange(expressions.Select(i => string.Format(path, i)));
    }

        return jsonObject.SelectTokensWithIntersection<string>(result).MostCommon();
    }

    protected virtual string[] GetIntersectionExpressionsByBase(JObject jsonObject)
    {
        return
        [
            Json.GetPath("INTERSECTION_PERSISTENT_PLAYER_BASE_OWNERSHIP_EXPRESSION_TYPE_OR_TYPE", jsonObject, PersistentBaseTypesEnum.HomePlanetBase, PersistentBaseTypesEnum.FreighterBase),
            Json.GetPath("INTERSECTION_PERSISTENT_PLAYER_BASE_OWNERSHIP_EXPRESSION_PTK", jsonObject, PlatformToken),
            Json.GetPath("INTERSECTION_PERSISTENT_PLAYER_BASE_OWNERSHIP_EXPRESSION_WITH_LID", jsonObject),
        ];
    }

    /// <summary>
    /// Gets the <see cref="UserIdentification"/> information for the specified property key from discoveries.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    protected virtual string? GetUserIdentificationByDiscovery(JObject jsonObject, string key)
    {
        var path = Json.GetPath("INTERSECTION_DISCOVERY_DATA_OWNERSHIP_KEY", jsonObject, key);
        var result = GetIntersectionExpressionsByDiscovery(jsonObject).Select(i => string.Format(path, i));

        return jsonObject.SelectTokensWithIntersection<string>(result).MostCommon();
    }

    protected virtual string[] GetIntersectionExpressionsByDiscovery(JObject jsonObject)
    {
        return
        [
            Json.GetPath("INTERSECTION_DISCOVERY_DATA_OWNERSHIP_EXPRESSION_PTK", jsonObject, PlatformToken),
            Json.GetPath("INTERSECTION_DISCOVERY_DATA_OWNERSHIP_EXPRESSION_WITH_LID", jsonObject),
        ];
    }

    /// <summary>
    /// Gets the <see cref="UserIdentification"/> information for the specified property key from settlements.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    protected virtual string? GetUserIdentificationBySettlement(JObject jsonObject, string key)
    {
        var expressions = GetIntersectionExpressionsBySettlement(jsonObject);
        var result = new List<string>();

        foreach (var context in GetContexts(jsonObject))
        {
            var path = Json.GetPath("INTERSECTION_SETTLEMENT_OWNERSHIP_KEY", jsonObject, context, key);
            result.AddRange(expressions.Select(i => string.Format(path, i)));
        }

        return jsonObject.SelectTokensWithIntersection<string>(result).MostCommon();
    }

    protected virtual string[] GetIntersectionExpressionsBySettlement(JObject jsonObject)
    {
        return
        [
            Json.GetPath("INTERSECTION_SETTLEMENT_OWNERSHIP_EXPRESSION_PTK", jsonObject, PlatformToken),
            Json.GetPath("INTERSECTION_SETTLEMENT_OWNERSHIP_EXPRESSION_WITH_LID", jsonObject),
        ];
    }

    #endregion
}
