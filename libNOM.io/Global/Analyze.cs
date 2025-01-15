using libNOM.io.Settings;

namespace libNOM.io.Global;


// EXTERNAL RELEASE: If any, add the new platform here as well.
public static class Analyze
{
    #region Field

    private static byte[] _headerByte = [];
    private static string? _headerString0x08;
    private static string? _headerString0x20;
    private static string? _headerString0xA0;

    #endregion

    #region AnalyzeFile

    /// <inheritdoc cref="AnalyzeFile(string, PlatformSettings?)"
    public static Container? AnalyzeFile(string path) => AnalyzeFile(path, new());

    /// <summary>
    /// Analysis a single file and loads it.
    /// </summary>
    /// <param name="path"></param>
    /// <returns>A pre-loaded <see cref="Container"/> if no incompatibilities.</returns>
    public static Container? AnalyzeFile(string path, PlatformSettings? platformSettings)
    {
        FileInfo data = new(path);
        if (!ExtractHeader(data))
            return null;

        var platform = GetPlatform(data, platformSettings, out var metaIndex);
        if (platform is null)
            return null;

        // Create container and load it before returning it.
        var container = new Container(metaIndex, platform) { DataFile = data, MetaFile = GetPlatformMeta(platform.GetType(), data) };
        platform.Load(container);
        return container;
    }

    #endregion

    #region AnalyzePath

    // public //

    /// <summary>
    /// Analyzes a path to get the <see cref="Platform"/> it contains.
    /// Default settings are used.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static IPlatform? AnalyzePath(string? path) => AnalyzePath(path, null, null);

    /// <summary>
    /// Analyzes a path to get the <see cref="Platform"/> it contains.
    /// Default settings are used.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="platformPreferred"></param>
    /// <returns></returns>
    public static IPlatform? AnalyzePath(string? path, PlatformEnum? platformPreferred) => AnalyzePath(path, null, platformPreferred);

    /// <summary>
    /// Analyzes a path to get the <see cref="Platform"/> it contains.
    /// Default settings are used.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="platformSettings"></param>
    /// <returns></returns>
    public static IPlatform? AnalyzePath(string? path, PlatformSettings? platformSettings) => AnalyzePath(path, platformSettings, null);

    /// <summary>
    /// Analyzes a path to get the <see cref="Platform"/> it contains.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="platformPreferred">Platform that will be checked first.</param>
    /// <param name="platformSettings">Settings for the platform found.</param>
    /// <returns></returns>
    public static IPlatform? AnalyzePath(string? path, PlatformSettings? platformSettings, PlatformEnum? platformPreferred)
    {
        if (!ValidatePath(path, out var directory))
            return null;

        platformSettings ??= new();

        if (platformSettings.LoadingStrategy == LoadingStrategyEnum.Empty)
            platformSettings = platformSettings with { LoadingStrategy = LoadingStrategyEnum.Hollow };

        foreach (var platformEnum in GetPlatformPreferredSequence(platformPreferred))
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
                return result;
        }

