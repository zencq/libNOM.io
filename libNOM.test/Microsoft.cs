using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;

using libNOM.io;
using libNOM.io.Interfaces;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace libNOM.test;


// Do not use System.Range for simplicity of the file and performance is not critical.
[TestClass]
[DeploymentItem("../../../Resources/TESTSUITE_ARCHIVE_PLATFORM_GOG.zip")]
[DeploymentItem("../../../Resources/TESTSUITE_ARCHIVE_PLATFORM_MICROSOFT.zip")]
[DeploymentItem("../../../Resources/TESTSUITE_ARCHIVE_PLATFORM_PLAYSTATION.zip")]
[DeploymentItem("../../../Resources/TESTSUITE_ARCHIVE_PLATFORM_STEAM.zip")]
[DeploymentItem("../../../Resources/TESTSUITE_ARCHIVE_PLATFORM_SWITCH.zip")]
public class MicrosoftTest : CommonTestClass
{
    #region Constant

    protected const int CONTAINERSINDEX_HEADER = 0xE; // 14
    protected const long CONTAINERSINDEX_FOOTER = 0x10000000; // 268435456

    protected const int BLOBCONTAINER_HEADER = 0x4; // 4
    protected const int BLOBCONTAINER_COUNT = 0x2; // 2

    protected const int META_LENGTH_TOTAL_VANILLA = 0x18 / sizeof(uint); // 6
    protected const int META_LENGTH_TOTAL_WAYPOINT = 0x118 / sizeof(uint); // 70
    protected const int META_LENGTH_TOTAL_WORLDS = 0x128 / sizeof(uint); // 74

    #endregion

    #region Meta

    private static uint[] DecryptMeta(IContainer container)
    {
        byte[] meta = File.ReadAllBytes(container.MetaFile!.FullName);
        return ToUInt32(meta);
    }

    private static void AssertCommonMeta(IContainer container, uint[] metaA, uint[] metaB)
    {
        Assert.AreEqual(metaA.Length, metaB.Length);

        if (metaA.Length == META_LENGTH_TOTAL_VANILLA || metaA.Length == META_LENGTH_TOTAL_WAYPOINT)
        {
            if (container.IsAccount)
            {
                AssertAllAreEqual(1, metaA[0], metaB[0]);
                AssertAllZero(metaA.Skip(1).Take(3), metaB.Skip(1).Take(3));
                AssertAllNotZero(metaA[4], metaB[4]);
                AssertAllZero(metaA.Skip(5), metaB.Skip(5));
            }
            else
            {
                Assert.IsTrue(metaA.Skip(5).SequenceEqual(metaB.Skip(5)));
            }
        }
        else if (metaA.Length == META_LENGTH_TOTAL_WORLDS)
        {
            if (container.IsAccount)
            {
                AssertAllAreEqual(1, metaA[0], metaB[0]);
                AssertAllZero(metaA.Skip(1).Take(3), metaB.Skip(1).Take(3));
                AssertAllNotZero(metaA[4], metaB[4]);
                AssertAllZero(metaA.Skip(5), metaB.Skip(5));
            }
            else
            {
                Assert.IsTrue(metaA.Skip(5).Take(67).SequenceEqual(metaB.Skip(5).Take(67)));
                AssertAllAreEqual(META_FORMAT_4, metaA[73], metaB[73]);
            }
        }
        else
            throw new AssertFailedException();
    }

    private static void AssertSpecificMeta(WriteResults results, IContainer containerA, IContainer containerB, uint[] metaA, uint[] metaB)
    {
        var bytesA = metaA.AsSpan().AsBytes().ToArray();
        var bytesB = metaB.AsSpan().AsBytes().ToArray();
        var prijectA = new PrivateObject(containerA);
        var prijectB = new PrivateObject(containerB);

        AssertAllAreEqual(results.BaseVersion, (uint)(int)(prijectA.GetFieldOrProperty(nameof(WriteResults.BaseVersion))), (uint)(int)(prijectB.GetFieldOrProperty(nameof(WriteResults.BaseVersion))), metaA[0], metaB[0]);
        AssertAllAreEqual(results.GameMode, (ushort)(prijectA.GetFieldOrProperty(nameof(WriteResults.GameMode))), (ushort)(prijectB.GetFieldOrProperty(nameof(WriteResults.GameMode))), BitConverter.ToInt16(bytesA, 4), BitConverter.ToInt16(bytesB, 4));
        AssertAllAreEqual(results.Season, (ushort)(containerA.Season), (ushort)(containerB.Season), BitConverter.ToUInt16(bytesA, 6), BitConverter.ToUInt16(bytesA, 6));
        AssertAllAreEqual(results.TotalPlayTime, containerA.TotalPlayTime, containerB.TotalPlayTime, metaA[2], metaB[2]);

        if (results.BaseVersion < 4140) // Waypoint
            return;

        AssertAllAreEqual(results.SaveName, containerA.SaveName, containerB.SaveName, GetString(bytesA.Skip(20).TakeWhile(i => i != 0)), GetString(bytesB.Skip(20).TakeWhile(i => i != 0)));
        AssertAllAreEqual(results.SaveSummary, containerA.SaveSummary, containerB.SaveSummary, GetString(bytesA.Skip(148).TakeWhile(i => i != 0)), GetString(bytesB.Skip(148).TakeWhile(i => i != 0)));
        AssertAllAreEqual(results.Difficulty, (byte)(containerA.Difficulty), (byte)(containerB.Difficulty), bytesA[276], bytesB[276]);
    }

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

