namespace libNOM.io.Enums;


/// <summary>
/// Specifies sync states used in the containers.index of the <see cref="PlatformMicrosoft"/> for single blobs.
/// </summary>
internal enum MicrosoftBlobSyncStateEnum
{
    Unknown_Zero = 0,
    Synced = 1,
    Modified = 2,
    Deleted = 3,
    Unknown_Four = 4,
    Created = 5,
}
