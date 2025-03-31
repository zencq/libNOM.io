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
public class SwitchTest : CommonTestClass
{
    #region Constant

    protected const uint META_HEADER = 0xCA55E77E;
    protected const int META_LENGTH_TOTAL_VANILLA = 0x64 / sizeof(uint); // 25
    protected const int META_LENGTH_TOTAL_WAYPOINT = 0x164 / sizeof(uint); // 89
    protected const int META_LENGTH_TOTAL_WORLDS_PART_I = 0x174 / sizeof(uint); // 93
    protected const int META_LENGTH_TOTAL_WORLDS_PART_II = 0x17C / sizeof(uint); // 95

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
            AssertAllAreEqual(META_HEADER, metaA[0], metaB[0]);
            AssertAllAreEqual(META_FORMAT_2, metaA[1], metaB[1]);

            if (!container.IsAccount)
            {
                // Changes to latest edited save in AccountData.
                AssertAllAreEqual(container.MetaIndex, metaA[3], metaB[3]);

                // TIMESTAMP
                Assert.IsTrue(metaA[4] < metaB[4]);
            }

            // Skip DECOMPRESSED SIZE and META INDEX and TIMESTAMP.
            Assert.IsTrue(metaA.Skip(5).SequenceEqual(metaB.Skip(5)));
        }
        else if (metaA.Length == META_LENGTH_TOTAL_WORLDS_PART_I)
        {
            AssertAllAreEqual(META_HEADER, metaA[0], metaB[0]);
            AssertAllAreEqual(META_FORMAT_3, metaA[1], metaB[1]);

            if (!container.IsAccount)
            {
                // Changes to latest edited save in AccountData.
                AssertAllAreEqual(container.MetaIndex, metaA[3], metaB[3]);

                // TIMESTAMP
                Assert.AreEqual(metaA[4], metaA[77]);
                Assert.AreEqual(metaB[4], metaB[77]);

                Assert.IsTrue(metaA[4] < metaB[4]);
            }

            // Skip DECOMPRESSED SIZE and META INDEX and TIMESTAMP.
            Assert.IsTrue(metaA.Skip(5).Take(72).SequenceEqual(metaB.Skip(5).Take(72)));

            // Skip TIMESTAMP.
            Assert.IsTrue(metaA.Skip(78).SequenceEqual(metaB.Skip(78)));
        }
        else if (metaA.Length == META_LENGTH_TOTAL_WORLDS_PART_II)
        {
            AssertAllAreEqual(META_HEADER, metaA[0], metaB[0]);
            AssertAllAreEqual(META_FORMAT_4, metaA[1], metaB[1]);

            if (!container.IsAccount)
            {
                // Changes to latest edited save in AccountData.
                // TODO: Unknown value for Worlds Part II.
                // AssertAllAreEqual(container.MetaIndex, metaA[3], metaB[3]);

                // TIMESTAMP
                AssertAllAreEqual(0, metaA[4], metaB[4]);
                Assert.IsTrue(metaA[77] < metaB[77]);
            }

            // Skip DECOMPRESSED SIZE and META INDEX.
            Assert.IsTrue(metaA.Skip(4).Take(73).SequenceEqual(metaB.Skip(4).Take(73)));

            // Skip TIMESTAMP.
            Assert.IsTrue(metaA.Skip(78).SequenceEqual(metaB.Skip(78)));
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

        // TODO: Unknown value for Worlds Part II.
        // AssertAllAreEqual(results.MetaIndex, (uint)(containerA.MetaIndex), (uint)(containerB.MetaIndex), metaA[3], metaB[3]);
        AssertAllAreEqual(results.BaseVersion, (uint)(int)(prijectA.GetFieldOrProperty(nameof(WriteResults.BaseVersion))), (uint)(int)(prijectB.GetFieldOrProperty(nameof(WriteResults.BaseVersion))), metaA[5], metaB[5]);
        AssertAllAreEqual(results.GameMode, (ushort)(prijectA.GetFieldOrProperty(nameof(WriteResults.GameMode))), (ushort)(prijectB.GetFieldOrProperty(nameof(WriteResults.GameMode))), BitConverter.ToInt16(bytesA, 24), BitConverter.ToInt16(bytesB, 24));
        AssertAllAreEqual(results.Season, (ushort)(containerA.Season), (ushort)(containerB.Season), BitConverter.ToUInt16(bytesA, 26), BitConverter.ToUInt16(bytesA, 26));
        AssertAllAreEqual(results.TotalPlayTime, containerA.TotalPlayTime, containerB.TotalPlayTime, BitConverter.ToUInt64(bytesA, 28), BitConverter.ToUInt64(bytesB, 28));

        if (results.BaseVersion < 4140) // Waypoint
            return;

        AssertAllAreEqual(results.SaveName, containerA.SaveName, containerB.SaveName, GetString(bytesA.Skip(40).TakeWhile(i => i != 0)), GetString(bytesB.Skip(40).TakeWhile(i => i != 0)));
        AssertAllAreEqual(results.SaveSummary, containerA.SaveSummary, containerB.SaveSummary, GetString(bytesA.Skip(168).TakeWhile(i => i != 0)), GetString(bytesB.Skip(168).TakeWhile(i => i != 0)));
        AssertAllAreEqual(results.Difficulty, (byte)(containerA.Difficulty), (byte)(containerB.Difficulty), bytesA[296], bytesB[296]);
    }

    #endregion

    [TestMethod]
    public void T101_Read()
    {
        // Arrange
        var expectAccountData = false;
        var path = GetCombinedPath("Switch", "1");
        var results = new ReadResults[]
        {
            new(0, "Slot1Auto", true, true, false, false, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Creative), DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4139, 5163, GameVersionEnum.Endurance, "", "", 18),
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonRead<PlatformSwitch>(path, results, expectAccountData, userIdentification);
    }

    [TestMethod]
    public void T102_Read()
    {
        // Arrange
        var expectAccountData = true;
        var path = GetCombinedPath("Switch", "2");
        var results = new ReadResults[]
        {
            new(0, "Slot1Auto", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4139, 4651, GameVersionEnum.Endurance, "", "", 12655),
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonRead<PlatformSwitch>(path, results, expectAccountData, userIdentification);
    }

    [TestMethod]
    public void T103_Read()
    {
        // Arrange
        var expectAccountData = true;
        var path = GetCombinedPath("Switch", "3");
        var results = new ReadResults[]
        {
            new(0, "Slot1Auto", true, true, false, false, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4139, 4651, GameVersionEnum.Endurance, "", "", 640),
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonRead<PlatformSwitch>(path, results, expectAccountData, userIdentification);
    }

    [TestMethod]
    public void T104_Read()
    {
        // Arrange
        var expectAccountData = true;
        var path = GetCombinedPath("Switch", "4");
        var results = new ReadResults[]
        {
            new(0, "Slot1Auto", true, true, false, false, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4139, 4651, GameVersionEnum.Endurance, "", "", 225),

            new(2, "Slot2Auto", true, true, false, false, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Survival), DifficultyPresetTypeEnum.Survival, SeasonEnum.None, 4139, 5675, GameVersionEnum.Endurance, "", "", 336),

            new(4, "Slot3Auto", true, true, false, false, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Creative), DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4139, 5163, GameVersionEnum.Endurance, "", "", 51),
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonRead<PlatformSwitch>(path, results, expectAccountData, userIdentification);
    }

    [TestMethod]
    public void T105_Read()
    {
        // Arrange
        var expectAccountData = true;
        var path = GetCombinedPath("Switch", "5");
        var results = new ReadResults[]
        {
            new(0, "Slot1Auto", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4145, 4657, GameVersionEnum.Singularity, "", "登上Inzadg球体", 63873),
            new(1, "Slot1Manual", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4145, 4657, GameVersionEnum.Singularity, "", "登上Inzadg球体", 63651),

            new(2, "Slot2Auto", true, true, false, false, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4145, 4657, GameVersionEnum.Singularity, "", "登上太空异象", 88),
            new(3, "Slot2Manual", true, true, false, false, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4145, 4657, GameVersionEnum.Singularity, "", "登上太空异象", 82),
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonRead<PlatformSwitch>(path, results, expectAccountData, userIdentification);
    }

    [TestMethod]
    public void T106_Read()
    {
        // Arrange
        var expectAccountData = true;
        var path = GetCombinedPath("Switch", "6");
        var results = new ReadResults[]
        {
            new(0, "Slot1Auto", true, true, false, true, true, false, false, false, SaveContextQueryEnum.Main, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4153, 4665, GameVersionEnum.WorldsPartI, "", "Ouverm殖民地内", 72800),
            new(1, "Slot1Manual", true, true, false, true, true, false, false, false, SaveContextQueryEnum.Main, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4153, 4665, GameVersionEnum.WorldsPartI, "", "Ouverm殖民地内", 72836),
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonRead<PlatformSwitch>(path, results, expectAccountData, userIdentification);
    }

    [TestMethod]
    public void T107_Read()
    {
        // Arrange
        var expectAccountData = true;
        var path = GetCombinedPath("Switch", "7");
        var results = new ReadResults[]
        {
            new(0, "Slot1Auto", true, true, false, true, true, false, false, false, SaveContextQueryEnum.Main, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4173, 4685, GameVersionEnum.WorldsPartIIWithDifficultyTag, "", "位于Baraso星系", 88479),
            new(1, "Slot1Manual", true, true, false, true, true, false, false, false, SaveContextQueryEnum.Main, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4173, 4685, GameVersionEnum.WorldsPartIIWithDifficultyTag, "", "登上Baraso空间站", 88558),

            new(2, "Slot2Auto", true, true, false, false, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4146, 4658, GameVersionEnum.Echoes, "", "登上太空异象", 117),
            new(3, "Slot2Manual", true, true, false, false, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4146, 4658, GameVersionEnum.Echoes, "", "登上太空异象", 101),
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonRead<PlatformSwitch>(path, results, expectAccountData, userIdentification);
    }

    [TestMethod]
    public void T200_Write_Default_0x7D2_Frontiers_Account()
    {
        // Arrange
        var originMusicVolume = 80; // 80
        var originUtcTicks = 638006787240000000; // 2022-10-06 18:45:24 +00:00
        var path = GetCombinedPath("Switch", "4");

        // Act
        // Assert
        TestCommonWriteDefaultAccount<PlatformSwitch>(path, originMusicVolume, originUtcTicks, DecryptMeta, AssertCommonMeta);
    }

    [TestMethod]
    public void T201_Write_Default_0x7D2_Frontiers()
    {
        // Arrange
        var containerIndex = 4;
        var originUnits = 0; // 0
        var originUtcTicks = 638006823360000000; // 2022-10-06 19:45:36 +00:00
        var path = GetCombinedPath("Switch", "4");
        var results = new WriteResults(6, 4139, (ushort)(PresetGameModeEnum.Creative), (ushort)(SeasonEnum.None), 51, "", "", (byte)(DifficultyPresetTypeEnum.Normal));

        // Act
        // Assert
        TestCommonWriteDefaultSave<PlatformSwitch>(path, containerIndex, originUnits, originUtcTicks, results, DecryptMeta, AssertCommonMeta, AssertSpecificMeta);
    }

    [TestMethod]
    public void T210_Write_Default_0x7D2_Waypoint_Account()
    {
        // Arrange
        var originMusicVolume = 80; // 80
        var originUtcTicks = 638298440840000000; // 2023-09-09 08:14:44 +00:00
        var path = GetCombinedPath("Switch", "5");

        // Act
        // Assert
        TestCommonWriteDefaultAccount<PlatformSwitch>(path, originMusicVolume, originUtcTicks, DecryptMeta, AssertCommonMeta);
    }

    [TestMethod]
    public void T211_Write_Default_0x7D2_Waypoint()
    {
        // Arrange
        var containerIndex = 0;
        var originUnits = 1000356262; // 1.000.356.262
        var originUtcTicks = 638093635960000000; // 2023-01-15 07:13:16 +00:00 (from meta)
        var path = GetCombinedPath("Switch", "5");
        var results = new WriteResults(2, 4145, (ushort)(PresetGameModeEnum.Normal), (ushort)(SeasonEnum.None), 63873, "", "登上Inzadg球体", (byte)(DifficultyPresetTypeEnum.Custom));

        // Act
        // Assert
        TestCommonWriteDefaultSave<PlatformSwitch>(path, containerIndex, originUnits, originUtcTicks, results, DecryptMeta, AssertCommonMeta, AssertSpecificMeta);
    }

    [TestMethod]
    public void T220_Write_Default_0x7D3_WorldsPartI_Account()
    {
        // Arrange
        var originMusicVolume = 80; // 80
        var originUtcTicks = 638749939740000000; // 2025-02-12 21:52:54 +00:00
        var path = GetCombinedPath("Switch", "6");

        // Act
        // Assert
        TestCommonWriteDefaultAccount<PlatformSwitch>(path, originMusicVolume, originUtcTicks, DecryptMeta, AssertCommonMeta);
    }

    [TestMethod]
    public void T221_Write_Default_0x7D3_WorldsPartI()
    {
        // Arrange
        var containerIndex = 0;
        var originUnits = 1001175713; // 1.001.175.713
        var originUtcTicks = 638580996090000000; // 2024-08-01 09:00:09 +00:00 (from meta)
        var path = GetCombinedPath("Switch", "6");
        var results = new WriteResults(2, 4153, (ushort)(PresetGameModeEnum.Normal), (ushort)(SeasonEnum.None), 72800, "", "Ouverm殖民地内", (byte)(DifficultyPresetTypeEnum.Custom));

        // Act
        // Assert
        TestCommonWriteDefaultSave<PlatformSwitch>(path, containerIndex, originUnits, originUtcTicks, results, DecryptMeta, AssertCommonMeta, AssertSpecificMeta);
    }

    [TestMethod]
    public void T230_Write_Default_0x7D4_WorldsPartII_Account()
    {
        // Arrange
        var originMusicVolume = 80; // 80
        var originUtcTicks = 638755870000000000; // 2025-02-19 18:36:40 +00:00
        var path = GetCombinedPath("Switch", "7");

        // Act
        // Assert
        TestCommonWriteDefaultAccount<PlatformSwitch>(path, originMusicVolume, originUtcTicks, DecryptMeta, AssertCommonMeta);
    }

    [TestMethod]
    public void T231_Write_Default_0x7D4_WorldsPartII()
    {
        // Arrange
        var containerIndex = 0;
        var originUnits = 994971933; // 1.000.356.262
        var originUtcTicks = 638755852790000000; // 2025-02-19 18:07:59 +00:00 (from meta)
        var path = GetCombinedPath("Switch", "7");
        var results = new WriteResults(2, 4173, (ushort)(PresetGameModeEnum.Normal), (ushort)(SeasonEnum.None), 88479, "", "位于Baraso星系", (byte)(DifficultyPresetTypeEnum.Custom));

        // Act
        // Assert
        TestCommonWriteDefaultSave<PlatformSwitch>(path, containerIndex, originUnits, originUtcTicks, results, DecryptMeta, AssertCommonMeta, AssertSpecificMeta);
    }

    [TestMethod]
    public void T240_Write_SetLastWriteTime_False()
    {
        // Arrange
        var containerIndex = 0;
        var originUnits = 0; // 0
        var originUtcTicks = 638006282230000000; // 2022-10-06 04:43:43 +00:00 (from meta)
        var path = GetCombinedPath("Switch", "1");

        // Act
        // Assert
        TestCommonWriteSetLastWriteTimeFalse<PlatformSwitch>(path, containerIndex, originUnits, originUtcTicks);
    }

    [TestMethod]
    public void T250_Write_WriteAlways_False()
    {
        var containerIndex = 0;
        var path = GetCombinedPath("Switch", "1");

        // Act
        // Assert
        TestCommonWriteWriteAlwaysFalse<PlatformSwitch>(path, containerIndex);
    }

    [TestMethod]
    public void T251_Write_WriteAlways_True()
    {
        var containerIndex = 0;
        var path = GetCombinedPath("Switch", "1");

        // Act
        // Assert
        TestCommonWriteWriteAlwaysTrue<PlatformSwitch>(path, containerIndex);
    }

    [TestMethod]
    public void T300_FileSystemWatcher()
    {
        var containerIndex = 0;
        var path = GetCombinedPath("Switch", "1");
        var pathWatching = Path.Combine(path, "manifest02.hg");

        // Act
        // Assert
        TestCommonFileSystemWatcher<PlatformSwitch>(path, pathWatching, containerIndex);
    }

    [TestMethod]
    public void T301_Copy()
    {
        // Arrange
        var copyOverwrite = new[] { 0, 2 }; // 1Auto -> 2Auto (overwrite)
        var copyCreate = new[] { 0, 1 }; // 1Auto -> 1Manual (create)
        var copyDelete = new[] { 6, 4 }; // 4Auto -> 3Auto (delete)
        var path = GetCombinedPath("Switch", "4");

        // Act
        // Assert
        TestCommonFileOperationCopy<PlatformSwitch>(path, copyOverwrite, copyCreate, copyDelete);
    }

    [TestMethod]
    public void T302_Delete()
    {
        // Arrange
        var deleteDelete = new[] { 0 }; // 1Auto
        var path = GetCombinedPath("Switch", "4");

        // Act
        // Assert
        TestCommonFileOperationDelete<PlatformSwitch>(path, deleteDelete);
    }

    [TestMethod]
    public void T303_Move()
    {
        // Arrange
        var moveCopy = new[] { 4, 5 }; // 3Auto -> 3Manual
        var moveOverwrite = new[] { 2, 5 }; // 2Auto -> 3Manual (overwrite)
        var moveDelete = new[] { 1, 0 }; // 1Manual -> 1Auto (delete)
        var moveCreate = new[] { 4, 9 }; // 3Auto -> 5Manual (create)
        var path = GetCombinedPath("Switch", "4");

        // Act
        // Assert
        TestCommonFileOperationMove<PlatformSwitch>(path, moveCopy, moveOverwrite, moveDelete, moveCreate);
    }

    [TestMethod]
    public void T304_Swap()
    {
        // Arrange
        var path = GetCombinedPath("Switch", "4");
        var results = new ReadResults[]
        {
            // before swap 2, "Slot2Auto"
            new(4, "Slot3Auto", true, true, false, false, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Survival), DifficultyPresetTypeEnum.Survival, SeasonEnum.None, 4139, 5675, GameVersionEnum.Endurance, "", "", 336),

            // before swap 4, "Slot3Auto"
            new(2, "Slot2Auto", true, true, false, false, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Creative), DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4139, 5163, GameVersionEnum.Endurance, "", "", 51),
        };
        var swapSwap = new[] { 2, 4 }; // 2Auto <-> 3Auto

        // Act
        // Assert
        TestCommonFileOperationSwap<PlatformSwitch>(path, results, swapSwap);
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

        var existingContainersCount = 6; // 3 + 1 (Slot3) + 2 (Slot4)
        var path = GetCombinedPath("Switch", "4");
        var transfer = new[] { 2, 3 }; // overwrite Slot3 // create Slot4
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonFileOperationTransfer<PlatformSwitch, PlatformGog>(pathGog, path, userIdentificationGog, userIdentification, slotGog, userDecisionsGog, transfer, existingContainersCount, resultsGog);
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

        var existingContainersCount = 6; // 3 + 1 (Slot3) + 2 (Slot4)
        var path = GetCombinedPath("Switch", "4");
        var transfer = new[] { 2, 3 }; // overwrite Slot3 // create Slot4
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonFileOperationTransfer<PlatformSwitch, PlatformMicrosoft>(pathMicrosoft, path, userIdentificationMicrosoft, userIdentification, slotMicrosoft, userDecisionsMicrosoft, transfer, existingContainersCount, resultsMicrosoft);
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

        var existingContainersCount = 6; // 3 + 1 (Slot3) + 2 (Slot4)
        var path = GetCombinedPath("Switch", "4");
        var transfer = new[] { 2, 3 }; // overwrite Slot3 // create Slot4
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonFileOperationTransfer<PlatformSwitch, PlatformPlaystation>(pathPlaystation, path, userIdentificationPlaystation, userIdentification, slotPlaystation, userDecisionsPlaystation, transfer, existingContainersCount, resultsPlaystation);
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

        var existingContainersCount = 6; // 3 + 1 (Slot3) + 2 (Slot4)
        var path = GetCombinedPath("Switch", "4");
        var transfer = new[] { 2, 3 }; // overwrite Slot3 // create Slot4
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonFileOperationTransfer<PlatformSwitch, PlatformPlaystation>(pathPlaystation, path, userIdentificationPlaystation, userIdentification, slotPlaystation, userDecisionsPlaystation, transfer, existingContainersCount, resultsPlaystation);
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

        var existingContainersCount = 6; // 3 + 1 (Slot3) + 2 (Slot4)
        var path = GetCombinedPath("Switch", "4");
        var transfer = new[] { 2, 3 }; // overwrite Slot3 // create Slot4
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonFileOperationTransfer<PlatformSwitch, PlatformSteam>(pathSteam, path, userIdentificationSteam, userIdentification, slotSteam, userDecisionsSteam, transfer, existingContainersCount, resultsSteam);
    }

    [TestMethod]
    public void T405_TransferFromSwitch()
    {
        // Arrange
        var pathSwitch = GetCombinedPath("Switch", "1");
        var resultsSwitch = new ReadResults[]
        {
            new(0, "Slot1Auto", true, true, false, false, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Creative), DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4139, 5163, GameVersionEnum.Endurance, "", "", 18),
        };
        var slotSwitch = 0; // get Slot1
        var userDecisionsSwitch = 0;
        var userIdentificationSwitch = ReadUserIdentification(pathSwitch);

        var existingContainersCount = 4; // 3 + 1 (Slot?)
        var path = GetCombinedPath("Switch", "4");
        var transfer = new[] { 2, 3 }; // overwrite Slot3 // create Slot4
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonFileOperationTransfer<PlatformSwitch, PlatformSwitch>(pathSwitch, path, userIdentificationSwitch, userIdentification, slotSwitch, userDecisionsSwitch, transfer, existingContainersCount, resultsSwitch);
    }
}