        var accountBlobA = File.ReadAllBytes(Path.Combine(path.Replace(DIRECTORY_TESTSUITE_ARCHIVE, DIRECTORY_TESTSUITE_ARCHIVE_TEMPLATE), "8C69E739E59646B995E48ACB5B01E16A", "container.23"));
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

        var accountBlobDataA = new FileInfo(Path.Combine(path.Replace(DIRECTORY_TESTSUITE_ARCHIVE, DIRECTORY_TESTSUITE_ARCHIVE_TEMPLATE), "8C69E739E59646B995E48ACB5B01E16A", "DB416844920844AE9C1B9693597B9BC0"));
        var accountBlobMetaA = new FileInfo(Path.Combine(path.Replace(DIRECTORY_TESTSUITE_ARCHIVE, DIRECTORY_TESTSUITE_ARCHIVE_TEMPLATE), "8C69E739E59646B995E48ACB5B01E16A", "2F87BC5994B24BF28F34369C6B0A2C2B"));

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

            var blobBlobA = File.ReadAllBytes(Path.Combine(path.Replace(DIRECTORY_TESTSUITE_ARCHIVE, DIRECTORY_TESTSUITE_ARCHIVE_TEMPLATE), blobGuidA, $"container.{blobContainerExtensionA}"));
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

            var blobBlobDataA = new FileInfo(Path.Combine(path.Replace(DIRECTORY_TESTSUITE_ARCHIVE, DIRECTORY_TESTSUITE_ARCHIVE_TEMPLATE), blobGuidA, blobBlobDataNameA));
            var blobBlobMetaA = new FileInfo(Path.Combine(path.Replace(DIRECTORY_TESTSUITE_ARCHIVE, DIRECTORY_TESTSUITE_ARCHIVE_TEMPLATE), blobGuidA, blobBlobMetaNameA));

            var blobBlobDataB = new FileInfo(Path.Combine(path, blobGuidB, blobBlobDataNameB));
            var blobBlobMetaB = new FileInfo(Path.Combine(path, blobGuidB, blobBlobMetaNameB));

