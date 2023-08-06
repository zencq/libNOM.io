using CommunityToolkit.Diagnostics;
using libNOM.io;
using libNOM.io.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;

namespace libNOM.test;


// Do not use System.Range for simplicity of the file and performace is not critial.
[TestClass]
[DeploymentItem("..\\..\\..\\Resources\\TESTSUITE_ARCHIVE.zip")]
public class SteamTest : CommonTestInitializeCleanup
{
    #region Constant

    private const uint META_HEADER = 0xEEEEEEBE; // 4008636094
    private const int META_SIZE = 0x68 / sizeof(uint); // 26
    private const int META_SIZE_WAYPOINT = 0x168 / sizeof(uint); // 90

    #endregion

    #region Meta

    /// <see cref="Platform.ReadMeta(Container)"/>
    /// <see cref="PlatformSteam.DecryptMeta(Container, byte[])"/>
    private static uint[] DecryptMeta(Container container)
    {
        byte[] meta = File.ReadAllBytes(container.MetaFile!.FullName);

        uint hash = 0;
        int iterations = meta.Length / sizeof(uint) == META_SIZE ? 8 : 6;
        uint[] key = GetKey(container);
        uint[] value = GetUInt32(meta);

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
        uint[] key = GetUInt32(Encoding.ASCII.GetBytes("NAESEVADNAYRTNRG"));

        key[0] = (indexRotated * 5) + 0xE6546B64;

        return key;
    }

    private static void AssertCommonMeta(Container container, uint[] metaA, uint[] metaB)
    {
        Assert.AreEqual(metaA.Length, metaB.Length);

        AssertAllAreEqual(META_HEADER, metaA[0], metaB[0]);

        if (metaA.Length == META_SIZE)
        {
            if (container.IsAccount || container.IsSave && !container.IsFrontiers)
            {
                // Editing account data is possible since Frontiers and therefore has always the new format but otherwise uses the old format.
                AssertAllAreEqual(container.IsAccount ? SAVE_FORMAT_3 : SAVE_FORMAT_2, metaA[1], metaB[1]);

                AssertAllNotZero(metaA.Skip(2).Take(4), metaB.Skip(2).Take(4));
                AssertAllNotZero(metaA.Skip(6).Take(8), metaB.Skip(6).Take(8));
                AssertAllZero(metaA.Skip(14), metaB.Skip(14));
            }
            else if (container.IsFrontiers)
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
        else if (metaA.Length == META_SIZE_WAYPOINT)
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
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameModeEnum, SeasonEnum SeasonEnum, int BaseVersion, GameVersionEnum GameVersionEnum)[]
{
            (0, true, false, PresetGameModeEnum.Unspecified, SeasonEnum.None, 4145, GameVersionEnum.Singularity), // 1Auto
            (1, true, false, PresetGameModeEnum.Unspecified, SeasonEnum.None, 4145, GameVersionEnum.Singularity), // 1Manual
            (2, true, false, PresetGameModeEnum.Unspecified, SeasonEnum.None, 4142, GameVersionEnum.WaypointWithSuperchargedSlots), // 2Auto
            (3, true, false, PresetGameModeEnum.Unspecified, SeasonEnum.None, 4143, GameVersionEnum.Fractal), // 2Manual
            (6, true, false, PresetGameModeEnum.Permadeath, SeasonEnum.None, 4142, GameVersionEnum.WaypointWithSuperchargedSlots), // 4Auto
            (7, true, false, PresetGameModeEnum.Permadeath, SeasonEnum.None, 4142, GameVersionEnum.WaypointWithSuperchargedSlots), // 4Manual
            (10, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4142, GameVersionEnum.WaypointWithSuperchargedSlots), // 6Auto
            (11, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4142, GameVersionEnum.WaypointWithSuperchargedSlots), // 6Manual
            (22, true, false, PresetGameModeEnum.Creative, SeasonEnum.None, 4142, GameVersionEnum.WaypointWithSuperchargedSlots), // 12Auto
            (23, true, false, PresetGameModeEnum.Creative, SeasonEnum.None, 4142, GameVersionEnum.WaypointWithSuperchargedSlots), // 12Manual
        };
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformSteam(path, settings);

        // Assert
        Assert.IsTrue(platform.HasAccountData);
        Assert.AreEqual(results.Length, platform.GetExistingContainers().Count());
        Assert.AreEqual(userIdentification[0], platform.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification[1], platform.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification[2], platform.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK);

        for (var i = 0; i < results.Length; i++)
        {
            var container = platform.GetSaveContainer(results[i].CollectionIndex)!;
            Assert.AreEqual(results[i].Exists, container.Exists);
            Assert.AreEqual(results[i].IsOld, container.IsOld);
            Assert.AreEqual(results[i].GameModeEnum, container.GameModeEnum);
            Assert.AreEqual(results[i].SeasonEnum, container.SeasonEnum);
            Assert.AreEqual(results[i].BaseVersion, container.BaseVersion);
            Assert.AreEqual(results[i].GameVersionEnum, container.GameVersionEnum);
        }
    }

