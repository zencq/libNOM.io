using CommunityToolkit.Diagnostics;
using libNOM.io;
using libNOM.io.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace libNOM.test;


// Do not use System.Range for simplicity of the file and performace is not critial.
[TestClass]
[DeploymentItem("..\\..\\..\\Resources\\TESTSUITE_ARCHIVE.zip")]
public class SwitchTest : CommonTestInitializeCleanup
{
    #region Constant

    protected const uint META_HEADER = 0xCA55E77E;
    protected const int META_LENGTH_TOTAL_VANILLA = 0x64 / sizeof(uint); // 25
    protected const int META_LENGTH_TOTAL_WAYPOINT = 0x164 / sizeof(uint); // 89

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

        if (metaA.Length == META_LENGTH_TOTAL_VANILLA || metaA.Length == META_LENGTH_TOTAL_WAYPOINT)
        {
            AssertAllAreEqual(META_HEADER, metaA[0], metaB[0]);
            AssertAllAreEqual(SAVE_FORMAT_3, metaA[1], metaB[1]);

            Assert.IsTrue(metaA.Skip(32).SequenceEqual(metaB.Skip(32)));
        }
        else
            throw new AssertFailedException();
    }

    #endregion

    [TestMethod]
    public void T01_Read()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "1");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameModeEnum, SeasonEnum SeasonEnum, int BaseVersion, GameVersionEnum GameVersionEnum)[]
        {
            (0, true, false, PresetGameModeEnum.Creative, SeasonEnum.None, 4139, GameVersionEnum.Endurance), // 1Auto
        };
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformSwitch(path, settings);

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
    public void T02_Read()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "2");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameModeEnum, SeasonEnum SeasonEnum, int BaseVersion, GameVersionEnum GameVersionEnum)[]
        {
            (0, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4139, GameVersionEnum.Endurance), // 1Auto
        };
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformSwitch(path, settings);

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
    public void T03_Read()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "3");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameModeEnum, SeasonEnum SeasonEnum, int BaseVersion, GameVersionEnum GameVersionEnum)[]
        {
            (0, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4139, GameVersionEnum.Endurance), // 1Auto
        };
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformSwitch(path, settings);

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
    public void T04_Read()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "4");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameModeEnum, SeasonEnum SeasonEnum, int BaseVersion, GameVersionEnum GameVersionEnum)[]
        {
            (0, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4139, GameVersionEnum.Endurance), // 1Auto
            (2, true, false, PresetGameModeEnum.Survival, SeasonEnum.None, 4139, GameVersionEnum.Endurance), // 2Auto
            (4, true, false, PresetGameModeEnum.Creative, SeasonEnum.None, 4139, GameVersionEnum.Endurance), // 3Auto
        };
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformSwitch(path, settings);

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
    public void T10_Write_Default_0x7D2_Frontiers()
    {
        var now = DateTimeOffset.UtcNow;
        var nowUnix = now.ToUnixTimeSeconds();
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "4");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };
        var writeCallback = false;

        // Act
        var platformA = new PlatformSwitch(path, settings);
        var containerA = platformA.GetSaveContainer(4)!;
        var metaA = DecryptMeta(containerA);

        containerA.WriteCallback += () =>
        {
            writeCallback = true;
        };

#pragma warning disable IDE0042 // Deconstruct variable declaration
        platformA.Load(containerA);
        (int Units, long UnixSeconds) valuesOrigin = (containerA.GetJsonValue<int>(UNITS_JSON_PATH), containerA.LastWriteTime!.Value.ToUniversalTime().ToUnixTimeSeconds());

        containerA.SetJsonValue(UNITS_NEW_AMOUNT, UNITS_JSON_PATH);
        platformA.Write(containerA, now);
        (int Units, long UnixSeconds) valuesSet = (containerA.GetJsonValue<int>(UNITS_JSON_PATH), containerA.LastWriteTime!.Value.ToUniversalTime().ToUnixTimeSeconds());

        var platformB = new PlatformSwitch(path, settings);
        var containerB = platformB.GetSaveContainer(4)!;
        var metaB = DecryptMeta(containerB);

        platformB.Load(containerB);
        (int Units, long UnixSeconds) valuesReload = (containerB.GetJsonValue<int>(UNITS_JSON_PATH), containerB.LastWriteTime!.Value.ToUniversalTime().ToUnixTimeSeconds());
#pragma warning restore IDE0042 // Deconstruct variable declaration

        // Assert
        Assert.IsTrue(writeCallback);

        Assert.AreEqual(0, valuesOrigin.Units);
        Assert.AreEqual(1665085773, valuesOrigin.UnixSeconds); // 2022-10-06 19:49:33 +00:00
        Assert.AreEqual(UNITS_NEW_AMOUNT, valuesSet.Units);
        Assert.AreEqual(nowUnix, valuesSet.UnixSeconds);

        Assert.AreEqual(UNITS_NEW_AMOUNT, valuesReload.Units);
        Assert.AreEqual(nowUnix, valuesReload.UnixSeconds);

        AssertCommonMeta(containerA, metaA, metaB);
        AssertAllAreEqual(6, (uint)(containerA.MetaIndex), (uint)(containerB.MetaIndex), metaA[3], metaB[3]);
        AssertAllAreEqual(4139, (uint)(containerA.BaseVersion), (uint)(containerB.BaseVersion), metaA[5], metaB[5]);
        var bytesA = BitConverter.GetBytes(metaA[6]);
        var bytesB = BitConverter.GetBytes(metaB[6]);
        AssertAllAreEqual((short)(PresetGameModeEnum.Creative), (short)(containerA.GameModeEnum), (short)(containerB.GameModeEnum), BitConverter.ToInt16(bytesA, 0), BitConverter.ToInt16(bytesB, 0));
        AssertAllAreEqual((short)(SeasonEnum.None), (short)(containerA.SeasonEnum), (short)(containerB.SeasonEnum), BitConverter.ToUInt16(bytesA, 2), BitConverter.ToUInt16(bytesA, 2));
        AssertAllAreEqual(51, containerA.TotalPlayTime, containerB.TotalPlayTime, metaA[7], metaB[7]);
    }

    [TestMethod]
    public void T12_Write_Default_0x7D2_Frontiers_Account()
    {
        var now = DateTimeOffset.UtcNow;
        var nowUnix = now.ToUnixTimeSeconds();
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "4");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };
        var writeCallback = false;

        // Act
        var platformA = new PlatformSwitch(path, settings);
        var containerA = platformA.GetAccountContainer();
        var metaA = DecryptMeta(containerA);

        containerA.WriteCallback += () =>
        {
            writeCallback = true;
        };

