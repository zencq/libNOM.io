using CommunityToolkit.Diagnostics;

namespace libNOM.io;


public partial class PlatformPlaystation : Platform
{
    #region Copy

    protected override void Copy(IEnumerable<(Container Source, Container Destination)> operationData, bool write)
    {
        if (_usesSaveStreaming)
        {
            base.Copy(operationData, write);
            return;
        }

        foreach (var (Source, Destination) in operationData)
            if (!Source.Exists)
            {
                Delete(Destination, false);
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

                // Update bytes in platform extra as it is what will be written later.
                // Could also be done in CopyPlatformExtra but here we do not need to override another method.
                Destination.Extra = Destination.Extra with
                {
                    Bytes = CreateData(Destination).ToArray(),
                    LastWriteTime = Source.LastWriteTime ?? DateTimeOffset.Now,
                };
            }

        if (write)
            WriteMemoryDat();
    }

    #endregion

    #region Delete

    protected override void Delete(IEnumerable<Container> containers, bool write)
    {
        if (_usesSaveStreaming)
        {
            base.Delete(containers, write);
            return;
        }

        Guard.IsTrue(CanDelete);

        DisableWatcher();

        foreach (var container in containers)
        {
            container.Reset();
            container.IncompatibilityTag = Constants.INCOMPATIBILITY_006;

            // Set afterwards again to make sure it is set to false.
            container.Exists = false;
        }

        if (write)
            WriteMemoryDat();

        EnableWatcher();
    }

    #endregion

    #region Move

    protected override void Move(IEnumerable<(Container Source, Container Destination)> containerOperationData, bool write)
    {
        if (_usesSaveStreaming)
        {
            base.Move(containerOperationData, write);
            return;
        }

        Copy(containerOperationData, false);
        Delete(containerOperationData.Select(i => i.Source), false);

        if (write)
            WriteMemoryDat();
    }

    #endregion

    #region Swap

    protected override void Swap(IEnumerable<(Container Source, Container Destination)> containerOperationData, bool write)
    {
        if (_usesSaveStreaming)
        {
            base.Swap(containerOperationData, write);
            return;
        }

        // Make sure everything can be swapped.
        foreach (var (Source, Destination) in containerOperationData.Where(i => i.Source.Exists && i.Destination.Exists))
        {
            if (!Source.IsLoaded)
                BuildContainerFull(Source);

            if (!Destination.IsLoaded)
                BuildContainerFull(Destination);

            if (!Source.IsCompatible || !Destination.IsCompatible)
                ThrowHelper.ThrowInvalidOperationException($"Cannot swap as at least one container is not compatible: {Source.IncompatibilityTag} >> {Destination.IncompatibilityTag}");
        }

        foreach (var (Source, Destination) in containerOperationData)
        {
            if (Source.Exists)
            {
                // Source and Destination exists. Swap.
                if (Destination.Exists)
                {
                    // Keep a copy to be able to set Source correctly after Destination is done.
                    var copy = Common.DeepCopy(Destination);

                    // Write Source to Destination.
                    Destination.LastWriteTime = Source.LastWriteTime ?? DateTimeOffset.Now;
                    Destination.SaveVersion = Source.SaveVersion;
                    Destination.SetJsonObject(Source.GetJsonObject());
                    CopyPlatformExtra(Destination, Source);
                    RebuildContainerFull(Destination);

                    // Write Destination to Source.
                    Source.LastWriteTime = copy.LastWriteTime ?? DateTimeOffset.Now;
                    Source.SaveVersion = copy.SaveVersion;
                    Source.SetJsonObject(copy.GetJsonObject());
                    CopyPlatformExtra(Source, copy);
                    RebuildContainerFull(Source);
                }
                // Source exists only. Move to Destination.
                else
                    Move(Source, Destination, false);
            }
            // Destination exists only. Move to Source.
            else if (Destination.Exists)
                Move(Destination, Source, false);
        }

        UpdateUserIdentification();

        if (write)
            WriteMemoryDat();
    }

    #endregion

    #region Transfer

    protected override void Transfer(TransferData sourceTransferData, int destinationSlotIndex, bool write)
    {
        if (_usesSaveStreaming)
        {
            base.Transfer(sourceTransferData, destinationSlotIndex, write);
            return;
        }

        PrepareTransferDestination(destinationSlotIndex);

        if (!sourceTransferData.UserIdentification.IsComplete() || !PlatformUserIdentification.IsComplete())
            ThrowHelper.ThrowInvalidOperationException("Cannot transfer as at least one user identification is not complete.");

        foreach (var (Source, Destination) in sourceTransferData.Containers.Zip(SaveContainerCollection.Where(i => i.SlotIndex == destinationSlotIndex), (Source, Destination) => (Source, Destination)))
            if (!Source.Exists)
            {
                Delete(Destination, false);
            }
            else if (Destination.Exists || !Destination.Exists && CanCreate)
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

                // Update bytes in platform extra as it is what will be written later.
                // Could also be done in CopyPlatformExtra but here we do not need to override another method.
                Destination.Extra = Destination.Extra with
                {
                    Bytes = CreateData(Destination).ToArray(),
                    LastWriteTime = Source.LastWriteTime ?? DateTimeOffset.Now,
                };

                TransferOwnership(Destination, sourceTransferData);
            }

        if (write)
            WriteMemoryDat();
    }

    #endregion
}