    [TestMethod]
    public void T02_Read_76561198043217184()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198043217184");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameModeEnum, SeasonEnum SeasonEnum, int BaseVersion, GameVersionEnum GameVersionEnum)[]
        {
            (0, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4125, GameVersionEnum.ExoMech), // 1Auto
            (1, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4125, GameVersionEnum.ExoMech), // 1Manual
            (2, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4126, GameVersionEnum.Origins), // 2Auto
            (3, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4126, GameVersionEnum.Origins), // 2Manual
            (4, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4126, GameVersionEnum.Desolation), // 3Auto
            (5, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4126, GameVersionEnum.Desolation), // 3Manual
            (6, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4125, GameVersionEnum.ExoMech), // 4Auto
            (7, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4125, GameVersionEnum.ExoMech), // 4Manual
            (8, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4125, GameVersionEnum.ExoMech), // 5Auto
            (9, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4125, GameVersionEnum.ExoMech), // 5Manual
        };
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformSteam(path, settings);

        // Assert
        Assert.IsFalse(platform.HasAccountData);
        Assert.AreEqual(results.Length, platform.GetExistingContainers().Count());
        Assert.AreEqual(userIdentification[0], platform.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification[1], platform.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification[2], platform.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK);

        for (var i = 0; i < results.Length; i++)
        {
            var container = platform.GetSaveContainer(results[i].CollectionIndex)!;
            Assert.AreEqual(results[i].Exists, container.Exists);
            Assert.AreEqual(results[i].IsOld, container.IsOld);
            Assert.AreEqual(results[i].GameModeEnum, container.GameModeEnum);
            Assert.AreEqual(results[i].SeasonEnum, container.SeasonEnum);
            Assert.AreEqual(results[i].BaseVersion, container.BaseVersion);
            Assert.AreEqual(results[i].GameVersionEnum, container.GameVersionEnum);
        }
    }

    [TestMethod]
    public void T03_Read_76561198371877533()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameModeEnum, SeasonEnum SeasonEnum, int BaseVersion, GameVersionEnum GameVersionEnum)[]
        {
            (0, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4135, GameVersionEnum.Frontiers), // 1Auto
            (1, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4135, GameVersionEnum.Frontiers), // 1Manual
            (2, true, false, PresetGameModeEnum.Creative, SeasonEnum.None, 4127, GameVersionEnum.Companions), // 2Auto
            (3, true, false, PresetGameModeEnum.Creative, SeasonEnum.None, 4127, GameVersionEnum.Companions), // 2Manual
            (4, true, false, PresetGameModeEnum.Seasonal, SeasonEnum.Pioneers, 4129, GameVersionEnum.Expeditions), // 3Auto
        };
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformSteam(path, settings);

        // Assert
        Assert.IsTrue(platform.HasAccountData);
        Assert.AreEqual(results.Length, platform.GetExistingContainers().Count());
        Assert.AreEqual(userIdentification[0], platform.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification[1], platform.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification[2], platform.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK);

        for (var i = 0; i < results.Length; i++)
        {
            var container = platform.GetSaveContainer(results[i].CollectionIndex)!;
            Assert.AreEqual(results[i].Exists, container.Exists);
            Assert.AreEqual(results[i].IsOld, container.IsOld);
            Assert.AreEqual(results[i].GameModeEnum, container.GameModeEnum);
            Assert.AreEqual(results[i].SeasonEnum, container.SeasonEnum);
            Assert.AreEqual(results[i].BaseVersion, container.BaseVersion);
            Assert.AreEqual(results[i].GameVersionEnum, container.GameVersionEnum);
        }
    }

    /// <summary>
    /// Same as <see cref="T03_Read_76561198371877533"/>.
    /// </summary>
    [TestMethod]
    public void T04_Read_NoAccountInDirectory()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "something");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameModeEnum, SeasonEnum SeasonEnum, int BaseVersion, GameVersionEnum GameVersionEnum)[]
        {
            (0, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4135, GameVersionEnum.Frontiers), // 1Auto
            (1, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4135, GameVersionEnum.Frontiers), // 1Manual
            (2, true, false, PresetGameModeEnum.Creative, SeasonEnum.None, 4127, GameVersionEnum.Companions), // 2Auto
            (3, true, false, PresetGameModeEnum.Creative, SeasonEnum.None, 4127, GameVersionEnum.Companions), // 2Manual
            (4, true, false, PresetGameModeEnum.Seasonal, SeasonEnum.Pioneers, 4129, GameVersionEnum.Expeditions), // 3Auto
        };
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformSteam(path, settings);

        // Assert
        Assert.IsTrue(platform.HasAccountData);
        Assert.AreEqual(results.Length, platform.GetExistingContainers().Count());
        Assert.AreEqual(userIdentification[0], platform.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification[1], platform.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification[2], platform.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK);

        for (var i = 0; i < results.Length; i++)
        {
            var container = platform.GetSaveContainer(results[i].CollectionIndex)!;
            Assert.AreEqual(results[i].Exists, container.Exists);
            Assert.AreEqual(results[i].IsOld, container.IsOld);
            Assert.AreEqual(results[i].GameModeEnum, container.GameModeEnum);
            Assert.AreEqual(results[i].SeasonEnum, container.SeasonEnum);
            Assert.AreEqual(results[i].BaseVersion, container.BaseVersion);
            Assert.AreEqual(results[i].GameVersionEnum, container.GameVersionEnum);
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
        };
        var userIdentification = ReadUserIdentification(path);
        var writeCallback = false;

        // Act
        var platformA = new PlatformSteam(path, settings);
        var containerA = platformA.GetSaveContainer(0)!;
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
        var containerB = platformB.GetSaveContainer(0)!;
        var metaB = DecryptMeta(containerB);

        platformB.Load(containerB);
        (int Units, long UtcTicks) valuesReload = (containerB.GetJsonValue<int>(UNITS_JSON_PATH), containerB.LastWriteTime!.Value.UtcTicks);
