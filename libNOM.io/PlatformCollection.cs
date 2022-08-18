using System.Collections.Concurrent;
using System.Security;

namespace libNOM.io;


/// <summary>
/// Holds all detected platforms.
/// </summary>
public class PlatformCollection
{
    #region Member

    private readonly ConcurrentDictionary<string, Platform> Collection = new();

    #endregion

    // //

    #region Constructor

    public PlatformCollection()
    {
        Reinitialize();
    }

    public PlatformCollection(string path) : this(path, null, null) { }

    public PlatformCollection(string path, PlatformEnum preferredPlatform) : this(path, null, preferredPlatform) { }

    public PlatformCollection(string path, PlatformSettings platformSettings) : this(path, platformSettings, null) { }

    public PlatformCollection(string path, PlatformSettings? platformSettings, PlatformEnum? preferredPlatform) : this()
    {
        AnalyzePath(path, platformSettings, preferredPlatform);
    }

    #endregion

    // //

    #region Analyze

    /// <summary>
    /// Analysis a single file and loads it.
    /// </summary>
    /// <param name="path"></param>
    /// <returns>A pre-loaded Container.</returns>
    public static Container? AnalyzeFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;

        var bytes = File.ReadAllBytes(path);
        int metaIndex, possibleIndex = metaIndex = Global.OFFSET_INDEX;

        // Convert header to different formats.
        var headerInteger = bytes.Take(4).GetUInt32().FirstOrDefault();
        var headerPlaintext = string.Concat(bytes.Take(30).GetString().Where(c => !char.IsWhiteSpace(c)));
        var headerString = bytes.Take(8).GetString();

        // Select a platform to convert the file with, based on the content.
        Platform platform;
        if (headerString == Global.HEADER_SAVEWIZARD)
        {
            platform = new PlatformPlaystation();

            // Try to get container index from file name if matches this regular expression: savedata\d{2}\.hg
            if (PlatformPlaystation.DirectoryData.AnchorFileRegex[0].IsMatch(Path.GetFileName(path)))
            {
                metaIndex = System.Convert.ToInt32(string.Concat(Path.GetFileNameWithoutExtension(path).Where(c => char.IsDigit(c))));
                possibleIndex = Global.OFFSET_INDEX + platform.COUNT_SLOTS * platform.COUNT_SAVES_PER_SLOT - 1; // 11 or 31
            }
        }
#if NETSTANDARD2_0
        else if (headerInteger == Global.HEADER_SAVE_STREAMING_CHUNK || headerString.Substring(0, 7) == Global.HEADER_PLAINTEXT_OBFUSCATED || headerPlaintext.Contains(Global.HEADER_PLAINTEXT))
#else
        else if (headerInteger == Global.HEADER_SAVE_STREAMING_CHUNK || headerString[..7] == Global.HEADER_PLAINTEXT_OBFUSCATED || headerPlaintext.Contains(Global.HEADER_PLAINTEXT))
#endif
        {
            platform = new PlatformSteam();

            // Try to get container index from file name if matches this regular expression: save\d{0,2}\.hg
            if (PlatformSteam.DirectoryData.AnchorFileRegex[0].IsMatch(Path.GetFileName(path)))
            {
                var stringValue = string.Concat(Path.GetFileNameWithoutExtension(path).Where(c => char.IsDigit(c)));

                metaIndex = string.IsNullOrEmpty(stringValue) ? Global.OFFSET_INDEX : (System.Convert.ToInt32(stringValue) + 1); // metaIndex = 3 is save2.hg
                possibleIndex = Global.OFFSET_INDEX + platform.COUNT_SLOTS * platform.COUNT_SAVES_PER_SLOT - 1; // 31
            }
        }
        else
        {
            platform = new PlatformMicrosoft();
        }

        var container = new Container(metaIndex > possibleIndex ? Global.OFFSET_INDEX : metaIndex)
        {
            DataFile = new FileInfo(path),
        };

