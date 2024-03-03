using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;

using libNOM.io;
using libNOM.io.Enums;
using libNOM.io.Globals;
using libNOM.io.Models;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace libNOM.test;


// Do not use System.Range for simplicity of the file and performace is not critical.
[TestClass]
[DeploymentItem("../../../Resources/TESTSUITE_ARCHIVE.zip")]
public class MicrosoftTest : CommonTestInitializeCleanup
{
    #region Constant

    protected const int CONTAINERSINDEX_HEADER = 0xE; // 14
    protected const long CONTAINERSINDEX_FOOTER = 0x10000000; // 268435456

    protected const int BLOBCONTAINER_HEADER = 0x4; // 4
    protected const int BLOBCONTAINER_COUNT = 0x2; // 2

    protected const int META_LENGTH_TOTAL_VANILLA = 0x18 / sizeof(uint); // 6
    protected const int META_LENGTH_TOTAL_WAYPOINT = 0x118 / sizeof(uint); // 70

    #endregion

    #region containers.index

    /// <see cref="PlatformMicrosoft.ParseContainersIndex()"/>
    private static void AssertContainersIndex(byte[] containersIndexA, byte[] containersIndexB, DateTimeOffset now, string path)
    {
        AssertAllAreEqual(1137, containersIndexA.Length, containersIndexB.Length);

        // Header Prefix (000 - 003)
        AssertAllAreEqual(CONTAINERSINDEX_HEADER, BitConverter.ToInt32(containersIndexA, 0), BitConverter.ToInt32(containersIndexB, 0));
        // Header Number of Containers (004 - 011)
        AssertAllAreEqual(7, BitConverter.ToInt64(containersIndexA, 4), BitConverter.ToInt64(containersIndexB, 4));
        // Header Game Identifier Length (012 - 015)
        var identifierLengthA = BitConverter.ToInt32(containersIndexA, 12);
        var identifierLengthB = BitConverter.ToInt32(containersIndexB, 12);
        AssertAllAreEqual(44, identifierLengthA, identifierLengthB);
        // Header Game Identifier (016 - 103)
        AssertAllAreEqual("HelloGames.NoMansSky_bs190hzg1sesy!NoMansSky", GetUnicode(containersIndexA.Skip(16).Take(identifierLengthA * 2)), GetUnicode(containersIndexB.Skip(16).Take(identifierLengthB * 2)));
        // Header Timestamp (104 - 111)
        Assert.AreEqual(638264331709588011, DateTimeOffset.FromFileTime(BitConverter.ToInt64(containersIndexA, 104)).UtcTicks);
        Assert.AreEqual(now.UtcTicks, DateTimeOffset.FromFileTime(BitConverter.ToInt64(containersIndexB, 104)).UtcTicks);
        // Header Sync State (112 - 115)
        Assert.AreEqual(3, BitConverter.ToInt32(containersIndexA, 112)); // MicrosoftIndexSyncStateEnum.Synced
        Assert.AreEqual(2, BitConverter.ToInt32(containersIndexB, 112)); // MicrosoftIndexSyncStateEnum.Modified
        // Header User Guid Length (116 - 119)
        var guidLengthA = BitConverter.ToInt32(containersIndexA, 116);
        var guidLengthB = BitConverter.ToInt32(containersIndexB, 116);
        AssertAllAreEqual(36, guidLengthA, guidLengthB);
        // Header User Guid (120 - 191)
        AssertAllAreEqual("d734a97e-2855-43d9-b848-0748c9010231", GetUnicode(containersIndexA.Skip(120).Take(guidLengthA * 2)), GetUnicode(containersIndexB.Skip(120).Take(guidLengthB * 2)));
        // Header Suffix (192 - 199)
        AssertAllAreEqual(CONTAINERSINDEX_FOOTER, BitConverter.ToInt64(containersIndexA, 192), BitConverter.ToInt64(containersIndexB, 192));

        // Account Identifier 1 Length (200 - 203)
        var accountLengthA1 = BitConverter.ToInt32(containersIndexA, 200);
        var accountLengthB1 = BitConverter.ToInt32(containersIndexB, 200);
        AssertAllAreEqual(11, accountLengthA1, accountLengthB1);
        // Account Identifier 1 (204 - 225)
        AssertAllAreEqual("AccountData", GetUnicode(containersIndexA.Skip(204).Take(accountLengthA1 * 2)), GetUnicode(containersIndexB.Skip(204).Take(accountLengthB1 * 2)));
        // Account Identifier 2 Length (226 - 229)
        var accountLengthA2 = BitConverter.ToInt32(containersIndexA, 226);
        var accountLengthB2 = BitConverter.ToInt32(containersIndexB, 226);
        AssertAllAreEqual(11, accountLengthA2, accountLengthB2);
        // Account Identifier 1 (230 - 251)
        AssertAllAreEqual("AccountData", GetUnicode(containersIndexA.Skip(230).Take(accountLengthA2 * 2)), GetUnicode(containersIndexB.Skip(230).Take(accountLengthB2 * 2)));
        // Account Sync Time Ticks Hex Length (252 - 255)
        var accountSyncLengthA = BitConverter.ToInt32(containersIndexA, 252);
        var accountSyncLengthB = BitConverter.ToInt32(containersIndexB, 252);
        AssertAllAreEqual(19, accountSyncLengthA, accountSyncLengthB);
        // Account Sync Time Ticks Hex (256 - 293)
        AssertAllAreEqual("\"0x8DB9207655AD8BC\"", GetUnicode(containersIndexA.Skip(256).Take(accountSyncLengthA * 2)), GetUnicode(containersIndexB.Skip(256).Take(accountSyncLengthB * 2)));
        // Account Container File Extension (294)
        Assert.AreEqual(23, containersIndexA[294]);
        Assert.AreEqual(24, containersIndexB[294]);
        // Account Sync State (295 - 298)
        AssertAllAreEqual(1, BitConverter.ToInt32(containersIndexA, 295)); // MicrosoftBlobSyncStateEnum.Synced
        AssertAllAreEqual(2, BitConverter.ToInt32(containersIndexB, 295)); // MicrosoftBlobSyncStateEnum.Modified
        // Account Directory Guid (299 - 314)
        AssertAllAreEqual("8C69E739E59646B995E48ACB5B01E16A", GetGuid(containersIndexA.Skip(299).Take(16)), GetGuid(containersIndexB.Skip(299).Take(16)));
        // Account Timestamp (315 - 322)
        Assert.AreEqual(638264331709580000 / TICK_DIVISOR, DateTimeOffset.FromFileTime(BitConverter.ToInt64(containersIndexA, 315)).UtcTicks / TICK_DIVISOR);
        Assert.AreEqual(now.UtcTicks / TICK_DIVISOR, DateTimeOffset.FromFileTime(BitConverter.ToInt64(containersIndexB, 315)).UtcTicks / TICK_DIVISOR);
        // Account Empty Space (323 - 330)
        AssertAllZero(containersIndexA.Skip(323).Take(8), containersIndexB.Skip(323).Take(8));
        // Account Total Size on Disk (331 - 338)
        var accountSizeDiskB = BitConverter.ToInt64(containersIndexB, 331);
        Assert.AreEqual(13712, accountSizeDiskB, 500);
        Assert.AreEqual(13712, BitConverter.ToInt64(containersIndexA, 331));

        var accountBlobA = File.ReadAllBytes(Path.Combine(path.Replace(nameof(Properties.Resources.TESTSUITE_ARCHIVE), $"{nameof(Properties.Resources.TESTSUITE_ARCHIVE)}_ZIP"), "8C69E739E59646B995E48ACB5B01E16A", "container.23"));
        var accountBlobB = File.ReadAllBytes(Path.Combine(path, "8C69E739E59646B995E48ACB5B01E16A", "container.24"));

        // Account Blob Prefix (000 - 003)
        AssertAllAreEqual(BLOBCONTAINER_HEADER, BitConverter.ToInt32(accountBlobA, 0), BitConverter.ToInt32(accountBlobB, 0));
        // Account Blob Number of Containers (004 - 007)
        AssertAllAreEqual(BLOBCONTAINER_COUNT, BitConverter.ToInt32(accountBlobA, 4), BitConverter.ToInt32(accountBlobB, 4));
        // Account Blob Data Identifier (008 - 015)
        AssertAllAreEqual("data", GetUnicode(accountBlobA.Skip(8).Take(4 * 2)), GetUnicode(accountBlobB.Skip(8).Take(4 * 2)));
        // Account Blob Empty Data Space (16 - 135)
        AssertAllZero(accountBlobA.Skip(16).Take(120), accountBlobB.Skip(16).Take(120));
        // Account Blob Data File Cloud (136 - 151)
        AssertAllAreEqual("DB416844920844AE9C1B9693597B9BC0", GetGuid(accountBlobA.Skip(136).Take(16)), GetGuid(accountBlobB.Skip(136).Take(16)));
        // Account Blob Data File Local (152 - 167)
        var accountBlobDataNameB = GetGuid(accountBlobB.Skip(152).Take(16));
        Assert.AreNotEqual("DB416844920844AE9C1B9693597B9BC0", accountBlobDataNameB);
        Assert.AreEqual("DB416844920844AE9C1B9693597B9BC0", GetGuid(accountBlobA.Skip(152).Take(16)));
        // Account Blob Meta Identifier (168 - 175)
        AssertAllAreEqual("meta", GetUnicode(accountBlobA.Skip(168).Take(4 * 2)), GetUnicode(accountBlobB.Skip(168).Take(4 * 2)));
        // Account Blob Empty Meta Space (176 - 295)
        AssertAllZero(accountBlobA.Skip(176).Take(120), accountBlobB.Skip(176).Take(120));
        // Account Blob Meta File Cloud (296 - 311)
        AssertAllAreEqual("2F87BC5994B24BF28F34369C6B0A2C2B", GetGuid(accountBlobA.Skip(296).Take(16)), GetGuid(accountBlobB.Skip(296).Take(16)));
        // Account Blob Meta File Local (312 - 327)
        var accountBlobMetaNameB = GetGuid(accountBlobB.Skip(312).Take(16));
        Assert.AreNotEqual("2F87BC5994B24BF28F34369C6B0A2C2B", accountBlobMetaNameB);
        Assert.AreEqual("2F87BC5994B24BF28F34369C6B0A2C2B", GetGuid(accountBlobA.Skip(312).Take(16)));

        var accountBlobDataA = new FileInfo(Path.Combine(path.Replace(nameof(Properties.Resources.TESTSUITE_ARCHIVE), $"{nameof(Properties.Resources.TESTSUITE_ARCHIVE)}_ZIP"), "8C69E739E59646B995E48ACB5B01E16A", "DB416844920844AE9C1B9693597B9BC0"));
        var accountBlobMetaA = new FileInfo(Path.Combine(path.Replace(nameof(Properties.Resources.TESTSUITE_ARCHIVE), $"{nameof(Properties.Resources.TESTSUITE_ARCHIVE)}_ZIP"), "8C69E739E59646B995E48ACB5B01E16A", "2F87BC5994B24BF28F34369C6B0A2C2B"));

        var accountBlobDataB = new FileInfo(Path.Combine(path, "8C69E739E59646B995E48ACB5B01E16A", accountBlobDataNameB));
        var accountBlobMetaB = new FileInfo(Path.Combine(path, "8C69E739E59646B995E48ACB5B01E16A", accountBlobMetaNameB));

        Assert.AreEqual(13712, accountBlobDataA.Length + accountBlobMetaA.Length);
        Assert.AreEqual(accountSizeDiskB, accountBlobDataB.Length + accountBlobMetaB.Length);

        var offset = 339;
        while (offset < containersIndexA.Length)
        {
            // Blob Identifier 1 Length
            var blobLengthA1 = BitConverter.ToInt32(containersIndexA, offset);
            var blobLengthB1 = BitConverter.ToInt32(containersIndexB, offset);
            Assert.AreEqual(blobLengthA1, blobLengthB1);

            offset += 4;

            // Blob Identifier 1
            Assert.AreEqual(GetUnicode(containersIndexA.Skip(offset).Take(blobLengthA1 * 2)), GetUnicode(containersIndexB.Skip(offset).Take(blobLengthB1 * 2)));

            offset += blobLengthA1 * 2;

            // Blob Identifier 2 Length
            var blobLengthA2 = BitConverter.ToInt32(containersIndexA, offset);
            var blobLengthB2 = BitConverter.ToInt32(containersIndexB, offset);
            Assert.AreEqual(blobLengthA2, blobLengthB2);

            offset += 4;

            // Blob Identifier 1
            Assert.AreEqual(GetUnicode(containersIndexA.Skip(offset).Take(blobLengthA2 * 2)), GetUnicode(containersIndexB.Skip(offset).Take(blobLengthB2 * 2)));

            offset += blobLengthA2 * 2;

            // Blob Sync Time Ticks Hex Length
            var blobSyncLengthA = BitConverter.ToInt32(containersIndexA, offset);
            var blobSyncLengthB = BitConverter.ToInt32(containersIndexB, offset);
            AssertAllAreEqual(blobSyncLengthA, blobSyncLengthB);

            offset += 4;

            // Blob Sync Time Ticks Hex
            Assert.AreEqual(GetUnicode(containersIndexA.Skip(offset).Take(blobSyncLengthA * 2)), GetUnicode(containersIndexB.Skip(offset).Take(blobSyncLengthB * 2)));

            offset += blobSyncLengthA * 2;

            // Blob Container File Extension
            var blobContainerExtensionA = containersIndexA[offset];
            var blobContainerExtensionB = containersIndexB[offset];
            Assert.AreEqual(blobContainerExtensionA, blobContainerExtensionB);

            offset += 1;

            // Blob Sync State
            AssertAllAreEqual(1, BitConverter.ToInt32(containersIndexA, offset), BitConverter.ToInt32(containersIndexB, offset)); // MicrosoftBlobSyncStateEnum.Synced

            offset += 4;

            // Blob Directory Guid
            var blobGuidA = GetGuid(containersIndexA.Skip(offset).Take(16));
            var blobGuidB = GetGuid(containersIndexB.Skip(offset).Take(16));
            Assert.AreEqual(blobGuidA, blobGuidB);

            offset += 16;

            // Blob Timestamp
            Assert.AreEqual(DateTimeOffset.FromFileTime(BitConverter.ToInt64(containersIndexA, offset)).UtcTicks, DateTimeOffset.FromFileTime(BitConverter.ToInt64(containersIndexB, offset)).UtcTicks);

            offset += 8;

            // Blob Empty Space
            AssertAllZero(containersIndexA.Skip(offset).Take(8), containersIndexB.Skip(offset).Take(8));


            offset += 8;

            // Blob Total Size on Disk
            var blobSizeDiskA = BitConverter.ToInt64(containersIndexA, offset);
            var blobSizeDiskB = BitConverter.ToInt64(containersIndexB, offset);
            Assert.AreEqual(blobSizeDiskA, blobSizeDiskB);

            offset += 8;

            var blobBlobA = File.ReadAllBytes(Path.Combine(path.Replace(nameof(Properties.Resources.TESTSUITE_ARCHIVE), $"{nameof(Properties.Resources.TESTSUITE_ARCHIVE)}_ZIP"), blobGuidA, $"container.{blobContainerExtensionA}"));
            var blobBlobB = File.ReadAllBytes(Path.Combine(path, blobGuidB, $"container.{blobContainerExtensionB}"));

            // Blob Blob Prefix (000 - 003)
            AssertAllAreEqual(BLOBCONTAINER_HEADER, BitConverter.ToInt32(blobBlobA, 0), BitConverter.ToInt32(blobBlobB, 0));
            // Blob Blob Number of Containers (004 - 007)
            AssertAllAreEqual(BLOBCONTAINER_COUNT, BitConverter.ToInt32(blobBlobA, 4), BitConverter.ToInt32(blobBlobB, 4));
            // Blob Blob Data Identifier (008 - 015)
            AssertAllAreEqual("data", GetUnicode(blobBlobA.Skip(8).Take(4 * 2)), GetUnicode(blobBlobB.Skip(8).Take(4 * 2)));
            // Blob Blob Empty Data Space (16 - 135)
            AssertAllZero(blobBlobA.Skip(16).Take(120), blobBlobB.Skip(16).Take(120));
            // Blob Blob Data File Cloud (136 - 151)
            Assert.AreEqual(GetGuid(blobBlobA.Skip(136).Take(16)), GetGuid(blobBlobB.Skip(136).Take(16)));
            // Blob Blob Data File Local (152 - 167)
            var blobBlobDataNameA = GetGuid(blobBlobA.Skip(152).Take(16));
            var blobBlobDataNameB = GetGuid(blobBlobB.Skip(152).Take(16));
            Assert.AreEqual(blobBlobDataNameA, blobBlobDataNameB);
            // Blob Blob Meta Identifier (168 - 175)
            AssertAllAreEqual("meta", GetUnicode(blobBlobA.Skip(168).Take(4 * 2)), GetUnicode(blobBlobB.Skip(168).Take(4 * 2)));
            // Blob Blob Empty Meta Space (176 - 295)
            AssertAllZero(blobBlobA.Skip(176).Take(120), blobBlobB.Skip(176).Take(120));
            // Blob Blob Meta File Cloud (296 - 311)
            Assert.AreEqual(GetGuid(blobBlobA.Skip(296).Take(16)), GetGuid(blobBlobB.Skip(296).Take(16)));
            // Blob Blob Meta File Local (312 - 327)
            var blobBlobMetaNameA = GetGuid(blobBlobA.Skip(312).Take(16));
            var blobBlobMetaNameB = GetGuid(blobBlobB.Skip(312).Take(16));
            Assert.AreEqual(blobBlobMetaNameA, blobBlobMetaNameB);

            var blobBlobDataA = new FileInfo(Path.Combine(path.Replace(nameof(Properties.Resources.TESTSUITE_ARCHIVE), $"{nameof(Properties.Resources.TESTSUITE_ARCHIVE)}_ZIP"), blobGuidA, blobBlobDataNameA));
            var blobBlobMetaA = new FileInfo(Path.Combine(path.Replace(nameof(Properties.Resources.TESTSUITE_ARCHIVE), $"{nameof(Properties.Resources.TESTSUITE_ARCHIVE)}_ZIP"), blobGuidA, blobBlobMetaNameA));

            var blobBlobDataB = new FileInfo(Path.Combine(path, blobGuidB, blobBlobDataNameB));
            var blobBlobMetaB = new FileInfo(Path.Combine(path, blobGuidB, blobBlobMetaNameB));

            Assert.AreEqual(blobSizeDiskA, blobBlobDataA.Length + blobBlobMetaA.Length);
            Assert.AreEqual(blobSizeDiskB, blobBlobDataB.Length + blobBlobMetaB.Length);
        }
    }

