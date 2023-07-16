using libNOM.io.Extensions;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Security;

namespace libNOM.io;


/// <summary>
/// Holds all detected platforms.
/// </summary>
public class PlatformCollection : IEnumerable<Platform>
{
    #region Field

    private readonly ConcurrentDictionary<string, Platform> _collection = new();

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

        ReadOnlySpan<byte> bytes = File.ReadAllBytes(path);
        int metaIndex, possibleIndex = metaIndex = Globals.Constants.OFFSET_INDEX;

        // Convert header to different formats.
        //var headerInteger = bytes[..4].CastToUInt32();
        //ReadOnlySpan<char> headerStringLong = bytes[..30].GetString().Where(c => !char.IsWhiteSpace(c)).ToString().ToReadOnlySpan();
        //ReadOnlySpan<char> headerStringShort = bytes[..8].GetString();

        //// Select a platform to convert the file with, based on the content.
        //Platform platform;
        //if (headerStringShort == Global.HEADER_SAVEWIZARD || (headerInteger == Global.HEADER_SAVE_STREAMING_CHUNK && headerStringLong.Contains("PS4|Final".ToReadOnlySpan(), StringComparison.Ordinal)))
        //{
        //    platform = new PlatformPlaystation();

        //    // Try to get container index from file name if matches this regular expression: savedata\d{2}\.hg
        //    if (PlatformPlaystation.DirectoryData.AnchorFileRegex[0].IsMatch(Path.GetFileName(path)))
        //    {
        //        metaIndex = System.Convert.ToInt32(string.Concat(Path.GetFileNameWithoutExtension(path).Where(c => char.IsDigit(c))));
        //        possibleIndex = Global.OFFSET_INDEX + platform.COUNT_SLOTS * platform.COUNT_SAVES_PER_SLOT - 1; // 11 or 31
        //    }
        //}
        //else if (headerInteger == Global.HEADER_SAVE_STREAMING_CHUNK || headerStringShort.Contains(Global.HEADER_PLAINTEXT_OBFUSCATED.ToReadOnlySpan(), StringComparison.Ordinal) || headerStringLong.Contains(Global.HEADER_PLAINTEXT.ToReadOnlySpan(), StringComparison.Ordinal))
        //{
        //    if (headerStringLong.Contains("NX1|Final".ToReadOnlySpan(), StringComparison.Ordinal))
        //    {
        //        platform = new PlatformSwitch();

        //        // Try to get container index from file name if matches this regular expression: savedata\d{2}\.hg
        //        if (PlatformSwitch.DirectoryData.AnchorFileRegex[1].IsMatch(Path.GetFileName(path)))
        //        {
        //            metaIndex = System.Convert.ToInt32(string.Concat(Path.GetFileNameWithoutExtension(path).Where(c => char.IsDigit(c))));
        //            possibleIndex = Global.OFFSET_INDEX + platform.COUNT_SLOTS * platform.COUNT_SAVES_PER_SLOT - 1; // 11 or 31
        //        }
        //    }
        //    else
        //    {
        //        platform = new PlatformSteam();

        //        // Try to get container index from file name if matches this regular expression: save\d{0,2}\.hg
        //        if (PlatformSteam.DirectoryData.AnchorFileRegex[0].IsMatch(Path.GetFileName(path)))
        //        {
        //            var stringValue = string.Concat(Path.GetFileNameWithoutExtension(path).Where(c => char.IsDigit(c)));

        //            metaIndex = string.IsNullOrEmpty(stringValue) ? Global.OFFSET_INDEX : (System.Convert.ToInt32(stringValue) + 1); // metaIndex = 3 is save2.hg
        //            possibleIndex = Global.OFFSET_INDEX + platform.COUNT_SLOTS * platform.COUNT_SAVES_PER_SLOT - 1; // 31
        //        }
        //    }
        //}
        //else
        //{
        //    platform = new PlatformMicrosoft();
        //}

        var container = new Container(metaIndex > possibleIndex ? Globals.Constants.OFFSET_INDEX : metaIndex)
        {
            DataFile = new FileInfo(path),
        };

        // Load container before returning it.
        //platform.Load(container);
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

        if (_collection.ContainsKey(path))
        {
            _collection[path].SetSettings(platformSettings);
            return _collection[path];
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

        //foreach (var platformEnum in platforms)
        //{
        //    // As Steam is more popular only use GOG if it is the preferred platform.
        //    Platform? platform = platformEnum switch
        //    {
        //        PlatformEnum.Gog => preferredPlatform == PlatformEnum.Gog ? new PlatformGog(directory!, platformSettings) : null,
        //        PlatformEnum.Microsoft => new PlatformMicrosoft(directory!, platformSettings),
        //        PlatformEnum.Playstation => new PlatformPlaystation(directory!, platformSettings),
        //        PlatformEnum.Steam => new PlatformSteam(directory!, platformSettings),
        //        PlatformEnum.Switch => new PlatformSwitch(directory!, platformSettings),
        //        _ => default,
        //    };
        //    if (platform?.IsLoaded == true)
        //    {
        //        _collection.TryAdd(path, platform);
        //        return platform;
        //    }
        //}

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
        _ = _collection.TryGetValue(path, out Platform? platform);
        return platform;
    }

    #endregion

    #region IEnumerable

    public IEnumerator<Platform> GetEnumerator()
    {
        foreach (var pair in _collection)
        {
            yield return pair.Value;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    #endregion

    #region Initialize

    /// <summary>
    /// Initializes the collection with empty PC <see cref="Platform"/>.
    /// Only PC platforms have a default path and can be located directly on the machine.
    /// </summary>
    public void Reinitialize()
    {
        _collection.Clear();
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
        //var directories = Platform.GetDirectoriesInDefaultPath<T>();
        var tasks = new List<Task>();

        //foreach (var directory in directories)
        //{
        //    tasks.Add(Task.Run(() =>
        //    {
        //        var platform = (T)(Activator.CreateInstance(typeof(T), directory))!;
        //        if (platform.IsValid)
        //        {
        //            _collection.TryAdd(directory.FullName, platform);
        //        }
        //    }));
        //}

        return tasks;
    }

    #endregion
}