        // Nothing found.
        return null;
    }

    // internal //

    /// <summary>
    /// Checks whether the specified path is valid.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="directory"></param>
    /// <returns></returns>
    internal static bool ValidatePath(string? path, out DirectoryInfo? directory)
    {
        directory = null;
        if (string.IsNullOrWhiteSpace(path))
            return false;

        directory = new DirectoryInfo(path);
        return directory.Exists;
    }

    #endregion

    #region Helper

    private static Platform CreatePlatform<T>(FileInfo data, PlatformSettings? platformSettings, out int metaIndex) where T : Platform
    {
        var typeofT = typeof(T);

        // For PlayStation we have to determine whether SaveWizard is used.
        Platform platform = typeofT == typeof(PlatformPlaystation) ? new PlatformPlaystation(_headerString0x08 == PlatformPlaystation.SAVEWIZARD_HEADER, platformSettings) : (T)(Activator.CreateInstance(typeofT, platformSettings))!;

        metaIndex = GetPlatformMetaIndex(typeofT, data, platform);

        return platform;
    }

    private static bool ExtractHeader(FileInfo data)
    {
        if (!data.Exists)
            return false;

        ReadOnlySpan<byte> bytes = data.ReadAllBytes();
        if (bytes.Length < 0xA0)
            return false;

        // Convert header with different lengths.
        _headerByte = bytes[..0x4].ToArray();
        _headerString0x08 = bytes[..0x08].GetString();
        _headerString0x20 = bytes[..0x20].GetString();
        _headerString0xA0 = bytes[..0xA0].GetString();

        return true;
    }

    private static Platform? GetPlatform(FileInfo data, PlatformSettings? platformSettings, out int metaIndex)
    {
        // Define variables and fill them while creating a platform.
        Platform? platform;

        // Select a platform based on the content.
        if (_headerString0x08 == PlatformPlaystation.SAVEWIZARD_HEADER || (_headerByte.SequenceEqual(Constants.SAVE_STREAMING_HEADER) && _headerString0xA0!.Contains("PS4|Final")))
        {
            platform = CreatePlatform<PlatformPlaystation>(data, platformSettings, out metaIndex);

            // Special case for legacy memory.dat as all save were in one file. Try to get the first existing save.
            if (metaIndex == -1 && platform?.GetSaveContainers().FirstOrDefault(i => i.Exists)?.MetaIndex is int index && index >= Constants.OFFSET_INDEX)
                metaIndex = index;

            if (metaIndex == -1)
                platform = null;
        }
        // StartsWith for uncompressed saves and plaintext JSON.
        else if (_headerByte.SequenceEqual(Constants.SAVE_STREAMING_HEADER) || _headerString0x20!.StartsWith("{\"F2P\":") || _headerString0x20!.StartsWith("{\"Version\":"))
        {
            if (_headerString0xA0!.Contains("NX1|Final"))
                platform = CreatePlatform<PlatformSwitch>(data, platformSettings, out metaIndex);
            else
                platform = CreatePlatform<PlatformSteam>(data, platformSettings, out metaIndex);
        }
        else
            platform = CreatePlatform<PlatformMicrosoft>(data, platformSettings, out metaIndex);

        return platform;
    }

    private static FileInfo? GetPlatformMeta(Type type, FileInfo data)
    {
        FileInfo? meta = null;

        if (type == typeof(PlatformSteam))
        {
            // Same filename as data but with a prefix.
            meta = new(Path.Combine(data.Directory!.FullName, $"mf_{data.Name}"));
        }
        else if (type == typeof(PlatformMicrosoft))
        {
            // Search for third file in folder (not data and not container).
            meta = data.Directory!.EnumerateFiles().FirstOrDefault(i => !i.Name.StartsWith("container.") && !i.FullName.Equals(data.FullName));
        }
        else if (type == typeof(PlatformPlaystation))
        {
            // Same file as meta is prepended.
            meta = data;
        }
        else if (type == typeof(PlatformSwitch))
        {
            // Replace text but keep number and file extension.
            meta = new(Path.Combine(data.Directory!.FullName, data.Name.Replace("savedata", "manifest")));
        }

        return meta;
    }

    private static int GetPlatformMetaIndex(Type type, FileInfo data, Platform platform)
    {
        var digits = string.Concat(Path.GetFileNameWithoutExtension(data.Name).Where(char.IsDigit));
        int? index = null;

        if (string.IsNullOrEmpty(digits))
        {
            if (type == typeof(PlatformPlaystation))
            {
                index = -1;
            }
        }
        else
        {
            if (type == typeof(PlatformSteam))
            {
                index = System.Convert.ToInt32(digits) + 1; // metaIndex = 3 is save2.hg
            }
            else if (type == typeof(PlatformPlaystation) || type == typeof(PlatformSwitch))
            {
                index = System.Convert.ToInt32(digits);
            }
        }

        var possibleIndex = Constants.OFFSET_INDEX + platform.MAX_SAVE_TOTAL - 1;
        if (index > possibleIndex)
            index = Constants.OFFSET_INDEX;

        return index ?? Constants.OFFSET_INDEX;
    }

    private static HashSet<PlatformEnum> GetPlatformPreferredSequence(PlatformEnum? platformPreferred)
    {
        var preferred = platformPreferred ?? PlatformEnum.Unknown;

        // First add the preferred platform and then everything else.
        HashSet<PlatformEnum> platformSequence = preferred is not PlatformEnum.Unknown ? new() { preferred } : new();

        // Preferred platform is already there (if specified) and Unknown is ignored later.
        foreach (var platformEnum in EnumExtensions.GetValues<PlatformEnum>())
            platformSequence.Add(platformEnum);

        return platformSequence;
    }

    #endregion
}
