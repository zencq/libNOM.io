using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

using CommunityToolkit.HighPerformance;

using DeepCopy;

using libNOM.io.Services;

using Newtonsoft.Json.Linq;

using SpookilySharp;

namespace libNOM.io;


public partial class PlatformSteam : Platform
{
    #region Constant

    internal const string ACCOUNT_PATTERN = "st_76561198*";

    internal static readonly string[] ANCHOR_FILE_PATTERN = ["save*.hg"];

    internal static readonly string PATH = ((Func<string>)(() =>
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HelloGames", "NMS");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) // SteamDeck
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "Steam", "steamapps", "compatdata", "275850", "pfx", "drive_c", "users", "steamuser", "Application Data", "HelloGames", "NMS");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) // macOS
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "HelloGames", "NMS");

        return string.Empty; // same as if not defined at all
    }))();

    protected static readonly uint[] META_ENCRYPTION_KEY = Encoding.ASCII.GetBytes("NAESEVADNAYRTNRG").AsSpan().Cast<byte, uint>().ToArray();
    protected const uint META_HEADER = 0xEEEEEEBE; // 4.008.636.094
    protected override int META_LENGTH_KNOWN => 0x58; // 88
    internal override int META_LENGTH_TOTAL_VANILLA => 0x68; // 104
    internal override int META_LENGTH_TOTAL_WAYPOINT => 0x168; // 360

    #endregion

    #region Field

    private string? _steamId; // will be set if available in path
    private SteamService? _steamService; // will be set if SteamService is accessed

    #endregion

    #region Property

    #region Configuration

    private SteamService SteamService => _steamService ??= new(); // { get; }

    #endregion

    #region Flags

    // public //

    public override bool CanCreate { get; } = true;

    public override bool CanRead { get; } = true;

    public override bool CanUpdate { get; } = true;

    public override bool CanDelete { get; } = true;

    public override bool HasModding { get; } = true;

    public override bool RestartToApply { get; } = false;

    // protected //

    protected override bool IsConsolePlatform { get; } = false;

    #endregion

    #region Platform Indicator

    // public //

    public override PlatformEnum PlatformEnum { get; } = PlatformEnum.Steam;

    // protected //

    protected override string[] PlatformAnchorFilePattern { get; } = ANCHOR_FILE_PATTERN;

    // On SteamDeck (with Proton) the Windows architecture is also used.
    protected override string? PlatformArchitecture => Common.IsWindowsOrLinux() ? "Win|Final" : (Common.IsMac() ? "Mac|Final" : null); // { get; }

    // Same as the architecture but for the process.
    protected override string? PlatformProcessPath => Common.IsWindowsOrLinux() ? @"steamapps\common\No Man's Sky\Binaries\NMS.exe" : (Common.IsMac() ? @"steamapps/common/No Man's Sky/No Man's Sky.app/Contents/MacOS/No Man's Sky" : null); // { get; }

    protected override string PlatformToken { get; } = "ST";

    #endregion

    #endregion

    // //

    #region Constructor

    public PlatformSteam() : base() { }

    public PlatformSteam(string path) : base(path) { }

    public PlatformSteam(string path, PlatformSettings platformSettings) : base(path, platformSettings) { }

    public PlatformSteam(DirectoryInfo directory) : base(directory) { }

    public PlatformSteam(DirectoryInfo directory, PlatformSettings platformSettings) : base(directory, platformSettings) { }

    /// <seealso href="https://help.steampowered.com/en/faqs/view/2816-BE67-5B69-0FEC"/>
    protected override void InitializeComponent(DirectoryInfo? directory, PlatformSettings? platformSettings)
    {
        // Proceed to base method even if no directory.
#if NETSTANDARD2_0
        if (directory?.Name.Length == 20 && directory!.Name.StartsWith(ACCOUNT_PATTERN.Substring(0, ACCOUNT_PATTERN.Length - 1)) && directory!.Name.Substring(11).All(char.IsDigit)) // implicit directory is not null
            _steamId = directory.Name.Substring(3); // remove "st_"
#else
        if (directory?.Name.Length == 20 && directory!.Name.StartsWith(ACCOUNT_PATTERN[..^1]) && directory!.Name[11..].All(char.IsDigit)) // implicit directory is not null
            _steamId = directory.Name[3..]; // remove "st_"
#endif

        base.InitializeComponent(directory, platformSettings);
    }

    #endregion

    // // Read / Write

    #region Generate

    private protected override Container CreateContainer(int metaIndex, PlatformExtra? extra)
    {
        var name = metaIndex == 0 ? "accountdata.hg" : $"save{(metaIndex == Constants.OFFSET_INDEX ? string.Empty : metaIndex - 1)}.hg";
        var data = new FileInfo(Path.Combine(Location.FullName, name));

        return new Container(metaIndex, this)
        {
            DataFile = data,
            MetaFile = new FileInfo(Path.Combine(Location.FullName, $"mf_{name}")),
            /// Additional values will be set in <see cref="UpdateContainerWithMetaInformation"/> and <see cref="Platform.UpdateContainerWithDataInformation"/>.
            Extra = extra ?? new()
            {
                LastWriteTime = data.LastWriteTime,
            },
        };
    }

    #endregion

    #region Load

    protected override Span<uint> DecryptMeta(Container container, Span<byte> meta)
    {
        var value = base.DecryptMeta(container, meta).ToArray(); // needs to be an array for the deep copy

        if (meta.Length != META_LENGTH_TOTAL_VANILLA && meta.Length != META_LENGTH_TOTAL_WAYPOINT)
            return value;

        // Best case is that it works with the value of the file but in case it was moved manually, try all other values as well.
#if NETSTANDARD2_0_OR_GREATER
        var enumValues = new StoragePersistentSlotEnum[] { container.PersistentStorageSlot }.Concat(((StoragePersistentSlotEnum[])(Enum.GetValues(typeof(StoragePersistentSlotEnum)))).Where(i => i > StoragePersistentSlotEnum.AccountData && i != container.PersistentStorageSlot));
#else
        var enumValues = new StoragePersistentSlotEnum[] { container.PersistentStorageSlot }.Concat(Enum.GetValues<StoragePersistentSlotEnum>().Where(i => i > StoragePersistentSlotEnum.AccountData && i != container.PersistentStorageSlot));
#endif

        foreach (var entry in enumValues)
        {
            // When overwriting META_ENCRYPTION_KEY[0] it can happen that the value is not set afterwards and therefore create a new collection to ensure it will be correct.
            ReadOnlySpan<uint> key = [(((uint)(entry) ^ 0x1422CB8C).RotateLeft(13) * 5) + 0xE6546B64, META_ENCRYPTION_KEY[1], META_ENCRYPTION_KEY[2], META_ENCRYPTION_KEY[3]];

            // DeepCopy as value would be changed otherwise and casting again does not work.
            Span<uint> result = DeepCopier.Copy(value);

            uint hash = 0;
            int iterations = meta.Length == META_LENGTH_TOTAL_VANILLA ? 8 : 6;
            int lastIndex = result.Length - 1;

            for (int i = 0; i < iterations; i++)
            {
                // Results in 0xF1BBCDC8 for SAVE_FORMAT_2 as in the original algorithm.
                hash += 0x9E3779B9;
            }
            for (int i = 0; i < iterations; i++)
            {
                uint current = result[0];
                int keyIndex = (int)(hash >> 2 & 3);
                int valueIndex = lastIndex;

                for (int j = lastIndex; j > 0; j--, valueIndex--)
                {
                    uint j1 = (current >> 3) ^ (result[valueIndex - 1] << 4);
                    uint j2 = (current * 4) ^ (result[valueIndex - 1] >> 5);
                    uint j3 = (result[valueIndex - 1] ^ key[(j & 3) ^ keyIndex]);
                    uint j4 = (current ^ hash);
                    result[valueIndex] -= (j1 + j2) ^ (j3 + j4);
                    current = result[valueIndex];
                }

                valueIndex = lastIndex;

                uint i1 = (current >> 3) ^ (result[valueIndex] << 4);
                uint i2 = (current * 4) ^ (result[valueIndex] >> 5);
                uint i3 = (result[valueIndex] ^ key[keyIndex]);
                uint i4 = (current ^ hash);
                result[0] -= (i1 + i2) ^ (i3 + i4);

                hash += 0x61C88647;
            }

            if (result[0] == META_HEADER)
                return result;
        }

        return value;
    }

