using System.Text;

using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;

using libNOM.io;
using libNOM.io.Enums;
using libNOM.io.Models;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace libNOM.test;


// Do not use System.Range for simplicity of the file and performace is not critical.
[TestClass]
[DeploymentItem("../../../Resources/TESTSUITE_ARCHIVE.zip")]
public class SteamTest : CommonTestInitializeCleanup
{
    #region Constant

    private const uint META_HEADER = 0xEEEEEEBE; // 4008636094

    private const int META_LENGTH_TOTAL_VANILLA = 0x68 / sizeof(uint); // 26
    private const int META_LENGTH_TOTAL_WAYPOINT = 0x168 / sizeof(uint); // 90

    #endregion

    #region Meta

    /// <see cref="Platform.ReadMeta(Container)"/>
    /// <see cref="PlatformSteam.DecryptMeta(Container, byte[])"/>
    private static uint[] DecryptMeta(Container container)
    {
        byte[] meta = File.ReadAllBytes(container.MetaFile!.FullName);

        uint hash = 0;
        int iterations = meta.Length / sizeof(uint) == META_LENGTH_TOTAL_VANILLA ? 8 : 6;
        uint[] key = GetKey(container);
        uint[] value = ToUInt32(meta);

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

    /// <see cref="PlatformSteam.GetKey(Container)"/>
    /// <see cref="PlatformSteam.RotateLeft(uint, int)"/>
    private static uint[] GetKey(Container container)
    {
        uint index = (uint)(container.MetaIndex == 0 ? 1 : container.MetaIndex) ^ 0x1422CB8C;
        uint indexRotated = (index << 13) | (index >> (32 - 13));
        uint[] key = ToUInt32(Encoding.ASCII.GetBytes("NAESEVADNAYRTNRG"));

        key[0] = (indexRotated * 5) + 0xE6546B64;

        return key;
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

    #endregion

    [TestMethod]
    public void T01_Read_76561198042453834()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198042453834");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum SeasonEnum, int BaseVersion, GameVersionEnum GameVersionEnum, string SaveName, string SaveSummary)[]
{
            (0, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4145, GameVersionEnum.Singularity, "Iteration 1", "Aboard the Space Anomaly"), // 1Auto
            (1, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4145, GameVersionEnum.Singularity, "Iteration 1", "Aboard the Space Anomaly"), // 1Manual
            (2, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4142, GameVersionEnum.WaypointWithSuperchargedSlots, "Playground", "Within Rigonn-Enve Outpost"), // 2Auto
            (3, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4143, GameVersionEnum.Fractal, "Playground", "Within Rigonn-Enve Outpost"), // 2Manual
            (6, true, false, PresetGameModeEnum.Permadeath, DifficultyPresetTypeEnum.Permadeath, SeasonEnum.None, 4142, GameVersionEnum.WaypointWithSuperchargedSlots, "The Final Frontier", "Within Wemexb Colony"), // 4Auto
            (7, true, false, PresetGameModeEnum.Permadeath, DifficultyPresetTypeEnum.Permadeath, SeasonEnum.None, 4142, GameVersionEnum.WaypointWithSuperchargedSlots, "The Final Frontier", "Within Wemexb Colony"), // 4Manual
            (8, true, false, PresetGameModeEnum.Permadeath, DifficultyPresetTypeEnum.Permadeath, SeasonEnum.None, 4147, GameVersionEnum.Omega, "Omega Permadeath", "Auf dem Planeten (Treeph)"), // 5Auto
            (10, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4142, GameVersionEnum.WaypointWithSuperchargedSlots, "", "On Planet (Ekios)"), // 6Auto
            (11, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4142, GameVersionEnum.WaypointWithSuperchargedSlots, "", "On Planet (Ekios)"), // 6Manual
            (12, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Relaxed, SeasonEnum.None, 4147, GameVersionEnum.Omega, "Omega Relax", "Auf dem Planeten (Joyo 86/Y2)"), // 7Auto
            (22, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4142, GameVersionEnum.WaypointWithSuperchargedSlots, "Collector", "Aboard the Space Anomaly"), // 12Auto
            (23, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4142, GameVersionEnum.WaypointWithSuperchargedSlots, "Collector", "Aboard the Space Anomaly"), // 12Manual
        };
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
            UseExternalSourcesForUserIdentification = false,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformSteam(path, settings);

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
            Assert.AreEqual(results[i].GameDifficulty, container.GameDifficulty);
            Assert.AreEqual(results[i].SeasonEnum, container.Season);
            Assert.AreEqual(results[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(results[i].GameVersionEnum, container.GameVersion);
            Assert.AreEqual(results[i].SaveName, container.SaveName);
            Assert.AreEqual(results[i].SaveSummary, container.SaveSummary);
        }
    }

    [TestMethod]
    public void T02_Read_76561198043217184()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198043217184");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum SeasonEnum, int BaseVersion, GameVersionEnum GameVersionEnum)[]
        {
            (0, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4125, GameVersionEnum.ExoMech), // 1Auto
            (1, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4125, GameVersionEnum.ExoMech), // 1Manual
            (2, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4126, GameVersionEnum.Origins), // 2Auto
            (3, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4126, GameVersionEnum.Origins), // 2Manual
            (4, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4126, GameVersionEnum.Desolation), // 3Auto
            (5, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4126, GameVersionEnum.Desolation), // 3Manual
            (6, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4125, GameVersionEnum.ExoMech), // 4Auto
            (7, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4125, GameVersionEnum.ExoMech), // 4Manual
            (8, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4125, GameVersionEnum.ExoMech), // 5Auto
            (9, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4125, GameVersionEnum.ExoMech), // 5Manual
        };
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
            UseExternalSourcesForUserIdentification = false,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformSteam(path, settings);

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
            Assert.AreEqual(results[i].GameDifficulty, container.GameDifficulty);
            Assert.AreEqual(results[i].SeasonEnum, container.Season);
            Assert.AreEqual(results[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(results[i].GameVersionEnum, container.GameVersion);
        }
    }

    [TestMethod]
    public void T03_Read_76561198371877533()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum SeasonEnum, int BaseVersion, GameVersionEnum GameVersionEnum)[]
        {
            (0, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, GameVersionEnum.Frontiers), // 1Auto
            (1, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, GameVersionEnum.Frontiers), // 1Manual
            (2, true, false, PresetGameModeEnum.Creative, DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4127, GameVersionEnum.Companions), // 2Auto
            (3, true, false, PresetGameModeEnum.Creative, DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4127, GameVersionEnum.Companions), // 2Manual
            (4, true, false, PresetGameModeEnum.Seasonal, DifficultyPresetTypeEnum.Normal, SeasonEnum.Pioneers, 4129, GameVersionEnum.Expeditions), // 3Auto
        };
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
            UseExternalSourcesForUserIdentification = false,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformSteam(path, settings);

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
            Assert.AreEqual(results[i].GameDifficulty, container.GameDifficulty);
            Assert.AreEqual(results[i].SeasonEnum, container.Season);
            Assert.AreEqual(results[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(results[i].GameVersionEnum, container.GameVersion);
        }
    }

    [TestMethod]
    public void T04_Read_76561198093556678()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198093556678");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum SeasonEnum, int BaseVersion, GameVersionEnum GameVersionEnum, string SaveName, string SaveSummary)[]
        {
            (26, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4147, GameVersionEnum.Omega, "", "Within Test Base Terrain Edits"), // 14Auto
            (27, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4147, GameVersionEnum.Omega, "", "Aboard the Space Anomaly"), // 14Manual
        };
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
            UseExternalSourcesForUserIdentification = false,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformSteam(path, settings);

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
            Assert.AreEqual(results[i].GameDifficulty, container.GameDifficulty);
            Assert.AreEqual(results[i].SeasonEnum, container.Season);
            Assert.AreEqual(results[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(results[i].GameVersionEnum, container.GameVersion);
        }
    }

    [TestMethod]
    public void T05_Read_76561199278291995()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561199278291995");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, bool HasActiveExpedition, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum SeasonEnum, int BaseVersion, GameVersionEnum GameVersionEnum, string SaveName, string SaveSummary)[]
        {
            (14, true, false, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4146, GameVersionEnum.Echoes, "The Cartographers Redux", "Aboard the Space Anomaly"), // 8Auto
            (15, true, false, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4146, GameVersionEnum.Echoes, "The Cartographers Redux", "Aboard the Space Anomaly"), // 8Manual
            (16, true, false, true, PresetGameModeEnum.Seasonal, DifficultyPresetTypeEnum.Normal, SeasonEnum.VoyagersRedux, 4146, GameVersionEnum.Echoes, "Voyagers Redux", "On freighter (DSE-6 Ariasaku)"), // 9Auto
            (17, true, false, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4146, GameVersionEnum.Echoes, "Voyagers Redux", "On freighter (DSE-6 Ariasaku)"), // 9Manual
            (20, true, false, true, PresetGameModeEnum.Seasonal, DifficultyPresetTypeEnum.Normal, SeasonEnum.OmegaExperimental, 4147, GameVersionEnum.Omega, "Omega", "Aboard the Space Anomaly"), // 11Auto
            (21, true, false, true, PresetGameModeEnum.Seasonal, DifficultyPresetTypeEnum.Normal, SeasonEnum.OmegaExperimental, 4147, GameVersionEnum.Omega, "Omega", "Aboard the Space Anomaly"), // 11Manual
            (22, true, false, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4147, GameVersionEnum.Omega, "", "Aboard the Space Anomaly"), // 12Auto
        };
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
            UseExternalSourcesForUserIdentification = false,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformSteam(path, settings);

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
            Assert.AreEqual(results[i].HasActiveExpedition, container.HasActiveExpedition);
            Assert.AreEqual(results[i].GameMode, (PresetGameModeEnum)(priect.GetFieldOrProperty("GameMode")));
            Assert.AreEqual(results[i].GameDifficulty, container.GameDifficulty);
            Assert.AreEqual(results[i].SeasonEnum, container.Season);
            Assert.AreEqual(results[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(results[i].GameVersionEnum, container.GameVersion);
        }
    }

    /// <summary>
    /// Same as <see cref="T03_Read_76561198371877533"/>.
    /// </summary>
    [TestMethod]
    public void T06_Read_NoAccountInDirectory()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "something");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum SeasonEnum, int BaseVersion, GameVersionEnum GameVersionEnum)[]
        {
            (0, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, GameVersionEnum.Frontiers), // 1Auto
            (1, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, GameVersionEnum.Frontiers), // 1Manual
            (2, true, false, PresetGameModeEnum.Creative, DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4127, GameVersionEnum.Companions), // 2Auto
            (3, true, false, PresetGameModeEnum.Creative, DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4127, GameVersionEnum.Companions), // 2Manual
            (4, true, false, PresetGameModeEnum.Seasonal, DifficultyPresetTypeEnum.Normal, SeasonEnum.Pioneers, 4129, GameVersionEnum.Expeditions), // 3Auto
        };
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
            UseExternalSourcesForUserIdentification = false,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformSteam(path, settings);

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
            Assert.AreEqual(results[i].GameDifficulty, container.GameDifficulty);
            Assert.AreEqual(results[i].SeasonEnum, container.Season);
            Assert.AreEqual(results[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(results[i].GameVersionEnum, container.GameVersion);
        }
    }

    [TestMethod]
    public void T10_Write_Default_0x7D1()
    {
        var now = DateTimeOffset.UtcNow;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198043217184");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            UseMapping = true,
        };
        var writeCallback = false;

        // Act
        var platformA = new PlatformSteam(path, settings);
        var containerA = GetOneSaveContainer(platformA, 0);
        var metaA = DecryptMeta(containerA);

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

        var platformB = new PlatformSteam(path, settings);
        var containerB = GetOneSaveContainer(platformB, 0);
        var metaB = DecryptMeta(containerB);

        platformB.Load(containerB);
        (int Units, long UtcTicks) valuesReload = (containerB.GetJsonValue<int>(UNITS_JSON_PATH), containerB.LastWriteTime!.Value.UtcTicks);
