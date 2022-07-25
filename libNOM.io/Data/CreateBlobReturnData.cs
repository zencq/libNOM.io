namespace libNOM.io.Data;


/// <summary>
/// Holds information that are necessary to properly change a blob after its update.
/// </summary>
internal record struct CreateBlobReturnData
{
    internal byte[] UpdatedContainerBytes;

    internal FileInfo OldDataFile;

    internal byte OldExtension;

    internal FileInfo OldMetaFile;
}
