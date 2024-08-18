using System.Globalization;
using System.IO.Compression;

using CommunityToolkit.Diagnostics;

using Newtonsoft.Json.Linq;

namespace libNOM.io;


// This partial class contains file operation related code, especially for backups.
public abstract partial class Platform : IPlatform, IEquatable<Platform>
{
    #region Initialize

    public IContainer? CreateBackupContainer(string file, int metaIndex)
    {
        var parts = Path.GetFileNameWithoutExtension(file).Split('.');

        // The filename of a backup needs to have the following format: "backup.{PlatformEnum}.{MetaIndex}.{CreatedAt}.{VersionEnum}" + ".zip"
        if (parts.Length < 5)
            return null;

        try
        {
            return new Container(metaIndex, this)
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

    public void CreateBackup(IContainer container)
    {
        var nonIContainer = container.ToContainer();

        // Remove first, to get rid of all backups in case MaxBackupCount was changed to zero.
        if (Settings.MaxBackupCount >= 0)
            RemoveOldBackups(nonIContainer);

        // No backups if set to zero (or negative).
        if (Settings.MaxBackupCount <= 0)
            return;

        // Does not make sense without the data file.
        Guard.IsNotNull(nonIContainer.DataFile);
        Guard.IsTrue(nonIContainer.DataFile.Exists);

        Directory.CreateDirectory(Settings.BackupDirectory); // ensure directory exists

        var createdAt = DateTime.Now;
        var name = $"backup.{PlatformEnum}.{nonIContainer.MetaIndex:D2}.{createdAt.ToString(Constants.FILE_TIMESTAMP_FORMAT)}.{(uint)(nonIContainer.GameVersion)}.zip".ToLowerInvariant();
        var path = Path.Combine(Settings.BackupDirectory, name);

        using (var zipArchive = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            nonIContainer.DataFile!.CreateZipArchiveEntry(zipArchive, "data");
            nonIContainer.MetaFile?.CreateZipArchiveEntry(zipArchive, "meta");
        }

        // Create new backup container.
        var backup = new Container(nonIContainer.MetaIndex, this)
        {
            DataFile = new(path),
            GameVersion = nonIContainer.GameVersion,
            IsBackup = true,
            LastWriteTime = createdAt,
        };
        nonIContainer.BackupCollection.Add(backup);

        nonIContainer.BackupCreatedCallback.Invoke(backup);
    }

    private void RemoveOldBackups(Container container)
    {
        // Remove the oldest backups above the maximum count.
        var outdated = container.BackupCollection.OrderByDescending(i => i.LastWriteTime).Skip(Settings.MaxBackupCount - 1);

        Delete(outdated); // delete before sending outdated into nirvana
        _ = outdated.All(container.BackupCollection.Remove); // remove all outdated from backup collection
    }

    public void RestoreBackup(IContainer backup) => RestoreBackup(backup, false);

    public void RestoreBackup(IContainer backup, bool write)
    {
        Guard.IsTrue(backup.Exists);
        Guard.IsTrue(backup.IsBackup);

        var nonIContainer = backup.ToContainer();

        if (!nonIContainer.IsLoaded)
            LoadBackupContainer(nonIContainer);

        if (!nonIContainer.IsCompatible)
            ThrowHelper.ThrowInvalidOperationException(nonIContainer.IncompatibilityException?.Message ?? nonIContainer.IncompatibilityTag ?? $"{nonIContainer} is incompatible.");

        var container = SaveContainerCollection.FirstOrDefault(i => i.CollectionIndex == nonIContainer.CollectionIndex);
        if (container is null)
            ThrowHelper.ThrowInvalidOperationException($"There is no {nameof(IContainer)} with index {nonIContainer.CollectionIndex} to restore to.");

        Rebuild(container, nonIContainer.GetJsonObject());
        if (write)
            Write(nonIContainer);
    }
}