        // Load container before returning it.
        platform.Load(container);
        return container;
    }

    /// <inheritdoc cref="AnalyzePath(string, PlatformSettings?, PlatformEnum?)"/>
    public Platform? AnalyzePath(string path)
    {
        return AnalyzePath(path, null, null);
    }

    /// <inheritdoc cref="AnalyzePath(string, PlatformSettings?, PlatformEnum?)"/>
    public Platform? AnalyzePath(string path, PlatformEnum preferredPlatform)
    {
        return AnalyzePath(path, null, preferredPlatform);
    }

    /// <inheritdoc cref="AnalyzePath(string, PlatformSettings?, PlatformEnum?)"/>
    public Platform? AnalyzePath(string path, PlatformSettings platformSettings)
    {
        return AnalyzePath(path, platformSettings, null);
    }

    /// <summary>
    /// Analyzes a path to find the <see cref="Platform"/> it contains.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="preferredPlatform">Platform that will be checked first.</param>
    /// <param name="platformSettings">Settings for the platform found.</param>
    /// <returns></returns>
    public Platform? AnalyzePath(string path, PlatformSettings? platformSettings, PlatformEnum? preferredPlatform)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        var invalid = IsPathInvalid(path, out DirectoryInfo? directory);
        if (invalid)
            return null;

        if (Collection.ContainsKey(path))
        {
            Collection[path].SetSettings(platformSettings);
            return Collection[path];
        }

        // Try preferred platform first.
        var platforms = preferredPlatform is null ? new HashSet<PlatformEnum>() : new HashSet<PlatformEnum> { preferredPlatform.Value };
#if NETSTANDARD2_0_OR_GREATER
        foreach (PlatformEnum platform in Enum.GetValues(typeof(PlatformEnum)))
#else
        foreach (var platform in Enum.GetValues<PlatformEnum>())
#endif
        {
            platforms.Add(platform);
        }

        foreach (var platformEnum in platforms)
        {
            // As Steam is more popular only use GOG if it is the preferred platform.
            Platform? platform = platformEnum switch
            {
                PlatformEnum.Gog => preferredPlatform == PlatformEnum.Gog ? new PlatformGog(directory!, platformSettings) : null,
                PlatformEnum.Microsoft => new PlatformMicrosoft(directory!, platformSettings),
                PlatformEnum.Playstation => new PlatformPlaystation(directory!, platformSettings),
                PlatformEnum.Steam => new PlatformSteam(directory!, platformSettings),
                PlatformEnum.Switch => new PlatformSwitch(directory!, platformSettings),
                _ => default,
            };
            if (platform?.IsLoaded == true)
            {
                Collection.TryAdd(path, platform);
                return platform;
            }
        }

        // Nothing found.
        return null;
    }

    /// <summary>
    /// Checks whether the specified path is valid.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="directory"></param>
    /// <returns></returns>
    private static bool IsPathInvalid(string path, out DirectoryInfo? directory)
    {
        try
        {
            directory = new DirectoryInfo(path);
        }
        catch (Exception x) when (x is ArgumentNullException or SecurityException or ArgumentException or PathTooLongException)
        {
            directory = null;
            return true;
        }

        return !directory.Exists;
    }

    #endregion

    #region Getter

    /// <summary>
    /// Gets the <see cref="Platform"/> in the specified path if exists.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public Platform? Get(string path)
    {
        _ = Collection.TryGetValue(path, out Platform? platform);
        return platform;
    }

    /// <summary>
    /// Gets all <see cref="Platform"/> in this collection.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public IEnumerable<Platform> Get()
    {
        return Collection.Select(p => p.Value);
    }

    #endregion

    #region Initialize

    /// <summary>
    /// Initializes the collection with empty PC <see cref="Platform"/>.
    /// Only PC platforms have a default path and can be located directly on the machine.
    /// </summary>
    public void Reinitialize()
    {
        Collection.Clear();
        var tasks = new List<Task>();

        // Only PC platforms can be located directly on the machine.
        tasks.AddRange(TryAddDirectory<PlatformGog>());
        tasks.AddRange(TryAddDirectory<PlatformMicrosoft>());
        tasks.AddRange(TryAddDirectory<PlatformSteam>());

        Task.WaitAll(tasks.ToArray());
    }

    /// <summary>
    /// Adds the specified <see cref="Platform"/> to the collection.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    private List<Task> TryAddDirectory<T>() where T : Platform
    {
        var directories = Platform.GetDirectoriesInDefaultPath<T>();
        var tasks = new List<Task>();

        foreach (var directory in directories)
        {
            tasks.Add(Task.Run(() =>
            {
                var platform = (T)(Activator.CreateInstance(typeof(T), directory))!;
                if (platform.IsValid)
                {
                    Collection.TryAdd(directory.FullName, platform);
                }
            }));
        }

        return tasks;
    }

    #endregion
}
