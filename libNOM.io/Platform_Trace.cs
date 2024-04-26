using libNOM.io.Trace;

namespace libNOM.io;


// This partial class contains tracing related code.
public abstract partial class Platform : IPlatform, IEquatable<Platform>
{
    #region Configuration

    public PlatformTrace? Trace { get; protected set; }

    #endregion

    #region Initialize

    protected virtual void InitializeTrace()
    {
        // Platform
        Trace = new()
        {
            DirectoryUID = _uid,
        };

        // Container
        foreach (var container in SaveContainerCollection.Where(i => i.IsCompatible))
            container.Trace = new()
            {
                BaseVersion = container.Extra.BaseVersion,
                GameMode = container.GameMode,
                MetaLength = container.Extra.MetaLength,
                PersistentStorageSlot = container.PersistentStorageSlot,
                Platform = container.Platform,
                SaveVersion = container.SaveVersion,
                SizeDecompressed = container.Extra.SizeDecompressed,
                SizeDisk = container.Extra.SizeDisk,
                UserIdentification = container.UserIdentification,
            };
    }

    #endregion
}
