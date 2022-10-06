using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SpookilySharp;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace libNOM.io;


#region Container

internal record struct SteamContainer
{
    internal uint Header; // 0
    internal uint Format; // 1
    internal ulong[] SpookyHash; // 2 - 5
    internal byte[] SHA256;// 6 - 13
    internal uint DecompressedSize; // 14
    internal uint CompressedSize; // 15
    internal uint ProfileHash; // 16
    internal uint SaveVersion; // 17
    internal uint GameModeEnum; // 18
    internal uint TotalPlayTime; // 19
}

public partial class Container
{
    internal SteamContainer? Steam { get; set; }
}

#endregion

#region PlatformDirectoryData

internal record class PlatformDirectoryDataSteam : PlatformDirectoryData
{
    internal override string DirectoryPath { get; } = ((Func<string>)(() =>
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HelloGames", "NMS");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "Steam", "steamapps", "compatdata", "275850", "pfx", "drive_c", "users", "steamuser", "Application Data", "HelloGames", "NMS");

        return string.Empty; // same as if not defined at all
    }))();

    internal override string DirectoryPathPattern { get; } = "st_*";

    internal override string[] AnchorFileGlob { get; } = new[] { "save*.hg" };

    internal override Regex[] AnchorFileRegex { get; } = new Regex[] { new("save\\d{0,2}\\.hg", RegexOptions.Compiled) };
}

#endregion

public partial class PlatformSteam : Platform
{
    #region Constant

    protected const uint META_HEADER = 0xEEEEEEBEU; // 4008636094
    protected const int META_SIZE = 0x68; // 104

    #endregion

    #region Field

    private readonly HttpClient _httpClient = new();
    private string? _steamId;

    #endregion

    #region Property

    #region Flags

    public override bool CanCreate { get; } = true;

    public override bool CanRead { get; } = true;

    public override bool CanUpdate { get; } = true;

    public override bool CanDelete { get; } = true;

    public override bool HasModding { get; } = true;

    public override bool IsWindowsPlatform { get; } = true;

    #endregion

    #region Platform Indicator

    internal static PlatformDirectoryData DirectoryData { get; } = new PlatformDirectoryDataSteam();

    internal override PlatformDirectoryData PlatformDirectoryData { get; } = DirectoryData;

    protected override string PlatformArchitecture { get; } = "Win|Final";

    public override PlatformEnum PlatformEnum { get; } = PlatformEnum.Steam;

    protected override string PlatformToken { get; } = "ST";

    #endregion

    #region Process (System)

    protected override string? ProcessPath { get; } = "steamapps\\common\\No Man's Sky\\Binaries\\NMS.exe";

    #endregion

    #endregion

    #region Getter

    /// <summary>
    /// Gets the necessary keys for the meta file encryption.
    /// </summary>
    /// <param name="container"></param>
    /// <returns></returns>
    protected static uint[] GetKeys(Container container)
    {
        // MurmurHash3_x86_32-like https://github.com/aappleby/smhasher/blob/master/src/MurmurHash3.cpp#L94
        var k1 = (uint)(container.MetaIndex) ^ 0x1422CB8CU;
        var h1 = RotateLeft(k1, 13) * 5 + 0xE6546B64U;
        var b1 = Encoding.ASCII.GetBytes("NAESEVADNAYRTNRG");
        for (int i = 0; i < 4; i++)
        {
            b1[i] = (byte)(h1 >> (i * 8));
        }

        return b1.GetUInt32();
    }

    protected static uint RotateLeft(uint value, int bits)
    {
        return (value << bits) | (value >> (32 - bits));
    }

    #endregion

    // //

    #region Constructor

    public PlatformSteam() : base(null, null) { }

    public PlatformSteam(DirectoryInfo? directory) : base(directory, null) { }

    public PlatformSteam(DirectoryInfo? directory, PlatformSettings? platformSettings) : base(directory, platformSettings) { }

