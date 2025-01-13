using CommunityToolkit.Diagnostics;

namespace libNOM.io;


// This partial class contains file operation related code.
public abstract partial class Platform : IPlatform, IEquatable<Platform>
{
    #region Copy

    public void Copy(IContainer source, IContainer destination) => Copy([(Source: source, Destination: destination)], true);

    public void Copy(IEnumerable<(IContainer Source, IContainer Destination)> operationData) => Copy(operationData, true);

    protected virtual void Copy(IEnumerable<(IContainer Source, IContainer Destination)> operationData, bool write)
    {
        foreach (var (Source, Destination) in operationData.Select(i => (Source: i.Source.ToContainer(), Destination: i.Destination.ToContainer())))
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
                Destination.CopyImportantProperties(Source);

                // Due to this CanCreate can be true.
                CopyContainerExtra(Destination, Source);

                // Write() or/and Invoke().
                if (write)
                    Write(Destination, Source.LastWriteTime ?? DateTimeOffset.Now);
                else
                    Destination.PropertiesChangedCallback.Invoke();
                Destination.JsonChangedCallback.Invoke();
            }
    }

    #endregion

    #region Delete

    public void Delete(IContainer container) => Delete([container], true);

    protected void Delete(IContainer container, bool write) => Delete([container], write);

    public void Delete(IEnumerable<IContainer> containers) => Delete(containers, true);

    protected virtual void Delete(IEnumerable<IContainer> containers, bool write)
    {
        Guard.IsTrue(CanDelete);

        DisableWatcher();

        foreach (var container in containers.Select(i => i.ToContainer()))
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

    public void Move(IContainer source, IContainer destination) => Move([(Source: source, Destination: destination)], true);

    protected void Move(IContainer source, IContainer destination, bool write) => Move([(Source: source, Destination: destination)], write);

    public void Move(IEnumerable<(IContainer Source, IContainer Destination)> operationData) => Move(operationData, true);

    protected virtual void Move(IEnumerable<(IContainer Source, IContainer Destination)> operationData, bool write)
    {
        Copy(operationData, write);
        Delete(operationData.Select(i => i.Source), write);
    }

    #endregion

    #region Swap

    public void Swap(IContainer source, IContainer destination) => Swap([(Source: source, Destination: destination)], true);

    public void Swap(IEnumerable<(IContainer Source, IContainer Destination)> operationData) => Swap(operationData, true);

    protected virtual void Swap(IEnumerable<(IContainer Source, IContainer Destination)> operationData, bool write)
    {
        foreach (var (Source, Destination) in operationData.Select(i => (Source: i.Source.ToContainer(), Destination: i.Destination.ToContainer())))
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
        CopyContainerExtra(container, other);
        if (write)
        {
            Write(container, other.LastWriteTime ?? DateTimeOffset.Now);
            RebuildContainerFull(container);
        }
    }

    #endregion

    // //

    #region Extra

    /// <summary>
    /// Creates the platform extra for the destination container.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    protected virtual void CreateContainerExtra(Container container, Container other)
    {
        CopyContainerExtra(container, other);

        // Reset bytes as from another platform it would not be right.
        container.Extra = container.Extra with
        {
            Bytes = null,
            MetaLength = (uint)(GetMetaBufferLength(container, force: true)),
        };
    }

    /// <summary>
    /// Copies the platform extra from the source into the destination container.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="source"></param>
    /// <returns></returns>
    protected virtual void CopyContainerExtra(Container destination, Container source)
    {
        // Overwrite all general values but keep platform specific stuff unchanged.
        destination.Extra = destination.Extra with
        {
            Bytes = source.Extra.Bytes,
            MetaLength = source.Extra.MetaLength,
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

    #region Helper

    private void EnsureIsLoaded(Container container)
    {
        if (!container.IsLoaded)
            BuildContainerFull(container);
    }

    #endregion
}
