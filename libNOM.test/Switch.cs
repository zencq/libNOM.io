using CommunityToolkit.Diagnostics;
using libNOM.io;
using libNOM.io.Enums;
using libNOM.io.Globals;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace libNOM.test;


[TestClass]
[DeploymentItem("..\\..\\..\\Resources\\TESTSUITE_ARCHIVE.zip")]
public class SwitchTest : CommonTestInitializeCleanup
{
    [TestMethod]
    public void T01_Read()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "1");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformSwitch(path, settings);

        // Assert
        Assert.IsFalse(platform.HasAccountData);
        Assert.AreEqual(1, platform.GetExistingContainers().Count());
        Assert.AreEqual(userIdentification[0], platform.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification[1], platform.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification[2], platform.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK);

        var container0 = platform.GetSaveContainer(0)!; // 1Auto
        Assert.IsTrue(container0.Exists);
        Assert.IsFalse(container0.IsOld);
        Assert.AreEqual(PresetGameModeEnum.Creative, container0.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container0.SeasonEnum);
        Assert.AreEqual(4139, container0.BaseVersion);
        Assert.AreEqual(VersionEnum.Endurance, container0.VersionEnum);
    }

    [TestMethod]
    public void T02_Read()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "2");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformSwitch(path, settings);

        // Assert
        Assert.IsTrue(platform.HasAccountData);
        Assert.AreEqual(1, platform.GetExistingContainers().Count());
        Assert.AreEqual(userIdentification[0], platform.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification[1], platform.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification[2], platform.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK);

        var container0 = platform.GetSaveContainer(0)!; // 1Auto
        Assert.IsTrue(container0.Exists);
        Assert.IsFalse(container0.IsOld);
        Assert.AreEqual(PresetGameModeEnum.Normal, container0.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container0.SeasonEnum);
        Assert.AreEqual(4139, container0.BaseVersion);
        Assert.AreEqual(VersionEnum.Endurance, container0.VersionEnum);
    }

    [TestMethod]
    public void T03_Read()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "3");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformSwitch(path, settings);

        // Assert
        Assert.IsTrue(platform.HasAccountData);
        Assert.AreEqual(1, platform.GetExistingContainers().Count());
        Assert.AreEqual(userIdentification[0], platform.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification[1], platform.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification[2], platform.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK);

        var container0 = platform.GetSaveContainer(0)!; // 1Auto
        Assert.IsTrue(container0.Exists);
        Assert.IsFalse(container0.IsOld);
        Assert.AreEqual(PresetGameModeEnum.Normal, container0.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container0.SeasonEnum);
        Assert.AreEqual(4139, container0.BaseVersion);
        Assert.AreEqual(VersionEnum.Endurance, container0.VersionEnum);
    }

    [TestMethod]
    public void T04_Read()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "4");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformSwitch(path, settings);

        // Assert
        Assert.IsTrue(platform.HasAccountData);
        Assert.AreEqual(3, platform.GetExistingContainers().Count());
        Assert.AreEqual(userIdentification[0], platform.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification[1], platform.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification[2], platform.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK);

        var container0 = platform.GetSaveContainer(0)!; // 1Auto
        Assert.IsTrue(container0.Exists);
        Assert.IsFalse(container0.IsOld);
        Assert.AreEqual(PresetGameModeEnum.Normal, container0.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container0.SeasonEnum);
        Assert.AreEqual(4139, container0.BaseVersion);
        Assert.AreEqual(VersionEnum.Endurance, container0.VersionEnum);

        var container2 = platform.GetSaveContainer(2)!; // 2Auto
        Assert.IsTrue(container2.Exists);
        Assert.IsFalse(container2.IsOld);
        Assert.AreEqual(PresetGameModeEnum.Survival, container2.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container2.SeasonEnum);
        Assert.AreEqual(4139, container2.BaseVersion);
        Assert.AreEqual(VersionEnum.Endurance, container2.VersionEnum);

        var container4 = platform.GetSaveContainer(4)!; // 3Auto
        Assert.IsTrue(container4.Exists);
        Assert.IsFalse(container4.IsOld);
        Assert.AreEqual(PresetGameModeEnum.Creative, container4.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container4.SeasonEnum);
        Assert.AreEqual(4139, container4.BaseVersion);
        Assert.AreEqual(VersionEnum.Endurance, container4.VersionEnum);
    }

    [TestMethod]
    public void T10_Write_Default()
    {
        var now = DateTimeOffset.UtcNow;
        var nowUnix = now.ToUnixTimeSeconds();
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "1");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };
        var userIdentification = ReadUserIdentification(path);
        var writeCallback = false;

        // Act
        var platform1 = new PlatformSwitch(path, settings);
        var container1 = platform1.GetSaveContainer(0)!;
        container1.WriteCallback += () =>
        {
            writeCallback = true;
        };

        platform1.Load(container1);
        var units10 = container1.GetJsonValue<int>(UNITS_JSON_PATH);
        var timestamp10 = container1.LastWriteTime.ToUniversalTime().ToUnixTimeSeconds();

        container1.SetJsonValue(UNITS_NEW_AMOUNT, UNITS_JSON_PATH);
        var units11 = container1.GetJsonValue<int>(UNITS_JSON_PATH);

        
        platform1.Write(container1, now);
        var timestamp11 = container1.LastWriteTime.ToUniversalTime().ToUnixTimeSeconds();

        var platform2 = new PlatformSwitch(path, settings);
        var container2 = platform2.GetSaveContainer(0)!;

        platform2.Load(container2);
        var units20 = container2.GetJsonValue<int>(UNITS_JSON_PATH);
        var timestamp20 = container2.LastWriteTime.ToUniversalTime().ToUnixTimeSeconds();

        // Assert
        Assert.AreEqual(0, units10);
        Assert.AreEqual(UNITS_NEW_AMOUNT, units11);
        Assert.AreEqual(1665031423, timestamp10); // 2022-10-06 04:43:43 +00:00
        Assert.AreEqual(nowUnix, timestamp11);
        Assert.IsTrue(writeCallback);

        Assert.IsFalse(platform2.HasAccountData);
        Assert.AreEqual(1, platform2.GetExistingContainers().Count());
        Assert.AreEqual(userIdentification[0], platform2.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification[1], platform2.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification[2], platform2.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification[3], platform2.PlatformUserIdentification.PTK);

        Assert.AreEqual(UNITS_NEW_AMOUNT, units20);
        Assert.AreEqual(nowUnix, timestamp20);
    }

    [TestMethod]
    public void T12_Write_SetLastWriteTime_False()
    {
        var now = DateTimeOffset.UtcNow;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "1");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            SetLastWriteTime = false,
        };
        var userIdentification = ReadUserIdentification(path);
        var writeCallback = false;

        // Act
        var platform1 = new PlatformSwitch(path, settings);
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

        var platform2 = new PlatformSwitch(path, settings);
        var container2 = platform2.GetSaveContainer(0)!;

        platform2.Load(container2);
        var units20 = container2.GetJsonValue<int>(UNITS_JSON_PATH);
        var timestamp20 = container2.LastWriteTime;

        // Assert
        Assert.AreEqual(0, units10); 
        Assert.AreEqual(UNITS_NEW_AMOUNT, units11);
        Assert.AreEqual(638006282230000000, timestamp10.UtcTicks); // 2021-09-04 22:16:24 +00:00
        Assert.AreEqual(638006282230000000, timestamp11.UtcTicks);
        Assert.IsTrue(writeCallback);

        Assert.IsFalse(platform2.HasAccountData);
        Assert.AreEqual(1, platform2.GetExistingContainers().Count());
        Assert.AreEqual(userIdentification[0], platform2.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification[1], platform2.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification[2], platform2.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification[3], platform2.PlatformUserIdentification.PTK);

        Assert.AreEqual(UNITS_NEW_AMOUNT, units20);
        Assert.AreEqual(638006282230000000, timestamp20.UtcTicks);
    }

    [TestMethod]
    public void T13_Write_WriteAlways_True()
    {
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "1");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            WriteAlways = true,
        };
        var userIdentification = ReadUserIdentification(path);
        var writeCallback = false;

        // Act
        var platform1 = new PlatformSwitch(path, settings);
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

        var platform2 = new PlatformSwitch(path, settings);
        var container2 = platform2.GetSaveContainer(0)!;

        platform2.Load(container2);
        container2.DataFile!.Refresh();
        var length20 = container1.DataFile!.Length;

        // Assert
        Assert.IsTrue(writeCallback);

        Assert.IsFalse(platform2.HasAccountData);
        Assert.AreEqual(1, platform2.GetExistingContainers().Count());
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
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "1");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            WriteAlways = false,
        };
        var userIdentification = ReadUserIdentification(path);
        var writeCallback = false;

        // Act
        var platform1 = new PlatformSwitch(path, settings);
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

        var platform2 = new PlatformSwitch(path, settings);
        var container2 = platform2.GetSaveContainer(0)!;

        platform2.Load(container2);
        container2.DataFile!.Refresh();
        var length20 = container1.DataFile!.Length;

        // Assert
        Assert.AreEqual(length10, length11);
        Assert.IsTrue(writeCallback);

        Assert.IsFalse(platform2.HasAccountData);
        Assert.AreEqual(1, platform2.GetExistingContainers().Count());
        Assert.AreEqual(userIdentification[0], platform2.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification[1], platform2.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification[2], platform2.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification[3], platform2.PlatformUserIdentification.PTK);

        Assert.AreEqual(length10, length20);
    }

    [TestMethod]
    public void T20_FileSystemWatcher()
    {
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "1");
        var pathSave = Path.Combine(path, "manifest02.hg");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            Watcher = true,
        };

        // Act
        var bytes = File.ReadAllBytes(pathSave);
        var platform = new PlatformSwitch(path, settings);

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
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "4");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };

        // Act
        var platform = new PlatformSwitch(path, settings);

        var container0 = platform.GetSaveContainer(0)!; // 1Auto
        var container1 = platform.GetSaveContainer(1)!; // 1Manual
        var container2 = platform.GetSaveContainer(2)!; // 2Auto
        var container4 = platform.GetSaveContainer(4)!; // 3Auto
        var container6 = platform.GetSaveContainer(6)!; // 4Auto

        platform.Copy(container0, container2); // 1Auto -> 2Auto (overwrite)
        platform.Copy(container0, container1); // 1Auto -> 1Manual (create)
        platform.Copy(container6, container4); // 4Auto -> 3Auto (delete)

        // Assert
        Assert.IsTrue(container2.Exists);
        Assert.AreEqual(container0.GameModeEnum, container2.GameModeEnum);
        Assert.AreEqual(container0.SeasonEnum, container2.SeasonEnum);
        Assert.AreEqual(container0.BaseVersion, container2.BaseVersion);
        Assert.AreEqual(container0.VersionEnum, container2.VersionEnum);

        Assert.IsTrue(container1.Exists);
        Assert.AreEqual(container0.GameModeEnum, container1.GameModeEnum);
        Assert.AreEqual(container0.SeasonEnum, container1.SeasonEnum);
        Assert.AreEqual(container0.BaseVersion, container1.BaseVersion);
        Assert.AreEqual(container0.VersionEnum, container1.VersionEnum);

        Assert.IsFalse(container4.Exists);
    }

    [TestMethod]
    public void T31_Delete()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "4");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };

        // Act
        var platform = new PlatformSwitch(path, settings);

        var container0 = platform.GetSaveContainer(0)!; // 1Auto

        platform.Delete(container0);

        // Assert
        Assert.IsFalse(container0.Exists);
        Assert.AreEqual(libNOM.io.Globals.Constants.INCOMPATIBILITY_006, container0.IncompatibilityTag);
    }

    [TestMethod]
    public void T32_Move()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "4");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };

        // Act
        var platform = new PlatformSwitch(path, settings);

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
        Assert.AreEqual(libNOM.io.Globals.Constants.INCOMPATIBILITY_006, container0.IncompatibilityTag);
        Assert.AreEqual(libNOM.io.Globals.Constants.INCOMPATIBILITY_006, container1.IncompatibilityTag);

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
