using System.Security.Cryptography;

using CommunityToolkit.HighPerformance;

using SpookilySharp;

namespace libNOM.io;


/// <summary>
/// Implementation for the Steam platform.
/// </summary>
// This partial class contains writing related code.
public partial class PlatformSteam : Platform
{
    #region Container

    protected override void WritePlatformSpecific(Container container, DateTimeOffset writeTime)
    {
        // Update PlatformArchitecture in save depending on the current operating system without changing the sync state.
        container.GetJsonObject().SetValue(PlatformArchitecture, "PLATFORM");
        base.WritePlatformSpecific(container, writeTime);
    }

    #endregion

    #region Meta

    protected override Span<uint> CreateMeta(Container container, ReadOnlySpan<byte> data)
    {
        var buffer = new byte[GetMetaBufferLength(container)];

        // Editing account data is possible since Frontiers and therefore has always the new format.
        using var writer = new BinaryWriter(new MemoryStream(buffer));

        writer.Write(META_HEADER); // 4
        writer.Write(GetMetaFormat(container)); // 4

        if (container.IsSave && container.IsVersion360Frontiers) // META_FORMAT_2 and META_FORMAT_3 and META_FORMAT_4
        {
            // SPOOKY HASH and SHA256 HASH not used.
            writer.Seek(0x30, SeekOrigin.Current); // 16 + 32 = 48

            writer.Write(container.Extra.SizeDecompressed); // 4

            // COMPRESSED SIZE and PROFILE HASH not used.
            writer.Seek(0x8, SeekOrigin.Current); // 4 + 4 = 8

            writer.Write(container.BaseVersion); // 4
            writer.Write((ushort)(container.GameMode)); // 2
            writer.Write((ushort)(container.Season)); // 2
            writer.Write(container.TotalPlayTime); // 8

            // Append buffered bytes that follow META_LENGTH_KNOWN_VANILLA.
            writer.Write(container.Extra.Bytes ?? []); // Extra.Bytes is 20 or 276 or 300 or 348

            OverwriteWaypointMeta(writer, container);
            OverwriteWorldsMeta(writer, container);
        }
        else // META_FORMAT_1
        {
            AppendHashes(writer, data); // 8 + 8 + 32 = 48
            writer.Write(container.Extra.SizeDecompressed); // 4

            // Seek to position of last known byte and append the cached bytes.
            writer.Seek(META_LENGTH_AFTER_VANILLA, SeekOrigin.Begin);
            writer.Write(container.Extra.Bytes ?? []);
        }

        return buffer.AsSpan().Cast<byte, uint>();
    }

    protected override void OverwriteWorldsMeta(BinaryWriter writer, Container container)
    {
        // Write appended.
        base.OverwriteWorldsMeta(writer, container);

        // Overwrite changed.
        if (container.IsVersion500WorldsPartI)
        {
            // COMPRESSED SIZE is used again.
            writer.Seek(0x3C, SeekOrigin.Begin); // 4 + 4 + 16 + 32 + 4 = 60
            writer.Write(container.Extra.SizeDisk); // 4
        }
    }

    private static void AppendHashes(BinaryWriter writer, ReadOnlySpan<byte> data)
    {
#if NETSTANDARD2_0_OR_GREATER
        var sha256 = SHA256.Create().ComputeHash(data.ToArray());
#else
        var sha256 = SHA256.HashData(data);
#endif

        var spookyHash = new SpookyHash(0x155AF93AC304200, 0x8AC7230489E7FFFF);
        spookyHash.Update(sha256);
        spookyHash.Update(data.ToArray());
        spookyHash.Final(out ulong spookyFinal1, out ulong spookyFinal2);

        writer.Write(spookyFinal1); // 8
        writer.Write(spookyFinal2); // 8
        writer.Write(sha256); // 256 / 8 = 32
    }

    protected override ReadOnlySpan<byte> EncryptMeta(Container container, ReadOnlySpan<byte> data, Span<byte> meta)
    {
        uint current = 0;
        uint hash = 0;
        int iterations = container.IsVersion400Waypoint ? 6 : 8;
        ReadOnlySpan<uint> key = [(((uint)(container.PersistentStorageSlot) ^ 0x1422CB8C).RotateLeft(13) * 5) + 0xE6546B64, META_ENCRYPTION_KEY[1], META_ENCRYPTION_KEY[2], META_ENCRYPTION_KEY[3]];
        Span<uint> result = Common.DeepCopy<uint>(meta.Cast<byte, uint>());

        int lastIndex = result.Length - 1;

        for (int i = 0; i < iterations; i++)
        {
            hash += 0x9E3779B9;

            int keyIndex = (int)((hash >> 2) & 3);
            int valueIndex = 0;

            for (int j = 0; j < lastIndex; j++, valueIndex++)
            {
                //uint j1 = (result[valueIndex + 1] >> 3) ^ (current << 4);
                //uint j2 = (result[valueIndex + 1] * 4) ^ (current >> 5);
                //uint j3 = (current ^ key[(j & 3) ^ keyIndex]);
                //uint j4 = (result[valueIndex + 1] ^ hash);
                //result[valueIndex] += (j1 + j2) ^ (j3 + j4);
                result[valueIndex] += (((result[valueIndex + 1] >> 3) ^ (current << 4)) + ((result[valueIndex + 1] * 4) ^ (current >> 5))) ^ ((current ^ key[(j & 3) ^ keyIndex]) + (result[valueIndex + 1] ^ hash));
                current = result[valueIndex];
            }

            //uint i1 = (result[0] >> 3) ^ (current << 4);
            //uint i2 = (result[0] * 4) ^ (current >> 5);
            //uint i3 = (current ^ key[(lastIndex & 3) ^ keyIndex]);
            //uint i4 = (result[0] ^ hash);
            //result[lastIndex] += (i1 + i2) ^ (i3 + i4);
            result[lastIndex] += (((result[0] >> 3) ^ (current << 4)) + ((result[0] * 4) ^ (current >> 5))) ^ ((current ^ key[(lastIndex & 3) ^ keyIndex]) + (result[0] ^ hash));
            current = result[lastIndex];
        }

        return result.Cast<uint, byte>();
    }

    #endregion
}
