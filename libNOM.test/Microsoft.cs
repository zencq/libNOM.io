using CommunityToolkit.Diagnostics;
using libNOM.io;
using libNOM.io.Enums;
using libNOM.io.Globals;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;

namespace libNOM.test;


[TestClass]
[DeploymentItem("..\\..\\..\\Resources\\TESTSUITE_ARCHIVE.zip")]
public class MicrosoftTest : CommonTestInitializeCleanup
{
    #region Constant

    private const int META_SIZE = 0x18 / sizeof(uint); // 6
    private const int META_SIZE_WAYPOINT = 0x118 / sizeof(uint); // 70

    protected const int TICK_DIVISOR = 10000;

    #endregion

    #region Meta

    /// <see cref="Platform.ReadMeta(Container)"/>
    /// <see cref="PlatformMicrosoft.DecryptMeta(Container, byte[])"/>
    private static uint[] DecryptMeta(Container container)
    {
        byte[] meta = File.ReadAllBytes(container.MetaFile!.FullName);
        return GetUInt32(meta);
    }

    private static void AssertCommonMeta(Container container, uint[] metaA, uint[] metaB)
    {
        Assert.AreEqual(metaA.Length, metaB.Length);

        if (metaA.Length == META_SIZE || metaA.Length == META_SIZE_WAYPOINT)
        {
            if (container.IsAccount)
            {
                AssertAllAreEqual(1, metaA[0], metaB[0]);

#if NETSTANDARD2_0
                AssertAllZero(metaA.Skip(1).Take(3).ToArray(), metaB.Skip(1).Take(3).ToArray());
#else
                AssertAllZero(metaA[1..4], metaB[1..4]);
#endif

                AssertAllNotZero(metaA[4], metaB[4]);
            }
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
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, SeasonEnum Season, int BaseVersion, VersionEnum Version)[]
        {
            (2, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4135, VersionEnum.Frontiers), // 2Auto
            (3, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4135, VersionEnum.Frontiers), // 2Manual
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
            Assert.AreEqual(results[i].GameMode, container.GameModeEnum);
            Assert.AreEqual(results[i].Season, container.SeasonEnum);
            Assert.AreEqual(results[i].BaseVersion, container.BaseVersion);
            Assert.AreEqual(results[i].Version, container.VersionEnum);
        }
    }

