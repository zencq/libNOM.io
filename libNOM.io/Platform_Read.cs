using System.Collections.Concurrent;
using System.Globalization;
using System.IO.Compression;

using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;

using libNOM.io.Interfaces;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace libNOM.io;


/// <summary>
/// Abstract base for all platforms which just hook into the methods they need.
/// </summary>
public abstract partial class Platform : IPlatform, IEquatable<Platform>
{
    #region Generate

    /// <summary>
    /// Generates all related containers as well as the user identification.
    /// </summary>
    private void GeneratePlatformData()
    {
        if (Settings.LoadingStrategy == LoadingStrategyEnum.Empty)
            return;

        SaveContainerCollection.Clear();
        SaveContainerCollection.AddRange(GenerateContainerCollection());
        SaveContainerCollection.Sort();

        UpdateUserIdentification();
        EnableWatcher();
    }

    /// <summary>
    /// Generates a <see cref="Container"/> collection with an entry for each possible file.
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerable<Container> GenerateContainerCollection()
    {
        var bag = new ConcurrentBag<Container>();

        var tasks = Enumerable.Range(0, Constants.OFFSET_INDEX + COUNT_SAVES_TOTAL).Select((metaIndex) =>
        {
            return Task.Run(() =>
            {
                if (metaIndex == 0)
                {
                    AccountContainer = CreateContainer(metaIndex);
                    BuildContainerFull(AccountContainer); // always full
                }
                else if (metaIndex > 1) // skip index 1
                {
                    var container = CreateContainer(metaIndex);

                    if (Settings.LoadingStrategy < LoadingStrategyEnum.Full)
                        BuildContainerHollow(container);
                    else
                        BuildContainerFull(container);

                    GenerateBackupCollection(container);
                    bag.Add(container);
                }
            });
        });
        Task.WaitAll(tasks.ToArray());

        return bag;
    }

    /// <inheritdoc cref="CreateContainer(int, PlatformExtra?)"/>
    internal Container CreateContainer(int metaIndex) => CreateContainer(metaIndex, null);

    /// <summary>
    /// Creates a <see cref="Container"/> with basic data.
    /// </summary>
    /// <param name="metaIndex"></param>
    /// <param name="extra">An optional object with additional data necessary for proper creation.</param>
    /// <returns></returns>
    private protected abstract Container CreateContainer(int metaIndex, PlatformExtra? extra); // private protected as PlatformExtra is internal

    /// <summary>
    /// Builds a <see cref="Container"/> by loading from disk and processing it by deserializing the data.
    /// </summary>
    /// <param name="container"></param>
    protected void BuildContainerFull(Container container)
    {
        var binary = LoadContainer(container);

        if (container.IsCompatible && DeserializeContainer(container, binary) is JObject jsonObject)
            ProcessContainerData(container, jsonObject);
    }

    /// <summary>
    /// Builds a <see cref="Container"/> by loading from disk and processing it by extracting from the string representation.
    /// </summary>
    /// <param name="container"></param>
    protected void BuildContainerHollow(Container container)
    {
        var binary = LoadContainer(container);

        if (container.IsCompatible)
            ProcessContainerData(container, binary.GetString(), false);
    }

    /// <summary>
    /// Generates a collection with all backups of the specified <see cref="Container"/> that matches the MetaIndex and this <see cref="Platform"/>.
    /// </summary>
    /// <param name="container"></param>
    protected void GenerateBackupCollection(Container container)
    {
        container.BackupCollection.Clear();

        // No directory, no backups.
        if (!Directory.Exists(Settings.Backup))
            return;

        foreach (var file in Directory.EnumerateFiles(Settings.Backup, $"backup.{PlatformEnum}.{container.MetaIndex:D2}.*.*.zip".ToLowerInvariant()))
        {
            var parts = Path.GetFileNameWithoutExtension(file).Split('.');

            // The filename of a backup needs to have the following format: "backup.{PlatformEnum}.{MetaIndex}.{CreatedAt}.{VersionEnum}" + ".zip"
            if (parts.Length < 5)
                continue;

            try
            {
                container.BackupCollection.Add(new(container.MetaIndex, this)
                {
                    DataFile = new(file),
                    GameVersion = (GameVersionEnum)(System.Convert.ToInt32(parts[4])),
                    IsBackup = true,
                    LastWriteTime = DateTimeOffset.ParseExact($"{parts[3]}", Constants.FILE_TIMESTAMP_FORMAT, CultureInfo.InvariantCulture),
                });
            }
            catch (FormatException) { } // ignore
        }
    }

