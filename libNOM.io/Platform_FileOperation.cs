using CommunityToolkit.Diagnostics;

using libNOM.io.Interfaces;

namespace libNOM.io;


// This partial class contains file operation related code.
public abstract partial class Platform : IPlatform, IEquatable<Platform>
{
    #region PlatformExtra

    /// <summary>
    /// Creates the platform extra for the destination container.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    protected virtual void CreatePlatformExtra(Container container, Container other)
    {
        CopyPlatformExtra(container, other);

        // Reset bytes as from another platform it would not be right.
        container.Extra = container.Extra with
        {
            Bytes = null,
        };
    }

    /// <summary>
    /// Copies the platform extra from the source container.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    protected virtual void CopyPlatformExtra(Container container, Container other)
    {
        // Overwrite all general values but keep platform specific stuff unchanged.
        container.Extra = container.Extra with
        {
            MetaFormat = other.Extra.MetaFormat,
            Bytes = other.Extra.Bytes,
            Size = other.Extra.Size,
            SizeDecompressed = other.Extra.SizeDecompressed,
            SizeDisk = other.Extra.SizeDisk,
            LastWriteTime = other.Extra.LastWriteTime,
            BaseVersion = other.Extra.BaseVersion,
            GameMode = other.Extra.GameMode,
            Season = other.Extra.Season,
            TotalPlayTime = other.Extra.TotalPlayTime,
            SaveName = other.Extra.SaveName,
            SaveSummary = other.Extra.SaveSummary,
            DifficultyPreset = other.Extra.DifficultyPreset,
        };
    }

    #endregion

    private void EnsureIsLoaded(Container container)
    {
        if (!container.IsLoaded)
            BuildContainerFull(container);
    }

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
                EnsureIsLoaded(Source);

                if (!Source.IsCompatible)
                    ThrowHelper.ThrowInvalidOperationException($"Cannot copy as the source container is not compatible: {Source.IncompatibilityTag}");

                Destination.SetJsonObject(Source.GetJsonObject());
                Destination.ClearIncompatibility();

                // Faking relevant properties to force it to Write().
                Destination.Exists = true;

                // Additional properties required to properly rebuild the container.
                Destination.GameVersion = Source.GameVersion;
                Destination.SaveVersion = Source.SaveVersion;

                // Due to this CanCreate can be true.
                CopyPlatformExtra(Destination, Source);

                // This "if" is not really useful in this method but properly implemented nonetheless.
                if (write)
                    Write(Destination, Source.LastWriteTime ?? DateTimeOffset.Now);
            }
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
                if (container.DataFile?.Exists == true)
                    container.DataFile!.Delete();

                if (container.MetaFile?.Exists == true)
                    container.MetaFile!.Delete();
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
        foreach (var (Source, Destination) in operationData)
        {
            if (Source.Exists)
            {
                // Source and Destination exists. Swap.
                if (Destination.Exists)
                {
                    EnsureIsLoaded(Source);
                    EnsureIsLoaded(Destination);

                    // Make sure they can be swapped.
                    if (!Source.IsCompatible || !Destination.IsCompatible)
                        ThrowHelper.ThrowInvalidOperationException($"Cannot swap as at least one container is not compatible. {Source.Identifier}: {Source.IncompatibilityTag} >> {Destination.Identifier}: {Destination.IncompatibilityTag}");

                    // Keep a copy to be able to set Source correctly after Destination is done.
                    var copy = Common.DeepCopy(Destination);

                    // Write Source to Destination.
                    SwapOverwrite(Destination, Source, write);

                    // Write Destination to Source.
                    SwapOverwrite(Source, copy, write);
                }
                // Source exists only. Move to Destination.
                else
                    Move(Source, Destination, write);
            }
            // Destination exists only. Move to Source.
            else if (Destination.Exists)
                Move(Destination, Source, write);
        }
    }

    private void SwapOverwrite(Container container, Container other, bool write)
    {
        container.SaveVersion = other.SaveVersion;
        container.SetJsonObject(other.GetJsonObject());
        CopyPlatformExtra(container, other);
        if (write)
        {
            Write(container, other.LastWriteTime ?? DateTimeOffset.Now);
            RebuildContainerFull(container);
        }
    }

    #endregion
}