#pragma warning disable IDE0042 // Deconstruct variable declaration
        platformA.Load(containerA);
        (int MusicVolume, long UnixSeconds) valuesOrigin = (containerA.GetJsonValue<int>(MUSICVOLUME_JSON_PATH), containerA.LastWriteTime!.Value.ToUniversalTime().ToUnixTimeSeconds());

        containerA.SetJsonValue(MUSICVOLUME_NEW_AMOUNT, MUSICVOLUME_JSON_PATH);
        platformA.Write(containerA, now);
        (int MusicVolume, long UnixSeconds) valuesSet = (containerA.GetJsonValue<int>(MUSICVOLUME_JSON_PATH), containerA.LastWriteTime!.Value.ToUniversalTime().ToUnixTimeSeconds());

        var platformB = new PlatformSwitch(path, settings);
        var containerB = platformB.GetAccountContainer();
        var metaB = DecryptMeta(containerB);

        platformB.Load(containerB);
        (int MusicVolume, long UnixSeconds) valuesReload = (containerB.GetJsonValue<int>(MUSICVOLUME_JSON_PATH), containerB.LastWriteTime!.Value.ToUniversalTime().ToUnixTimeSeconds());
#pragma warning restore IDE0042 // Deconstruct variable declaration

        // Assert
        Assert.IsTrue(writeCallback);

        Assert.AreEqual(80, valuesOrigin.MusicVolume);
        Assert.AreEqual(1665084621, valuesOrigin.UnixSeconds); // 2022-10-06 19:30:21 +00:00
        Assert.AreEqual(MUSICVOLUME_NEW_AMOUNT, valuesSet.MusicVolume);
        Assert.AreEqual(nowUnix, valuesSet.UnixSeconds);

        Assert.AreEqual(MUSICVOLUME_NEW_AMOUNT, valuesReload.MusicVolume);
        Assert.AreEqual(nowUnix, valuesReload.UnixSeconds);

        AssertCommonMeta(containerA, metaA, metaB);
    }

    [TestMethod]
    public void T13_Write_SetLastWriteTime_False()
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
        var timestamp10 = container1.LastWriteTime!.Value;

        container1.SetJsonValue(UNITS_NEW_AMOUNT, UNITS_JSON_PATH);
        var units11 = container1.GetJsonValue<int>(UNITS_JSON_PATH);

        platform1.Write(container1, now);
        var timestamp11 = container1.LastWriteTime!.Value;

        var platform2 = new PlatformSwitch(path, settings);
        var container2 = platform2.GetSaveContainer(0)!;

        platform2.Load(container2);
        var units20 = container2.GetJsonValue<int>(UNITS_JSON_PATH);
        var timestamp20 = container2.LastWriteTime!.Value;

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
    public void T14_Write_WriteAlways_True()
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
    public void T15_Write_WriteAlways_False()
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
        Assert.AreEqual(container0.GameVersionEnum, container2.GameVersionEnum);

        Assert.IsTrue(container1.Exists);
        Assert.AreEqual(container0.GameModeEnum, container1.GameModeEnum);
        Assert.AreEqual(container0.SeasonEnum, container1.SeasonEnum);
        Assert.AreEqual(container0.BaseVersion, container1.BaseVersion);
        Assert.AreEqual(container0.GameVersionEnum, container1.GameVersionEnum);

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
        var versionEnum2 = container2.GameVersionEnum;
        platform.Copy(container4, container5);
        platform.Move(container2, container5); // overwrite

        platform.Move(container1, container0); // delete

        var gameModeEnum4 = container4.GameModeEnum;
        var seasonEnum4 = container4.SeasonEnum;
        var baseVersion4 = container4.BaseVersion;
        var versionEnum4 = container4.GameVersionEnum;
        platform.Move(container4, container9); // move

        // Assert
        Assert.IsFalse(container2.Exists);
        Assert.IsTrue(container5.Exists);
        Assert.AreEqual(gameModeEnum2, container5.GameModeEnum);
        Assert.AreEqual(seasonEnum2, container5.SeasonEnum);
        Assert.AreEqual(baseVersion2, container5.BaseVersion);
        Assert.AreEqual(versionEnum2, container5.GameVersionEnum);

        Assert.IsFalse(container0.Exists);
        Assert.IsFalse(container1.Exists);
        Assert.AreEqual(libNOM.io.Globals.Constants.INCOMPATIBILITY_006, container0.IncompatibilityTag);
        Assert.AreEqual(libNOM.io.Globals.Constants.INCOMPATIBILITY_006, container1.IncompatibilityTag);

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