    #endregion

    #region Meta

    /// <see cref="Platform.ReadMeta(Container)"/>
    /// <see cref="PlatformMicrosoft.DecryptMeta(Container, byte[])"/>
    private static uint[] DecryptMeta(Container container)
    {
        byte[] meta = File.ReadAllBytes(container.MetaFile!.FullName);
        return ToUInt32(meta);
    }

    private static void AssertCommonMeta(Container container, uint[] metaA, uint[] metaB)
    {
        Assert.AreEqual(metaA.Length, metaB.Length);

        if (metaA.Length == META_LENGTH_TOTAL_VANILLA || metaA.Length == META_LENGTH_TOTAL_WAYPOINT)
        {
            if (container.IsAccount)
            {
                AssertAllAreEqual(1, metaA[0], metaB[0]);
                AssertAllZero(metaA.Skip(1).Take(3), metaB.Skip(1).Take(3));
                AssertAllNotZero(metaA[4], metaB[4]);
            }

            Assert.IsTrue(metaA.Skip(5).SequenceEqual(metaB.Skip(5)));
        }
        else
            throw new AssertFailedException();
    }

    #endregion

    [TestMethod]
    public void T01_Read_0009000000C73498()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "0009000000C73498_29070100B936489ABCE8B9AF3980429C");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum Season, int BaseVersion, GameVersionEnum Version)[]
        {
            (2, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, GameVersionEnum.Frontiers), // 2Auto
            (3, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, GameVersionEnum.Frontiers), // 2Manual
        };
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformMicrosoft(path, settings);

        // Assert
        Assert.IsTrue(platform.HasAccountData);
        Assert.AreEqual(results.Length, GetExistingContainers(platform).Count());
        Assert.AreEqual(userIdentification[0], platform.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification[1], platform.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification[2], platform.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK);

        for (var i = 0; i < results.Length; i++)
        {
            var container = GetOneSaveContainer(platform, results[i].CollectionIndex);
            var priect = new PrivateObject(container);

            Assert.AreEqual(results[i].Exists, container.Exists);
            Assert.AreEqual(results[i].IsOld, container.IsOld);
            Assert.AreEqual(results[i].GameMode, (PresetGameModeEnum)(priect.GetFieldOrProperty("GameMode")));
            Assert.AreEqual(results[i].GameDifficulty, container.Difficulty);
            Assert.AreEqual(results[i].Season, container.Season);
            Assert.AreEqual(results[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(results[i].Version, container.GameVersion);
        }
    }

    [TestMethod]
    public void T02_Read_000901F4E735CFAC()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "000901F4E735CFAC_29070100B936489ABCE8B9AF3980429C");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum Season, int BaseVersion, GameVersionEnum Version)[]
        {
            (0, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4138, GameVersionEnum.Outlaws), // 1Auto
            (1, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4138, GameVersionEnum.Outlaws), // 1Manual
            (2, true, false, PresetGameModeEnum.Seasonal, DifficultyPresetTypeEnum.Normal, SeasonEnum.Exobiology, 4137, GameVersionEnum.SentinelWithVehicleAI), // 2Auto
            (3, true, false, PresetGameModeEnum.Seasonal, DifficultyPresetTypeEnum.Normal, SeasonEnum.Exobiology, 4137, GameVersionEnum.SentinelWithVehicleAI), // 2Manual
            (4, true, false, PresetGameModeEnum.Seasonal, DifficultyPresetTypeEnum.Normal, SeasonEnum.Blighted, 4138, GameVersionEnum.Outlaws), // 3Auto
            (5, true, false, PresetGameModeEnum.Seasonal, DifficultyPresetTypeEnum.Normal, SeasonEnum.Blighted, 4138, GameVersionEnum.Outlaws), // 3Manual
        };
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformMicrosoft(path, settings);

        // Assert
        Assert.IsTrue(platform.HasAccountData);
        Assert.AreEqual(results.Length, GetExistingContainers(platform).Count());
        Assert.AreEqual(userIdentification[0], platform.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification[1], platform.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification[2], platform.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK);

        for (var i = 0; i < results.Length; i++)
        {
            var container = GetOneSaveContainer(platform, results[i].CollectionIndex);
            var priect = new PrivateObject(container);

            Assert.AreEqual(results[i].Exists, container.Exists);
            Assert.AreEqual(results[i].IsOld, container.IsOld);
            Assert.AreEqual(results[i].GameMode, (PresetGameModeEnum)(priect.GetFieldOrProperty("GameMode")));
            Assert.AreEqual(results[i].GameDifficulty, container.Difficulty);
            Assert.AreEqual(results[i].Season, container.Season);
            Assert.AreEqual(results[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(results[i].Version, container.GameVersion);
        }
    }

    [TestMethod]
    public void T03_Read_000901F8A36808E0()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "000901F8A36808E0_29070100B936489ABCE8B9AF3980429C");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum Season, int BaseVersion, GameVersionEnum Version, string SaveName, string SaveSummary)[]
        {
            (0, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4143, GameVersionEnum.Fractal, "1. Haupt", "Auf dem Frachter (WF-4 Dawajima)"), // 1Auto
            (1, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4143, GameVersionEnum.Fractal, "1. Haupt", "Auf dem Frachter (WF-4 Dawajima)"), // 1Manual
            (2, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Survival, SeasonEnum.None, 4142, GameVersionEnum.WaypointWithSuperchargedSlots, "", "An Bord von „Otfolk“-Station Majoris"), // 2Auto
            (3, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Survival, SeasonEnum.None, 4142, GameVersionEnum.WaypointWithSuperchargedSlots, "", "An Bord von „Otfolk“-Station Majoris"), // 2Manual
            (4, true, false, PresetGameModeEnum.Permadeath, DifficultyPresetTypeEnum.Permadeath, SeasonEnum.None, 4142, GameVersionEnum.WaypointWithSuperchargedSlots, "", "An Bord von Akelmon-Yelan Primus"), // 3Auto
            (5, true, false, PresetGameModeEnum.Permadeath, DifficultyPresetTypeEnum.Permadeath, SeasonEnum.None, 4142, GameVersionEnum.WaypointWithSuperchargedSlots, "", "An Bord von Akelmon-Yelan Primus"), // 3Manual
            (6, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4143, GameVersionEnum.Fractal, "2. Haupt", "Auf dem Frachter (WF-4 Dawajima)"), // 4Auto
            (7, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4143, GameVersionEnum.Fractal, "2. Haupt", "Auf dem Frachter (WF-4 Dawajima)"), // 4Manual
            (8, true, false, PresetGameModeEnum.Seasonal, DifficultyPresetTypeEnum.Normal, SeasonEnum.Utopia, 4143, GameVersionEnum.Fractal, "Utopia", "An Bord von „Pivogarde III“-Zentrum"), // 5Auto
            (9, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4143, GameVersionEnum.Fractal, "Utopia", "An Bord von „Pivogarde III“-Zentrum"), // 5Manual
        };
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformMicrosoft(path, settings);

        // Assert
        Assert.IsTrue(platform.HasAccountData);
        Assert.AreEqual(results.Length, GetExistingContainers(platform).Count());
        Assert.AreEqual(userIdentification[0], platform.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification[1], platform.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification[2], platform.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK);

        for (var i = 0; i < results.Length; i++)
        {
            var container = GetOneSaveContainer(platform, results[i].CollectionIndex);
            var priect = new PrivateObject(container);

            Assert.AreEqual(results[i].Exists, container.Exists);
            Assert.AreEqual(results[i].IsOld, container.IsOld);
            Assert.AreEqual(results[i].GameMode, (PresetGameModeEnum)(priect.GetFieldOrProperty("GameMode")));
            Assert.AreEqual(results[i].GameDifficulty, container.Difficulty);
            Assert.AreEqual(results[i].Season, container.Season);
            Assert.AreEqual(results[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(results[i].Version, container.GameVersion);
            Assert.AreEqual(results[i].SaveName, container.SaveName);
            Assert.AreEqual(results[i].SaveSummary, container.SaveSummary);
        }
    }

    [TestMethod]
    public void T04_Read_000901FB44140B02()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "000901FB44140B02_29070100B936489ABCE8B9AF3980429C");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum Season, int BaseVersion, GameVersionEnum Version)[]
        {
            (0, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4134, GameVersionEnum.PrismsWithBytebeatAuthor), // 1Auto
            (2, true, false, PresetGameModeEnum.Permadeath, DifficultyPresetTypeEnum.Permadeath, SeasonEnum.None, 4127, GameVersionEnum.Companions), // 2Auto
            (4, true, false, PresetGameModeEnum.Survival, DifficultyPresetTypeEnum.Survival, SeasonEnum.None, 4133, GameVersionEnum.Beachhead), // 3Auto
        };
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformMicrosoft(path, settings);

        // Assert
        Assert.IsFalse(platform.HasAccountData);
        Assert.AreEqual(results.Length, GetExistingContainers(platform).Count());
        Assert.AreEqual(userIdentification[0], platform.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification[1], platform.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification[2], platform.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK);

        var container1 = GetOneSaveContainer(platform, 1); // 1Manual
        Assert.IsFalse(container1.Exists);
        Assert.IsFalse(container1.IsOld);
        Assert.AreEqual(Constants.INCOMPATIBILITY_005, container1.IncompatibilityTag);

        for (var i = 0; i < results.Length; i++)
        {
            var container = GetOneSaveContainer(platform, results[i].CollectionIndex);
            var priect = new PrivateObject(container);

            Assert.AreEqual(results[i].Exists, container.Exists);
            Assert.AreEqual(results[i].IsOld, container.IsOld);
            Assert.AreEqual(results[i].GameMode, (PresetGameModeEnum)(priect.GetFieldOrProperty("GameMode")));
            Assert.AreEqual(results[i].GameDifficulty, container.Difficulty);
            Assert.AreEqual(results[i].Season, container.Season);
            Assert.AreEqual(results[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(results[i].Version, container.GameVersion);
        }
    }

    [TestMethod]
    public void T05_Read_000901FE2C5492FC()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "000901FE2C5492FC_29070100B936489ABCE8B9AF3980429C");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum Season, int BaseVersion, GameVersionEnum Version)[]
        {
            (1, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, GameVersionEnum.Emergence), // 1Manual
        };
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformMicrosoft(path, settings);

        // Assert
        Assert.IsFalse(platform.HasAccountData);
        Assert.AreEqual(results.Length, GetExistingContainers(platform).Count());
        Assert.AreEqual(userIdentification[0], platform.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification[1], platform.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification[2], platform.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK);

        var container0 = GetOneSaveContainer(platform, 0); // 1Auto
        Assert.IsFalse(container0.Exists);
        Assert.IsFalse(container0.IsOld);
        Assert.AreEqual(Constants.INCOMPATIBILITY_004, container0.IncompatibilityTag);

        for (var i = 0; i < results.Length; i++)
        {
            var container = GetOneSaveContainer(platform, results[i].CollectionIndex);
            var priect = new PrivateObject(container);

            Assert.AreEqual(results[i].Exists, container.Exists);
            Assert.AreEqual(results[i].IsOld, container.IsOld);
            Assert.AreEqual(results[i].GameMode, (PresetGameModeEnum)(priect.GetFieldOrProperty("GameMode")));
            Assert.AreEqual(results[i].GameDifficulty, container.Difficulty);
            Assert.AreEqual(results[i].Season, container.Season);
            Assert.AreEqual(results[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(results[i].Version, container.GameVersion);
        }
    }

    [TestMethod]
    public void T06_Read_000901FFCAB85905()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "000901FFCAB85905_29070100B936489ABCE8B9AF3980429C");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum Season, int BaseVersion, GameVersionEnum Version)[]
        {
            (0, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4138, GameVersionEnum.Outlaws), // 1Auto
            (1, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4138, GameVersionEnum.Outlaws), // 1Manual
        };
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformMicrosoft(path, settings);

        // Assert
        Assert.IsFalse(platform.HasAccountData);
        Assert.AreEqual(results.Length, GetExistingContainers(platform).Count());
        Assert.AreEqual(userIdentification[0], platform.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification[1], platform.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification[2], platform.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK);

        for (var i = 0; i < results.Length; i++)
        {
            var container = GetOneSaveContainer(platform, results[i].CollectionIndex);
            var priect = new PrivateObject(container);

            Assert.AreEqual(results[i].Exists, container.Exists);
            Assert.AreEqual(results[i].IsOld, container.IsOld);
            Assert.AreEqual(results[i].GameMode, (PresetGameModeEnum)(priect.GetFieldOrProperty("GameMode")));
            Assert.AreEqual(results[i].GameDifficulty, container.Difficulty);
            Assert.AreEqual(results[i].Season, container.Season);
            Assert.AreEqual(results[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(results[i].Version, container.GameVersion);
        }
    }

    [TestMethod]
    public void T07_Read_00090000025A963A()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "00090000025A963A_29070100B936489ABCE8B9AF3980429C");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum Season, int BaseVersion, GameVersionEnum Version, string SaveName, string SaveSummary)[]
        {
            (0, true, false, PresetGameModeEnum.Creative, DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4142, GameVersionEnum.WaypointWithSuperchargedSlots, "Test56789012345678901234567890123456789012", "An Bord von „Negfengf“-Station Majoris"), // 1Auto // for some reason SaveVersion has still the old format
            (1, true, false, PresetGameModeEnum.Creative, DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4140, GameVersionEnum.Waypoint, "Test56789012345678901234567890123456789012", "An Bord von „Negfengf“-Station Majoris"), // 1Manual
            (2, true, false, PresetGameModeEnum.Seasonal, DifficultyPresetTypeEnum.Normal, SeasonEnum.Polestar, 4139, GameVersionEnum.Endurance, "", ""), // 2Auto
            (6, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, GameVersionEnum.Emergence, "", ""), // 4Auto
            (7, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4124, GameVersionEnum.LivingShip, "", ""), // 4Manual
        };
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformMicrosoft(path, settings);

        // Assert
        Assert.IsTrue(platform.HasAccountData);
        Assert.AreEqual(results.Length, GetExistingContainers(platform).Count());
        Assert.AreEqual(userIdentification[0], platform.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification[1], platform.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification[2], platform.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK);

        for (var i = 0; i < results.Length; i++)
        {
            var container = GetOneSaveContainer(platform, results[i].CollectionIndex);
            var priect = new PrivateObject(container);

            Assert.AreEqual(results[i].Exists, container.Exists);
            Assert.AreEqual(results[i].IsOld, container.IsOld);
            Assert.AreEqual(results[i].GameMode, (PresetGameModeEnum)(priect.GetFieldOrProperty("GameMode")));
            Assert.AreEqual(results[i].GameDifficulty, container.Difficulty);
            Assert.AreEqual(results[i].Season, container.Season);
            Assert.AreEqual(results[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(results[i].Version, container.GameVersion);
            Assert.AreEqual(results[i].SaveName, container.SaveName);
            Assert.AreEqual(results[i].SaveSummary, container.SaveSummary);
        }
    }

    /// <summary>
    /// Same as <see cref="T06_Read_000901FFCAB85905"/>.
    /// </summary>
    [TestMethod]
    public void T08_Read_NoAccountInDirectory()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "something");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum Season, int BaseVersion, GameVersionEnum Version)[]
        {
            (0, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4138, GameVersionEnum.Outlaws), // 1Auto
            (1, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4138, GameVersionEnum.Outlaws), // 1Manual
        };
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformMicrosoft(path, settings);

        // Assert
        Assert.IsFalse(platform.HasAccountData);
        Assert.AreEqual(results.Length, GetExistingContainers(platform).Count());
        Assert.AreEqual(userIdentification[0], platform.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification[1], platform.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification[2], platform.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK);

        for (var i = 0; i < results.Length; i++)
        {
            var container = GetOneSaveContainer(platform, results[i].CollectionIndex);
            var priect = new PrivateObject(container);

            Assert.AreEqual(results[i].Exists, container.Exists);
            Assert.AreEqual(results[i].IsOld, container.IsOld);
            Assert.AreEqual(results[i].GameMode, (PresetGameModeEnum)(priect.GetFieldOrProperty("GameMode")));
            Assert.AreEqual(results[i].GameDifficulty, container.Difficulty);
            Assert.AreEqual(results[i].Season, container.Season);
            Assert.AreEqual(results[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(results[i].Version, container.GameVersion);
        }
    }

    [TestMethod]
    public void T10_Write_Default_ContainersIndex()
    {
        var now = DateTimeOffset.UtcNow;
        var nowTicks = NullifyTicks(now).UtcTicks;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "00090000025A963A_29070100B936489ABCE8B9AF3980429C");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            UseMapping = true,
        };
        var writeCallback = false;

        // Act
        var platformA = new PlatformMicrosoft(path, settings);
        var containerA = platformA.GetAccountContainer()!;
        var containersIndexA = File.ReadAllBytes(Path.Combine(path, "containers.index"));

        containerA.WriteCallback += () =>
        {
            writeCallback = true;
        };