#pragma warning restore IDE0042 // Deconstruct variable declaration

        // Assert
        Assert.IsTrue(writeCallback);

        Assert.AreEqual(-123571, valuesOrigin.Units); // 4294843725
        Assert.AreEqual(637376113620000000, valuesOrigin.UtcTicks); // 2020-10-06 20:02:42 +00:00
        Assert.AreEqual(UNITS_NEW_AMOUNT, valuesSet.Units);
        Assert.AreEqual(now.UtcTicks, valuesSet.UtcTicks);

        Assert.AreEqual(UNITS_NEW_AMOUNT, valuesReload.Units);
        Assert.AreEqual(now.UtcTicks, valuesReload.UtcTicks);

        AssertCommonMeta(containerA, metaA, metaB);
    }

    [TestMethod]
    public void T11_Write_Default_0x7D2_Frontiers()
    {
        var now = DateTimeOffset.UtcNow;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            UseMapping = true,
        };
        var writeCallback = false;

        // Act
        var platformA = new PlatformSteam(path, settings);
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

        var platformB = new PlatformSteam(path, settings);
        var containerB = GetOneSaveContainer(platformB, 0);
        var metaB = DecryptMeta(containerB);
        var priectB = new PrivateObject(containerB);

        platformB.Load(containerB);
        (int Units, long UtcTicks) valuesReload = (containerB.GetJsonValue<int>(UNITS_JSON_PATH), containerB.LastWriteTime!.Value.UtcTicks);