    #endregion

    #region Load

    public void Load(Container container)
    {
        if (container.IsBackup)
            LoadBackupContainer(container);
        else
            LoadSaveContainer(container);
    }

    /// <summary>
    /// Loads data of the specified backup.
    /// </summary>
    /// <param name="container"></param>
    /// <exception cref="ArgumentException"/>
    private void LoadBackupContainer(Container container)
    {
        Guard.IsTrue(container.Exists);
        Guard.IsTrue(container.IsBackup);

        // Load
        container.ClearIncompatibility();

        using var zipArchive = ZipFile.Open(container.DataFile!.FullName, ZipArchiveMode.Read);
        if (zipArchive.ReadEntry("data", out var data))
        {
            _ = zipArchive.ReadEntry("meta", out var meta);

            // Loads all meta information into the extra property.
            LoadMeta(container, meta);

            var binary = LoadData(container, data);
            if (binary.IsEmpty())
                container.IncompatibilityTag = Constants.INCOMPATIBILITY_001;
            else if (DeserializeContainer(container, binary) is JObject jsonObject)
                ProcessContainerData(container, jsonObject);
        }
    }

    /// <summary>
    /// Loads data of the specified save.
    /// </summary>
    /// <param name="container"></param>
    private void LoadSaveContainer(Container container)
    {
        if (Settings.LoadingStrategy < LoadingStrategyEnum.Current)
            Settings = Settings with { LoadingStrategy = LoadingStrategyEnum.Current };

        if (Settings.LoadingStrategy == LoadingStrategyEnum.Current && container.IsSave)
        {
            // Unloads data by removing the reference to the JSON object.
            var loadedContainers = SaveContainerCollection.Where(i => i.IsLoaded && !i.Equals(container));
            foreach (var loadedContainer in loadedContainers)
                loadedContainer.SetJsonObject(null);
        }

        BuildContainerFull(container);
    }

    /// <summary>
    /// Loads the save data of a <see cref="Container"/> into a processable format using meta data.
    /// </summary>
    /// <param name="container"></param>
    /// <returns></returns>
    protected virtual ReadOnlySpan<byte> LoadContainer(Container container)
    {
        // Any incompatibility will be set again while loading.
        container.ClearIncompatibility();

        if (container.Exists)
        {
            // Loads all meta information into the extra property.
            LoadMeta(container);

            var data = LoadData(container);
            if (data.IsEmpty())
                container.IncompatibilityTag = Constants.INCOMPATIBILITY_001;
            else
                return data;
        }

        container.IncompatibilityTag ??= Constants.INCOMPATIBILITY_006;
        return [];
    }

    public void Reload(Container container)
    {
        if (container.IsLoaded)
            RebuildContainerFull(container);
        else
            RebuildContainerHollow(container);
    }

    public void Rebuild(Container container, JObject jsonObject) => ProcessContainerData(container, jsonObject);

    /// <summary>
    /// Rebuilds a <see cref="Container"/> by loading from disk and processing it by deserializing the data.
    /// </summary>
    /// <param name="container"></param>
    protected void RebuildContainerFull(Container container) => BuildContainerFull(container);