    [TestMethod]
    public void T02_Read_000901F4E735CFAC()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "000901F4E735CFAC_29070100B936489ABCE8B9AF3980429C");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, SeasonEnum Season, int BaseVersion, VersionEnum Version)[]
        {
            (0, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4138, VersionEnum.Outlaws), // 1Auto
            (1, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4138, VersionEnum.Outlaws), // 1Manual
            (2, true, false, PresetGameModeEnum.Seasonal, SeasonEnum.Exobiology, 4137, VersionEnum.SentinelWithVehicleAI), // 2Auto
            (3, true, false, PresetGameModeEnum.Seasonal, SeasonEnum.Exobiology, 4137, VersionEnum.SentinelWithVehicleAI), // 2Manual
            (4, true, false, PresetGameModeEnum.Seasonal, SeasonEnum.Blighted, 4138, VersionEnum.Outlaws), // 3Auto
            (5, true, false, PresetGameModeEnum.Seasonal, SeasonEnum.Blighted, 4138, VersionEnum.Outlaws), // 3Manual
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
            Assert.AreEqual(results[i].GameMode, container.GameModeEnum);
            Assert.AreEqual(results[i].Season, container.SeasonEnum);
            Assert.AreEqual(results[i].BaseVersion, container.BaseVersion);
            Assert.AreEqual(results[i].Version, container.VersionEnum);
        }
    }

    [TestMethod]
    public void T03_Read_000901F8A36808E0()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "000901F8A36808E0_29070100B936489ABCE8B9AF3980429C");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, SeasonEnum Season, int BaseVersion, VersionEnum Version)[]
        {
            (0, true, false, PresetGameModeEnum.Unspecified, SeasonEnum.None, 4143, VersionEnum.Fractal), // 1Auto
            (1, true, false, PresetGameModeEnum.Unspecified, SeasonEnum.None, 4143, VersionEnum.Fractal), // 1Manual
            (2, true, false, PresetGameModeEnum.Survival, SeasonEnum.None, 4142, VersionEnum.WaypointWithSuperchargedSlots), // 2Auto
            (3, true, false, PresetGameModeEnum.Survival, SeasonEnum.None, 4142, VersionEnum.WaypointWithSuperchargedSlots), // 2Manual
            (4, true, false, PresetGameModeEnum.Permadeath, SeasonEnum.None, 4142, VersionEnum.WaypointWithSuperchargedSlots), // 3Auto
            (5, true, false, PresetGameModeEnum.Permadeath, SeasonEnum.None, 4142, VersionEnum.WaypointWithSuperchargedSlots), // 3Manual
            (6, true, false, PresetGameModeEnum.Unspecified, SeasonEnum.None, 4143, VersionEnum.Fractal), // 4Auto
            (7, true, false, PresetGameModeEnum.Unspecified, SeasonEnum.None, 4143, VersionEnum.Fractal), // 4Manual
            (8, true, false, PresetGameModeEnum.Seasonal, SeasonEnum.Utopia, 4143, VersionEnum.Fractal), // 5Auto
            (9, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4143, VersionEnum.Fractal), // 5Manual
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
            Assert.AreEqual(results[i].GameMode, container.GameModeEnum);
            Assert.AreEqual(results[i].Season, container.SeasonEnum);
            Assert.AreEqual(results[i].BaseVersion, container.BaseVersion);
            Assert.AreEqual(results[i].Version, container.VersionEnum);
        }
    }

    [TestMethod]
    public void T04_Read_000901FB44140B02()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "000901FB44140B02_29070100B936489ABCE8B9AF3980429C");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, SeasonEnum Season, int BaseVersion, VersionEnum Version)[]
        {
            (0, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4134, VersionEnum.PrismsWithBytebeatAuthor), // 1Auto
            (2, true, false, PresetGameModeEnum.Permadeath, SeasonEnum.None, 4127, VersionEnum.Companions), // 2Auto
            (4, true, false, PresetGameModeEnum.Survival, SeasonEnum.None, 4133, VersionEnum.Beachhead), // 3Auto
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
        Assert.AreEqual(results.Length, platform.GetExistingContainers().Count());
        Assert.AreEqual(userIdentification[0], platform.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification[1], platform.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification[2], platform.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK);

        var container1 = platform.GetSaveContainer(1)!; // 1Manual
        Assert.IsFalse(container1.Exists);
        Assert.IsFalse(container1.IsOld);
        Assert.AreEqual(Constants.INCOMPATIBILITY_005, container1.IncompatibilityTag);

        for (var i = 0; i < results.Length; i++)
        {
            var container = platform.GetSaveContainer(results[i].CollectionIndex)!;
            Assert.AreEqual(results[i].Exists, container.Exists);
            Assert.AreEqual(results[i].IsOld, container.IsOld);
            Assert.AreEqual(results[i].GameMode, container.GameModeEnum);
            Assert.AreEqual(results[i].Season, container.SeasonEnum);
            Assert.AreEqual(results[i].BaseVersion, container.BaseVersion);
            Assert.AreEqual(results[i].Version, container.VersionEnum);
        }
    }

    [TestMethod]
    public void T05_Read_000901FE2C5492FC()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "000901FE2C5492FC_29070100B936489ABCE8B9AF3980429C");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, SeasonEnum Season, int BaseVersion, VersionEnum Version)[]
        {
            (1, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4135, VersionEnum.Emergence), // 1Manual
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
        Assert.AreEqual(results.Length, platform.GetExistingContainers().Count());
        Assert.AreEqual(userIdentification[0], platform.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification[1], platform.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification[2], platform.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK);

        var container0 = platform.GetSaveContainer(0)!; // 1Auto
        Assert.IsFalse(container0.Exists);
        Assert.IsFalse(container0.IsOld);
        Assert.AreEqual(Constants.INCOMPATIBILITY_004, container0.IncompatibilityTag);

        for (var i = 0; i < results.Length; i++)
        {
            var container = platform.GetSaveContainer(results[i].CollectionIndex)!;
            Assert.AreEqual(results[i].Exists, container.Exists);
            Assert.AreEqual(results[i].IsOld, container.IsOld);
            Assert.AreEqual(results[i].GameMode, container.GameModeEnum);
            Assert.AreEqual(results[i].Season, container.SeasonEnum);
            Assert.AreEqual(results[i].BaseVersion, container.BaseVersion);
            Assert.AreEqual(results[i].Version, container.VersionEnum);
        }
    }

    [TestMethod]
    public void T06_Read_000901FFCAB85905()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "000901FFCAB85905_29070100B936489ABCE8B9AF3980429C");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, SeasonEnum Season, int BaseVersion, VersionEnum Version)[]
        {
            (0, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4138, VersionEnum.Outlaws), // 1Auto
            (1, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4138, VersionEnum.Outlaws), // 1Manual
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
            Assert.AreEqual(results[i].GameMode, container.GameModeEnum);
            Assert.AreEqual(results[i].Season, container.SeasonEnum);
            Assert.AreEqual(results[i].BaseVersion, container.BaseVersion);
            Assert.AreEqual(results[i].Version, container.VersionEnum);
        }
    }

    [TestMethod]
    public void T07_Read_00090000025A963A()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "00090000025A963A_29070100B936489ABCE8B9AF3980429C");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, SeasonEnum Season, int BaseVersion, VersionEnum Version)[]
        {
            (0, true, false, PresetGameModeEnum.Creative, SeasonEnum.None, 4142, VersionEnum.WaypointWithSuperchargedSlots), // 1Auto
            (1, true, false, PresetGameModeEnum.Creative, SeasonEnum.None, 4140, VersionEnum.Waypoint), // 1Manual
            (2, true, false, PresetGameModeEnum.Seasonal, SeasonEnum.Polestar, 4139, VersionEnum.Endurance), // 2Auto
            (6, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4135, VersionEnum.Emergence), // 4Auto
            (7, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4124, VersionEnum.LivingShip), // 4Manual
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
            Assert.AreEqual(results[i].GameMode, container.GameModeEnum);
            Assert.AreEqual(results[i].Season, container.SeasonEnum);
            Assert.AreEqual(results[i].BaseVersion, container.BaseVersion);
            Assert.AreEqual(results[i].Version, container.VersionEnum);
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
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, SeasonEnum Season, int BaseVersion, VersionEnum Version)[]
        {
            (0, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4138, VersionEnum.Outlaws), // 1Auto
            (1, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4138, VersionEnum.Outlaws), // 1Manual
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
            Assert.AreEqual(results[i].GameMode, container.GameModeEnum);
            Assert.AreEqual(results[i].Season, container.SeasonEnum);
            Assert.AreEqual(results[i].BaseVersion, container.BaseVersion);
            Assert.AreEqual(results[i].Version, container.VersionEnum);
        }
    }

    [TestMethod]
    public void T10_Write_Default()
    {
        var now = DateTimeOffset.UtcNow;
        var nowTicks = now.UtcTicks / TICK_DIVISOR;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "000901F8A36808E0_29070100B936489ABCE8B9AF3980429C");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };
        var userIdentification = ReadUserIdentification(path);
        var writeCallback = false;

        // Act
        var platformA = new PlatformMicrosoft(path, settings);
        var containerA = platformA.GetSaveContainer(0)!;
        var metaA = DecryptMeta(containerA);

        containerA.WriteCallback += () =>
        {
            writeCallback = true;
        };