#if !NETSTANDARD2_0
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0057: Use range operator", Justification = "The range operator is not supported in netstandard2.0 and Slice() has no performance penalties.")]
#endif
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

        // Do not write wrong data in case a step before failed.
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
                    SaveName = disk.Slice(88, 128).GetSaveRenamingString(),
                    SaveSummary = disk.Slice(216, 128).GetSaveRenamingString(),
                    DifficultyPreset = disk[344],
                };
            }

            container.GameVersion = Meta.GameVersion.Get(container.Extra.BaseVersion); // not 100% accurate but good enough
            container.SaveVersion = Meta.SaveVersion.Calculate(container); // needs GameVersion
        }

        // Size is save to write always.
        container.Extra = container.Extra with { Size = (uint)(disk.Length) };
    }

    #endregion

    #region Write

    public override void Write(Container container, DateTimeOffset writeTime)
    {
        // Update PlatformArchitecture in save depending on the current operating system without changing the sync state.
        container.GetJsonObject().SetValue(PlatformArchitecture, "PLATFORM");
        base.Write(container, writeTime);
    }

    protected override Span<uint> CreateMeta(Container container, ReadOnlySpan<byte> data)
    {
        var buffer = new byte[container.MetaSize];

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

            // Insert trailing bytes and the extended Waypoint data.
            AddWaypointMeta(writer, container); // Extra.Bytes is 272 or 16
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
            writer.Write(container.Extra.Bytes ?? []); // 16
        }

        return buffer.AsSpan().Cast<byte, uint>();
    }

    protected override ReadOnlySpan<byte> EncryptMeta(Container container, ReadOnlySpan<byte> data, Span<byte> meta)
    {
        uint current = 0;
        uint hash = 0;
        int iterations = container.MetaFormat < MetaFormatEnum.Waypoint ? 8 : 6;
        ReadOnlySpan<uint> key = [(((uint)(container.PersistentStorageSlot) ^ 0x1422CB8C).RotateLeft(13) * 5) + 0xE6546B64, META_ENCRYPTION_KEY[1], META_ENCRYPTION_KEY[2], META_ENCRYPTION_KEY[3]];
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
