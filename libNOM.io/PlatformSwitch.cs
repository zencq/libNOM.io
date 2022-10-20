using LazyCache;
using Microsoft.Extensions.Caching.Memory;
using System.Text.RegularExpressions;

namespace libNOM.io;


#region Container

internal record class SwitchContainer
{
    internal uint BaseVersion;

    internal uint GameMode;

    internal uint MetaIndex;

    internal byte[] MetaTail = null!;

    internal uint TotalPlayTime;
}

public partial class Container
{
    internal SwitchContainer? Switch { get; set; }
}

#endregion

#region PlatformDirectoryData

internal record class PlatformDirectoryDataSwitch : PlatformDirectoryData
{
    internal override string[] AnchorFileGlob { get; } = new[] { "manifest*.hg", "savedata*.hg" }; // uses the same as PlayStation for data file but we use this here to have a unique one

    internal override Regex[] AnchorFileRegex { get; } = new Regex[] { new("manifest\\d{2}\\.hg", RegexOptions.Compiled), new("savedata\\d{2}\\.hg", RegexOptions.Compiled) };
}

#endregion

public partial class PlatformSwitch : Platform
{
    #region Constant

    protected const int META_KNOWN = 0x20; // 32
    private const int META_SIZE = 0x64; // 100
    private const int META_SIZE_WAYPOINT = 0x164; // 256

    #endregion

    #region Property

    #region Flags

    public override bool CanDelete { get; } = true;

    public override bool CanCreate { get; } = true;

    public override bool CanRead { get; } = true;

    public override bool CanUpdate { get; } = true;

    public override bool IsValid => AnchorFileIndex == 0; // { get; } // seconds index is only there to have the actual save data filename to get the MetaIndex in AnalyzeFile 

    #endregion

    #region Platform Indicator

    internal static PlatformDirectoryData DirectoryData { get; } = new PlatformDirectoryDataSwitch();

    internal override PlatformDirectoryData PlatformDirectoryData { get; } = DirectoryData; // { get; }

    protected override string PlatformArchitecture { get; } = "NX1|Final";

    public override PlatformEnum PlatformEnum { get; } = PlatformEnum.Switch;

    protected override string PlatformToken { get; } = "NS";

    #endregion

    #endregion

    #region Constructor

    public PlatformSwitch() : base(null, null) { }

    public PlatformSwitch(DirectoryInfo? directory) : base(directory, null) { }

    public PlatformSwitch(DirectoryInfo? directory, PlatformSettings? platformSettings) : base(directory, platformSettings) { }

    #endregion

    #region Read

    #region Create

    protected override Container CreateContainer(int metaIndex, object? extra)
    {
        return new Container(metaIndex)
        {
            DataFile = new FileInfo(Path.Combine(Location!.FullName, $"savedata{metaIndex:D2}.hg")),
            MetaFile = new FileInfo(Path.Combine(Location!.FullName, $"manifest{metaIndex:D2}.hg")),
        };
    }

    #endregion

    #region Load

    protected override uint[] DecryptMeta(Container container, byte[] meta)
    {
        var metaInt = base.DecryptMeta(container, meta);

        container.Switch = new()
        {
            BaseVersion = metaInt[5],
            GameMode = metaInt[6],
            MetaIndex = metaInt[3],
#if NETSTANDARD2_0
            MetaTail = meta.Skip(META_KNOWN).ToArray(),
#else
            MetaTail = meta[META_KNOWN..],
#endif
            TotalPlayTime = metaInt[7],
        };

        return metaInt;
    }

    protected override byte[] DecompressData(Container container, uint[] meta, byte[] data)
    {
        // No compression for account data.
        if (!container.IsSave)
            return data;

        var result = new List<byte>();

        var offset = 0;
        while (offset < data.Length)
        {
            var chunkHeader = data.Skip(offset).Take(SAVE_STREAMING_CHUNK_HEADER_SIZE).GetUInt32();
            offset += SAVE_STREAMING_CHUNK_HEADER_SIZE;

            var chunkCompressed = (int)(chunkHeader[1]);
            var chunkDecompressed = (int)(chunkHeader[2]);

            var source = data.Skip(offset).Take(chunkCompressed).ToArray();
            offset += chunkCompressed;

            _ = LZ4_Decode(source, out byte[] target, chunkDecompressed);
            result.AddRange(target);
        }

        return result.ToArray();
    }

    #endregion

    #endregion

    #region Write

    protected override byte[] CreateMeta(Container container, byte[] data, int decompressedSize)
    {
        //  0. META HEADER          (  4)
        //  1. META FORMAT          (  4)
        //  2. DECOMPRESSED SIZE    (  4)
        //  3. META INDEX           (  4)
        //  4. TIMESTAMP            (  4)
        //  5. SAVE VERSION         (  4)
        //  6. GAME MODE            (  2)
        //  7. SEASON               (  2)
        //  8. TOTAL PLAY TIME      (  4)
        //  9. UNKNOWN              ( 68)
        //                          (100)

        //  9. UNKNOWN              (324) // Waypoint
        //                          (356)

        // Use default size if tail is not set.
        var bufferSize = container.Switch!.MetaTail is null ? (container.IsWaypoint ? META_SIZE_WAYPOINT : META_SIZE) : (META_KNOWN + container.Switch!.MetaTail!.Length);
        var buffer = new byte[bufferSize];
        var unixSeconds = (uint)(container.LastWriteTime.ToUniversalTime().ToUnixTimeSeconds());

        using var writer = new BinaryWriter(new MemoryStream(buffer));

        if (container.MetaIndex == 0)
        {
            // Reuse most of the data from the SwitchContainer as it contains data from another regular save.
            writer.Write(META_HEADER); // 4
            writer.Write(SAVE_FORMAT_360); // 4
            writer.Write(decompressedSize); // 4
            writer.Write(container.Switch!.MetaIndex); // 4
            writer.Write(unixSeconds); // 4
            writer.Write(container.Switch!.BaseVersion); // 4
            writer.Write(container.Switch!.GameMode); // 4
            writer.Write(container.Switch!.TotalPlayTime); // 4
            writer.Write(container.Switch!.MetaTail ?? Array.Empty<byte>()); // 68 or 336
        }
        else
        {
            writer.Write(META_HEADER); // 4
            writer.Write(SAVE_FORMAT_360); // 4
            writer.Write(decompressedSize); // 4
            writer.Write(container.MetaIndex); // 4
            writer.Write(unixSeconds); // 4
            writer.Write(container.BaseVersion); // 4
            writer.Write((ushort)(container.GameModeEnum ?? 0)); // 2
            writer.Write((ushort)(container.SeasonEnum)); // 2
            writer.Write((int)(container.TotalPlayTime)); // 4
            writer.Write(container.Switch!.MetaTail ?? Array.Empty<byte>()); // 68 or 336
        }

        return EncryptMeta(container, data, CompressMeta(container, data, buffer));
    }

    #endregion
}
