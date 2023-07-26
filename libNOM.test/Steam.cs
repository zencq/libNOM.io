using CommunityToolkit.Diagnostics;
using libNOM.io;
using libNOM.io.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace libNOM.test;


[TestClass]
[DeploymentItem("..\\..\\..\\Resources\\TESTSUITE_ARCHIVE.zip")]
public class SteamTest : CommonTestInitializeCleanup
{
    [TestMethod]
    public void T01_Read_76561198042453834()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198042453834");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformSteam(path, settings);

        // Assert
        Assert.IsTrue(platform.HasAccountData);
        Assert.AreEqual(10, platform.GetExistingContainers().Count());
        Assert.AreEqual(userIdentification[0], platform.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification[1], platform.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification[2], platform.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK);

        var container0 = platform.GetSaveContainer(0)!; // 1Auto
        Assert.IsTrue(container0.Exists);
        Assert.IsFalse(container0.IsOld);
        Assert.AreEqual(PresetGameModeEnum.Unspecified, container0.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container0.SeasonEnum);
        Assert.AreEqual(4145, container0.BaseVersion);
        Assert.AreEqual(VersionEnum.Singularity, container0.VersionEnum);

        var container1 = platform.GetSaveContainer(1)!; // 1Manual
        Assert.IsTrue(container1.Exists);
        Assert.IsFalse(container1.IsOld);
        Assert.AreEqual(PresetGameModeEnum.Unspecified, container1.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container1.SeasonEnum);
        Assert.AreEqual(4145, container1.BaseVersion);
        Assert.AreEqual(VersionEnum.Singularity, container1.VersionEnum);

        var container2 = platform.GetSaveContainer(2)!; // 2Auto
        Assert.IsTrue(container2.Exists);
        Assert.IsFalse(container2.IsOld);
        Assert.AreEqual(PresetGameModeEnum.Unspecified, container2.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container2.SeasonEnum);
        Assert.AreEqual(4142, container2.BaseVersion);
        Assert.AreEqual(VersionEnum.WaypointWithSuperchargedSlots, container2.VersionEnum);

        var container3 = platform.GetSaveContainer(3)!; // 2Manual
        Assert.IsTrue(container3.Exists);
        Assert.IsFalse(container3.IsOld);
        Assert.AreEqual(PresetGameModeEnum.Unspecified, container3.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container3.SeasonEnum);
        Assert.AreEqual(4143, container3.BaseVersion);
        Assert.AreEqual(VersionEnum.Fractal, container3.VersionEnum);

        var container6 = platform.GetSaveContainer(6)!; // 4Auto
        Assert.IsTrue(container6.Exists);
        Assert.IsFalse(container6.IsOld);
        Assert.AreEqual(PresetGameModeEnum.Permadeath, container6.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container6.SeasonEnum);
        Assert.AreEqual(4142, container6.BaseVersion);
        Assert.AreEqual(VersionEnum.WaypointWithSuperchargedSlots, container6.VersionEnum);

        var container7 = platform.GetSaveContainer(7)!; // 4Manual
        Assert.IsTrue(container7.Exists);
        Assert.IsFalse(container7.IsOld);
        Assert.AreEqual(PresetGameModeEnum.Permadeath, container7.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container7.SeasonEnum);
        Assert.AreEqual(4142, container7.BaseVersion);
        Assert.AreEqual(VersionEnum.WaypointWithSuperchargedSlots, container7.VersionEnum);

        var container10 = platform.GetSaveContainer(10)!; // 6Auto
        Assert.IsTrue(container10.Exists);
        Assert.IsFalse(container10.IsOld);
        Assert.AreEqual(PresetGameModeEnum.Normal, container10.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container10.SeasonEnum);
        Assert.AreEqual(4142, container10.BaseVersion);
        Assert.AreEqual(VersionEnum.WaypointWithSuperchargedSlots, container10.VersionEnum);

        var container11 = platform.GetSaveContainer(11)!; // 6Manual
        Assert.IsTrue(container11.Exists);
        Assert.IsFalse(container11.IsOld);
        Assert.AreEqual(PresetGameModeEnum.Normal, container11.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container11.SeasonEnum);
        Assert.AreEqual(4142, container11.BaseVersion);
        Assert.AreEqual(VersionEnum.WaypointWithSuperchargedSlots, container11.VersionEnum);

