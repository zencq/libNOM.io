using CommunityToolkit.HighPerformance;

namespace libNOM.io;


// This partial class contains tracing related code.
public partial class PlatformMicrosoft : Platform
{
    #region Initialize

    protected override void InitializeTrace()
    {
        base.InitializeTrace();

        using var reader = new BinaryReader(File.Open(_containersindex.FullName, FileMode.Open, FileAccess.Read, FileShare.Read));

        // Platform
        SetPlatformTrace(reader);

        // Container
        SetContainerTrace(reader);
    }

    private void SetPlatformTrace(BinaryReader reader)
    {
        var header = reader.ReadInt32();

        // Skipping the following elements:
        // Count (8)
        // _processIdentifier (int * 2)
        // _lastWriteTime (8)
        reader.BaseStream.Seek(8 + reader.ReadInt32() * 2 + 8, SeekOrigin.Current);
        var syncGlobal = reader.ReadInt32();

        // Skipping the following elements:
        // _accountGuid (int * 2)
        reader.BaseStream.Seek(reader.ReadInt32() * 2, SeekOrigin.Current);
        var footer = reader.ReadInt64();

        Trace = Trace! with
        {
            MicrosoftContainersIndexHeader = header,
            MicrosoftContainersIndexProcessIdentifier = _processIdentifier,
            MicrosoftContainersIndexTimestamp = _lastWriteTime,
            MicrosoftContainersIndexSyncState = syncGlobal,
            MicrosoftContainersIndexAccountGuid = _accountGuid,
            MicrosoftContainersIndexFooter = footer,
        };
    }

    private void SetContainerTrace(BinaryReader reader)
    {
        foreach (var container in SaveContainerCollection.Where(i => i.IsCompatible))
        {
            var identifier1 = reader.ReadBytes(reader.ReadInt32() * 2).AsSpan().Cast<byte, char>().ToString();
            var identifier2 = reader.ReadBytes(reader.ReadInt32() * 2).AsSpan().Cast<byte, char>().ToString();

            // Skipping the following elements:
            // MicrosoftSyncTime (int * 2)
            // MicrosoftBlobContainerExtension (1)
            // MicrosoftSyncState (4)
            // MicrosoftBlobDirectoryGuid (16)
            // LastWriteTime (8)
            // Empty (8)
            reader.BaseStream.Seek(reader.ReadInt32() * 2 + 1 + 4 + 16 + 8 + 8, SeekOrigin.Current);
            var size = reader.ReadInt64();

            container.Trace = new()
            {
                MicrosoftDirectoryGuid = container.Extra.MicrosoftBlobDirectoryGuid,
                MicrosoftExtension = container.Extra.MicrosoftBlobContainerExtension,
                MicrosoftIdentifier1 = identifier1,
                MicrosoftIdentifier2 = identifier2,
                MicrosoftSize = size,
                MicrosoftSyncState = (byte)(container.Extra.MicrosoftSyncState!),
                MicrosoftSyncTime = container.Extra.MicrosoftSyncTime,
            };
        }
    }

    #endregion
}
