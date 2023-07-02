namespace libNOM.io;


public static class Convert
{
    #region ToJson

    /// <inheritdoc cref="ToJson(Container, string?, bool, bool)"/>
    public static void ToJson(string input)
    {
        ToJson(input, string.Empty, true, true);
    }

    /// <inheritdoc cref="ToJson(Container, string?, bool, bool)"/>
    public static void ToJson(string input, bool indent, bool deobfuscate)
    {
        ToJson(input, string.Empty, indent, deobfuscate);
    }

    /// <inheritdoc cref="ToJson(Container, string?, bool, bool)"/>
    public static void ToJson(string input, string? output)
    {
        ToJson(input, output, true, true);
    }

    /// <inheritdoc cref="ToJson(Container, string?, bool, bool)"/>
    public static void ToJson(string input, string? output, bool indent, bool deobfuscate)
    {
        // Method contains all relevant checks...
        var container = PlatformCollection.AnalyzeFile(input);

        // ...so just throw an exception if container is null.
        if (container is null)
            throw new InvalidOperationException("The specified file does not contain valid data.");

        ToJson(container, output, indent, deobfuscate);
    }

    /// <inheritdoc cref="ToJson(Container, string?, bool, bool)"/>
    public static void ToJson(Container container)
    {
        ToJson(container, string.Empty, true, true);
    }

    /// <inheritdoc cref="ToJson(Container, string?, bool, bool)"/>
    public static void ToJson(Container container, bool indent, bool deobfuscate)
    {
        ToJson(container, string.Empty, indent, deobfuscate);
    }

    /// <inheritdoc cref="ToJson(Container, string?, bool, bool)"/>
    public static void ToJson(Container container, string? output)
    {
        ToJson(container, output, true, true);
    }

    /// <summary>
    /// Saves a loaded <see cref="Container"/> to a plaintext JSON file according to the specified flags.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="output"></param>
    /// <param name="indent"></param>
    /// <param name="deobfuscate"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public static void ToJson(Container container, string? output, bool indent, bool deobfuscate)
    {
        if (!container.IsLoaded)
            throw new InvalidOperationException("The specified container is not loaded.");

        var result = container.GetJsonObject()?.GetString(indent, !deobfuscate);
        if (result is null)
            throw new InvalidOperationException("The specified container does not contain valid data.");

        output = string.IsNullOrWhiteSpace(output) ? container.DataFile!.Directory!.FullName : output;
        var path = Path.Combine(output, $"{container.DataFile!.Name}.{DateTime.Now.ToString(Global.FILE_TIMESTAMP_FORMAT)}.json");

        File.WriteAllText(path, result);
    }

    #endregion

    #region ToSave

    /// <summary>
    /// Converts an input file to a <see cref="Container"/>.
    /// </summary>
    /// <param name="input"></param>
    public static Container? ToSaveContainer(string? input)
    {
        return GetContainer(input);
    }

    /// <inheritdoc cref="ToSaveFile(string, string?, PlatformEnum)"/>
    public static void ToSaveFile(string input, PlatformEnum outputPlatform)
    {
        ToSaveFile(input, null, outputPlatform);
    }

    /// <summary>
    /// Converts an input file to a save of the specified platform.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="output"></param>
    /// <param name="outputPlatform"></param>
    /// <exception cref="InvalidDataException"></exception>
    public static void ToSaveFile(string input, string? output, PlatformEnum outputPlatform)
    {
        //Platform? platform = outputPlatform switch
        //{
        //    PlatformEnum.Gog => new PlatformGog(),
        //    PlatformEnum.Microsoft => new PlatformMicrosoft(),
        //    PlatformEnum.Playstation => new PlatformPlaystation(),
        //    PlatformEnum.Steam => new PlatformSteam(),
        //    PlatformEnum.Switch => new PlatformSwitch(),
        //    _ => null,
        //};
        //if (platform is null)
        //    throw new InvalidDataException("The specified output platform is not supported.");

        //// Method contains all relevant checks so just throw an exception if container is null.
        //var container = GetContainer(input);
        //if (container is null)
        //    throw new InvalidDataException("Unable to read input file.");

        //var name = $"{container.DataFile!.Name}.{outputPlatform}.{DateTime.Now.ToString(Global.FILE_TIMESTAMP_FORMAT)}";
        //output = string.IsNullOrWhiteSpace(output) ? container.DataFile.Directory!.FullName : output;

        //// Set new files the converted content will be written to.
        //container.DataFile = new FileInfo(Path.Combine(output, $"{name}.data"));
        //container.MetaFile = new FileInfo(Path.Combine(output, $"{name}.meta"));

        //platform.JustWrite(container);

        //container.RefreshFileInfo();
    }

    /// <summary>
    /// Tries to get a valid container from the specified input file.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private static Container? GetContainer(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        Container? container = null;
        try
        {
            container = PlatformCollection.AnalyzeFile(input);
        }
        catch 
        {
            // Nothing to do.
        }
        if (container is null)
        {
            //container = new Container(-1) { DataFile = new(input) };
            //container.SetJsonObject(Platform.ReadToByte(input).GetJson());
            //if (!container.IsLoaded)
            //    return null;
        }
        return container;
    }

    #endregion
}
