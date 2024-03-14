using System.IO.Compression;

using CommunityToolkit.Diagnostics;

using libNOM.io.Interfaces;

using Newtonsoft.Json.Linq;

namespace libNOM.io;


// This partial class contains file operation related code, especially for backups.
public abstract partial class Platform : IPlatform, IEquatable<Platform>
{
    public void Backup(Container container)
    {
        // Does not make sense without the data file.
        Guard.IsNotNull(container.DataFile);
        Guard.IsTrue(container.DataFile.Exists);

        Directory.CreateDirectory(Settings.Backup); // ensure directory exists

        var createdAt = DateTime.Now;
        var name = $"backup.{PlatformEnum}.{container.MetaIndex:D2}.{createdAt.ToString(Constants.FILE_TIMESTAMP_FORMAT)}.{(uint)(container.GameVersion)}.zip".ToLowerInvariant();
        var path = Path.Combine(Settings.Backup, name);

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

        // Remove the oldest backups above the maximum count.
        var outdated = container.BackupCollection.OrderByDescending(i => i.LastWriteTime).Skip(Settings.MaxBackupCount);
        _ = outdated.All(container.BackupCollection.Remove); // remove all outdated from backup collection
        Delete(outdated);

        container.BackupCreatedCallback.Invoke(backup);
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
        ProcessContainerData(container!, backup.GetJsonObject()); // rebuild to container with the new data

        // Set IsSynced to false as ProcessContainerData set it to true but it is not compared to the state on disk.
        container!.IsSynced = false;
        container!.BackupRestoredCallback.Invoke();
    }

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
            else if (DeserializeContainer(container, binary) is JObject jsonObject)
            {
                ProcessContainerData(container, jsonObject);
            }
        }
    }
}
