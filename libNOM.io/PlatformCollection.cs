﻿using libNOM.io.Interfaces;
using System.Collections;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace libNOM.io;


/// <summary>
/// Holds all accounts of all detected platforms.
/// </summary>
public class PlatformCollection : IEnumerable<IPlatform>
{
    #region Field

    private readonly ConcurrentDictionary<string, IPlatform> _collection = new();

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

    // //

    #region Constructor

    public PlatformCollection()
    {
        Reinitialize();
    }

    public PlatformCollection(string path) : this(path, null, null) { }

    public PlatformCollection(string path, PlatformEnum platformPreferred) : this(path, null, platformPreferred) { }

    public PlatformCollection(string path, PlatformSettings platformSettings) : this(path, platformSettings, null) { }

    public PlatformCollection(string path, PlatformSettings? platformSettings, PlatformEnum? platformPreferred) : this() // Reinitialize before Analyze
    {
        _ = AnalyzePath(path, platformSettings, platformPreferred);
    }

    #endregion

    #region IEnumerable

    public IEnumerator<IPlatform> GetEnumerator()
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

    // public //

    #region Initialize

    /// <summary>
    /// Initializes the collection with empty PC <see cref="Platform"/>s.
    /// Only on a PC, platforms have a default path and can be located directly on the machine.
    /// </summary>
    public void Reinitialize()
    {
        _collection.Clear();
        var tasks = new List<Task>();

        // Only PC platforms can be located directly on the machine.
        tasks.AddRange(TryAddDirectory<PlatformSteam>()); // is available on all operating systems

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            tasks.AddRange(TryAddDirectory<PlatformGog>());
            tasks.AddRange(TryAddDirectory<PlatformMicrosoft>());
        }
        // Not yet available.
        //else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) // macOS
        //{
        //    tasks.AddRange(TryAddDirectory<PlatformApple>());
        //}

