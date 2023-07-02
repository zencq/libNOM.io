using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SpookilySharp;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace libNOM.io;


#region Container

internal record class PlatformExtraSteam
{
    internal uint[] MetaTail = null!;
}

public partial class Container
{
    internal PlatformExtraSteam? Steam { get; set; }
}

#endregion

public partial class PlatformSteam : Platform
{
    #region Constant

    #region Platform Specific

    protected const uint META_HEADER = 0xEEEEEEBE; // 4008636094
    protected const int META_KNOWN = 0x14; // 20
    protected const int META_SIZE = 0x68; // 104
    protected const int META_SIZE_WAYPOINT = 0x168; // 360

    #endregion

    #region Directory Data

    public const string ACCOUNT_PATTERN = "st_76561198*";
    public static readonly string[] ANCHOR_FILE_GLOB = new[] { "save*.hg" };
#if NETSTANDARD2_0_OR_GREATER || NET6_0
    public static readonly Regex[] ANCHOR_FILE_REGEX = new Regex[] { AnchorFileRegex0! };
#else
    public static readonly Regex[] ANCHOR_FILE_REGEX = new Regex[] { AnchorFileRegex0() };
#endif
    public static readonly string PATH = ((Func<string>)(() =>
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HelloGames", "NMS");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) // SteamDeck
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "Steam", "steamapps", "compatdata", "275850", "pfx", "drive_c", "users", "steamuser", "Application Data", "HelloGames", "NMS");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) // macOS
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "HelloGames", "NMS");

        return string.Empty; // same as if not defined at all
    }))();

    #endregion

    #region Generated Regex

#if NETSTANDARD2_0_OR_GREATER || NET6_0
    private static readonly Regex AnchorFileRegex0 = new("save\\d{0,2}\\.hg", RegexOptions.Compiled);
#else
    [GeneratedRegex("save\\d{0,2}\\.hg", RegexOptions.Compiled)]
    private static partial Regex AnchorFileRegex0();
#endif

    #endregion

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

    public override bool IsPersonalComputerPlatform { get; } = true;

    public override bool RestartToApply { get; } = false;

    #endregion

    #region Platform Indicator

    protected override string[] PlatformAnchorFileGlob { get; } = ANCHOR_FILE_GLOB;

    protected override Regex[] PlatformAnchorFileRegex { get; } = ANCHOR_FILE_REGEX;

    protected override string PlatformArchitecture { get; } = "Win|Final";

    public override PlatformEnum PlatformEnum { get; } = PlatformEnum.Steam;

    protected override string? PlatformProcess
    {
        get
        {
            // On SteamDeck (with Proton) the Windows executable is also used.
            // TODO: Verify
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return "steamapps\\common\\No Man's Sky\\Binaries\\NMS.exe";

            // TODO: Get executable
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) // macOS
                return "???";

            return null; // same as if not defined at all
        }
    }

    protected override string PlatformToken { get; } = "ST";

    #endregion

    #endregion

    // //

    #region Constructor

    public PlatformSteam(string path) : base(path) { }

    public PlatformSteam(string path, PlatformSettings platformSettings) : base(path, platformSettings) { }

    public PlatformSteam(DirectoryInfo directory) : base(directory) { }

    public PlatformSteam(DirectoryInfo directory, PlatformSettings platformSettings) : base(directory, platformSettings) { }

    /// <seealso href="https://help.steampowered.com/en/faqs/view/2816-BE67-5B69-0FEC"/>
    protected override void InitializeComponent(DirectoryInfo? directory, PlatformSettings? platformSettings)
    {
        // Proceed to base method even if no directory.
#if NETSTANDARD2_0
        if (directory is not null && directory.Name.Length == 20 && directory.Name.StartsWith(ACCOUNT_PATTERN.Substring(0, ACCOUNT_PATTERN.Length - 1)) && directory.Name.Substring(11).IsAllDigits())
            _steamId = directory.Name.Substring(3); // remove "st_"
#else
        if (directory is not null && directory.Name.Length == 20 && directory.Name.StartsWith(ACCOUNT_PATTERN[..^1]) && directory.Name[11..].IsAllDigits()) 
            _steamId = directory.Name[3..]; // remove "st_"
#endif

        base.InitializeComponent(directory, platformSettings);
    }