#pragma warning disable IDE0042 // Deconstruct variable declaration
        platformA.Load(containerA);
        (int MusicVolume, long UtcTicks) valuesOrigin = (containerA.GetJsonValue<int>(MUSICVOLUME_JSON_PATH), containerA.LastWriteTime!.Value.UtcTicks);

        containerA.SetJsonValue(MUSICVOLUME_NEW_AMOUNT, MUSICVOLUME_JSON_PATH);
        platformA.Write(containerA, now);
        (int MusicVolume, long UtcTicks) valuesSet = (containerA.GetJsonValue<int>(MUSICVOLUME_JSON_PATH), containerA.LastWriteTime!.Value.UtcTicks);

        var platformB = new PlatformMicrosoft(path, settings);
        var containerB = platformB.GetAccountContainer()!;
        var containersIndexB = File.ReadAllBytes(Path.Combine(path, "containers.index"));

        platformB.Load(containerB);
        (int MusicVolume, long UtcTicks) valuesReload = (containerB.GetJsonValue<int>(MUSICVOLUME_JSON_PATH), containerB.LastWriteTime!.Value.UtcTicks);
#pragma warning restore IDE0042 // Deconstruct variable declaration

        // Assert
        Assert.IsTrue(writeCallback);

        Assert.AreEqual(80, valuesOrigin.MusicVolume);
        Assert.AreEqual(638264331709580000, valuesOrigin.UtcTicks); // 2023-07-31 20:46:10 +00:00
        Assert.AreEqual(MUSICVOLUME_NEW_AMOUNT, valuesSet.MusicVolume);
        Assert.AreEqual(nowTicks, valuesSet.UtcTicks);

        Assert.AreEqual(MUSICVOLUME_NEW_AMOUNT, valuesReload.MusicVolume);
        Assert.AreEqual(nowTicks, valuesReload.UtcTicks);

        AssertContainersIndex(containersIndexA, containersIndexB, now, path);
    }

    [TestMethod]
    public void T11_Write_Default()
    {
        var now = DateTimeOffset.UtcNow;
        var nowTicks = NullifyTicks(now).UtcTicks;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "000901F8A36808E0_29070100B936489ABCE8B9AF3980429C");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            UseMapping = true,
        };
        var writeCallback = false;

        // Act
        var platformA = new PlatformMicrosoft(path, settings);
        var containerA = GetOneSaveContainer(platformA, 0);
        var metaA = DecryptMeta(containerA);
        var priectA = new PrivateObject(containerA);

        containerA.WriteCallback += () =>
        {
            writeCallback = true;
        };

