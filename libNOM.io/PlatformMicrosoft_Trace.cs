using System.Linq;

using CommunityToolkit.HighPerformance;

namespace libNOM.io;


/// <summary>
/// Implementation for the Microsoft platform.
/// </summary>
// This partial class contains some general code.
public partial class PlatformMicrosoft : Platform
{
    #region Trace

    protected override void InitializeTrace()
    {
        base.InitializeTrace();

        using var reader = new BinaryReader(File.Open(_containersindex.FullName, FileMode.Open, FileAccess.Read, FileShare.Read));

        // Platform
        var header = reader.ReadInt32();
        var count = reader.ReadInt64();

        reader.BaseStream.Seek(reader.ReadInt32() * 2 + 8, SeekOrigin.Current); // skip _processIdentifier and _lastWriteTime

        var syncGlobal = reader.ReadInt32();

        reader.BaseStream.Seek(reader.ReadInt32() * 2, SeekOrigin.Current); // skip _accountGuid

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

        // Container
        foreach (var container in SaveContainerCollection)
        {
            var identifier1 = reader.ReadBytes(reader.ReadInt32() * 2).AsSpan().Cast<byte, char>().ToString();
            var identifier2 = reader.ReadBytes(reader.ReadInt32() * 2).AsSpan().Cast<byte, char>().ToString();

            // skip
            // MicrosoftSyncTime
            // MicrosoftBlobContainerExtension
            // MicrosoftSyncState
            // MicrosoftBlobDirectoryGuid
            // LastWriteTime
            // Empty
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
