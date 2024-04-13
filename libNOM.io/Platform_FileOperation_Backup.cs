using System.Globalization;
using System.IO.Compression;

using CommunityToolkit.Diagnostics;

using libNOM.io.Interfaces;

using Newtonsoft.Json.Linq;

namespace libNOM.io;


// This partial class contains file operation related code, especially for backups.
public abstract partial class Platform : IPlatform, IEquatable<Platform>
{
    #region Initialize

    public Container? CreateBackupContainer(string file, int metaIndex)
    {
        var parts = Path.GetFileNameWithoutExtension(file).Split('.');

        // The filename of a backup needs to have the following format: "backup.{PlatformEnum}.{MetaIndex}.{CreatedAt}.{VersionEnum}" + ".zip"
        if (parts.Length < 5)
            return null;

        try
        {
            return new(metaIndex, this)
            {
                DataFile = new(file),
                GameVersion = (GameVersionEnum)(System.Convert.ToInt32(parts[4])),
                IsBackup = true,
                LastWriteTime = DateTimeOffset.ParseExact($"{parts[3]}", Constants.FILE_TIMESTAMP_FORMAT, CultureInfo.InvariantCulture),
            };
        }
        catch (FormatException)
        {
            return null;
        }
    }

    /// <summary>
    /// Generates a collection with all backups of the specified <see cref="Container"/> that matches the MetaIndex and this <see cref="Platform"/>.
    /// </summary>
    /// <param name="container"></param>
    protected void GenerateBackupCollection(Container container)
    {
        container.BackupCollection.Clear();

        // No directory, no backups.
        if (!Directory.Exists(Settings.BackupDirectory))
            return;

        foreach (var file in Directory.EnumerateFiles(Settings.BackupDirectory, $"backup.{PlatformEnum}.{container.MetaIndex:D2}.*.*.zip".ToLowerInvariant()))
        {
            var backup = CreateBackupContainer(file, container.MetaIndex);
            if (backup is null)
                continue;

            container.BackupCollection.Add(backup);
        }
    }

    #endregion

    #region Load

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
            {
                container.IncompatibilityTag = Constants.INCOMPATIBILITY_001;
            }
            else if (Deserialize(container, binary) is JObject jsonObject)
            {
                UpdateContainerWithJsonInformation(container, jsonObject);
            }
        }
    }

    #endregion

    public void Backup(Container container)
    {
        // Remove first, to get rid of all backups in case MaxBackupCount was changed to zero.
        if (Settings.MaxBackupCount >= 0)
            RemoveOldBackups(container);

        // No backups if set to zero (or negative).
        if (Settings.MaxBackupCount <= 0)
            return;

        // Does not make sense without the data file.
        Guard.IsNotNull(container.DataFile);
        Guard.IsTrue(container.DataFile.Exists);

        Directory.CreateDirectory(Settings.BackupDirectory); // ensure directory exists

        var createdAt = DateTime.Now;
        var name = $"backup.{PlatformEnum}.{container.MetaIndex:D2}.{createdAt.ToString(Constants.FILE_TIMESTAMP_FORMAT)}.{(uint)(container.GameVersion)}.zip".ToLowerInvariant();
        var path = Path.Combine(Settings.BackupDirectory, name);

        using (var zipArchive = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            container.DataFile!.CreateZipArchiveEntry(zipArchive, "data");
            container.MetaFile?.CreateZipArchiveEntry(zipArchive, "meta");
        }

        // Create new backup container.
        var backup = new Container(container.MetaIndex, this)
        {
            DataFile = new(path),
            GameVersion = container.GameVersion,
            IsBackup = true,
            LastWriteTime = createdAt,
        };
        container.BackupCollection.Add(backup);

        container.BackupCreatedCallback.Invoke(backup);
    }

    private void RemoveOldBackups(Container container)
    {
        // Remove the oldest backups above the maximum count.
        var outdated = container.BackupCollection.OrderByDescending(i => i.LastWriteTime).Skip(Settings.MaxBackupCount - 1);

        Delete(outdated); // delete before sending outdated into nirvana
        _ = outdated.All(container.BackupCollection.Remove); // remove all outdated from backup collection
    }

    public void Restore(Container backup)
    {
        // Does not make sense without it being an existing backup.
        Guard.IsTrue(backup.Exists);
        Guard.IsTrue(backup.IsBackup);

        if (!backup.IsLoaded)
            LoadBackupContainer(backup);

        if (!backup.IsCompatible)
            ThrowHelper.ThrowInvalidOperationException(backup.IncompatibilityException?.Message ?? backup.IncompatibilityTag ?? $"{backup} is incompatible.");

        var container = SaveContainerCollection.First(i => i.CollectionIndex == backup.CollectionIndex);
        UpdateContainerWithJsonInformation(container!, backup.GetJsonObject()); // rebuild to container with the new data

        // Set IsSynced to false as ProcessContainerData set it to true but it is not compared to the state on disk.
        container!.IsSynced = false;
        container!.BackupRestoredCallback.Invoke();
    }
}
