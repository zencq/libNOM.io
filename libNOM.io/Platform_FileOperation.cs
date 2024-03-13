using System.Globalization;
using System.IO.Compression;

using CommunityToolkit.Diagnostics;

using libNOM.io.Interfaces;

using Microsoft.Extensions.Caching.Memory;

using Newtonsoft.Json.Linq;

namespace libNOM.io;


/// <summary>
/// Abstract base for all platforms which just hook into the methods they need.
/// </summary>
public abstract partial class Platform : IPlatform, IEquatable<Platform>
{
    // // File Operation

    #region Backup

    public void Backup(Container container)
    {
        // Does not make sense without the data file.
        Guard.IsNotNull(container.DataFile);
        Guard.IsTrue(container.DataFile.Exists);

        var createdAt = DateTime.Now;
        var name = $"backup.{PlatformEnum}.{container.MetaIndex:D2}.{createdAt.ToString(Constants.FILE_TIMESTAMP_FORMAT)}.{(uint)(container.GameVersion)}.zip".ToLowerInvariant();
        var path = Path.Combine(Settings.Backup, name);

        Directory.CreateDirectory(Settings.Backup); // ensure directory exists
        using (var zipArchive = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            _ = zipArchive.CreateEntryFromFile(container.DataFile.FullName, "data");
            if (container.MetaFile?.Exists == true)
                _ = zipArchive.CreateEntryFromFile(container.MetaFile.FullName, "meta");
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
        if (outdated.Any())
        {
            Delete(outdated);
            foreach (var item in outdated)
                container.BackupCollection.Remove(item);
        }

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
        Rebuild(container!, backup.GetJsonObject());

        // Set IsSynced to false as ProcessContainerData set it to true but it is not compared to the state on disk.
        container!.IsSynced = false;
        container!.BackupRestoredCallback.Invoke();
    }

    #endregion

    #region Copy

    public void Copy(Container source, Container destination) => Copy([(Source: source, Destination: destination)], true);

    public void Copy(IEnumerable<(Container Source, Container Destination)> operationData) => Copy(operationData, true);

    protected virtual void Copy(IEnumerable<(Container Source, Container Destination)> operationData, bool write)
    {
        foreach (var (Source, Destination) in operationData)
            if (!Source.Exists)
            {
                Delete(Destination, write);
            }
            else if (Destination.Exists || (!Destination.Exists && CanCreate))
            {
                if (!Source.IsLoaded)
                    BuildContainerFull(Source);

                if (!Source.IsCompatible)
                    ThrowHelper.ThrowInvalidOperationException($"Cannot copy as the source container is not compatible: {Source.IncompatibilityTag}");

                Destination.SetJsonObject(Source.GetJsonObject());
                Destination.ClearIncompatibility();

                // Due to this CanCreate can be true.
                CopyPlatformExtra(Destination, Source);

                // Faking relevant properties to force it to Write().
                Destination.Exists = true;

                // Additional properties required to properly rebuild the container.
                Destination.GameVersion = Source.GameVersion;
                Destination.SaveVersion = Source.SaveVersion;

                // This "if" is not really useful in this method but properly implemented nonetheless.
                if (write)
                    Write(Destination, Source.LastWriteTime ?? DateTimeOffset.Now);
            }

        UpdateUserIdentification();
    }

    /// <summary>
    /// Copies the platform extra from the source container.
    /// </summary>
    /// <param name="destination"></param>
    /// <param name="source"></param>
    /// <returns></returns>
    protected virtual void CopyPlatformExtra(Container destination, Container source)
    {
        // Overwrite all general values but keep platform specific stuff unchanged.
        destination.Extra = destination.Extra with
        {
            MetaFormat = source.Extra.MetaFormat,
            Bytes = source.Extra.Bytes,
            Size = source.Extra.Size,
            SizeDecompressed = source.Extra.SizeDecompressed,
            SizeDisk = source.Extra.SizeDisk,
            LastWriteTime = source.Extra.LastWriteTime,
            BaseVersion = source.Extra.BaseVersion,
            GameMode = source.Extra.GameMode,
            Season = source.Extra.Season,
            TotalPlayTime = source.Extra.TotalPlayTime,
            SaveName = source.Extra.SaveName,
            SaveSummary = source.Extra.SaveSummary,
            DifficultyPreset = source.Extra.DifficultyPreset,
        };
    }

    #endregion

    #region Delete

    public void Delete(Container container) => Delete([container], true);

    protected void Delete(Container container, bool write) => Delete([container], write);

    public void Delete(IEnumerable<Container> containers) => Delete(containers, true);

    protected virtual void Delete(IEnumerable<Container> containers, bool write)
    {
        Guard.IsTrue(CanDelete);

        DisableWatcher();

        foreach (var container in containers)
        {
            if (write)
            {
                try
                {
                    container.DataFile?.Delete();
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { } // nothing to do
                try
                {
                    container.MetaFile?.Delete();
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { } // nothing to do
            }

            container.Reset();
            container.IncompatibilityTag = Constants.INCOMPATIBILITY_006;
        }

        EnableWatcher();
    }

    #endregion

    #region Move

    public void Move(Container source, Container destination) => Move([(Source: source, Destination: destination)], true);

    protected void Move(Container source, Container destination, bool write) => Move([(Source: source, Destination: destination)], write);

    public void Move(IEnumerable<(Container Source, Container Destination)> operationData) => Move(operationData, true);

    protected virtual void Move(IEnumerable<(Container Source, Container Destination)> operationData, bool write)
    {
        Copy(operationData, write);
        Delete(operationData.Select(i => i.Source), write);
    }

    #endregion

    #region Swap

    public void Swap(Container source, Container destination) => Swap([(Source: source, Destination: destination)], true);

    public void Swap(IEnumerable<(Container Source, Container Destination)> operationData) => Swap(operationData, true);

    protected virtual void Swap(IEnumerable<(Container Source, Container Destination)> operationData, bool write)
    {
        // Make sure everything can be swapped.
        foreach (var (Source, Destination) in operationData.Where(i => i.Source.Exists && i.Destination.Exists))
        {
            if (!Source.IsLoaded)
                BuildContainerFull(Source);

            if (!Destination.IsLoaded)
                BuildContainerFull(Destination);

            if (!Source.IsCompatible || !Destination.IsCompatible)
                ThrowHelper.ThrowInvalidOperationException($"Cannot swap as at least one container is not compatible: {Source.IncompatibilityTag} >> {Destination.IncompatibilityTag}");
        }

        foreach (var (Source, Destination) in operationData)
        {
            if (Source.Exists)
            {
                // Source and Destination exists. Swap.
                if (Destination.Exists)
                {
                    // Keep a copy to be able to set Source correctly after Destination is done.
                    var copy = Common.DeepCopy(Destination);

                    // Write Source to Destination.
                    Destination.SaveVersion = Source.SaveVersion;
                    Destination.SetJsonObject(Source.GetJsonObject());
                    CopyPlatformExtra(Destination, Source);
                    Write(Destination, Source.LastWriteTime ?? DateTimeOffset.Now);
                    RebuildContainerFull(Destination);

                    // Write Destination to Source.
                    Source.SaveVersion = copy.SaveVersion;
                    Source.SetJsonObject(copy.GetJsonObject());
                    CopyPlatformExtra(Source, copy);
                    Write(Source, copy.LastWriteTime ?? DateTimeOffset.Now);
                    RebuildContainerFull(Source);
                }
                // Source exists only. Move to Destination.
                else
                    Move(Source, Destination, write);
            }
            // Destination exists only. Move to Source.
            else if (Destination.Exists)
                Move(Destination, Source, write);
        }

        UpdateUserIdentification();
    }

    #endregion

    #region Transfer

    public TransferData GetSourceTransferData(int sourceSlotIndex)
    {
        PrepareUserIdentification();

        var sourceTransferData = new TransferData(SaveContainerCollection.Where(i => i.SlotIndex == sourceSlotIndex), true, [], true, true, true, PlatformUserIdentification);

        foreach (var container in sourceTransferData.Containers)
        {
            if (!container.Exists)
                continue;

            if (!container.IsLoaded)
                BuildContainerFull(container);

            var jsonObject = container.GetJsonObject();

            var expressions = new[]
            {
                Json.GetPath("INTERSECTION_PERSISTENT_PLAYER_BASE_OWNERSHIP_EXPRESSION_TYPE_OR_TYPE", jsonObject),
                Json.GetPath("INTERSECTION_PERSISTENT_PLAYER_BASE_OWNERSHIP_EXPRESSION_THIS_UID", jsonObject, PlatformUserIdentification.UID),
            };

            foreach (var context in GetContexts(jsonObject))
            {
                var path = Json.GetPath("INTERSECTION_PERSISTENT_PLAYER_BASE_FOR_TRANSFER", jsonObject, context);
                foreach (var persistentPlayerBase in jsonObject.SelectTokensWithIntersection<JObject>(expressions.Select(i => string.Format(path, i))))
                {
                    var name = persistentPlayerBase.GetValue<string>("RELATIVE_BASE_NAME");
                    if (string.IsNullOrEmpty(name))
                        name = EnumExtensions.Parse<PersistentBaseTypesEnum>(persistentPlayerBase.GetValue<string>("RELATIVE_BASE_TYPE")) switch
                        {
                            PersistentBaseTypesEnum.FreighterBase => "Freighter Base",
                            PersistentBaseTypesEnum.HomePlanetBase => "Unnamed Planet Base",
                            _ => "Unnamed Base",
                        };

                    sourceTransferData.TransferBaseUserDecision[GetBaseIdentifier(persistentPlayerBase)] = new(context, name!, true);
                }
            }
        }

        UpdateUserIdentification();

        return sourceTransferData with { UserIdentification = PlatformUserIdentification };
    }

    private void PrepareUserIdentification()
    {
        // If user identification is not complete, load saves until it is.
        foreach (var container in SaveContainerCollection.Where(i => i.Exists && !i.IsLoaded))
        {
            // Faking while-loop by checking first.
            if (PlatformUserIdentification.IsComplete())
                break;

            BuildContainerFull(container);
        }
    }

    /// <summary>
    /// Creates an unique identifier for bases based on its location.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <returns></returns>
    private static string GetBaseIdentifier(JObject jsonObject)
    {
#if NETSTANDARD2_0
        var galacticAddress = jsonObject.GetValue<string>("RELATIVE_BASE_GALACTIC_ADDRESS")!;
        var galacticInteger = galacticAddress.StartsWith("0x") ? long.Parse(galacticAddress.Substring(2), NumberStyles.HexNumber) : long.Parse(galacticAddress);
#else
        ReadOnlySpan<char> galacticAddress = jsonObject.GetValue<string>("RELATIVE_BASE_GALACTIC_ADDRESS");
        var galacticInteger = galacticAddress.StartsWith("0x") ? long.Parse(galacticAddress[2..], NumberStyles.HexNumber) : long.Parse(galacticAddress);
#endif

        var positionX = jsonObject.GetValue<int>("RELATIVE_BASE_POSITION_0");
        var positionY = jsonObject.GetValue<int>("RELATIVE_BASE_POSITION_1");
        var positionZ = jsonObject.GetValue<int>("RELATIVE_BASE_POSITION_2");

        return $"{galacticInteger}{positionX:+000000;-000000}{positionY:+000000;-000000}{positionZ:+000000;-000000}";
    }

    /// <summary>
    /// Ensures that the destination is prepared for the incoming <see cref="Transfer(TransferData, int)"/>.
    /// Mainly to lookup the user identification.
    /// </summary>
    /// <param name="destinationSlotIndex"></param>
    protected void PrepareTransferDestination(int destinationSlotIndex)
    {
        // Load destination as they are needed anyway.
        foreach (var container in SaveContainerCollection.Where(i => i.SlotIndex == destinationSlotIndex))
            if (container.Exists && !container.IsLoaded)
                BuildContainerFull(container);

        PrepareUserIdentification();
    }

    public void Transfer(TransferData sourceTransferData, int destinationSlotIndex) => Transfer(sourceTransferData, destinationSlotIndex, true);

    /// <inheritdoc cref="Transfer(TransferData, int)"/>
    /// <param name="write"></param>
    /// <exception cref="InvalidOperationException"></exception>
    protected virtual void Transfer(TransferData sourceTransferData, int destinationSlotIndex, bool write)
    {
        PrepareTransferDestination(destinationSlotIndex);

        if (!sourceTransferData.UserIdentification.IsComplete() || !PlatformUserIdentification.IsComplete())
            ThrowHelper.ThrowInvalidOperationException("Cannot transfer as at least one user identification is not complete.");

        foreach (var (Source, Destination) in sourceTransferData.Containers.Zip(SaveContainerCollection.Where(i => i.SlotIndex == destinationSlotIndex), (Source, Destination) => (Source, Destination)))
            if (!Source.Exists)
            {
                Delete(Destination, write);
            }
            else if (Destination.Exists || (!Destination.Exists && CanCreate))
            {
                if (!Source.IsCompatible)
                    ThrowHelper.ThrowInvalidOperationException($"Cannot copy as the source container is not compatible: {Source.IncompatibilityTag}");

                // Needs to be set first to use the correct obfuscation sate.
                Destination.Platform = this;

                Destination.SetJsonObject(Source.GetJsonObject());
                Destination.ClearIncompatibility();

                // Due to this CanCreate can be true.
                CreatePlatformExtra(Destination, Source);

                // Faking relevant properties to force it to Write().
                Destination.Exists = true;

                // Additional properties required to properly rebuild the container.
                Destination.GameVersion = Source.GameVersion;
                Destination.SaveVersion = Source.SaveVersion;
                Destination.UserIdentification = PlatformUserIdentification; // update to match new platform

                TransferOwnership(Destination, sourceTransferData);

                // This "if" is not really useful in this method but properly implemented nonetheless.
                if (write)
                    Write(Destination, Source.LastWriteTime ?? DateTimeOffset.Now);
            }
    }

    /// <summary>
    /// Creates the platform extra for the destination container.
    /// </summary>
    /// <param name="destination"></param>
    /// <param name="source"></param>
    /// <returns></returns>
    protected virtual void CreatePlatformExtra(Container destination, Container source)
    {
        CopyPlatformExtra(destination, source);

        // Reset bytes as from another platform it would not be right.
        destination.Extra = destination.Extra with
        {
            Bytes = null,
        };
    }

    /// <summary>
    /// Transfers ownerships to new container according to the prepared data.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="sourceTransferData"></param>
    protected void TransferOwnership(Container container, TransferData sourceTransferData)
    {
        var jsonObject = container.GetJsonObject();

        // Change token for Platform.
        jsonObject.SetValue(PlatformArchitecture, "PLATFORM");

        if (sourceTransferData.TransferDiscovery) // 1.0
            TransferGeneralOwnership(jsonObject, sourceTransferData, SaveContextQueryEnum.DontCare, "TRANSFER_UID_DISCOVERY");

        if (container.IsVersion(GameVersionEnum.Foundation) && sourceTransferData.TransferBase) // 1.1
            foreach (var context in GetContexts(jsonObject))
                TransferBaseOwnership(jsonObject, sourceTransferData, context);

        if (container.IsVersion351PrismsWithByteBeatAuthor && sourceTransferData.TransferByteBeat) // 3.51
            TransferByteBeatOwnership(jsonObject, sourceTransferData);

        if (container.IsVersion360Frontiers && sourceTransferData.TransferSettlement) // 3.6
            foreach (var context in GetContexts(jsonObject))
                TransferGeneralOwnership(jsonObject, sourceTransferData, context, "TRANSFER_UID_SETTLEMENT");
    }

    /// <summary>
    /// Generic method that transfers ownerships according to the specified path.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <param name="sourceTransferData"></param>
    /// <param name="context"></param>
    /// <param name="pathIdentifier"></param>
    protected void TransferGeneralOwnership(JObject jsonObject, TransferData sourceTransferData, SaveContextQueryEnum context, string pathIdentifier)
    {
        var path = Json.GetPath(pathIdentifier, jsonObject, context, sourceTransferData.UserIdentification.UID);
        foreach (var ownership in jsonObject.SelectTokens(path).Cast<JObject>())
            TransferGeneralOwnership(ownership);
    }

    /// <summary>
    /// Transfers ownerships in the specified JSON token.
    /// </summary>
    /// <param name="jsonObject"></param>
    protected void TransferGeneralOwnership(JObject jsonObject)
    {
        // Only UID is guaranteed.
        jsonObject.SetValue(PlatformUserIdentification.UID, "RELATIVE_OWNER_UID");

        // Replace LID, PTK, and USN if it is not empty.
        jsonObject.SetValueIfNotNullOrEmpty(PlatformUserIdentification.LID, "RELATIVE_OWNER_LID");
        jsonObject.SetValueIfNotNullOrEmpty(PlatformUserIdentification.USN, "RELATIVE_OWNER_USN");
        jsonObject.SetValueIfNotNullOrEmpty(PlatformToken, "RELATIVE_OWNER_PTK");
    }

    /// <summary>
    /// Transfers ownerships of all selected bases.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <param name="sourceTransferData"></param>
    /// <param name="context"></param>
    protected void TransferBaseOwnership(JObject jsonObject, TransferData sourceTransferData, SaveContextQueryEnum context)
    {
        var path = Json.GetPath("TRANSFER_UID_BASE", jsonObject, context);
        foreach (var persistentPlayerBase in jsonObject.SelectTokens(path).Cast<JObject>())
            if (sourceTransferData.TransferBaseUserDecision.TryGetValue(GetBaseIdentifier(persistentPlayerBase), out var userDecision) && userDecision.DoTransfer)
                TransferGeneralOwnership(persistentPlayerBase.GetValue<JObject>("RELATIVE_BASE_OWNER")!);
    }

    /// <summary>
    /// Transfers ownerships of the ByteBeat library.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <param name="sourceTransferData"></param>
    protected void TransferByteBeatOwnership(JObject jsonObject, TransferData sourceTransferData)
    {
        var path = Json.GetPath("TRANSFER_UID_BYTEBEAT", jsonObject, sourceTransferData.UserIdentification.UID);
        foreach (var mySong in jsonObject.SelectTokens(path).Cast<JObject>())
        {
            mySong.SetValueIfNotNullOrEmpty(PlatformUserIdentification.UID, "RELATIVE_SONG_AUTHOR_ID");
            mySong.SetValueIfNotNullOrEmpty(PlatformUserIdentification.USN, "RELATIVE_SONG_AUTHOR_USERNAME");
            mySong.SetValueIfNotNullOrEmpty(PlatformToken, "RELATIVE_SONG_AUTHOR_PLATFORM");
        }
    }

    #endregion
}