        var container22 = platform.GetSaveContainer(22)!; // 12Auto
        Assert.IsTrue(container22.Exists);
        Assert.IsFalse(container22.IsOld);
        Assert.AreEqual(PresetGameModeEnum.Creative, container22.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container22.SeasonEnum);
        Assert.AreEqual(4142, container22.BaseVersion);
        Assert.AreEqual(VersionEnum.WaypointWithSuperchargedSlots, container22.VersionEnum);

        var container23 = platform.GetSaveContainer(23)!; // 12Manual
        Assert.IsTrue(container23.Exists);
        Assert.IsFalse(container23.IsOld);
        Assert.AreEqual(PresetGameModeEnum.Creative, container23.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container23.SeasonEnum);
        Assert.AreEqual(4142, container23.BaseVersion);
        Assert.AreEqual(VersionEnum.WaypointWithSuperchargedSlots, container23.VersionEnum);
    }

    [TestMethod]
    public void T02_Read_76561198043217184()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198043217184");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformSteam(path, settings);

        // Assert
        Assert.IsFalse(platform.HasAccountData);
        Assert.AreEqual(10, platform.GetExistingContainers().Count());
        Assert.AreEqual(userIdentification[0], platform.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification[1], platform.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification[2], platform.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK);

        var container0 = platform.GetSaveContainer(0)!; // 1Auto
        Assert.IsTrue(container0.Exists);
        Assert.IsFalse(container0.IsOld);
        Assert.AreEqual(PresetGameModeEnum.Normal, container0.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container0.SeasonEnum);
        Assert.AreEqual(4125, container0.BaseVersion);
        Assert.AreEqual(VersionEnum.ExoMech, container0.VersionEnum);

        var container1 = platform.GetSaveContainer(1)!; // 1Manual
        Assert.IsTrue(container1.Exists);
        Assert.IsFalse(container1.IsOld);
        Assert.AreEqual(PresetGameModeEnum.Normal, container1.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container1.SeasonEnum);
        Assert.AreEqual(4125, container1.BaseVersion);
        Assert.AreEqual(VersionEnum.ExoMech, container1.VersionEnum);

        var container2 = platform.GetSaveContainer(2)!; // 2Auto
        Assert.IsTrue(container2.Exists);
        Assert.IsFalse(container2.IsOld);
        Assert.AreEqual(PresetGameModeEnum.Normal, container2.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container2.SeasonEnum);
        Assert.AreEqual(4126, container2.BaseVersion);
        Assert.AreEqual(VersionEnum.Origins, container2.VersionEnum);

        var container3 = platform.GetSaveContainer(3)!; // 2Manual
        Assert.IsTrue(container3.Exists);
        Assert.IsFalse(container3.IsOld);
        Assert.AreEqual(PresetGameModeEnum.Normal, container3.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container3.SeasonEnum);
        Assert.AreEqual(4126, container3.BaseVersion);
        Assert.AreEqual(VersionEnum.Origins, container3.VersionEnum);

        var container4 = platform.GetSaveContainer(4)!; // 3Auto
        Assert.IsTrue(container4.Exists);
        Assert.IsFalse(container4.IsOld);
        Assert.AreEqual(PresetGameModeEnum.Normal, container4.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container4.SeasonEnum);
        Assert.AreEqual(4126, container4.BaseVersion);
        Assert.AreEqual(VersionEnum.Desolation, container4.VersionEnum);

        var container5 = platform.GetSaveContainer(5)!; // 3Manual
        Assert.IsTrue(container5.Exists);
        Assert.IsFalse(container5.IsOld);
        Assert.AreEqual(PresetGameModeEnum.Normal, container5.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container5.SeasonEnum);
        Assert.AreEqual(4126, container5.BaseVersion);
        Assert.AreEqual(VersionEnum.Desolation, container5.VersionEnum);

        var container6 = platform.GetSaveContainer(6)!; // 4Auto
        Assert.IsTrue(container6.Exists);
        Assert.IsFalse(container6.IsOld);
        Assert.AreEqual(PresetGameModeEnum.Normal, container6.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container6.SeasonEnum);
        Assert.AreEqual(4125, container6.BaseVersion);
        Assert.AreEqual(VersionEnum.ExoMech, container6.VersionEnum);

        var container7 = platform.GetSaveContainer(7)!; // 4Manual
        Assert.IsTrue(container7.Exists);
        Assert.IsFalse(container7.IsOld);
        Assert.AreEqual(PresetGameModeEnum.Normal, container7.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container7.SeasonEnum);
        Assert.AreEqual(4125, container7.BaseVersion);
        Assert.AreEqual(VersionEnum.ExoMech, container7.VersionEnum);

        var container8 = platform.GetSaveContainer(8)!; // 5Auto
        Assert.IsTrue(container8.Exists);
        Assert.IsFalse(container8.IsOld);
        Assert.AreEqual(PresetGameModeEnum.Normal, container8.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container8.SeasonEnum);
        Assert.AreEqual(4125, container8.BaseVersion);
        Assert.AreEqual(VersionEnum.ExoMech, container8.VersionEnum);

        var container9 = platform.GetSaveContainer(9)!; // 5Manual
        Assert.IsTrue(container9.Exists);
        Assert.IsFalse(container9.IsOld);
        Assert.AreEqual(PresetGameModeEnum.Normal, container9.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container9.SeasonEnum);
        Assert.AreEqual(4125, container9.BaseVersion);
        Assert.AreEqual(VersionEnum.ExoMech, container9.VersionEnum);
    }

    [TestMethod]
    public void T03_Read_76561198371877533()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformSteam(path, settings);

        // Assert
        Assert.IsTrue(platform.HasAccountData);
        Assert.AreEqual(5, platform.GetExistingContainers().Count());
        Assert.AreEqual(userIdentification[0], platform.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification[1], platform.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification[2], platform.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK);

        var container0 = platform.GetSaveContainer(0)!; // 1Auto
        Assert.IsTrue(container0.Exists);
        Assert.IsFalse(container0.IsOld);
        Assert.AreEqual(PresetGameModeEnum.Normal, container0.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container0.SeasonEnum);
        Assert.AreEqual(4135, container0.BaseVersion);
        Assert.AreEqual(VersionEnum.Frontiers, container0.VersionEnum);

        var container1 = platform.GetSaveContainer(1)!; // 1Manual
        Assert.IsTrue(container1.Exists);
        Assert.IsFalse(container1.IsOld);
        Assert.AreEqual(PresetGameModeEnum.Normal, container1.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container1.SeasonEnum);
        Assert.AreEqual(4135, container1.BaseVersion);
        Assert.AreEqual(VersionEnum.Frontiers, container1.VersionEnum);

        var container2 = platform.GetSaveContainer(2)!; // 2Auto
        Assert.IsTrue(container2.Exists);
        Assert.IsFalse(container2.IsOld);
        Assert.AreEqual(PresetGameModeEnum.Creative, container2.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container2.SeasonEnum);
        Assert.AreEqual(4127, container2.BaseVersion);
        Assert.AreEqual(VersionEnum.Companions, container2.VersionEnum);

        var container3 = platform.GetSaveContainer(3)!; // 2Manual
        Assert.IsTrue(container3.Exists);
        Assert.IsFalse(container3.IsOld);
        Assert.AreEqual(PresetGameModeEnum.Creative, container3.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container3.SeasonEnum);
        Assert.AreEqual(4127, container3.BaseVersion);
        Assert.AreEqual(VersionEnum.Companions, container3.VersionEnum);

        var container4 = platform.GetSaveContainer(4)!; // 3Auto
        Assert.IsTrue(container4.Exists);
        Assert.IsFalse(container4.IsOld);
        Assert.AreEqual(PresetGameModeEnum.Seasonal, container4.GameModeEnum);
        Assert.AreEqual(SeasonEnum.Pioneers, container4.SeasonEnum);
        Assert.AreEqual(4129, container4.BaseVersion);
        Assert.AreEqual(VersionEnum.Expeditions, container4.VersionEnum);
    }

    /// <summary>
    /// Same as <see cref="T03_Read_76561198371877533"/>.
    /// </summary>
    [TestMethod]
    public void T04_Read_NoAccountInDirectory()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "something");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformSteam(path, settings);

        // Assert
        Assert.IsTrue(platform.HasAccountData);
        Assert.AreEqual(5, platform.GetExistingContainers().Count());
        Assert.AreEqual(userIdentification[0], platform.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification[1], platform.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification[2], platform.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK);

        var container0 = platform.GetSaveContainer(0)!; // 1Auto
        Assert.IsTrue(container0.Exists);
        Assert.IsFalse(container0.IsOld);
        Assert.AreEqual(PresetGameModeEnum.Normal, container0.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container0.SeasonEnum);
        Assert.AreEqual(4135, container0.BaseVersion);
        Assert.AreEqual(VersionEnum.Frontiers, container0.VersionEnum);

        var container1 = platform.GetSaveContainer(1)!; // 1Manual
        Assert.IsTrue(container1.Exists);
        Assert.IsFalse(container1.IsOld);
        Assert.AreEqual(PresetGameModeEnum.Normal, container1.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container1.SeasonEnum);
        Assert.AreEqual(4135, container1.BaseVersion);
        Assert.AreEqual(VersionEnum.Frontiers, container1.VersionEnum);

        var container2 = platform.GetSaveContainer(2)!; // 2Auto
        Assert.IsTrue(container2.Exists);
        Assert.IsFalse(container2.IsOld);
        Assert.AreEqual(PresetGameModeEnum.Creative, container2.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container2.SeasonEnum);
        Assert.AreEqual(4127, container2.BaseVersion);
        Assert.AreEqual(VersionEnum.Companions, container2.VersionEnum);

        var container3 = platform.GetSaveContainer(3)!; // 2Manual
        Assert.IsTrue(container3.Exists);
        Assert.IsFalse(container3.IsOld);
        Assert.AreEqual(PresetGameModeEnum.Creative, container3.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container3.SeasonEnum);
        Assert.AreEqual(4127, container3.BaseVersion);
        Assert.AreEqual(VersionEnum.Companions, container3.VersionEnum);

        var container4 = platform.GetSaveContainer(4)!; // 3Auto
        Assert.IsTrue(container4.Exists);
        Assert.IsFalse(container4.IsOld);
        Assert.AreEqual(PresetGameModeEnum.Seasonal, container4.GameModeEnum);
        Assert.AreEqual(SeasonEnum.Pioneers, container4.SeasonEnum);
        Assert.AreEqual(4129, container4.BaseVersion);
        Assert.AreEqual(VersionEnum.Expeditions, container4.VersionEnum);
    }

    [TestMethod]
    public void T10_Write_Default_2001()
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
        var platform1 = new PlatformSteam(path, settings);
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

        var platform2 = new PlatformSteam(path, settings);
        var container2 = platform2.GetSaveContainer(0)!;

        platform2.Load(container2);
        var units20 = container2.GetJsonValue<int>(UNITS_JSON_PATH);
        var timestamp20 = container2.LastWriteTime;

        // Assert
        Assert.AreEqual(-123571, units10); // 4294843725
        Assert.AreEqual(UNITS_NEW_AMOUNT, units11);
        Assert.AreEqual(637376113621684301, timestamp10.UtcTicks); // 2020-10-06 20:02:42 +00:00
        Assert.AreEqual(now.UtcTicks, timestamp11.UtcTicks);
        Assert.IsTrue(writeCallback);

        Assert.IsFalse(platform2.HasAccountData);
        Assert.AreEqual(10, platform2.GetExistingContainers().Count());
        Assert.AreEqual(userIdentification[0], platform2.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification[1], platform2.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification[2], platform2.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification[3], platform2.PlatformUserIdentification.PTK);

        Assert.AreEqual(UNITS_NEW_AMOUNT, units20);
        Assert.AreEqual(now.UtcTicks, timestamp20.UtcTicks);
    }

    [TestMethod]
    public void T11_Write_Default_2002()
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
        var platform1 = new PlatformSteam(path, settings);
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

        var platform2 = new PlatformSteam(path, settings);
        var container2 = platform2.GetSaveContainer(0)!;

        platform2.Load(container2);
        var units20 = container2.GetJsonValue<int>(UNITS_JSON_PATH);
        var timestamp20 = container2.LastWriteTime;

        // Assert
        Assert.AreEqual(-1221111157, units10); // 3073856139
        Assert.AreEqual(UNITS_NEW_AMOUNT, units11);
        Assert.AreEqual(637663905840000000, timestamp10.UtcTicks); // 2021-09-04 22:16:24 +00:00
        Assert.AreEqual(now.UtcTicks, timestamp11.UtcTicks);
        Assert.IsTrue(writeCallback);

        Assert.IsTrue(platform2.HasAccountData);
        Assert.AreEqual(5, platform2.GetExistingContainers().Count());
        Assert.AreEqual(userIdentification[0], platform2.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification[1], platform2.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification[2], platform2.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification[3], platform2.PlatformUserIdentification.PTK);

        Assert.AreEqual(UNITS_NEW_AMOUNT, units20);
        Assert.AreEqual(now.UtcTicks, timestamp20.UtcTicks);
    }

    [TestMethod]
    public void T12_Write_SetLastWriteTime_False()
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
        var platform1 = new PlatformSteam(path, settings);
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

        var platform2 = new PlatformSteam(path, settings);
        var container2 = platform2.GetSaveContainer(0)!;

        platform2.Load(container2);
        var units20 = container2.GetJsonValue<int>(UNITS_JSON_PATH);
        var timestamp20 = container2.LastWriteTime;

        // Assert
        Assert.AreEqual(-1221111157, units10); // 3073856139
        Assert.AreEqual(UNITS_NEW_AMOUNT, units11);
        Assert.AreEqual(637663905840000000, timestamp10.UtcTicks); // 2021-09-04 22:16:24 +00:00
        Assert.AreEqual(637663905840000000, timestamp11.UtcTicks);
        Assert.IsTrue(writeCallback);

        Assert.IsTrue(platform2.HasAccountData);
        Assert.AreEqual(5, platform2.GetExistingContainers().Count());
        Assert.AreEqual(userIdentification[0], platform2.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification[1], platform2.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification[2], platform2.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification[3], platform2.PlatformUserIdentification.PTK);

        Assert.AreEqual(UNITS_NEW_AMOUNT, units20);
        Assert.AreEqual(637663905840000000, timestamp20.UtcTicks);
    }

    [TestMethod]
    public void T13_Write_WriteAlways_True()
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
        var platform1 = new PlatformSteam(path, settings);
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

        var platform2 = new PlatformSteam(path, settings);
        var container2 = platform2.GetSaveContainer(0)!;

        platform2.Load(container2);
        container2.DataFile!.Refresh();
        var length20 = container1.DataFile!.Length;

        // Assert
        Assert.IsTrue(writeCallback);

        Assert.IsFalse(platform2.HasAccountData);
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
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198043217184");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            WriteAlways = false,
        };
        var userIdentification = ReadUserIdentification(path);
        var writeCallback = false;

        // Act
        var platform1 = new PlatformSteam(path, settings);
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

        var platform2 = new PlatformSteam(path, settings);
        var container2 = platform2.GetSaveContainer(0)!;

        platform2.Load(container2);
        container2.DataFile!.Refresh();
        var length20 = container1.DataFile!.Length;

        // Assert
        Assert.AreEqual(length10, length11);
        Assert.IsTrue(writeCallback);

        Assert.IsFalse(platform2.HasAccountData);
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
        Assert.AreEqual(container0.VersionEnum, container2.VersionEnum);

        Assert.IsTrue(container7.Exists);
        Assert.AreEqual(container3.GameModeEnum, container7.GameModeEnum);
        Assert.AreEqual(container3.SeasonEnum, container7.SeasonEnum);
        Assert.AreEqual(container3.BaseVersion, container7.BaseVersion);
        Assert.AreEqual(container3.VersionEnum, container7.VersionEnum);

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
        var versionEnum4 = container4.VersionEnum;
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