    /// <summary>
    /// Rebuilds a <see cref="Container"/> by loading from disk and processing it by extracting from the string representation.
    /// </summary>
    /// <param name="container"></param>
    protected void RebuildContainerHollow(Container container)
    {
        var binary = LoadContainer(container);

        if (container.IsCompatible)
            ProcessContainerData(container, binary.GetString(), true); // force
    }

    /// <inheritdoc cref="LoadMeta(Container, Span{byte})"/>
    protected void LoadMeta(Container container)
    {
        // 1. Read
        LoadMeta(container, ReadMeta(container));
    }

    /// <summary>
    /// Loads the meta file into a processable format including reading, decrypting, and decompressing.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="read">Already read content of the meta file.</param>
    /// <returns></returns>
    protected void LoadMeta(Container container, Span<byte> read)
    {
        // 2. Decrypt
        // 3. Decompress
        var result = read.IsEmpty() ? [] : DecompressMeta(container, DecryptMeta(container, read));
        // 4. Update Container Information
        UpdateContainerWithMetaInformation(container, result.AsBytes(), result);
    }

    /// <summary>
    /// Reads the content of the meta file.
    /// </summary>
    /// <param name="container"></param>
    /// <returns></returns>
    protected virtual Span<byte> ReadMeta(Container container)
    {
        return container.MetaFile?.ReadAllBytes() ?? [];
    }

    /// <summary>
    /// Decrypts the read content of the meta file.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="meta"></param>
    /// <returns></returns>
    protected virtual Span<uint> DecryptMeta(Container container, Span<byte> meta)
    {
        return meta.Cast<byte, uint>();
    }

    /// <summary>
    /// Decompresses the read and decrypted content of the meta file.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="meta"></param>
    /// <returns></returns>
    protected virtual ReadOnlySpan<uint> DecompressMeta(Container container, ReadOnlySpan<uint> meta)
    {
        return meta;
    }

    /// <summary>
    /// Updates the specified container with information from the meta file.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="disk"></param>
    /// <param name="decompressed"></param>
    protected abstract void UpdateContainerWithMetaInformation(Container container, ReadOnlySpan<byte> disk, ReadOnlySpan<uint> decompressed);

    /// <inheritdoc cref="LoadData(Container, ReadOnlySpan{byte})"/>
    protected virtual ReadOnlySpan<byte> LoadData(Container container)
    {
        // 1. Read
        return LoadData(container, ReadData(container));
    }

    /// <summary>
    /// Loads the data file into a processable format including reading, decrypting, and decompressing.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="read"></param>
    /// <returns></returns>
    protected ReadOnlySpan<byte> LoadData(Container container, ReadOnlySpan<byte> read)
    {
        if (read.IsEmpty())
            return read;

        // 2. Decrypt
        // 3. Decompress
        var result = DecompressData(container, DecryptData(container, read));
        // 4. Update Container Information
        UpdateContainerWithDataInformation(container, read, result);

        return result;
    }

    /// <summary>
    /// Reads the content of the data file.
    /// </summary>
    /// <param name="container"></param>
    /// <returns></returns>
    protected virtual ReadOnlySpan<byte> ReadData(Container container)
    {
        return container.DataFile?.ReadAllBytes() ?? [];
    }

    /// <summary>
    /// Decrypts the read content of the data file.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    protected virtual ReadOnlySpan<byte> DecryptData(Container container, ReadOnlySpan<byte> data)
    {
        return data;
    }

    /// <summary>
    /// Decompresses the read and decrypted content of the data file.
    /// </summary>
    /// <param name="container"></param
    /// <param name="data"></param>
    /// <returns></returns>
    protected virtual ReadOnlySpan<byte> DecompressData(Container container, ReadOnlySpan<byte> data)
    {
        if (container.IsAccount || data.Cast<uint>(0) != Constants.SAVE_STREAMING_HEADER) // no compression before Frontiers
            return data;

        var offset = 0;
        ReadOnlySpan<byte> result = [];

        while (offset < data.Length)
        {
            var chunkHeader = data.Slice(offset, Constants.SAVE_STREAMING_HEADER_TOTAL_LENGTH).Cast<byte, uint>();
            var sizeCompressed = (int)(chunkHeader[1]);

            offset += Constants.SAVE_STREAMING_HEADER_TOTAL_LENGTH;
            _ = LZ4.Decode(data.Slice(offset, sizeCompressed), out var target, (int)(chunkHeader[2]));
            offset += sizeCompressed;

            result = result.Concat(target);
        }

        return result;
    }

