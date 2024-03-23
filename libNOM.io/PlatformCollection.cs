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

    #region Property

    public PlatformCollectionSettings CollectionSettings { get; set; }

    public PlatformSettings PlatformSettings { get; set; }

    #endregion

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

    #region Constructor

    public PlatformCollection() : this(platformSettings: null, collectionSettings: null) { }

    public PlatformCollection(string? path) : this(path, null, null) { }

    public PlatformCollection(PlatformSettings? platformSettings) : this(platformSettings, collectionSettings: null) { }

    public PlatformCollection(PlatformCollectionSettings? collectionSettings) : this(platformSettings: null, collectionSettings) { }

    public PlatformCollection(string? path, PlatformSettings? platformSettings) : this(path, platformSettings, null) { }

    public PlatformCollection(string? path, PlatformCollectionSettings? collectionSettings) : this(path, null, collectionSettings) { }

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

    #endregion

    #region IEnumerable

    public IEnumerator<IPlatform> GetEnumerator()
    {
        foreach (var pair in _collection)
            yield return pair.Value;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

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

    #region Analyze

    /// <summary>
    /// Analysis a single file and loads it.
    /// </summary>
    /// <param name="path"></param>
    /// <returns>A pre-loaded <see cref="Container"/> if no incompatibilities.</returns>
    public static Container? AnalyzeFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;

        FileInfo? meta = null;
        Platform? platform = null;

        ReadOnlySpan<byte> bytes = File.ReadAllBytes(path);
        var data = new FileInfo(path);
        var directory = data.Directory!.FullName;
        var fullPath = Path.GetFullPath(path);
        int metaIndex, possibleIndex = metaIndex = Constants.OFFSET_INDEX;

        // Convert header with different lengths.
        var headerInteger = bytes.Cast<uint>(0);
        var headerString0x08 = bytes[..0x08].GetString();
        var headerString0x20 = bytes[..0x20].GetString();
        var headerString0xA0 = bytes[..0xA0].GetString();

        // Select a platform below to convert the file with, based on the content.
        // All kinds of Playstation.
        if (headerString0x08 == PlatformPlaystation.SAVEWIZARD_HEADER || (headerInteger == Constants.SAVE_STREAMING_HEADER && headerString0xA0.Contains("PS4|Final")))
        {
            // Only for files in the save streaming format.
            if (Directory.EnumerateFiles(directory, PlatformPlaystation.ANCHOR_FILE_PATTERN[0]).Any(fullPath.Equals))
            {
                platform = new PlatformPlaystation(headerString0x08 == PlatformPlaystation.SAVEWIZARD_HEADER);
                meta = new(path);
                metaIndex = System.Convert.ToInt32(string.Concat(Path.GetFileNameWithoutExtension(path).Where(char.IsDigit)));
                possibleIndex = Constants.OFFSET_INDEX + platform.COUNT_SAVES_TOTAL - 1; // 11 or 31
            }
        }
        // StartsWith for uncompressed saves and plaintext JSON.
        else if (headerInteger == Constants.SAVE_STREAMING_HEADER || headerString0x20.StartsWith("{\"F2P\":") || headerString0x20.StartsWith("{\"Version\":"))
        {
            if (headerString0xA0.Contains("NX1|Final"))
            {
                platform = new PlatformSwitch();

                // Try to get container index from file name if matches this regular expression: savedata\d{2}\.hg
                if (Directory.EnumerateFiles(directory, PlatformSwitch.ANCHOR_FILE_PATTERN[1]).Any(fullPath.Equals))
                {
                    meta = new(Path.Combine(directory, data.Name.Replace("savedata", "manifest")));
                    metaIndex = System.Convert.ToInt32(string.Concat(Path.GetFileNameWithoutExtension(path).Where(char.IsDigit)));
                    possibleIndex = Constants.OFFSET_INDEX + platform.COUNT_SAVES_TOTAL - 1; // 31
                }
            }
            else
            {
                platform = new PlatformSteam();

                // Try to get container index from file name if matches this regular expression: save\d{0,2}\.hg
                if (Directory.EnumerateFiles(directory, PlatformSteam.ANCHOR_FILE_PATTERN[0]).Any(fullPath.Equals))
                {
                    var stringValue = string.Concat(Path.GetFileNameWithoutExtension(path).Where(char.IsDigit));

                    meta = new(Path.Combine(directory, $"mf_{data.Name}"));
                    metaIndex = string.IsNullOrEmpty(stringValue) ? Constants.OFFSET_INDEX : (System.Convert.ToInt32(stringValue) + 1); // metaIndex = 3 is save2.hg
                    possibleIndex = Constants.OFFSET_INDEX + platform.COUNT_SAVES_TOTAL - 1; // 31
                }
            }
        }
        else
        {
            platform = new PlatformMicrosoft();

            metaIndex = Constants.OFFSET_INDEX;
            possibleIndex = Constants.OFFSET_INDEX + platform.COUNT_SAVES_TOTAL - 1; // 31
        }

        if (platform is null)
            return null;

        if (headerString0x20.StartsWith("{\"F2P\":4098,"))
        {
            metaIndex = 0;
        }
        else if (metaIndex > possibleIndex)
        {
            metaIndex = Constants.OFFSET_INDEX;
        }

        // Create container and load it before returning it.
        var container = new Container(metaIndex, platform)
        {
            DataFile = data,
            MetaFile = meta,
        };
        platform.Load(container);
        return container;
    }

    /// <summary>
    /// Analyzes a path to get the <see cref="Platform"/> it contains.
    /// Default settings are used.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public IPlatform? AnalyzePath(string? path) => AnalyzePath(path, null, CollectionSettings.PreferredPlatform);

    /// <summary>
    /// Analyzes a path to get the <see cref="Platform"/> it contains.
    /// Default settings are used.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="platformPreferred"></param>
    /// <returns></returns>
    public IPlatform? AnalyzePath(string? path, PlatformEnum? platformPreferred) => AnalyzePath(path, null, platformPreferred);

    /// <summary>
    /// Analyzes a path to get the <see cref="Platform"/> it contains.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="platformSettings"></param>
    /// <returns></returns>
    public IPlatform? AnalyzePath(string? path, PlatformSettings? platformSettings) => AnalyzePath(path, platformSettings, CollectionSettings.PreferredPlatform);

    /// <summary>
    /// Analyzes a path to get the <see cref="Platform"/> it contains.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="platformPreferred">Platform that will be checked first.</param>
    /// <param name="platformSettings">Settings for the platform found.</param>
    /// <returns></returns>
    public IPlatform? AnalyzePath(string? path, PlatformSettings? platformSettings, PlatformEnum? platformPreferred)
    {
        if (!ValidatePath(path, out var directory))
            return null;

        platformSettings ??= PlatformSettings;

        if (platformSettings.LoadingStrategy == LoadingStrategyEnum.Empty)
            platformSettings = platformSettings with { LoadingStrategy = LoadingStrategyEnum.Hollow };

        if (_collection.TryGetValue(path!, out var platform))
        {
            platform.SetSettings(platformSettings);
            return platform;
        }

        // First add the preferred platform and then everything else.
        HashSet<PlatformEnum> platformSequence = platformPreferred is not null and not PlatformEnum.Unknown ? new() { platformPreferred.Value } : new();

        foreach (var platformEnum in EnumExtensions.GetValues<PlatformEnum>())
            platformSequence.Add(platformEnum);

        foreach (var platformEnum in platformSequence)
        {
            IPlatform? result = platformEnum switch
            {
                PlatformEnum.Gog => new PlatformGog(directory!, platformSettings),
                PlatformEnum.Microsoft => new PlatformMicrosoft(directory!, platformSettings),
                PlatformEnum.Playstation => new PlatformPlaystation(directory!, platformSettings),
                PlatformEnum.Steam => new PlatformSteam(directory!, platformSettings),
                PlatformEnum.Switch => new PlatformSwitch(directory!, platformSettings),
                _ => null,
            };
            if (result?.IsLoaded == true)
            {
                _collection.TryAdd(path!, result);
                return result;
            }
        }

        // Nothing found.
        return null;
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
    private static IEnumerable<DirectoryInfo> GetAccountsInPlatform<T>() where T : IPlatform
    {
        var typeofT = typeof(T);
        return typeofT switch
        {
            _ when typeofT == typeof(PlatformGog) => GetAccountsInPath(PlatformGog.PATH, PlatformGog.ACCOUNT_PATTERN),
            _ when typeofT == typeof(PlatformMicrosoft) => GetAccountsInPath(PlatformMicrosoft.PATH, PlatformMicrosoft.ACCOUNT_PATTERN),
            _ when typeofT == typeof(PlatformSteam) => GetAccountsInPath(PlatformSteam.PATH, PlatformSteam.ACCOUNT_PATTERN),
            _ => [],
        };
    }

    /// <summary>
    /// Gets an enumerable of <see cref="DirectoryInfo"/> of all accounts in the specified path.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private static IEnumerable<DirectoryInfo> GetAccountsInPath(string path, string pattern)
    {
        return ValidatePath(path, out var directory) ? directory!.EnumerateDirectories(pattern) : [];
    }

    /// <summary>
    /// Checks whether the specified path is valid.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="directory"></param>
    /// <returns></returns>
    private static bool ValidatePath(string? path, out DirectoryInfo? directory)
    {
        directory = null;
        if (string.IsNullOrWhiteSpace(path))
            return false;

        directory = new DirectoryInfo(path);
        return directory.Exists;
    }
}
