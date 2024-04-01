﻿using CommunityToolkit.Diagnostics;

namespace libNOM.io;


// This partial class contains file operation related code.
public partial class PlatformPlaystation : Platform
{
    #region Extra

    protected override void CreateContainerExtra(Container container, Container other)
    {
        if (_usesSaveStreaming)
            base.CreateContainerExtra(container, other);
        else
        {
            // base.CreatePlatformExtra() resets Extra.Bytes but here we want keep it and therefore calling CopyPlatformExtra() directly.
            CopyContainerExtra(container, other);

            container.Extra = container.Extra with
            {
                MetaLength = (uint)(container.IsVersion400Waypoint ? META_LENGTH_TOTAL_WAYPOINT : META_LENGTH_TOTAL_VANILLA),
            };
        }
    }

    protected override void CopyContainerExtra(Container container, Container other)
    {
        base.CopyContainerExtra(container, other);

        if (!_usesSaveStreaming)
            // Update bytes in platform extra as it is what will be written later.
            container.Extra = container.Extra with
            {
                Bytes = CreateData(container).ToArray(),
                LastWriteTime = other.LastWriteTime ?? DateTimeOffset.Now,
            };
    }

    #endregion

    #region Copy

    protected override void Copy(IEnumerable<(Container Source, Container Destination)> operationData, bool write)
    {
        base.Copy(operationData, _usesSaveStreaming);

        if (!_usesSaveStreaming && write)
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

            // Set afterwards again to ensure it is set to false.
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
        base.Swap(containerOperationData, _usesSaveStreaming);

        if (!_usesSaveStreaming && write)
            WriteMemoryDat();
    }

    #endregion

    #region Transfer

    protected override void Transfer(TransferData sourceTransferData, int destinationSlotIndex, bool write)
    {
        base.Transfer(sourceTransferData, destinationSlotIndex, _usesSaveStreaming);

        if (!_usesSaveStreaming && write)
            WriteMemoryDat();
    }

    #endregion
}
