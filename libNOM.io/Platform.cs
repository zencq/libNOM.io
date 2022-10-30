using K4os.Compression.LZ4;
using LazyCache;
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
public abstract partial class Platform
{
    #region Constant

    protected const int CACHE_EXPIRATION = 250; // milliseconds
    protected const VersionEnum LOWEST_SUPPORTED_VERSION = VersionEnum.BeyondWithVehicleCam;
    protected const uint META_HEADER = 0xCA55E77E;
    protected const uint SAVE_FORMAT_100 = 0x7D0; // 2000
    protected const uint SAVE_FORMAT_110 = 0x7D1; // 2001
    protected const uint SAVE_FORMAT_360 = 0x7D2; // 2002
    protected const int SAVE_STREAMING_CHUNK_HEADER_SIZE = 0x10; // 16
    protected const int SAVE_STREAMING_CHUNK_SIZE = 0x80000; // 524288

    // Overrideable
    internal virtual int COUNT_SAVES_PER_SLOT => 2; // { get; }
    internal virtual int COUNT_SLOTS => 15; // { get; }

    #endregion

    #region Field

    protected readonly IAppCache _cache = new CachingService();
    protected bool _init;
    protected readonly LazyCacheEntryOptions _options = new();
    protected FileSystemWatcher _watcher = new();

    #endregion

    #region Property

    #region Container

    protected Container? AccountContainer { get; set; }

    protected List<Container> ContainerCollection { get; } = new();

    #endregion

    #region Configuration

    public DirectoryInfo? Location { get; protected set; }

    protected int AnchorFileIndex { get; set; }

    protected PlatformSettings Settings { get; set; } = null!; // is set in InitializeComponent no matter what

    #endregion

    #region Flags

    public virtual bool CanCreate { get; }

    public virtual bool CanRead { get; }

    public virtual bool CanUpdate { get; }

    public virtual bool CanDelete { get; }

    public bool Exists => Location?.Exists == true; // { get; }

    public bool IsLoaded => ContainerCollection.Any(); // { get; }

    /// <summary>
    /// Whether the game is currently running on this platform.
    /// Throws a Win32Exception if the using app only targets x86 as the game is a x64 process.
    /// </summary>
    /// <exception cref="Win32Exception" />
    public bool IsRunning // { get; }
    {
        get
        {
            if (string.IsNullOrEmpty(ProcessPath))
                return false;

            var process = Process.GetProcessesByName("NMS").FirstOrDefault(p => p.MainModule?.FileName?.EndsWith(ProcessPath, StringComparison.Ordinal) == true);
            return process is not null && !process.HasExited;
        }
    }

    public virtual bool IsValid => PlatformDirectoryData.AnchorFileRegex.ContainsIndex(AnchorFileIndex); // { get; }

    public virtual bool IsWindowsPlatform { get; }

    public virtual bool HasAccountData => AccountContainer?.Exists == true; // { get; }

    public virtual bool HasModding { get; }

    public virtual bool RestartToApply { get; }

    #endregion

    #region Platform Indicator

    internal abstract PlatformDirectoryData PlatformDirectoryData { get; }

    protected abstract string PlatformArchitecture { get; }

    public abstract PlatformEnum PlatformEnum { get; }

    protected abstract string PlatformToken { get; }

    public UserIdentificationData PlatformUserIdentification { get; } = new();

    #endregion

    #region Process (System)

    protected virtual string? ProcessPath { get; }

    #endregion

    #endregion

    #region Getter

    /// <summary>
    /// Gets the index of the index of the anchor file defined in <see cref="PlatformDirectoryData"/>.
    /// </summary>
    /// <param name="directory"></param>
    /// <returns></returns>
    protected int GetAnchorFileIndex(DirectoryInfo directory)
    {
        for (var i = 0; i < PlatformDirectoryData.AnchorFileRegex.Length; i++)
        {
            if (directory.GetFiles().Any(f => PlatformDirectoryData.AnchorFileRegex[i].IsMatch(f.Name)))
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Creates an unique identifer for bases based on its location.
    /// </summary>
    /// <param name="jToken"></param>
    /// <returns></returns>
    protected string GetBaseIdentifier(JToken jToken)
    {
        // Indirect cast from double to int.
        var x = jToken.SelectToken(Settings.Mapping ? "Position[0]" : "wMC[0]")!.Value<int>();
        var y = jToken.SelectToken(Settings.Mapping ? "Position[1]" : "wMC[1]")!.Value<int>();
        var z = jToken.SelectToken(Settings.Mapping ? "Position[2]" : "wMC[2]")!.Value<int>();

        return $"{jToken[Settings.Mapping ? "GalacticAddress" : "oZw"]}{x:+0;-#}{y:+0;-#}{z:+0;-#}";
    }

    /// <summary>
    /// Collects the <see cref="UserIdentificationData"/> for the platform by searching through all containers.
    /// </summary>
    /// <param name="propertyName"></param>
    /// <returns></returns>
    protected string? GetPlatformUserIdentificationPropertyValue(string propertyName)
    {
        return (propertyName switch
        {
            "LID" => ContainerCollection.Select(c => c.UserIdentification?.LID),
            "UID" => ContainerCollection.Select(c => c.UserIdentification?.UID),
            "USN" => ContainerCollection.Select(c => c.UserIdentification?.USN),
            _ => Array.Empty<string?>(),
        }).Where(name => !string.IsNullOrWhiteSpace(name)).FirstOrDefault();
    }

    /// <summary>
    /// Returns the number of possible save slots.
    /// </summary>
    /// <returns></returns>
    public int GetPossibleSlots()
    {
        return COUNT_SLOTS;
    }

    #endregion

    #region Setter

    /// <summary>
    /// Updates the instance with the new configuration.
    /// </summary>
    /// <param name="platformSettings"></param>
    public void SetSettings(PlatformSettings? platformSettings)
    {
        // Cache old strategy first to be able to properly react to the change.
        var oldStrategy = Settings.LoadingStrategy;

        // Update.
        Settings = platformSettings ?? new();

        // Set new loadingStrategy to trigger collection operations.
        if (Settings.LoadingStrategy == LoadingStrategyEnum.Empty && oldStrategy > LoadingStrategyEnum.Empty)
        {
            ClearContainerCollection();
        }
        else if (Settings.LoadingStrategy > LoadingStrategyEnum.Empty && oldStrategy == LoadingStrategyEnum.Empty)
        {
            BuildPlatform();
        }
    }

    #endregion

    // //

    #region Constructor

    public Platform() : this(null, null) { }

    public Platform(DirectoryInfo? directory) : this(directory, null) { }

    public Platform(DirectoryInfo? directory, PlatformSettings? platformSettings)
    {
        InitializeComponent(directory, platformSettings);
    }

    /// <summary>
    /// Workaround to be able to decide when inherited classes initialize their components.
    /// </summary>
    /// <param name="directory"></param>
    /// <param name="platformSettings"></param>
    protected virtual void InitializeComponent(DirectoryInfo? directory, PlatformSettings? platformSettings)
    {
        // Make sure settings are always set.
        Settings = platformSettings ?? new();

        // Stop if no directory set.
        if (directory is null)
            return;

        Location = directory;
        AnchorFileIndex = GetAnchorFileIndex(directory);

        // Stop if no anchor file found.
        if (!IsValid)
            return;

        // Watcher
        _options.RegisterPostEvictionCallback(OnCacheEviction);
        _options.SetAbsoluteExpiration(TimeSpan.FromMilliseconds(CACHE_EXPIRATION), ExpirationMode.ImmediateEviction);

        _watcher.Changed += OnWatcherEvent;
        _watcher.Created += OnWatcherEvent;
        _watcher.Deleted += OnWatcherEvent;
        _watcher.Renamed += OnWatcherEvent;

        _watcher.Filter = PlatformDirectoryData.AnchorFileGlob[AnchorFileIndex];
        _watcher.Path = Location.FullName;

        // Loading
        BuildPlatform();
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

    // //

    #region Backup

    /// <summary>
    /// Creates a backup of the specified <see cref="Container"/>.
    /// </summary>
    /// <param name="container"></param>
    public void Backup(Container container)
    {
        // Does not make sense without the data file.
        if (container.DataFile?.Exists == true)
        {
            var createdAt = DateTime.Now;
            var name = $"backup.{PlatformEnum}.{container.MetaIndex:D2}.{createdAt.ToString(Global.FILE_TIMESTAMP_FORMAT)}.{container.VersionEnum.Numerate()}.zip";
            var path = Path.Combine(Settings.Backup, name.ToLowerInvariant());

            Directory.CreateDirectory(Settings.Backup);
            using (var zip = ZipFile.Open(path, ZipArchiveMode.Update))
            {
                _ = zip.CreateEntryFromFile(container.DataFile.FullName, "data");
                if (container.MetaFile?.Exists == true)
                {
                    _ = zip.CreateEntryFromFile(container.MetaFile.FullName, "meta");
                }
            }

            // Create new backup.
            var backup = new Container(container.MetaIndex)
            {
                DataFile = new(path),
                IsBackup = true,
                LastWriteTime = createdAt,
                VersionEnum = container.VersionEnum,
            };
            container.BackupCollection.Add(backup);

            // Remove the oldest backups above the maximum count.
            var outdated = container.BackupCollection.OrderByDescending(b => b.LastWriteTime).Skip(Settings.MaxBackupCount);
            if (outdated.Any())
            {
                Delete(outdated);
                foreach (var item in outdated) { container.BackupCollection.Remove(item); }
            }

            container.BackupCreatedCallback.Invoke(backup);
        }
    }

    /// <summary>
    /// Loads all backups into the specified <see cref="Container"/> that matches the MetaIndex and this <see cref="Platform"/>.
    /// </summary>
    /// <param name="container"></param>
    protected void LoadBackupCollection(Container container)
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
                container.BackupCollection.Add(new Container(container.MetaIndex)
                {
                    DataFile = new(file),
                    IsBackup = true,
                    LastWriteTime = DateTimeOffset.ParseExact($"{parts[3]}", Global.FILE_TIMESTAMP_FORMAT, CultureInfo.InvariantCulture),
                    VersionEnum = System.Convert.ToInt32(parts[4]).DenumerateTo<VersionEnum>(),
                });
            }
            catch (FormatException)
            {
                continue;
            }
        }
    }

