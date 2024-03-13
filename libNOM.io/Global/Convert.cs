using System.Reflection;

using CommunityToolkit.Diagnostics;

namespace libNOM.io.Global;


public static class Convert
{
    #region ToJson

    /// <summary>
    /// Converts the specified file to an indented and deobfuscated plaintext JSON file.
    /// The result will be right next to the specified input file.
    /// </summary>
    /// <param name="file"></param>
    public static void ToJson(string file) => ToJson(file, null, true, true);

    /// <summary>
    /// Converts the specified file to a plaintext JSON file according to the specified flags.
    /// The result will be right next to the specified input file.
    /// </summary>
    /// <param name="file"></param>
    /// <param name="path"></param>
    public static void ToJson(string file, bool indented, bool deobfuscated) => ToJson(file, null, indented, deobfuscated);

    /// <summary>
    /// Converts the specified file to an indented and deobfuscated plaintext JSON file.
    /// The result will be in the specified output path or next to the specified input file if the path is invalid.
    /// </summary>
    /// <param name="file"></param>
    /// <param name="path"></param>
    public static void ToJson(string file, string? path) => ToJson(file, path, true, true);

    /// <summary>
    /// Converts the specified file to a plaintext JSON file according to the specified flags.
    /// The result will be in the specified output path or next to the specified input file if the path is invalid.
    /// </summary>
    /// <param name="file"></param>
    /// <param name="path"></param>
    /// <param name="indented"></param>
    /// <param name="deobfuscated"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public static void ToJson(string file, string? path, bool indented, bool deobfuscated)
    {
        // Method contains all relevant checks...
        var container = PlatformCollection.AnalyzeFile(file);

        // ...so just throw an exception if container is null.
        if (container?.IsCompatible != true)
            ThrowHelper.ThrowInvalidOperationException("The specified file does not contain valid data.");

        ToJson(container, path, indented, deobfuscated);
    }

    /// <summary>
    /// Converts the specified container to an indented and deobfuscated plaintext JSON file.
    /// The result will be right next to the data file of the <see cref="Container"/>. If none is set, the current working directory is used.
    /// </summary>
    /// <param name="file"></param>
    public static void ToJson(Container container) => ToJson(container, null, true, true);

    /// <summary>
    /// Converts the specified container to a plaintext JSON file according to the specified flags.
    /// The result will be right next to the data file of the <see cref="Container"/>. If none is set, the current working directory is used.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="indented"></param>
    /// <param name="deobfuscated"></param>
    public static void ToJson(Container container, bool indented, bool deobfuscated) => ToJson(container, null, indented, deobfuscated);

    /// <summary>
    /// Converts the specified container to an indented and deobfuscated plaintext JSON file.
    /// The result will be in the specified output path. If that is invalid it will be right next to the data file of the <see cref="Container"/> or in the current working directory if no data file is set.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="path"></param>
    public static void ToJson(Container container, string? path) => ToJson(container, path, true, true);

    /// <summary>
    /// Converts the specified container to a plaintext JSON file according to the specified flags.
    /// The result will be in the specified output path. If that is invalid it will be right next to the data file of the <see cref="Container"/> or in the current working directory if no data file is set.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="path"></param>
    /// <param name="indented"></param>
    /// <param name="deobfuscated"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public static void ToJson(Container container, string? path, bool indented, bool deobfuscated)
    {
        if (string.IsNullOrWhiteSpace(path))
            path = container.DataFile?.Directory?.FullName ?? Directory.GetCurrentDirectory();

        var name = container.DataFile?.Name ?? Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? "libNOM.io";

        var result = container.GetJsonObject().GetString(indented, !deobfuscated); // throws InvalidOperationException if not loaded
        var file = Path.Combine(path, $"{name}.{DateTime.Now.ToString(Constants.FILE_TIMESTAMP_FORMAT)}.json");

        File.WriteAllText(file, result);
    }

    #endregion

    #region ToSave

    // public //

    /// <summary>
    /// Converts the specified file to a <see cref="Container"/> for the specified platform.
    /// </summary>
    /// <param name="file"></param>
    /// <param name="platform"></param>
    public static Container? ToSaveContainer(string? file, Platform platform) => GetContainer(file, platform);

    /// <summary>
    /// Converts an input file to a save of the specified platform.
    /// The result will be right next to the specified input file.
    /// </summary>
    /// <param name="file"></param>
    /// <param name="targetPlatform"></param>
    public static void ToSaveFile(string file, PlatformEnum targetPlatform) => ToSaveFile(file, targetPlatform, null);

    /// <summary>
    /// Converts an input file to a save of the specified platform.
    /// The result will be in the specified output path or next to the specified input file if the path is invalid.
    /// </summary>
    /// <param name="file"></param>
    /// <param name="targetPlatform"></param>
    /// <param name="path"></param>
    /// <exception cref="InvalidDataException"></exception>
    public static void ToSaveFile(string file, PlatformEnum targetPlatform, string? path)
    {
        Platform platform = targetPlatform switch
        {
            PlatformEnum.Gog => new PlatformGog(),
            PlatformEnum.Microsoft => new PlatformMicrosoft(),
            PlatformEnum.Playstation => new PlatformPlaystation(),
            PlatformEnum.Steam => new PlatformSteam(),
            PlatformEnum.Switch => new PlatformSwitch(),
            _ => throw new InvalidDataException("The specified output platform is not yet supported."),
        };

        // Method contains all relevant checks so just throw an exception if container is null.
        var container = GetContainer(file, platform) ?? throw new InvalidDataException("Unable to read input file.");

        if (string.IsNullOrWhiteSpace(path))
            path = container.DataFile?.Directory?.FullName ?? Directory.GetCurrentDirectory();

        var name = $"{container.DataFile?.Name ?? ("libNOM.io")}.{platform}.{DateTime.Now.ToString(Constants.FILE_TIMESTAMP_FORMAT)}";

        // Set new files the converted content will be written to.
        container.DataFile = new FileInfo(Path.Combine(path, $"{name}.data"));
        container.MetaFile = new FileInfo(Path.Combine(path, $"{name}.meta"));
        container.Exists = true; // fake it be able to create the data
        container.IsSynced = true;
        container.Platform = platform; // to get the right sizes

        platform.JustWrite(container);
    }

    // private //

    /// <summary>
    /// Tries to get a valid container from the specified input file.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private static Container? GetContainer(string? input, Platform? platform)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        Container? container = null;
        try
        {
            container = PlatformCollection.AnalyzeFile(input!);
        }
        catch (Exception ex) when (ex is OverflowException) { } // use fallback below
        if (container is null && File.Exists(input))
        {
            container = new Container(-1, platform!) { DataFile = new(input) };
            try
            {
                // Try original save files first.
                ReadOnlySpan<byte> bytes = File.ReadAllBytes(input);
                container.SetJsonObject(bytes.GetJson());
            }
            catch (Exception ex) when (ex is ArgumentOutOfRangeException) { }
            try
            {
                // If it is a plaintext JSON file, the first try above fails.
                var text = File.ReadAllText(input);
                container.SetJsonObject(text.GetJson());
            }
            catch { }
            if (!container.IsLoaded) // no valid JSON in specified file
                return null;
        }
        return container;
    }

    #endregion
}
