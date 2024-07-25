using System.Security.Cryptography;
using System.Text;

using CommunityToolkit.HighPerformance;

using libNOM.io.Services;
using libNOM.io.Settings;

using Newtonsoft.Json.Linq;

using SpookilySharp;

namespace libNOM.io;


/// <summary>
/// Implementation for the Steam platform.
/// </summary>
// This partial class contains all related code.
public partial class PlatformSteam : Platform
{
    #region Constant

    internal const string ACCOUNT_PATTERN = "st_76561198*";

    internal static readonly string[] ANCHOR_FILE_PATTERN = ["save??.hg"];

    internal static readonly string PATH = ((Func<string>)(() =>
    {
        if (Common.IsWindows())
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HelloGames", "NMS");

        if (Common.IsLinux()) // SteamDeck
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "Steam", "steamapps", "compatdata", "275850", "pfx", "drive_c", "users", "steamuser", "Application Data", "HelloGames", "NMS");

        if (Common.IsMac())
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "HelloGames", "NMS");

        return string.Empty; // same as if not defined at all
    }))();

    protected static readonly uint[] META_ENCRYPTION_KEY = Encoding.ASCII.GetBytes("NAESEVADNAYRTNRG").AsSpan().Cast<byte, uint>().ToArray();
    protected const uint META_HEADER = 0xEEEEEEBE; // 4,008,636,094
    protected override int META_LENGTH_KNOWN_VANILLA => 0x58; // 88
    internal override int META_LENGTH_TOTAL_VANILLA => 0x68; // 104
    internal override int META_LENGTH_TOTAL_WAYPOINT => 0x168; // 360
    internal override int META_LENGTH_TOTAL_WORLDS => 0x180; // 384

    #endregion

    #region Field

    private SteamService? _steamService; // will be set if SteamService is accessed

    #endregion

    // Property

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

    public override RestartRequirementEnum RestartToApply { get; } = RestartRequirementEnum.AccountOnly;

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

    // Initialize

    #region Constructor

    public PlatformSteam() : base() { }

    public PlatformSteam(string? path) : base(path) { }

    public PlatformSteam(string? path, PlatformSettings? platformSettings) : base(path, platformSettings) { }

    public PlatformSteam(PlatformSettings? platformSettings) : base(platformSettings) { }

    public PlatformSteam(DirectoryInfo? directory) : base(directory) { }

    public PlatformSteam(DirectoryInfo? directory, PlatformSettings? platformSettings) : base(directory, platformSettings) { }

    #endregion

    #region Initialize

    protected override void InitializeWatcher()
    {
        base.InitializeWatcher();

        // Files can have 0 or 1 or 2 numbers in its name.
#if NETSTANDARD2_0_OR_GREATER
        _watcher.Filter = PlatformAnchorFilePattern[AnchorFileIndex].Replace("??", "*");
#else
        _watcher.Filters.Add(PlatformAnchorFilePattern[AnchorFileIndex].Replace("??", "?"));
        _watcher.Filters.Add(PlatformAnchorFilePattern[AnchorFileIndex].Replace("??", string.Empty));
#endif
    }

    /// <seealso href="https://help.steampowered.com/en/faqs/view/2816-BE67-5B69-0FEC"/>
    protected override void InitializePlatformSpecific()
    {
        // Extract UID from directory name if possible.
        if (Location.Name.Length == 20 && Location.Name.StartsWith(ACCOUNT_PATTERN[..^1]) && Location.Name[(ACCOUNT_PATTERN.Length - 1)..].All(char.IsDigit))
            _uid = Location.Name[3..]; // remove "st_"
    }

    private protected override Container CreateContainer(int metaIndex, ContainerExtra? _)
    {
        var name = metaIndex == 0 ? "accountdata.hg" : $"save{(metaIndex == Constants.OFFSET_INDEX ? string.Empty : metaIndex - 1)}.hg";
        var data = new FileInfo(Path.Combine(Location.FullName, name));

        return new Container(metaIndex, this)
        {
            DataFile = data,
            MetaFile = new FileInfo(Path.Combine(Location.FullName, $"mf_{name}")),
            /// Additional values will be set in <see cref="UpdateContainerWithMetaInformation"/> and <see cref="Platform.UpdateContainerWithDataInformation"/>.
            Extra = new()
            {
                LastWriteTime = data.LastWriteTime,
            },
        };
    }

    #endregion

    #region Process

    protected override void UpdateContainerWithMetaInformation(Container container, ReadOnlySpan<byte> disk, ReadOnlySpan<uint> decompressed)
    {
        /**
          0. META HEADER          (  4)
          1. META FORMAT          (  4)
          2. SPOOKY HASH          ( 16) // META_FORMAT_2
          6. SHA256 HASH          ( 32) // META_FORMAT_2
         14. DECOMPRESSED SIZE    (  4) // META_FORMAT_4
         15. COMPRESSED SIZE      (  4) // META_FORMAT_4
         16. PROFILE HASH         (  4) // META_FORMAT_1
         17. BASE VERSION         (  4) // META_FORMAT_4
         18. GAME MODE            (  2) // META_FORMAT_4
         18. SEASON               (  2) // META_FORMAT_4
         19. TOTAL PLAY TIME      (  4) // META_FORMAT_4
         20. EMPTY                (  8)

         22. EMPTY                ( 16) // META_FORMAT_2
                                  (104)

         22. SAVE NAME            (128) // META_FORMAT_4 // may contain additional junk data after null terminator
         54. SAVE SUMMARY         (128) // META_FORMAT_4 // may contain additional junk data after null terminator

         86. DIFFICULTY PRESET    (  1) // META_FORMAT_3
         86. EMPTY                ( 15) // META_FORMAT_3 // may contain additional junk data
                                  (360)

         86. DIFFICULTY PRESET    (  4) // META_FORMAT_4
         87. SLOT IDENTIFIER      (  8) // META_FORMAT_4
         89. TIMESTAMP            (  4) // META_FORMAT_4
         90. META FORMAT          (  4) // META_FORMAT_4
         91. EMPTY                ( 20) // META_FORMAT_4
                                  (384)
         */

        // Do not write wrong data in case a step before failed.
        if (decompressed.TryGetValue(0, out var header) && header == META_HEADER)
        {
            // Vanilla metadata always available but not always set depending on the META_FORMAT.
            container.Extra = container.Extra with
            {
                Bytes = disk[META_LENGTH_KNOWN_VANILLA..].ToArray(),
                SizeDecompressed = decompressed[14],
                BaseVersion = (int)(decompressed[17]),
                GameMode = disk.Cast<ushort>(72),
                Season = disk.Cast<ushort>(74),
                TotalPlayTime = decompressed[19],
            };

            if (container.IsAccount)
            {
                container.GameVersion = Meta.GameVersion.Get(this, disk.Length, decompressed[1]);
            }
            if (container.IsSave)
            {
                // Extended metadata since Waypoint 4.00.
                if (disk.Length == META_LENGTH_TOTAL_WAYPOINT)
                    UpdateContainerWithWaypointMetaInformation(container, disk);

                // Extended metadata since Worlds 5.00.
                if (disk.Length == META_LENGTH_TOTAL_WORLDS)
                    UpdateContainerWithWorldsMetaInformation(container, disk, decompressed);

                // GameVersion with BaseVersion only is not 100% accurate but good enough to calculate SaveVersion.
                container.SaveVersion = Meta.SaveVersion.Calculate(container, Meta.GameVersion.Get(container.Extra.BaseVersion));
            }
        }

        // Size is save to write always.
        container.Extra = container.Extra with { MetaLength = (uint)(disk.Length) };
    }

    protected override void UpdateContainerWithWorldsMetaInformation(Container container, ReadOnlySpan<byte> disk, ReadOnlySpan<uint> decompressed)
    {
        base.UpdateContainerWithWorldsMetaInformation(container, disk, decompressed);

        container.Extra = container.Extra with
        {
            LastWriteTime = DateTimeOffset.FromUnixTimeSeconds(decompressed[89]).ToLocalTime(),
        };
    }

    #endregion

    // //

    #region Read

    protected override Span<uint> DecryptMeta(Container container, Span<byte> meta)
    {
        var value = base.DecryptMeta(container, meta);

        if (meta.Length != META_LENGTH_TOTAL_VANILLA && meta.Length != META_LENGTH_TOTAL_WAYPOINT && meta.Length != META_LENGTH_TOTAL_WORLDS)
            return value;

        // Best case is that it works with the value of the file but in case it was moved manually, try all other values as well.
        var enumValues = new StoragePersistentSlotEnum[] { container.PersistentStorageSlot }.Concat(EnumExtensions.GetValues<StoragePersistentSlotEnum>().Where(i => i > StoragePersistentSlotEnum.AccountData && i != container.PersistentStorageSlot));

        foreach (var entry in enumValues)
        {
            // When overwriting META_ENCRYPTION_KEY[0] it can happen that the value is not set afterwards and therefore create a new collection to ensure it will be correct.
            ReadOnlySpan<uint> key = [(((uint)(entry) ^ 0x1422CB8C).RotateLeft(13) * 5) + 0xE6546B64, META_ENCRYPTION_KEY[1], META_ENCRYPTION_KEY[2], META_ENCRYPTION_KEY[3]];

            // DeepCopy as value would be changed otherwise and casting again does not work.
            Span<uint> result = Common.DeepCopy(value);

            uint hash = 0;
            int iterations = meta.Length == META_LENGTH_TOTAL_VANILLA ? 8 : 6;
            int lastIndex = result.Length - 1;

            // Results in 0xF1BBCDC8 for META_FORMAT_2 as in the original algorithm.
            for (int i = 0; i < iterations; i++)
                hash += 0x9E3779B9;

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
                uint i3 = (result[valueIndex] ^ key[keyIndex]); // [(0 & 3) ^ keyIndex] as in j3 to be precise (0 would be the last executed index) but (0 & 3) is always zero and therefore does not matter
                uint i4 = (current ^ hash);
                result[0] -= (i1 + i2) ^ (i3 + i4);

                hash += 0x61C88647;
            }

            if (result[0] == META_HEADER)
                return result;
        }

        return value;
    }

    #endregion

    // //

    #region Write

    protected override void WritePlatformSpecific(Container container, DateTimeOffset writeTime)
    {
        // Update PlatformArchitecture in save depending on the current operating system without changing the sync state.
        container.GetJsonObject().SetValue(PlatformArchitecture, "PLATFORM");
        base.WritePlatformSpecific(container, writeTime);
    }

    protected override Span<uint> CreateMeta(Container container, ReadOnlySpan<byte> data)
    {
        var buffer = CreateMetaBuffer(container);

        // Editing account data is possible since Frontiers and therefore has always the new format.
        using var writer = new BinaryWriter(new MemoryStream(buffer));

        writer.Write(META_HEADER); // 4
        writer.Write(container.GameVersion switch // 4
        {
            >= GameVersionEnum.Worlds => Constants.META_FORMAT_4,
            >= GameVersionEnum.Frontiers => Constants.META_FORMAT_3,
            _ => Constants.META_FORMAT_2,
        });

        if (container.IsSave && container.IsVersion360Frontiers) // META_FORMAT_3 and META_FORMAT_4
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

            // Append buffered bytes that follow META_LENGTH_KNOWN_VANILLA.
            writer.Write(container.Extra.Bytes ?? []); // Extra.Bytes is 272 or 296

            OverwriteWaypointMeta(writer, container);
            OverwriteWorldsMeta(writer, container);
        }
        else // META_FORMAT_2
        {
            AppendHashes(writer, data); // 8 + 8 + 32 = 48

            // Seek to position of last known byte and append the cached bytes.
            writer.Seek(META_LENGTH_KNOWN_VANILLA, SeekOrigin.Begin);
            writer.Write(container.Extra.Bytes ?? []); // 16
        }

        return buffer.AsSpan().Cast<byte, uint>();
    }

    protected override void OverwriteWorldsMeta(BinaryWriter writer, Container container)
    {
        // Write appended.
        base.OverwriteWorldsMeta(writer, container);

        // Overwrite changed.
        if (container.IsVersion500Worlds)
        {
            // COMPRESSED SIZE is used again.
            writer.Seek(0x3C, SeekOrigin.Begin); // 4 + 4 + 16 + 32 = 48
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
        Span<uint> value = Common.DeepCopy(meta.Cast<byte, uint>());

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
            uint i3 = (current ^ key[(lastIndex & 3) ^ keyIndex]);
            uint i4 = (value[0] ^ hash);
            value[lastIndex] += (i1 + i2) ^ (i3 + i4);
            current = value[lastIndex];
        }

        return value.Cast<uint, byte>();
    }

    #endregion

    // //

    #region UserIdentification

    protected override string GetUserIdentification(JObject jsonObject, string key)
    {
        // Base call not as default as _uid can also be null.
        var result = key switch
        {
            "LID" => _uid,
            "UID" => _uid,
            _ => null,
        } ?? base.GetUserIdentification(jsonObject, key);

        // Get via API only if not found in-file.
        if (key == "USN" && string.IsNullOrEmpty(result) && Settings.UseExternalSourcesForUserIdentification && _uid is not null)
            result = GetUserIdentificationBySteam();

        return result ?? string.Empty;
    }

    /// <summary>
    /// Gets the <see cref="UserIdentification"/> information for the USN by calling the Steam Web-API.
    /// </summary>
    /// <returns></returns>
    private string? GetUserIdentificationBySteam()
    {
        // Ensure STEAM_API_KEY is a formal valid one.
        if (!Properties.Resources.STEAM_API_KEY.All(char.IsLetterOrDigit))
            return null;

        var task = SteamService.GetPersonaNameAsync(_uid!); // _uid has been checked before
        task.Wait();
        return task.Result;
    }

    #endregion
}
