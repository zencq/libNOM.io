using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;

using libNOM.io;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace libNOM.test;


// Do not use System.Range for simplicity of the file and performace is not critical.
[TestClass]
[DeploymentItem("../../../Resources/TESTSUITE_ARCHIVE.zip")]
public class SwitchTest : CommonTestClass
{
    #region Constant

    protected const uint META_HEADER = 0xCA55E77E;
    protected const int META_LENGTH_TOTAL_VANILLA = 0x64 / sizeof(uint); // 25
    protected const int META_LENGTH_TOTAL_WAYPOINT = 0x164 / sizeof(uint); // 89

    #endregion

    #region Meta

    private static uint[] DecryptMeta(Container container)
    {
        byte[] meta = File.ReadAllBytes(container.MetaFile!.FullName);
        return ToUInt32(meta);
    }

    private static void AssertCommonMeta(uint[] metaA, uint[] metaB)
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

    private static void AssertSpecificMeta(WriteResults results, Container containerA, Container containerB, uint[] metaA, uint[] metaB)
    {
        var bytesA = metaA.AsSpan().AsBytes().ToArray();
        var bytesB = metaB.AsSpan().AsBytes().ToArray();
        var priectA = new PrivateObject(containerA);
        var priectB = new PrivateObject(containerB);

        AssertAllAreEqual(results.MetaIndex, (uint)(containerA.MetaIndex), (uint)(containerB.MetaIndex), metaA[3], metaB[3]);
        AssertAllAreEqual(results.BaseVersion, (uint)(int)(priectA.GetFieldOrProperty(nameof(WriteResults.BaseVersion))), (uint)(int)(priectB.GetFieldOrProperty(nameof(WriteResults.BaseVersion))), metaA[5], metaB[5]);
        AssertAllAreEqual(results.GameMode, (ushort)(priectA.GetFieldOrProperty(nameof(WriteResults.GameMode))), (ushort)(priectB.GetFieldOrProperty(nameof(WriteResults.GameMode))), BitConverter.ToInt16(bytesA, 24), BitConverter.ToInt16(bytesB, 24));
        AssertAllAreEqual(results.Season, (ushort)(containerA.Season), (ushort)(containerB.Season), BitConverter.ToUInt16(bytesA, 26), BitConverter.ToUInt16(bytesA, 26));
        AssertAllAreEqual(results.TotalPlayTime, containerA.TotalPlayTime, containerB.TotalPlayTime, metaA[7], metaB[7]);

        if (results.BaseVersion < 4140) // Waypoint
            return;

        AssertAllAreEqual(results.SaveName, containerA.SaveName, containerB.SaveName, GetString(bytesA.Skip(40).TakeWhile(i => i != 0)), GetString(bytesB.Skip(40).TakeWhile(i => i != 0)));
        AssertAllAreEqual(results.SaveSummary, containerA.SaveSummary, containerB.SaveSummary, GetString(bytesA.Skip(168).TakeWhile(i => i != 0)), GetString(bytesB.Skip(168).TakeWhile(i => i != 0)));
        AssertAllAreEqual(results.Difficulty, (byte)(containerA.Difficulty), (byte)(containerB.Difficulty), bytesA[296], bytesB[296]);
    }

    #endregion