#pragma warning restore IDE0042 // Deconstruct variable declaration

        // Assert
        Assert.IsTrue(writeCallback);

        Assert.AreEqual(-1221111157, valuesOrigin.Units); // 3073856139
        Assert.AreEqual(637663905840000000, valuesOrigin.UtcTicks); // 2021-09-04 22:16:24 +00:00
        Assert.AreEqual(UNITS_NEW_AMOUNT, valuesSet.Units);
        Assert.AreEqual(now.UtcTicks, valuesSet.UtcTicks);

        Assert.AreEqual(UNITS_NEW_AMOUNT, valuesReload.Units);
        Assert.AreEqual(now.UtcTicks, valuesReload.UtcTicks);

        AssertCommonMeta(containerA, metaA, metaB);

        var bytesA = metaA.AsSpan().AsBytes().ToArray();
        var bytesB = metaB.AsSpan().AsBytes().ToArray();

        AssertAllAreEqual(4135, (uint)(int)(priectA.GetFieldOrProperty("BaseVersion")), (uint)(int)(priectB.GetFieldOrProperty("BaseVersion")), metaA[17], metaB[17]);
        AssertAllAreEqual((ushort)(PresetGameModeEnum.Normal), (ushort)(PresetGameModeEnum)(priectA.GetFieldOrProperty("GameMode")), (ushort)(PresetGameModeEnum)(priectB.GetFieldOrProperty("GameMode")), BitConverter.ToInt16(bytesA, 72), BitConverter.ToInt16(bytesB, 72));
        AssertAllAreEqual((ushort)(SeasonEnum.None), (ushort)(containerA.Season), (ushort)(containerB.Season), BitConverter.ToUInt16(bytesA, 74), BitConverter.ToUInt16(bytesA, 74));
        AssertAllAreEqual(94164, containerA.TotalPlayTime, containerB.TotalPlayTime, metaA[19], metaB[19]);
    }

    [TestMethod]
    public void T12_Write_Default_0x7D2_Waypoint()
    {
        var now = DateTimeOffset.UtcNow;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198042453834");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            UseMapping = true,
        };
        var writeCallback = false;

        // Act
        var platformA = new PlatformSteam(path, settings);
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

        var platformB = new PlatformSteam(path, settings);
        var containerB = GetOneSaveContainer(platformB, 0);
        var metaB = DecryptMeta(containerB);
        var priectB = new PrivateObject(containerB);

        platformB.Load(containerB);
        (int Units, long UtcTicks) valuesReload = (containerB.GetJsonValue<int>(UNITS_JSON_PATH), containerB.LastWriteTime!.Value.UtcTicks);