#pragma warning disable IDE0042 // Deconstruct variable declaration
        platformA.Load(containerA);
        (int Units, long UtcTicks) valuesOrigin = (containerA.GetJsonValue<int>(UNITS_JSON_PATH), containerA.LastWriteTime.UtcTicks);

        containerA.SetJsonValue(UNITS_NEW_AMOUNT, UNITS_JSON_PATH);
        platformA.Write(containerA, now);
        (int Units, long UtcTicks) valuesSet = (containerA.GetJsonValue<int>(UNITS_JSON_PATH), containerA.LastWriteTime.UtcTicks / TICK_DIVISOR);

        var platformB = new PlatformMicrosoft(path, settings);
        var containerB = platformB.GetSaveContainer(0)!;
        var metaB = DecryptMeta(containerB);

        platformB.Load(containerB);
        (int Units, long UtcTicks) valuesReload = (containerB.GetJsonValue<int>(UNITS_JSON_PATH), containerB.LastWriteTime.UtcTicks / TICK_DIVISOR);
#pragma warning restore IDE0042 // Deconstruct variable declaration

        // Assert
        Assert.AreEqual(1504909789, valuesOrigin.Units);
        Assert.AreEqual(638126763444620000, valuesOrigin.UtcTicks); // 2023-02-22 15:25:44 +00:00
        Assert.AreEqual(UNITS_NEW_AMOUNT, valuesSet.Units);
        Assert.AreEqual(nowTicks, valuesSet.UtcTicks);
        Assert.IsTrue(writeCallback);

        Assert.IsTrue(platformB.HasAccountData);
        Assert.AreEqual(10, platformB.GetExistingContainers().Count());
        Assert.AreEqual(userIdentification[0], platformB.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification[1], platformB.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification[2], platformB.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification[3], platformB.PlatformUserIdentification.PTK);

        Assert.AreEqual(UNITS_NEW_AMOUNT, valuesReload.Units);
        Assert.AreEqual(nowTicks, valuesReload.UtcTicks);

        AssertCommonMeta(containerA, metaA, metaB);
        AssertAllAreEqual(4143, (uint)(containerA.BaseVersion), (uint)(containerB.BaseVersion), metaA[0], metaB[0]);
        var bytesA = BitConverter.GetBytes(metaA[1]);
        var bytesB = BitConverter.GetBytes(metaB[1]);
        AssertAllAreEqual((uint)(PresetGameModeEnum.Normal), (uint)(containerA.GameModeEnum!) + 1, (uint)(containerB.GameModeEnum!) + 1, (uint)(BitConverter.ToInt16(bytesA, 0)), (uint)(BitConverter.ToInt16(bytesB, 0)));
        AssertAllAreEqual((uint)(SeasonEnum.None), (uint)(containerA.SeasonEnum), (uint)(containerB.SeasonEnum), BitConverter.ToUInt16(bytesA, 2), BitConverter.ToUInt16(bytesA, 2));
        AssertAllAreEqual(635119, (uint)(containerA.TotalPlayTime), (uint)(containerB.TotalPlayTime), metaA[2], metaB[2]);
    }

    [TestMethod]
    public void T11_Write_Default_Account()
    {
        var now = DateTimeOffset.UtcNow;
        var nowTicks = now.UtcTicks / TICK_DIVISOR;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "00090000025A963A_29070100B936489ABCE8B9AF3980429C");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };
        var userIdentification = ReadUserIdentification(path);
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
        (int MusicVolume, long UtcTicks) valuesOrigin = (containerA.GetJsonValue<int>(MUSICVOLUME_JSON_PATH), containerA.LastWriteTime.UtcTicks);

        containerA.SetJsonValue(MUSICVOLUME_NEW_AMOUNT, MUSICVOLUME_JSON_PATH);
        platformA.Write(containerA, now);
        (int MusicVolume, long UtcTicks) valuesSet = (containerA.GetJsonValue<int>(MUSICVOLUME_JSON_PATH), containerA.LastWriteTime.UtcTicks / TICK_DIVISOR);

        var platformB = new PlatformMicrosoft(path, settings);
        var containerB = platformB.GetAccountContainer()!;
        var metaB = DecryptMeta(containerB);

        platformB.Load(containerB);
        (int MusicVolume, long UtcTicks) valuesReload = (containerB.GetJsonValue<int>(MUSICVOLUME_JSON_PATH), containerB.LastWriteTime.UtcTicks / TICK_DIVISOR);