#pragma warning disable IDE0042 // Deconstruct variable declaration
        platformA.Load(containerA);
        (int Units, long UtcTicks) valuesOrigin = (containerA.GetJsonValue<int>(UNITS_JSON_PATH), containerA.LastWriteTime!.Value.UtcTicks);

        containerA.SetJsonValue(UNITS_NEW_AMOUNT, UNITS_JSON_PATH);
        platformA.Write(containerA, now);
        (int Units, long UtcTicks) valuesSet = (containerA.GetJsonValue<int>(UNITS_JSON_PATH), containerA.LastWriteTime!.Value.UtcTicks);

        var platformB = new PlatformMicrosoft(path, settings);
        var containerB = GetOneSaveContainer(platformB, 0);
        var metaB = DecryptMeta(containerB);
        var priectB = new PrivateObject(containerB);

        platformB.Load(containerB);
        (int Units, long UtcTicks) valuesReload = (containerB.GetJsonValue<int>(UNITS_JSON_PATH), containerB.LastWriteTime!.Value.UtcTicks);
#pragma warning restore IDE0042 // Deconstruct variable declaration

        // Assert
        Assert.IsTrue(writeCallback);

        Assert.AreEqual(1504909789, valuesOrigin.Units);
        Assert.AreEqual(638126763444620000, valuesOrigin.UtcTicks); // 2023-02-22 15:25:44 +00:00
        Assert.AreEqual(UNITS_NEW_AMOUNT, valuesSet.Units);
        Assert.AreEqual(nowTicks, valuesSet.UtcTicks);

        Assert.AreEqual(UNITS_NEW_AMOUNT, valuesReload.Units);
        Assert.AreEqual(nowTicks, valuesReload.UtcTicks);

        AssertCommonMeta(containerA, metaA, metaB);

        var bytesA = metaA.AsSpan().AsBytes().ToArray();
        var bytesB = metaB.AsSpan().AsBytes().ToArray();

        AssertAllAreEqual(4143, (uint)(int)(priectA.GetFieldOrProperty("BaseVersion")), (uint)(int)(priectB.GetFieldOrProperty("BaseVersion")), metaA[0], metaB[0]);
        AssertAllAreEqual((ushort)(PresetGameModeEnum.Normal), (ushort)(PresetGameModeEnum)(priectA.GetFieldOrProperty("GameMode")), (ushort)(PresetGameModeEnum)(priectB.GetFieldOrProperty("GameMode")), BitConverter.ToInt16(bytesA, 4), BitConverter.ToInt16(bytesB, 4));
        AssertAllAreEqual((ushort)(SeasonEnum.None), (ushort)(containerA.Season), (ushort)(containerB.Season), BitConverter.ToUInt16(bytesA, 6), BitConverter.ToUInt16(bytesA, 6));
        AssertAllAreEqual(635119, containerA.TotalPlayTime, containerB.TotalPlayTime, metaA[2], metaB[2]);
        AssertAllAreEqual("1. Haupt", containerA.SaveName, containerB.SaveName, GetString(bytesA.Skip(20).TakeWhile(i => i != 0)), GetString(bytesB.Skip(20).TakeWhile(i => i != 0)));
        AssertAllAreEqual("Auf dem Frachter (WF-4 Dawajima)", containerA.SaveSummary, containerB.SaveSummary, GetString(bytesA.Skip(148).TakeWhile(i => i != 0)), GetString(bytesB.Skip(148).TakeWhile(i => i != 0)));
        AssertAllAreEqual((byte)(DifficultyPresetTypeEnum.Custom), (byte)(containerA.Difficulty), (byte)(containerB.Difficulty), bytesA[276], bytesB[276]);
    }

    /// <summary>
    /// Same as <see cref="T10_Write_Default_ContainersIndex"/> but with assert for manifest.
    /// </summary>
    [TestMethod]
    public void T12_Write_Default_Account()
    {
        var now = DateTimeOffset.UtcNow;
        var nowTicks = NullifyTicks(now).UtcTicks;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "00090000025A963A_29070100B936489ABCE8B9AF3980429C");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            UseMapping = true,
        };
        var writeCallback = false;

        // Act
        var platformA = new PlatformMicrosoft(path, settings);
        var containerA = platformA.GetAccountContainer()!;
        var metaA = DecryptMeta(containerA);

        containerA.WriteCallback += () =>
        {
            writeCallback = true;
        };