#pragma warning restore IDE0042 // Deconstruct variable declaration

        // Assert
        Assert.IsTrue(writeCallback);

        Assert.AreEqual(1199342306, valuesOrigin.Units);
        Assert.AreEqual(638234536920000000, valuesOrigin.UtcTicks); // 2023-06-27 09:08:12 +00:00
        Assert.AreEqual(UNITS_NEW_AMOUNT, valuesSet.Units);
        Assert.AreEqual(now.UtcTicks, valuesSet.UtcTicks);

        Assert.AreEqual(UNITS_NEW_AMOUNT, valuesReload.Units);
        Assert.AreEqual(now.UtcTicks, valuesReload.UtcTicks);

        AssertCommonMeta(containerA, metaA, metaB);

        var bytesA = metaA.AsSpan().AsBytes().ToArray();
        var bytesB = metaB.AsSpan().AsBytes().ToArray();

        AssertAllAreEqual(4145, (uint)(int)(priectA.GetFieldOrProperty("BaseVersion")), (uint)(int)(priectB.GetFieldOrProperty("BaseVersion")), metaA[17], metaB[17]);
        AssertAllAreEqual((ushort)(PresetGameModeEnum.Normal), (ushort)(PresetGameModeEnum)(priectA.GetFieldOrProperty("GameMode")), (ushort)(PresetGameModeEnum)(priectB.GetFieldOrProperty("GameMode")), BitConverter.ToInt16(bytesA, 72), BitConverter.ToInt16(bytesB, 72));
        AssertAllAreEqual((ushort)(SeasonEnum.None), (ushort)(containerA.Season), (ushort)(containerB.Season), BitConverter.ToUInt16(bytesA, 74), BitConverter.ToUInt16(bytesA, 74));
        AssertAllAreEqual(1253526, containerA.TotalPlayTime, containerB.TotalPlayTime, metaA[19], metaB[19]);
        AssertAllAreEqual("Iteration 1", containerA.SaveName, containerB.SaveName, GetString(bytesA.Skip(88).TakeWhile(i => i != 0)), GetString(bytesB.Skip(88).TakeWhile(i => i != 0)));
        AssertAllAreEqual("Aboard the Space Anomaly", containerA.SaveSummary, containerB.SaveSummary, GetString(bytesA.Skip(216).TakeWhile(i => i != 0)), GetString(bytesB.Skip(216).TakeWhile(i => i != 0)));
        AssertAllAreEqual((byte)(DifficultyPresetTypeEnum.Custom), (byte)(containerA.GameDifficulty), (byte)(containerB.GameDifficulty), bytesA[344], bytesB[344]);
    }

    [TestMethod]
    public void T13_Write_Default_0x7D2_Frontiers_Account()
    {
        var now = DateTimeOffset.UtcNow;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            UseMapping = true,
        };
        var writeCallback = false;

        // Act
        var platformA = new PlatformSteam(path, settings);
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

        var platformB = new PlatformSteam(path, settings);
        var containerB = platformB.GetAccountContainer()!;
        var metaB = DecryptMeta(containerB);

        platformB.Load(containerB);
        (int MusicVolume, long UtcTicks) valuesReload = (containerB.GetJsonValue<int>(MUSICVOLUME_JSON_PATH), containerB.LastWriteTime!.Value.UtcTicks);