#pragma warning restore IDE0042 // Deconstruct variable declaration

        // Assert
        Assert.AreEqual(80, valuesOrigin.MusicVolume);
        Assert.AreEqual(638264331709580000, valuesOrigin.UtcTicks); // 2023-07-31 20:46:10 +00:00
        Assert.AreEqual(MUSICVOLUME_NEW_AMOUNT, valuesSet.MusicVolume);
        Assert.AreEqual(nowTicks, valuesSet.UtcTicks);
        Assert.IsTrue(writeCallback);

        Assert.AreEqual(MUSICVOLUME_NEW_AMOUNT, valuesReload.MusicVolume);
        Assert.AreEqual(nowTicks, valuesReload.UtcTicks);

        AssertCommonMeta(containerA, metaA, metaB);
    }

    [TestMethod]
    public void T12_Write_SetLastWriteTime_False()
    {
        var now = DateTimeOffset.UtcNow;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "000901F8A36808E0_29070100B936489ABCE8B9AF3980429C");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            SetLastWriteTime = false,
        };
        var userIdentification = ReadUserIdentification(path);
        var writeCallback = false;

        // Act
        var platform1 = new PlatformMicrosoft(path, settings);
        var container1 = platform1.GetSaveContainer(0)!;
        container1.WriteCallback += () =>
        {
            writeCallback = true;
        };

        platform1.Load(container1);
        var units10 = container1.GetJsonValue<int>(UNITS_JSON_PATH);
        var timestamp10 = container1.LastWriteTime;

        container1.SetJsonValue(UNITS_NEW_AMOUNT, UNITS_JSON_PATH);
        var units11 = container1.GetJsonValue<int>(UNITS_JSON_PATH);

        platform1.Write(container1, now);
        var timestamp11 = container1.LastWriteTime;

        var platform2 = new PlatformMicrosoft(path, settings);
        var container2 = platform2.GetSaveContainer(0)!;

        platform2.Load(container2);
        var units20 = container2.GetJsonValue<int>(UNITS_JSON_PATH);
        var timestamp20 = container2.LastWriteTime;

        // Assert
        Assert.AreEqual(1504909789, units10);
        Assert.AreEqual(UNITS_NEW_AMOUNT, units11);
        Assert.AreEqual(638126763444620000, timestamp10.UtcTicks); // 2023-02-22 15:25:44 +00:00
        Assert.AreEqual(638126763444620000, timestamp11.UtcTicks);
        Assert.IsTrue(writeCallback);

        Assert.IsTrue(platform2.HasAccountData);
        Assert.AreEqual(10, platform2.GetExistingContainers().Count());
        Assert.AreEqual(userIdentification[0], platform2.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification[1], platform2.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification[2], platform2.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification[3], platform2.PlatformUserIdentification.PTK);

        Assert.AreEqual(UNITS_NEW_AMOUNT, units20);
        Assert.AreEqual(638126763444620000, timestamp20.UtcTicks);
    }

    [TestMethod]
    public void T13_Write_WriteAlways_True()
    {
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "000901F8A36808E0_29070100B936489ABCE8B9AF3980429C");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            WriteAlways = true,
        };
        var userIdentification = ReadUserIdentification(path);
        var writeCallback = false;

        // Act
        var platform1 = new PlatformMicrosoft(path, settings);
        var container1 = platform1.GetSaveContainer(0)!;
        container1.WriteCallback += () =>
        {
            writeCallback = true;
        };

        platform1.Load(container1);
        container1.DataFile!.Refresh();
        var length10 = container1.DataFile!.Length;

        platform1.Write(container1);
        container1.DataFile!.Refresh();
        var length11 = container1.DataFile!.Length;

        var platform2 = new PlatformMicrosoft(path, settings);
        var container2 = platform2.GetSaveContainer(0)!;

        platform2.Load(container2);
        container2.DataFile!.Refresh();
        var length20 = container1.DataFile!.Length;

        // Assert
        Assert.IsTrue(writeCallback);

        Assert.IsTrue(platform2.HasAccountData);
        Assert.AreEqual(10, platform2.GetExistingContainers().Count());
        Assert.AreEqual(userIdentification[0], platform2.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification[1], platform2.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification[2], platform2.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification[3], platform2.PlatformUserIdentification.PTK);

        Assert.AreNotEqual(length10, length11);
        Assert.AreNotEqual(length10, length20);

        Assert.AreEqual(length11, length20);
    }

    [TestMethod]
    public void T14_Write_WriteAlways_False()
    {
        var now = DateTimeOffset.UtcNow;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "000901F8A36808E0_29070100B936489ABCE8B9AF3980429C");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            WriteAlways = false,
        };
        var userIdentification = ReadUserIdentification(path);
        var writeCallback = false;

        // Act
        var platform1 = new PlatformMicrosoft(path, settings);
        var container1 = platform1.GetSaveContainer(0)!;
        container1.WriteCallback += () =>
        {
            writeCallback = true;
        };

        platform1.Load(container1);
        container1.DataFile!.Refresh();
        var length10 = container1.DataFile!.Length;

        platform1.Write(container1, now);
        container1.DataFile!.Refresh();
        var length11 = container1.DataFile!.Length;

        var platform2 = new PlatformMicrosoft(path, settings);
        var container2 = platform2.GetSaveContainer(0)!;

        platform2.Load(container2);
        container2.DataFile!.Refresh();
        var length20 = container1.DataFile!.Length;

        // Assert
        Assert.AreEqual(length10, length11);
        Assert.IsTrue(writeCallback);

        Assert.IsTrue(platform2.HasAccountData);
        Assert.AreEqual(10, platform2.GetExistingContainers().Count());
        Assert.AreEqual(userIdentification[0], platform2.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification[1], platform2.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification[2], platform2.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification[3], platform2.PlatformUserIdentification.PTK);

        Assert.AreEqual(length10, length20);
    }

    [TestMethod]
    public void T20_FileSystemWatcher()
    {
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "000901FB44140B02_29070100B936489ABCE8B9AF3980429C");
        var path_containers_index = Path.Combine(path, "containers.index");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            Watcher = true,
        };

        // Act
        var bytes = File.ReadAllBytes(path_containers_index);
        var platform = new PlatformMicrosoft(path, settings);

        var container = platform.GetSaveContainer(0)!;
        platform.Load(container);

        File.WriteAllBytes(path_containers_index, bytes);
        Thread.Sleep(FILESYSTEMWATCHER_SLEEP);
        var watchers1 = platform.GetWatcherContainers();
        var count1 = watchers1.Count();
        var synced1 = container.IsSynced;

        container.SetJsonValue(UNITS_NEW_AMOUNT, UNITS_JSON_PATH);
        var synced2 = container.IsSynced;

        File.WriteAllBytes(path_containers_index, bytes);
        Thread.Sleep(FILESYSTEMWATCHER_SLEEP);
        var watchers2 = platform.GetWatcherContainers();
        var count2 = watchers2.Count();
        var synced3 = container.IsSynced;

        var watcherContainer2 = watchers2.FirstOrDefault();
        Guard.IsNotNull(watcherContainer2);
        platform.OnWatcherDecision(watcherContainer2, false);
        var synced4 = container.IsSynced;

        File.WriteAllBytes(path_containers_index, bytes);
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
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "000901FB44140B02_29070100B936489ABCE8B9AF3980429C");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };

        // Act
        var platform = new PlatformMicrosoft(path, settings);

        var container0 = platform.GetSaveContainer(0)!; // 1Auto
        var container1 = platform.GetSaveContainer(1)!; // 1Manual
        var container2 = platform.GetSaveContainer(2)!; // 2Auto
        var container4 = platform.GetSaveContainer(4)!; // 3Auto
        var container8 = platform.GetSaveContainer(8)!; // 5Auto
        var container9 = platform.GetSaveContainer(9)!; // 5Manual

        platform.Copy(container4, container1); // 3Auto -> 1Manual (overwrite)
        platform.Copy(container2, container8); // 2Auto -> 5Auto (create)
        platform.Copy(container9, container0); // 5Manual -> 1Auto (delete)

        // Assert
        Assert.IsTrue(container1.Exists);
        Assert.AreEqual(container4.GameModeEnum, container1.GameModeEnum);
        Assert.AreEqual(container4.SeasonEnum, container1.SeasonEnum);
        Assert.AreEqual(container4.BaseVersion, container1.BaseVersion);
        Assert.AreEqual(container4.VersionEnum, container1.VersionEnum);

        Assert.IsTrue(container8.Exists);
        Assert.AreEqual(container2.GameModeEnum, container8.GameModeEnum);
        Assert.AreEqual(container2.SeasonEnum, container8.SeasonEnum);
        Assert.AreEqual(container2.BaseVersion, container8.BaseVersion);
        Assert.AreEqual(container2.VersionEnum, container8.VersionEnum);

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

        var container0 = platform.GetSaveContainer(0)!; // 1Auto
        var container1 = platform.GetSaveContainer(1)!; // 1Manual

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

        var container0 = platform.GetSaveContainer(0)!; // 1Auto
        var container1 = platform.GetSaveContainer(1)!; // 1Manual
        var container2 = platform.GetSaveContainer(2)!; // 2Auto
        var container4 = platform.GetSaveContainer(4)!; // 3Auto
        var container5 = platform.GetSaveContainer(5)!; // 3Manual
        var container9 = platform.GetSaveContainer(9)!; // 5Manual

        var gameModeEnum2 = container2.GameModeEnum;
        var seasonEnum2 = container2.SeasonEnum;
        var baseVersion2 = container2.BaseVersion;
        var versionEnum2 = container2.VersionEnum;
        platform.Copy(container4, container5);
        platform.Move(container2, container5); // overwrite

        // 1 is corrupted, therefore 0 gets deleted and then 1 is also deleted after copying.
        platform.Move(container1, container0); // delete

        var gameModeEnum4 = container4.GameModeEnum;
        var seasonEnum4 = container4.SeasonEnum;
        var baseVersion4 = container4.BaseVersion;
        var versionEnum4 = container4.VersionEnum;
        platform.Move(container4, container9); // move

        // Assert
        Assert.IsFalse(container2.Exists);
        Assert.IsTrue(container5.Exists);
        Assert.AreEqual(gameModeEnum2, container5.GameModeEnum);
        Assert.AreEqual(seasonEnum2, container5.SeasonEnum);
        Assert.AreEqual(baseVersion2, container5.BaseVersion);
        Assert.AreEqual(versionEnum2, container5.VersionEnum);

        Assert.IsFalse(container0.Exists);
        Assert.IsFalse(container1.Exists);
        Assert.AreEqual(libNOM.io.Globals.Constants.INCOMPATIBILITY_004, container0.IncompatibilityTag);
        Assert.AreEqual(libNOM.io.Globals.Constants.INCOMPATIBILITY_004, container1.IncompatibilityTag);

        Assert.IsFalse(container4.Exists);
        Assert.IsTrue(container9.Exists);
        Assert.AreEqual(gameModeEnum4, container9.GameModeEnum);
        Assert.AreEqual(seasonEnum4, container9.SeasonEnum);
        Assert.AreEqual(baseVersion4, container9.BaseVersion);
        Assert.AreEqual(versionEnum4, container9.VersionEnum);
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