    [TestMethod]
    public void T01_Read()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "1");
        var results = new ReadResults[]
        {
            new(0, "Slot1Auto", true, true, false, false, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Creative), DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4139, 5163, GameVersionEnum.Endurance, "", "", 18),
        };
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformSwitch(path, settings);

        // Assert
        AssertCommonRead(results, expectAccountData: false, userIdentification, platform);
    }

    [TestMethod]
    public void T02_Read()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "2");
        var results = new ReadResults[]
        {
            new(0, "Slot1Auto", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4139, 4651, GameVersionEnum.Endurance, "", "", 12655),
        };
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformSwitch(path, settings);

        // Assert
        AssertCommonRead(results, expectAccountData: true, userIdentification, platform);
    }

    [TestMethod]
    public void T03_Read()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "3");
        var results = new ReadResults[]
        {
            new(0, "Slot1Auto", true, true, false, false, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4139, 4651, GameVersionEnum.Endurance, "", "", 640),
        };
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformSwitch(path, settings);

        // Assert
        AssertCommonRead(results, expectAccountData: true, userIdentification, platform);
    }

    [TestMethod]
    public void T04_Read()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "4");
        var results = new ReadResults[]
        {
            new(0, "Slot1Auto", true, true, false, false, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4139, 4651, GameVersionEnum.Endurance, "", "", 225),

            new(2, "Slot2Auto", true, true, false, false, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Survival), DifficultyPresetTypeEnum.Survival, SeasonEnum.None, 4139, 5675, GameVersionEnum.Endurance, "", "", 336),

            new(4, "Slot3Auto", true, true, false, false, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Creative), DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4139, 5163, GameVersionEnum.Endurance, "", "", 51),
        };
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformSwitch(path, settings);

        // Assert
        AssertCommonRead(results, expectAccountData: true, userIdentification, platform);
    }

    [TestMethod]
    public void T05_Read()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "5");
        var results = new ReadResults[]
       {
            new(0, "Slot1Auto", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4145, 4657, GameVersionEnum.Singularity, "", "登上Inzadg球体", 63873),
            new(1, "Slot1Manual", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4145, 4657, GameVersionEnum.Singularity, "", "登上Inzadg球体", 63651),

            new(2, "Slot2Auto", true, true, false, false, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4145, 4657, GameVersionEnum.Singularity, "", "登上太空异象", 88),
            new(3, "Slot2Manual", true, true, false, false, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4145, 4657, GameVersionEnum.Singularity, "", "登上太空异象", 82),
       };
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformSwitch(path, settings);

        // Assert
        AssertCommonRead(results, expectAccountData: true, userIdentification, platform);
    }

    [TestMethod]
    public void T10_Write_Default_0x7D2_Frontiers()
    {
        var now = DateTimeOffset.UtcNow;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "4");
        var results = new WriteResults(6, 4139, (ushort)(PresetGameModeEnum.Creative), (ushort)(SeasonEnum.None), 51, "", "", (byte)(DifficultyPresetTypeEnum.Normal));
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            UseMapping = true,
        };
        var writeCallback = false;

        // Act
        var platformA = new PlatformSwitch(path, settings);
        var containerA = platformA.GetSaveContainer(4);
        Guard.IsNotNull(containerA);
        var metaA = DecryptMeta(containerA);

        containerA.WriteCallback += () =>
        {
            writeCallback = true;
        };

        platformA.Load(containerA);
        (int Units, long UtcTicks) valuesOrigin = (containerA.GetJsonValue<int>(UNITS_JSON_PATH), containerA.LastWriteTime!.Value.UtcTicks);

        containerA.SetJsonValue(UNITS_NEW_AMOUNT, UNITS_JSON_PATH);
        platformA.Write(containerA, now);
        (int Units, long UtcTicks) valuesSet = (containerA.GetJsonValue<int>(UNITS_JSON_PATH), containerA.LastWriteTime!.Value.UtcTicks);

        var platformB = new PlatformSwitch(path, settings);
        var containerB = platformB.GetSaveContainer(4);
        Guard.IsNotNull(containerB);
        var metaB = DecryptMeta(containerB);

        platformB.Load(containerB);
        (int Units, long UtcTicks) valuesReload = (containerB.GetJsonValue<int>(UNITS_JSON_PATH), containerB.LastWriteTime!.Value.UtcTicks);

        // Assert
        Assert.IsTrue(writeCallback);

        AssertCommonWriteValues(0, 638006823360000000, valuesOrigin); // 0 // 2022-10-06 19:45:36 +00:00
        AssertCommonWriteValues(UNITS_NEW_AMOUNT, now.UtcTicks, valuesSet);
        AssertCommonWriteValues(UNITS_NEW_AMOUNT, now.UtcTicks, valuesReload);

        AssertCommonMeta(metaA, metaB);
        AssertSpecificMeta(results, containerA, containerB, metaA, metaB);
    }

    [TestMethod]
    public void T11_Write_Default_0x7D2_Frontiers_Account()
    {
        var now = DateTimeOffset.UtcNow;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "4");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            UseMapping = true,
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

        platformA.Load(containerA);
        (int MusicVolume, long UtcTicks) valuesOrigin = (containerA.GetJsonValue<int>(MUSICVOLUME_JSON_PATH), containerA.LastWriteTime!.Value.UtcTicks);

        containerA.SetJsonValue(MUSICVOLUME_NEW_AMOUNT, MUSICVOLUME_JSON_PATH);
        platformA.Write(containerA, now);
        (int MusicVolume, long UtcTicks) valuesSet = (containerA.GetJsonValue<int>(MUSICVOLUME_JSON_PATH), containerA.LastWriteTime!.Value.UtcTicks);

        var platformB = new PlatformSwitch(path, settings);
        var containerB = platformB.GetAccountContainer();
        var metaB = DecryptMeta(containerB);

        platformB.Load(containerB);
        (int MusicVolume, long UtcTicks) valuesReload = (containerB.GetJsonValue<int>(MUSICVOLUME_JSON_PATH), containerB.LastWriteTime!.Value.UtcTicks);

        // Assert
        Assert.IsTrue(writeCallback);

        AssertCommonWriteValues(80, 638006823320000000, valuesOrigin); // 80 // 2022-10-06 19:45:32 +00:00
        AssertCommonWriteValues(MUSICVOLUME_NEW_AMOUNT, now.UtcTicks, valuesSet);
        AssertCommonWriteValues(MUSICVOLUME_NEW_AMOUNT, now.UtcTicks, valuesReload);

        AssertCommonMeta(metaA, metaB);
    }

    [TestMethod]
    public void T12_Write_Default_0x7D2_Waypoint()
    {
        var now = DateTimeOffset.UtcNow;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "5");
        var results = new WriteResults(2, 4145, (ushort)(PresetGameModeEnum.Normal), (ushort)(SeasonEnum.None), 63873, "", "登上Inzadg球体", (byte)(DifficultyPresetTypeEnum.Custom));
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            UseMapping = true,
        };
        var writeCallback = false;

        // Act
        var platformA = new PlatformSwitch(path, settings);
        var containerA = platformA.GetSaveContainer(0);
        Guard.IsNotNull(containerA);
        var metaA = DecryptMeta(containerA);

        containerA.WriteCallback += () =>
        {
            writeCallback = true;
        };

        platformA.Load(containerA);
        (int Units, long UtcTicks) valuesOrigin = (containerA.GetJsonValue<int>(UNITS_JSON_PATH), containerA.LastWriteTime!.Value.UtcTicks);

        containerA.SetJsonValue(UNITS_NEW_AMOUNT, UNITS_JSON_PATH);
        platformA.Write(containerA, now);
        (int Units, long UtcTicks) valuesSet = (containerA.GetJsonValue<int>(UNITS_JSON_PATH), containerA.LastWriteTime!.Value.UtcTicks);

        var platformB = new PlatformSwitch(path, settings);
        var containerB = platformB.GetSaveContainer(0);
        Guard.IsNotNull(containerB);
        var metaB = DecryptMeta(containerB);

        platformB.Load(containerB);
        (int Units, long UtcTicks) valuesReload = (containerB.GetJsonValue<int>(UNITS_JSON_PATH), containerB.LastWriteTime!.Value.UtcTicks);

        // Assert
        Assert.IsTrue(writeCallback);

        AssertCommonWriteValues(1000356262, 638093635960000000, valuesOrigin); // 1.000.356.262 // 2023-01-15 07:13:16 +00:00
        AssertCommonWriteValues(UNITS_NEW_AMOUNT, now.UtcTicks, valuesSet);
        AssertCommonWriteValues(UNITS_NEW_AMOUNT, now.UtcTicks, valuesReload);

        AssertCommonMeta(metaA, metaB);
        AssertSpecificMeta(results, containerA, containerB, metaA, metaB);
    }

    [TestMethod]
    public void T13_Write_Default_0x7D2_Waypoint_Account()
    {
        var now = DateTimeOffset.UtcNow;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "5");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            UseMapping = true,
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

        platformA.Load(containerA);
        (int MusicVolume, long UtcTicks) valuesOrigin = (containerA.GetJsonValue<int>(MUSICVOLUME_JSON_PATH), containerA.LastWriteTime!.Value.UtcTicks);

        containerA.SetJsonValue(MUSICVOLUME_NEW_AMOUNT, MUSICVOLUME_JSON_PATH);
        platformA.Write(containerA, now);
        (int MusicVolume, long UtcTicks) valuesSet = (containerA.GetJsonValue<int>(MUSICVOLUME_JSON_PATH), containerA.LastWriteTime!.Value.UtcTicks);

        var platformB = new PlatformSwitch(path, settings);
        var containerB = platformB.GetAccountContainer();
        Guard.IsNotNull(containerB);
        var metaB = DecryptMeta(containerB);

        platformB.Load(containerB);
        (int MusicVolume, long UtcTicks) valuesReload = (containerB.GetJsonValue<int>(MUSICVOLUME_JSON_PATH), containerB.LastWriteTime!.Value.UtcTicks);

        // Assert
        Assert.IsTrue(writeCallback);

        AssertCommonWriteValues(80, 638298440840000000, valuesOrigin); // 80 // 2023-09-09 08:14:44 +00:00
        AssertCommonWriteValues(MUSICVOLUME_NEW_AMOUNT, now.UtcTicks, valuesSet);
        AssertCommonWriteValues(MUSICVOLUME_NEW_AMOUNT, now.UtcTicks, valuesReload);

        AssertCommonMeta(metaA, metaB);
    }

    [TestMethod]
    public void T14_Write_SetLastWriteTime_False()
    {
        var now = DateTimeOffset.UtcNow;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "1");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            SetLastWriteTime = false,
            UseMapping = true,
        };
        var writeCallback = false;

        // Act
        var platformA = new PlatformSwitch(path, settings);
        var containerA = platformA.GetSaveContainer(0);
        Guard.IsNotNull(containerA);

        containerA.WriteCallback += () =>
        {
            writeCallback = true;
        };

        platformA.Load(containerA);
        (int Units, long UtcTicks) valuesOrigin = (containerA.GetJsonValue<int>(UNITS_JSON_PATH), containerA.LastWriteTime!.Value.UtcTicks);

        containerA.SetJsonValue(UNITS_NEW_AMOUNT, UNITS_JSON_PATH);
        platformA.Write(containerA, now);
        (int Units, long UtcTicks) valuesSet = (containerA.GetJsonValue<int>(UNITS_JSON_PATH), containerA.LastWriteTime!.Value.UtcTicks);

        var platformB = new PlatformSwitch(path, settings);
        var containerB = platformB.GetSaveContainer(0);
        Guard.IsNotNull(containerB);

        platformB.Load(containerB);
        (int Units, long UtcTicks) valuesReload = (containerB.GetJsonValue<int>(UNITS_JSON_PATH), containerB.LastWriteTime!.Value.UtcTicks);

        // Assert
        Assert.IsTrue(writeCallback);

        AssertCommonWriteValues(0, 638006282230000000, valuesOrigin); // 0 // 2022-10-06 04:43:43
        AssertCommonWriteValues(UNITS_NEW_AMOUNT, 638006282230000000, valuesSet);
        AssertCommonWriteValues(UNITS_NEW_AMOUNT, 638006282230000000, valuesReload);
    }

    [TestMethod]
    public void T15_Write_WriteAlways_True()
    {
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "1");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            WriteAlways = true,
        };
        var writeCallback = false;

        // Act
        var platformA = new PlatformSwitch(path, settings);
        var containerA = platformA.GetSaveContainer(0);
        Guard.IsNotNull(containerA);

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

        var platformB = new PlatformSwitch(path, settings);
        var containerB = platformB.GetSaveContainer(0);
        Guard.IsNotNull(containerB);

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
    public void T16_Write_WriteAlways_False()
    {
        var now = DateTimeOffset.UtcNow;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "1");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            WriteAlways = false,
        };
        var writeCallback = false;

        // Act
        var platformA = new PlatformSwitch(path, settings);
        var containerA = platformA.GetSaveContainer(0);
        Guard.IsNotNull(containerA);

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

        var platformB = new PlatformSwitch(path, settings);
        var containerB = platformB.GetSaveContainer(0);
        Guard.IsNotNull(containerB);

        platformB.Load(containerB);
        containerB.DataFile!.Refresh();
        var lengthReload = containerA.DataFile!.Length;

        // Assert
        Assert.IsTrue(writeCallback);

        Assert.AreEqual(lengthOrigin, lengthSet);
        Assert.AreEqual(lengthOrigin, lengthReload); // then lengthSet and lengthReload AreEqual too
    }

    [TestMethod]
    public void T20_FileSystemWatcher()
    {
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "1");
        var pathWatching = Path.Combine(path, "manifest02.hg");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            UseMapping = true,
            Watcher = true,
        };

        // Act
        var bytes = File.ReadAllBytes(pathWatching);

        var platform = new PlatformSwitch(path, settings);
        var container = platform.GetSaveContainer(0);
        Guard.IsNotNull(container);

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
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "4");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };

        // Act
        var platform = new PlatformSwitch(path, settings);

        var container0 = platform.GetSaveContainer(0); // 1Auto
        var container1 = platform.GetSaveContainer(1); // 1Manual (!Exists)
        var container2 = platform.GetSaveContainer(2); // 2Auto
        var container4 = platform.GetSaveContainer(4); // 3Auto
        var container6 = platform.GetSaveContainer(6); // 4Auto (!Exists)

        Guard.IsNotNull(container0);
        Guard.IsNotNull(container1);
        Guard.IsNotNull(container2);
        Guard.IsNotNull(container4);
        Guard.IsNotNull(container6);

        platform.Copy(container0, container2); // 1Auto -> 2Auto (overwrite)
        platform.Copy(container0, container1); // 1Auto -> 1Manual (create)
        platform.Copy(container6, container4); // 4Auto -> 3Auto (delete)

        // Assert
        Assert.IsTrue(container0.Exists);
        Assert.IsTrue(container2.Exists);
        AssertCommonFileOperation(GetFileOperationResults(container0), GetFileOperationResults(container2));

        Assert.IsTrue(container0.Exists);
        Assert.IsTrue(container1.Exists);
        AssertCommonFileOperation(GetFileOperationResults(container0), GetFileOperationResults(container1));

        Assert.IsFalse(container6.Exists);
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

        var container0 = platform.GetSaveContainer(0); // 1Auto

        Guard.IsNotNull(container0);

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

        var container0 = platform.GetSaveContainer(0); // 1Auto
        var container1 = platform.GetSaveContainer(1); // 1Manual (!Exists)
        var container2 = platform.GetSaveContainer(2); // 2Auto
        var container4 = platform.GetSaveContainer(4); // 3Auto
        var container5 = platform.GetSaveContainer(5); // 3Manual
        var container9 = platform.GetSaveContainer(9); // 5Manual (!Exists)

        Guard.IsNotNull(container0);
        Guard.IsNotNull(container1);
        Guard.IsNotNull(container2);
        Guard.IsNotNull(container4);
        Guard.IsNotNull(container5);
        Guard.IsNotNull(container9);

        var result2 = GetFileOperationResults(container2);
        var result4 = GetFileOperationResults(container4);

        platform.Copy(container4, container5);

        platform.Move(container2, container5); // overwrite
        platform.Move(container1, container0); // delete in same slot
        platform.Move(container4, container9); // move

        // Assert
        Assert.IsFalse(container2.Exists);
        Assert.IsTrue(container5.Exists);
        AssertCommonFileOperation(result2, GetFileOperationResults(container5));

        Assert.IsFalse(container1.Exists);
        Assert.IsFalse(container0.Exists);
        Assert.AreEqual(libNOM.io.Globals.Constants.INCOMPATIBILITY_006, container1.IncompatibilityTag);
        Assert.AreEqual(libNOM.io.Globals.Constants.INCOMPATIBILITY_006, container0.IncompatibilityTag);

        Assert.IsFalse(container4.Exists);
        Assert.IsTrue(container9.Exists);
        AssertCommonFileOperation(result4, GetFileOperationResults(container9));
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
        var userIdentificationGog = ReadUserIdentification(pathGog);

        var offset = 2;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "4");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
            UseExternalSourcesForUserIdentification = false,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platformGog = new PlatformGog(pathGog, settings);
        var transferGog = platformGog.GetSourceTransferData(1); // get Slot2

        var platform = new PlatformSwitch(path, settings);

        platform.Transfer(transferGog, 2); // overwrite Slot3
        platform.Transfer(transferGog, 3); // create Slot4

        // Assert
        Assert.AreEqual(1, transferGog.TransferBaseUserDecision.Count);
        Assert.AreEqual(6, GetExistingContainers(platform).Count()); // 3 + 1 (Slot3) + 2 (Slot4)

        AssertCommonSourceTransferData(userIdentificationGog, platformGog, transferGog);
        AssertCommonTransfer(resultsGog, userIdentification, platform, offset);
    }

    //[TestMethod]
    //public void T41_TransferFromMicrosoft()
    //{
    //    // Arrange
    //    var pathMicrosoft = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "0009000000C73498_29070100B936489ABCE8B9AF3980429C");
    //    var resultsMicrosoft = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum Season, int BaseVersion, GameVersionEnum Version)[]
    //    {
    //        (2, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, GameVersionEnum.Frontiers), // 2Auto
    //        (3, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, GameVersionEnum.Frontiers), // 2Manual
    //    };
    //    var userIdentificationMicrosoft = ReadUserIdentification(pathMicrosoft);

    //    var offset = 2;
    //    var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "4");
    //    var settings = new PlatformSettings
    //    {
    //        LoadingStrategy = LoadingStrategyEnum.Full,
    //        UseExternalSourcesForUserIdentification = false,
    //    };
    //    var userIdentification = ReadUserIdentification(path);

    //    // Act
    //    var platformMicrosoft = new PlatformMicrosoft(pathMicrosoft, settings);
    //    var transferMicrosoft = platformMicrosoft.GetSourceTransferData(1); // get Slot2

    //    var platform = new PlatformSwitch(path, settings);

    //    platform.Transfer(transferMicrosoft, 2); // overwrite Slot3
    //    platform.Transfer(transferMicrosoft, 3); // create Slot4

    //    // Assert
    //    Assert.AreEqual(8, transferMicrosoft.TransferBaseUserDecision.Count);
    //    Assert.AreEqual(6, GetExistingContainers(platform).Count()); // 3 + 1 (Slot3) + 2 (Slot4)

    //    AssertCommonSourceTransferData(userIdentificationMicrosoft, platformMicrosoft, transferMicrosoft);
    //    AssertCommonTransfer(resultsMicrosoft, userIdentification, platform, offset);
    //}

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
    //    var userIdentificationPlaystation = ReadUserIdentification(pathPlaystation);

    //    var offset = 2;
    //    var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "4");
    //    var settings = new PlatformSettings
    //    {
    //        LoadingStrategy = LoadingStrategyEnum.Full,
    //        UseExternalSourcesForUserIdentification = false,
    //    };
    //    var userIdentification = ReadUserIdentification(path);

    //    // Act
    //    var platformPlaystation = new PlatformPlaystation(pathPlaystation, settings);
    //    var transferPlaystation = platformPlaystation.GetSourceTransferData(1); // get Slot2

    //    var platform = new PlatformSwitch(path, settings);

    //    platform.Transfer(transferPlaystation, 2); // overwrite Slot3
    //    platform.Transfer(transferPlaystation, 3); // create Slot4

    //    // Assert
    //    Assert.AreEqual(24, transferPlaystation.TransferBaseUserDecision.Count);
    //    Assert.AreEqual(6, GetExistingContainers(platform).Count()); // 3 + 1 (Slot3) + 2 (Slot4)

    //    AssertCommonSourceTransferData(userIdentificationPlaystation, platformPlaystation, transferPlaystation);
    //    AssertCommonTransfer(resultsPlaystation, userIdentification, platform, offset);
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
    //    var userIdentificationPlaystation = ReadUserIdentification(pathPlaystation);

    //    var offset = 2;
    //    var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "4");
    //    var settings = new PlatformSettings
    //    {
    //        LoadingStrategy = LoadingStrategyEnum.Full,
    //        UseExternalSourcesForUserIdentification = false,
    //    };
    //    var userIdentification = ReadUserIdentification(path);

    //    // Act
    //    var platformPlaystation = new PlatformPlaystation(pathPlaystation, settings);
    //    var transferPlaystation = platformPlaystation.GetSourceTransferData(1); // get Slot2

    //    var platform = new PlatformSwitch(path, settings);

    //    platform.Transfer(transferPlaystation, 2); // overwrite Slot3
    //    platform.Transfer(transferPlaystation, 3); // create Slot4

    //    // Assert
    //    Assert.AreEqual(4, transferPlaystation.TransferBaseUserDecision.Count);
    //    Assert.AreEqual(6, GetExistingContainers(platform).Count()); // 3 + 1 (Slot3) + 2 (Slot4)

    //    AssertCommonSourceTransferData(userIdentificationPlaystation, platformPlaystation, transferPlaystation);
    //    AssertCommonTransfer(resultsPlaystation, userIdentification, platform, offset);
    //}

    [TestMethod]
    public void T44_TransferFromSteam()
    {
        // Arrange
        var pathSteam = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");
        var resultsSteam = new ReadResults[]
        {
            new(2, "Slot2Auto", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Creative), DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4127, 5151, GameVersionEnum.Companions, "", "", 4765),
            new(3, "Slot2Manual", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Creative), DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4127, 5151, GameVersionEnum.Companions, "", "", 4271),
        };
        var userIdentificationSteam = ReadUserIdentification(pathSteam);

        var offset = 2;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "4");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
            UseExternalSourcesForUserIdentification = false,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platformSteam = new PlatformSteam(pathSteam, settings);
        var transferSteam = platformSteam.GetSourceTransferData(1); // get Slot2

        var platform = new PlatformSwitch(path, settings);

        platform.Transfer(transferSteam, 2); // overwrite Slot3
        platform.Transfer(transferSteam, 3); // create Slot4

        // Assert
        Assert.AreEqual(2, transferSteam.TransferBaseUserDecision.Count);
        Assert.AreEqual(6, GetExistingContainers(platform).Count()); // 3 + 1 (Slot3) + 2 (Slot4)

        AssertCommonSourceTransferData(userIdentificationSteam, platformSteam, transferSteam);
        AssertCommonTransfer(resultsSteam, userIdentification, platform, offset);
    }

    [TestMethod]
    public void T45_TransferFromSwitch()
    {
        // Arrange
        var pathSwitch = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "1");
        var resultsSwitch = new ReadResults[]
        {
            new(0, "Slot1Auto", true, true, false, false, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Creative), DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4139, 5163, GameVersionEnum.Endurance, "", "", 18),
        };
        var userIdentificationSwitch = ReadUserIdentification(pathSwitch);

        var offset = 4;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "4");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
            UseExternalSourcesForUserIdentification = false,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platformSwitch = new PlatformSwitch(pathSwitch, settings);
        var transferSwitch = platformSwitch.GetSourceTransferData(0); // get Slot1

        var platform = new PlatformSwitch(path, settings);

        platform.Transfer(transferSwitch, 2); // overwrite Slot3
        platform.Transfer(transferSwitch, 3); // create Slot4

        // Assert
        Assert.AreEqual(0, transferSwitch.TransferBaseUserDecision.Count);
        Assert.AreEqual(4, GetExistingContainers(platform).Count()); // 3 + 1 (Slot?)

        AssertCommonSourceTransferData(userIdentificationSwitch, platformSwitch, transferSwitch);
        AssertCommonTransfer(resultsSwitch, userIdentification, platform, offset);
    }
}
