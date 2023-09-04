using CommunityToolkit.HighPerformance;
using libNOM.io.Services;
using Newtonsoft.Json.Linq;
using SpookilySharp;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace libNOM.io;


public partial class PlatformSteam : Platform
{
    #region Constant

    #region Platform Specific

    protected const uint META_HEADER = 0xEEEEEEBE; // 4008636094

    protected const int META_LENGTH_KNOWN = 0x58; // 88
    protected override int META_LENGTH_TOTAL_VANILLA => 0x68; // 104
    protected override int META_LENGTH_TOTAL_WAYPOINT => 0x168; // 360

    #endregion

    #region Generated Regex

#if NETSTANDARD2_0_OR_GREATER || NET6_0
    protected static readonly Regex AnchorFileRegex0 = new("save\\d{0,2}\\.hg", RegexOptions.Compiled);
#else
    [GeneratedRegex("save\\d{0,2}\\.hg", RegexOptions.Compiled)]
    protected static partial Regex AnchorFileRegex0();
#endif

    #endregion

    #region Directory Data

    protected const string ACCOUNT_PATTERN = "st_76561198*";

    protected static readonly string[] ANCHOR_FILE_GLOB = new[] { "save*.hg" };
#if NETSTANDARD2_0_OR_GREATER || NET6_0
    protected static readonly Regex[] ANCHOR_FILE_REGEX = new Regex[] { AnchorFileRegex0 };
#else
    protected static readonly Regex[] ANCHOR_FILE_REGEX = new Regex[] { AnchorFileRegex0() };
#endif

