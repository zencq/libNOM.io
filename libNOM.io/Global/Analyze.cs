using libNOM.io.Interfaces;
using libNOM.io.Settings;

namespace libNOM.io.Global;


public static class Analyze
{
    #region Field

    private static uint _headerInteger = uint.MaxValue;
    private static string? _headerString0x08;
    private static string? _headerString0x20;
    private static string? _headerString0xA0;

    #endregion

    // Analyze

    #region File

    // public //

    /// <inheritdoc cref="AnalyzeFile(string, PlatformSettings?)"
    public static Container? AnalyzeFile(string path) => AnalyzeFile(path, new());

    /// <summary>
    /// Analysis a single file and loads it.
    /// </summary>
    /// <param name="path"></param>
    /// <returns>A pre-loaded <see cref="Container"/> if no incompatibilities.</returns>
    public static Container? AnalyzeFile(string path, PlatformSettings? platformSettings)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;

        FileInfo data = new(path);
        UpdateHeader(data);

        // Define variables and fill them while generating a platform.
        FileInfo? meta;
        int metaIndex;
        Platform? platform;

        // Select a platform below to convert the file with, based on the content.
        if (_headerString0x08 == PlatformPlaystation.SAVEWIZARD_HEADER || (_headerInteger == Constants.SAVE_STREAMING_HEADER && _headerString0xA0!.Contains("PS4|Final")))
        {
            platform = GenerateCommonPlatform<PlatformPlaystation>(data, platformSettings, out metaIndex, out meta);
        }
        // StartsWith for uncompressed saves and plaintext JSON.
        else if (_headerInteger == Constants.SAVE_STREAMING_HEADER || _headerString0x20!.StartsWith("{\"F2P\":") || _headerString0x20.StartsWith("{\"Version\":"))
        {
            if (_headerString0xA0!.Contains("NX1|Final"))
                platform = GenerateCommonPlatform<PlatformSwitch>(data, platformSettings, out metaIndex, out meta);
            else
                platform = GenerateCommonPlatform<PlatformSteam>(data, platformSettings, out metaIndex, out meta);
        }
        else
            platform = GenerateCommonPlatform<PlatformMicrosoft>(data, platformSettings, out metaIndex, out meta);

        if (platform is null)
            return null;

        // Create container and load it before returning it.
        var container = new Container(metaIndex, platform) { DataFile = data, MetaFile = meta };
        platform.Load(container);
        return container;
    }

    // private //

    private static void UpdateHeader(FileInfo data)
    {
        ReadOnlySpan<byte> bytes = data.ReadAllBytes();

        // Convert header with different lengths.
        _headerInteger = bytes.Cast<uint>(0);
        _headerString0x08 = bytes[..0x08].GetString();
        _headerString0x20 = bytes[..0x20].GetString();
        _headerString0xA0 = bytes[..0xA0].GetString();
    }

    #endregion

    #region Path

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

        foreach (var platformEnum in GetPreferredPlatformSequence(platformPreferred))
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

    private static HashSet<PlatformEnum> GetPreferredPlatformSequence(PlatformEnum? platformPreferred)
    {
        // First add the preferred platform and then everything else.
        HashSet<PlatformEnum> platformSequence = platformPreferred is not null and not PlatformEnum.Unknown ? new() { platformPreferred.Value } : new();

        foreach (var platformEnum in EnumExtensions.GetValues<PlatformEnum>())
            platformSequence.Add(platformEnum);

        return platformSequence;
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

    // private //

    private static Platform? GenerateCommonPlatform<T>(FileInfo data, PlatformSettings? platformSettings, out int metaIndex, out FileInfo? meta) where T : Platform
    {
        meta = GetCommonPlatformMeta<T>(data);
        metaIndex = GetCommonPlatformMetaIndex<T>(data.Name);

        var platform = GetCommonPlatform<T>(platformSettings);
        if (platform is not null)
        {
            var possibleIndex = Constants.OFFSET_INDEX + platform.COUNT_SAVES_TOTAL - 1; // 31
            if (metaIndex > possibleIndex)
                metaIndex = Constants.OFFSET_INDEX;
        }
        return platform;
    }

    private static Platform? GetCommonPlatform<T>(PlatformSettings? platformSettings) where T : Platform => typeof(T) switch
    {
        var typeofT when typeofT == typeof(PlatformPlaystation) => new PlatformPlaystation(_headerString0x08 == PlatformPlaystation.SAVEWIZARD_HEADER, platformSettings),
        _ => (T)(Activator.CreateInstance(typeof(T), platformSettings))!,
    };

    private static FileInfo? GetCommonPlatformMeta<T>(FileInfo data) where T : Platform => typeof(T) switch
    {
        var typeofT when typeofT == typeof(PlatformPlaystation) => data,
        var typeofT when typeofT == typeof(PlatformSteam) => new(Path.Combine(data.Directory!.FullName, $"mf_{data.Name}")),
        var typeofT when typeofT == typeof(PlatformSwitch) => new(Path.Combine(data.Directory!.FullName, data.Name.Replace("savedata", "manifest"))),
        _ => null,
    };

    private static int GetCommonPlatformMetaIndex<T>(string name) where T : Platform => typeof(T) switch
    {
        var typeofT when typeofT == typeof(PlatformPlaystation) => System.Convert.ToInt32(string.Concat(Path.GetFileNameWithoutExtension(name).Where(char.IsDigit))),
        var typeofT when typeofT == typeof(PlatformSteam) && string.Concat(Path.GetFileNameWithoutExtension(name).Where(char.IsDigit)) is string stringValue && !string.IsNullOrEmpty(stringValue) => System.Convert.ToInt32(stringValue) + 1, // metaIndex = 3 is save2.h,
        var typeofT when typeofT == typeof(PlatformSwitch) => System.Convert.ToInt32(string.Concat(Path.GetFileNameWithoutExtension(name).Where(char.IsDigit))),
        _ => Constants.OFFSET_INDEX,
    };
}