            Assert.AreEqual(blobSizeDiskA, blobBlobDataA.Length + blobBlobMetaA.Length);
            Assert.AreEqual(blobSizeDiskB, blobBlobDataB.Length + blobBlobMetaB.Length);
        }
    }

    #endregion

    [TestMethod]
    public void T101_Read_0009000000C73498()
    {
        // Arrange
        var expectAccountData = true;
        var path = GetCombinedPath("Microsoft", "wgs", "0009000000C73498_29070100B936489ABCE8B9AF3980429C");
        var results = new ReadResults[]
        {
            new(2, "Slot2Auto", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, 4647, GameVersionEnum.Frontiers, "", "", 423841),
            new(3, "Slot2Manual", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, 4647, GameVersionEnum.Frontiers, "", "", 419023),
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonRead<PlatformMicrosoft>(path, results, expectAccountData, userIdentification);
    }

    [TestMethod]
    public void T102_Read_000901F4E735CFAC()
    {
        // Arrange
        var expectAccountData = true;
        var path = GetCombinedPath("Microsoft", "wgs", "000901F4E735CFAC_29070100B936489ABCE8B9AF3980429C");
        var results = new ReadResults[]
        {
            new(0, "Slot1Auto", true, true, false, true, true, true, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4138, 4650, GameVersionEnum.Outlaws, "", "", 167579),
            new(1, "Slot1Manual", true, true, false, true, true, true, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4138, 4650, GameVersionEnum.Outlaws, "", "", 167475),

            new(2, "Slot2Auto", true, true, false, true, true, false, true, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Seasonal), DifficultyPresetTypeEnum.Normal, SeasonEnum.Exobiology, 4137, 597033, GameVersionEnum.SentinelWithVehicleAI, "", "", 32652),
            new(3, "Slot2Manual", true, true, false, true, true, false, true, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Seasonal), DifficultyPresetTypeEnum.Normal, SeasonEnum.Exobiology, 4137, 597033, GameVersionEnum.SentinelWithVehicleAI, "", "", 16416),

            new(4, "Slot3Auto", true, true, false, true, true, false, true, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Seasonal), DifficultyPresetTypeEnum.Normal, SeasonEnum.Blighted, 4138, 662570, GameVersionEnum.Outlaws, "", "", 9371),
            new(5, "Slot3Manual", true, true, false, true, true, false, true, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Seasonal), DifficultyPresetTypeEnum.Normal, SeasonEnum.Blighted, 4138, 662570, GameVersionEnum.Outlaws, "", "", 1994),
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonRead<PlatformMicrosoft>(path, results, expectAccountData, userIdentification);
    }

    [TestMethod]
    public void T103_Read_000901F8A36808E0()
    {
        // Arrange
        var expectAccountData = true;
        var path = GetCombinedPath("Microsoft", "wgs", "000901F8A36808E0_29070100B936489ABCE8B9AF3980429C");
        var results = new ReadResults[]
        {
            new(0, "Slot1Auto", true, true, false, true, true, true, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4143, 4655, GameVersionEnum.Fractal, "1. Haupt", "Auf dem Frachter (WF-4 Dawajima)", 635119),
            new(1, "Slot1Manual", true, true, false, true, true, true, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4143, 4655, GameVersionEnum.Fractal, "1. Haupt", "Auf dem Frachter (WF-4 Dawajima)", 635125),

            new(2, "Slot2Auto", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Survival, SeasonEnum.None, 4142, 4654, GameVersionEnum.WaypointWithSuperchargedSlots, "", "An Bord von „Otfolk“-Station Majoris", 29745),
            new(3, "Slot2Manual", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Survival, SeasonEnum.None, 4142, 4654, GameVersionEnum.WaypointWithSuperchargedSlots, "", "An Bord von „Otfolk“-Station Majoris", 29748),

            new(4, "Slot3Auto", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Permadeath), DifficultyPresetTypeEnum.Permadeath, SeasonEnum.None, 4142, 6702, GameVersionEnum.WaypointWithSuperchargedSlots, "", "An Bord von Akelmon-Yelan Primus", 23986),
            new(5, "Slot3Manual", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Permadeath), DifficultyPresetTypeEnum.Permadeath, SeasonEnum.None, 4142, 6702, GameVersionEnum.WaypointWithSuperchargedSlots, "", "An Bord von Akelmon-Yelan Primus", 23077),

            new(6, "Slot4Auto", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4143, 4655, GameVersionEnum.Fractal, "2. Haupt", "Auf dem Frachter (WF-4 Dawajima)", 60155),
            new(7, "Slot4Manual", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4143, 4655, GameVersionEnum.Fractal, "2. Haupt", "Auf dem Frachter (WF-4 Dawajima)", 60173),

            new(8, "Slot5Auto", true, true, false, true, true, false, true, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Seasonal), DifficultyPresetTypeEnum.Normal, SeasonEnum.Utopia, 4143, 1121327, GameVersionEnum.Fractal, "Utopia", "An Bord von „Pivogarde III“-Zentrum", 14155),
            new(9, "Slot5Manual", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4143, 4655, GameVersionEnum.Fractal, "Utopia", "An Bord von „Pivogarde III“-Zentrum", 14195),
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonRead<PlatformMicrosoft>(path, results, expectAccountData, userIdentification);
    }

    [TestMethod]
    public void T104_Read_000901FB44140B02()
    {
        // Arrange
        var expectAccountData = false;
        var path = GetCombinedPath("Microsoft", "wgs", "000901FB44140B02_29070100B936489ABCE8B9AF3980429C");
        var results = new ReadResults[]
        {
            new(0, "Slot1Auto", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4134, 4646, GameVersionEnum.PrismsWithByteBeatAuthor, "", "", 256522),

            new(2, "Slot2Auto", true, true, false, false, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Permadeath), DifficultyPresetTypeEnum.Permadeath, SeasonEnum.None, 4127, 6687, GameVersionEnum.Companions, "", "", 403),

            new(4, "Slot3Auto", true, true, false, false, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Survival), DifficultyPresetTypeEnum.Survival, SeasonEnum.None, 4133, 5669, GameVersionEnum.Beachhead, "", "", 4136),
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonRead<PlatformMicrosoft>(path, results, expectAccountData, userIdentification);
    }

    [TestMethod]
    public void T105_Read_000901FE2C5492FC()
    {
        // Arrange
        var expectAccountData = false;
        var path = GetCombinedPath("Microsoft", "wgs", "000901FE2C5492FC_29070100B936489ABCE8B9AF3980429C");
        var results = new ReadResults[]
        {
            new(1, "Slot1Manual", true, true, false, true, true, true, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, 4647, GameVersionEnum.Emergence, "", "", 64807),
        };

        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonRead<PlatformMicrosoft>(path, results, expectAccountData, userIdentification);
    }

    [TestMethod]
    public void T106_Read_000901FFCAB85905()
    {
        // Arrange
        var expectAccountData = false;
        var path = GetCombinedPath("Microsoft", "wgs", "000901FFCAB85905_29070100B936489ABCE8B9AF3980429C");
        var results = new ReadResults[]
        {
            new(0, "Slot1Auto", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4138, 4650, GameVersionEnum.Outlaws, "", "", 240856),
            new(1, "Slot1Manual", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4138, 4650, GameVersionEnum.Outlaws, "", "", 240851),
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonRead<PlatformMicrosoft>(path, results, expectAccountData, userIdentification);
    }

    [TestMethod]
    public void T107_Read_00090000025A963A()
    {
        // Arrange
        var expectAccountData = true;
        var path = GetCombinedPath("Microsoft", "wgs", "00090000025A963A_29070100B936489ABCE8B9AF3980429C");
        var results = new ReadResults[]
        {
            new(0, "Slot1Auto", true, true, false, false, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Creative), DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4142, 4654, GameVersionEnum.WaypointWithSuperchargedSlots, "Test56789012345678901234567890123456789012", "An Bord von „Negfengf“-Station Majoris", 362), // for some reason SaveVersion has still the old format
            new(1, "Slot1Manual", true, true, false, false, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Creative), DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4140, 4652, GameVersionEnum.Waypoint, "Test56789012345678901234567890123456789012", "An Bord von „Negfengf“-Station Majoris", 344),

            new(2, "Slot2Auto", true, true, false, true, true, false, true, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Seasonal), DifficultyPresetTypeEnum.Normal, SeasonEnum.Polestar, 4139, 793643, GameVersionEnum.Endurance, "", "", 32),

            new(6, "Slot4Auto", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, 4647, GameVersionEnum.Emergence, "", "", 773474),
            new(7, "Slot4Manual", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4124, 4636, GameVersionEnum.LivingShip, "", "", 771852),
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonRead<PlatformMicrosoft>(path, results, expectAccountData, userIdentification);
    }

    [TestMethod]
    public void T108_Read_00090000025A963A_0x7D3_Worlds()
    {
        // Arrange
        var expectAccountData = true;
        var path = GetCombinedPath("Microsoft", "wgs", "00090000025A963A_29070100B936489ABCE8B9AF3980429C_0x7D3_Worlds");
        var results = new ReadResults[]
        {
            new(0, "Slot1Auto", true, true, false, false, false, false, false, false, SaveContextQueryEnum.Main, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4153, 4665, GameVersionEnum.WorldsPartI, "Test56789012345678901234567890123456789012", "An Bord von „Negfengf“-Station Majoris", 378),
            new(1, "Slot1Manual", true, true, false, false, false, false, false, false, SaveContextQueryEnum.Main, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4153, 4665, GameVersionEnum.WorldsPartI, "Test56789012345678901234567890123456789012", "An Bord von „Negfengf“-Station Majoris", 388),

            new(2, "Slot2Auto", true, true, false, true, true, false, true, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Seasonal), DifficultyPresetTypeEnum.Normal, SeasonEnum.Voyagers, 4146, 1252402, GameVersionEnum.Echoes,"Voyagers", "Auf dem Planeten (Lehave)", 6072),
            new(3, "Slot2Manual", true, true, false, true, true, false, true, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Seasonal), DifficultyPresetTypeEnum.Normal, SeasonEnum.Voyagers, 4146, 1252402, GameVersionEnum.Echoes,"Voyagers", "Auf dem Planeten (Lehave)", 5936),

            new(4, "Slot3Auto", true, true, false, true, true, false, true, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Seasonal), DifficultyPresetTypeEnum.Normal, SeasonEnum.Voyagers, 4146, 1252402, GameVersionEnum.Echoes, "Reisende", "Auf dem Planeten (Leigha Yamak)", 425),
            new(5, "Slot3Manual", true, true, false, true, true, false, true, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Seasonal), DifficultyPresetTypeEnum.Normal, SeasonEnum.Voyagers, 4146, 1252402, GameVersionEnum.Echoes, "Reisende","Auf dem Planeten (Leigha Yamak)", 416),

            new(6, "Slot4Auto", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, 4647, GameVersionEnum.Emergence, "�\u0001", "", 773474),
            new(7, "Slot4Manual", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4124, 4636, GameVersionEnum.LivingShip, "", "", 771852),

            new(8, "Slot5Auto", true, true, false, true, false, false, false, false, SaveContextQueryEnum.Main, nameof(PresetGameModeEnum.Permadeath), DifficultyPresetTypeEnum.Permadeath, SeasonEnum.None, 4149, 6709, GameVersionEnum.OmegaWithMicrosoftV2, "The Final Frontier","Within Wemexb Colony", 2964),
            new(9, "Slot5Manual", true, true, false, true, false, false, false, false, SaveContextQueryEnum.Main, nameof(PresetGameModeEnum.Permadeath), DifficultyPresetTypeEnum.Permadeath, SeasonEnum.None, 4149, 6709, GameVersionEnum.OmegaWithMicrosoftV2, "The Final Frontier", "Innerhalb von Wemexb Colony", 3003),
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonRead<PlatformMicrosoft>(path, results, expectAccountData, userIdentification);
    }

    [TestMethod]
    public void T109_Read_000900000104066F()
    {
        // Arrange
        var expectAccountData = true;
        var path = GetCombinedPath("Microsoft", "wgs", "000900000104066F_29070100B936489ABCE8B9AF3980429C");
        var results = new ReadResults[]
        {
            new(0, "Slot1Auto", true, true, false, true, true, true, false, false, SaveContextQueryEnum.Main, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4149, 4661, GameVersionEnum.OmegaWithMicrosoftV2, "", "On freighter (Spear of Benevolence)", 169127),
            new(1, "Slot1Manual", true, true, false, true, true, true, false, false, SaveContextQueryEnum.Main, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4149, 4661, GameVersionEnum.OmegaWithMicrosoftV2, "", "On freighter (Spear of Benevolence)", 169144),
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonRead<PlatformMicrosoft>(path, results, expectAccountData, userIdentification);
    }

    /// <summary>
    /// Same as <see cref="T106_Read_000901FFCAB85905"/>.
    /// </summary>
    [TestMethod]
    public void T110_Read_NoAccountInDirectory()
    {
        // Arrange
        var expectAccountData = false;
        var path = GetCombinedPath("Microsoft", "something");
        var results = new ReadResults[]
        {
            new(0, "Slot1Auto", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4138, 4650, GameVersionEnum.Outlaws, "", "", 240856),
            new(1, "Slot1Manual", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4138, 4650, GameVersionEnum.Outlaws, "", "", 240851),
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonRead<PlatformMicrosoft>(path, results, expectAccountData, userIdentification);
    }

    /// <summary>
    /// Same as <see cref="CommonTestClass.TestCommonWriteDefaultAccount"/> but with asserts.
    /// </summary>
    [TestMethod]
    public void T200_Write_Default_ContainersIndex()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var originMusicVolume = 80; // 80
        var originUtcTicks = 638264331709580000; // 2023-07-31 20:46:10 +00:00
        var path = GetCombinedPath("Microsoft", "wgs", "00090000025A963A_29070100B936489ABCE8B9AF3980429C");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            UseMapping = true,
        };
        var writeCallback = false;

        // Act
        var platformA = new PlatformMicrosoft(path, settings);
        var containerA = platformA.GetAccountContainer();
        Guard.IsNotNull(containerA);
        var containersIndexA = File.ReadAllBytes(Path.Combine(path, "containers.index"));

        containerA.PropertiesChangedCallback += () =>
        {
            writeCallback = true;
        };

        platformA.Load(containerA);
        (int MusicVolume, long UtcTicks) valuesOrigin = (containerA.GetJsonValue<int>(MUSICVOLUME_JSONPATH), containerA.LastWriteTime!.Value.UtcTicks);

        containerA.SetJsonValue(MUSICVOLUME_NEW_AMOUNT, MUSICVOLUME_JSONPATH);
        platformA.Write(containerA, now);
        (int MusicVolume, long UtcTicks) valuesSet = (containerA.GetJsonValue<int>(MUSICVOLUME_JSONPATH), containerA.LastWriteTime!.Value.UtcTicks);

        var platformB = new PlatformMicrosoft(path, settings);
        var containerB = platformB.GetAccountContainer();
        Guard.IsNotNull(containerB);
        var containersIndexB = File.ReadAllBytes(Path.Combine(path, "containers.index"));

        platformB.Load(containerB);
        (int MusicVolume, long UtcTicks) valuesReload = (containerB.GetJsonValue<int>(MUSICVOLUME_JSONPATH), containerB.LastWriteTime!.Value.UtcTicks);

        // Assert
        Assert.IsTrue(writeCallback);

        AssertCommonWriteValues(originMusicVolume, originUtcTicks, valuesOrigin);
        AssertCommonWriteValues(MUSICVOLUME_NEW_AMOUNT, now.UtcTicks, valuesSet);
        AssertCommonWriteValues(MUSICVOLUME_NEW_AMOUNT, now.UtcTicks, valuesReload);

        AssertContainersIndex(containersIndexA, containersIndexB, now, path);
    }

    [TestMethod]
    public void T201_Write_Default_Account()
    {
        // Arrange
        var originMusicVolume = 80; // 80
        var originUtcTicks = 638264331709580000; // 2023-07-31 20:46:10 +00:00
        var path = GetCombinedPath("Microsoft", "wgs", "00090000025A963A_29070100B936489ABCE8B9AF3980429C");

        // Act
        // Assert
        TestCommonWriteDefaultAccount<PlatformMicrosoft>(path, originMusicVolume, originUtcTicks, DecryptMeta, AssertCommonMeta);
    }

    [TestMethod]
    public void T202_Write_Default()
    {
        // Arrange
        var containerIndex = 0;
        var originUnits = 1504909789; // 1,504,909,789
        var originUtcTicks = 638126763444620000; // 2023-02-22 15:25:44 +00:00
        var path = GetCombinedPath("Microsoft", "wgs", "000901F8A36808E0_29070100B936489ABCE8B9AF3980429C");
        var results = new WriteResults(uint.MaxValue, 4143, (ushort)(PresetGameModeEnum.Normal), (ushort)(SeasonEnum.None), 635119, "1. Haupt", "Auf dem Frachter (WF-4 Dawajima)", (byte)(DifficultyPresetTypeEnum.Custom));

        // Act
        // Assert
        TestCommonWriteDefaultSave<PlatformMicrosoft>(path, containerIndex, originUnits, originUtcTicks, results, DecryptMeta, AssertCommonMeta, AssertSpecificMeta);
    }

    [TestMethod]
    public void T203_Write_Default_HGSAVEV2_Omega()
    {
        // Arrange
        var containerIndex = 0;
        var originUnits = 252495937; // 252,495,937
        var originUtcTicks = 638452693524090000; // 2023-03-05 21:02:32 +00:00
        var path = GetCombinedPath("Microsoft", "wgs", "000900000104066F_29070100B936489ABCE8B9AF3980429C");
        var results = new WriteResults(uint.MaxValue, 4149, (ushort)(PresetGameModeEnum.Normal), (ushort)(SeasonEnum.None), 169127, "", "On freighter (Spear of Benevolence)", (byte)(DifficultyPresetTypeEnum.Normal));

        // Act
        // Assert
        TestCommonWriteDefaultSave<PlatformMicrosoft>(path, containerIndex, originUnits, originUtcTicks, results, DecryptMeta, AssertCommonMeta, AssertSpecificMeta);
    }

    [TestMethod]
    public void T204_Write_Default_0x7D3_Worlds()
    {
        // Arrange
        var containerIndex = 0;
        var originUnits = 20221021; // 20,221,021
        var originUtcTicks = 638575130110000000; // 2024-07-25 14:03:31 +00:00
        var path = GetCombinedPath("Microsoft", "wgs", "00090000025A963A_29070100B936489ABCE8B9AF3980429C_0x7D3_Worlds");
        var results = new WriteResults(uint.MaxValue, 4153, (ushort)(PresetGameModeEnum.Normal), (ushort)(SeasonEnum.None), 378, "Test56789012345678901234567890123456789012", "An Bord von „Negfengf“-Station Majoris", (byte)(DifficultyPresetTypeEnum.Creative));
    
        // Act
        // Assert
        TestCommonWriteDefaultSave<PlatformMicrosoft>(path, containerIndex, originUnits, originUtcTicks, results, DecryptMeta, AssertCommonMeta, AssertSpecificMeta);
    }

    [TestMethod]
    public void T210_Write_SetLastWriteTime_False()
    {
        // Arrange
        var containerIndex = 0;
        var originUnits = 1504909789; // 1,504,909,789
        var originUtcTicks = 638126763444620000; // 2023-02-22 15:25:44 +00:00
        var path = GetCombinedPath("Microsoft", "wgs", "000901F8A36808E0_29070100B936489ABCE8B9AF3980429C");

        // Act
        // Assert
        TestCommonWriteSetLastWriteTimeFalse<PlatformMicrosoft>(path, containerIndex, originUnits, originUtcTicks);
    }

    [TestMethod]
    public void T220_Write_WriteAlways_False()
    {
        // Arrange
        var containerIndex = 0;
        var path = GetCombinedPath("Microsoft", "wgs", "000901F8A36808E0_29070100B936489ABCE8B9AF3980429C");

        // Act
        // Assert
        TestCommonWriteWriteAlwaysFalse<PlatformMicrosoft>(path, containerIndex);
    }

    [TestMethod]
    public void T221_Write_WriteAlways_True()
    {
        // Arrange
        var containerIndex = 0;
        var path = GetCombinedPath("Microsoft", "wgs", "000901F8A36808E0_29070100B936489ABCE8B9AF3980429C");

        // Act
        // Assert
        TestCommonWriteWriteAlwaysTrue<PlatformMicrosoft>(path, containerIndex);
    }

    [TestMethod]
    public void T300_FileSystemWatcher()
    {
        // Arrange
        var containerIndex = 0;
        var path = GetCombinedPath("Microsoft", "wgs", "000901FB44140B02_29070100B936489ABCE8B9AF3980429C");
        var pathWatching = Path.Combine(path, "containers.index");

        // Act
        // Assert
        TestCommonFileSystemWatcher<PlatformMicrosoft>(path, pathWatching, containerIndex);
    }

    [TestMethod]
    public void T301_Copy()
    {
        // Arrange
        var copyOverwrite = new[] { 4, 1 }; // 3Auto -> 1Manual (overwrite)
        var copyCreate = new[] { 2, 8 }; // 2Auto -> 5Auto (create)
        var copyDelete = new[] { 9, 0 }; // 5Manual -> 1Auto (delete)
        var path = GetCombinedPath("Microsoft", "wgs", "000901FB44140B02_29070100B936489ABCE8B9AF3980429C");

        // Act
        // Assert
        TestCommonFileOperationCopy<PlatformMicrosoft>(path, copyOverwrite, copyCreate, copyDelete);
    }

    [TestMethod]
    public void T302_Delete()
    {
        // Arrange
        var deleteDelete = new[] { 0, 1 }; // 1Auto, 1Manual
        var path = GetCombinedPath("Microsoft", "wgs", "000901FB44140B02_29070100B936489ABCE8B9AF3980429C");

        // Act
        // Assert
        TestCommonFileOperationDelete<PlatformMicrosoft>(path, deleteDelete);
    }

    [TestMethod]
    public void T303_Move()
    {
        // Arrange
        var moveCopy = new[] { 4, 5 }; // 3Auto -> 3Manual
        var moveOverwrite = new[] { 2, 5 }; // 2Auto -> 3Manual (overwrite)
        var moveDelete = new[] { 1, 0 }; // 1Manual -> 1Auto (delete) // 1 is corrupted, therefore 0 gets deleted and then 1 is also deleted after copying.
        var moveCreate = new[] { 4, 9 }; // 3Auto -> 5Manual (create)
        var path = GetCombinedPath("Microsoft", "wgs", "000901FB44140B02_29070100B936489ABCE8B9AF3980429C");

        // Act
        // Assert
        TestCommonFileOperationMove<PlatformMicrosoft>(path, moveCopy, moveOverwrite, moveDelete, moveCreate);
    }

    [TestMethod]
    public void T304_Swap()
    {
        // Arrange
        var path = GetCombinedPath("Microsoft", "wgs", "000901F8A36808E0_29070100B936489ABCE8B9AF3980429C");
        var results = new ReadResults[]
        {
            // before swap 3, "Slot2Manual"
            new(8, "Slot5Auto", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Survival, SeasonEnum.None, 4142, 4654, GameVersionEnum.WaypointWithSuperchargedSlots, "", "An Bord von „Otfolk“-Station Majoris", 29748),

            // before swap 8, "Slot5Auto"
            new(3, "Slot2Manual", true, true, false, true, true, false, true, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Seasonal), DifficultyPresetTypeEnum.Normal, SeasonEnum.Utopia, 4143, 1121327, GameVersionEnum.Fractal, "Utopia", "An Bord von „Pivogarde III“-Zentrum", 14155),
        };
        var swapSwap = new[] { 3, 8 }; // 2Manual <-> 5Auto

        // Act
        // Assert
        TestCommonFileOperationSwap<PlatformMicrosoft>(path, results, swapSwap);
    }

    [TestMethod]
    public void T400_TransferFromGog()
    {
        // Arrange
        var pathGog = GetCombinedPath("Gog", "DefaultUser");
        var resultsGog = new ReadResults[]
        {
            new(2, "Slot2Auto", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, 4647, GameVersionEnum.Emergence, "", "", 19977),
            new(3, "Slot2Manual", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, 4647, GameVersionEnum.Emergence, "", "", 5048),
        };
        var slotGog = 1; // get Slot2
        var userDecisionsGog = 1;
        var userIdentificationGog = ReadUserIdentification(pathGog);

        var existingContainersCount = 8; // 6 + 2 (Slot?)
        var path = GetCombinedPath("Microsoft", "wgs", "000901F4E735CFAC_29070100B936489ABCE8B9AF3980429C");
        var transfer = new[] { 2, 3 }; // overwrite Slot3 // create Slot4
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonFileOperationTransfer<PlatformMicrosoft, PlatformGog>(pathGog, path, userIdentificationGog, userIdentification, slotGog, userDecisionsGog, transfer, existingContainersCount, resultsGog);
    }

    [TestMethod]
    public void T401_TransferFromMicrosoft()
    {
        // Arrange
        var pathMicrosoft = GetCombinedPath("Microsoft", "wgs", "0009000000C73498_29070100B936489ABCE8B9AF3980429C");
        var resultsMicrosoft = new ReadResults[]
        {
            new(2, "Slot2Auto", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, 4647, GameVersionEnum.Frontiers, "", "", 423841),
            new(3, "Slot2Manual", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, 4647, GameVersionEnum.Frontiers, "", "", 419023),
        };
        var slotMicrosoft = 1; // get Slot2
        var userDecisionsMicrosoft = 8;
        var userIdentificationMicrosoft = ReadUserIdentification(pathMicrosoft);

        var existingContainersCount = 8; // 6 + 2 (Slot?)
        var path = GetCombinedPath("Microsoft", "wgs", "000901F4E735CFAC_29070100B936489ABCE8B9AF3980429C");
        var transfer = new[] { 2, 3 }; // overwrite Slot3 // create Slot4
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonFileOperationTransfer<PlatformMicrosoft, PlatformMicrosoft>(pathMicrosoft, path, userIdentificationMicrosoft, userIdentification, slotMicrosoft, userDecisionsMicrosoft, transfer, existingContainersCount, resultsMicrosoft);
    }

    [TestMethod]
    public void T402_TransferFromPlaystation_0x7D1()
    {
        // Arrange
        var pathPlaystation = GetCombinedPath("Playstation", "0x7D1", "SaveWizard", "1");
        var resultsPlaystation = new ReadResults[]
        {
            new(2, "Slot2Auto", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4134, 4646, GameVersionEnum.PrismsWithByteBeatAuthor, "", "", 598862),
            new(3, "Slot2Manual", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4134, 4646, GameVersionEnum.PrismsWithByteBeatAuthor, "", "", 598818),
        };
        var slotPlaystation = 1; // get Slot2
        var userDecisionsPlaystation = 24;
        var userIdentificationPlaystation = ReadUserIdentification(pathPlaystation);

        var existingContainersCount = 8; // 6 + 2 (Slot?)
        var path = GetCombinedPath("Microsoft", "wgs", "000901F4E735CFAC_29070100B936489ABCE8B9AF3980429C");
        var transfer = new[] { 2, 3 }; // overwrite Slot3 // create Slot4
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonFileOperationTransfer<PlatformMicrosoft, PlatformPlaystation>(pathPlaystation, path, userIdentificationPlaystation, userIdentification, slotPlaystation, userDecisionsPlaystation, transfer, existingContainersCount, resultsPlaystation);
    }

    [TestMethod]
    public void T403_TransferFromPlaystation_0x7D2()
    {
        // Arrange
        var pathPlaystation = GetCombinedPath("Playstation", "0x7D2", "SaveWizard", "4");
        var resultsPlaystation = new ReadResults[]
        {
            new(2, "Slot2Auto", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, 4647, GameVersionEnum.Frontiers, "", "", 101604),
            new(3, "Slot2Manual", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, 4647, GameVersionEnum.Frontiers, "", "", 101653),
        };
        var slotPlaystation = 1; // get Slot2
        var userDecisionsPlaystation = 4;
        var userIdentificationPlaystation = ReadUserIdentification(pathPlaystation);

        var existingContainersCount = 8; // 6 + 2 (Slot?)
        var path = GetCombinedPath("Microsoft", "wgs", "000901F4E735CFAC_29070100B936489ABCE8B9AF3980429C");
        var transfer = new[] { 2, 3 }; // overwrite Slot3 // create Slot4
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonFileOperationTransfer<PlatformMicrosoft, PlatformPlaystation>(pathPlaystation, path, userIdentificationPlaystation, userIdentification, slotPlaystation, userDecisionsPlaystation, transfer, existingContainersCount, resultsPlaystation);
    }

    [TestMethod]
    public void T404_TransferFromSteam()
    {
        // Arrange
        var pathSteam = GetCombinedPath("Steam", "st_76561198371877533");
        var resultsSteam = new ReadResults[]
        {
            new(2, "Slot2Auto", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Creative), DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4127, 5151, GameVersionEnum.Companions, "", "", 4765),
            new(3, "Slot2Manual", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Creative), DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4127, 5151, GameVersionEnum.Companions, "", "", 4271),
        };
        var slotSteam = 1; // get Slot2
        var userDecisionsSteam = 2;
        var userIdentificationSteam = ReadUserIdentification(pathSteam);

        var existingContainersCount = 8; // 6 + 2 (Slot?)
        var path = GetCombinedPath("Microsoft", "wgs", "000901F4E735CFAC_29070100B936489ABCE8B9AF3980429C");
        var transfer = new[] { 2, 3 }; // overwrite Slot3 // create Slot4
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonFileOperationTransfer<PlatformMicrosoft, PlatformSteam>(pathSteam, path, userIdentificationSteam, userIdentification, slotSteam, userDecisionsSteam, transfer, existingContainersCount, resultsSteam);
    }

    [TestMethod]
    public void T405_TransferFromSwitch()
    {
        // Arrange
        var pathSwitch = GetCombinedPath("Switch", "4");
        var resultsSwitch = new ReadResults[]
        {
            new(2, "Slot2Auto", true, true, false, false, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Survival), DifficultyPresetTypeEnum.Survival, SeasonEnum.None, 4139, 5675, GameVersionEnum.Endurance, "", "", 336),
        };
        var slotSwitch = 1; // get Slot2
        var userDecisionsSwitch = 0;
        var userIdentificationSwitch = ReadUserIdentification(pathSwitch);

        var existingContainersCount = 6; // 6 - 1 (Slot?) + 1 (Slot?)
        var path = GetCombinedPath("Microsoft", "wgs", "000901F4E735CFAC_29070100B936489ABCE8B9AF3980429C");
        var transfer = new[] { 2, 3 }; // overwrite Slot3 // create Slot4
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonFileOperationTransfer<PlatformMicrosoft, PlatformSwitch>(pathSwitch, path, userIdentificationSwitch, userIdentification, slotSwitch, userDecisionsSwitch, transfer, existingContainersCount, resultsSwitch);
    }
}
