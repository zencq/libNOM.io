using CommunityToolkit.Diagnostics;

using libNOM.io;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Newtonsoft.Json.Linq;

namespace libNOM.test;


[TestClass]
[DeploymentItem("../../../Resources/TESTSUITE_ARCHIVE.zip")]
public class ContainerTest : CommonTestClass
{
    protected static readonly int[] GALACTICADDRESS_INDICES = [2, 0, 1];
    protected const string GALACTICADDRESS_JSONPATH_KEY = "GALACTICADDRESS";
    protected static readonly string[] GALACTICADDRESS_JSONPATH = ["", "PlayerStateData.UniverseAddress.GalacticAddress", "", "{0}.PlayerStateData.UniverseAddress.GalacticAddress"];

    protected static readonly int[] VALIDSLOTINDICES_INDICES = [2, 3, 1];
    protected const string VALIDSLOTINDICES_JSONPATH_KEY = "VALIDSLOTINDICES";
    protected static readonly string[] VALIDSLOTINDICES_JSONPATH = ["", "PlayerStateData.Inventory.ValidSlotIndices", "", "{0}.PlayerStateData.Inventory.ValidSlotIndices"];

    [TestMethod]
    public void T01_Backup()
    {
        // Arrange
        var backupCreatedCallback = false;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };

        // Act
        var platform = new PlatformSteam(path, settings);
        var container = GetOneSaveContainer(platform, 0);
        var pattern = $"backup.{platform.PlatformEnum}.{container.MetaIndex:D2}.*.{(uint)(container.GameVersion)}.zip".ToLowerInvariant();

        container.BackupCreatedCallback += (backup) =>
        {
            backupCreatedCallback = true;
        };

        var backups0Container = container.BackupCollection.Count;
        var backups0File = Directory.Exists(settings.Backup);

        platform.Backup(container);
        var backups1Container = container.BackupCollection.Count;
        var backups1File = Directory.GetFiles(settings.Backup, pattern).Length;

        platform.Backup(container);
        var backups2Container = container.BackupCollection.Count;
        var backups2File = Directory.GetFiles(settings.Backup, pattern).Length;

        var backups2ContainerNew = GetOneSaveContainer(new PlatformSteam(path, settings), 0).BackupCollection.Count;
        var backups2FileAfter = Directory.GetFiles(settings.Backup, pattern).Length;

        // Assert
        Assert.IsTrue(backupCreatedCallback);

        Assert.AreEqual(0, backups0Container); Assert.IsFalse(backups0File);

        AssertAllAreEqual(1, backups1Container, backups1File);
        AssertAllAreEqual(2, backups2Container, backups2File);