#endregion

    // // Read / Write

    #region Generate

    protected override Container CreateContainer(int metaIndex, object? extra)
    {
        if (metaIndex == 0)
            return new Container(metaIndex)
            {
                DataFile = new FileInfo(Path.Combine(Location.FullName, "accountdata.hg")),
                MetaFile = new FileInfo(Path.Combine(Location.FullName, "mf_accountdata.hg")),
            };

        var steamIndex = metaIndex == Global.OFFSET_INDEX ? string.Empty : $"{metaIndex - 1}";
        return new Container(metaIndex)
        {
            DataFile = new FileInfo(Path.Combine(Location.FullName, $"save{steamIndex}.hg")),
            MetaFile = new FileInfo(Path.Combine(Location.FullName, $"mf_save{steamIndex}.hg")),
        };
    }

    #endregion

    #region Load

    protected override uint[] DecryptMeta(Container container, byte[] meta)
    {
        // TODO known method below does not work for account data even though goatfungus seems to be the same...
        if (!container.IsSave || meta.Length is not META_SIZE and not META_SIZE_WAYPOINT)
            return Array.Empty<uint>();

        uint hash = 0;
        var iterations = meta.Length == META_SIZE_WAYPOINT ? 6 : 8;
        var keys = GetKeys(container);
        var values = meta.GetUInt32();

        var lastIndex = values.Length - 1;

        for (int i = 0; i < iterations; i++)
        {
            hash += 0x9E3779B9;
            hash &= 0xFFFFFFFF;
        }
        for (int i = 0; i < iterations; i++)
        {
            var keysIndex = (int)(hash >> 2 & 3);
            var valuesIndex = lastIndex;
            var value = values[0];

            for (int j = lastIndex; j > 0; j--, valuesIndex--)
            {
                var j1 = (value >> 3) ^ ((values[valuesIndex - 1] & 0xFFFFFFF) << 4);
                var j2 = ((value * 4) & 0xFFFFFFFF) ^ (values[valuesIndex - 1] >> 5);
                var j3 = (values[valuesIndex - 1] ^ keys[(j & 3) ^ keysIndex]);
                var j4 = (value ^ hash);
                values[valuesIndex] = (values[valuesIndex] - ((j1 + j2) ^ (j3 + j4))) & 0xFFFFFFFF;
                value = values[valuesIndex];
            }

            valuesIndex = lastIndex;

            var i1 = (value >> 3) ^ ((values[valuesIndex] & 0xFFFFFFF) << 4);
            var i2 = ((value * 4) & 0xFFFFFFFF) ^ (values[valuesIndex] >> 5);
            var i3 = (values[valuesIndex] ^ keys[keysIndex]);
            var i4 = (value ^ hash);
            values[0] = (values[0] - ((i1 + i2) ^ (i3 + i4))) & 0xFFFFFFFF;

            hash += 0x61C88647;
        }

        container.Steam = new()
        {
#if NETSTANDARD2_0
            MetaTail = values.Skip(META_KNOWN).ToArray()
#else
            MetaTail = values[META_KNOWN..],
#endif
        };

        return values;
    }

    /// <summary>
    /// Gets the necessary keys for the meta file encryption.
    /// </summary>
    /// <param name="container"></param>
    /// <returns></returns>
    protected static uint[] GetKeys(Container container)
    {
        var bytes = Encoding.ASCII.GetBytes("NAESEVADNAYRTNRG");
        var index = (uint)(container.MetaIndex) ^ 0x1422CB8C;

        var unsignedArray = bytes.GetUInt32();
        unsignedArray[0] = ((RotateLeft(index, 13) * 5) + 0xE6546B64) & 0xFFFFFFFF;

        return unsignedArray;
    }

    protected static uint RotateLeft(uint value, int bits)
    {
        return (value << bits) | (value >> (32 - bits));
    }

    protected override byte[] DecompressData(Container container, uint[] meta, byte[] data)
    {
#if NETSTANDARD2_0
        // No compression for account data and before Frontiers.
        if (!container.IsSave || data.Take(4).GetUInt32().FirstOrDefault() != Globals.Constant.HEADER_SAVE_STREAMING_CHUNK)
            return data;
#else
        // No compression for account data and before Frontiers.
        if (!container.IsSave || data[..4].GetUInt32().FirstOrDefault() != Globals.Constant.HEADER_SAVE_STREAMING_CHUNK)
            return data;
#endif

        var result = new List<byte>();

        var offset = 0;
        while (offset < data.Length)
        {
            var chunkHeader = data.Skip(offset).Take(Globals.Constant.SAVE_STREAMING_HEADER_SIZE).GetUInt32();
            offset += Globals.Constant.SAVE_STREAMING_HEADER_SIZE;

            var chunkCompressed = (int)(chunkHeader[1]);
            var chunkDecompressed = (int)(chunkHeader[2]);

            var source = data.Skip(offset).Take(chunkCompressed).ToArray();
            offset += chunkCompressed;

            _ = Globals.LZ4.Decode(source, out byte[] target, chunkDecompressed);
            result.AddRange(target);
        }

        return result.ToArray();
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
            var source = data.Skip(offset).Take(Globals.Constant.SAVE_STREAMING_CHUNK_SIZE).ToArray();
            _ = Globals.LZ4.Encode(source, out byte[] target);

            offset += Globals.Constant.SAVE_STREAMING_CHUNK_SIZE;

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
        //  6. SHA256 HASH          ( 32)
        // 14. DECOMPRESSED SIZE    (  4)
        // 15. COMPRESSED SIZE      (  4)
        // 16. PROFILE HASH         (  4)
        // 17. SAVE VERSION         (  4)
        // 18. GAME MODE            (  2)
        // 18. SEASON               (  2)
        // 19. TOTAL PLAY TIME      (  4)
        // 20. UNKNOWN              ( 24) // Foundation
        //                          (104)

        // 20. UNKNOWN              (280) // Waypoint
        //                          (360)

        // TODO known method below does not work for account data even though goatfungus seems to be the same...
        if (!container.IsSave)
            return ReadMeta(container);

        // META_KNOWN and Steam.MetaTail are using uint and therefore need to be multiplied by 4 to get the actual buffer size.
        var bufferSize = container.Steam?.MetaTail is not null ? (META_KNOWN + container.Steam!.MetaTail.Length) * 4 : (container.IsWaypoint ? META_SIZE_WAYPOINT : META_SIZE);
        var buffer = new byte[bufferSize];

        using var writer = new BinaryWriter(new MemoryStream(buffer));
        writer.Write(META_HEADER); // 4

        if (!container.IsFrontiers)
        {
#if NETSTANDARD2_0_OR_GREATER
            var sha256 = SHA256.Create().ComputeHash(data);
#else
            var sha256 = SHA256.HashData(data);
#endif

            var spookyHash = new SpookyHash(0x155AF93AC304200, 0x8AC7230489E7FFFF);
            spookyHash.Update(sha256);
            spookyHash.Update(data);
            spookyHash.Final(out ulong hash1, out ulong hash2);

            writer.Write(Globals.Constant.SAVE_FORMAT_2); // 4

            writer.Write(hash1); // 8
            writer.Write(hash2); // 8

            writer.Write(sha256); // 256 / 8 = 32
        }
        else
        {
            writer.Write(Globals.Constant.SAVE_FORMAT_3); // 4

            // SPOOKY HASH and SHA256 HASH not used.
            writer.Seek(0x30, SeekOrigin.Current);

            writer.Write(decompressedSize); // 4

            // COMPRESSED SIZE and PROFILE HASH not used.
            writer.Seek(0x8, SeekOrigin.Current);

            writer.Write(container.BaseVersion); // 4
            writer.Write((ushort)(container.GameModeEnum ?? 0)); // 2
            writer.Write((ushort)(container.SeasonEnum)); // 2
            writer.Write(container.TotalPlayTime); // 4
        }

        // Seek to position of last known byte and append the tail.
        if (container.Steam!.MetaTail is not null)
        {
            writer.Seek(META_KNOWN, SeekOrigin.Begin);

            foreach (var value in container.Steam!.MetaTail) // 24 or 280
                writer.Write(value);
        }

        return EncryptMeta(container, data, CompressMeta(container, data, buffer));
    }

    protected override byte[] EncryptMeta(Container container, byte[] data, byte[] meta)
    {
        uint hash = 0;
        var iterations = container.IsWaypoint ? 6 : 8;
        var keys = GetKeys(container);
        var values = meta.GetUInt32();

        var lastIndex = values.Length - 1;
        uint value = 0;

        for (int i = 0; i < iterations; i++)
        {
            hash += 0x9E3779B9;

            var keysIndex = (int)((hash >> 2) & 3);
            var valuesIndex = 0;

            for (int j = 0; j < lastIndex; j++, valuesIndex++)
            {
                var j1 = (values[valuesIndex + 1] >> 3) ^ ((value & 0xFFFFFFF) << 4);
                var j2 = ((values[valuesIndex + 1] * 4) & 0xFFFFFFF) ^ (value >> 5);
                var j3 = (value ^ keys[(j & 3) ^ keysIndex]);
                var j4 = (values[valuesIndex + 1] ^ hash);
                values[valuesIndex] = (values[valuesIndex] + ((j1 + j2) ^ (j3 + j4))) & 0xFFFFFFF;
                value = values[valuesIndex];
            }

            var i1 = (values[0] >> 3) ^ ((value & 0xFFFFFFF) << 4);
            var i2 = ((values[0] * 4) & 0xFFFFFFF) ^ (value >> 5);
            var i3 = (value ^ keys[keysIndex ^ 1]);
            var i4 = (values[0] ^ hash);
            values[lastIndex] = (values[lastIndex] + ((i1 + i2) ^ (i3 + i4))) & 0xFFFFFFF;
            value = values[lastIndex];
        }

        return values.GetBytes();
    }

    #endregion

    // // File Operation

    #region Copy

    protected override bool GuardPlatformExtra(Container source)
    {
        return source.Steam is null;
    }

    protected override void CopyPlatformExtra(Container destination, Container source)
    {
        destination.Steam = new PlatformExtraSteam
        {
            MetaTail = source.Steam!.MetaTail,
        };
    }

    #endregion

    #region Transfer

    protected override void CreatePlatformExtra(Container destination, Container source)
    {
        destination.Steam = new PlatformExtraSteam
        {
            MetaTail = new uint[(source.IsWaypoint ? META_SIZE_WAYPOINT : META_SIZE) - META_KNOWN],
        };
    }

    #endregion

    // // User Identification

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

        var usesMapping = jsonObject.UsesMapping();

        var path = usesMapping ? $"PlayerStateData.PersistentPlayerBases[?({{0}})].Owner.{key}" : $"6f=.F?0[?({{0}})].3?K.{key}";
        var expressions = new[]
        {
            usesMapping ? $"@.BaseType.PersistentBaseTypes == '{PersistentBaseTypesEnum.HomePlanetBase}' || @.BaseType.PersistentBaseTypes == '{PersistentBaseTypesEnum.FreighterBase}'" : $"@.peI.DPp == '{PersistentBaseTypesEnum.HomePlanetBase}' || @.peI.DPp == '{PersistentBaseTypesEnum.FreighterBase}'", // only with own base
            usesMapping ? $"@.Owner.UID == '{_steamId}'" : $"@.3?K.K7E == '{_steamId}'", // only with specified value
        };

        return GetUserIdentificationIntersection(jsonObject, path, expressions);
    }

    protected override IEnumerable<JToken> GetUserIdentificationByDiscovery(JObject jsonObject, string key)
    {
        if (_steamId is null)
            return base.GetUserIdentificationByBase(jsonObject, key);

        var usesMapping = jsonObject.UsesMapping();

        var path = usesMapping ? $"DiscoveryManagerData.DiscoveryData-v1.Store.Record[?({{0}})].OWS.{key}" : $"fDu.ETO.OsQ.?fB[?({{0}})].ksu.{key}";
        var expressions = new[]
        {
            usesMapping ? $"@.OWS.UID == '{_steamId}'" : $"@.ksu.K7E == '{_steamId}'", // only with specified value
        };

        return GetUserIdentificationIntersection(jsonObject, path, expressions);
    }

    protected override IEnumerable<JToken> GetUserIdentificationBySettlement(JObject jsonObject, string key)
    {
        if (_steamId is null)
            return base.GetUserIdentificationByBase(jsonObject, key);

        var usesMapping = jsonObject.UsesMapping();

        var path = usesMapping ? $"PlayerStateData.SettlementStatesV2[?({{0}})].Owner.{key}" : $"6f=.GQA[?({{0}})].3?K.{key}";
        var expressions = new[]
        {
            usesMapping ? $"@.Owner.UID == '{_steamId}'" : $"@.3?K.K7E == '{_steamId}'", // only with specified value
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
}
