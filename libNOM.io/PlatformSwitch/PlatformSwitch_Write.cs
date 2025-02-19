﻿using CommunityToolkit.HighPerformance;

namespace libNOM.io;


/// <summary>
/// Implementation for the Nintendo Switch platform.
/// </summary>
// This partial class contains writing related code.
public partial class PlatformSwitch : Platform
{
    #region Meta

    protected override Span<uint> CreateMeta(Container container, ReadOnlySpan<byte> data)
    {
        var buffer = container.IsAccount ? CreateAccountMeta(container) : CreateSaveMeta(container);
        return buffer.Cast<byte, uint>();
    }

    private Span<byte> CreateAccountMeta(Container container)
    {
        byte[] buffer = container.Extra.Bytes ?? new byte[GetMetaBufferLength(container)];

        // Overwrite only SizeDecompressed.
        using var writer = new BinaryWriter(new MemoryStream(buffer));

        writer.Seek(0x8, SeekOrigin.Begin);
        writer.Write(container.Extra.SizeDecompressed); // 4

        return buffer;
    }

    private Span<byte> CreateSaveMeta(Container container)
    {
        byte[] buffer = new byte[GetMetaBufferLength(container)];

        using var writer = new BinaryWriter(new MemoryStream(buffer));

        writer.Write(META_HEADER); // 4
        writer.Write(GetMetaFormat(container)); // 4
        writer.Write(container.Extra.SizeDecompressed); // 4
        writer.Write(container.MetaIndex); // 4 // TODO: Unknown in Worlds Part II.
        writer.Write((uint)(container.IsVersion550WorldsPartII ? 0 : container.LastWriteTime!.Value.ToUniversalTime().ToUnixTimeSeconds())); // 4
        writer.Write(container.BaseVersion); // 4
        writer.Write((ushort)(container.GameMode)); // 2
        writer.Write((ushort)(container.Season)); // 2
        writer.Write(container.TotalPlayTime); // 8

        // Append buffered bytes that follow META_LENGTH_KNOWN_VANILLA.
        writer.Write(container.Extra.Bytes ?? []); // Extra.Bytes is 64 or 320 or 336 or 344

        OverwriteWaypointMeta(writer, container);
        OverwriteWorldsMeta(writer, container);

        return buffer;
    }

    #endregion
}