        AssertAllAreEqual(2, backups2ContainerNew, backups2FileAfter);
    }

    [TestMethod]
    public void T02_Restore()
    {
        // Arrange
        var backupRestoredCallback = false;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };

        // Act
        var platform = new PlatformSteam(path, settings);
        var container = GetOneSaveContainer(platform, 0);

        container.BackupRestoredCallback += () =>
        {
            backupRestoredCallback = true;
        };

        platform.Backup(container);

        var backup = container.BackupCollection.First();
        platform.Restore(backup);

        // Assert
        Assert.AreEqual(1, container.BackupCollection.Count);

        Assert.IsFalse(container.IsSynced);
        Assert.IsTrue(backupRestoredCallback);
    }

    [TestMethod]
    public void T10_JsonValue_Path()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Current,
            UseMapping = true,
        };

        libNOM.io.Globals.Constants.JSONPATH_EXTENSION.Add(UNITS_JSONPATH_KEY, UNITS_JSONPATH);

        // Act
        var platform = new PlatformSteam(path, settings);
        var container = platform.GetSaveContainer(0);
        Guard.IsNotNull(container);

        platform.Load(container);
        var units1 = container.GetJsonValue<int>(UNITS_JSONPATH_KEY);

        container.SetJsonValue(UNITS_NEW_AMOUNT, UNITS_JSONPATH_KEY);
        var units2 = container.GetJsonValue<int>(UNITS_JSONPATH_KEY);

        // Assert
        Assert.IsFalse(container.IsSynced);
        Assert.AreEqual(-1221111157, units1); // 3.073.856.139
        Assert.AreEqual(UNITS_NEW_AMOUNT, units2);
    }

    [TestMethod]
    public void T11_JsonValue_Digits()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Current,
            UseMapping = true,
        };

        // Act
        var platform = new PlatformSteam(path, settings);
        var container = platform.GetSaveContainer(0);
        Guard.IsNotNull(container);

        platform.Load(container);
        var units1 = container.GetJsonValue<int>(UNITS_INDICES);

        container.SetJsonValue(UNITS_NEW_AMOUNT, UNITS_INDICES);
        var units2 = container.GetJsonValue<int>(UNITS_INDICES);

        // Assert
        Assert.IsFalse(container.IsSynced);
        Assert.AreEqual(-1221111157, units1); // 3.073.856.139
        Assert.AreEqual(UNITS_NEW_AMOUNT, units2);
    }

    [TestMethod]
    public void T20_SaveName()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "GameMode", "Custom");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Current,
        };

        // Act
        var platform = new PlatformSteam(path, settings);
        var container = platform.GetSaveContainer(16);
        Guard.IsNotNull(container);

        var name0 = container.SaveName;

        platform.Load(container);
        var name1 = container.SaveName;

        container.SaveName = "SaveName Test";
        var name2 = container.SaveName;

        // Assert
        Assert.AreEqual("Custom Normal", name0); // field
        Assert.AreEqual("Custom Normal", name1); // property
        Assert.AreEqual("SaveName Test", name2); // changed
    }

    [TestMethod]
    public void T30_SetJObject()
    {
        // Arrange
        var galacticAddress = new JObject
        {
            { "VoxelX", 1234 },
            { "VoxelY", 234 },
            { "VoxelZ", 34 },
            { "SolarSystemIndex", 4 },
            { "PlanetIndex", 4 },
        };
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Current,
            UseMapping = true,
        };

        libNOM.io.Globals.Constants.JSONPATH_EXTENSION.Add(GALACTICADDRESS_JSONPATH_KEY, GALACTICADDRESS_JSONPATH);

        // Act
        var platform = new PlatformSteam(path, settings);
        var container = platform.GetSaveContainer(0);
        Guard.IsNotNull(container);

        platform.Load(container);
        var galacticAddress_A1 = (JObject)(container.GetJsonToken(GALACTICADDRESS_JSONPATH_KEY)!);
        var galacticAddress_A2 = container.GetJsonValue<JObject>(GALACTICADDRESS_INDICES)!;

        container.SetJsonValue(galacticAddress, GALACTICADDRESS_JSONPATH_KEY);
        var galacticAddress_B1 = (JObject)(container.GetJsonToken(GALACTICADDRESS_JSONPATH_KEY)!);
        var galacticAddress_B2 = container.GetJsonValue<JObject>(GALACTICADDRESS_INDICES)!;

        // Assert
        Assert.AreEqual(galacticAddress_A1, galacticAddress_A2);
        Assert.AreEqual(1445, galacticAddress_A1["VoxelX"]);
        Assert.AreEqual(0, galacticAddress_A1["VoxelY"]);
        Assert.AreEqual(-905, galacticAddress_A1["VoxelZ"]);
        Assert.AreEqual(345, galacticAddress_A1["SolarSystemIndex"]);
        Assert.AreEqual(0, galacticAddress_A1["PlanetIndex"]);

        Assert.AreEqual(galacticAddress_B1, galacticAddress_B2);
        Assert.AreEqual(galacticAddress["VoxelX"], galacticAddress_B1["VoxelX"]);
        Assert.AreEqual(galacticAddress["VoxelY"], galacticAddress_B1["VoxelY"]);
        Assert.AreEqual(galacticAddress["VoxelZ"], galacticAddress_B1["VoxelZ"]);
        Assert.AreEqual(galacticAddress["SolarSystemIndex"], galacticAddress_B1["SolarSystemIndex"]);
        Assert.AreEqual(galacticAddress["PlanetIndex"], galacticAddress_B1["PlanetIndex"]);
    }

    [TestMethod]
    public void T31_SetJArray()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Current,
            UseMapping = true,
        };
        var validSlotIndices = new JArray { new JObject { { "X", 0 }, { "Y", 0 } } };

        libNOM.io.Globals.Constants.JSONPATH_EXTENSION.Add(VALIDSLOTINDICES_JSONPATH_KEY, VALIDSLOTINDICES_JSONPATH);

        // Act
        var platform = new PlatformSteam(path, settings);
        var container = platform.GetSaveContainer(0);
        Guard.IsNotNull(container);

        platform.Load(container);
        var validSlotIndices_A1 = (JArray)(container.GetJsonToken(VALIDSLOTINDICES_JSONPATH_KEY)!);
        var validSlotIndices_A2 = container.GetJsonValue<JArray>(VALIDSLOTINDICES_INDICES)!;

        container.SetJsonValue(validSlotIndices, VALIDSLOTINDICES_JSONPATH_KEY);
        var validSlotIndices_B1 = (JArray)(container.GetJsonToken(VALIDSLOTINDICES_JSONPATH_KEY)!);
        var validSlotIndices_B2 = container.GetJsonValue<JArray>(VALIDSLOTINDICES_INDICES)!;

        // Assert
        Assert.AreEqual(validSlotIndices_A1, validSlotIndices_A2);
        Assert.AreEqual(29, validSlotIndices_A1.Count);

        Assert.AreEqual(validSlotIndices_B1, validSlotIndices_B2);
        Assert.AreEqual(1, validSlotIndices_B1.Count);
    }
}
