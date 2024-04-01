using System.Collections;
using System.Collections.Concurrent;

using libNOM.io.Interfaces;
using libNOM.io.Settings;

namespace libNOM.io;


/// <summary>
/// Holds all accounts of all detected platforms.
/// </summary>
public class PlatformCollection : IEnumerable<IPlatform>
{
    #region Field

    private readonly ConcurrentDictionary<string, IPlatform> _collection = new();

    #endregion

    // //

    #region Property

    public PlatformCollectionSettings CollectionSettings { get; set; }

    public PlatformSettings PlatformSettings { get; set; }

    #endregion

    // Accessor

    #region Getter

    /// <summary>
    /// Gets the <see cref="Platform"/> associated with the specified path.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    /// <exception cref="KeyNotFoundException"/>
    public IPlatform Get(string path)
    {
        return _collection[path];
    }

    /// <summary>
    /// Attempts to get the <see cref="Platform"/> associated with the specified path.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public bool TryGet(string path, out IPlatform? platform)
    {
        return _collection.TryGetValue(path, out platform);
    }

    #endregion

    // Initialize

    #region Constructor

    public PlatformCollection() : this(platformSettings: null, collectionSettings: null) { }

    public PlatformCollection(string? path) : this(path, null, null) { }

    public PlatformCollection(DirectoryInfo? directory) : this(directory, null, null) { }

    public PlatformCollection(PlatformSettings? platformSettings) : this(platformSettings, collectionSettings: null) { }

    public PlatformCollection(PlatformCollectionSettings? collectionSettings) : this(platformSettings: null, collectionSettings) { }

    public PlatformCollection(string? path, PlatformSettings? platformSettings) : this(path, platformSettings, null) { }

    public PlatformCollection(DirectoryInfo? directory, PlatformSettings? platformSettings) : this(directory, platformSettings, null) { }

    public PlatformCollection(string? path, PlatformCollectionSettings? collectionSettings) : this(path, null, collectionSettings) { }

    public PlatformCollection(DirectoryInfo? directory, PlatformCollectionSettings? collectionSettings) : this(directory, null, collectionSettings) { }

    public PlatformCollection(PlatformSettings? platformSettings, PlatformCollectionSettings? collectionSettings)
    {
        CollectionSettings = collectionSettings ?? new();
        PlatformSettings = platformSettings ?? new();

        Reinitialize();
    }

    public PlatformCollection(string? path, PlatformSettings? platformSettings, PlatformCollectionSettings? collectionSettings) : this(platformSettings, collectionSettings) // Reinitialize() before AnalyzePath()
    {
        _ = AnalyzePath(path, platformSettings, CollectionSettings.PreferredPlatform);
    }

    public PlatformCollection(DirectoryInfo? directory, PlatformSettings? platformSettings, PlatformCollectionSettings? collectionSettings) : this(directory?.FullName, platformSettings, collectionSettings) { }

    #endregion

    #region Initialize

    /// <summary>
    /// Initializes the collection.
    /// Only on a PC, platforms have a default path and can be located directly on the machine.
    /// </summary>
    public void Reinitialize()
    {
        _collection.Clear();

        if (!CollectionSettings.AnalyzeLocal)
            return;

        var tasks = new List<Task>();
        tasks.AddRange(TryAddDirectory<PlatformSteam>()); // is available on all 3 operating systems
        if (Common.IsWindows())
        {
            tasks.AddRange(TryAddDirectory<PlatformGog>());
            tasks.AddRange(TryAddDirectory<PlatformMicrosoft>());
        }
        Task.WaitAll([.. tasks]);
    }

    #endregion

    // Interface

    #region IEnumerable

    public IEnumerator<IPlatform> GetEnumerator()
    {
        foreach (var pair in _collection)
            yield return pair.Value;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion

    // //

    #region Analyze

    /// <summary>
    /// Analyzes a path to get the <see cref="Platform"/> it contains.
    /// <see cref="PlatformSettings"/> and <see cref="CollectionSettings"/> are used to populate undefined parameters.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public IPlatform? AnalyzePath(string? path) => AnalyzePath(path, PlatformSettings, CollectionSettings.PreferredPlatform);

    /// <summary>
    /// Analyzes a path to get the <see cref="Platform"/> it contains.
    /// <see cref="PlatformSettings"/> are used to populate undefined parameters.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="platformPreferred"></param>
    /// <returns></returns>
    public IPlatform? AnalyzePath(string? path, PlatformEnum? platformPreferred) => AnalyzePath(path, null, platformPreferred);

    /// <summary>
    /// Analyzes a path to get the <see cref="Platform"/> it contains.
    /// <see cref="CollectionSettings"/> are used to populate undefined parameters.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="platformSettings"></param>
    /// <returns></returns>
    public IPlatform? AnalyzePath(string? path, PlatformSettings? platformSettings) => AnalyzePath(path, platformSettings, CollectionSettings.PreferredPlatform);

    /// <summary>
    /// Analyzes a path to get the <see cref="Platform"/> it contains.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="platformSettings">Settings for the platform.</param>
    /// <param name="platformPreferred">Platform that will be checked first.</param>
    /// <returns></returns>
    public IPlatform? AnalyzePath(string? path, PlatformSettings? platformSettings, PlatformEnum? platformPreferred)
    {
        if (!Analyze.ValidatePath(path, out _))
            return null;

        platformSettings ??= PlatformSettings;

        if (platformSettings.LoadingStrategy == LoadingStrategyEnum.Empty)
            platformSettings = platformSettings with { LoadingStrategy = LoadingStrategyEnum.Hollow };

        if (_collection.TryGetValue(path!, out var platform))
        {
            platform.SetSettings(platformSettings);
            return platform;
        }

        var result = Analyze.AnalyzePath(path, platformSettings, platformPreferred);
        if (result is not null)
            _collection.TryAdd(path!, result);

        return result;
    }

    #endregion

    // private //

    /// <summary>
    /// Adds all accounts available in the default path of the specified <see cref="Platform"/> to the collection.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    private List<Task> TryAddDirectory<T>() where T : IPlatform
    {
        var tasks = new List<Task>();

        foreach (var directory in GetAccountsInPlatform<T>())
            tasks.Add(Task.Run(() =>
            {
                var platform = (T)(Activator.CreateInstance(typeof(T), directory))!;
                if (platform.IsValid)
                {
                    _collection.TryAdd(directory.FullName, platform);
                }
            }));

        return tasks;
    }

    /// <summary>
    /// Gets an enumerable of <see cref="DirectoryInfo"/> of all accounts of the specified <see cref="Platform"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    private static IEnumerable<DirectoryInfo> GetAccountsInPlatform<T>() where T : IPlatform => typeof(T) switch
    {
        var typeofT when typeofT == typeof(PlatformGog) => GetAccountsInPath(PlatformGog.PATH, PlatformGog.ACCOUNT_PATTERN),
        var typeofT when typeofT == typeof(PlatformMicrosoft) => GetAccountsInPath(PlatformMicrosoft.PATH, PlatformMicrosoft.ACCOUNT_PATTERN),
        var typeofT when typeofT == typeof(PlatformSteam) => GetAccountsInPath(PlatformSteam.PATH, PlatformSteam.ACCOUNT_PATTERN),
        _ => [],
    };

    /// <summary>
    /// Gets an enumerable of <see cref="DirectoryInfo"/> of all accounts in the specified path.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private static IEnumerable<DirectoryInfo> GetAccountsInPath(string path, string pattern) => Analyze.ValidatePath(path, out var directory) ? directory!.EnumerateDirectories(pattern) : [];
}