#pragma warning restore IDE0042 // Deconstruct variable declaration

        // Assert
        Assert.IsTrue(writeCallback);

        Assert.AreEqual(80, valuesOrigin.MusicVolume);
        Assert.AreEqual(637663896760000000, valuesOrigin.UtcTicks); // 2021-09-04 22:01:16 +00:00
        Assert.AreEqual(MUSICVOLUME_NEW_AMOUNT, valuesSet.MusicVolume);
        Assert.AreEqual(now.UtcTicks, valuesSet.UtcTicks);

        Assert.AreEqual(MUSICVOLUME_NEW_AMOUNT, valuesReload.MusicVolume);
        Assert.AreEqual(now.UtcTicks, valuesReload.UtcTicks);

        AssertCommonMeta(containerA, metaA, metaB);
    }

    [TestMethod]
    public void T14_Write_Default_0x7D2_Waypoint_Account()
    {
        var now = DateTimeOffset.UtcNow;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198042453834");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            UseMapping = true,
        };
        var writeCallback = false;

        // Act
        var platformA = new PlatformSteam(path, settings);
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

        var platformB = new PlatformSteam(path, settings);
        var containerB = platformB.GetAccountContainer()!;
        var metaB = DecryptMeta(containerB);

        platformB.Load(containerB);
        (int MusicVolume, long UtcTicks) valuesReload = (containerB.GetJsonValue<int>(MUSICVOLUME_JSON_PATH), containerB.LastWriteTime!.Value.UtcTicks);
