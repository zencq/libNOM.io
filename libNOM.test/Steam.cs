﻿using System.Text;

using CommunityToolkit.HighPerformance;

using libNOM.io;
using libNOM.io.Interfaces;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Newtonsoft.Json;

namespace libNOM.test;


// Do not use System.Range for simplicity of the file and performance is not critical.
[TestClass]
[DeploymentItem("../../../Resources/TESTSUITE_ARCHIVE_PLATFORM_GOG.zip")]
[DeploymentItem("../../../Resources/TESTSUITE_ARCHIVE_PLATFORM_MICROSOFT.zip")]
[DeploymentItem("../../../Resources/TESTSUITE_ARCHIVE_PLATFORM_PLAYSTATION.zip")]
[DeploymentItem("../../../Resources/TESTSUITE_ARCHIVE_PLATFORM_STEAM.zip")]
[DeploymentItem("../../../Resources/TESTSUITE_ARCHIVE_PLATFORM_SWITCH.zip")]
public class SteamTest : CommonTestClass
{
    #region Constant

    private static readonly uint[] META_ENCRYPTION_KEY = Encoding.ASCII.GetBytes("NAESEVADNAYRTNRG").AsSpan().Cast<byte, uint>().ToArray();
    private const uint META_HEADER = 0xEEEEEEBE; // 4,008,636,094
    private const int META_LENGTH_TOTAL_VANILLA = 0x68 / sizeof(uint); // 26
    private const int META_LENGTH_TOTAL_WAYPOINT = 0x168 / sizeof(uint); // 90
    private const int META_LENGTH_TOTAL_WORLDS_PART_I = 0x180 / sizeof(uint); // 96
    private const int META_LENGTH_TOTAL_WORLDS_PART_II = 0x1B0 / sizeof(uint); // 108

    #endregion

    #region Meta

