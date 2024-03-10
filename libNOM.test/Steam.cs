using System.Text;

using CommunityToolkit.HighPerformance;

using libNOM.io;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace libNOM.test;


// Do not use System.Range for simplicity of the file and performace is not critical.
[TestClass]
[DeploymentItem("../../../Resources/TESTSUITE_ARCHIVE.zip")]
public class SteamTest : CommonTestClass
{
    #region Constant

    private static readonly uint[] META_ENCRYPTION_KEY = Encoding.ASCII.GetBytes("NAESEVADNAYRTNRG").AsSpan().Cast<byte, uint>().ToArray();
    private const uint META_HEADER = 0xEEEEEEBE; // 4.008.636.094
    private const int META_LENGTH_TOTAL_VANILLA = 0x68 / sizeof(uint); // 26
    private const int META_LENGTH_TOTAL_WAYPOINT = 0x168 / sizeof(uint); // 90

    #endregion

    #region Meta

    private static uint[] DecryptMeta(Container container)
    {
        var meta = File.ReadAllBytes(container.MetaFile!.FullName);
        var value = ToUInt32(meta);

        if (value.Length != META_LENGTH_TOTAL_VANILLA && value.Length != META_LENGTH_TOTAL_WAYPOINT)
            return value;

        // Best case is that it works with the value of the file but in case it was moved manually, try all other values as well.
        var enumValues = new int[] { container.IsAccount ? 1 : container.MetaIndex }.Concat(Enumerable.Range(0, 30).Where(i => i > 1 && i != container.MetaIndex));

        foreach (var entry in enumValues)
        {
            // When overwriting META_ENCRYPTION_KEY[0] it can happen that the value is not set afterwards and therefore create a new collection to ensure it will be correct.
            ReadOnlySpan<uint> key = [(RotateLeft((uint)(entry) ^ 0x1422CB8C, 13) * 5) + 0xE6546B64, META_ENCRYPTION_KEY[1], META_ENCRYPTION_KEY[2], META_ENCRYPTION_KEY[3]];

            // DeepCopy as value would be changed otherwise and casting again does not work.
            Span<uint> result = DeepCopy(value);

            uint hash = 0;
            int iterations = value.Length == META_LENGTH_TOTAL_VANILLA ? 8 : 6;
            int lastIndex = result.Length - 1;

            // Results in 0xF1BBCDC8 for SAVE_FORMAT_2 as in the original algorithm.
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
                uint i3 = (result[valueIndex] ^ key[keyIndex]);
                uint i4 = (current ^ hash);
                result[0] -= (i1 + i2) ^ (i3 + i4);

                hash += 0x61C88647;
            }

            if (result[0] == META_HEADER)
                return result.ToArray();
        }