    /// <summary>
    /// Loads data of a specified backup.
    /// </summary>
    /// <param name="container"></param>
    public void LoadBackupContainer(Container container)
    {
        if (!container.Exists || !container.IsBackup)
            return;

        // Reset before building/processing it (again).
        container.ClearIncompatibility();

        using var zip = ZipFile.Open(container.DataFile!.FullName, ZipArchiveMode.Read);

        if (ReadZipEntry(zip, "data", out var data))
        {
            _ = ReadZipEntry(zip, "meta", out var meta);

            var binary = LoadData(container, LoadMeta(container, meta), data);
            if (binary.IsNullOrEmpty())
            {
                container.IncompatibilityTag = "F001_Empty";
                return;
            }

            if (DeserializeContainer(container, binary) is JObject jsonObject)
            {
                ProcessContainer(container, jsonObject);
            }
        }
    }

    /// <summary>
    /// Restores an archived container by extracting the files and reloading the data.
    /// </summary>
    /// <param name="backup"></param>
    /// <exception cref="InvalidOperationException"/>
    public void Restore(Container backup)
    {
        if (!backup.Exists || !backup.IsBackup)
            return;

        if (!backup.IsLoaded)
        {
            LoadBackupContainer(backup);
        }

        if (!backup.IsCompatible)
            throw new InvalidOperationException(backup.IncompatibilityTag);

        var container = GetContainer(backup.MetaIndex);
        if (container is null)
            return;

        ProcessContainer(container, backup.GetJsonObject()!);
        container.IsSynced = false;

        container.BackupRestoredCallback.Invoke();
    }

    /// <summary>
    /// Reads the binary of a zip archive entry.
    /// </summary>
    /// <param name="zip"></param>
    /// <param name="entryName"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    protected static bool ReadZipEntry(ZipArchive zip, string entryName, out byte[] result)
    {
        var entry = zip.GetEntry(entryName);
        if (entry is null)
        {
            result = Array.Empty<byte>();
            return false;
        }

        using var stream = new MemoryStream();
        entry.Open().CopyTo(stream);
        result = stream.ToArray();
        return true;
    }

    #endregion

    #region Container

    /// <summary>
    /// Clears all available <see cref="Container"/> in this platform.
    /// </summary>
    protected void ClearContainerCollection()
    {
        AccountContainer = null;
        ContainerCollection.Clear();

        DisableWatcher();
    }

    /// <summary>
    /// Gets a <see cref="Container"/> from this platform. Index 0 is for account data and the saves start at 2.
    /// </summary>
    /// <param name="metaIndex"></param>
    /// <returns></returns>
    public Container? GetContainer(int metaIndex)
    {
        if (metaIndex is 0 or 1)
            return AccountContainer;

        var containerIndex = metaIndex - Global.OFFSET_INDEX;
        if (ContainerCollection.ContainsIndex(containerIndex))
            return ContainerCollection[containerIndex];

        return null;
    }

    /// <summary>
    /// Gets all <see cref="Container"/> cached by the <see cref="FileSystemWatcher"/>.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    protected virtual IEnumerable<Container> GetCachedContainers(string name)
    {
        return ContainerCollection.Where(c => c.DataFile!.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets all existing <see cref="Container"/> in this collection.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Container> GetExistingContainers()
    {
        return ContainerCollection.Where(c => c.IsSave && c.Exists);
    }

    /// <summary>
    /// Gets all <see cref="Container"/> that are currently loaded.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Container> GetLoadedContainer()
    {
        return ContainerCollection.Where(c => c.IsSave && c.IsLoaded);
    }

    /// <summary>
    /// Gets all <see cref="Container"/> for specified slot.
    /// </summary>
    /// <param name="slotIndex"></param>
    /// <returns></returns>
    public IEnumerable<Container> GetSlotContainers(int slotIndex)
    {
        return ContainerCollection.Where(c => c.IsSave && c.SlotIndex == slotIndex);
    }

    /// <summary>
    /// Gets all loaded but unsynced <see cref="Container"/> in this collection.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Container> GetUnsyncedContainers()
    {
        return ContainerCollection.Where(c => c.IsSave && c.IsLoaded && !c.IsSynced);
    }

    /// <summary>
    /// Gets all <see cref="Container"/> with unresolved changes by the <see cref="FileSystemWatcher"/>.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Container> GetWatcherContainers()
    {
        return ContainerCollection.Where(c => c.HasWatcherChange);
    }

    #endregion

    #region Copy

    /// <inheritdoc cref="Copy(Container, Container, bool)"/>
    public void Copy(Container source, Container destination)
    {
        Copy(source, destination, true);
    }

    /// <summary>
    /// Uses a pair of <see cref="Container"/> to copy from one location to another.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    /// <param name="write"></param>
    /// <exception cref="InvalidOperationException"/>
    protected void Copy(Container source, Container destination, bool write)
    {
        Copy(new[] { new ContainerOperationData { Destination = destination, Source = source } }, write);
    }

    /// <inheritdoc cref="Copy(IEnumerable{ContainerOperationData}, bool)"/>
    public void Copy(IEnumerable<ContainerOperationData> containerOperationData)
    {
        Copy(containerOperationData, true);
    }

    /// <summary>
    /// Uses an enumerable of <see cref="ContainerOperationData"/> to copy them from one location to another.
    /// </summary>
    /// <param name="containerOperationData"></param>
    /// <param name="write"></param>
    /// <exception cref="InvalidOperationException"/>
    protected abstract void Copy(IEnumerable<ContainerOperationData> containerOperationData, bool write);

    #endregion

    #region Delete

    /// <inheritdoc cref="Delete(IEnumerable{Container}, bool)"/>
    public void Delete(Container container)
    {
        Delete(new[] { container }, true);
    }

    /// <summary>
    /// Deletes a <see cref="Container"/>.
    /// </summary>
    /// <param name="container"></param>
    protected void Delete(Container container, bool write)
    {
        Delete(new[] { container }, write);
    }

    /// <inheritdoc cref="Delete(IEnumerable{Container}, bool)"/>
    public void Delete(IEnumerable<Container> containers)
    {
        Delete(containers, true);
    }

    /// <summary>
    /// Deletes an enumerable of <see cref="Container"/>.
    /// </summary>
    /// <param name="containers"></param>
    /// <param name="write"></param>
    protected virtual void Delete(IEnumerable<Container> containers, bool write)
    {
        if (!CanDelete)
            return;

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
                    catch (Exception x) when (x is IOException or NotSupportedException or PathTooLongException or UnauthorizedAccessException) { }
                    container.DataFile.Refresh();
                }
                if (container.MetaFile?.Exists == true)
                {
                    try
                    {
                        File.Delete(container.MetaFile.FullName);
                    }
                    catch (Exception x) when (x is IOException or NotSupportedException or PathTooLongException or UnauthorizedAccessException) { }
                    container.MetaFile.Refresh();
                }
            }
            container.Reset();
        }