    protected override void InitializeComponent(DirectoryInfo? directory, PlatformSettings? platformSettings)
    {
        // Proceed to base method even if no directory.
        if (directory is not null)
        {
#if NETSTANDARD2_0
            _steamId = directory.Name.Substring(3); // remove "st_"
#else
            _steamId = directory.Name[3..]; // remove "st_"
#endif
        }

        base.InitializeComponent(directory, platformSettings);
    }

    #endregion

    // //

    #region Read

    #region Create

    protected override Container CreateContainer(int metaIndex, object? extra)
    {
        if (metaIndex == 0)
            return new Container(metaIndex)
            {
                DataFile = new FileInfo(Path.Combine(Location!.FullName, "accountdata.hg")),
                MetaFile = new FileInfo(Path.Combine(Location!.FullName, "mf_accountdata.hg")),
            };

        var steamIndex = metaIndex == Global.OFFSET_INDEX ? string.Empty : $"{metaIndex - 1}";
        return new Container(metaIndex)
        {
            DataFile = new FileInfo(Path.Combine(Location!.FullName, $"save{steamIndex}.hg")),
            MetaFile = new FileInfo(Path.Combine(Location!.FullName, $"mf_save{steamIndex}.hg")),
        };
    }

    #endregion

    #region Load

    protected override uint[] DecryptMeta(Container container, byte[] meta)
    {
        if (meta.Length != META_SIZE)
        {
            return Array.Empty<uint>();
        }

        var hash = 0xF1BBCDC8U;
        var keys = GetKeys(container);
        var values = meta.GetUInt32();

        for (int i = 0; i < 8; i++)
        {
            var idx_key = (int)((hash >> 2) & 3);
            var idx_value = values.Length - 1;
            var value = values[0];

            for (int j = 25; j > 0; j--, idx_value--)
            {
                var j1 = (value >> 3) ^ (values[idx_value - 1] << 4);
                var j2 = (value * 4) ^ (values[idx_value - 1] >> 5);
                var j3 = (values[idx_value - 1] ^ keys[(j & 3) ^ idx_key]);
                var j4 = (value ^ hash);
                values[idx_value] -= (j1 + j2) ^ (j3 + j4);
                value = values[idx_value];
            }

            idx_value = values.Length - 1;

            var i1 = (value >> 3) ^ (values[idx_value] << 4);
            var i2 = (value * 4) ^ (values[idx_value] >> 5);
            var i3 = (values[idx_value] ^ keys[idx_key]);
            var i4 = (value ^ hash);
            values[0] -= (i1 + i2) ^ (i3 + i4);

            hash += 0x61C88647U;
        }

        var bytes = values.GetBytes();

        container.Steam = new SteamContainer()
        {
            Header = values[0],
            Format = values[1],
            SpookyHash = new[] { BitConverter.ToUInt64(bytes, 8), BitConverter.ToUInt64(bytes, 16) },
#if NETSTANDARD2_0
            SHA256 = bytes.Skip(24).Take(32).ToArray(),
#else
            SHA256 = bytes[24..56],
#endif
            DecompressedSize = values[14],
            CompressedSize = values[15],
            ProfileHash = values[16],
            SaveVersion = values[17],
            GameModeEnum = values[18],
            TotalPlayTime = values[19],
        };

        return values;
    }