    /// <summary>
    /// Updates the specified container with information from the data file.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="disk"></param>
    /// <param name="decompressed"></param>
    protected virtual void UpdateContainerWithDataInformation(Container container, ReadOnlySpan<byte> disk, ReadOnlySpan<byte> decompressed)
    {
        container.Extra = container.Extra with
        {
            SizeDecompressed = (uint)(decompressed.Length),
            SizeDisk = (uint)(disk.Length),
        };
    }

    /// <summary>
    /// Deserializes the read data of a <see cref="Container"/> into a JSON object.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="binary"></param>
    /// <returns></returns>
    protected virtual JObject? DeserializeContainer(Container container, ReadOnlySpan<byte> binary)
    {
        JObject? jsonObject;
        try
        {
            jsonObject = binary.GetJson();
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException or JsonReaderException or JsonSerializationException)
        {
            container.IncompatibilityException = ex;
            container.IncompatibilityTag = Constants.INCOMPATIBILITY_002;
            return null;
        }
        if (jsonObject is null)
        {
            container.IncompatibilityTag = Constants.INCOMPATIBILITY_003;
            return null;
        }
        return jsonObject;
    }

    #endregion

    #region Process

    /// <summary>
    /// Processes the read JSON object and fills the properties of the container.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="jsonObject"></param>
    /// <param name="force"></param>
    private void ProcessContainerData(Container container, JObject jsonObject)
    {
        // Setting the JSON will enable all properties.
        container.SetJsonObject(jsonObject);

        // No need to do these things for AccountData.
        if (container.IsSave)
        {
            container.UserIdentification = GetUserIdentification(jsonObject);
            UpdateUserIdentification();
        }

        // If we are in here, the container is in sync (again).
        container.IsSynced = true;
    }

    /// <inheritdoc cref="ProcessContainerData(Container, JObject, bool)"/>
    private static void ProcessContainerData(Container container, string json, bool force)
    {
        // Values relevant for AccountData first.
        if (container.SaveVersion <= 0 || force)
            container.SaveVersion = Meta.SaveVersion.Get(json);

        // Then all independent values.
        if (container.Extra.GameMode == 0 || force)
            container.GameMode = Meta.GameMode.Get(json);

        if (container.TotalPlayTime == 0 || force)
            container.TotalPlayTime = Meta.TotalPlayTime.Get(json);

        // Finally all remaining values that depend on others.
        if (container.GameMode == PresetGameModeEnum.Seasonal && container.Season == SeasonEnum.None || force)
            container.Season = Meta.Season.Get(json); // needs GameMode

        if (container.BaseVersion <= 0 || force)
            container.BaseVersion = Meta.BaseVersion.Calculate(container); // needs SaveVersion and GameMode and Season

        if (container.GameVersion == GameVersionEnum.Unknown)
            container.GameVersion = Meta.GameVersion.Get(container.BaseVersion, json); // needs BaseVersion

        if (container.Difficulty == DifficultyPresetTypeEnum.Invalid || force)
            container.Difficulty = Meta.DifficultyPreset.Get(container, json); // needs GameMode and GameVersion

        if (container.IsVersion400Waypoint) // needs GameVersion
        {
            if (string.IsNullOrEmpty(container.SaveName) || force)
                container.SaveName = Meta.SaveName.Get(json);

            if (string.IsNullOrEmpty(container.SaveSummary) || force)
                container.SaveSummary = Meta.SaveSummary.Get(json);
        }
    }

    #endregion
}
