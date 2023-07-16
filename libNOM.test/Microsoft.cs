using libNOM.io;
using libNOM.io.Enums;
using libNOM.io.Globals;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace libNOM.test;


[TestClass]
[DeploymentItem("..\\..\\..\\Resources\\TESTSUITE_ARCHIVE.zip")]
public class MicrosoftTest : CommonTestInitializeCleanup
{
    private const int TICK_DIVISOR = 10000;
    private const string UNITS_JSON_PATH = "PlayerStateData.Units";
    private const int UNITS_NEW_AMOUNT = 29070100;

    [TestMethod]
    public void Read_0009000000C73498()
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
    public void Read_000901F4E735CFAC()
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
    public void Read_000901F8A36808E0()
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
    public void Read_000901FB44140B02()
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
    public void Read_000901FE2C5492FC()
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
    public void Read_000901FFCAB85905()
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

    [TestMethod]
    public void Read_NoAccountInDirectory()
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
    public void Write_Default()
    {
        var now = DateTimeOffset.UtcNow;
        var now_ticks = now.UtcTicks / TICK_DIVISOR;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "000901F8A36808E0_29070100B936489ABCE8B9AF3980429C");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform1 = new PlatformMicrosoft(path, settings);
        var container1 = platform1.GetSaveContainer(0)!;

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
    public void Write_SetLastWriteTime_False()
    {
        var now = DateTimeOffset.UtcNow;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "000901F8A36808E0_29070100B936489ABCE8B9AF3980429C");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            SetLastWriteTime = false,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform1 = new PlatformMicrosoft(path, settings);
        var container1 = platform1.GetSaveContainer(0)!;

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
    public void Write_WriteAlways_True()
    {
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "000901F8A36808E0_29070100B936489ABCE8B9AF3980429C");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            SetLastWriteTime = false, // otherwise would set SetLastWriteTime anyway and test would not be possible
            WriteAlways = true,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform1 = new PlatformMicrosoft(path, settings);
        var container1 = platform1.GetSaveContainer(0)!;

        platform1.Load(container1);
        container1.DataFile!.Refresh();
        var timestamp10 = container1.DataFile!.LastWriteTime.ToUniversalTime().Ticks / TICK_DIVISOR;

        platform1.Write(container1);
        container1.DataFile!.Refresh();
        var timestamp11 = container1.DataFile!.LastWriteTime.ToUniversalTime().Ticks / TICK_DIVISOR;

        var platform2 = new PlatformMicrosoft(path, settings);
        var container2 = platform2.GetSaveContainer(0)!;

        platform2.Load(container2);
        container2.DataFile!.Refresh();
        var timestamp20 = container2.DataFile!.LastWriteTime.ToUniversalTime().Ticks / TICK_DIVISOR;

        // Assert
        Assert.IsTrue(platform2.HasAccountData);
        Assert.AreEqual(10, platform2.GetExistingContainers().Count());
        Assert.AreEqual(userIdentification[0], platform2.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification[1], platform2.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification[2], platform2.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification[3], platform2.PlatformUserIdentification.PTK);

        Assert.AreNotEqual(timestamp10, timestamp11);
        Assert.AreNotEqual(timestamp10, timestamp20);

        Assert.AreEqual(timestamp11, timestamp20);
    }

    [TestMethod]
    public void Write_WriteAlways_False()
    {
        var now = DateTimeOffset.UtcNow;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "000901F8A36808E0_29070100B936489ABCE8B9AF3980429C");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            SetLastWriteTime = false, // otherwise would set SetLastWriteTime anyway and test would not be possible
            WriteAlways = false,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform1 = new PlatformMicrosoft(path, settings);
        var container1 = platform1.GetSaveContainer(0)!;

        platform1.Load(container1);
        container1.DataFile!.Refresh();
        var timestamp10 = container1.DataFile!.LastWriteTime.ToUniversalTime().Ticks;

        platform1.Write(container1, now);
        container1.DataFile!.Refresh();
        var timestamp11 = container1.DataFile!.LastWriteTime.ToUniversalTime().Ticks;

        var platform2 = new PlatformMicrosoft(path, settings);
        var container2 = platform2.GetSaveContainer(0)!;

        platform2.Load(container2);
        container2.DataFile!.Refresh();
        var timestamp20 = container2.DataFile!.LastWriteTime.ToUniversalTime().Ticks;

        // Assert

        Assert.AreEqual(timestamp10, timestamp11);

        Assert.IsTrue(platform2.HasAccountData);
        Assert.AreEqual(10, platform2.GetExistingContainers().Count());
        Assert.AreEqual(userIdentification[0], platform2.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification[1], platform2.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification[2], platform2.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification[3], platform2.PlatformUserIdentification.PTK);

        Assert.AreEqual(timestamp10, timestamp20);
    }

    [TestMethod]
    public void Copy()
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

        platform.Copy(container4, container1); // 3Auto -> 1Manual
        platform.Copy(container2, container8); // 2Auto -> 5Auto
        platform.Copy(container9, container0); // 5Manual -> 1Auto

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
    public void Delete()
    {
        // Arrange
        // Act
        // Assert
    }

    [TestMethod]
    public void Move()
    {
        // Arrange
        // Act
        // Assert
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

    [TestMethod]
    public void FileSystemWatcher()
    {
        // Arrange
        // Act

        // ... Read/Write/React

        // Assert
    }
}