#pragma warning restore IDE0042 // Deconstruct variable declaration

        // Assert
        Assert.IsTrue(writeCallback);

        Assert.AreEqual(-123571, valuesOrigin.Units); // 4294843725
        Assert.AreEqual(637376113621684301, valuesOrigin.UtcTicks); // 2020-10-06 20:02:42 +00:00
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
        };
        var userIdentification = ReadUserIdentification(path);
        var writeCallback = false;

        // Act
        var platformA = new PlatformSteam(path, settings);
        var containerA = platformA.GetSaveContainer(0)!;
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
        var containerB = platformB.GetSaveContainer(0)!;
        var metaB = DecryptMeta(containerB);

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
        AssertAllAreEqual(4135, (uint)(containerA.BaseVersion), (uint)(containerB.BaseVersion), metaA[17], metaB[17]);
        var bytesA = BitConverter.GetBytes(metaA[18]);
        var bytesB = BitConverter.GetBytes(metaB[18]);
        AssertAllAreEqual((uint)(PresetGameModeEnum.Normal), (uint)(containerA.GameModeEnum!), (uint)(containerB.GameModeEnum!), (uint)(BitConverter.ToInt16(bytesA, 0)), (uint)(BitConverter.ToInt16(bytesB, 0)));
        AssertAllAreEqual((uint)(SeasonEnum.None), (uint)(containerA.SeasonEnum), (uint)(containerB.SeasonEnum), BitConverter.ToUInt16(bytesA, 2), BitConverter.ToUInt16(bytesA, 2));
        AssertAllAreEqual(94164, (uint)(containerA.TotalPlayTime), (uint)(containerB.TotalPlayTime), metaA[19], metaB[19]);
    }

    [TestMethod]
    public void T12_Write_Default_0x7D2_Waypoint()
    {
        var now = DateTimeOffset.UtcNow;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198042453834");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };
        var userIdentification = ReadUserIdentification(path);
        var writeCallback = false;

        // Act
        var platformA = new PlatformSteam(path, settings);
        var containerA = platformA.GetSaveContainer(0)!;
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
        var containerB = platformB.GetSaveContainer(0)!;
        var metaB = DecryptMeta(containerB);

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
        AssertAllAreEqual(4145, (uint)(containerA.BaseVersion), (uint)(containerB.BaseVersion), metaA[17], metaB[17]);
        var bytesA = BitConverter.GetBytes(metaA[18]);
        var bytesB = BitConverter.GetBytes(metaB[18]);
        // Container GameMode + 1 as it is Unspecified but internally with the number for Normal.
        AssertAllAreEqual((uint)(PresetGameModeEnum.Normal), (uint)(containerA.GameModeEnum!) + 1, (uint)(containerB.GameModeEnum!) + 1, (uint)(BitConverter.ToInt16(bytesA, 0)), (uint)(BitConverter.ToInt16(bytesB, 0)));
        AssertAllAreEqual((uint)(SeasonEnum.None), (uint)(containerA.SeasonEnum), (uint)(containerB.SeasonEnum), BitConverter.ToUInt16(bytesA, 2), BitConverter.ToUInt16(bytesA, 2));
        AssertAllAreEqual(1253526, (uint)(containerA.TotalPlayTime), (uint)(containerB.TotalPlayTime), metaA[19], metaB[19]);
    }

    [TestMethod]
    public void T13_Write_Default_0x7D2_Frontiers_Account()
    {
        var now = DateTimeOffset.UtcNow;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };
        var userIdentification = ReadUserIdentification(path);
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
        };
        var userIdentification = ReadUserIdentification(path);
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
        Assert.AreEqual(638263807917034129, valuesOrigin.UtcTicks); // 2023-07-22 15:12:32 +00:00
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
        };
        var userIdentification = ReadUserIdentification(path);
        var writeCallback = false;

        // Act
        var platformA = new PlatformSteam(path, settings);
        var containerA = platformA.GetSaveContainer(0)!;

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
        var containerB = platformB.GetSaveContainer(0)!;

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
            WriteAlways = true,
        };
        var userIdentification = ReadUserIdentification(path);
        var writeCallback = false;

        // Act
        var platformA = new PlatformSteam(path, settings);
        var containerA = platformA.GetSaveContainer(0)!;

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
        var containerB = platformB.GetSaveContainer(0)!;

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
            WriteAlways = false,
        };
        var userIdentification = ReadUserIdentification(path);
        var writeCallback = false;

        // Act
        var platformA = new PlatformSteam(path, settings);
        var containerA = platformA.GetSaveContainer(0)!;

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
        var containerB = platformB.GetSaveContainer(0)!;

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
        var pathSave = Path.Combine(path, "save.hg");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            Watcher = true,
        };

        // Act
        var bytes = File.ReadAllBytes(pathSave);
        var platform = new PlatformSteam(path, settings);

        var container = platform.GetSaveContainer(0)!;
        platform.Load(container);

        File.WriteAllBytes(pathSave, bytes);
        Thread.Sleep(FILESYSTEMWATCHER_SLEEP);
        var watchers1 = platform.GetWatcherContainers();
        var count1 = watchers1.Count();
        var synced1 = container.IsSynced;

        container.SetJsonValue(UNITS_NEW_AMOUNT, UNITS_JSON_PATH);
        var synced2 = container.IsSynced;

        File.WriteAllBytes(pathSave, bytes);
        Thread.Sleep(FILESYSTEMWATCHER_SLEEP);
        var watchers2 = platform.GetWatcherContainers();
        var count2 = watchers2.Count();
        var synced3 = container.IsSynced;

        var watcherContainer2 = watchers2.FirstOrDefault();
        Guard.IsNotNull(watcherContainer2);
        platform.OnWatcherDecision(watcherContainer2, false);
        var synced4 = container.IsSynced;

        File.WriteAllBytes(pathSave, bytes);
        Thread.Sleep(FILESYSTEMWATCHER_SLEEP);
        var watchers3 = platform.GetWatcherContainers();
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

        var container0 = platform.GetSaveContainer(0)!; // 1Auto
        var container2 = platform.GetSaveContainer(2)!; // 2Auto
        var container3 = platform.GetSaveContainer(3)!; // 2Manual
        var container4 = platform.GetSaveContainer(4)!; // 3Auto
        var container7 = platform.GetSaveContainer(7)!; // 4Manual
        var container9 = platform.GetSaveContainer(9)!; // 5Manual

        platform.Copy(container0, container2); // 1Auto -> 2Auto (overwrite)
        platform.Copy(container3, container7); // 2Manual -> 4Manual (create)
        platform.Copy(container9, container4); // 5Manual -> 3Auto (delete)

        // Assert
        Assert.IsTrue(container2.Exists);
        Assert.AreEqual(container0.GameModeEnum, container2.GameModeEnum);
        Assert.AreEqual(container0.SeasonEnum, container2.SeasonEnum);
        Assert.AreEqual(container0.BaseVersion, container2.BaseVersion);
        Assert.AreEqual(container0.GameVersionEnum, container2.GameVersionEnum);

        Assert.IsTrue(container7.Exists);
        Assert.AreEqual(container3.GameModeEnum, container7.GameModeEnum);
        Assert.AreEqual(container3.SeasonEnum, container7.SeasonEnum);
        Assert.AreEqual(container3.BaseVersion, container7.BaseVersion);
        Assert.AreEqual(container3.GameVersionEnum, container7.GameVersionEnum);

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

        var container0 = platform.GetSaveContainer(0)!; // 1Auto
        var container1 = platform.GetSaveContainer(1)!; // 1Manual

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

        var container0 = platform.GetSaveContainer(0)!; // 1Auto
        var container1 = platform.GetSaveContainer(1)!; // 1Manual
        var container2 = platform.GetSaveContainer(2)!; // 2Auto
        var container4 = platform.GetSaveContainer(4)!; // 3Auto
        var container8 = platform.GetSaveContainer(8)!; // 5Auto
        var container9 = platform.GetSaveContainer(9)!; // 5Manual

        var totalPlayTime1 = container1.TotalPlayTime;
        platform.Move(container1, container0); // overwrite in same slot

        platform.Move(container8, container2); // delete

        var gameModeEnum4 = container4.GameModeEnum;
        var seasonEnum4 = container4.SeasonEnum;
        var baseVersion4 = container4.BaseVersion;
        var versionEnum4 = container4.GameVersionEnum;
        platform.Move(container4, container9); // move

        // Assert
        Assert.IsTrue(container0.Exists);
        Assert.IsFalse(container1.Exists);
        Assert.AreEqual(totalPlayTime1, container0.TotalPlayTime);

        Assert.IsFalse(container2.Exists);
        Assert.IsFalse(container8.Exists);
        Assert.AreEqual(libNOM.io.Globals.Constants.INCOMPATIBILITY_006, container2.IncompatibilityTag);
        Assert.AreEqual(libNOM.io.Globals.Constants.INCOMPATIBILITY_006, container8.IncompatibilityTag);

        Assert.IsFalse(container4.Exists);
        Assert.IsTrue(container9.Exists);
        Assert.AreEqual(gameModeEnum4, container9.GameModeEnum);
        Assert.AreEqual(seasonEnum4, container9.SeasonEnum);
        Assert.AreEqual(baseVersion4, container9.BaseVersion);
        Assert.AreEqual(versionEnum4, container9.GameVersionEnum);
    }

    [TestMethod]
    public void TransferToGog()
    {
        // Arrange
        // Act

        // ... Read User/Read User/Transfer/Compare

        // Assert
    }

    [TestMethod]
    public void TransferToMicrosoft()
    {
        // Arrange
        // Act
        // Assert
    }

    [TestMethod]
    public void TransferToPlaystation()
    {
        // Arrange
        // Act
        // Assert
    }

    [TestMethod]
    public void TransferToSteam()
    {
        // Arrange
        // Act
        // Assert
    }

    [TestMethod]
    public void TransferToSwitch()
    {
        // Arrange
        // Act
        // Assert
    }
}