    protected override byte[] DecompressData(Container container, uint[] meta, byte[] data)
    {
        // No compression for account data and before Frontiers.
        if (!container.IsSave || data.Take(4).GetUInt32().FirstOrDefault() != Global.HEADER_SAVE_STREAMING_CHUNK)
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

    #region UserIdentification

    protected override string GetUserIdentification(JObject jsonObject, string key)
    {
        if (key is "LID" or "UID" && _steamId is not null)
            return _steamId;

        var result = base.GetUserIdentification(jsonObject, key);
        if (!string.IsNullOrEmpty(result))
            return result;

        if (key is "USN" && _steamId is not null)
            return GetUserIdentificationBySteam() ?? string.Empty;

        return result;
    }

    protected override IEnumerable<JToken> GetUserIdentificationByBase(JObject jsonObject, string key)
    {
        if (_steamId is null)
            return base.GetUserIdentificationByBase(jsonObject, key);

        var path = Settings.Mapping ? $"PlayerStateData.PersistentPlayerBases[?({{0}})].Owner.{key}" : $"6f=.F?0[?({{0}})].3?K.{key}";
        var expressions = new[]
        {
            Settings.Mapping ? $"@.BaseType.PersistentBaseTypes == '{PersistentBaseTypesEnum.HomePlanetBase}' || @.BaseType.PersistentBaseTypes == '{PersistentBaseTypesEnum.FreighterBase}'" : $"@.peI.DPp == '{PersistentBaseTypesEnum.HomePlanetBase}' || @.peI.DPp == '{PersistentBaseTypesEnum.FreighterBase}'", // only with own base
            Settings.Mapping ? $"@.Owner.UID == '{_steamId}'" : $"@.3?K.K7E == '{_steamId}'", // only with specified value
        };

        return GetUserIdentificationIntersection(jsonObject, path, expressions);
    }

    protected override IEnumerable<JToken> GetUserIdentificationByDiscovery(JObject jsonObject, string key)
    {
        if (_steamId is null)
            return base.GetUserIdentificationByBase(jsonObject, key);

        var path = Settings.Mapping ? $"DiscoveryManagerData.DiscoveryData-v1.Store.Record[?({{0}})].OWS.{key}" : $"fDu.ETO.OsQ.?fB[?({{0}})].ksu.{key}";
        var expressions = new[]
        {
            Settings.Mapping ? $"@.OWS.UID == '{_steamId}'" : $"@.ksu.K7E == '{_steamId}'", // only with specified value
        };

        return GetUserIdentificationIntersection(jsonObject, path, expressions);
    }

    protected override IEnumerable<JToken> GetUserIdentificationBySettlement(JObject jsonObject, string key)
    {
        if (_steamId is null)
            return base.GetUserIdentificationByBase(jsonObject, key);

        var path = Settings.Mapping ? $"PlayerStateData.SettlementStatesV2[?({{0}})].Owner.{key}" : $"6f=.GQA[?({{0}})].3?K.{key}";
        var expressions = new[]
        {
            Settings.Mapping ? $"@.Owner.UID == '{_steamId}'" : $"@.3?K.K7E == '{_steamId}'", // only with specified value
        };

        return GetUserIdentificationIntersection(jsonObject, path, expressions);
    }

    /// <summary>
    /// Gets the <see cref="UserIdentificationData"/> information for the USN by calling the Steam Web-API.
    /// </summary>
    /// <returns></returns>
    private string? GetUserIdentificationBySteam()
    {
        if (string.IsNullOrWhiteSpace(Properties.Resources.STEAM_API_KEY) || !Properties.Resources.STEAM_API_KEY.All(char.IsLetterOrDigit))
            return null;

        var requestUri = $"http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/?key={Properties.Resources.STEAM_API_KEY}&steamids={_steamId}";

        var responseTask = _httpClient.GetAsync(requestUri);
        responseTask.Wait();

        var response = responseTask.Result;
        response.EnsureSuccessStatusCode();

        var contentTask = response.Content.ReadAsStringAsync();
        contentTask.Wait();

        var jsonObject = JsonConvert.DeserializeObject(contentTask.Result) as JObject;
        return jsonObject?.SelectToken("response.players[0].personaname")?.Value<string>();
    }

    #endregion

    #region Write

    protected override byte[] CompressData(Container container, byte[] data)
    {
        if (!container.IsSave || !container.IsFrontiers)
            return data;

        var result = new List<byte>();

        var offset = 0;
        while (offset < data.Length)
        {
            var source = data.Skip(offset).Take(SAVE_STREAMING_CHUNK_SIZE).ToArray();
            _ = LZ4_Encode(source, out byte[] target);

            offset += SAVE_STREAMING_CHUNK_SIZE;

            var chunkHeader = new uint[4];
            chunkHeader[0] = Global.HEADER_SAVE_STREAMING_CHUNK;
            chunkHeader[1] = (uint)(target.Length);
            chunkHeader[2] = (uint)(source.Length);

            result.AddRange(chunkHeader.GetBytes());
            result.AddRange(target);
        }

        return result.ToArray();
    }

    protected override byte[] CreateMeta(Container container, byte[] data, int decompressedSize)
    {
        //  0. META HEADER          (  4)
        //  1. META FORMAT          (  4)
        //  2. SPOOKY HASH          ( 16)
        //  3. SHA256 HASH          ( 32)
        //  4. DECOMPRESSED SIZE    (  4)
        //  5. COMPRESSED SIZE      (  4)
        //  6. PROFILE HASH         (  4)
        //  7. SAVE VERSION         (  4)
        //  8. GAME MODE            (  2)
        //  9. SEASON               (  2)
        // 10. TOTAL PLAY TIME      (  4)
        // 11. UNKNOWN              ( 24)
        //                          (104)

        var buffer = new byte[META_SIZE];

        if (!container.IsSave || !container.IsFrontiers)
        {
            // TODO does not work as below even though goatfungus seems to be the same...
            if (!container.IsSave)
                return ReadMeta(container);

            var sha256 = SHA256.Create().ComputeHash(data);

            var sh = new SpookyHash(0x155af93ac304200, 0x8ac7230489e7ffff);
            sh.Update(sha256);
            sh.Update(data);
            sh.Final(out ulong spookyHash1, out ulong spookyHash2);

            using var writer = new BinaryWriter(new MemoryStream(buffer));

            writer.Write(META_HEADER); // 4 >> 1
            writer.Write(SAVE_FORMAT_110); // 4 >> 1

            writer.Write(spookyHash1); // 8 >> 2
            writer.Write(spookyHash2); // 8 >> 2

            writer.Write(sha256); // 256 / 8 = 32 >> 8
        }
        else
        {
            using var writer = new BinaryWriter(new MemoryStream(buffer));

            writer.Write(META_HEADER); // 4 >> 1
            writer.Write(SAVE_FORMAT_360); // 4 >> 1

            // SPOOKY HASH and SHA256 HASH not used.
            writer.Seek(0x30, SeekOrigin.Current);

            writer.Write(decompressedSize); // 4 >> 1

            // COMPRESSED SIZE and PROFILE HASH not used.
            writer.Seek(0x8, SeekOrigin.Current);

            writer.Write(container.BaseVersion); // 4 >> 1
            writer.Write((ushort)(container.GameModeEnum)); // 2 >> 0.5
            writer.Write((ushort)(container.SeasonEnum)); // 2 >> 0.5
            writer.Write(container.TotalPlayTime); // 8 >> 2
        }

        return EncryptMeta(container, data, CompressMeta(container, data, buffer));
    }

    protected override byte[] EncryptMeta(Container container, byte[] data, byte[] meta)
    {
        var hash = 0U;
        var keys = GetKeys(container);
        var value = 0U;
        var values = meta.GetUInt32();

        for (int i = 0; i < 8; i++)
        {
            hash += 0x9E3779B9;

            var idx_key = (int)((hash >> 2) & 3);
            var idx_value = 0;

            for (int j = 0; j < 25; j++, idx_value++)
            {
                var j1 = (values[idx_value + 1] >> 3) ^ (value << 4);
                var j2 = (values[idx_value + 1] * 4) ^ (value >> 5);
                var j3 = (value ^ keys[(j & 3) ^ idx_key]);
                var j4 = (values[idx_value + 1] ^ hash);
                values[idx_value] += (j1 + j2) ^ (j3 + j4);
                value = values[idx_value];
            }

            var i1 = (values[0] >> 3) ^ (value << 4);
            var i2 = (values[0] * 4) ^ (value >> 5);
            var i3 = (value ^ keys[idx_key ^ 1]);
            var i4 = (values[0] ^ hash);
#if NETSTANDARD2_0
            values[values.Length - 1] += (i1 + i2) ^ (i3 + i4);
            value = values[values.Length - 1];
#else
            values[^1] += (i1 + i2) ^ (i3 + i4);
            value = values[^1];
#endif
        }

        return values.GetBytes();
    }

    #endregion
}