#pragma warning disable IDE0042 // Deconstruct variable declaration
        platformA.Load(containerA);
        (int MusicVolume, long UtcTicks) valuesOrigin = (containerA.GetJsonValue<int>(MUSICVOLUME_JSON_PATH), containerA.LastWriteTime!.Value.UtcTicks);

        containerA.SetJsonValue(MUSICVOLUME_NEW_AMOUNT, MUSICVOLUME_JSON_PATH);
        platformA.Write(containerA, now);
        (int MusicVolume, long UtcTicks) valuesSet = (containerA.GetJsonValue<int>(MUSICVOLUME_JSON_PATH), containerA.LastWriteTime!.Value.UtcTicks);

        var platformB = new PlatformMicrosoft(path, settings);
        var containerB = platformB.GetAccountContainer()!;
        var metaB = DecryptMeta(containerB);

        platformB.Load(containerB);
        (int MusicVolume, long UtcTicks) valuesReload = (containerB.GetJsonValue<int>(MUSICVOLUME_JSON_PATH), containerB.LastWriteTime!.Value.UtcTicks);
#pragma warning restore IDE0042 // Deconstruct variable declaration

        // Assert
        Assert.IsTrue(writeCallback);

        Assert.AreEqual(80, valuesOrigin.MusicVolume);
        Assert.AreEqual(638264331709580000, valuesOrigin.UtcTicks); // 2023-07-31 20:46:10 +00:00
        Assert.AreEqual(MUSICVOLUME_NEW_AMOUNT, valuesSet.MusicVolume);
        Assert.AreEqual(nowTicks, valuesSet.UtcTicks);

        Assert.AreEqual(MUSICVOLUME_NEW_AMOUNT, valuesReload.MusicVolume);
        Assert.AreEqual(nowTicks, valuesReload.UtcTicks);

        AssertCommonMeta(containerA, metaA, metaB);
    }

    [TestMethod]
    public void T13_Write_SetLastWriteTime_False()
    {
        var now = DateTimeOffset.UtcNow;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "000901F8A36808E0_29070100B936489ABCE8B9AF3980429C");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            SetLastWriteTime = false,
            UseMapping = true,
        };
        var writeCallback = false;

        // Act
        var platformA = new PlatformMicrosoft(path, settings);
        var containerA = GetOneSaveContainer(platformA, 0);

        containerA.WriteCallback += () =>
        {
            writeCallback = true;
        };

#pragma warning disable IDE0042 // Deconstruct variable declaration
        platformA.Load(containerA);
        (int Units, long UtcTicks) valuesOrigin = (containerA.GetJsonValue<int>(UNITS_JSON_PATH), containerA.LastWriteTime!.Value.UtcTicks);

        containerA.SetJsonValue(UNITS_NEW_AMOUNT, UNITS_JSON_PATH);
        platformA.Write(containerA, now);
        (int Units, long UtcTicks) valuesSet = (containerA.GetJsonValue<int>(UNITS_JSON_PATH), containerA.LastWriteTime!.Value.UtcTicks);

        var platformB = new PlatformMicrosoft(path, settings);
        var containerB = GetOneSaveContainer(platformB, 0);

        platformB.Load(containerB);
        (int Units, long UtcTicks) valuesReload = (containerB.GetJsonValue<int>(UNITS_JSON_PATH), containerB.LastWriteTime!.Value.UtcTicks);
