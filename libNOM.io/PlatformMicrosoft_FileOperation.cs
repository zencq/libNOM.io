using CommunityToolkit.Diagnostics;

namespace libNOM.io;


// This partial class contains file operation related code.
public partial class PlatformMicrosoft : Platform
{
    #region PlatformExtra

    protected override void CreatePlatformExtra(Container destination, Container source)
    {
        base.CreatePlatformExtra(destination, source);

        // Always creating dummy blob data (already created in CopyPlatformExtra() if destination does not exist).
        if (destination.Exists)
            ExecuteCanCreate(destination);
    }

    protected override void CopyPlatformExtra(Container destination, Container source)
    {
        base.CopyPlatformExtra(destination, source);

        // Creating dummy blob data only necessary if destination does not exist.
        if (!destination.Exists)
            ExecuteCanCreate(destination);
    }

    private void ExecuteCanCreate(Container Destination)
    {
        var directoryGuid = Guid.NewGuid();
        var directory = new DirectoryInfo(Path.Combine(Location!.FullName, directoryGuid.ToPath()));

        // Update container and its extra with dummy data.
        Destination.Extra = Destination.Extra with
        {
            MicrosoftSyncTime = string.Empty,
            MicrosoftBlobContainerExtension = 0,
            MicrosoftSyncState = MicrosoftBlobSyncStateEnum.Created,
            MicrosoftBlobDirectoryGuid = directoryGuid,
            MicrosoftBlobDataFile = Destination.DataFile = new(Path.Combine(directory.FullName, "data.guid")),
            MicrosoftBlobMetaFile = Destination.MetaFile = new(Path.Combine(directory.FullName, "meta.guid")),

            MicrosoftBlobDirectory = directory,
        };

        // Prepare blob container file content. Guid of data and meta file will be set while executing Write().
        var buffer = new byte[BLOBCONTAINER_TOTAL_LENGTH];
        using (var writer = new BinaryWriter(new MemoryStream(buffer)))
        {
            writer.Write(BLOBCONTAINER_HEADER);
            writer.Write(BLOBCONTAINER_COUNT);

            writer.Write("data".GetUnicodeBytes());
            writer.Seek(BLOBCONTAINER_IDENTIFIER_LENGTH - 8 + 32, SeekOrigin.Current);

            writer.Write("meta".GetUnicodeBytes());
        }

        // Write a dummy file.
        Directory.CreateDirectory(Destination.Extra.MicrosoftBlobDirectory!.FullName);
        File.WriteAllBytes(Destination.Extra.MicrosoftBlobContainerFile!.FullName, buffer);
    }

    #endregion

    #region Delete

    protected override void Delete(IEnumerable<Container> containers, bool write)
    {
        Guard.IsTrue(CanDelete);

        DisableWatcher();

        foreach (var container in containers)
        {
            if (write)
            {
                if (container.Extra.MicrosoftBlobDirectory?.Exists == true)
                    container.Extra.MicrosoftBlobDirectory!.Delete();
            }

            container.Reset();

            container.DataFile = container.MetaFile = null; // set to null as it constantly changes anyway
            container.Extra = container.Extra with { MicrosoftSyncState = MicrosoftBlobSyncStateEnum.Deleted };
            container.IncompatibilityTag = Constants.INCOMPATIBILITY_004;
        }

        if (Settings.SetLastWriteTime)
            _lastWriteTime = DateTimeOffset.Now.LocalDateTime; // global timestamp has full accuracy

        if (write)
            WriteContainersIndex();

        EnableWatcher();
    }

    #endregion
}