        return value;
    }

    private static void AssertCommonMeta(Container container, uint[] metaA, uint[] metaB)
    {
        Assert.AreEqual(metaA.Length, metaB.Length);

        AssertAllAreEqual(META_HEADER, metaA[0], metaB[0]);

        if (metaA.Length == META_LENGTH_TOTAL_VANILLA)
        {
            if (container.IsAccount || container.IsSave && !container.IsVersion360Frontiers)
            {
                // Editing account data is possible since Frontiers and therefore has always the new format but otherwise uses the old format.
                AssertAllAreEqual(container.IsAccount ? SAVE_FORMAT_3 : SAVE_FORMAT_2, metaA[1], metaB[1]);

                AssertAllNotZero(metaA.Skip(2).Take(4), metaB.Skip(2).Take(4));
                AssertAllNotZero(metaA.Skip(6).Take(8), metaB.Skip(6).Take(8));
                AssertAllZero(metaA.Skip(14), metaB.Skip(14));
            }
            else if (container.IsVersion360Frontiers)
            {
                AssertAllAreEqual(SAVE_FORMAT_3, metaA[1], metaB[1]);

                AssertAllZero(metaA.Skip(2).Take(12), metaB.Skip(2).Take(12));
                AssertAllNotZero(metaA[14], metaB[14]);
                AssertAllZero(metaA.Skip(15).Take(2), metaB.Skip(15).Take(2));
                Assert.IsTrue(metaA.Skip(20).SequenceEqual(metaB.Skip(20)));
            }
            else
                throw new AssertFailedException();
        }
        else if (metaA.Length == META_LENGTH_TOTAL_WAYPOINT)
        {
            AssertAllAreEqual(SAVE_FORMAT_3, metaA[1], metaB[1]);

            if (container.IsAccount)
            {
                AssertAllNotZero(metaA.Skip(2).Take(4), metaB.Skip(2).Take(4));
                AssertAllNotZero(metaA.Skip(6).Take(8), metaB.Skip(6).Take(8));
                AssertAllZero(metaA.Skip(14), metaB.Skip(14));
            }
            else
            {
                AssertAllZero(metaA.Skip(2).Take(12), metaB.Skip(2).Take(12));
                AssertAllNotZero(metaA[14], metaB[14]);
                AssertAllZero(metaA.Skip(15).Take(2), metaB.Skip(15).Take(2));
                Assert.IsTrue(metaA.Skip(20).SequenceEqual(metaB.Skip(20)));
            }
        }
        else
            throw new AssertFailedException();
    }

    private static void AssertSpecificMeta(WriteResults results, Container containerA, Container containerB, uint[] metaA, uint[] metaB)
    {
        if (results.BaseVersion < 4135) // Frontiers
            return;

        var bytesA = metaA.AsSpan().AsBytes().ToArray();
        var bytesB = metaB.AsSpan().AsBytes().ToArray();
        var priectA = new PrivateObject(containerA);
        var priectB = new PrivateObject(containerB);

        AssertAllAreEqual(results.BaseVersion, (uint)(int)(priectA.GetFieldOrProperty(nameof(WriteResults.BaseVersion))), (uint)(int)(priectB.GetFieldOrProperty(nameof(WriteResults.BaseVersion))), metaA[17], metaB[17]);
        AssertAllAreEqual(results.GameMode, (ushort)(priectA.GetFieldOrProperty(nameof(WriteResults.GameMode))), (ushort)(priectB.GetFieldOrProperty(nameof(WriteResults.GameMode))), BitConverter.ToInt16(bytesA, 72), BitConverter.ToInt16(bytesB, 72));
        AssertAllAreEqual(results.Season, (ushort)(containerA.Season), (ushort)(containerB.Season), BitConverter.ToUInt16(bytesA, 74), BitConverter.ToUInt16(bytesA, 74));
        AssertAllAreEqual(results.TotalPlayTime, containerA.TotalPlayTime, containerB.TotalPlayTime, metaA[19], metaB[19]);

        if (results.BaseVersion < 4140) // Waypoint
            return;

        AssertAllAreEqual(results.SaveName, containerA.SaveName, containerB.SaveName, GetString(bytesA.Skip(88).TakeWhile(i => i != 0)), GetString(bytesB.Skip(88).TakeWhile(i => i != 0)));
        AssertAllAreEqual(results.SaveSummary, containerA.SaveSummary, containerB.SaveSummary, GetString(bytesA.Skip(216).TakeWhile(i => i != 0)), GetString(bytesB.Skip(216).TakeWhile(i => i != 0)));
        AssertAllAreEqual(results.Difficulty, (byte)(containerA.Difficulty), (byte)(containerB.Difficulty), bytesA[344], bytesB[344]);
    }

    #endregion

    [TestMethod]
    public void T01_Read_76561198042453834()
    {
        // Arrange
        var expectAccountData = true;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198042453834");
        var results = new ReadResults[]
        {
            new(0, "Slot1Auto", true, true, false, true, true, true, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4145, 4657, GameVersionEnum.Singularity, "Iteration 1", "Aboard the Space Anomaly", 1253526),
            new(1, "Slot1Manual", true, true, false, true, true, true, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4145, 4657, GameVersionEnum.Singularity, "Iteration 1", "Aboard the Space Anomaly", 1253533),

            new(2, "Slot2Auto", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4142, 4654, GameVersionEnum.WaypointWithSuperchargedSlots, "Playground", "Within Rigonn-Enve Outpost", 902),
            new(3, "Slot2Manual", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4143, 4655, GameVersionEnum.Fractal, "Playground", "Within Rigonn-Enve Outpost", 919),

            new(6, "Slot4Auto", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Permadeath), DifficultyPresetTypeEnum.Permadeath, SeasonEnum.None, 4142, 6702, GameVersionEnum.WaypointWithSuperchargedSlots, "The Final Frontier", "Within Wemexb Colony", 2961),
            new(7, "Slot4Manual", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Permadeath), DifficultyPresetTypeEnum.Permadeath, SeasonEnum.None, 4142, 6702, GameVersionEnum.WaypointWithSuperchargedSlots, "The Final Frontier", "Within Wemexb Colony", 2964),

            new(8, "Slot5Auto", true, true, false, false, false, false, false, false, SaveContextQueryEnum.Main, nameof(PresetGameModeEnum.Permadeath), DifficultyPresetTypeEnum.Permadeath, SeasonEnum.None, 4147, 6707, GameVersionEnum.Omega, "Omega Permadeath", "Auf dem Planeten (Treeph)", 52),

            new(10, "Slot6Auto", true, true, false, true, true, true, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4142, 4654, GameVersionEnum.WaypointWithSuperchargedSlots, "", "On Planet (Ekios)", 1231502),
            new(11, "Slot6Manual", true, true, false, true, true, true, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4142, 4654, GameVersionEnum.WaypointWithSuperchargedSlots, "", "On Planet (Ekios)", 1231357),

            new(12, "Slot7Auto", true, true, false, false, false, false, false, false, SaveContextQueryEnum.Main, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Relaxed, SeasonEnum.None, 4147, 4659, GameVersionEnum.Omega, "Omega Relax", "Auf dem Planeten (Joyo 86/Y2)", 3),

            new(22, "Slot12Auto", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4142, 4654, GameVersionEnum.WaypointWithSuperchargedSlots, "Collector", "Aboard the Space Anomaly", 12049),
            new(23, "Slot12Manual", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4142, 4654, GameVersionEnum.WaypointWithSuperchargedSlots, "Collector", "Aboard the Space Anomaly", 12048),
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonRead<PlatformSteam>(path, results, expectAccountData, userIdentification);
    }

    [TestMethod]
    public void T02_Read_76561198043217184()
    {
        // Arrange
        var expectAccountData = false;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198043217184");
        var results = new ReadResults[]
        {
            new(0, "Slot1Auto", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4125, 4637, GameVersionEnum.ExoMech, "", "", 149345),
            new(1, "Slot1Manual", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4125, 4637, GameVersionEnum.ExoMech, "", "", 147812),

            new(2, "Slot2Auto", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4126, 4638, GameVersionEnum.Origins, "", "", 81063),
            new(3, "Slot2Manual", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4126, 4638, GameVersionEnum.Origins, "", "", 79694),

            new(4, "Slot3Auto", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4126, 4638, GameVersionEnum.Desolation, "", "", 273099),
            new(5, "Slot3Manual", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4126, 4638, GameVersionEnum.Desolation, "", "", 273091),

            new(6, "Slot4Auto", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4125, 4637, GameVersionEnum.ExoMech, "", "", 203285),
            new(7, "Slot4Manual", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4125, 4637, GameVersionEnum.ExoMech, "", "", 203275),

            new(8, "Slot5Auto", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4125, 4637, GameVersionEnum.ExoMech, "", "", 250803),
            new(9, "Slot5Manual", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4125, 4637, GameVersionEnum.ExoMech, "", "", 250955),
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonRead<PlatformSteam>(path, results, expectAccountData, userIdentification);
    }

    [TestMethod]
    public void T03_Read_76561198371877533()
    {
        // Arrange
        var expectAccountData = true;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");
        var results = new ReadResults[]
        {
            new(0, "Slot1Auto", true, true, false, true, true, true, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, 4647, GameVersionEnum.Frontiers, "", "", 94164),
            new(1, "Slot1Manual", true, true, false, true, true, true, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, 4647, GameVersionEnum.Frontiers, "", "", 94946),

            new(2, "Slot2Auto", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Creative), DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4127, 5151, GameVersionEnum.Companions, "", "", 4765),
            new(3, "Slot2Manual", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Creative), DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4127, 5151, GameVersionEnum.Companions, "", "", 4271),

            new(4, "Slot3Auto", true, true, false, true, true, false, true, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Seasonal), DifficultyPresetTypeEnum.Normal, SeasonEnum.Pioneers, 4129, 7201, GameVersionEnum.Expeditions, "", "", 6237),
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonRead<PlatformSteam>(path, results, expectAccountData, userIdentification);
    }

    [TestMethod]
    public void T04_Read_76561198093556678()
    {
        // Arrange
        var expectAccountData = false;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198093556678");
        var results = new ReadResults[]
        {
            new(26, "Slot14Auto", true, true, false, true, false, false, false, false, SaveContextQueryEnum.Main, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4147, 4659, GameVersionEnum.Omega, "", "Within Test Base Terrain Edits", 12462),
            new(27, "Slot14Manual", true, true, false, true, false, false, false, false, SaveContextQueryEnum.Main, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4147, 4659, GameVersionEnum.Omega, "", "Aboard the Space Anomaly", 12521),
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonRead<PlatformSteam>(path, results, expectAccountData, userIdentification);
    }

    [TestMethod]
    public void T05_Read_76561199278291995()
    {
        // Arrange
        var expectAccountData = false;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561199278291995");
        var results = new ReadResults[]
        {
            new(14, "Slot8Auto", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4146, 4658, GameVersionEnum.Echoes, "The Cartographers Redux", "Aboard the Space Anomaly", 17887),
            new(15, "Slot8Manual", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4146, 4658, GameVersionEnum.Echoes, "The Cartographers Redux", "Aboard the Space Anomaly", 17885),

            new(16, "Slot9Auto", true, true, false, true, true, false, true, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Seasonal), DifficultyPresetTypeEnum.Normal, SeasonEnum.VoyagersRedux, 4146, 1514546, GameVersionEnum.Echoes, "Voyagers Redux", "On freighter (DSE-6 Ariasaku)", 40390),
            new(17, "Slot9Manual", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4146, 4658, GameVersionEnum.Echoes, "Voyagers Redux", "On freighter (DSE-6 Ariasaku)", 40436),

            new(20, "Slot11Auto", true, true, false, true, true, false, true, false, SaveContextQueryEnum.Season, nameof(PresetGameModeEnum.Seasonal), DifficultyPresetTypeEnum.Normal, SeasonEnum.OmegaExperimental, 4147, 4659, GameVersionEnum.Omega, "Omega", "Aboard the Space Anomaly", 42101),
            new(21, "Slot11Manual", true, true, false, true, true, false, true, false, SaveContextQueryEnum.Season, nameof(PresetGameModeEnum.Seasonal), DifficultyPresetTypeEnum.Normal, SeasonEnum.OmegaExperimental, 4147, 4659, GameVersionEnum.Omega, "Omega", "Aboard the Space Anomaly", 42125),

            new(22, "Slot12Auto", true, true, false, true, true, false, false, false, SaveContextQueryEnum.Main, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4147, 4659, GameVersionEnum.Omega, "", "Aboard the Space Anomaly", 42148),
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonRead<PlatformSteam>(path, results, expectAccountData, userIdentification);
    }

    /// <summary>
    /// Same as <see cref="T03_Read_76561198371877533"/>.
    /// </summary>
    [TestMethod]
    public void T06_Read_NoAccountInDirectory()
    {
        // Arrange
        var expectAccountData = true;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "something");
        var results = new ReadResults[]
        {
            new(0, "Slot1Auto", true, true, false, true, true, true, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, 4647, GameVersionEnum.Frontiers, "", "", 94164),
            new(1, "Slot1Manual", true, true, false, true, true, true, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, 4647, GameVersionEnum.Frontiers, "", "", 94946),

            new(2, "Slot2Auto", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Creative), DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4127, 5151, GameVersionEnum.Companions, "", "", 4765),
            new(3, "Slot2Manual", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Creative), DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4127, 5151, GameVersionEnum.Companions, "", "", 4271),

            new(4, "Slot3Auto", true, true, false, true, true, false, true, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Seasonal), DifficultyPresetTypeEnum.Normal, SeasonEnum.Pioneers, 4129, 7201, GameVersionEnum.Expeditions, "", "", 6237),
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonRead<PlatformSteam>(path, results, expectAccountData, userIdentification);
    }

    [TestMethod]
    public void T10_Write_Default_0x7D1()
    {
        // Arrange
        var containerIndex = 0;
        var originUnits = -123571; // 4.294.843.725
        var originUtcTicks = 637376113620000000; // 2020-10-06 20:02:42 +00:00
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198043217184");
        var results = new WriteResults(uint.MaxValue, 4125, (ushort)(PresetGameModeEnum.Unspecified), (ushort)(SeasonEnum.None), 0, "", "", (byte)(DifficultyPresetTypeEnum.Invalid));

        // Act
        // Assert
        TestCommonWriteDefaultSave<PlatformSteam>(path, containerIndex, originUnits, originUtcTicks, results, DecryptMeta, AssertCommonMeta, AssertSpecificMeta);
    }

    [TestMethod]
    public void T11_Write_Default_0x7D2_Frontiers_Account()
    {
        // Arrange
        var originMusicVolume = 80; // 80
        var originUtcTicks = 637663896760000000; // 2021-09-04 22:01:16 +00:00
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");

        // Act
        // Assert
        TestCommonWriteDefaultAccount<PlatformSteam>(path, originMusicVolume, originUtcTicks, DecryptMeta, AssertCommonMeta);
    }

    [TestMethod]
    public void T12_Write_Default_0x7D2_Frontiers()
    {
        // Arrange
        var containerIndex = 0;
        var originUnits = -1221111157; // 3.073.856.139
        var originUtcTicks = 637663905840000000; // 2021-09-04 22:16:24 +00:00
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");
        var results = new WriteResults(uint.MaxValue, 4135, (ushort)(PresetGameModeEnum.Normal), (ushort)(SeasonEnum.None), 94164, "", "", (byte)(DifficultyPresetTypeEnum.Normal));

        // Act
        // Assert
        TestCommonWriteDefaultSave<PlatformSteam>(path, containerIndex, originUnits, originUtcTicks, results, DecryptMeta, AssertCommonMeta, AssertSpecificMeta);
    }

    [TestMethod]
    public void T13_Write_Default_0x7D2_Waypoint_Account()
    {
        // Arrange
        var originMusicVolume = 80; // 80
        var originUtcTicks = 638263807910000000; // 2023-07-31 06:13:11 +00:00
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198042453834");

        // Act
        // Assert
        TestCommonWriteDefaultAccount<PlatformSteam>(path, originMusicVolume, originUtcTicks, DecryptMeta, AssertCommonMeta);
    }

    [TestMethod]
    public void T14_Write_Default_0x7D2_Waypoint()
    {
        // Arrange
        var containerIndex = 0;
        var originUnits = 1199342306; // 1.199.342.306
        var originUtcTicks = 638234536920000000; // 2023-06-27 09:08:12 +00:00
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198042453834");
        var results = new WriteResults(uint.MaxValue, 4145, (ushort)(PresetGameModeEnum.Normal), (ushort)(SeasonEnum.None), 1253526, "Iteration 1", "Aboard the Space Anomaly", (byte)(DifficultyPresetTypeEnum.Custom));

        // Act
        // Assert
        TestCommonWriteDefaultSave<PlatformSteam>(path, containerIndex, originUnits, originUtcTicks, results, DecryptMeta, AssertCommonMeta, AssertSpecificMeta);
    }

    [TestMethod]
    public void T15_Write_SetLastWriteTime_False()
    {
        // Arrange
        var containerIndex = 0;
        var originUnits = -1221111157; // 3.073.856.139
        var originUtcTicks = 637663905840000000; // 2021-09-04 22:16:24 +00:00
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");

        // Act
        // Assert
        TestCommonWriteSetLastWriteTimeFalse<PlatformSteam>(path, containerIndex, originUnits, originUtcTicks);
    }

    [TestMethod]
    public void T16_Write_WriteAlways_False()
    {
        // Arrange
        var containerIndex = 0;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198043217184");

        // Act
        // Assert
        TestCommonWriteWriteAlwaysFalse<PlatformSteam>(path, containerIndex);
    }

    [TestMethod]
    public void T17_Write_WriteAlways_True()
    {
        // Arrange
        var containerIndex = 0;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198043217184");

        // Act
        // Assert
        TestCommonWriteWriteAlwaysTrue<PlatformSteam>(path, containerIndex);
    }

    [TestMethod]
    public void T20_FileSystemWatcher()
    {
        // Arrange
        var containerIndex = 0;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");
        var pathWatching = Path.Combine(path, "save.hg");

        // Act
        // Assert
        TestCommonFileSystemWatcher<PlatformSteam>(path, pathWatching, containerIndex);
    }

    [TestMethod]
    public void T30_Copy()
    {
        // Arrange
        var copyOverwrite = new[] { 0, 2 }; // 1Auto -> 2Auto (overwrite)
        var copyCreate = new[] { 3, 7 }; // 2Manual -> 4Manual (create)
        var copyDelete = new[] { 9, 4 }; // 5Manual -> 3Auto (delete)
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");

        // Act
        // Assert
        TestCommonFileOperationCopy<PlatformSteam>(path, copyOverwrite, copyCreate, copyDelete);
    }

    [TestMethod]
    public void T31_Delete()
    {
        // Arrange
        var deleteDelete = new[] { 0, 1 }; // 1Auto, 1Manual
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");

        // Act
        // Assert
        TestCommonFileOperationDelete<PlatformSteam>(path, deleteDelete);
    }

    [TestMethod]
    public void T32_Move()
    {
        // Arrange
        var moveCopy = Array.Empty<int>();
        var moveOverwrite = new[] { 1, 0 }; // 1Manual -> 1Auto (overwrite)
        var moveDelete = new[] { 8, 2 }; // 5Auto -> 2Auto (delete)
        var moveCreate = new[] { 4, 9 }; // 3Auto -> 5Manual (create)
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");

        // Act
        // Assert
        TestCommonFileOperationMove<PlatformSteam>(path, moveCopy, moveOverwrite, moveDelete, moveCreate);
    }

    [TestMethod]
    public void T40_TransferFromGog()
    {
        // Arrange
        var pathGog = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Gog", "DefaultUser");
        var resultsGog = new ReadResults[]
        {
            new(2, "Slot2Auto", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, 4647, GameVersionEnum.Emergence, "", "", 19977),
            new(3, "Slot2Manual", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, 4647, GameVersionEnum.Emergence, "", "", 5048),
        };
        var slotGog = 1; // get Slot2
        var userDecisionsGog = 1;
        var userIdentificationGog = ReadUserIdentification(pathGog)!;

        var existingContainersCount = 8; // 5 + 1 (Slot3) + 2 (Slot4)
        var offset = 2;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");
        var transfer = new[] { 2, 3 }; // overwrite Slot3 // create Slot4
        var userIdentification = ReadUserIdentification(path)!;

        // Act
        // Assert
        TestCommonFileOperationTransfer<PlatformSteam, PlatformGog>(pathGog, path, userIdentificationGog, userIdentification, slotGog, userDecisionsGog, transfer, existingContainersCount, resultsGog, offset);
    }

    [TestMethod]
    public void T41_TransferFromMicrosoft()
    {
        // Arrange
        var pathMicrosoft = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "0009000000C73498_29070100B936489ABCE8B9AF3980429C");
        var resultsMicrosoft = new ReadResults[]
        {
            new(2, "Slot2Auto", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, 4647, GameVersionEnum.Frontiers, "", "", 423841),
            new(3, "Slot2Manual", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, 4647, GameVersionEnum.Frontiers, "", "", 419023),
        };
        var slotMicrosoft = 1; // get Slot2
        var userDecisionsMicrosoft = 8;
        var userIdentificationMicrosoft = ReadUserIdentification(pathMicrosoft);

        var existingContainersCount = 8; // 5 + 1 (Slot3) + 2 (Slot4)
        var offset = 2;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");
        var transfer = new[] { 2, 3 }; // overwrite Slot3 // create Slot4
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonFileOperationTransfer<PlatformSteam, PlatformMicrosoft>(pathMicrosoft, path, userIdentificationMicrosoft, userIdentification, slotMicrosoft, userDecisionsMicrosoft, transfer, existingContainersCount, resultsMicrosoft, offset);
    }

    //[TestMethod]
    //public void T42_TransferFromPlaystation_0x7D1()
    //{
    //    // Arrange
    //    var pathPlaystation = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D1", "SaveWizard", "1");
    //    var resultsPlaystation = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum Season, int BaseVersion, GameVersionEnum Version)[]
    //    {
    //        (2, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4134, GameVersionEnum.PrismsWithBytebeatAuthor), // 2Auto
    //        (3, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4134, GameVersionEnum.PrismsWithBytebeatAuthor), // 2Manual
    //    };
    //    var slotPlaystation = 1; // get Slot2
    //    var userDecisionsPlaystation = 24;
    //    var userIdentificationPlaystation = ReadUserIdentification(pathPlaystation);

    //    var existingContainersCount = 8; // 5 + 1 (Slot3) + 2 (Slot4)
    //    var offset = 2;
    //    var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");
    //    var transfer = new[] { 2, 3 }; // overwrite Slot3 // create Slot4
    //    var userIdentification = ReadUserIdentification(path);

    //    // Act
    //    // Assert
    //    TestCommonFileOperationTransfer<PlatformSteam, PlatformPlaystation>(pathPlaystation, path, userIdentificationPlaystation, userIdentification, slotPlaystation, userDecisionsPlaystation, transfer, existingContainersCount, resultsPlaystation, offset);
    //}

    //[TestMethod]
    //public void T43_TransferFromPlaystation_0x7D2()
    //{
    //    // Arrange
    //    var pathPlaystation = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D2", "SaveWizard", "4");
    //    var resultsPlaystation = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum Season, int BaseVersion, GameVersionEnum Version)[]
    //    {
    //        (2, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, GameVersionEnum.Frontiers), // 2Auto
    //        (3, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, GameVersionEnum.Frontiers), // 2Manual
    //    };
    //    var slotPlaystation = 1; // get Slot2
    //    var userDecisionsPlaystation = 4;
    //    var userIdentificationPlaystation = ReadUserIdentification(pathPlaystation);

    //    var existingContainersCount = 8; // 5 + 1 (Slot3) + 2 (Slot4)
    //    var offset = 2;
    //    var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");
    //    var transfer = new[] { 2, 3 }; // overwrite Slot3 // create Slot4
    //    var userIdentification = ReadUserIdentification(path);

    //    // Act
    //    // Assert
    //    TestCommonFileOperationTransfer<PlatformSteam, PlatformPlaystation>(pathPlaystation, path, userIdentificationPlaystation, userIdentification, slotPlaystation, userDecisionsPlaystation, transfer, existingContainersCount, resultsPlaystation, offset);
    //}

    [TestMethod]
    public void T44_TransferFromSteam()
    {
        // Arrange
        var pathSteam = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198042453834");
        var resultsSteam = new ReadResults[]
        {
            new(6, "Slot4Auto", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Permadeath), DifficultyPresetTypeEnum.Permadeath, SeasonEnum.None, 4142, 6702, GameVersionEnum.WaypointWithSuperchargedSlots, "The Final Frontier", "Within Wemexb Colony", 2961),
            new(7, "Slot4Manual", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Permadeath), DifficultyPresetTypeEnum.Permadeath, SeasonEnum.None, 4142, 6702, GameVersionEnum.WaypointWithSuperchargedSlots, "The Final Frontier", "Within Wemexb Colony", 2964),
        };
        var slotSteam = 3; // get Slot4
        var userDecisionsSteam = 1;
        var userIdentificationSteam = ReadUserIdentification(pathSteam);

        var existingContainersCount = 8; // 5 + 1 (Slot3) + 2 (Slot4)
        var offset = -2;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");
        var transfer = new[] { 2, 3 }; // overwrite Slot3 // create Slot4
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonFileOperationTransfer<PlatformSteam, PlatformSteam>(pathSteam, path, userIdentificationSteam, userIdentification, slotSteam, userDecisionsSteam, transfer, existingContainersCount, resultsSteam, offset);
    }

    [TestMethod]
    public void T45_TransferFromSwitch()
    {
        // Arrange
        var pathSwitch = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "4");
        var resultsSwitch = new ReadResults[]
        {
            new(2, "Slot2Auto", true, true, false, false, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Survival), DifficultyPresetTypeEnum.Survival, SeasonEnum.None, 4139, 5675, GameVersionEnum.Endurance, "", "", 336),
        };
        var slotSwitch = 1; // get Slot2
        var userDecisionsSwitch = 0;
        var userIdentificationSwitch = ReadUserIdentification(pathSwitch);

        var existingContainersCount = 6; // 5 + 1 (Slot?)
        var offset = 2;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");
        var transfer = new[] { 2, 3 }; // overwrite Slot3 // create Slot4
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonFileOperationTransfer<PlatformSteam, PlatformSwitch>(pathSwitch, path, userIdentificationSwitch, userIdentification, slotSwitch, userDecisionsSwitch, transfer, existingContainersCount, resultsSwitch, offset);
    }
}