#pragma warning restore IDE0042 // Deconstruct variable declaration

        // Assert
        Assert.IsTrue(writeCallback);

        Assert.AreEqual(80, valuesOrigin.MusicVolume);
        Assert.AreEqual(638263807920000000, valuesOrigin.UtcTicks); // 2023-07-22 08:13:12 +00:00
        Assert.AreEqual(MUSICVOLUME_NEW_AMOUNT, valuesSet.MusicVolume);
        Assert.AreEqual(now.UtcTicks, valuesSet.UtcTicks);

        Assert.AreEqual(MUSICVOLUME_NEW_AMOUNT, valuesReload.MusicVolume);
        Assert.AreEqual(now.UtcTicks, valuesReload.UtcTicks);

        AssertCommonMeta(containerA, metaA, metaB);
    }

    [TestMethod]
    public void T15_Write_SetLastWriteTime_False()
    {
        var now = DateTimeOffset.UtcNow;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            SetLastWriteTime = false,
            UseMapping = true,
        };
        var writeCallback = false;

        // Act
        var platformA = new PlatformSteam(path, settings);
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

        var platformB = new PlatformSteam(path, settings);
        var containerB = GetOneSaveContainer(platformB, 0);

        platformB.Load(containerB);
        (int Units, long UtcTicks) valuesReload = (containerB.GetJsonValue<int>(UNITS_JSON_PATH), containerB.LastWriteTime!.Value.UtcTicks);