    private static uint[] DecryptMeta(IContainer container)
    {
        var meta = File.ReadAllBytes(container.MetaFile!.FullName);
        var value = ToUInt32(meta);

        if (value.Length != META_LENGTH_TOTAL_VANILLA && value.Length != META_LENGTH_TOTAL_WAYPOINT && value.Length != META_LENGTH_TOTAL_WORLDS_PART_I && value.Length != META_LENGTH_TOTAL_WORLDS_PART_II)
            return value;

        // Best case is that it works with the value of the file but in case it was moved manually, try all other values as well.
        var enumValues = new int[] { container.IsAccount ? 1 : container.MetaIndex }.Concat(Enumerable.Range(0, 32).Where(i => i > 1 && i != container.MetaIndex));

        foreach (var entry in enumValues)
        {
            // When overwriting META_ENCRYPTION_KEY[0] it can happen that the value is not set afterwards and therefore create a new collection to ensure it will be correct.
            ReadOnlySpan<uint> key = [(RotateLeft((uint)(entry) ^ 0x1422CB8C, 13) * 5) + 0xE6546B64, META_ENCRYPTION_KEY[1], META_ENCRYPTION_KEY[2], META_ENCRYPTION_KEY[3]];

            // DeepCopy as value would be changed otherwise and casting again does not work.
            var serialized = JsonConvert.SerializeObject(value);
            Span<uint> result = JsonConvert.DeserializeObject<uint[]>(serialized)!;

            uint hash = 0;
            int iterations = value.Length == META_LENGTH_TOTAL_VANILLA ? 8 : 6;
            int lastIndex = result.Length - 1;

            // Results in 0xF1BBCDC8 for META_FORMAT_1 as in the original algorithm.
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

    private static uint RotateLeft(uint value, int bits)
    {
        return (value << bits) | (value >> (32 - bits));
    }

    private static void AssertCommonMeta(IContainer container, uint[] metaA, uint[] metaB)
    {
        Assert.AreEqual(metaA.Length, metaB.Length);

        AssertAllAreEqual(META_HEADER, metaA[0], metaB[0]);

        if (metaA.Length == META_LENGTH_TOTAL_VANILLA)
        {
            if (container.IsAccount || container.IsSave && !container.IsVersion360Frontiers)
            {
                // Editing account data is possible since Frontiers and therefore has always the new format but otherwise uses the old format.
                AssertAllAreEqual(container.IsAccount ? META_FORMAT_2 : META_FORMAT_1, metaA[1], metaB[1]);

                AssertAllNotZero(metaA.Skip(2).Take(4), metaB.Skip(2).Take(4));
                AssertAllNotZero(metaA.Skip(6).Take(8), metaB.Skip(6).Take(8));
                AssertAllZero(metaA.Skip(15), metaB.Skip(15));
            }
            else if (container.IsVersion360Frontiers)
            {
                AssertAllAreEqual(META_FORMAT_2, metaA[1], metaB[1]);

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
            AssertAllAreEqual(META_FORMAT_2, metaA[1], metaB[1]);

            if (container.IsAccount)
            {
                AssertAllNotZero(metaA.Skip(2).Take(4), metaB.Skip(2).Take(4));
                AssertAllNotZero(metaA.Skip(6).Take(8), metaB.Skip(6).Take(8));
                AssertAllZero(metaA.Skip(15), metaB.Skip(15));
            }
            else
            {
                AssertAllZero(metaA.Skip(2).Take(12), metaB.Skip(2).Take(12));
                AssertAllNotZero(metaA[14], metaB[14]);
                AssertAllZero(metaA.Skip(15).Take(2), metaB.Skip(15).Take(2));
                Assert.IsTrue(metaA.Skip(21).SequenceEqual(metaB.Skip(21)));
            }
        }
        else if (metaA.Length == META_LENGTH_TOTAL_WORLDS_PART_I || metaA.Length == META_LENGTH_TOTAL_WORLDS_PART_II)
        {
            var metaFormat = container.IsVersion550WorldsPartII ? META_FORMAT_4 : META_FORMAT_3;

            AssertAllAreEqual(metaFormat, metaA[1], metaB[1]);

            if (container.IsAccount)
            {
                AssertAllNotZero(metaA.Skip(2).Take(4), metaB.Skip(2).Take(4));
                AssertAllNotZero(metaA.Skip(6).Take(8), metaB.Skip(6).Take(8));
                AssertAllZero(metaA.Skip(15), metaB.Skip(15));
            }
            else
            {
                AssertAllZero(metaA.Skip(2).Take(12), metaB.Skip(2).Take(12));
                AssertAllNotZero(metaA.Skip(14).Take(2), metaB.Skip(14).Take(2));
                AssertAllZero(metaA[16], metaB[16]);
                Assert.IsTrue(metaA.Skip(21).Take(68).SequenceEqual(metaB.Skip(21).Take(68)));
                AssertAllNotZero(metaA[89], metaB[89]);
                AssertAllAreEqual(metaFormat, metaA[90], metaB[90]);
                AssertAllAreEqual(metaA.Skip(91), metaB.Skip(91));
            }
        }
        else
            throw new AssertFailedException();
    }

    private static void AssertSpecificMeta(WriteResults results, IContainer containerA, IContainer containerB, uint[] metaA, uint[] metaB)
    {
        if (results.BaseVersion < 4135) // Frontiers
            return;

        var bytesA = metaA.AsSpan().AsBytes().ToArray();
        var bytesB = metaB.AsSpan().AsBytes().ToArray();
        var prijectA = new PrivateObject(containerA);
        var prijectB = new PrivateObject(containerB);

        AssertAllAreEqual(results.BaseVersion, (uint)(int)(prijectA.GetFieldOrProperty(nameof(WriteResults.BaseVersion))), (uint)(int)(prijectB.GetFieldOrProperty(nameof(WriteResults.BaseVersion))), metaA[17], metaB[17]);
        AssertAllAreEqual(results.GameMode, (ushort)(prijectA.GetFieldOrProperty(nameof(WriteResults.GameMode))), (ushort)(prijectB.GetFieldOrProperty(nameof(WriteResults.GameMode))), BitConverter.ToInt16(bytesA, 72), BitConverter.ToInt16(bytesB, 72));
        AssertAllAreEqual(results.Season, (ushort)(containerA.Season), (ushort)(containerB.Season), BitConverter.ToUInt16(bytesA, 74), BitConverter.ToUInt16(bytesA, 74));
        AssertAllAreEqual(results.TotalPlayTime, containerA.TotalPlayTime, containerB.TotalPlayTime, BitConverter.ToUInt64(bytesA, 76), BitConverter.ToUInt64(bytesB, 76));

        if (results.BaseVersion < 4140) // Waypoint
            return;

        AssertAllAreEqual(results.SaveName, containerA.SaveName, containerB.SaveName, GetString(bytesA.Skip(88).TakeWhile(i => i != 0)), GetString(bytesB.Skip(88).TakeWhile(i => i != 0)));
        AssertAllAreEqual(results.SaveSummary, containerA.SaveSummary, containerB.SaveSummary, GetString(bytesA.Skip(216).TakeWhile(i => i != 0)), GetString(bytesB.Skip(216).TakeWhile(i => i != 0)));
        AssertAllAreEqual(results.Difficulty, (byte)(containerA.Difficulty), (byte)(containerB.Difficulty), bytesA[344], bytesB[344]);
    }

    #endregion

    [TestMethod]
    public void T101_Read_76561198042453834()
    {
        // Arrange
        var expectAccountData = true;
        var path = GetCombinedPath("Steam", "st_76561198042453834");
        var results = new ReadResults[]
        {
            new(0, "Slot1Auto", true, true, false, true, true, true, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4145, 4657, GameVersionEnum.Singularity, "Iteration 1", "Aboard the Space Anomaly", 1253526),
            new(1, "Slot1Manual", true, true, false, true, true, true, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4145, 4657, GameVersionEnum.Singularity, "Iteration 1", "Aboard the Space Anomaly", 1253533),

            new(2, "Slot2Auto", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4142, 4654, GameVersionEnum.WaypointWithSuperchargedSlots, "Playground", "Within Rigonn-Enve Outpost", 902),
            new(3, "Slot2Manual", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4143, 4655, GameVersionEnum.Fractal, "Playground", "Within Rigonn-Enve Outpost", 919),

            new(6, "Slot4Auto", true, true, false, true, false, false, false, false, SaveContextQueryEnum.Main, nameof(PresetGameModeEnum.Permadeath), DifficultyPresetTypeEnum.Permadeath, SeasonEnum.None, 4153, 6713, GameVersionEnum.WorldsPartI, "The Final Frontier", "Im Hebino XVIII-System", 5495),
            new(7, "Slot4Manual", true, true, false, true, false, false, false, false, SaveContextQueryEnum.Main, nameof(PresetGameModeEnum.Permadeath), DifficultyPresetTypeEnum.Permadeath, SeasonEnum.None, 4153, 6713, GameVersionEnum.WorldsPartI, "The Final Frontier", "An Bord von „Hebino XVIII“-Treffpunkt", 5521),

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
    public void T102_Read_76561198043217184()
    {
        // Arrange
        var expectAccountData = false;
        var path = GetCombinedPath("Steam", "st_76561198043217184");
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
    public void T103_Read_76561198371877533()
    {
        // Arrange
        var expectAccountData = true;
        var path = GetCombinedPath("Steam", "st_76561198371877533");
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
    public void T104_Read_76561198093556678()
    {
        // Arrange
        var expectAccountData = false;
        var path = GetCombinedPath("Steam", "st_76561198093556678");
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
    public void T105_Read_76561199278291995()
    {
        // Arrange
        var expectAccountData = false;
        var path = GetCombinedPath("Steam", "st_76561199278291995");
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

    [TestMethod]
    public void T106_Read_76561198042453834_0x7D3_WorldsPartI()
    {
        // Arrange
        var expectAccountData = true;
        var path = GetCombinedPath("Steam", "st_76561198042453834_0x7D3");
        var results = new ReadResults[]
        {
            new(0, "Slot1Auto", true, true, false, true, true, true, false, false, SaveContextQueryEnum.Main, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4153, 4665, GameVersionEnum.WorldsPartI, "Iteration 1", "An Bord von Die „Batannam“-Sphäre", 1287227),
            new(1, "Slot1Manual", true, true, false, true, true, true, false, false, SaveContextQueryEnum.Main, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4153, 4665, GameVersionEnum.WorldsPartI, "Iteration 1", "An Bord von Die „Batannam“-Sphäre", 1287234),
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonRead<PlatformSteam>(path, results, expectAccountData, userIdentification);
    }

    [TestMethod]
    public void T107_Read_76561198370111076_0x7D4_WorldsPartII()
    {
        // Arrange
        var expectAccountData = true;
        var path = GetCombinedPath("Steam", "st_76561198370111076_0x7D4");
        var results = new ReadResults[]
        {
            new(0, "Slot1Auto", true, true, false, true, true, true, false, false, SaveContextQueryEnum.Main, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4172, 4684, GameVersionEnum.WorldsPartIIWithDifficultyTag, "Main", "Main", 20437),

            new(2, "Slot2Auto", true, true, false, true, true, true, false, false, SaveContextQueryEnum.Main, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4172, 4684, GameVersionEnum.WorldsPartIIWithDifficultyTag, "Møddęd Şävę", "Within New Atlantis", 4294987702),
            new(3, "Slot2Manual", true, true, false, true, true, true, false, false, SaveContextQueryEnum.Main, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4172, 4684, GameVersionEnum.WorldsPartIIWithDifficultyTag, "Møddęd Şävę", "Main", 4294987733),
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonRead<PlatformSteam>(path, results, expectAccountData, userIdentification);
    }

    /// <summary>
    /// Same as <see cref="T103_Read_76561198371877533"/>.
    /// </summary>
    [TestMethod]
    public void T110_Read_NoAccountInDirectory()
    {
        // Arrange
        var expectAccountData = true;
        var path = GetCombinedPath("Steam", "something");
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
    public void T200_Write_Default_0x7D1()
    {
        // Arrange
        var containerIndex = 0;
        var originUnits = -123571; // 4.294.843.725
        var originUtcTicks = 637376113620000000; // 2020-10-06 20:02:42 +00:00
        var path = GetCombinedPath("Steam", "st_76561198043217184");
        var results = new WriteResults(uint.MaxValue, 4125, (ushort)(PresetGameModeEnum.Unspecified), (ushort)(SeasonEnum.None), 0, "", "", (byte)(DifficultyPresetTypeEnum.Invalid));

        // Act
        // Assert
        TestCommonWriteDefaultSave<PlatformSteam>(path, containerIndex, originUnits, originUtcTicks, results, DecryptMeta, AssertCommonMeta, AssertSpecificMeta);
    }

    [TestMethod]
    public void T210_Write_Default_0x7D2_Frontiers_Account()
    {
        // Arrange
        var originMusicVolume = 80; // 80
        var originUtcTicks = 637663896760000000; // 2021-09-04 22:01:16 +00:00
        var path = GetCombinedPath("Steam", "st_76561198371877533");

        // Act
        // Assert
        TestCommonWriteDefaultAccount<PlatformSteam>(path, originMusicVolume, originUtcTicks, DecryptMeta, AssertCommonMeta);
    }

    [TestMethod]
    public void T211_Write_Default_0x7D2_Frontiers()
    {
        // Arrange
        var containerIndex = 0;
        var originUnits = -1221111157; // 3.073.856.139
        var originUtcTicks = 637663905840000000; // 2021-09-04 22:16:24 +00:00
        var path = GetCombinedPath("Steam", "st_76561198371877533");
        var results = new WriteResults(uint.MaxValue, 4135, (ushort)(PresetGameModeEnum.Normal), (ushort)(SeasonEnum.None), 94164, "", "", (byte)(DifficultyPresetTypeEnum.Normal));

        // Act
        // Assert
        TestCommonWriteDefaultSave<PlatformSteam>(path, containerIndex, originUnits, originUtcTicks, results, DecryptMeta, AssertCommonMeta, AssertSpecificMeta);
    }

    [TestMethod]
    public void T220_Write_Default_0x7D2_Waypoint_Account()
    {
        // Arrange
        var originMusicVolume = 80; // 80
        var originUtcTicks = 638263807920000000; // 2023-07-31 06:13:12 +00:00
        var path = GetCombinedPath("Steam", "st_76561198042453834");

        // Act
        // Assert
        TestCommonWriteDefaultAccount<PlatformSteam>(path, originMusicVolume, originUtcTicks, DecryptMeta, AssertCommonMeta);
    }

    [TestMethod]
    public void T221_Write_Default_0x7D2_Waypoint()
    {
        // Arrange
        var containerIndex = 0;
        var originUnits = 1199342306; // 1,199,342,306
        var originUtcTicks = 638234536920000000; // 2023-06-27 09:08:12 +00:00
        var path = GetCombinedPath("Steam", "st_76561198042453834");
        var results = new WriteResults(uint.MaxValue, 4145, (ushort)(PresetGameModeEnum.Normal), (ushort)(SeasonEnum.None), 1253526, "Iteration 1", "Aboard the Space Anomaly", (byte)(DifficultyPresetTypeEnum.Custom));

        // Act
        // Assert
        TestCommonWriteDefaultSave<PlatformSteam>(path, containerIndex, originUnits, originUtcTicks, results, DecryptMeta, AssertCommonMeta, AssertSpecificMeta);
    }

    /// <summary>
    /// No changes compared to <see cref="T220_Write_Default_0x7D2_Waypoint_Account"/>.
    /// </summary>
    [TestMethod]
    public void T222_Write_Default_0x7D3_WorldsPartI_Account()
    {
        // Arrange
        var originMusicVolume = 80; // 80
        var originUtcTicks = 638569393020000000; // 2024-07-18 22:41:42 +00:00
        var path = GetCombinedPath("Steam", "st_76561198042453834_0x7D3");

        // Act
        // Assert
        TestCommonWriteDefaultAccount<PlatformSteam>(path, originMusicVolume, originUtcTicks, DecryptMeta, AssertCommonMeta);
    }

    [TestMethod]
    public void T223_Write_Default_0x7D3_WorldsPartI()
    {
        // Arrange
        var containerIndex = 0;
        var originUnits = 1230523743; // 1,230,523,743
        var originUtcTicks = 638569393610000000; // 2024-07-18 22:42:41 +00:00
        var path = GetCombinedPath("Steam", "st_76561198042453834_0x7D3");
        var results = new WriteResults(uint.MaxValue, 4153, (ushort)(PresetGameModeEnum.Normal), (ushort)(SeasonEnum.None), 1287227, "Iteration 1", "An Bord von Die „Batannam“-Sphäre", (byte)(DifficultyPresetTypeEnum.Custom));

        // Act
        // Assert
        TestCommonWriteDefaultSave<PlatformSteam>(path, containerIndex, originUnits, originUtcTicks, results, DecryptMeta, AssertCommonMeta, AssertSpecificMeta);
    }

    /// <summary>
    /// No changes compared to <see cref="T220_Write_Default_0x7D2_Waypoint_Account"/>.
    /// </summary>
    [TestMethod]
    public void T224_Write_Default_0x7D4_WorldsPartII_Account()
    {
        // Arrange
        var originMusicVolume = 80; // 80
        var originUtcTicks = 638748002980000000; // 2025-02-10 16:04:58 +00:00
        var path = GetCombinedPath("Steam", "st_76561198370111076_0x7D4");

        // Act
        // Assert
        TestCommonWriteDefaultAccount<PlatformSteam>(path, originMusicVolume, originUtcTicks, DecryptMeta, AssertCommonMeta);
    }

    [TestMethod]
    public void T225_Write_Default_0x7D4_WorldsPartII()
    {
        // Arrange
        var containerIndex = 2;
        var originUnits = -1;
        var originUtcTicks = 638746854450000000; // 2025-02-09 08:10:45 +00:00
        var path = GetCombinedPath("Steam", "st_76561198370111076_0x7D4");
        var results = new WriteResults(uint.MaxValue, 4172, (ushort)(PresetGameModeEnum.Normal), (ushort)(SeasonEnum.None), 4294987702, "Møddęd Şävę", "Within New Atlantis", (byte)(DifficultyPresetTypeEnum.Normal));

        // Act
        // Assert
        TestCommonWriteDefaultSave<PlatformSteam>(path, containerIndex, originUnits, originUtcTicks, results, DecryptMeta, AssertCommonMeta, AssertSpecificMeta);
    }

    [TestMethod]
    public void T230_Write_SetLastWriteTime_False()
    {
        // Arrange
        var containerIndex = 0;
        var originUnits = -1221111157; // 3.073.856.139
        var originUtcTicks = 637663905840000000; // 2021-09-04 22:16:24 +00:00
        var path = GetCombinedPath("Steam", "st_76561198371877533");

        // Act
        // Assert
        TestCommonWriteSetLastWriteTimeFalse<PlatformSteam>(path, containerIndex, originUnits, originUtcTicks);
    }

    [TestMethod]
    public void T240_Write_WriteAlways_False()
    {
        // Arrange
        var containerIndex = 0;
        var path = GetCombinedPath("Steam", "st_76561198043217184");

        // Act
        // Assert
        TestCommonWriteWriteAlwaysFalse<PlatformSteam>(path, containerIndex);
    }

    [TestMethod]
    public void T241_Write_WriteAlways_True()
    {
        // Arrange
        var containerIndex = 0;
        var path = GetCombinedPath("Steam", "st_76561198043217184");

        // Act
        // Assert
        TestCommonWriteWriteAlwaysTrue<PlatformSteam>(path, containerIndex);
    }

    [TestMethod]
    public void T300_FileSystemWatcher()
    {
        // Arrange
        var containerIndex = 0;
        var path = GetCombinedPath("Steam", "st_76561198371877533");
        var pathWatching = Path.Combine(path, "save.hg");

        // Act
        // Assert
        TestCommonFileSystemWatcher<PlatformSteam>(path, pathWatching, containerIndex);
    }

    [TestMethod]
    public void T301_Copy()
    {
        // Arrange
        var copyOverwrite = new[] { 0, 2 }; // 1Auto -> 2Auto (overwrite)
        var copyCreate = new[] { 3, 7 }; // 2Manual -> 4Manual (create)
        var copyDelete = new[] { 9, 4 }; // 5Manual -> 3Auto (delete)
        var path = GetCombinedPath("Steam", "st_76561198371877533");

        // Act
        // Assert
        TestCommonFileOperationCopy<PlatformSteam>(path, copyOverwrite, copyCreate, copyDelete);
    }

    [TestMethod]
    public void T302_Delete()
    {
        // Arrange
        var deleteDelete = new[] { 0, 1 }; // 1Auto, 1Manual
        var path = GetCombinedPath("Steam", "st_76561198371877533");

        // Act
        // Assert
        TestCommonFileOperationDelete<PlatformSteam>(path, deleteDelete);
    }

    [TestMethod]
    public void T303_Move()
    {
        // Arrange
        var moveCopy = Array.Empty<int>();
        var moveOverwrite = new[] { 1, 0 }; // 1Manual -> 1Auto (overwrite)
        var moveDelete = new[] { 8, 2 }; // 5Auto -> 2Auto (delete)
        var moveCreate = new[] { 4, 9 }; // 3Auto -> 5Manual (create)
        var path = GetCombinedPath("Steam", "st_76561198371877533");

        // Act
        // Assert
        TestCommonFileOperationMove<PlatformSteam>(path, moveCopy, moveOverwrite, moveDelete, moveCreate);
    }

    [TestMethod]
    public void T304_Swap()
    {
        // Arrange
        var path = GetCombinedPath("Steam", "st_76561198042453834");
        var results = new ReadResults[]
        {
            // before swap 3, "Slot2Manual"
            new(8, "Slot5Auto", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4143, 4655, GameVersionEnum.Fractal, "Playground", "Within Rigonn-Enve Outpost", 919),

            // before swap 8, "Slot5Auto"
            new(3, "Slot2Manual", true, true, false, false, false, false, false, false, SaveContextQueryEnum.Main, nameof(PresetGameModeEnum.Permadeath), DifficultyPresetTypeEnum.Permadeath, SeasonEnum.None, 4147, 6707, GameVersionEnum.Omega, "Omega Permadeath", "Auf dem Planeten (Treeph)", 52),
        };
        var swapSwap = new[] { 3, 8 }; // 2Manual <-> 5Auto

        // Act
        // Assert
        TestCommonFileOperationSwap<PlatformSteam>(path, results, swapSwap);
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
        var userIdentificationGog = ReadUserIdentification(pathGog)!;

        var existingContainersCount = 8; // 5 + 1 (Slot3) + 2 (Slot4)
        var path = GetCombinedPath("Steam", "st_76561198371877533");
        var transfer = new[] { 2, 3 }; // overwrite Slot3 // create Slot4
        var userIdentification = ReadUserIdentification(path)!;

        // Act
        // Assert
        TestCommonFileOperationTransfer<PlatformSteam, PlatformGog>(pathGog, path, userIdentificationGog, userIdentification, slotGog, userDecisionsGog, transfer, existingContainersCount, resultsGog);
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

        var existingContainersCount = 8; // 5 + 1 (Slot3) + 2 (Slot4)
        var path = GetCombinedPath("Steam", "st_76561198371877533");
        var transfer = new[] { 2, 3 }; // overwrite Slot3 // create Slot4
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonFileOperationTransfer<PlatformSteam, PlatformMicrosoft>(pathMicrosoft, path, userIdentificationMicrosoft, userIdentification, slotMicrosoft, userDecisionsMicrosoft, transfer, existingContainersCount, resultsMicrosoft);
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

        var existingContainersCount = 8; // 5 + 1 (Slot3) + 2 (Slot4)
        var path = GetCombinedPath("Steam", "st_76561198371877533");
        var transfer = new[] { 2, 3 }; // overwrite Slot3 // create Slot4
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonFileOperationTransfer<PlatformSteam, PlatformPlaystation>(pathPlaystation, path, userIdentificationPlaystation, userIdentification, slotPlaystation, userDecisionsPlaystation, transfer, existingContainersCount, resultsPlaystation);
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

        var existingContainersCount = 8; // 5 + 1 (Slot3) + 2 (Slot4)
        var path = GetCombinedPath("Steam", "st_76561198371877533");
        var transfer = new[] { 2, 3 }; // overwrite Slot3 // create Slot4
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonFileOperationTransfer<PlatformSteam, PlatformPlaystation>(pathPlaystation, path, userIdentificationPlaystation, userIdentification, slotPlaystation, userDecisionsPlaystation, transfer, existingContainersCount, resultsPlaystation);
    }

    [TestMethod]
    public void T404_TransferFromSteam()
    {
        // Arrange
        var pathSteam = GetCombinedPath("Steam", "st_76561198042453834");
        var resultsSteam = new ReadResults[]
        {
            new(6, "Slot4Auto", true, true, false, true, false, false, false, false, SaveContextQueryEnum.Main, nameof(PresetGameModeEnum.Permadeath), DifficultyPresetTypeEnum.Permadeath, SeasonEnum.None, 4153, 6713, GameVersionEnum.WorldsPartI, "The Final Frontier", "Im Hebino XVIII-System", 5495),
            new(7, "Slot4Manual", true, true, false, true, false, false, false, false, SaveContextQueryEnum.Main, nameof(PresetGameModeEnum.Permadeath), DifficultyPresetTypeEnum.Permadeath, SeasonEnum.None, 4153, 6713, GameVersionEnum.WorldsPartI, "The Final Frontier", "An Bord von „Hebino XVIII“-Treffpunkt", 5521),
        };
        var slotSteam = 3; // get Slot4
        var userDecisionsSteam = 1;
        var userIdentificationSteam = ReadUserIdentification(pathSteam);

        var existingContainersCount = 8; // 5 + 1 (Slot3) + 2 (Slot4)
        var path = GetCombinedPath("Steam", "st_76561198371877533");
        var transfer = new[] { 2, 3 }; // overwrite Slot3 // create Slot4
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonFileOperationTransfer<PlatformSteam, PlatformSteam>(pathSteam, path, userIdentificationSteam, userIdentification, slotSteam, userDecisionsSteam, transfer, existingContainersCount, resultsSteam);
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

        var existingContainersCount = 6; // 5 + 1 (Slot?)
        var path = GetCombinedPath("Steam", "st_76561198371877533");
        var transfer = new[] { 2, 3 }; // overwrite Slot3 // create Slot4
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonFileOperationTransfer<PlatformSteam, PlatformSwitch>(pathSwitch, path, userIdentificationSwitch, userIdentification, slotSwitch, userDecisionsSwitch, transfer, existingContainersCount, resultsSwitch);
    }
}
