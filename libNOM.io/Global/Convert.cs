using System.Reflection;

using CommunityToolkit.Diagnostics;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace libNOM.io.Global;


public static class Convert
{
    #region ToJson

    /// <inheritdoc cref="ToJson(IContainer)"/>
    public static string ToJson(string input) => ToJson(new FileInfo(input), null, true, true);

    /// <inheritdoc cref="ToJson(IContainer)"/>
    public static string ToJson(FileInfo input) => ToJson(input, null, true, true);

    /// <summary>
    /// Converts the specified input to an indented and deobfuscated plaintext JSON file.
    /// The result will be returned.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static string ToJson(IContainer input) => ToJson(input, null, true, true);

    /// <inheritdoc cref="ToJson(IContainer, bool, bool)"/>
    public static string ToJson(string input, bool indented, bool deobfuscated) => ToJson(new FileInfo(input), null, indented, deobfuscated);

    /// <inheritdoc cref="ToJson(IContainer, bool, bool)"/>
    public static string ToJson(FileInfo input, bool indented, bool deobfuscated) => ToJson(input, null, indented, deobfuscated);

    /// <summary>
    /// Converts the specified input to a plaintext JSON file according to the specified flags.
    /// The result will be returned.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="indented"></param>
    /// <param name="deobfuscated"></param>
    public static string ToJson(IContainer input, bool indented, bool deobfuscated) => ToJson(input, null, indented, deobfuscated);

    /// <inheritdoc cref="ToJson(IContainer, string?)"/>
    public static string ToJson(string input, string? output) => ToJson(new FileInfo(input), output, true, true);

    /// <inheritdoc cref="ToJson(IContainer, string?)"/>
    public static string ToJson(FileInfo input, string? output) => ToJson(input, output, true, true);

    /// <summary>
    /// Converts the specified input to an indented and deobfuscated plaintext JSON file.
    /// The result will be in the specified output or returned if the output is invalid.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="output"></param>
    public static string ToJson(IContainer input, string? output) => ToJson(input, output, true, true);

    /// <inheritdoc cref="ToJson(IContainer, string?, bool, bool)"/>
    public static string ToJson(string input, string? output, bool indented, bool deobfuscated) => ToJson(new FileInfo(input), output, indented, deobfuscated);

    /// <inheritdoc cref="ToJson(IContainer, string?, bool, bool)"/>
    public static string ToJson(FileInfo input, string? output, bool indented, bool deobfuscated)
    {
        // Method contains all relevant checks...
        var container = Analyze.AnalyzeFile(input);

        // ...so just throw an exception if container is null.
        if (container?.IsCompatible != true)
            ThrowHelper.ThrowInvalidOperationException("The specified file does not contain valid data.");

        return ToJson(container, output, indented, deobfuscated);
    }

    /// <summary>
    /// Converts the specified input to a plaintext JSON file according to the specified flags.
    /// The result will be in the specified output or returned if the output is invalid.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="output"></param>
    /// <param name="indented"></param>
    /// <param name="deobfuscated"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public static string ToJson(IContainer input, string? output, bool indented, bool deobfuscated)
    {
        var result = input.GetJsonObject().GetString(indented, !deobfuscated, useAccount: input.IsAccount); // throws InvalidOperationException if not loaded

        if (!string.IsNullOrWhiteSpace(output))
        {
            var path1 = File.Exists(output) ? new FileInfo(output).Directory!.FullName : Directory.Exists(output) ? output : input.DataFile?.Directory?.FullName ?? Directory.GetCurrentDirectory(); // path where to write the new file

            var name = File.Exists(output) ? Path.GetFileName(output) : input.DataFile?.Name ?? Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? "libNOM.io"; // actual filename without timestamp and new extension
            var file = Path.Combine(path1, $"{name}.{DateTime.Now.ToString(Constants.FILE_TIMESTAMP_FORMAT)}.json"); // full path

            File.WriteAllText(file, result);
        }

        return result;
    }

    #endregion

    #region ToSaveContainer

    /// <inheritdoc cref="ToSaveContainer(FileInfo?, IPlatform)"/>
    public static IContainer? ToSaveContainer(string input, IPlatform platform) => GetContainer(new FileInfo(input), platform);

    /// <summary>
    /// Converts the specified input to a <see cref="Container"/> for the specified platform.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="platform"></param>
    public static IContainer? ToSaveContainer(FileInfo? input, IPlatform platform) => GetContainer(input, platform);

    #endregion

    #region ToSaveFile

    /// <summary>
    /// Converts the input to a save of the specified platform.
    /// The result will be right next to the specified input file.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="targetPlatform"></param>
    public static void ToSaveFile(FileInfo? input, PlatformEnum targetPlatform) => ToSaveFile(input, targetPlatform, null);

    /// <summary>
    /// Converts an input to a save of the specified platform.
    /// The result will be in the specified output path or next to the specified input file if the path is invalid or in the current directory if that fails as well.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="targetPlatform"></param>
    /// <param name="output"></param>
    /// <exception cref="InvalidDataException"></exception>
    // EXTERNAL RELEASE: If any, add the new platform here as well.
    public static void ToSaveFile(FileInfo? input, PlatformEnum targetPlatform, string? output)
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
        var container = GetContainer(input, platform) ?? throw new InvalidDataException("Unable to read input file.");

