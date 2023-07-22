using CommunityToolkit.Diagnostics;
using libNOM.io;
using libNOM.io.Enums;
using libNOM.io.Globals;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace libNOM.test;


[TestClass]
[DeploymentItem("..\\..\\..\\Resources\\TESTSUITE_ARCHIVE.zip")]
public class MicrosoftTest : CommonTestInitializeCleanup
{
    [TestMethod]
    public void T01_Read_0009000000C73498()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "0009000000C73498_29070100B936489ABCE8B9AF3980429C");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformMicrosoft(path, settings);

        // Assert
        Assert.IsTrue(platform.HasAccountData);
        Assert.AreEqual(2, platform.GetExistingContainers().Count());
        Assert.AreEqual(userIdentification[0], platform.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification[1], platform.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification[2], platform.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK);

        var container2 = platform.GetSaveContainer(2)!; // 2Auto
        Assert.IsTrue(container2.Exists);
        Assert.AreEqual(PresetGameModeEnum.Normal, container2.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container2.SeasonEnum);
        Assert.AreEqual(4135, container2.BaseVersion);
        Assert.AreEqual(VersionEnum.Frontiers, container2.VersionEnum);

        var container3 = platform.GetSaveContainer(3)!; // 2Manual
        Assert.IsTrue(container3.Exists);
        Assert.AreEqual(PresetGameModeEnum.Normal, container3.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container3.SeasonEnum);
        Assert.AreEqual(4135, container3.BaseVersion);
        Assert.AreEqual(VersionEnum.Frontiers, container3.VersionEnum);
    }

    [TestMethod]
    public void T02_Read_000901F4E735CFAC()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "000901F4E735CFAC_29070100B936489ABCE8B9AF3980429C");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformMicrosoft(path, settings);

        // Assert
        Assert.IsTrue(platform.HasAccountData);
        Assert.AreEqual(6, platform.GetExistingContainers().Count());
        Assert.AreEqual(userIdentification[0], platform.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification[1], platform.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification[2], platform.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK);

        var container0 = platform.GetSaveContainer(0)!; // 1Auto
        Assert.IsTrue(container0.Exists);
        Assert.AreEqual(PresetGameModeEnum.Normal, container0.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container0.SeasonEnum);
        Assert.AreEqual(4138, container0.BaseVersion);
        Assert.AreEqual(VersionEnum.Outlaws, container0.VersionEnum);

        var container1 = platform.GetSaveContainer(1)!; // 1Manual
        Assert.IsTrue(container1.Exists);
        Assert.AreEqual(PresetGameModeEnum.Normal, container1.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container1.SeasonEnum);
        Assert.AreEqual(4138, container1.BaseVersion);
        Assert.AreEqual(VersionEnum.Outlaws, container1.VersionEnum);

        var container2 = platform.GetSaveContainer(2)!; // 2Auto
        Assert.IsTrue(container2.Exists);
        Assert.AreEqual(PresetGameModeEnum.Seasonal, container2.GameModeEnum);
        Assert.AreEqual(SeasonEnum.Exobiology, container2.SeasonEnum);
        Assert.AreEqual(4137, container2.BaseVersion);
        Assert.AreEqual(VersionEnum.SentinelWithVehicleAI, container2.VersionEnum);

        var container3 = platform.GetSaveContainer(3)!; // 2Manual
        Assert.IsTrue(container3.Exists);
        Assert.AreEqual(PresetGameModeEnum.Seasonal, container3.GameModeEnum);
        Assert.AreEqual(SeasonEnum.Exobiology, container3.SeasonEnum);
        Assert.AreEqual(4137, container3.BaseVersion);
        Assert.AreEqual(VersionEnum.SentinelWithVehicleAI, container3.VersionEnum);

        var container4 = platform.GetSaveContainer(4)!; // 3Auto
        Assert.IsTrue(container4.Exists);
        Assert.AreEqual(PresetGameModeEnum.Seasonal, container4.GameModeEnum);
        Assert.AreEqual(SeasonEnum.Blighted, container4.SeasonEnum);
        Assert.AreEqual(4138, container4.BaseVersion);
        Assert.AreEqual(VersionEnum.Outlaws, container4.VersionEnum);

        var container5 = platform.GetSaveContainer(5)!; // 3Manual
        Assert.IsTrue(container5.Exists);
        Assert.AreEqual(PresetGameModeEnum.Seasonal, container5.GameModeEnum);
        Assert.AreEqual(SeasonEnum.Blighted, container5.SeasonEnum);
        Assert.AreEqual(4138, container5.BaseVersion);
        Assert.AreEqual(VersionEnum.Outlaws, container5.VersionEnum);
    }

    [TestMethod]
    public void T03_Read_000901F8A36808E0()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "000901F8A36808E0_29070100B936489ABCE8B9AF3980429C");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformMicrosoft(path, settings);

        // Assert
        Assert.IsTrue(platform.HasAccountData);
        Assert.AreEqual(10, platform.GetExistingContainers().Count());
        Assert.AreEqual(userIdentification[0], platform.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification[1], platform.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification[2], platform.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK);

        var container0 = platform.GetSaveContainer(0)!; // 1Auto
        Assert.IsTrue(container0.Exists);
        Assert.AreEqual(PresetGameModeEnum.Unspecified, container0.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container0.SeasonEnum);
        Assert.AreEqual(4143, container0.BaseVersion);
        Assert.AreEqual(VersionEnum.Fractal, container0.VersionEnum);

        var container1 = platform.GetSaveContainer(1)!; // 1Manual
        Assert.IsTrue(container1.Exists);
        Assert.AreEqual(PresetGameModeEnum.Unspecified, container1.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container1.SeasonEnum);
        Assert.AreEqual(4143, container1.BaseVersion);
        Assert.AreEqual(VersionEnum.Fractal, container1.VersionEnum);

        var container2 = platform.GetSaveContainer(2)!; // 2Auto
        Assert.IsTrue(container2.Exists);
        Assert.AreEqual(PresetGameModeEnum.Survival, container2.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container2.SeasonEnum);
        Assert.AreEqual(4142, container2.BaseVersion);
        Assert.AreEqual(VersionEnum.WaypointWithSuperchargedSlots, container2.VersionEnum);

        var container3 = platform.GetSaveContainer(3)!; // 2Manual
        Assert.IsTrue(container3.Exists);
        Assert.AreEqual(PresetGameModeEnum.Survival, container3.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container3.SeasonEnum);
        Assert.AreEqual(4142, container3.BaseVersion);
        Assert.AreEqual(VersionEnum.WaypointWithSuperchargedSlots, container3.VersionEnum);

        var container4 = platform.GetSaveContainer(4)!; // 3Auto
        Assert.IsTrue(container4.Exists);
        Assert.AreEqual(PresetGameModeEnum.Permadeath, container4.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container4.SeasonEnum);
        Assert.AreEqual(4142, container4.BaseVersion);
        Assert.AreEqual(VersionEnum.WaypointWithSuperchargedSlots, container4.VersionEnum);

        var container5 = platform.GetSaveContainer(5)!; // 3Manual
        Assert.IsTrue(container5.Exists);
        Assert.AreEqual(PresetGameModeEnum.Permadeath, container5.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container5.SeasonEnum);
        Assert.AreEqual(4142, container5.BaseVersion);
        Assert.AreEqual(VersionEnum.WaypointWithSuperchargedSlots, container5.VersionEnum);

        var container6 = platform.GetSaveContainer(6)!; // 4Auto
        Assert.IsTrue(container6.Exists);
        Assert.AreEqual(PresetGameModeEnum.Unspecified, container6.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container6.SeasonEnum);
        Assert.AreEqual(4143, container6.BaseVersion);
        Assert.AreEqual(VersionEnum.Fractal, container6.VersionEnum);

        var container7 = platform.GetSaveContainer(7)!; // 4Manual
        Assert.IsTrue(container7.Exists);
        Assert.AreEqual(PresetGameModeEnum.Unspecified, container7.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container7.SeasonEnum);
        Assert.AreEqual(4143, container7.BaseVersion);
        Assert.AreEqual(VersionEnum.Fractal, container7.VersionEnum);

        var container8 = platform.GetSaveContainer(8)!; // 5Auto
        Assert.IsTrue(container8.Exists);
        Assert.AreEqual(PresetGameModeEnum.Seasonal, container8.GameModeEnum);
        Assert.AreEqual(SeasonEnum.Utopia, container8.SeasonEnum);
        Assert.AreEqual(4143, container8.BaseVersion);
        Assert.AreEqual(VersionEnum.Fractal, container8.VersionEnum);

        var container9 = platform.GetSaveContainer(9)!; // 5Manual
        Assert.IsTrue(container9.Exists);
        Assert.AreEqual(PresetGameModeEnum.Normal, container9.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container9.SeasonEnum);
        Assert.AreEqual(4143, container9.BaseVersion);
        Assert.AreEqual(VersionEnum.Fractal, container9.VersionEnum);
    }

    [TestMethod]
    public void T04_Read_000901FB44140B02()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "000901FB44140B02_29070100B936489ABCE8B9AF3980429C");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformMicrosoft(path, settings);

        // Assert
        Assert.IsFalse(platform.HasAccountData);
        Assert.AreEqual(3, platform.GetExistingContainers().Count());
        Assert.AreEqual(userIdentification[0], platform.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification[1], platform.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification[2], platform.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK);

        var container0 = platform.GetSaveContainer(0)!; // 1Auto
        Assert.IsTrue(container0.Exists);
        Assert.AreEqual(PresetGameModeEnum.Normal, container0.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container0.SeasonEnum);
        Assert.AreEqual(4134, container0.BaseVersion);
        Assert.AreEqual(VersionEnum.PrismsWithBytebeatAuthor, container0.VersionEnum);

        var container1 = platform.GetSaveContainer(1)!; // 1Manual
        Assert.IsFalse(container1.Exists);
        Assert.AreEqual(Constants.INCOMPATIBILITY_005, container1.IncompatibilityTag);

        var container2 = platform.GetSaveContainer(2)!; // 2Auto
        Assert.IsTrue(container2.Exists);
        Assert.AreEqual(PresetGameModeEnum.Permadeath, container2.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container2.SeasonEnum);
        Assert.AreEqual(4127, container2.BaseVersion);
        Assert.AreEqual(VersionEnum.Companions, container2.VersionEnum);

        var container4 = platform.GetSaveContainer(4)!; // 3Auto
        Assert.IsTrue(container4.Exists);
        Assert.AreEqual(PresetGameModeEnum.Survival, container4.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container4.SeasonEnum);
        Assert.AreEqual(4133, container4.BaseVersion);
        Assert.AreEqual(VersionEnum.Beachhead, container4.VersionEnum);
    }

    [TestMethod]
    public void T05_Read_000901FE2C5492FC()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "000901FE2C5492FC_29070100B936489ABCE8B9AF3980429C");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformMicrosoft(path, settings);

        // Assert
        Assert.IsFalse(platform.HasAccountData);
        Assert.AreEqual(1, platform.GetExistingContainers().Count());
        Assert.AreEqual(userIdentification[0], platform.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification[1], platform.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification[2], platform.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK);

        var container0 = platform.GetSaveContainer(0)!; // 1Auto
        Assert.IsFalse(container0.Exists);
        Assert.AreEqual(libNOM.io.Globals.Constants.INCOMPATIBILITY_004, container0.IncompatibilityTag);

        var container1 = platform.GetSaveContainer(1)!; // 1Manual
        Assert.IsTrue(container1.Exists);
        Assert.AreEqual(PresetGameModeEnum.Normal, container1.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container1.SeasonEnum);
        Assert.AreEqual(4135, container1.BaseVersion);
        Assert.AreEqual(VersionEnum.Emergence, container1.VersionEnum);
    }

    [TestMethod]
    public void T06_Read_000901FFCAB85905()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "000901FFCAB85905_29070100B936489ABCE8B9AF3980429C");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformMicrosoft(path, settings);

        // Assert
        Assert.IsFalse(platform.HasAccountData);
        Assert.AreEqual(2, platform.GetExistingContainers().Count());
        Assert.AreEqual(userIdentification[0], platform.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification[1], platform.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification[2], platform.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK);

        var container0 = platform.GetSaveContainer(0)!; // 1Auto
        Assert.IsTrue(container0.Exists);
        Assert.AreEqual(PresetGameModeEnum.Normal, container0.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container0.SeasonEnum);
        Assert.AreEqual(4138, container0.BaseVersion);
        Assert.AreEqual(VersionEnum.Outlaws, container0.VersionEnum);

        var container1 = platform.GetSaveContainer(1)!; // 1Manual
        Assert.IsTrue(container1.Exists);
        Assert.AreEqual(PresetGameModeEnum.Normal, container1.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container1.SeasonEnum);
        Assert.AreEqual(4138, container1.BaseVersion);
        Assert.AreEqual(VersionEnum.Outlaws, container1.VersionEnum);
    }

    /// <summary>
    /// Same as <see cref="T06_Read_000901FFCAB85905"/>.
    /// </summary>
    [TestMethod]
    public void T07_Read_NoAccountInDirectory()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "something");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformMicrosoft(path, settings);

        // Assert
        Assert.IsFalse(platform.HasAccountData);
        Assert.AreEqual(2, platform.GetExistingContainers().Count());
        Assert.AreEqual(userIdentification[0], platform.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification[1], platform.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification[2], platform.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK);

        var container0 = platform.GetSaveContainer(0)!; // 1Auto
        Assert.IsTrue(container0.Exists);
        Assert.AreEqual(PresetGameModeEnum.Normal, container0.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container0.SeasonEnum);
        Assert.AreEqual(4138, container0.BaseVersion);
        Assert.AreEqual(VersionEnum.Outlaws, container0.VersionEnum);

        var container1 = platform.GetSaveContainer(1)!; // 1Manual
        Assert.IsTrue(container1.Exists);
        Assert.AreEqual(PresetGameModeEnum.Normal, container1.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container1.SeasonEnum);
        Assert.AreEqual(4138, container1.BaseVersion);
        Assert.AreEqual(VersionEnum.Outlaws, container1.VersionEnum);
    }

    [TestMethod]
    public void T10_Write_Default()
    {
        var now = DateTimeOffset.UtcNow;
        var now_ticks = now.UtcTicks / TICK_DIVISOR;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "000901F8A36808E0_29070100B936489ABCE8B9AF3980429C");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
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
        Assert.AreEqual(now_ticks, timestamp11.UtcTicks / TICK_DIVISOR);
        Assert.IsTrue(writeCallback);

        Assert.IsTrue(platform2.HasAccountData);
        Assert.AreEqual(10, platform2.GetExistingContainers().Count());
        Assert.AreEqual(userIdentification[0], platform2.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification[1], platform2.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification[2], platform2.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification[3], platform2.PlatformUserIdentification.PTK);

        Assert.AreEqual(UNITS_NEW_AMOUNT, units20);
        Assert.AreEqual(now_ticks, timestamp20.UtcTicks / TICK_DIVISOR);
    }

    [TestMethod]
    public void T11_Write_SetLastWriteTime_False()
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
    public void T12_Write_WriteAlways_True()
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
    public void T13_Write_WriteAlways_False()
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
        var container4 = platform.GetSaveContainer(4)!; // 3Auto
        var container9 = platform.GetSaveContainer(9)!; // 5Manual

        // 1 is corrupted, therefore 0 gets deleted and then 1 is also deleted after copying.
        platform.Move(container1, container0); // delete

        var gameModeEnum4 = container4.GameModeEnum;
        var seasonEnum4 = container4.SeasonEnum;
        var baseVersion4 = container4.BaseVersion;
        var versionEnum4 = container4.VersionEnum;
        platform.Move(container4, container9); // move

        // Assert
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
