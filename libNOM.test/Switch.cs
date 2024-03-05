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
public class SwitchTest : CommonTestClass
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

    #endregion

    [TestMethod]
    public void T01_Read()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "1");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum SeasonEnum, int BaseVersion, GameVersionEnum GameVersionEnum)[]
        {
            (0, true, false, PresetGameModeEnum.Creative, DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4139, GameVersionEnum.Endurance), // 1Auto
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
            Assert.AreEqual(results[i].GameDifficulty, container.Difficulty);
            Assert.AreEqual(results[i].SeasonEnum, container.Season);
            Assert.AreEqual(results[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(results[i].GameVersionEnum, container.GameVersion);
        }
    }

    [TestMethod]
    public void T02_Read()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "2");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum SeasonEnum, int BaseVersion, GameVersionEnum GameVersionEnum)[]
        {
            (0, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4139, GameVersionEnum.Endurance), // 1Auto
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
            Assert.AreEqual(results[i].GameDifficulty, container.Difficulty);
            Assert.AreEqual(results[i].SeasonEnum, container.Season);
            Assert.AreEqual(results[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(results[i].GameVersionEnum, container.GameVersion);
        }
    }

    [TestMethod]
    public void T03_Read()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "3");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum SeasonEnum, int BaseVersion, GameVersionEnum GameVersionEnum)[]
        {
            (0, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4139, GameVersionEnum.Endurance), // 1Auto
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
            Assert.AreEqual(results[i].GameDifficulty, container.Difficulty);
            Assert.AreEqual(results[i].SeasonEnum, container.Season);
            Assert.AreEqual(results[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(results[i].GameVersionEnum, container.GameVersion);
        }
    }

    [TestMethod]
    public void T04_Read()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "4");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum SeasonEnum, int BaseVersion, GameVersionEnum GameVersionEnum)[]
        {
            (0, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4139, GameVersionEnum.Endurance), // 1Auto
            (2, true, false, PresetGameModeEnum.Survival, DifficultyPresetTypeEnum.Survival, SeasonEnum.None, 4139, GameVersionEnum.Endurance), // 2Auto
            (4, true, false, PresetGameModeEnum.Creative, DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4139, GameVersionEnum.Endurance), // 3Auto
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
            Assert.AreEqual(results[i].GameDifficulty, container.Difficulty);
            Assert.AreEqual(results[i].SeasonEnum, container.Season);
            Assert.AreEqual(results[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(results[i].GameVersionEnum, container.GameVersion);
        }
    }

    [TestMethod]
    public void T05_Read()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "5");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum SeasonEnum, int BaseVersion, GameVersionEnum GameVersionEnum, string SaveName, string SaveSummary)[]
        {
            (0, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4145, GameVersionEnum.Singularity, "", "登上Inzadg球体"), // 1Auto
            (1, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4145, GameVersionEnum.Singularity, "", "登上Inzadg球体"), // 1Manual
            (2, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4145, GameVersionEnum.Singularity, "", "登上太空异象"), // 2Auto
            (3, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4145, GameVersionEnum.Singularity, "", "登上太空异象"), // 2Manual
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
            Assert.AreEqual(results[i].GameDifficulty, container.Difficulty);
            Assert.AreEqual(results[i].SeasonEnum, container.Season);
            Assert.AreEqual(results[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(results[i].GameVersionEnum, container.GameVersion);
            Assert.AreEqual(results[i].SaveName, container.SaveName);
            Assert.AreEqual(results[i].SaveSummary, container.SaveSummary);
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
            UseMapping = true,
        };
        var writeCallback = false;

        // Act
        var platformA = new PlatformSwitch(path, settings);
        var containerA = GetOneSaveContainer(platformA, 4);
        var metaA = DecryptMeta(containerA);
        var priectA = new PrivateObject(containerA);

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
        var containerB = GetOneSaveContainer(platformB, 4);
        var metaB = DecryptMeta(containerB);
        var priectB = new PrivateObject(containerB);

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

        AssertCommonMeta(metaA, metaB);

        var bytesA = metaA.AsSpan().AsBytes().ToArray();
        var bytesB = metaB.AsSpan().AsBytes().ToArray();

        AssertAllAreEqual(6, (uint)(containerA.MetaIndex), (uint)(containerB.MetaIndex), metaA[3], metaB[3]);
        AssertAllAreEqual(4139, (uint)(int)(priectA.GetFieldOrProperty("BaseVersion")), (uint)(int)(priectB.GetFieldOrProperty("BaseVersion")), metaA[5], metaB[5]);
        AssertAllAreEqual((ushort)(PresetGameModeEnum.Creative), (ushort)(PresetGameModeEnum)(priectA.GetFieldOrProperty("GameMode")), (ushort)(PresetGameModeEnum)(priectB.GetFieldOrProperty("GameMode")), BitConverter.ToInt16(bytesA, 24), BitConverter.ToInt16(bytesB, 24));
        AssertAllAreEqual((ushort)(SeasonEnum.None), (ushort)(containerA.Season), (ushort)(containerB.Season), BitConverter.ToUInt16(bytesA, 26), BitConverter.ToUInt16(bytesA, 26));
        AssertAllAreEqual(51, containerA.TotalPlayTime, containerB.TotalPlayTime, metaA[7], metaB[7]);
    }

    [TestMethod]
    public void T11_Write_Default_0x7D2_Frontiers_Account()
    {
        var now = DateTimeOffset.UtcNow;
        var nowUnix = now.ToUnixTimeSeconds();
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
        Assert.AreEqual(1665085532, valuesOrigin.UnixSeconds); // 2022-10-06 19:45:32 +00:00
        Assert.AreEqual(MUSICVOLUME_NEW_AMOUNT, valuesSet.MusicVolume);
        Assert.AreEqual(nowUnix, valuesSet.UnixSeconds);

        Assert.AreEqual(MUSICVOLUME_NEW_AMOUNT, valuesReload.MusicVolume);
        Assert.AreEqual(nowUnix, valuesReload.UnixSeconds);

        AssertCommonMeta(metaA, metaB);
    }

    [TestMethod]
    public void T12_Write_Default_0x7D2_Waypoint()
    {
        var now = DateTimeOffset.UtcNow;
        var nowUnix = now.ToUnixTimeSeconds();
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "5");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            UseMapping = true,
        };
        var writeCallback = false;

        // Act
        var platformA = new PlatformSwitch(path, settings);
        var containerA = GetOneSaveContainer(platformA, 0);
        var metaA = DecryptMeta(containerA);
        var priectA = new PrivateObject(containerA);

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
        var containerB = GetOneSaveContainer(platformB, 0);
        var metaB = DecryptMeta(containerB);
        var priectB = new PrivateObject(containerB);

        platformB.Load(containerB);
        (int Units, long UnixSeconds) valuesReload = (containerB.GetJsonValue<int>(UNITS_JSON_PATH), containerB.LastWriteTime!.Value.ToUniversalTime().ToUnixTimeSeconds());
