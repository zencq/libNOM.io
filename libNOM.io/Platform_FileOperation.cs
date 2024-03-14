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
}