        if (string.IsNullOrWhiteSpace(output))
            output = container.DataFile?.Directory?.FullName ?? Directory.GetCurrentDirectory();

        var name = $"{container.DataFile?.Name ?? "libNOM.io"}.{platform}.{DateTime.Now.ToString(Constants.FILE_TIMESTAMP_FORMAT)}";

        // Set new files the converted content will be written to.
        container.DataFile = new FileInfo(Path.Combine(output, $"{name}.data"));
        container.MetaFile = new FileInfo(Path.Combine(output, $"{name}.meta"));

        container.Exists = true; // fake it be able to create the data
        container.Extra = container.Extra with { MetaLength = 0 }; // reset to get the length of the target platform

        platform.PrepareWrite(container);
    }

    /// <summary>
    /// Converts the input to a save of the specified platform.
    /// The result will be in the current directory using the default naming pattern of the specified platform/index.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="targetPlatform"></param>
    /// <param name="index"></param>
    public static void ToSaveFile(string input, PlatformEnum targetPlatform, int index) => ToSaveFile(input, targetPlatform, index,null);

    /// <summary>
    /// Converts an input to a save of the specified platform.
    /// The result will be in the specified output path and uses the default naming pattern of the specified platform/index.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="targetPlatform"></param>
    /// <param name="index"></param>
    /// <param name="output"></param>
    /// <exception cref="InvalidDataException"></exception>
    // EXTERNAL RELEASE: If any, add the new platform here as well.
    public static void ToSaveFile(string input, PlatformEnum targetPlatform, int index, string? output)
    {
        Platform platform = targetPlatform switch
        {
            PlatformEnum.Gog => new PlatformGog(output, new() { LoadingStrategy = LoadingStrategyEnum.Hollow }),
            PlatformEnum.Microsoft => new PlatformMicrosoft(output, new() { LoadingStrategy = LoadingStrategyEnum.Hollow }),
            PlatformEnum.Playstation => new PlatformPlaystation(output, new() { LoadingStrategy = LoadingStrategyEnum.Hollow }),
            PlatformEnum.Steam => new PlatformSteam(output, new() { LoadingStrategy = LoadingStrategyEnum.Hollow }),
            PlatformEnum.Switch => new PlatformSwitch(output, new() { LoadingStrategy = LoadingStrategyEnum.Hollow }),
            _ => throw new InvalidDataException("The specified output platform is not yet supported."),
        };

        // Method contains all relevant checks so just throw an exception if container is null.
        var container = platform.GetSaveContainer(index)?.ToContainer();
        var name = $"{container?.DataFile?.Name ?? "libNOM.io"}.{platform}.{DateTime.Now.ToString(Constants.FILE_TIMESTAMP_FORMAT)}";

        if (string.IsNullOrWhiteSpace(output))
            output = container?.DataFile?.Directory?.FullName ?? Directory.GetCurrentDirectory();

        if (container is null)
        {
            container = new Container(Constants.OFFSET_INDEX, platform) ?? throw new InvalidDataException("Unable to read input file.");

            // Set new files the converted content will be written to.
            container.DataFile = new FileInfo(Path.Combine(output, $"{name}.data"));
            container.MetaFile = new FileInfo(Path.Combine(output, $"{name}.meta"));
        }

        container.Exists = true; // fake it be able to create the data
        container.Extra = container.Extra with { MetaLength = 0 }; // reset to get the length of the target platform

        container.ClearIncompatibility();
        container.SetJsonObject(input.GetJson()); // set specified input as new JSON after faking and resetting other properties

        platform.PrepareWrite(container);
    }

    #endregion

    #region Helper

    /// <summary>
    /// Tries to get a valid container from the specified input file.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private static Container? GetContainer(FileInfo? input, IPlatform platform)
    {
        if (CreateContainer(input, platform) is Container container && container.Exists)
        {
            // Clear incompatibility to ensure that IsLoaded does not fail due to any.
            container.ClearIncompatibility();

            // Try original save files first.
            if (!container.IsLoaded)
                container.SetJsonObject(ReadAllBytes(input!.FullName)); // input is an existing file as container would be null otherwise

            // If it is a plaintext JSON file, the first try above fails.
            if (!container.IsLoaded)
                container.SetJsonObject(ReadAllText(input!.FullName));

            return container.IsLoaded ? container : null;
        }

        return null;
    }

    private static Container? CreateContainer(FileInfo? input, IPlatform platform)
    {
        Container? container = null;

        if (input?.Exists == true)
        {
            try
            {
                container = Analyze.AnalyzeFile(input, platform.Settings);
            }
            catch (Exception ex) when (ex is OverflowException) { } // use fallback below

            container ??= new Container(-1, platform) { DataFile = input };
        }

        return container;
    }

    private static JObject? ReadAllBytes(string input)
    {
        try
        {
            ReadOnlySpan<byte> bytes = File.ReadAllBytes(input);
            return bytes.GetJson(escapeHashedIds: true); // assume account is not used here 
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException or JsonReaderException)
        {
            return null;
        }
    }

    private static JObject? ReadAllText(string input)
    {
        string text = File.ReadAllText(input);
        return text.GetJson();
    }

    #endregion
}