#pragma warning restore IDE0042 // Deconstruct variable declaration

        // Assert
        Assert.IsTrue(writeCallback);

        Assert.AreEqual(1000356262, valuesOrigin.Units);
        Assert.AreEqual(1673766796, valuesOrigin.UnixSeconds); // 2023-01-15 07:13:16 +00:00
        Assert.AreEqual(UNITS_NEW_AMOUNT, valuesSet.Units);
        Assert.AreEqual(nowUnix, valuesSet.UnixSeconds);

        Assert.AreEqual(UNITS_NEW_AMOUNT, valuesReload.Units);
        Assert.AreEqual(nowUnix, valuesReload.UnixSeconds);

        AssertCommonMeta(metaA, metaB);

        var bytesA = metaA.AsSpan().AsBytes().ToArray();
        var bytesB = metaB.AsSpan().AsBytes().ToArray();

        AssertAllAreEqual(2, (uint)(containerA.MetaIndex), (uint)(containerB.MetaIndex), metaA[3], metaB[3]);
        AssertAllAreEqual(4145, (uint)(int)(priectA.GetFieldOrProperty("BaseVersion")), (uint)(int)(priectB.GetFieldOrProperty("BaseVersion")), metaA[5], metaB[5]);
        AssertAllAreEqual((ushort)(PresetGameModeEnum.Normal), (ushort)(PresetGameModeEnum)(priectA.GetFieldOrProperty("GameMode")), (ushort)(PresetGameModeEnum)(priectB.GetFieldOrProperty("GameMode")), BitConverter.ToInt16(bytesA, 24), BitConverter.ToInt16(bytesB, 24));
        AssertAllAreEqual((ushort)(SeasonEnum.None), (ushort)(containerA.Season), (ushort)(containerB.Season), BitConverter.ToUInt16(bytesA, 26), BitConverter.ToUInt16(bytesA, 26));
        AssertAllAreEqual(63873, containerA.TotalPlayTime, containerB.TotalPlayTime, metaA[7], metaB[7]);
        AssertAllAreEqual("", containerA.SaveName, containerB.SaveName, GetString(bytesA.Skip(40).TakeWhile(i => i != 0)), GetString(bytesB.Skip(40).TakeWhile(i => i != 0)));
        AssertAllAreEqual("登上Inzadg球体", containerA.SaveSummary, containerB.SaveSummary, GetString(bytesA.Skip(168).TakeWhile(i => i != 0)), GetString(bytesB.Skip(168).TakeWhile(i => i != 0)));
        AssertAllAreEqual((byte)(DifficultyPresetTypeEnum.Custom), (byte)(containerA.Difficulty), (byte)(containerB.Difficulty), bytesA[296], bytesB[296]);
    }

    [TestMethod]
    public void T13_Write_Default_0x7D2_Waypoint_Account()
    {
        var now = DateTimeOffset.UtcNow;
        var nowUnix = now.ToUnixTimeSeconds();
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
        Assert.AreEqual(1694247284, valuesOrigin.UnixSeconds); // 2023-09-09 08:14:44 +00:00
        Assert.AreEqual(MUSICVOLUME_NEW_AMOUNT, valuesSet.MusicVolume);
        Assert.AreEqual(nowUnix, valuesSet.UnixSeconds);

        Assert.AreEqual(MUSICVOLUME_NEW_AMOUNT, valuesReload.MusicVolume);
        Assert.AreEqual(nowUnix, valuesReload.UnixSeconds);

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
        var userIdentification = ReadUserIdentification(path);
        var writeCallback = false;

        // Act
        var platformA = new PlatformSwitch(path, settings);
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

        var platformB = new PlatformSwitch(path, settings);
        var containerB = GetOneSaveContainer(platformB, 0);
        var metaB = DecryptMeta(containerB);
        var priectB = new PrivateObject(containerB);

        platformB.Load(containerB);
        (int Units, long UtcTicks) valuesReload = (containerB.GetJsonValue<int>(UNITS_JSON_PATH), containerB.LastWriteTime!.Value.UtcTicks);
#pragma warning restore IDE0042 // Deconstruct variable declaration

        // Assert
        Assert.IsTrue(writeCallback);

        Assert.AreEqual(0, valuesOrigin.Units);
        Assert.AreEqual(638006282230000000, valuesOrigin.UtcTicks); // 2021-09-04 22:16:24 +00:00
        Assert.AreEqual(UNITS_NEW_AMOUNT, valuesSet.Units);
        Assert.AreEqual(638006282230000000, valuesSet.UtcTicks);

        Assert.AreEqual(UNITS_NEW_AMOUNT, valuesReload.Units);
        Assert.AreEqual(638006282230000000, valuesReload.UtcTicks);
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
        var userIdentification = ReadUserIdentification(path);
        var writeCallback = false;

        // Act
        var platformA = new PlatformSwitch(path, settings);
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

        var platformB = new PlatformSwitch(path, settings);
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
    public void T16_Write_WriteAlways_False()
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
        var platformA = new PlatformSwitch(path, settings);
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

        var platformB = new PlatformSwitch(path, settings);
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
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "4");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };

        // Act
        var platform = new PlatformSwitch(path, settings);

        var container0 = GetOneSaveContainer(platform, 0); // 1Auto
        var container1 = GetOneSaveContainer(platform, 1); // 1Manual
        var container2 = GetOneSaveContainer(platform, 2); // 2Auto
        var container4 = GetOneSaveContainer(platform, 4); // 3Auto
        var container6 = GetOneSaveContainer(platform, 6); // 4Auto

        platform.Copy(container0, container2); // 1Auto -> 2Auto (overwrite)
        platform.Copy(container0, container1); // 1Auto -> 1Manual (create)
        platform.Copy(container6, container4); // 4Auto -> 3Auto (delete)

        // Assert
        var priect0 = new PrivateObject(container0);
        var priect1 = new PrivateObject(container1);
        var priect2 = new PrivateObject(container2);

        Assert.IsTrue(container2.Exists);
        Assert.AreEqual((PresetGameModeEnum)(priect0.GetFieldOrProperty("GameMode")), (PresetGameModeEnum)(priect2.GetFieldOrProperty("GameMode")));
        Assert.AreEqual(container0.Difficulty, container2.Difficulty);
        Assert.AreEqual(container0.Season, container2.Season);
        Assert.AreEqual((int)(priect0.GetFieldOrProperty("BaseVersion")), (int)(priect2.GetFieldOrProperty("BaseVersion")));
        Assert.AreEqual(container0.GameVersion, container2.GameVersion);
        Assert.AreEqual(container0.TotalPlayTime, container2.TotalPlayTime);

        Assert.IsTrue(container1.Exists);
        Assert.AreEqual((PresetGameModeEnum)(priect0.GetFieldOrProperty("GameMode")), (PresetGameModeEnum)(priect1.GetFieldOrProperty("GameMode")));
        Assert.AreEqual(container0.Difficulty, container1.Difficulty);
        Assert.AreEqual(container0.Season, container1.Season);
        Assert.AreEqual((int)(priect0.GetFieldOrProperty("BaseVersion")), (int)(priect1.GetFieldOrProperty("BaseVersion")));
        Assert.AreEqual(container0.GameVersion, container1.GameVersion);
        Assert.AreEqual(container0.TotalPlayTime, container1.TotalPlayTime);

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

        var container0 = GetOneSaveContainer(platform, 0); // 1Auto

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

        var container0 = GetOneSaveContainer(platform, 0); // 1Auto
        var container1 = GetOneSaveContainer(platform, 1); // 1Manual
        var container2 = GetOneSaveContainer(platform, 2); // 2Auto
        var container4 = GetOneSaveContainer(platform, 4); // 3Auto
        var container5 = GetOneSaveContainer(platform, 5); // 3Manual
        var container9 = GetOneSaveContainer(platform, 9); // 5Manual

        var priect2 = new PrivateObject(container2);
        var priect4 = new PrivateObject(container4);

        var gameModeEnum2 = (PresetGameModeEnum)(priect2.GetFieldOrProperty("GameMode"));
        var gameDifficultyEnum2 = container2.Difficulty;
        var seasonEnum2 = container2.Season;
        var baseVersion2 = (int)(priect2.GetFieldOrProperty("BaseVersion"));
        var versionEnum2 = container2.GameVersion;
        var totalPlayTime2 = container2.TotalPlayTime;
        platform.Copy(container4, container5);
        platform.Move(container2, container5); // overwrite

        platform.Move(container1, container0); // delete

        var gameModeEnum4 = (PresetGameModeEnum)(priect4.GetFieldOrProperty("GameMode"));
        var gameDifficultyEnum4 = container4.Difficulty;
        var seasonEnum4 = container4.Season;
        var baseVersion4 = (int)(priect4.GetFieldOrProperty("BaseVersion"));
        var versionEnum4 = container4.GameVersion;
        var totalPlayTime4 = container4.TotalPlayTime;
        platform.Move(container4, container9); // move

        // Assert
        var priect5 = new PrivateObject(container5);
        var priect9 = new PrivateObject(container9);

        Assert.IsFalse(container2.Exists); Assert.IsTrue(container5.Exists);
        Assert.AreEqual(gameModeEnum2, (PresetGameModeEnum)(priect5.GetFieldOrProperty("GameMode")));
        Assert.AreEqual(gameDifficultyEnum2, container5.Difficulty);
        Assert.AreEqual(seasonEnum2, container5.Season);
        Assert.AreEqual(baseVersion2, (int)(priect5.GetFieldOrProperty("BaseVersion")));
        Assert.AreEqual(versionEnum2, container5.GameVersion);
        Assert.AreEqual(totalPlayTime2, container5.TotalPlayTime);

        Assert.IsFalse(container0.Exists);
        Assert.IsFalse(container1.Exists);
        Assert.AreEqual(libNOM.io.Globals.Constants.INCOMPATIBILITY_006, container0.IncompatibilityTag);
        Assert.AreEqual(libNOM.io.Globals.Constants.INCOMPATIBILITY_006, container1.IncompatibilityTag);

        Assert.IsFalse(container4.Exists); Assert.IsTrue(container9.Exists);
        Assert.AreEqual(gameModeEnum4, (PresetGameModeEnum)(priect9.GetFieldOrProperty("GameMode")));
        Assert.AreEqual(gameDifficultyEnum4, container9.Difficulty);
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
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "4");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
            UseExternalSourcesForUserIdentification = false,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platformGog = new PlatformGog(pathGog, settings);
        var transfer = platformGog.GetSourceTransferData(1);

        var platform = new PlatformSwitch(path, settings);

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
        Assert.AreEqual(6, GetExistingContainers(platform).Count()); // + 1 + 2

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
            Assert.AreEqual(resultsGog[i].GameDifficulty, container.Difficulty);
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
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "4");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
            UseExternalSourcesForUserIdentification = false,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platformMicrosoft = new PlatformMicrosoft(pathMicrosoft, settings);
        var transfer = platformMicrosoft.GetSourceTransferData(1);

        var platform = new PlatformSwitch(path, settings);

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
        Assert.AreEqual(6, GetExistingContainers(platform).Count()); // + 1 + 2

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
            Assert.AreEqual(resultsMicrosoft[i].GameDifficulty, container.Difficulty);
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
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "4");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
            UseExternalSourcesForUserIdentification = false,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platformPlaystation = new PlatformPlaystation(pathPlaystation, settings);
        var transfer = platformPlaystation.GetSourceTransferData(1);

        var platform = new PlatformSwitch(path, settings);

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
        Assert.AreEqual(6, GetExistingContainers(platform).Count()); // + 1 + 2

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
            Assert.AreEqual(resultsPlaystation[i].GameDifficulty, container.Difficulty);
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
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "4");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
            UseExternalSourcesForUserIdentification = false,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platformPlaystation = new PlatformPlaystation(pathPlaystation, settings);
        var transfer = platformPlaystation.GetSourceTransferData(1);

        var platform = new PlatformSwitch(path, settings);

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
        Assert.AreEqual(6, GetExistingContainers(platform).Count()); // + 1 + 2

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
            Assert.AreEqual(resultsPlaystation[i].GameDifficulty, container.Difficulty);
            Assert.AreEqual(resultsPlaystation[i].Season, container.Season);
            Assert.AreEqual(resultsPlaystation[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(resultsPlaystation[i].Version, container.GameVersion);
        }
    }

    [TestMethod]
    public void T44_TransferFromSteam()
    {
        // Arrange
        var pathSteam = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");
        var resultsSteam = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum Season, int BaseVersion, GameVersionEnum Version)[]
        {
            (2, true, false, PresetGameModeEnum.Creative, DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4127, GameVersionEnum.Companions), // 2Auto
            (3, true, false, PresetGameModeEnum.Creative, DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4127, GameVersionEnum.Companions), // 2Manual
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
        var transfer = platformSteam.GetSourceTransferData(1);

        var platform = new PlatformSwitch(path, settings);

        platform.Transfer(transfer, 2); // overwrite
        var container4 = GetOneSaveContainer(platform, 4);
        var priect4 = new PrivateObject(container4);
        var userIdentification4 = (UserIdentification)(priect4.GetFieldOrProperty("UserIdentification"));

        platform.Transfer(transfer, 3); // create
        var container6 = GetOneSaveContainer(platform, 6);
        var priect6 = new PrivateObject(container6);
        var userIdentification6 = (UserIdentification)(priect6.GetFieldOrProperty("UserIdentification"));

        // Assert
        AssertAllAreEqual(2, transfer.TransferBaseUserDecision.Count);
        Assert.AreEqual(6, GetExistingContainers(platform).Count()); // + 1 + 2

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
            Assert.AreEqual(resultsSteam[i].GameDifficulty, container.Difficulty);
            Assert.AreEqual(resultsSteam[i].Season, container.Season);
            Assert.AreEqual(resultsSteam[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(resultsSteam[i].Version, container.GameVersion);
        }
    }

    [TestMethod]
    public void T45_TransferFromSwitch()
    {
        // Arrange
        var pathSwitch = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "1");
        var resultsSwitch = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum Season, int BaseVersion, GameVersionEnum Version)[]
        {
            (0, true, false, PresetGameModeEnum.Creative, DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4139, GameVersionEnum.Endurance), // 1Auto
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
        var transfer = platformSwitch.GetSourceTransferData(0);

        var platform = new PlatformSwitch(path, settings);

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
        Assert.AreEqual(4, GetExistingContainers(platform).Count()); // + 1

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
            Assert.AreEqual(resultsSwitch[i].GameDifficulty, container.Difficulty);
            Assert.AreEqual(resultsSwitch[i].Season, container.Season);
            Assert.AreEqual(resultsSwitch[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(resultsSwitch[i].Version, container.GameVersion);
        }
    }
}