#pragma warning restore IDE0042 // Deconstruct variable declaration

        // Assert
        Assert.IsTrue(writeCallback);

        Assert.AreEqual(1504909789, valuesOrigin.Units);
        Assert.AreEqual(638126763444620000, valuesOrigin.UtcTicks); // 2023-02-22 15:25:44 +00:00
        Assert.AreEqual(UNITS_NEW_AMOUNT, valuesSet.Units);
        Assert.AreEqual(638126763444620000, valuesSet.UtcTicks);

        Assert.AreEqual(UNITS_NEW_AMOUNT, valuesReload.Units);
        Assert.AreEqual(638126763444620000, valuesReload.UtcTicks);
    }

    [TestMethod]
    public void T14_Write_WriteAlways_True()
    {
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "000901F8A36808E0_29070100B936489ABCE8B9AF3980429C");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            WriteAlways = true,
        };
        var writeCallback = false;

        // Act
        var platformA = new PlatformMicrosoft(path, settings);
        var containerA = GetOneSaveContainer(platformA, 0);

        containerA.WriteCallback += () =>
        {
            writeCallback = true;
        };

        platformA.Load(containerA);
        containerA.DataFile!.Refresh();
        var lengthOrigin = containerA.DataFile!.Length;

        platformA.Write(containerA);
        containerA.DataFile!.Refresh();
        var lengthSet = containerA.DataFile!.Length;

        var platformB = new PlatformMicrosoft(path, settings);
        var containerB = GetOneSaveContainer(platformB, 0);

        platformB.Load(containerB);
        containerB.DataFile!.Refresh();
        var lengthReload = containerA.DataFile!.Length;

        // Assert
        Assert.IsTrue(writeCallback);

        Assert.AreNotEqual(lengthOrigin, lengthSet);
        Assert.AreNotEqual(lengthOrigin, lengthReload);

        Assert.AreEqual(lengthSet, lengthReload);
    }

    [TestMethod]
    public void T15_Write_WriteAlways_False()
    {
        var now = DateTimeOffset.UtcNow;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "000901F8A36808E0_29070100B936489ABCE8B9AF3980429C");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            WriteAlways = false,
        };
        var writeCallback = false;

        // Act
        var platformA = new PlatformMicrosoft(path, settings);
        var containerA = GetOneSaveContainer(platformA, 0);

        containerA.WriteCallback += () =>
        {
            writeCallback = true;
        };

        platformA.Load(containerA);
        containerA.DataFile!.Refresh();
        var lengthOrigin = containerA.DataFile!.Length;

        platformA.Write(containerA, now);
        containerA.DataFile!.Refresh();
        var lengthSet = containerA.DataFile!.Length;

        var platformB = new PlatformMicrosoft(path, settings);
        var containerB = GetOneSaveContainer(platformB, 0);

        platformB.Load(containerB);
        containerB.DataFile!.Refresh();
        var lengthReload = containerA.DataFile!.Length;

        // Assert
        Assert.IsTrue(writeCallback);

        Assert.AreEqual(lengthOrigin, lengthSet);
        Assert.AreEqual(lengthOrigin, lengthReload);
    }

    [TestMethod]
    public void T20_FileSystemWatcher()
    {
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "000901FB44140B02_29070100B936489ABCE8B9AF3980429C");
        var pathWatching = Path.Combine(path, "containers.index");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            UseMapping = true,
            Watcher = true,
        };

        // Act
        var bytes = File.ReadAllBytes(pathWatching);

        var platform = new PlatformMicrosoft(path, settings);
        var container = GetOneSaveContainer(platform, 0);

        platform.Load(container);

        File.WriteAllBytes(pathWatching, bytes);
        Thread.Sleep(FILESYSTEMWATCHER_SLEEP);
        var watchers1 = GetWatcherChangeContainers(platform);
        var count1 = watchers1.Count();
        var synced1 = container.IsSynced;

        container.SetJsonValue(UNITS_NEW_AMOUNT, UNITS_JSON_PATH);
        var synced2 = container.IsSynced;

        File.WriteAllBytes(pathWatching, bytes);
        Thread.Sleep(FILESYSTEMWATCHER_SLEEP);
        var watchers2 = GetWatcherChangeContainers(platform);
        var count2 = watchers2.Count();
        var synced3 = container.IsSynced;

        var watcherContainer2 = watchers2.FirstOrDefault();
        Guard.IsNotNull(watcherContainer2);
        platform.OnWatcherDecision(watcherContainer2, false);
        var synced4 = container.IsSynced;

        File.WriteAllBytes(pathWatching, bytes);
        Thread.Sleep(FILESYSTEMWATCHER_SLEEP);
        var watchers3 = GetWatcherChangeContainers(platform);
        var count3 = watchers3.Count();
        var synced5 = container.IsSynced;

        var watcherContainer3 = watchers3.FirstOrDefault();
        Guard.IsNotNull(watcherContainer3);
        platform.OnWatcherDecision(watcherContainer3, true);
        var synced6 = container.IsSynced;

        // Assert
        Assert.AreEqual(0, count1);
        Assert.IsTrue(synced1);

        Assert.IsFalse(synced2);

        Assert.AreEqual(1, count2);
        Assert.IsFalse(synced3);

        Assert.AreEqual(container, watcherContainer2);
        Assert.IsFalse(synced4);

        Assert.AreEqual(1, count3);
        Assert.IsFalse(synced5);

        Assert.AreEqual(container, watcherContainer3);
        Assert.IsTrue(synced6);
    }

    [TestMethod]
    public void T30_Copy()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "000901FB44140B02_29070100B936489ABCE8B9AF3980429C");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };

        // Act
        var platform = new PlatformMicrosoft(path, settings);

        var container0 = GetOneSaveContainer(platform, 0); // 1Auto
        var container1 = GetOneSaveContainer(platform, 1); // 1Manual
        var container2 = GetOneSaveContainer(platform, 2); // 2Auto
        var container4 = GetOneSaveContainer(platform, 4); // 3Auto
        var container8 = GetOneSaveContainer(platform, 8); // 5Auto
        var container9 = GetOneSaveContainer(platform, 9); // 5Manual

        platform.Copy(container4, container1); // 3Auto -> 1Manual (overwrite)
        platform.Copy(container2, container8); // 2Auto -> 5Auto (create)
        platform.Copy(container9, container0); // 5Manual -> 1Auto (delete)

        // Assert
        var priect1 = new PrivateObject(container1);
        var priect2 = new PrivateObject(container2);
        var priect4 = new PrivateObject(container4);
        var priect8 = new PrivateObject(container8);

        Assert.IsTrue(container1.Exists);
        Assert.AreEqual((PresetGameModeEnum)(priect4.GetFieldOrProperty("GameMode")), (PresetGameModeEnum)(priect1.GetFieldOrProperty("GameMode")));
        Assert.AreEqual(container4.Difficulty, container1.Difficulty);
        Assert.AreEqual(container4.Season, container1.Season);
        Assert.AreEqual((int)(priect4.GetFieldOrProperty("BaseVersion")), (int)(priect1.GetFieldOrProperty("BaseVersion")));
        Assert.AreEqual(container4.GameVersion, container1.GameVersion);
        Assert.AreEqual(container4.TotalPlayTime, container1.TotalPlayTime);

        Assert.IsTrue(container8.Exists);
        Assert.AreEqual((PresetGameModeEnum)(priect2.GetFieldOrProperty("GameMode")), (PresetGameModeEnum)(priect8.GetFieldOrProperty("GameMode")));
        Assert.AreEqual(container2.Difficulty, container8.Difficulty);
        Assert.AreEqual(container2.Season, container8.Season);
        Assert.AreEqual((int)(priect2.GetFieldOrProperty("BaseVersion")), (int)(priect8.GetFieldOrProperty("BaseVersion")));
        Assert.AreEqual(container4.GameVersion, container1.GameVersion);
        Assert.AreEqual(container4.TotalPlayTime, container1.TotalPlayTime);

        Assert.IsFalse(container0.Exists);
    }

    [TestMethod]
    public void T31_Delete()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "000901FB44140B02_29070100B936489ABCE8B9AF3980429C");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };

        // Act
        var platform = new PlatformMicrosoft(path, settings);

        var container0 = GetOneSaveContainer(platform, 0); // 1Auto
        var container1 = GetOneSaveContainer(platform, 1); // 1Manual

        platform.Delete(container0);
        platform.Delete(container1);

        // Assert
        Assert.IsFalse(container0.Exists);
        Assert.IsNull(container0.DataFile);
        Assert.AreEqual(libNOM.io.Globals.Constants.INCOMPATIBILITY_004, container0.IncompatibilityTag);

        Assert.IsFalse(container1.Exists);
        Assert.IsNull(container1.DataFile);
        Assert.AreEqual(libNOM.io.Globals.Constants.INCOMPATIBILITY_004, container1.IncompatibilityTag);
    }

    [TestMethod]
    public void T32_Move()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "000901FB44140B02_29070100B936489ABCE8B9AF3980429C");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };

        // Act
        var platform = new PlatformMicrosoft(path, settings);

        var container0 = GetOneSaveContainer(platform, 0); // 1Auto
        var container1 = GetOneSaveContainer(platform, 1); // 1Manual
        var container2 = GetOneSaveContainer(platform, 2); // 2Auto
        var container4 = GetOneSaveContainer(platform, 4); // 3Auto
        var container5 = GetOneSaveContainer(platform, 5); // 3Manual
        var container9 = GetOneSaveContainer(platform, 9); // 5Manual

        var priect2 = new PrivateObject(container2);
        var priect4 = new PrivateObject(container4);

        var gameModeEnum2 = (PresetGameModeEnum)(priect2.GetFieldOrProperty("GameMode"));
        var gameDifficultyEnum2 = container2.Difficulty;
        var seasonEnum2 = container2.Season;
        var baseVersion2 = (int)(priect2.GetFieldOrProperty("BaseVersion"));
        var versionEnum2 = container2.GameVersion;
        var totalPlayTime2 = container2.TotalPlayTime;
        platform.Copy(container4, container5);
        platform.Move(container2, container5); // overwrite

        // 1 is corrupted, therefore 0 gets deleted and then 1 is also deleted after copying.
        platform.Move(container1, container0); // delete

        var gameModeEnum4 = (PresetGameModeEnum)(priect4.GetFieldOrProperty("GameMode"));
        var gameDifficultyEnum4 = container4.Difficulty;
        var seasonEnum4 = container4.Season;
        var baseVersion4 = (int)(priect4.GetFieldOrProperty("BaseVersion"));
        var versionEnum4 = container4.GameVersion;
        var totalPlayTime4 = container4.TotalPlayTime;
        platform.Move(container4, container9); // move

        // Assert
        var priect5 = new PrivateObject(container5);
        var priect9 = new PrivateObject(container9);

        Assert.IsFalse(container2.Exists); Assert.IsTrue(container5.Exists);
        Assert.AreEqual(gameModeEnum2, (PresetGameModeEnum)(priect5.GetFieldOrProperty("GameMode")));
        Assert.AreEqual(gameDifficultyEnum2, container5.Difficulty);
        Assert.AreEqual(seasonEnum2, container5.Season);
        Assert.AreEqual(baseVersion2, (int)(priect5.GetFieldOrProperty("BaseVersion")));
        Assert.AreEqual(versionEnum2, container5.GameVersion);
        Assert.AreEqual(totalPlayTime2, container5.TotalPlayTime);

        Assert.IsFalse(container0.Exists);
        Assert.IsFalse(container1.Exists);
        Assert.AreEqual(libNOM.io.Globals.Constants.INCOMPATIBILITY_004, container0.IncompatibilityTag);
        Assert.AreEqual(libNOM.io.Globals.Constants.INCOMPATIBILITY_004, container1.IncompatibilityTag);

        Assert.IsFalse(container4.Exists); Assert.IsTrue(container9.Exists);
        Assert.AreEqual(gameModeEnum4, (PresetGameModeEnum)(priect9.GetFieldOrProperty("GameMode")));
        Assert.AreEqual(gameDifficultyEnum4, container9.Difficulty);
        Assert.AreEqual(seasonEnum4, container9.Season);
        Assert.AreEqual(baseVersion4, (int)(priect9.GetFieldOrProperty("BaseVersion")));
        Assert.AreEqual(versionEnum4, container9.GameVersion);
        Assert.AreEqual(totalPlayTime4, container9.TotalPlayTime);
    }

    [TestMethod]
    public void T40_TransferFromGog()
    {
        // Arrange
        var pathGog = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Gog", "DefaultUser");
        var resultsGog = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum Season, int BaseVersion, GameVersionEnum Version)[]
        {
            (2, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, GameVersionEnum.Emergence), // 2Auto
            (3, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, GameVersionEnum.Emergence), // 2Manual
        };
        var userIdentificationGog = ReadUserIdentification(pathGog);

        var offset = 2;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "000901F4E735CFAC_29070100B936489ABCE8B9AF3980429C");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
            UseExternalSourcesForUserIdentification = false,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platformGog = new PlatformGog(pathGog, settings);
        var transfer = platformGog.GetSourceTransferData(1);

        var platform = new PlatformMicrosoft(path, settings);

        platform.Transfer(transfer, 2); // overwrite
        var container4 = GetOneSaveContainer(platform, 4);
        var priect4 = new PrivateObject(container4);
        var userIdentification4 = (UserIdentification)(priect4.GetFieldOrProperty("UserIdentification"));

        platform.Transfer(transfer, 3); // create
        var container6 = GetOneSaveContainer(platform, 6);
        var priect6 = new PrivateObject(container6);
        var userIdentification6 = (UserIdentification)(priect6.GetFieldOrProperty("UserIdentification"));

        // Assert
        AssertAllAreEqual(1, transfer.TransferBaseUserDecision.Count);
        Assert.AreEqual(8, GetExistingContainers(platform).Count()); // + 2

        AssertAllAreEqual(userIdentificationGog[0], platformGog.PlatformUserIdentification.LID!, transfer.UserIdentification.LID!);
        AssertAllAreEqual(userIdentificationGog[1], platformGog.PlatformUserIdentification.UID!, transfer.UserIdentification.UID!);
        AssertAllAreEqual(userIdentificationGog[2], platformGog.PlatformUserIdentification.USN!, transfer.UserIdentification.USN!);
        AssertAllAreEqual(userIdentificationGog[3], platformGog.PlatformUserIdentification.PTK!, transfer.UserIdentification.PTK!);

        AssertAllAreEqual(userIdentification[0], platform.PlatformUserIdentification.LID!, userIdentification4.LID!, userIdentification6.LID!);
        AssertAllAreEqual(userIdentification[1], platform.PlatformUserIdentification.UID!, userIdentification4.UID!, userIdentification6.UID!);
        AssertAllAreEqual(userIdentification[2], platform.PlatformUserIdentification.USN!, userIdentification4.USN!, userIdentification6.USN!);
        AssertAllAreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK!, userIdentification4.PTK!, userIdentification6.PTK!);

        for (var i = 0; i < resultsGog.Length; i++)
        {
            var container = GetOneSaveContainer(platform, resultsGog[i].CollectionIndex + offset);
            var priect = new PrivateObject(container);

            Assert.AreEqual(resultsGog[i].Exists, container.Exists);
            Assert.AreEqual(resultsGog[i].IsOld, container.IsOld);
            Assert.AreEqual(resultsGog[i].GameMode, (PresetGameModeEnum)(priect.GetFieldOrProperty("GameMode")));
            Assert.AreEqual(resultsGog[i].GameDifficulty, container.Difficulty);
            Assert.AreEqual(resultsGog[i].Season, container.Season);
            Assert.AreEqual(resultsGog[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(resultsGog[i].Version, container.GameVersion);
        }
    }

    [TestMethod]
    public void T41_TransferFromMicrosoft()
    {
        // Arrange
        var pathMicrosoft = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "0009000000C73498_29070100B936489ABCE8B9AF3980429C");
        var resultsMicrosoft = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum Season, int BaseVersion, GameVersionEnum Version)[]
        {
            (2, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, GameVersionEnum.Frontiers), // 2Auto
            (3, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, GameVersionEnum.Frontiers), // 2Manual
        };
        var userIdentificationMicrosoft = ReadUserIdentification(pathMicrosoft);

        var offset = 2;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "000901F4E735CFAC_29070100B936489ABCE8B9AF3980429C");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
            UseExternalSourcesForUserIdentification = false,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platformMicrosoft = new PlatformMicrosoft(pathMicrosoft, settings);
        var transfer = platformMicrosoft.GetSourceTransferData(1);

        var platform = new PlatformMicrosoft(path, settings);

        platform.Transfer(transfer, 2); // overwrite
        var container4 = GetOneSaveContainer(platform, 4);
        var priect4 = new PrivateObject(container4);
        var userIdentification4 = (UserIdentification)(priect4.GetFieldOrProperty("UserIdentification"));

        platform.Transfer(transfer, 3); // create
        var container6 = GetOneSaveContainer(platform, 6);
        var priect6 = new PrivateObject(container6);
        var userIdentification6 = (UserIdentification)(priect6.GetFieldOrProperty("UserIdentification"));

        // Assert
        AssertAllAreEqual(8, transfer.TransferBaseUserDecision.Count);
        Assert.AreEqual(8, GetExistingContainers(platform).Count()); // + 2

        AssertAllAreEqual(userIdentificationMicrosoft[0], platformMicrosoft.PlatformUserIdentification.LID!, transfer.UserIdentification.LID!);
        AssertAllAreEqual(userIdentificationMicrosoft[1], platformMicrosoft.PlatformUserIdentification.UID!, transfer.UserIdentification.UID!);
        AssertAllAreEqual(userIdentificationMicrosoft[2], platformMicrosoft.PlatformUserIdentification.USN!, transfer.UserIdentification.USN!);
        AssertAllAreEqual(userIdentificationMicrosoft[3], platformMicrosoft.PlatformUserIdentification.PTK!, transfer.UserIdentification.PTK!);

        AssertAllAreEqual(userIdentification[1], platform.PlatformUserIdentification.UID!, userIdentification4.UID!, userIdentification6.UID!);
        AssertAllAreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK!, userIdentification4.PTK!, userIdentification6.PTK!);

        for (var i = 0; i < resultsMicrosoft.Length; i++)
        {
            var container = GetOneSaveContainer(platform, resultsMicrosoft[i].CollectionIndex + offset);
            var priect = new PrivateObject(container);

            Assert.AreEqual(resultsMicrosoft[i].Exists, container.Exists);
            Assert.AreEqual(resultsMicrosoft[i].IsOld, container.IsOld);
            Assert.AreEqual(resultsMicrosoft[i].GameMode, (PresetGameModeEnum)(priect.GetFieldOrProperty("GameMode")));
            Assert.AreEqual(resultsMicrosoft[i].GameDifficulty, container.Difficulty);
            Assert.AreEqual(resultsMicrosoft[i].Season, container.Season);
            Assert.AreEqual(resultsMicrosoft[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(resultsMicrosoft[i].Version, container.GameVersion);
        }
    }

    [TestMethod]
    public void T42_TransferFromPlaystation_0x7D1()
    {
        // Arrange
        var pathPlaystation = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D1", "SaveWizard", "1");
        var resultsPlaystation = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum Season, int BaseVersion, GameVersionEnum Version)[]
        {
            (2, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4134, GameVersionEnum.PrismsWithBytebeatAuthor), // 2Auto
            (3, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4134, GameVersionEnum.PrismsWithBytebeatAuthor), // 2Manual
        };
        var userIdentificationPlaystation = ReadUserIdentification(pathPlaystation);

        var offset = 2;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "000901F4E735CFAC_29070100B936489ABCE8B9AF3980429C");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
            UseExternalSourcesForUserIdentification = false,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platformPlaystation = new PlatformPlaystation(pathPlaystation, settings);
        var transfer = platformPlaystation.GetSourceTransferData(1);

        var platform = new PlatformMicrosoft(path, settings);

        platform.Transfer(transfer, 2); // overwrite
        var container4 = GetOneSaveContainer(platform, 4);
        var priect4 = new PrivateObject(container4);
        var userIdentification4 = (UserIdentification)(priect4.GetFieldOrProperty("UserIdentification"));

        platform.Transfer(transfer, 3); // create
        var container6 = GetOneSaveContainer(platform, 6);
        var priect6 = new PrivateObject(container6);
        var userIdentification6 = (UserIdentification)(priect6.GetFieldOrProperty("UserIdentification"));

        // Assert
        AssertAllAreEqual(24, transfer.TransferBaseUserDecision.Count);
        Assert.AreEqual(8, GetExistingContainers(platform).Count()); // + 2

        AssertAllAreEqual(userIdentificationPlaystation[0], platformPlaystation.PlatformUserIdentification.LID!, transfer.UserIdentification.LID!);
        AssertAllAreEqual(userIdentificationPlaystation[1], platformPlaystation.PlatformUserIdentification.UID!, transfer.UserIdentification.UID!);
        AssertAllAreEqual(userIdentificationPlaystation[2], platformPlaystation.PlatformUserIdentification.USN!, transfer.UserIdentification.USN!);
        AssertAllAreEqual(userIdentificationPlaystation[3], platformPlaystation.PlatformUserIdentification.PTK!, transfer.UserIdentification.PTK!);

        AssertAllAreEqual(userIdentification[0], platform.PlatformUserIdentification.LID!, userIdentification4.LID!, userIdentification6.LID!);
        AssertAllAreEqual(userIdentification[1], platform.PlatformUserIdentification.UID!, userIdentification4.UID!, userIdentification6.UID!);
        AssertAllAreEqual(userIdentification[2], platform.PlatformUserIdentification.USN!, userIdentification4.USN!, userIdentification6.USN!);
        AssertAllAreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK!, userIdentification4.PTK!, userIdentification6.PTK!);

        for (var i = 0; i < resultsPlaystation.Length; i++)
        {
            var container = GetOneSaveContainer(platform, resultsPlaystation[i].CollectionIndex + offset);
            var priect = new PrivateObject(container);

            Assert.AreEqual(resultsPlaystation[i].Exists, container.Exists);
            Assert.AreEqual(resultsPlaystation[i].IsOld, container.IsOld);
            Assert.AreEqual(resultsPlaystation[i].GameMode, (PresetGameModeEnum)(priect.GetFieldOrProperty("GameMode")));
            Assert.AreEqual(resultsPlaystation[i].GameDifficulty, container.Difficulty);
            Assert.AreEqual(resultsPlaystation[i].Season, container.Season);
            Assert.AreEqual(resultsPlaystation[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(resultsPlaystation[i].Version, container.GameVersion);
        }
    }

    [TestMethod]
    public void T43_TransferFromPlaystation_0x7D2()
    {
        // Arrange
        var pathPlaystation = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D2", "SaveWizard", "4");
        var resultsPlaystation = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum Season, int BaseVersion, GameVersionEnum Version)[]
        {
            (2, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, GameVersionEnum.Frontiers), // 2Auto
            (3, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, GameVersionEnum.Frontiers), // 2Manual
        };
        var userIdentificationPlaystation = ReadUserIdentification(pathPlaystation);

        var offset = 2;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "000901F4E735CFAC_29070100B936489ABCE8B9AF3980429C");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
            UseExternalSourcesForUserIdentification = false,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platformPlaystation = new PlatformPlaystation(pathPlaystation, settings);
        var transfer = platformPlaystation.GetSourceTransferData(1);

        var platform = new PlatformMicrosoft(path, settings);

        platform.Transfer(transfer, 2); // overwrite
        var container4 = GetOneSaveContainer(platform, 4);
        var priect4 = new PrivateObject(container4);
        var userIdentification4 = (UserIdentification)(priect4.GetFieldOrProperty("UserIdentification"));

        platform.Transfer(transfer, 3); // create
        var container6 = GetOneSaveContainer(platform, 6);
        var priect6 = new PrivateObject(container6);
        var userIdentification6 = (UserIdentification)(priect6.GetFieldOrProperty("UserIdentification"));

        // Assert
        AssertAllAreEqual(4, transfer.TransferBaseUserDecision.Count);
        Assert.AreEqual(8, GetExistingContainers(platform).Count()); // + 2

        AssertAllAreEqual(userIdentificationPlaystation[0], platformPlaystation.PlatformUserIdentification.LID!, transfer.UserIdentification.LID!);
        AssertAllAreEqual(userIdentificationPlaystation[1], platformPlaystation.PlatformUserIdentification.UID!, transfer.UserIdentification.UID!);
        AssertAllAreEqual(userIdentificationPlaystation[2], platformPlaystation.PlatformUserIdentification.USN!, transfer.UserIdentification.USN!);
        AssertAllAreEqual(userIdentificationPlaystation[3], platformPlaystation.PlatformUserIdentification.PTK!, transfer.UserIdentification.PTK!);

        AssertAllAreEqual(userIdentification[0], platform.PlatformUserIdentification.LID!, userIdentification4.LID!, userIdentification6.LID!);
        AssertAllAreEqual(userIdentification[1], platform.PlatformUserIdentification.UID!, userIdentification4.UID!, userIdentification6.UID!);
        AssertAllAreEqual(userIdentification[2], platform.PlatformUserIdentification.USN!, userIdentification4.USN!, userIdentification6.USN!);
        AssertAllAreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK!, userIdentification4.PTK!, userIdentification6.PTK!);

        for (var i = 0; i < resultsPlaystation.Length; i++)
        {
            var container = GetOneSaveContainer(platform, resultsPlaystation[i].CollectionIndex + offset);
            var priect = new PrivateObject(container);

            Assert.AreEqual(resultsPlaystation[i].Exists, container.Exists);
            Assert.AreEqual(resultsPlaystation[i].IsOld, container.IsOld);
            Assert.AreEqual(resultsPlaystation[i].GameMode, (PresetGameModeEnum)(priect.GetFieldOrProperty("GameMode")));
            Assert.AreEqual(resultsPlaystation[i].GameDifficulty, container.Difficulty);
            Assert.AreEqual(resultsPlaystation[i].Season, container.Season);
            Assert.AreEqual(resultsPlaystation[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(resultsPlaystation[i].Version, container.GameVersion);
        }
    }

    [TestMethod]
    public void T44_TransferFromSteam()
    {
        // Arrange
        var pathSteam = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");
        var resultsSteam = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum Season, int BaseVersion, GameVersionEnum Version)[]
        {
            (2, true, false, PresetGameModeEnum.Creative, DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4127, GameVersionEnum.Companions), // 2Auto
            (3, true, false, PresetGameModeEnum.Creative, DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4127, GameVersionEnum.Companions), // 2Manual
        };
        var userIdentificationSteam = ReadUserIdentification(pathSteam);

        var offset = 2;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "000901F4E735CFAC_29070100B936489ABCE8B9AF3980429C");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
            UseExternalSourcesForUserIdentification = false,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platformSteam = new PlatformSteam(pathSteam, settings);
        var transfer = platformSteam.GetSourceTransferData(1);

        var platform = new PlatformMicrosoft(path, settings);

        platform.Transfer(transfer, 2); // overwrite
        var container4 = GetOneSaveContainer(platform, 4);
        var priect4 = new PrivateObject(container4);
        var userIdentification4 = (UserIdentification)(priect4.GetFieldOrProperty("UserIdentification"));

        platform.Transfer(transfer, 3); // create
        var container6 = GetOneSaveContainer(platform, 6);
        var priect6 = new PrivateObject(container6);
        var userIdentification6 = (UserIdentification)(priect6.GetFieldOrProperty("UserIdentification"));

        // Assert
        AssertAllAreEqual(2, transfer.TransferBaseUserDecision.Count);
        Assert.AreEqual(8, GetExistingContainers(platform).Count()); // + 2

        AssertAllAreEqual(userIdentificationSteam[0], platformSteam.PlatformUserIdentification.LID!, transfer.UserIdentification.LID!);
        AssertAllAreEqual(userIdentificationSteam[1], platformSteam.PlatformUserIdentification.UID!, transfer.UserIdentification.UID!);
        AssertAllAreEqual(userIdentificationSteam[2], platformSteam.PlatformUserIdentification.USN!, transfer.UserIdentification.USN!);
        AssertAllAreEqual(userIdentificationSteam[3], platformSteam.PlatformUserIdentification.PTK!, transfer.UserIdentification.PTK!);

        AssertAllAreEqual(userIdentification[0], platform.PlatformUserIdentification.LID!, userIdentification4.LID!, userIdentification6.LID!);
        AssertAllAreEqual(userIdentification[1], platform.PlatformUserIdentification.UID!, userIdentification4.UID!, userIdentification6.UID!);
        AssertAllAreEqual(userIdentification[2], platform.PlatformUserIdentification.USN!, userIdentification4.USN!, userIdentification6.USN!);
        AssertAllAreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK!, userIdentification4.PTK!, userIdentification6.PTK!);

        for (var i = 0; i < resultsSteam.Length; i++)
        {
            var container = GetOneSaveContainer(platform, resultsSteam[i].CollectionIndex + offset);
            var priect = new PrivateObject(container);

            Assert.AreEqual(resultsSteam[i].Exists, container.Exists);
            Assert.AreEqual(resultsSteam[i].IsOld, container.IsOld);
            Assert.AreEqual(resultsSteam[i].GameMode, (PresetGameModeEnum)(priect.GetFieldOrProperty("GameMode")));
            Assert.AreEqual(resultsSteam[i].GameDifficulty, container.Difficulty);
            Assert.AreEqual(resultsSteam[i].Season, container.Season);
            Assert.AreEqual(resultsSteam[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(resultsSteam[i].Version, container.GameVersion);
        }
    }

    [TestMethod]
    public void T45_TransferFromSwitch()
    {
        // Arrange
        var pathSwitch = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "4");
        var resultsSwitch = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum Season, int BaseVersion, GameVersionEnum Version)[]
        {
            (2, true, false, PresetGameModeEnum.Survival, DifficultyPresetTypeEnum.Survival, SeasonEnum.None, 4139, GameVersionEnum.Endurance), // 2Auto
        };
        var userIdentificationSwitch = ReadUserIdentification(pathSwitch);

        var offset = 2;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "000901F4E735CFAC_29070100B936489ABCE8B9AF3980429C");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
            UseExternalSourcesForUserIdentification = false,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platformSwitch = new PlatformSwitch(pathSwitch, settings);
        var transfer = platformSwitch.GetSourceTransferData(1);

        var platform = new PlatformMicrosoft(path, settings);

        platform.Transfer(transfer, 2); // overwrite
        var container4 = GetOneSaveContainer(platform, 4);
        var priect4 = new PrivateObject(container4);
        var userIdentification4 = (UserIdentification)(priect4.GetFieldOrProperty("UserIdentification"));

        platform.Transfer(transfer, 3); // create
        var container6 = GetOneSaveContainer(platform, 6);
        var priect6 = new PrivateObject(container6);
        var userIdentification6 = (UserIdentification)(priect6.GetFieldOrProperty("UserIdentification"));

        // Assert
        AssertAllAreEqual(0, transfer.TransferBaseUserDecision.Count);
        Assert.AreEqual(6, GetExistingContainers(platform).Count()); // - 1 + 1

        AssertAllAreEqual(userIdentificationSwitch[0], platformSwitch.PlatformUserIdentification.LID!, transfer.UserIdentification.LID!);
        AssertAllAreEqual(userIdentificationSwitch[1], platformSwitch.PlatformUserIdentification.UID!, transfer.UserIdentification.UID!);
        AssertAllAreEqual(userIdentificationSwitch[2], platformSwitch.PlatformUserIdentification.USN!, transfer.UserIdentification.USN!);
        AssertAllAreEqual(userIdentificationSwitch[3], platformSwitch.PlatformUserIdentification.PTK!, transfer.UserIdentification.PTK!);

        AssertAllAreEqual(userIdentification[0], platform.PlatformUserIdentification.LID!, userIdentification4.LID!, userIdentification6.LID!);
        AssertAllAreEqual(userIdentification[1], platform.PlatformUserIdentification.UID!, userIdentification4.UID!, userIdentification6.UID!);
        AssertAllAreEqual(userIdentification[2], platform.PlatformUserIdentification.USN!, userIdentification4.USN!, userIdentification6.USN!);
        AssertAllAreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK!, userIdentification4.PTK!, userIdentification6.PTK!);

        for (var i = 0; i < resultsSwitch.Length; i++)
        {
            var container = GetOneSaveContainer(platform, resultsSwitch[i].CollectionIndex + offset);
            var priect = new PrivateObject(container);

            Assert.AreEqual(resultsSwitch[i].Exists, container.Exists);
            Assert.AreEqual(resultsSwitch[i].IsOld, container.IsOld);
            Assert.AreEqual(resultsSwitch[i].GameMode, (PresetGameModeEnum)(priect.GetFieldOrProperty("GameMode")));
            Assert.AreEqual(resultsSwitch[i].GameDifficulty, container.Difficulty);
            Assert.AreEqual(resultsSwitch[i].Season, container.Season);
            Assert.AreEqual(resultsSwitch[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(resultsSwitch[i].Version, container.GameVersion);
        }
    }
}