    protected static readonly string PATH = ((Func<string>)(() =>
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

    #endregion

    #region Field

    private SteamService? _steamService;
    private string? _steamId;

    #endregion

    #region Property

    private SteamService SteamService => _steamService ??= new(); // { get; }

    #region Configuration

    // public //

    public override PlatformEnum PlatformEnum { get; } = PlatformEnum.Steam;

    // protected //

    protected override string[] PlatformAnchorFileGlob { get; } = ANCHOR_FILE_GLOB;

    protected override Regex[] PlatformAnchorFileRegex { get; } = ANCHOR_FILE_REGEX;

    protected override string? PlatformArchitecture // { get; }
    {
        get
        {
            // On SteamDeck (with Proton) the Windows architecture is also used.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return "Win|Final";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) // macOS
                return "Mac|Final";

            return null; // same as if not defined at all
        }
    }

    protected override string? PlatformProcessPath // { get; }
    {
        get
        {
            // On SteamDeck (with Proton) the Windows executable is also used.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return @"steamapps\common\No Man's Sky\Binaries\NMS.exe";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) // macOS
                return @"steamapps/common/No Man's Sky/No Man's Sky.app/Contents/MacOS/No Man's Sky";

            return null; // same as if not defined at all
        }
    }

    protected override string PlatformToken { get; } = "ST";

    #endregion

    #region Flags

    // public //

    public override bool HasModding { get; } = true;

    public override bool RestartToApply { get; } = false;

    // protected //

    protected override bool CanCreate { get; } = true;

    protected override bool CanRead { get; } = true;

    protected override bool CanUpdate { get; } = true;

    protected override bool CanDelete { get; } = true;

    protected override bool IsConsolePlatform { get; } = false;

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
        if (directory is not null && directory.Name.Length == 20 && directory.Name.StartsWith(ACCOUNT_PATTERN.Substring(0, ACCOUNT_PATTERN.Length - 1)) && directory.Name.Substring(11).All(char.IsDigit))
            _steamId = directory.Name.Substring(3); // remove "st_"
#else
        if (directory is not null && directory.Name.Length == 20 && directory.Name.StartsWith(ACCOUNT_PATTERN[..^1]) && directory.Name[11..].All(char.IsDigit))
            _steamId = directory.Name[3..]; // remove "st_"
#endif

        base.InitializeComponent(directory, platformSettings);
    }

    #endregion

    // // Read / Write

    #region Generate

    private protected override Container CreateContainer(int metaIndex, PlatformExtra? extra)
    {
        var steamIndex = metaIndex == Constants.OFFSET_INDEX ? string.Empty : $"{metaIndex - 1}";
        var name = metaIndex == 0 ? "accountdata.hg" : $"save{steamIndex}.hg";
        var data = new FileInfo(Path.Combine(Location.FullName, name));
        var meta = new FileInfo(Path.Combine(Location.FullName, $"mf_{name}"));

        return new Container(metaIndex)
        {
            DataFile = data,
            MetaFile = meta,
            /// Additional values will be set in <see cref="UpdateContainerWithMetaInformation"/> and <see cref="UpdateContainerWithDataInformation"/>.
            Extra = new()
            {
                LastWriteTime = data.LastWriteTime,
            },
        };
    }

    #endregion

    #region Load

    protected override Span<uint> DecryptMeta(Container container, Span<byte> meta)
    {
        uint hash = 0;
        int iterations = meta.Length == META_LENGTH_TOTAL_VANILLA ? 8 : 6;
        ReadOnlySpan<uint> key = GetKey(container);
        Span<uint> value = base.DecryptMeta(container, meta);

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

        return value;
    }

    /// <summary>
    /// Gets the necessary key for the meta file encryption.
    /// </summary>
    /// <param name="container"></param>
    /// <returns></returns>
    private static Span<uint> GetKey(Container container)
    {
        Span<uint> key = Encoding.ASCII.GetBytes("NAESEVADNAYRTNRG").AsSpan().Cast<byte, uint>();
        key[0] = (RotateLeft((uint)(container.PersistentStorageSlot) ^ 0x1422CB8C, 13) * 5) + 0xE6546B64;
        return key;
    }

    private static uint RotateLeft(uint value, int bits)
    {
        return (value << bits) | (value >> (32 - bits));
    }

    protected override void UpdateContainerWithMetaInformation(Container container, ReadOnlySpan<byte> disk, ReadOnlySpan<uint> decompressed)
    {
        //  0. META HEADER          (  4)
        //  1. META FORMAT          (  4)
        //  2. SPOOKY HASH          ( 16) // SAVE_FORMAT_2
        //  6. SHA256 HASH          ( 32) // SAVE_FORMAT_2
        // 14. DECOMPRESSED SIZE    (  4) // SAVE_FORMAT_3
        // 15. COMPRESSED SIZE      (  4) // SAVE_FORMAT_1
        // 16. PROFILE HASH         (  4) // SAVE_FORMAT_1
        // 17. BASE VERSION         (  4) // SAVE_FORMAT_3
        // 18. GAME MODE            (  2) // SAVE_FORMAT_3
        // 18. SEASON               (  2) // SAVE_FORMAT_3
        // 19. TOTAL PLAY TIME      (  4) // SAVE_FORMAT_3
        // 20. EMPTY                (  8)

        // 22. EMPTY                ( 16) // SAVE_FORMAT_2
        //                          (104)

        // 22. SAVE NAME            (128) // SAVE_FORMAT_3 // may contain additional junk data after null terminator
        // 54. SAVE SUMMARY         (128) // SAVE_FORMAT_3 // may contain additional junk data after null terminator
        // 86. DIFFICULTY PRESET    (  1) // SAVE_FORMAT_3
        // 86. EMPTY                ( 15) // SAVE_FORMAT_3 // may contain additional junk data
        //                          (360)

        // Do not write wrong data in case decrypting failed.
        if (decompressed.TryGetValue(0, out var header) && header == META_HEADER)
        {
            // Vanilla data always available but not always set depending on the SAVE_FORMAT.
            container.Extra = container.Extra with
            {
                MetaFormat = disk.Length == META_LENGTH_TOTAL_VANILLA ? (decompressed[1] == Constants.SAVE_FORMAT_2 ? MetaFormatEnum.Foundation : (decompressed[1] == Constants.SAVE_FORMAT_3 ? MetaFormatEnum.Frontiers : MetaFormatEnum.Unknown)) : (disk.Length == META_LENGTH_TOTAL_WAYPOINT ? MetaFormatEnum.Waypoint : MetaFormatEnum.Unknown),
                Bytes = disk.Slice(META_LENGTH_KNOWN).ToArray(),
                SizeDecompressed = decompressed[14],
                BaseVersion = (int)(decompressed[17]),
                GameMode = disk.Cast<ushort>(72),
                Season = disk.Cast<ushort>(74),
                TotalPlayTime = decompressed[19],
            };

            // Extended data since Waypoint.
            if (disk.Length == META_LENGTH_TOTAL_WAYPOINT)
            {
                container.Extra = container.Extra with
                {
                    SaveName = decompressed.Slice(22, 32).GetSaveRenamingString(),
                    SaveSummary = decompressed.Slice(54, 32).GetSaveRenamingString(),
                    DifficultyPreset = disk[344],
                };
            }

            // Only write if all three values are in their valid ranges.
            if (container.Extra.BaseVersion.IsBaseVersion() && container.Extra.GameMode.IsGameMode() && container.Extra.Season.IsSeason())
                container.SaveVersion = Calculate.CalculateSaveVersion(container);
        }

        // Size is save to write always.
        container.Extra = container.Extra with { Size = (uint)(disk.Length) };
    }

    #endregion

    #region Write

    public override void Write(Container container, DateTimeOffset writeTime)
    {
        // Update PlatformArchitecture in save depending on the current operating system without changing the sync state.
        container.GetJsonObject().SetValue(PlatformArchitecture, "8>q", "Platform");
        base.Write(container, writeTime);
    }

    protected override Span<uint> CreateMeta(Container container, ReadOnlySpan<byte> data)
    {
        var buffer = new byte[GetMetaSize(container)];

        // Editing account data is possible since Frontiers and therefore has always the new format.
        using var writer = new BinaryWriter(new MemoryStream(buffer));
        writer.Write(META_HEADER); // 4
        writer.Write((container.IsAccount || container.IsVersion360Frontiers) ? Constants.SAVE_FORMAT_3 : Constants.SAVE_FORMAT_2); // 4

        if (container.IsSave && container.MetaFormat >= MetaFormatEnum.Frontiers) // SAVE_FORMAT_3
        {
            // SPOOKY HASH and SHA256 HASH not used.
            writer.Seek(0x30, SeekOrigin.Current); // 16 + 32 = 48

            writer.Write(container.Extra.SizeDecompressed); // 4

            // COMPRESSED SIZE and PROFILE HASH not used.
            writer.Seek(0x8, SeekOrigin.Current); // 4 + 4 = 8

            writer.Write(container.BaseVersion); // 4
            writer.Write((ushort)(container.GameMode)); // 2
            writer.Write((ushort)(container.Season)); // 2
            writer.Write(container.TotalPlayTime); // 4

            // Skip EMPTY bytes.
            writer.Seek(0x8, SeekOrigin.Current); // 8

            // Extended data since Waypoint.
            if (container.MetaFormat >= MetaFormatEnum.Waypoint)
            {
                // Append cached bytes and modify afterwards.
                writer.Write(container.Extra.Bytes ?? Array.Empty<byte>()); // 272
#if NETSTANDARD2_0
                writer.Seek(META_LENGTH_KNOWN, SeekOrigin.Begin);
                writer.Write(container.SaveName.GetSaveRenamingBytes().ToArray()); // 128

                writer.Seek(META_LENGTH_KNOWN + Constants.SAVE_RENAMING_LENGTH, SeekOrigin.Begin);
                writer.Write(container.SaveSummary.GetSaveRenamingBytes().ToArray()); // 128
#else
                writer.Seek(META_LENGTH_KNOWN, SeekOrigin.Begin);
                writer.Write(container.SaveName.GetSaveRenamingBytes()); // 128

                writer.Seek(META_LENGTH_KNOWN + Constants.SAVE_RENAMING_LENGTH, SeekOrigin.Begin);
                writer.Write(container.SaveSummary.GetSaveRenamingBytes()); // 128
#endif
                writer.Seek(META_LENGTH_KNOWN + Constants.SAVE_RENAMING_LENGTH * 2, SeekOrigin.Begin);
                writer.Write((byte)(container.GameDifficulty)); // 1
            }
            else
            {
                writer.Write(container.Extra.Bytes ?? Array.Empty<byte>()); // 16
            }
        }
        else // SAVE_FORMAT_2
        {
#if NETSTANDARD2_0_OR_GREATER
            var sha256 = SHA256.Create().ComputeHash(data.ToArray());
#else
            var sha256 = SHA256.HashData(data);
#endif
            var spookyHash = new SpookyHash(0x155AF93AC304200, 0x8AC7230489E7FFFF);
            spookyHash.Update(sha256);
            spookyHash.Update(data.ToArray());
            spookyHash.Final(out ulong hash1, out ulong hash2);

            writer.Write(hash1); // 8
            writer.Write(hash2); // 8

            writer.Write(sha256); // 256 / 8 = 32

            // Seek to position of last known byte and append the cached bytes.
            writer.Seek(META_LENGTH_KNOWN, SeekOrigin.Begin);
            writer.Write(container.Extra.Bytes ?? Array.Empty<byte>()); // 16
        }

        return buffer.AsSpan().Cast<byte, uint>();
    }

    protected override ReadOnlySpan<byte> EncryptMeta(Container container, ReadOnlySpan<byte> data, Span<byte> meta)
    {
        uint current = 0;
        uint hash = 0;
        int iterations = container.MetaFormat < MetaFormatEnum.Waypoint ? 8 : 6;
        ReadOnlySpan<uint> key = GetKey(container);
        Span<uint> value = meta.Cast<byte, uint>();

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

        return value.Cast<uint, byte>();
    }

    #endregion

    // // File Operation

    // TODO Transfer Refactoring

    #region Transfer

    protected override void CreatePlatformExtra(Container destination, Container source)
    {
        destination.Extra = new()
        {
            Bytes = new byte[(source.IsVersion400Waypoint ? META_LENGTH_TOTAL_WAYPOINT : META_LENGTH_TOTAL_VANILLA) - META_LENGTH_KNOWN],
            Size = source.Extra.Size,
            SizeDecompressed = source.Extra.SizeDecompressed,
            SizeDisk = source.DataFile?.Exists == true ? (uint)(source.DataFile!.Length) : 0,
            LastWriteTime = source.Extra.LastWriteTime ?? DateTimeOffset.Now,
            BaseVersion = source.Extra.BaseVersion,
            GameMode = source.Extra.GameMode,
            Season = source.Extra.Season,
            TotalPlayTime = source.Extra.TotalPlayTime,
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

        // Get via API only if not found in-file.
        if (key is "USN" && _steamId is not null && Settings.UseExternalSourcesForUserIdentification)
            return GetUserIdentificationBySteam() ?? string.Empty;

        return result;
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
        // Ensure STEAM_API_KEY is a valid one.
        if (!Properties.Resources.STEAM_API_KEY.All(char.IsLetterOrDigit))
            return null;

        var task = SteamService.GetPersonaNameAsync(_steamId!);
        task.Wait();
        return task.Result;
    }

    #endregion
}
