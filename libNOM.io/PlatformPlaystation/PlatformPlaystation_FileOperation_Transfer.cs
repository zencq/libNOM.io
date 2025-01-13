namespace libNOM.io;


// This partial class contains file operation related code.
public partial class PlatformPlaystation : Platform
{
    #region Destination

    protected override void Transfer(TransferData sourceTransferData, int destinationSlotIndex, bool write, bool ignoreIncompleteUserIdentification = false)
    {
        base.Transfer(sourceTransferData, destinationSlotIndex, _usesSaveStreaming, ignoreIncompleteUserIdentification: ignoreIncompleteUserIdentification);

        if (!_usesSaveStreaming && write)
            WriteMemoryDat();
    }

    #endregion
}