#pragma warning restore IDE0042 // Deconstruct variable declaration

        // Assert
        Assert.IsTrue(writeCallback);

        Assert.AreEqual(-1221111157, valuesOrigin.Units); // 3073856139
        Assert.AreEqual(637663905840000000, valuesOrigin.UtcTicks); // 2021-09-04 22:16:24 +00:00
        Assert.AreEqual(UNITS_NEW_AMOUNT, valuesSet.Units);
        Assert.AreEqual(637663905840000000, valuesSet.UtcTicks);

        Assert.AreEqual(UNITS_NEW_AMOUNT, valuesReload.Units);
        Assert.AreEqual(637663905840000000, valuesReload.UtcTicks);
    }

    [TestMethod]
    public void T16_Write_WriteAlways_True()
    {
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198043217184");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            UseMapping = true,
            WriteAlways = true,
        };
        var writeCallback = false;

        // Act
        var platformA = new PlatformSteam(path, settings);
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

        var platformB = new PlatformSteam(path, settings);
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
    public void T17_Write_WriteAlways_False()
    {
        var now = DateTimeOffset.UtcNow;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198043217184");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            UseMapping = true,
            WriteAlways = false,
        };
        var writeCallback = false;

        // Act
        var platformA = new PlatformSteam(path, settings);
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

        var platformB = new PlatformSteam(path, settings);
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
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");
        var pathWatching = Path.Combine(path, "save.hg");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            UseMapping = true,
            Watcher = true,
        };

        // Act
        var bytes = File.ReadAllBytes(pathWatching);

        var platform = new PlatformSteam(path, settings);
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
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };

        // Act
        var platform = new PlatformSteam(path, settings);

        var container0 = GetOneSaveContainer(platform, 0); // 1Auto
        var container2 = GetOneSaveContainer(platform, 2); // 2Auto
        var container3 = GetOneSaveContainer(platform, 3); // 2Manual
        var container4 = GetOneSaveContainer(platform, 4); // 3Auto
        var container7 = GetOneSaveContainer(platform, 7); // 4Manual (!Exists)
        var container9 = GetOneSaveContainer(platform, 9); // 5Manual (!Exists)

        platform.Copy(container0, container2); // 1Auto -> 2Auto (overwrite)
        platform.Copy(container3, container7); // 2Manual -> 4Manual (create)
        platform.Copy(container9, container4); // 5Manual -> 3Auto (delete)

        // Assert
        var priect0 = new PrivateObject(container0);
        var priect2 = new PrivateObject(container2);
        var priect3 = new PrivateObject(container3);
        var priect7 = new PrivateObject(container7);

        Assert.IsTrue(container2.Exists);
        Assert.AreEqual((PresetGameModeEnum)(priect0.GetFieldOrProperty("GameMode")), (PresetGameModeEnum)(priect2.GetFieldOrProperty("GameMode")));
        Assert.AreEqual(container0.GameDifficulty, container2.GameDifficulty);
        Assert.AreEqual(container0.Season, container2.Season);
        Assert.AreEqual((int)(priect0.GetFieldOrProperty("BaseVersion")), (int)(priect2.GetFieldOrProperty("BaseVersion")));
        Assert.AreEqual(container0.GameVersion, container2.GameVersion);
        Assert.AreEqual(container0.TotalPlayTime, container2.TotalPlayTime);

        Assert.IsTrue(container7.Exists);
        Assert.AreEqual((PresetGameModeEnum)(priect3.GetFieldOrProperty("GameMode")), (PresetGameModeEnum)(priect7.GetFieldOrProperty("GameMode")));
        Assert.AreEqual(container3.GameDifficulty, container7.GameDifficulty);
        Assert.AreEqual(container3.Season, container7.Season);
        Assert.AreEqual((int)(priect3.GetFieldOrProperty("BaseVersion")), (int)(priect7.GetFieldOrProperty("BaseVersion")));
        Assert.AreEqual(container3.GameVersion, container7.GameVersion);
        Assert.AreEqual(container3.TotalPlayTime, container7.TotalPlayTime);

        Assert.IsFalse(container4.Exists);
    }

    [TestMethod]
    public void T31_Delete()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };

        // Act
        var platform = new PlatformSteam(path, settings);

        var container0 = GetOneSaveContainer(platform, 0); // 1Auto
        var container1 = GetOneSaveContainer(platform, 1); // 1Manual

        platform.Delete(container0);
        platform.Delete(container1);

        // Assert
        Assert.IsFalse(container0.Exists);
        Assert.AreEqual(libNOM.io.Globals.Constants.INCOMPATIBILITY_006, container0.IncompatibilityTag);

        Assert.IsFalse(container1.Exists);
        Assert.AreEqual(libNOM.io.Globals.Constants.INCOMPATIBILITY_006, container1.IncompatibilityTag);
    }

    [TestMethod]
    public void T32_Move()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };

        // Act
        var platform = new PlatformSteam(path, settings);

        var container0 = GetOneSaveContainer(platform, 0); // 1Auto
        var container1 = GetOneSaveContainer(platform, 1); // 1Manual
        var container2 = GetOneSaveContainer(platform, 2); // 2Auto
        var container4 = GetOneSaveContainer(platform, 4); // 3Auto
        var container8 = GetOneSaveContainer(platform, 8); // 5Auto
        var container9 = GetOneSaveContainer(platform, 9); // 5Manual

        var priect1 = new PrivateObject(container1);
        var priect4 = new PrivateObject(container4);

        var gameModeEnum1 = (PresetGameModeEnum)(priect1.GetFieldOrProperty("GameMode"));
        var gameDifficultyEnum1 = container1.GameDifficulty;
        var seasonEnum1 = container1.Season;
        var baseVersion1 = (int)(priect1.GetFieldOrProperty("BaseVersion"));
        var versionEnum1 = container1.GameVersion;
        var totalPlayTime1 = container1.TotalPlayTime;
        platform.Move(container1, container0); // overwrite in same slot

        platform.Move(container8, container2); // delete

        var gameModeEnum4 = (PresetGameModeEnum)(priect4.GetFieldOrProperty("GameMode"));
        var gameDifficultyEnum4 = container4.GameDifficulty;
        var seasonEnum4 = container4.Season;
        var baseVersion4 = (int)(priect4.GetFieldOrProperty("BaseVersion"));
        var versionEnum4 = container4.GameVersion;
        var totalPlayTime4 = container4.TotalPlayTime;
        platform.Move(container4, container9); // move

        // Assert
        var priect0 = new PrivateObject(container0);
        var priect9 = new PrivateObject(container9);

        Assert.IsFalse(container1.Exists); Assert.IsTrue(container0.Exists);
        Assert.AreEqual(gameModeEnum1, (PresetGameModeEnum)(priect0.GetFieldOrProperty("GameMode")));
        Assert.AreEqual(gameDifficultyEnum1, container0.GameDifficulty);
        Assert.AreEqual(seasonEnum1, container0.Season);
        Assert.AreEqual(baseVersion1, (int)(priect0.GetFieldOrProperty("BaseVersion")));
        Assert.AreEqual(versionEnum1, container0.GameVersion);
        Assert.AreEqual(totalPlayTime1, container0.TotalPlayTime);

        Assert.IsFalse(container2.Exists);
        Assert.IsFalse(container8.Exists);
        Assert.AreEqual(libNOM.io.Globals.Constants.INCOMPATIBILITY_006, container2.IncompatibilityTag);
        Assert.AreEqual(libNOM.io.Globals.Constants.INCOMPATIBILITY_006, container8.IncompatibilityTag);

        Assert.IsFalse(container4.Exists); Assert.IsTrue(container9.Exists);
        Assert.AreEqual(gameModeEnum4, (PresetGameModeEnum)(priect9.GetFieldOrProperty("GameMode")));
        Assert.AreEqual(gameDifficultyEnum4, container9.GameDifficulty);
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
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
            UseExternalSourcesForUserIdentification = false,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platformGog = new PlatformGog(pathGog, settings);
        var transfer = platformGog.GetSourceTransferData(1);

        var platform = new PlatformSteam(path, settings);

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
        Assert.AreEqual(8, GetExistingContainers(platform).Count()); // + 1 + 2

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
            Assert.AreEqual(resultsGog[i].GameDifficulty, container.GameDifficulty);
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
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
            UseExternalSourcesForUserIdentification = false,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platformMicrosoft = new PlatformMicrosoft(pathMicrosoft, settings);
        var transfer = platformMicrosoft.GetSourceTransferData(1);

        var platform = new PlatformSteam(path, settings);

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
        Assert.AreEqual(8, GetExistingContainers(platform).Count()); // + 1 + 2

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
            Assert.AreEqual(resultsMicrosoft[i].GameDifficulty, container.GameDifficulty);
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
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
            UseExternalSourcesForUserIdentification = false,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platformPlaystation = new PlatformPlaystation(pathPlaystation, settings);
        var transfer = platformPlaystation.GetSourceTransferData(1);

        var platform = new PlatformSteam(path, settings);

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
        Assert.AreEqual(8, GetExistingContainers(platform).Count()); // + 1 + 2

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
            Assert.AreEqual(resultsPlaystation[i].GameDifficulty, container.GameDifficulty);
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
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
            UseExternalSourcesForUserIdentification = false,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platformPlaystation = new PlatformPlaystation(pathPlaystation, settings);
        var transfer = platformPlaystation.GetSourceTransferData(1);

        var platform = new PlatformSteam(path, settings);

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
        Assert.AreEqual(8, GetExistingContainers(platform).Count()); // + 1 + 2

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
            Assert.AreEqual(resultsPlaystation[i].GameDifficulty, container.GameDifficulty);
            Assert.AreEqual(resultsPlaystation[i].Season, container.Season);
            Assert.AreEqual(resultsPlaystation[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(resultsPlaystation[i].Version, container.GameVersion);
        }
    }

    [TestMethod]
    public void T44_TransferFromSteam()
    {
        // Arrange
        var pathSteam = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198042453834");
        var resultsSteam = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum Season, int BaseVersion, GameVersionEnum Version, string SaveName, string SaveSummary)[]
        {
            (6, true, false, PresetGameModeEnum.Permadeath, DifficultyPresetTypeEnum.Permadeath, SeasonEnum.None, 4142, GameVersionEnum.WaypointWithSuperchargedSlots, "The Final Frontier", "Within Wemexb Colony"), // 4Auto
            (7, true, false, PresetGameModeEnum.Permadeath, DifficultyPresetTypeEnum.Permadeath, SeasonEnum.None, 4142, GameVersionEnum.WaypointWithSuperchargedSlots, "The Final Frontier", "Within Wemexb Colony"), // 4Manual
        };
        var userIdentificationSteam = ReadUserIdentification(pathSteam);

        var offset = -2;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
            UseExternalSourcesForUserIdentification = false,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platformSteam = new PlatformSteam(pathSteam, settings);
        var transfer = platformSteam.GetSourceTransferData(3);

        var platform = new PlatformSteam(path, settings);

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
        Assert.AreEqual(8, GetExistingContainers(platform).Count()); // + 1 + 2

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
            Assert.AreEqual(resultsSteam[i].GameDifficulty, container.GameDifficulty);
            Assert.AreEqual(resultsSteam[i].Season, container.Season);
            Assert.AreEqual(resultsSteam[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(resultsSteam[i].Version, container.GameVersion);
            Assert.AreEqual(resultsSteam[i].SaveName, container.SaveName);
            Assert.AreEqual(resultsSteam[i].SaveSummary, container.SaveSummary);
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
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
            UseExternalSourcesForUserIdentification = false,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platformSwitch = new PlatformSwitch(pathSwitch, settings);
        var transfer = platformSwitch.GetSourceTransferData(1);

        var platform = new PlatformSteam(path, settings);

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
        Assert.AreEqual(6, GetExistingContainers(platform).Count()); // + 1

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
            Assert.AreEqual(resultsSwitch[i].GameDifficulty, container.GameDifficulty);
            Assert.AreEqual(resultsSwitch[i].Season, container.Season);
            Assert.AreEqual(resultsSwitch[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(resultsSwitch[i].Version, container.GameVersion);
        }
    }
}