        Task.WaitAll(tasks.ToArray());
    }

    #endregion

    #region Analyze

    /// <summary>
    /// Analysis a single file and loads it.
    /// </summary>
    /// <param name="path"></param>
    /// <returns>A pre-loaded Container.</returns>
    public static Container? AnalyzeFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;

        ReadOnlySpan<byte> bytes = File.ReadAllBytes(path);
        FileInfo? meta = null;
        int metaIndex, possibleIndex = metaIndex = Constants.OFFSET_INDEX;
        Platform? platform = null;

        // Convert header with different lengths.
        var headerInteger = bytes.Cast<uint>(0);
        var headerString0x08 = bytes.Slice(0, 0x08).GetString();
        var headerString0x20 = bytes.Slice(0, 0x20).GetString();
        var headerString0xA0 = bytes.Slice(0, 0xA0).GetString();

        // Select a platform below to convert the file with, based on the content.
        // All kinds of Playstation.
        if (headerString0x08 == PlatformPlaystation.SAVEWIZARD_HEADER || (headerInteger == Constants.SAVE_STREAMING_HEADER && headerString0xA0.Contains("PS4|Final")))
        {
            // Only for files in the save streaming format.
            if (PlatformPlaystation.ANCHOR_FILE_REGEX[0].IsMatch(Path.GetFileName(path)))
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
            if (headerString0x20.Contains("NX1|Final"))
            {
                platform = new PlatformSwitch();

                // Try to get container index from file name if matches this regular expression: savedata\d{2}\.hg
                if (PlatformSwitch.ANCHOR_FILE_REGEX[1].IsMatch(Path.GetFileName(path)))
                {
                    metaIndex = System.Convert.ToInt32(string.Concat(Path.GetFileNameWithoutExtension(path).Where(char.IsDigit)));
                    possibleIndex = Constants.OFFSET_INDEX + platform.COUNT_SAVES_TOTAL - 1; // 31
                }
            }
            else
            {
                platform = new PlatformSteam();

                // Try to get container index from file name if matches this regular expression: save\d{0,2}\.hg
                if (PlatformSteam.ANCHOR_FILE_REGEX[0].IsMatch(Path.GetFileName(path)))
                {
                    var stringValue = string.Concat(Path.GetFileNameWithoutExtension(path).Where(char.IsDigit));

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
        var container = new Container(metaIndex)
        {
            DataFile = new FileInfo(path),
            MetaFile = meta,
        };
        platform.Load(container);
        return container.IsLoaded ? container : null;
    }

    /// <summary>
    /// Analyzes a path to get the <see cref="Platform"/> it contains.
    /// Default settings are used.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public IPlatform? AnalyzePath(string path) => AnalyzePath(path, null, null);

    /// <summary>
    /// Analyzes a path to get the <see cref="Platform"/> it contains.
    /// Default settings are used.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="platformPreferred"></param>
    /// <returns></returns>
    public IPlatform? AnalyzePath(string path, PlatformEnum platformPreferred) => AnalyzePath(path, null, platformPreferred);

    /// <summary>
    /// Analyzes a path to get the <see cref="Platform"/> it contains.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="platformSettings"></param>
    /// <returns></returns>
    public IPlatform? AnalyzePath(string path, PlatformSettings platformSettings) => AnalyzePath(path, platformSettings, null);

    /// <summary>
    /// Analyzes a path to get the <see cref="Platform"/> it contains.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="platformPreferred">Platform that will be checked first.</param>
    /// <param name="platformSettings">Settings for the platform found.</param>
    /// <returns></returns>
    public IPlatform? AnalyzePath(string path, PlatformSettings? platformSettings, PlatformEnum? platformPreferred)
    {
        platformSettings ??= new() { LoadingStrategy = LoadingStrategyEnum.Hollow };

        if (_collection.ContainsKey(path))
        {
            _collection[path].SetSettings(platformSettings);
            return _collection[path];
        }

        if (!IsPathValid(path, out var directory))
            return null;

        // First add the preferred platform and then everything else.
        HashSet<PlatformEnum> platforms = platformPreferred is not null and not PlatformEnum.Unknown ? new() { platformPreferred.Value } : new();

#if NETSTANDARD2_0_OR_GREATER
        foreach (var platform in (PlatformEnum[])(Enum.GetValues(typeof(PlatformEnum))))
            platforms.Add(platform);
#else
        foreach (var platform in Enum.GetValues<PlatformEnum>())
            platforms.Add(platform);
#endif

        foreach (var platform in platforms)
        {
            Platform? result = platform switch
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
                _collection.TryAdd(path, result);
                return result;
            }
        }

        // Nothing found.
        return null;
    }

    #endregion

    // private //

    /// <summary>
    /// Checks whether the specified path is valid.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="directory"></param>
    /// <returns></returns>
    private static bool IsPathValid(string path, out DirectoryInfo? directory)
    {
        directory = null;
        if (string.IsNullOrWhiteSpace(path))
            return false;

        directory = new DirectoryInfo(path);
        return directory.Exists;
    }

    /// <summary>
    /// Adds all accounts available in the default path of the specified <see cref="Platform"/> to the collection.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    private List<Task> TryAddDirectory<T>() where T : IPlatform
    {
        var directories = GetAccountsInPlatform<T>();
        var tasks = new List<Task>();

        foreach (var directory in directories)
        {
            tasks.Add(Task.Run(() =>
            {
                var platform = (T)(Activator.CreateInstance(typeof(T), directory))!;
                if (platform.IsValid)
                {
                    _collection.TryAdd(directory.FullName, platform);
                }
            }));
        }

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
            // Not yet available.
            //Type _ when typeofT == typeof(PlatformApple) => GetAccountsInPath(PlatformApple.PATH, PlatformApple.ACCOUNT_PATTERN);
            Type _ when typeofT == typeof(PlatformGog) => GetAccountsInPath(PlatformGog.PATH, PlatformGog.ACCOUNT_PATTERN),
            Type _ when typeofT == typeof(PlatformMicrosoft) => GetAccountsInPath(PlatformMicrosoft.PATH, PlatformMicrosoft.ACCOUNT_PATTERN),
            Type _ when typeofT == typeof(PlatformSteam) => GetAccountsInPath(PlatformSteam.PATH, PlatformSteam.ACCOUNT_PATTERN),
            _ => Enumerable.Empty<DirectoryInfo>(),
        };
    }

    /// <summary>
    /// Gets an enumerable of <see cref="DirectoryInfo"/> of all accounts in the specified path.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private static IEnumerable<DirectoryInfo> GetAccountsInPath(string path, string pattern)
    {
        return IsPathValid(path, out var directory) ? directory!.GetDirectories(pattern) : Enumerable.Empty<DirectoryInfo>();
    }
}