        EnableWatcher();
    }

    #endregion

    #region FileSystemWatcher

    /// <summary>
    /// Disables the <see cref="FileSystemWatcher"/>.
    /// </summary>
    protected void DisableWatcher()
    {
        _watcher.EnableRaisingEvents = false;
    }

    /// <summary>
    /// Enables the <see cref="FileSystemWatcher"/>.
    /// </summary>
    protected void EnableWatcher()
    {
        _watcher.EnableRaisingEvents = Settings.Watcher;
    }

    /// <summary>
    /// Gets called on a watcher event.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="e"></param>
    protected void OnWatcherEvent(object source, FileSystemEventArgs e)
    {
        // Workaround to update the value and keep the immediate eviction.
        var contains = _cache.TryGetValue(e.Name, out Lazy<WatcherChangeTypes>? lazyType);
        if (contains)
        {
            _cache.Remove(e.Name);
            _cache.GetOrAdd(e.Name, () => (lazyType!.Value | e.ChangeType), _options);
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

        var changeType = (WatcherChangeTypes)(value) switch
        {
            WatcherChangeTypes.Renamed => WatcherChangeTypes.Created, // Save Streaming
            WatcherChangeTypes.Deleted | WatcherChangeTypes.Renamed => WatcherChangeTypes.Changed, // Save Streaming
            WatcherChangeTypes.Created | WatcherChangeTypes.Changed => WatcherChangeTypes.Created, // Previous Format
            WatcherChangeTypes.Deleted | WatcherChangeTypes.Created => WatcherChangeTypes.Changed, // Microsoft
            _ => (WatcherChangeTypes)(value),
        };
        foreach (var container in GetCachedContainers((string)(key)))
        {
            container.SetWatcherChange(changeType);
            if (container.IsSynced)
            {
                OnWatcherDecision(container, true);
            }
        }
    }

    /// <summary>
    /// Resolves automatic decions or those made by the user.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="execute"></param>
    public void OnWatcherDecision(Container container, bool execute)
    {
        if (execute)
        {
            BuildContainer(container);
            UpdatePlatformUserIdentification();

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

    #region LZ4

    /// <summary>
    /// Compresses data from one buffer into another.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="target"></param>
    /// <returns>Number of bytes written, or negative value if output buffer is too small.</returns>
    protected static int LZ4_Encode(byte[] source, out byte[] target)
    {
        target = new byte[LZ4Codec.MaximumOutputSize(source.Length)];
        var bytesWritten = LZ4Codec.Encode(source, 0, source.Length, target, 0, target.Length);

        target = target.Take(bytesWritten).ToArray();
        return bytesWritten;
    }

    /// <summary>
    /// Decompresses data from one buffer into another.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="target"></param>
    /// <param name="targetLength"></param>
    /// <returns>Number of bytes written, or negative value if output buffer is too small.</returns>
    protected static int LZ4_Decode(byte[] source, out byte[] target, int targetLength)
    {
        target = Array.Empty<byte>();
        var bytesWritten = -1;

        if (targetLength > 0)
        {
            target = new byte[targetLength];
            bytesWritten = LZ4Codec.Decode(source, 0, source.Length, target, 0, target.Length);
        }

        // Fallback. https://github.com/MiloszKrajewski/K4os.Compression.LZ4#decompression
        if (bytesWritten < 0)
        {
            target = new byte[source.Length * 255];
            bytesWritten = LZ4Codec.Decode(source, 0, source.Length, target, 0, target.Length);
        }

        return bytesWritten;
    }

    #endregion

    #region Move

    /// <inheritdoc cref="Move(Container, Container, bool)"/>
    public void Move(Container source, Container destination)
    {
        Move(source, destination, true);
    }

    /// <summary>
    /// Uses a pair of <see cref="Container"/> to move it from one location to another.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    /// <param name="write"></param>
    protected void Move(Container source, Container destination, bool write)
    {
        Move(new[] { new ContainerOperationData { Destination = destination, Source = source } }, write);
    }

    /// <inheritdoc cref="Move(IEnumerable{ContainerOperationData}, bool)"/>
    public void Move(IEnumerable<ContainerOperationData> containerOperationData)
    {
        Move(containerOperationData, true);
    }

    /// <summary>
    /// Uses an enumerable of <see cref="Container"/> pairs to move them from one location to another.
    /// </summary>
    /// <param name="containerOperationData"></param>
    /// <param name="write"></param>
    protected virtual void Move(IEnumerable<ContainerOperationData> containerOperationData, bool write)
    {
        Copy(containerOperationData, write);
        Delete(containerOperationData.Select(c => c.Source), write);
    }

    #endregion

    #region Path

    /// <summary>
    /// Gets an enumerable of <see cref="DirectoryInfo"/> that contain save files for the specified <see cref="Platform"/>.
    /// Only PC platforms have a default path and can be located directly on the machine.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    internal static IEnumerable<DirectoryInfo> GetDirectoriesInDefaultPath<T>() where T : Platform
    {
        var typeT = typeof(T);

        if (typeT == typeof(PlatformGog))
            return GetAccountsInDefaultPath(PlatformGog.DirectoryData);

        if (typeT == typeof(PlatformMicrosoft))
            return GetAccountsInDefaultPath(PlatformMicrosoft.DirectoryData);

        if (typeT == typeof(PlatformSteam))
            return GetAccountsInDefaultPath(PlatformSteam.DirectoryData);

        return Array.Empty<DirectoryInfo>();
    }

    /// <summary>
    /// Gets an enumerable of <see cref="DirectoryInfo"/> that matches the specified <see cref="Data.PlatformDirectoryData"/>.
    /// </summary>
    /// <param name="directoryData"></param>
    /// <returns></returns>
    internal static IEnumerable<DirectoryInfo> GetAccountsInDefaultPath(PlatformDirectoryData directoryData)
    {
        var directoryPath = new DirectoryInfo(directoryData.DirectoryPath);

        // Exit if path does not exist.
        if (!directoryPath.Exists)
            return Array.Empty<DirectoryInfo>();

        return directoryPath.GetDirectories(directoryData.DirectoryPathPattern).Where(d => HasAnchorFile(d, directoryData.AnchorFileRegex));
    }

    /// <summary>
    /// Checks whether a specified directory contains one of the specified patterns.
    /// </summary>
    /// <param name="directory"></param>
    /// <param name="patterns"></param>
    /// <returns></returns>
    protected static bool HasAnchorFile(DirectoryInfo directory, Regex[] patterns)
    {
        foreach (var pattern in patterns)
        {
            if (directory.GetFiles().Any(f => pattern.IsMatch(f.Name)))
                return true;
        }
        return false;
    }

    #endregion

    #region Read

    #region Build

    /// <summary>
    /// Loads the platform related data.
    /// </summary>
    protected void BuildPlatform()
    {
        if (Settings.LoadingStrategy == LoadingStrategyEnum.Empty)
            return;

        var collection = BuildContainerCollection();

        ContainerCollection.Clear();
        ContainerCollection.AddRange(collection.ToArray());
        ContainerCollection.Sort();

        UpdatePlatformUserIdentification();
        EnableWatcher();
    }

    /// <summary>
    /// Builds a <see cref="Container"/> for each possible file.
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerable<Container> BuildContainerCollection()
    {
        var bag = new ConcurrentBag<Container>();
        var tasks = new List<Task>
        {
            Task.Run(() => AccountContainer = BuildContainer(0)),
        };

        _init = true;

        for (var slotIndex = 0; slotIndex < COUNT_SLOTS; slotIndex++)
        {
            foreach (var containerIndex in new[] { (COUNT_SAVES_PER_SLOT * slotIndex), (COUNT_SAVES_PER_SLOT * slotIndex + 1) })
            {
                tasks.Add(Task.Run(() =>
                {
                    var container = BuildContainer(containerIndex + Global.OFFSET_INDEX);
                    LoadBackupCollection(container);
                    bag.Add(container);
                }));
            }
        }

        Task.WaitAll(tasks.ToArray());

        _init = false;
        return bag;
    }

    /// <inheritdoc cref="BuildContainer(int, object?)"/>
    protected Container BuildContainer(int metaIndex)
    {
        return BuildContainer(metaIndex, null);
    }

    /// <summary>
    /// Builds a <see cref="Container"/> by creating, loading from disk, and processing it.
    /// </summary>
    /// <param name="metaIndex"></param>
    /// <param name="extra">An optional object with additional data if necessary.</param>
    /// <returns></returns>
    protected Container BuildContainer(int metaIndex, object? extra)
    {
        var container = CreateContainer(metaIndex, extra);
        return BuildContainer(container);
    }

    /// <summary>
    /// Builds a <see cref="Container"/> by loading from disk and processing it.
    /// </summary>
    /// <param name="container"></param>
    /// <returns></returns>
    protected Container BuildContainer(Container container)
    {
        var binary = LoadContainer(container);

        if (container.IsCompatible)
        {
            if (_init && container.IsSave && Settings.LoadingStrategy < LoadingStrategyEnum.Full)
            {
                ProcessContainer(container, binary.GetString());
            }
            else if (DeserializeContainer(container, binary) is JObject jsonObject)
            {
                ProcessContainer(container, jsonObject);
            }
        }

        return container;
    }

    /// <summary>
    /// Rebuild the container by updating its properties.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="jsonObject"></param>
    public void Rebuild(Container container, JObject jsonObject)
    {
        ProcessContainer(container, jsonObject);
        UpdatePlatformUserIdentification();
    }

    #endregion

    #region Create

    /// <summary>
    /// Creates a <see cref="Container"/> with general and platform specific data.
    /// </summary>
    /// <param name="metaIndex"></param>
    /// <param name="extra">An optional object with additional data if necessary.</param>
    /// <returns></returns>
    protected abstract Container CreateContainer(int metaIndex, object? extra);

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
            if (!data.IsNullOrEmpty())
                return data;
        }

        container.IncompatibilityTag = "F001_Empty";
        return Array.Empty<byte>();
    }

    /// <summary>
    /// Loads the meta file into a processable format including reading, decrypting, and decompressing.
    /// </summary>
    /// <param name="container"></param>
    /// <returns></returns>
    protected uint[] LoadMeta(Container container)
    {
        // 1. Read
        return LoadMeta(container, ReadMeta(container));
    }

    protected uint[] LoadMeta(Container container, byte[] read)
    {
        // 2. Decrypt
        // 3. Decompress
        return DecompressMeta(container, DecryptMeta(container, read));
    }

    /// <summary>
    /// Reads the meta file from the disk.
    /// </summary>
    /// <param name="container"></param>
    /// <returns></returns>
    protected virtual byte[] ReadMeta(Container container)
    {
        return ReadToByte(container.MetaFile);
    }

    /// <summary>
    /// Decrypts the read meta file.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="meta"></param>
    /// <returns></returns>
    protected virtual uint[] DecryptMeta(Container container, byte[] meta)
    {
        return meta.GetUInt32();
    }

    /// <summary>
    /// Decompresses the meta file content.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="meta"></param>
    /// <returns></returns>
    protected virtual uint[] DecompressMeta(Container container, uint[] meta)
    {
        return meta;
    }

    /// <summary>
    /// Loads the data file into a processable format including reading, decrypting, and decompressing.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="meta"></param>
    /// <returns></returns>
    protected virtual byte[] LoadData(Container container, uint[] meta)
    {
        // 1. Read
        return LoadData(container, meta, ReadData(container));
    }

    protected virtual byte[] LoadData(Container container, uint[] meta, byte[] read)
    {
        // 2. Decrypt
        // 3. Decompress
        return DecompressData(container, meta, DecryptData(container, meta, read));
    }

    /// <summary>
    /// Reads the data file from the disk.
    /// </summary>
    /// <param name="container"></param>
    /// <returns></returns>
    protected virtual byte[] ReadData(Container container)
    {
        return ReadToByte(container.DataFile);
    }

    /// <summary>
    /// Decrypts the data meta file.
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
    /// Decompresses the data file content.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="meta"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    protected virtual byte[] DecompressData(Container container, uint[] meta, byte[] data)
    {
        return data;
    }

    /// <summary>
    /// Loads data of a <see cref="Container"/> in consideration of the loading strategy.
    /// </summary>
    /// <param name="container"></param>
    public void Load(Container container)
    {
        if (Settings.LoadingStrategy < LoadingStrategyEnum.Current)
        {
            Settings = Settings with { LoadingStrategy = LoadingStrategyEnum.Current };
        }

        if (Settings.LoadingStrategy == LoadingStrategyEnum.Current && container.IsSave)
        {
            var loadedContainers = ContainerCollection.Where(c => (c.IsLoaded && c.IsSynced || c.IsBackup) && !c.Equals(container));
            foreach (var loadedContainer in loadedContainers)
            {
                Unload(loadedContainer);
            }
        }

        Reload(container);
    }

    /// <summary>
    /// Reads any file into a byte buffer.
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    internal static byte[] ReadToByte(string? file)
    {
        if (string.IsNullOrWhiteSpace(file))
            return Array.Empty<byte>();

        return ReadToByte(new FileInfo(file));
    }

    /// <summary>
    /// Reads any file into a byte buffer.
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    internal static byte[] ReadToByte(FileInfo? file)
    {
        if (file?.Exists != true)
            return Array.Empty<byte>();

        try
        {
            using var reader = new BinaryReader(File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            return reader.ReadBytes((int)(reader.BaseStream.Length));
        }
        catch (Exception x) when (x is PathTooLongException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return Array.Empty<byte>();
        }
    }

    /// <summary>
    /// Reloads the specified <see cref="Container"/> from disk.
    /// </summary>
    /// <param name="container"></param>
    public void Reload(Container container)
    {
        BuildContainer(container);
        UpdatePlatformUserIdentification();
    }

    /// <summary>
    /// Unloads data of a <see cref="Container"/> by removing the JSON object.
    /// </summary>
    /// <param name="container"></param>
    protected static void Unload(Container container)
    {
        container.SetJsonObject(null);
    }

    #endregion

    #region Process

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
        catch (Exception x) when (x is JsonReaderException or JsonSerializationException)
        {
            container.IncompatibilityException = x;
            container.IncompatibilityTag = "F002_Deserialization";
            return null;
        }
        if (jsonObject is null)
        {
            container.IncompatibilityTag = "F002_Deserialization";
            return null;
        }

        if (Settings.Mapping)
        {
            container.UnknownKeys = Mapping.Deobfuscate(jsonObject);
        }

        return jsonObject;
    }

    /// <summary>
    /// Processes the read JSON object and fills the properties if the container.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="jsonObject"></param>
    internal void ProcessContainer(Container container, JObject jsonObject)
    {
        // If we get here, the container is in sync (again).
        container.IsSynced = true;

        // No need to do these things for account data.
        if (container.IsSave)
        {
            container.TotalPlayTime = Global.GetTotalPlayTime(jsonObject);
            container.UserIdentification = GetUserIdentification(jsonObject);

            container.Version = Global.GetVersion(jsonObject);
            container.SeasonEnum = GetSeasonEnum(container); // works after Version is set
            container.GameModeEnum = Global.GetGameModeEnum(container, jsonObject);
            container.BaseVersion = Global.CalculateBaseVersion(container.Version, container.GameModeEnum!.Value, container.SeasonEnum); // works after Version and SeasonEnum and GameModeEnum are set
            container.VersionEnum = GetVersionEnum(container, jsonObject); // works after BaseVersion is set
            container.IsOld = container.VersionEnum < LOWEST_SUPPORTED_VERSION; // works after VersionEnum is set
        }

        container.SetJsonObject(jsonObject);
    }

    /// <inheritdoc cref="ProcessContainer(Container, JObject)"/>
    internal static void ProcessContainer(Container container, string json)
    {
        container.IsSynced = true;
        container.TotalPlayTime = Global.GetTotalPlayTime(json);

        container.Version = Global.GetVersion(json);
        container.SeasonEnum = GetSeasonEnum(container); // works after Version is set
        container.GameModeEnum = Global.GetGameModeEnum(container, json);
        container.BaseVersion = Global.CalculateBaseVersion(container.Version, container.GameModeEnum!.Value, container.SeasonEnum); // works after Version and SeasonEnum and GameModeEnum are set
        container.VersionEnum = GetVersionEnum(container, json); // works after BaseVersion is set
        container.IsOld = container.VersionEnum < LOWEST_SUPPORTED_VERSION; // works after VersionEnum is set
    }

    /// <summary>
    /// Gets the Expedition for the specified container.
    /// </summary>
    /// <param name="container"></param>
    /// <returns></returns>
    protected static SeasonEnum GetSeasonEnum(Container container)
    {
        var mode = Global.GetGameModeEnum(container);
        if (mode is null or < PresetGameModeEnum.Seasonal)
            return SeasonEnum.Pioneers;

        for (var i = Enum.GetValues(typeof(SeasonEnum)).Length; i > 1; i--)
        {
            if (Global.CalculateBaseVersion(container.Version, mode.Value, i) is >= Global.THRESHOLD and < Global.THRESHOLD_GAMEMODE)
                return i.DenumerateTo<SeasonEnum>();
        }

        return SeasonEnum.Pioneers;
    }

    /// <summary>
    /// Gets the game version for the specified container.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="jsonObject"></param>
    /// <returns></returns>
    protected VersionEnum GetVersionEnum(Container container, JObject jsonObject)
    {
        /** SaveVersion and new Keys to determine the GameVersion.

        GameVersion = CreativeVersion/BaseVersion (Obfuscated = Deobfuscated)
        ??? = ????/???? (??? = ?)

        Waypoint
        405 = 4654/4142
        404 = 4653/4141
        403 = 4652/4140
        400 = 4652/4140

        Endurance
        399 = 5163/4139
        398 = 5163/4139
        397 = 5163/4139
        396 = 5163/4139
        395 = 5163/4139
        394 = 5163/4139

        Leviathan
        393 = 5162/4138
        392 = 5162/4138
        391 = 5162/4138
        390 = 5162/4138 (Sd6 = NextLoadSpawnsWithFreshStart)

        Outlaws
        389 = 5162/4138
        388 = 5162/4138
        387 = 5162/4138
        385 = 5162/4138

        SentinelWithVehicleAI
        384 = 5161/4137 (Agx = VehicleAIControlEnabled)

        SentinelWithWeaponResource
        382 = 5161/4137
        381 = 5161/4137

        Sentinel
        380 = 5160/4136

        Emergence
        375 = 5159/4135
        374 = 5159/4135
        373 = 5159/4135
        371 = 5159/4135
        370 = 5159/4135 (qs? = SandwormOverrides)

        Frontiers
        368 = 5159/4135
        367 = 5159/4135
        366 = 5159/4135
        365 = 5159/4135
        364 = 5159/4135
        363 = 5159/4135
        362 = 5159/4135
        361 = 5159/4135
        360 = 5159/4135

        PrismsWithBytebeatAuthor
        353 = 5158/4134
        352 = 5158/4134
        351 = 5157/4133 (m7b = AuthorOnlineID)

        Prisms
        350 = 5157/4133 (8iI = ByteBeatLibrary)

        Beachhead
        342 = 5157/4133
        341 = 5157/4133
        340 = 5157/4133 (Whh = MainMissionTitle)

        Expeditions
        338 = 5157/4133
        337 = 5157/4133
        335 = 5154/4130
        334 = 5153/4129
        333 = 5153/4129
        332 = 5153/4129
        330 = 5153/4129

        Companions
        322 = 5151/4127
        321 = 5151/4127
        320 = 5151/4127 (Mcl = Pets)

        NextGeneration
        315 = 5151/4127
        313 = 5151/4127
        310 = 5151/4127

        Origins
        305 = 5150/4126
        303 = 5150/4126
        302 = 5150/4126
        301 = 5150/4126
        300 = 5150/4126 (ux@ = PreviousUniverseAddress)

        Desolation
        262 = 5150/4126
        261 = 5150/4126
        260 = 5150/4126 (Ovv = AbandonedFreighterPositionInSystem)

        Crossplay
        255 = 5150/4126
        254 = 5150/4126
        253 = 5150/4126
        252 = 5150/4126
        251 = 5150/4126

        ExoMech
        241 = 5149/4125
        240 = 5149/4125

        LivingShip
        233 = 5148/4124
        232 = 5148/4124
        231 = 5148/4124
        230 = 5148/4124 (Xf4 = CurrentPos)

        SynthesisWithJetpack
        227 = 5148/4124
        226 = 5148/4124

        Synthesis
        224 = 5147/4123
        223 = 5146/4122
        222 = 5146/4122
        220 = 5146/4122

        BeyondWithVehicleCam
        216 = 5143/4119
        215 = 5143/4119
        214 = 5143/4119
        213 = 5143/4119
        212 = 5143/4119
        211 = 5143/4119 (wb: = UsesThirdPersonVehicleCam)

        Beyond
        209 = 5141/4117
        */

        if (container.BaseVersion >= 4142) // 4.05
        {
            return VersionEnum.WaypointWithSuperchargedSlots;
        }

        if (container.BaseVersion >= 4141) // 4.04
            return VersionEnum.WaypointWithAgileStat;

        if (container.BaseVersion >= 4140) // 4.00
            return VersionEnum.Waypoint;

        if (container.BaseVersion >= 4139) // 3.94
            return VersionEnum.Endurance;

        if (container.BaseVersion >= 4138) // 3.85, 3.90
        {
            var nextLoadSpawnsWithFreshStart = jsonObject.SelectToken(Settings.Mapping ? "PlayerStateData.NextLoadSpawnsWithFreshStart" : "6f=.Sd6");
            if (nextLoadSpawnsWithFreshStart is not null)
                return VersionEnum.Leviathan;

            return VersionEnum.Outlaws;
        }

        if (container.BaseVersion >= 4137) // 3.81, 3.84
        {
            var vehicleAIControlEnabled = jsonObject.SelectToken(Settings.Mapping ? "PlayerStateData.VehicleAIControlEnabled" : "6f=.Agx");
            if (vehicleAIControlEnabled is not null)
                return VersionEnum.SentinelWithVehicleAI;

            return VersionEnum.SentinelWithWeaponResource;
        }

        if (container.BaseVersion >= 4136) // 3.80
            return VersionEnum.Sentinel;

        if (container.BaseVersion >= 4135) // 3.60, 3.70
        {
            var sandwormOverrides = jsonObject.SelectTokens(Settings.Mapping ? "PlayerStateData.SeasonData.SandwormOverrides" : "6f=.Rol.qs?");
            if (sandwormOverrides.Any())
                return VersionEnum.Emergence;

            return VersionEnum.Frontiers;
        }

        if (container.BaseVersion >= 4129) // 3.30, 3.40, 3.50, 3.51
        {
            var authorOnlineID = jsonObject.SelectTokens(Settings.Mapping ? "PlayerStateData.ByteBeatLibrary.MySongs..AuthorOnlineID" : "6f=.8iI.ON4..m7b");
            if (authorOnlineID.Any())
                return VersionEnum.PrismsWithBytebeatAuthor;

            var byteBeatLibrary = jsonObject.SelectToken(Settings.Mapping ? "PlayerStateData.ByteBeatLibrary" : "6f=.8iI");
            if (byteBeatLibrary is not null)
                return VersionEnum.Prisms;

            var mainMissionTitle = jsonObject.SelectToken(Settings.Mapping ? "PlayerStateData.SeasonData.MainMissionTitle" : "6f=.Rol.Whh");
            if (mainMissionTitle is not null)
                return VersionEnum.Beachhead;

            return VersionEnum.Expeditions;
        }

        if (container.BaseVersion >= 4127) // 3.10, 3.20
        {
            var pets = jsonObject.SelectToken(Settings.Mapping ? "PlayerStateData.Pets" : "6f=.;4P");
            if (pets is not null)
                return VersionEnum.Companions;

            return VersionEnum.NextGeneration;
        }

        if (container.BaseVersion >= 4126) // 2.50, 2.60, 3.00
        {
            var previousUniverseAddress = jsonObject.SelectToken(Settings.Mapping ? "PlayerStateData.PreviousUniverseAddress" : "6f=.ux@");
            if (previousUniverseAddress is not null)
                return VersionEnum.Origins;

            var abandonedFreighterPositionInSystem = jsonObject.SelectToken(Settings.Mapping ? "SpawnStateData.AbandonedFreighterPositionInSystem" : "6f=.Ovv");
            if (abandonedFreighterPositionInSystem is not null)
                return VersionEnum.Desolation;

            return VersionEnum.Crossplay;
        }

        if (container.BaseVersion >= 4125) // 2.40
            return VersionEnum.ExoMech;

        if (container.BaseVersion >= 4124) // 2.26, 2.30
        {
            var currentPos = jsonObject.SelectTokens(Settings.Mapping ? "PlayerStateData..CurrentPos" : "6f=..Xf4");
            if (currentPos.Any())
                return VersionEnum.LivingShip;

            return VersionEnum.SynthesisWithJetpack;
        }

        if (container.BaseVersion >= 4122) // 2.20
            return VersionEnum.Synthesis;

        if (container.BaseVersion >= 4119) // 2.11
            return VersionEnum.BeyondWithVehicleCam;

        return VersionEnum.Unknown;
    }

    /// <inheritdoc cref="GetVersionEnum(Container, JObject)"/>
    protected static VersionEnum GetVersionEnum(Container container, string json)
    {
        if (container.BaseVersion >= 4142) // 4.05
        {
            return VersionEnum.WaypointWithSuperchargedSlots;
        }

        if (container.BaseVersion >= 4141) // 4.04
            return VersionEnum.WaypointWithAgileStat;

        if (container.BaseVersion >= 4140) // 4.00
            return VersionEnum.Waypoint;

        if (container.BaseVersion >= 4139) // 3.94
            return VersionEnum.Endurance;

        if (container.BaseVersion >= 4138) // 3.85, 3.90
        {
            var nextLoadSpawnsWithFreshStart = json.Contains("\"Sd6\":"); // NextLoadSpawnsWithFreshStart
            if (nextLoadSpawnsWithFreshStart)
                return VersionEnum.Leviathan;

            return VersionEnum.Outlaws;
        }

        if (container.BaseVersion >= 4137) // 3.81, 3.84
        {
            var vehicleAIControlEnabled = json.Contains("\"Agx\":"); // VehicleAIControlEnabled
            if (vehicleAIControlEnabled)
                return VersionEnum.SentinelWithVehicleAI;

            return VersionEnum.SentinelWithWeaponResource;
        }

        if (container.BaseVersion >= 4136) // 3.80
            return VersionEnum.Sentinel;

        if (container.BaseVersion >= 4135) // 3.60, 3.70
        {
            var sandwormOverrides = json.Contains("\"qs?\":"); // SandwormOverrides
            if (sandwormOverrides)
                return VersionEnum.Emergence;

            return VersionEnum.Frontiers;
        }

        if (container.BaseVersion >= 4129) // 3.30, 3.40, 3.50, 3.51
        {
            var authorOnlineID = json.Contains("\"m7b\":"); // AuthorOnlineID
            if (authorOnlineID)
                return VersionEnum.PrismsWithBytebeatAuthor;

            var byteBeatLibrary = json.Contains("\"8iI\":"); // ByteBeatLibrary
            if (byteBeatLibrary)
                return VersionEnum.Prisms;

            var mainMissionTitle = json.Contains("\"Whh\":"); // MainMissionTitle
            if (mainMissionTitle)
                return VersionEnum.Beachhead;

            return VersionEnum.Expeditions;
        }

        if (container.BaseVersion >= 4127) // 3.10, 3.20
        {
            var pets = json.Contains("\"Mcl\":"); // Pets
            if (pets)
                return VersionEnum.Companions;

            return VersionEnum.NextGeneration;
        }

        if (container.BaseVersion >= 4126) // 2.50, 2.60, 3.00
        {
            var previousUniverseAddress = json.Contains("\"ux@\":"); // PreviousUniverseAddress
            if (previousUniverseAddress)
                return VersionEnum.Origins;

            var abandonedFreighterPositionInSystem = json.Contains("\"Ovv\":"); // AbandonedFreighterPositionInSystem
            if (abandonedFreighterPositionInSystem)
                return VersionEnum.Desolation;

            return VersionEnum.Crossplay;
        }

        if (container.BaseVersion >= 4125) // 2.40
            return VersionEnum.ExoMech;

        if (container.BaseVersion >= 4124) // 2.26, 2.30
        {
            var currentPos = json.Contains("\"Xf4\":"); // CurrentPos
            if (currentPos)
                return VersionEnum.LivingShip;

            return VersionEnum.SynthesisWithJetpack;
        }

        if (container.BaseVersion >= 4122) // 2.20
            return VersionEnum.Synthesis;

        if (container.BaseVersion >= 4119) // 2.11
            return VersionEnum.BeyondWithVehicleCam;

        return VersionEnum.Unknown;
    }

    #endregion

    #endregion

    #region Swap

    /// <inheritdoc cref="Swap(Container, Container, bool)"/>
    public void Swap(Container source, Container destination)
    {
        Swap(source, destination, true);
    }

    /// <summary>
    /// Uses a pair of <see cref="Container"/> to swap theirs locations.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    /// <param name="write"></param>
    /// <exception cref="InvalidOperationException"/>
    protected void Swap(Container source, Container destination, bool write)
    {
        Swap(new[] { new ContainerOperationData { Destination = destination, Source = source } }, write);
    }

    /// <inheritdoc cref="Swap(IEnumerable{ContainerOperationData}, bool)"/>
    public void Swap(IEnumerable<ContainerOperationData> containerOperationData)
    {
        Swap(containerOperationData, true);
    }

    /// <summary>
    /// Uses an enumerable of <see cref="Container"/> to swap their respective locations.
    /// </summary>
    /// <param name="containerOperationData"></param>
    /// <param name="write"></param>
    /// <exception cref="InvalidOperationException"/>
    protected virtual void Swap(IEnumerable<ContainerOperationData> containerOperationData, bool write)
    {
        foreach (var (Source, Destination) in containerOperationData.Select(d => (d.Source, d.Destination)))
        {
            if (Source.Exists)
            {
                // Source and Destination exists. Swap.
                if (Destination.Exists)
                {
                    if (!Source.IsLoaded)
                    {
                        BuildContainer(Source);
                    }
                    if (!Destination.IsLoaded)
                    {
                        BuildContainer(Destination);
                    }
                    if (!Source.IsCompatible || !Destination.IsCompatible)
                    {
                        throw new InvalidOperationException("Cannot swap as at least one container is invalid.");
                    }

                    // Cache.
                    var jsonObject = Destination.GetJsonObject();
                    var writeTime = Destination.LastWriteTime;

                    // Write Source to Destination.
                    Destination.SetJsonObject(Source.GetJsonObject());
                    Write(Destination, Source.LastWriteTime);
                    BuildContainer(Destination);

                    // Write Destination to Source.
                    Source.SetJsonObject(jsonObject);
                    Write(Source, writeTime);
                    BuildContainer(Source);
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

        UpdatePlatformUserIdentification();
    }

    #endregion

    #region Transfer

    /// <summary>
    /// Prepares the specified slot for transfer.
    /// </summary>
    /// <param name="sourceSlot"></param>
    /// <returns></returns>
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
            {
                BuildContainer(container);
            }

            foreach (var playerBase in container.GetJsonObject()!.SelectTokens(Settings.Mapping ? "PlayerStateData.PersistentPlayerBases[*]" : "6f=.F?0[*]"))
            {
                var baseType = playerBase.SelectToken(Settings.Mapping ? "BaseType.PersistentBaseTypes" : "peI.DPp")?.Value<string>();
                var freighterBase = PersistentBaseTypesEnum.FreighterBase.ToString();

                var isHomePlanetBase = baseType == PersistentBaseTypesEnum.HomePlanetBase.ToString();
                var isFreighterBase = baseType == freighterBase;

                var name = playerBase.SelectToken(Settings.Mapping ? "Name" : "NKm")?.Value<string>();
                if (string.IsNullOrEmpty(name))
                {
                    if (isFreighterBase)
                    {
                        name = freighterBase;
                    }
                    else if (!isHomePlanetBase)
                    {
                        name = "Unknown Base Type";
                    }
                }

                var isOwnBase = isHomePlanetBase || isFreighterBase;
                var identifier = GetBaseIdentifier(playerBase);

                data.TransferBaseReadonly[identifier] = isOwnBase;
                data.TransferBaseUserDecision[identifier] = new() { DoTransfer = isOwnBase, Name = name! };
            }
        }

        UpdatePlatformUserIdentification();
        data.UserIdentification = PlatformUserIdentification;
        return data;
    }

    /// <summary>
    /// Ensures that the destination is well prepared as well as updating the user identification.
    /// </summary>
    /// <param name="destinationSlot"></param>
    public void PrepareTransferDestination(int destinationSlot)
    {
        foreach (var container in GetSlotContainers(destinationSlot))
        {
            if (!container.Exists)
                continue;

            if (!container.IsLoaded)
            {
                BuildContainer(container);
            }
        }

        UpdatePlatformUserIdentification();
    }

    /// <inheritdoc cref="Transfer(ContainerTransferData, int, bool)"/>
    public void Transfer(ContainerTransferData sourceTransferData, int destinationSlot)
    {
        Transfer(sourceTransferData, destinationSlot, true);
    }

    /// <summary>
    /// Transfers a specified slot to another account or platform according to the prepared data.
    /// Works like copy but with additional ownership transfer.
    /// </summary>
    /// <param name="sourceTransferData"></param>
    /// <param name="destinationSlot"></param>
    /// <param name="write"></param>
    /// <exception cref="InvalidOperationException"></exception>
    protected virtual void Transfer(ContainerTransferData sourceTransferData, int destinationSlot, bool write)
    {
        if (!sourceTransferData.UserIdentification.IsComplete() || !PlatformUserIdentification.IsComplete())
            throw new InvalidOperationException("Cannot transfer as at least one UserIdentification is not complete.");

        var destinationContainers = GetSlotContainers(destinationSlot);

#if NETSTANDARD2_0_OR_GREATER
        foreach (var (Source, Destination) in sourceTransferData.Containers.Zip(destinationContainers, (Source, Destination) => (Source, Destination)))
#else
        foreach (var (Source, Destination) in sourceTransferData.Containers.Zip(destinationContainers))
#endif
        {
            if (!Source.Exists)
            {
                Delete(Destination);
            }
            else if (Destination.Exists || !Destination.Exists && CanCreate)
            {
                if (!Source.IsLoaded)
                {
                    BuildContainer(Source);
                }
                if (!Source.IsCompatible)
                {
                    throw new InvalidOperationException(Source.IncompatibilityTag);
                }

                // Faking relevant properties to force it to Write().
                Destination.Exists = true;
                Destination.IsSynced = false;

                // Properties requied to properly build the container below.
                Destination.BaseVersion = Source.BaseVersion;
                Destination.VersionEnum = Source.VersionEnum;

                Destination.SetJsonObject(Source.GetJsonObject());
                TransferOwnership(Destination, sourceTransferData);

                if (write)
                {
                    Write(Destination, writeTime: Source.LastWriteTime);
                    BuildContainer(Destination);
                }
            }
            //else
            //    continue;
        }
    }

    /// <summary>
    /// Transfers ownerships to new container according to the prepared data.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="sourceTransferData"></param>
    protected void TransferOwnership(Container container, ContainerTransferData sourceTransferData)
    {
        var jsonObject = container.GetJsonObject()!;

        // Change token for Platform.
        jsonObject[Settings.Mapping ? "Platform" : "8>q"] = PlatformArchitecture;

        if (sourceTransferData.TransferDiscovery)
        {
            TransferGeneralOwnership(jsonObject, Settings.Mapping ? $"DiscoveryManagerData.DiscoveryData-v1.Store.Record..[?(@.UID == '{sourceTransferData.UserIdentification!.UID}')]" : $"fDu.ETO.OsQ.?fB..[?(@.K7E == '{sourceTransferData.UserIdentification!.UID}')]");
        }

        if (sourceTransferData.TransferBase) // 1.1
        {
            TransferBaseOwnership(jsonObject, sourceTransferData);
        }

        if (container.IsPrismsWithBytebeatAuthor && sourceTransferData.TransferBytebeat) // 3.51
        {
            TransferBytebeatOwnership(jsonObject, sourceTransferData.UserIdentification!.UID);
        }

        if (container.IsFrontiers && sourceTransferData.TransferSettlement) // 3.6
        {
            TransferGeneralOwnership(jsonObject, Settings.Mapping ? $"PlayerStateData.SettlementStatesV2..[?(@.UID == '{sourceTransferData.UserIdentification!.UID}')]" : $"6f=.GQA..[?(@.K7E == '{sourceTransferData.UserIdentification!.UID}')]");
        }

        container.SetJsonObject(jsonObject);
    }

    /// <summary>
    /// Transfers ownerships of all selected bases.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <param name="sourceTransferData"></param>
    protected void TransferBaseOwnership(JObject jsonObject, ContainerTransferData sourceTransferData)
    {
        foreach (var playerBase in jsonObject.SelectTokens(Settings.Mapping ? "PlayerStateData.PersistentPlayerBases[*]" : "6f=.F?0[*]"))
        {
            var identifier = GetBaseIdentifier(playerBase);

            if (sourceTransferData.TransferBaseReadonly[identifier] && sourceTransferData.TransferBaseUserDecision[identifier].DoTransfer)
            {
                TransferGeneralOwnership(playerBase[Settings.Mapping ? "Owner" : "3?K"]!);
            }
        }
    }

    /// <summary>
    /// Transfers ownerships of the ByteBeat library.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <param name="uid"></param>
    protected void TransferBytebeatOwnership(JObject jsonObject, string? uid)
    {
        if (PlatformUserIdentification is null || uid is null)
            return;

        foreach (var token in jsonObject.SelectTokens(Settings.Mapping ? $"PlayerStateData.MySongs[?(@.AuthorOnlineID == '{uid}')]" : $"6f=.ON4[?(@.m7b == '{uid}')]"))
        {
            token[Settings.Mapping ? "AuthorOnlineID" : "m7b"] = PlatformUserIdentification.UID;
            token[Settings.Mapping ? "AuthorUsername" : "4ha"] = PlatformUserIdentification.USN;
            token[Settings.Mapping ? "AuthorPlatform" : "d2f"] = PlatformToken;
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
        {
            TransferGeneralOwnership(token);
        }
    }

    /// <summary>
    /// Transfers ownerships in the specified JSON token.
    /// </summary>
    /// <param name="jToken"></param>
    protected void TransferGeneralOwnership(JToken jToken)
    {
        if (PlatformUserIdentification is null)
            return;

        // Determine once.
        var UID = Settings.Mapping ? "UID" : "K7E";
        var LID = Settings.Mapping ? "LID" : "f5Q";
        var USN = Settings.Mapping ? "USN" : "V?:";
        var PTK = Settings.Mapping ? "PTK" : "D6b";

        // Only UID is guaranteed.
        jToken[UID] = PlatformUserIdentification.UID;

        // Replace LID, PTK, and USN if it is not empty.
        if (!string.IsNullOrEmpty(jToken[LID]?.Value<string>()))
        {
            jToken[LID] = PlatformUserIdentification.LID;
        }
        if (!string.IsNullOrEmpty(jToken[USN]?.Value<string>()))
        {
            jToken[USN] = PlatformUserIdentification.USN;
        }
        if (!string.IsNullOrEmpty(jToken[PTK]?.Value<string>()))
        {
            jToken[PTK] = PlatformToken;
        }
    }

    #endregion

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
        // Mapping here if necessary to use human-readable names in override methods.
        key = key switch
        {
            "LID" => Settings.Mapping ? "LID" : "f5Q",
            "UID" => Settings.Mapping ? "UID" : "K7E",
            "USN" => Settings.Mapping ? "USN" : "V?:",
            _ => string.Empty,
        };
        if (string.IsNullOrEmpty(key))
            return string.Empty;

        var byBase = GetUserIdentificationByBase(jsonObject, key);
        var byDiscovery = GetUserIdentificationByDiscovery(jsonObject, key);
        var bySettlement = GetUserIdentificationBySettlement(jsonObject, key);

        return byBase.Concat(byDiscovery).Concat(bySettlement).MostCommon() ?? string.Empty;
    }

    /// <summary>
    /// Gets the <see cref="UserIdentificationData"/> information for the specified property key from bases.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    /// <seealso href="https://stackoverflow.com/a/38256828"/>
    protected virtual IEnumerable<JToken> GetUserIdentificationByBase(JObject jsonObject, string key)
    {
        var path = Settings.Mapping ? $"PlayerStateData.PersistentPlayerBases[?({{0}})].Owner.{key}" : $"6f=.F?0[?({{0}})].3?K.{key}";
        var expressions = new[]
        {
            Settings.Mapping ? $"@.BaseType.PersistentBaseTypes == '{PersistentBaseTypesEnum.HomePlanetBase}' || @.BaseType.PersistentBaseTypes == '{PersistentBaseTypesEnum.FreighterBase}'" : $"@.peI.DPp == '{PersistentBaseTypesEnum.HomePlanetBase}' || @.peI.DPp == '{PersistentBaseTypesEnum.FreighterBase}'", // only with own base
            Settings.Mapping ? $"@.Owner.PTK == '' || @.Owner.PTK == '{PlatformToken}'" : $"@.3?K.D6b == '' || @.3?K.D6b == '{PlatformToken}'", // only with valid platform
            Settings.Mapping ? $"@.Owner.LID != ''" : $"@.3?K.f5Q != ''", // only if set
        };

        return GetUserIdentificationIntersection(jsonObject, path, expressions);
    }

    /// <summary>
    /// Gets the <see cref="UserIdentificationData"/> information for the specified property key from discoveries.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    protected virtual IEnumerable<JToken> GetUserIdentificationByDiscovery(JObject jsonObject, string key)
    {
        var path = Settings.Mapping ? $"DiscoveryManagerData.DiscoveryData-v1.Store.Record[?({{0}})].OWS.{key}" : $"fDu.ETO.OsQ.?fB[?({{0}})].ksu.{key}";
        var expressions = new[]
        {
            Settings.Mapping ? $"@.OWS.PTK == '' || @.OWS.PTK == '{PlatformToken}'" : $"@.ksu.D6b == '' || @.ksu.D6b == '{PlatformToken}'", // only with valid platform
            Settings.Mapping ? $"@.OWS.LID != ''" : $"@.ksu.f5Q != ''", // only if set
        };

        return GetUserIdentificationIntersection(jsonObject, path, expressions);
    }

    /// <summary>
    /// Gets the <see cref="UserIdentificationData"/> information for the specified property key from settlements.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    protected virtual IEnumerable<JToken> GetUserIdentificationBySettlement(JObject jsonObject, string key)
    {
        var path = Settings.Mapping ? $"PlayerStateData.SettlementStatesV2[?({{0}})].Owner.{key}" : $"6f=.GQA[?({{0}})].3?K.{key}";
        var expressions = new[]
        {
            Settings.Mapping ? $"@.Owner.PTK == '{PlatformToken}'" : $"@.3?K.D6b == '{PlatformToken}'", // only with valid platform
            Settings.Mapping ? $"@.Owner.LID != ''" : $"@.3?K.f5Q != ''", // only if set
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
    protected static IEnumerable<JToken> GetUserIdentificationIntersection(JObject jsonObject, string path, params string[] expressions)
    {
        if (expressions.Length == 0)
            return Array.Empty<JToken>();

        IEnumerable<JToken> result = null!;
        foreach (var expression in expressions)
        {
            var query = jsonObject.SelectTokens(string.Format(path, expression));
            result = result is null ? query : result.Intersect(query);
        }
        return result;
    }

    /// <summary>
    /// Updates the <see cref="UserIdentificationData"/> with data from all loaded containers.
    /// </summary>
    protected void UpdatePlatformUserIdentification()
    {
        PlatformUserIdentification.LID = GetPlatformUserIdentificationPropertyValue(nameof(UserIdentificationData.LID));
        PlatformUserIdentification.PTK = PlatformToken;
        PlatformUserIdentification.UID = GetPlatformUserIdentificationPropertyValue(nameof(UserIdentificationData.UID));
        PlatformUserIdentification.USN = GetPlatformUserIdentificationPropertyValue(nameof(UserIdentificationData.USN));
    }

    #endregion

    #region Write

    /// <summary>
    /// Writes the specified container to drive in the most basic manner.
    /// </summary>
    /// <param name="container"></param>
    internal void JustWrite(Container container)
    {
        if (!CanUpdate)
            return;

        var (data, decompressedSize) = CreateData(container);
        var meta = CreateMeta(container, data, decompressedSize);

        WriteMeta(container, meta);
        WriteData(container, data);
    }

    /// <inheritdoc cref="Write(Container, DateTimeOffset)"/>
    public void Write(Container container)
    {
        Write(container, default);
    }

    /// <summary>
    /// Writes the specified container to drive and sets the timestamp to the specified value.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="writeTime"></param>
    public virtual void Write(Container container, DateTimeOffset writeTime)
    {
        if (!CanUpdate || !container.IsLoaded)
            return;

        DisableWatcher();

        // if (!container.IsSynced) // TODO: temporarily disabled
        {
            container.Exists = true;
            container.IsSynced = true;

            JustWrite(container);
        }

        if (Settings.LastWriteTime)
        {
            container.LastWriteTime = writeTime.Equals(default) ? DateTimeOffset.Now.LocalDateTime : writeTime;
            WriteTime(container);
        }

        EnableWatcher();

        // Always refresh in case something above was executed.
        container.RefreshFileInfo();
        container.WriteCallback.Invoke();
    }

    /// <summary>
    /// Creates binary data file content ready to write to disk.
    /// </summary>
    /// <param name="container"></param>
    /// <returns></returns>
    protected virtual (byte[], int) CreateData(Container container)
    {
        var plain = container.GetJsonObject()!.GetBytes(Settings.Mapping);
        var encrypted = EncryptData(container, CompressData(container, plain));

        return (encrypted, plain.Length);
    }

    /// <summary>
    /// Compresses the data file content.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    protected virtual byte[] CompressData(Container container, byte[] data)
    {
        return data;
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
        container.DataFile!.Refresh();
    }

    /// <summary>
    /// Creates binary meta file content ready to write to disk.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="data"></param>
    /// <param name="decompressedSize"></param>
    /// <returns></returns>
    protected abstract byte[] CreateMeta(Container container, byte[] data, int decompressedSize);

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
        container.MetaFile!.Refresh();
    }

    /// <summary>
    /// Writes the creation and last write time of the container to the files.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="creation"></param>
    /// <param name="lastWrite"></param>
    protected static void WriteTime(Container container)
    {
        if (container.DataFile is not null)
        {
            File.SetCreationTime(container.DataFile.FullName, container.LastWriteTime.LocalDateTime);
            File.SetLastWriteTime(container.DataFile.FullName, container.LastWriteTime.LocalDateTime);
            container.DataFile.Refresh();
        }

        if (container.MetaFile is not null)
        {
            File.SetCreationTime(container.MetaFile.FullName, container.LastWriteTime.LocalDateTime);
            File.SetLastWriteTime(container.MetaFile.FullName, container.LastWriteTime.LocalDateTime);
            container.MetaFile.Refresh();
        }
    }

    #endregion
}
