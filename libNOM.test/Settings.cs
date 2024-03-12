using libNOM.io;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace libNOM.test;


[TestClass]
[DeploymentItem("../../../Resources/TESTSUITE_ARCHIVE.zip")]
public class SettingsTest : CommonTestClass
{
    [TestMethod]
    public void LoadingStrategyHollow()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };

        // Act
        var platform = new PlatformSteam(path, settings);
        var loadedContainers = GetLoadedContainers(platform).Count();

        // Assert
        Assert.AreEqual(0, loadedContainers);
    }

    [TestMethod]
    public void LoadingStrategyCurrent()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Current,
        };

        // Act
        var platform = new PlatformSteam(path, settings);
        var loadedContainers0 = GetLoadedContainers(platform).Count();

        platform.Load(GetOneSaveContainer(platform, 1));
        var loadedContainers1 = GetLoadedContainers(platform).Count();

        platform.Load(GetOneSaveContainer(platform, 2));
        var loadedContainers2 = GetLoadedContainers(platform).Count();

        // Assert
        Assert.AreEqual(0, loadedContainers0);
        Assert.AreEqual(1, loadedContainers1);
        Assert.AreEqual(1, loadedContainers2);
    }

    [TestMethod]
    public void LoadingStrategyPartial()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Partial,
        };

        // Act
        var platform = new PlatformSteam(path, settings);
        var loadedContainers0 = GetLoadedContainers(platform).Count();

        platform.Load(GetOneSaveContainer(platform, 1));
        var loadedContainers1 = GetLoadedContainers(platform).Count();

        platform.Load(GetOneSaveContainer(platform, 2));
        var loadedContainers2 = GetLoadedContainers(platform).Count();

        // Assert
        Assert.AreEqual(0, loadedContainers0);
        Assert.AreEqual(1, loadedContainers1);
        Assert.AreEqual(2, loadedContainers2);
    }

    [TestMethod]
    public void LoadingStrategyFull()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };

        // Act
        var platform = new PlatformSteam(path, settings);
        var loadedContainers = GetLoadedContainers(platform);

        // Assert
        Assert.AreEqual(5, loadedContainers.Count());
    }

    [TestMethod]
    public void MaxBackupCount()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Current,
            MaxBackupCount = 2,
        };

        // Act
        var platform = new PlatformSteam(path, settings);

        var backupPath = platform.Settings.Backup;
        var container = GetOneSaveContainer(platform, 0);
        var searchPattern = $"backup.{platform.PlatformEnum}.{container.MetaIndex:D2}.*.zip".ToLowerInvariant();

        platform.Backup(container);
        var backups1 = Directory.GetFiles(backupPath, searchPattern).Length;

        platform.Backup(container);
        var backups2 = Directory.GetFiles(backupPath, searchPattern).Length;

        platform.Backup(container);
        var backups3 = Directory.GetFiles(backupPath, searchPattern).Length;

        // Assert
        Assert.AreEqual(1, backups1);
        Assert.AreEqual(2, backups2);
        Assert.AreEqual(2, backups3);
    }

    [TestMethod]
    public void UseMapping_True()
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
        var container = GetOneSaveContainer(platform, 0);

        platform.Load(container);

        // Assert
        Assert.IsNull(container.GetJsonObject()!.SelectToken("F2P"));
        Assert.IsNotNull(container.GetJsonObject()!.SelectToken("Version"));
    }

    [TestMethod]
    public void UseMapping_False()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Current,
            UseMapping = false,
        };

        // Act
        var platform = new PlatformSteam(path, settings);
        var container = GetOneSaveContainer(platform, 0);

        platform.Load(container);

        // Assert
        Assert.IsNotNull(container.GetJsonObject()!.SelectToken("F2P"));
        Assert.IsNull(container.GetJsonObject()!.SelectToken("Version"));
    }
}
