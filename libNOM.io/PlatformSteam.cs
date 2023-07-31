using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SpookilySharp;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace libNOM.io;


#region Extra

internal record class PlatformExtraSteam
{
    internal int MetaSize;

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

    protected override string? PlatformArchitecture
    {
        get
        {
            // On SteamDeck (with Proton) the Windows architecture is also used.
            // TODO: Verify
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return "Win|Final";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) // macOS
                return "Mac|Final";

            return null; // same as if not defined at all
        }
    }

    public override PlatformEnum PlatformEnum { get; } = PlatformEnum.Steam;

    protected override string? PlatformProcess
    {
        get
        {
            // On SteamDeck (with Proton) the Windows executable is also used.
            // TODO: Verify
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return @"steamapps\common\No Man's Sky\Binaries\NMS.exe";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) // macOS
                return @"steamapps/common/No Man's Sky/No Man's Sky.app/Contents/MacOS/No Man's Sky";

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
        {
            var dataFile = new FileInfo(Path.Combine(Location.FullName, "accountdata.hg"));
            return new Container(metaIndex)
            {
                DataFile = dataFile,
                LastWriteTime = dataFile.LastWriteTime,
                MetaFile = new FileInfo(Path.Combine(Location.FullName, "mf_accountdata.hg")),
                /// Steam = ... // will be set in <see cref="DecryptMeta"/>
            };
        }
        else
        {
            var steamIndex = metaIndex == Globals.Constants.OFFSET_INDEX ? string.Empty : $"{metaIndex - 1}";

            var dataFile = new FileInfo(Path.Combine(Location.FullName, $"save{steamIndex}.hg"));
            return new Container(metaIndex)
            {
                DataFile = dataFile,
                LastWriteTime = dataFile.LastWriteTime,
                MetaFile = new FileInfo(Path.Combine(Location.FullName, $"mf_save{steamIndex}.hg")),
                /// Steam = ... // will be set in <see cref="DecryptMeta"/>
            };
        }
    }

    #endregion

    #region Load

    protected override uint[] DecryptMeta(Container container, byte[] meta)
    {
        uint hash = 0;
        int iterations = meta.Length == META_SIZE ? 8 : 6;
        uint[] key = GetKey(container);
        uint[] value = meta.GetUInt32();

        int lastIndex = value.Length - 1;

        for (int i = 0; i < iterations; i++)
        {
            // Results in 0xF1BBCDC8 for SAVE_FORMAT_2 as in the original algorithm.
            hash += 0x9E3779B9;
        }
        for (int i = 0; i < iterations; i++)
        {
            uint current = value[0];
            int keyIndex = (int)(hash >> 2 & 3);
            int valueIndex = lastIndex;

            for (int j = lastIndex; j > 0; j--, valueIndex--)
            {
                uint j1 = (current >> 3) ^ (value[valueIndex - 1] << 4);
                uint j2 = (current * 4) ^ (value[valueIndex - 1] >> 5);
                uint j3 = (value[valueIndex - 1] ^ key[(j & 3) ^ keyIndex]);
                uint j4 = (current ^ hash);
                value[valueIndex] -= (j1 + j2) ^ (j3 + j4);
                current = value[valueIndex];
            }

            valueIndex = lastIndex;

            uint i1 = (current >> 3) ^ (value[valueIndex] << 4);
            uint i2 = (current * 4) ^ (value[valueIndex] >> 5);
            uint i3 = (value[valueIndex] ^ key[keyIndex]);
            uint i4 = (current ^ hash);
            value[0] -= (i1 + i2) ^ (i3 + i4);

            hash += 0x61C88647;
        }

        container.Steam = new()
        {
            MetaSize = meta.Length,
#if NETSTANDARD2_0
            MetaTail = value.Skip(META_KNOWN).ToArray()
#else
            MetaTail = value[META_KNOWN..],
#endif
        };

        return value;
    }

    /// <summary>
    /// Gets the necessary key for the meta file encryption.
    /// </summary>
    /// <param name="container"></param>
    /// <returns></returns>
    private static uint[] GetKey(Container container)
    {
        uint index = (uint)(container.MetaIndex == 0 ? 1 : container.MetaIndex) ^ 0x1422CB8C;
        uint[] key = Encoding.ASCII.GetBytes("NAESEVADNAYRTNRG").GetUInt32();

        key[0] = (RotateLeft(index, 13) * 5) + 0xE6546B64;

        return key;
    }

    private static uint RotateLeft(uint value, int bits)
    {
        return (value << bits) | (value >> (32 - bits));
    }

    #endregion

    #region Write

    public override void Write(Container container, DateTimeOffset writeTime)
    {
        // Update Platform marker in save depending on the current operating system without changing the sync state.
        container.GetJsonObject()?.SetValue(PlatformArchitecture, "8>q", "Platform");
        base.Write(container, writeTime);
    }

    protected override byte[] CreateMeta(Container container, byte[] data, int decompressedSize)
    {
        //  0. META HEADER          (  4)
        //  1. META FORMAT          (  4)
        //  2. SPOOKY HASH          ( 16) // SAVE_FORMAT_2
        //  6. SHA256 HASH          ( 32) // SAVE_FORMAT_2
        // 14. DECOMPRESSED SIZE    (  4) // SAVE_FORMAT_3
        // 15. COMPRESSED SIZE      (  4) // SAVE_FORMAT_1
        // 16. PROFILE HASH         (  4) // SAVE_FORMAT_1
        // 17. SAVE VERSION         (  4) // SAVE_FORMAT_3
        // 18. GAME MODE            (  2) // SAVE_FORMAT_3
        // 18. SEASON               (  2) // SAVE_FORMAT_3
        // 19. TOTAL PLAY TIME      (  4) // SAVE_FORMAT_3
        // 20. UNKNOWN              ( 24) // SAVE_FORMAT_2
        //                          (104)

        // 20. UNKNOWN              (280) // SAVE_FORMAT_3
        //                          (360)

        var buffer = new byte[container.Steam!.MetaSize];

        // Editing account data is possible since Frontiers and therefore has always the new format.
        using var writer = new BinaryWriter(new MemoryStream(buffer));
        writer.Write(META_HEADER); // 4
        writer.Write((container.IsAccount || container.IsFrontiers ) ? Globals.Constants.SAVE_FORMAT_3 : Globals.Constants.SAVE_FORMAT_2); // 4

        if (container.IsSave && container.IsFrontiers) // SAVE_FORMAT_3
        {
            // SPOOKY HASH and SHA256 HASH not used.
            writer.Seek(0x30, SeekOrigin.Current);

            writer.Write(decompressedSize); // 4

            // COMPRESSED SIZE and PROFILE HASH not used.
            writer.Seek(0x8, SeekOrigin.Current);

            writer.Write(container.BaseVersion); // 4
            writer.Write((ushort)((container.GameModeEnum ?? 0) == 0 ? PresetGameModeEnum.Normal : container.GameModeEnum!)); // 2
            writer.Write((ushort)(container.SeasonEnum)); // 2
            writer.Write(container.TotalPlayTime); // 4
        }
        else // SAVE_FORMAT_2
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

            writer.Write(hash1); // 8
            writer.Write(hash2); // 8

            writer.Write(sha256); // 256 / 8 = 32
        }

        // Seek to position of last known byte and append the tail.
        if (container.Steam!.MetaTail is not null)
        {
            writer.Seek(META_KNOWN * sizeof(uint), SeekOrigin.Begin);

            foreach (var value in container.Steam!.MetaTail) // 24 or 280
                writer.Write(value);
        }

        return EncryptMeta(container, data, CompressMeta(container, data, buffer));
    }

    protected override byte[] EncryptMeta(Container container, byte[] data, byte[] meta)
    {
        uint current = 0;
        uint hash = 0;
        int iterations = container.Steam!.MetaSize == META_SIZE ? 8 : 6;
        uint[] key = GetKey(container);
        uint[] value = meta.GetUInt32();

        int lastIndex = value.Length - 1;

        for (int i = 0; i < iterations; i++)
        {
            hash += 0x9E3779B9;

            int keyIndex = (int)((hash >> 2) & 3);
            int valueIndex = 0;

            for (int j = 0; j < lastIndex; j++, valueIndex++)
            {
                uint j1 = (value[valueIndex + 1] >> 3) ^ (current << 4);
                uint j2 = (value[valueIndex + 1] * 4) ^ (current >> 5);
                uint j3 = (current ^ key[(j & 3) ^ keyIndex]);
                uint j4 = (value[valueIndex + 1] ^ hash);
                value[valueIndex] += (j1 + j2) ^ (j3 + j4);
                current = value[valueIndex];
            }

            uint i1 = (value[0] >> 3) ^ (current << 4);
            uint i2 = (value[0] * 4) ^ (current >> 5);
            uint i3 = (current ^ key[keyIndex ^ 1]);
            uint i4 = (value[0] ^ hash);
            value[lastIndex] += (i1 + i2) ^ (i3 + i4);
            current = value[lastIndex];
        }

        return value.GetBytes();
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
            MetaSize = source.Steam!.MetaSize,
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

    protected override IEnumerable<string> GetUserIdentificationByBase(JObject jsonObject, string key)
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

    protected override IEnumerable<string> GetUserIdentificationByDiscovery(JObject jsonObject, string key)
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

    protected override IEnumerable<string> GetUserIdentificationBySettlement(JObject jsonObject, string key)
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
