using libNOM.io;
using libNOM.io.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace libNOM.test;


[TestClass]
[DeploymentItem("..\\..\\..\\Resources\\TESTSUITE_ARCHIVE.zip")]
public class ContainerTest : CommonTestInitializeCleanup
{
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
        var container = platform.GetSaveContainer(0)!;
        container.BackupCreatedCallback += (backup) =>
        {
            backupCreatedCallback = true;
        };

        var backups0 = container.BackupCollection.Count;

        platform.Backup(container);
        var backups1 = container.BackupCollection.Count;

        platform.Backup(container);
        var backups2 = container.BackupCollection.Count;

        // Assert
        Assert.IsTrue(backupCreatedCallback);

        Assert.AreEqual(0, backups0);
        Assert.AreEqual(1, backups1);
        Assert.AreEqual(2, backups2);

        Assert.AreEqual(2, new PlatformSteam(path, settings).GetSaveContainer(0)!.BackupCollection.Count);
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
        var container = platform.GetSaveContainer(0)!;
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
    public void T03_JsonValue_Path()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Current,
        };

        // Act
        var platform = new PlatformSteam(path, settings);
        var container = platform.GetSaveContainer(0)!;

        platform.Load(container);
        var units1 = container.GetJsonValue<int>(UNITS_JSON_PATH);

        container.SetJsonValue(UNITS_NEW_AMOUNT, UNITS_JSON_PATH);
        var units2 = container.GetJsonValue<int>(UNITS_JSON_PATH);

        // Assert
        Assert.IsFalse(container.IsSynced);
        Assert.AreEqual(-1221111157, units1); // 3073856139
        Assert.AreEqual(UNITS_NEW_AMOUNT, units2);
    }

    [TestMethod]
    public void T04_JsonValue_Digits()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Current,
        };

        // Act
        var platform = new PlatformSteam(path, settings);
        var container = platform.GetSaveContainer(0)!;

        platform.Load(container);
        var units1 = container.GetJsonValue<int>(UNITS_INDICES);

        container.SetJsonValue(UNITS_NEW_AMOUNT, UNITS_INDICES);
        var units2 = container.GetJsonValue<int>(UNITS_INDICES);

        // Assert
        Assert.IsFalse(container.IsSynced);
        Assert.AreEqual(-1221111157, units1); // 3073856139
        Assert.AreEqual(UNITS_NEW_AMOUNT, units2);
    }

    [TestMethod]
    public void T05_SaveName()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "GameMode", "Custom");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Current,
        };

        // Act
        var platform = new PlatformSteam(path, settings);
        var container = platform.GetSaveContainer(16)!;

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
}
