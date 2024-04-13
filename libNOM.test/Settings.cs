using CommunityToolkit.Diagnostics;

using libNOM.io;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace libNOM.test;


[TestClass]
[DeploymentItem("../../../Resources/TESTSUITE_ARCHIVE_PLATFORM_STEAM.zip")]
public class SettingsTest : CommonTestClass
{
    [TestMethod]
    public void T00_LoadingStrategyHollow()
    {
        // Arrange
        var path = GetCombinedPath("Steam", "st_76561198371877533");
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
    public void T01_LoadingStrategyCurrent()
    {
        // Arrange
        var path = GetCombinedPath("Steam", "st_76561198371877533");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Current,
        };

        // Act
        var platform = new PlatformSteam(path, settings);
        var loadedContainers0 = GetLoadedContainers(platform).Count();

        platform.Load(platform.GetSaveContainer(1)!);
        var loadedContainers1 = GetLoadedContainers(platform).Count();

        platform.Load(platform.GetSaveContainer(2)!);
        var loadedContainers2 = GetLoadedContainers(platform).Count();

        // Assert
        Assert.AreEqual(0, loadedContainers0);
        Assert.AreEqual(1, loadedContainers1);
        Assert.AreEqual(1, loadedContainers2);
    }

    [TestMethod]
    public void T02_LoadingStrategyPartial()
    {
        // Arrange
        var path = GetCombinedPath("Steam", "st_76561198371877533");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Partial,
        };

        // Act
        var platform = new PlatformSteam(path, settings);
        var loadedContainers0 = GetLoadedContainers(platform).Count();

        platform.Load(platform.GetSaveContainer(1)!);
        var loadedContainers1 = GetLoadedContainers(platform).Count();

        platform.Load(platform.GetSaveContainer(2)!);
        var loadedContainers2 = GetLoadedContainers(platform).Count();

        // Assert
        Assert.AreEqual(0, loadedContainers0);
        Assert.AreEqual(1, loadedContainers1);
        Assert.AreEqual(2, loadedContainers2);
    }

    [TestMethod]
    public void T03_LoadingStrategyFull()
    {
        // Arrange
        var path = GetCombinedPath("Steam", "st_76561198371877533");
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
    public void T10_MaxBackupCount()
    {
        // Arrange
        var path = GetCombinedPath("Steam", "st_76561198371877533");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Current,
            MaxBackupCount = 2,
        };

        // Act
        var platform = new PlatformSteam(path, settings);
        var container = platform.GetSaveContainer(0);
        Guard.IsNotNull(container);

        var backupDirectory = platform.Settings.BackupDirectory;
        var searchPattern = $"backup.{platform.PlatformEnum}.{container.MetaIndex:D2}.*.zip".ToLowerInvariant();

        platform.Backup(container);
        var backups1 = Directory.GetFiles(backupDirectory, searchPattern).Length;

        platform.Backup(container);
        var backups2 = Directory.GetFiles(backupDirectory, searchPattern).Length;

        platform.Backup(container);
        var backups3 = Directory.GetFiles(backupDirectory, searchPattern).Length;

        // Assert
        Assert.AreEqual(1, backups1);
        Assert.AreEqual(2, backups2);
        Assert.AreEqual(2, backups3);
    }

    [TestMethod]
    public void T11_MaxBackupCount_None()
    {
        // Arrange
        var path = GetCombinedPath("Steam", "st_76561198371877533");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Current,
            MaxBackupCount = 2,
        };

        // Act
        var platform = new PlatformSteam(path, settings);
        var container = platform.GetSaveContainer(0);
        Guard.IsNotNull(container);

        var backupDirectory = platform.Settings.BackupDirectory;
        var searchPattern = $"backup.{platform.PlatformEnum}.{container.MetaIndex:D2}.*.zip".ToLowerInvariant();

        platform.Backup(container);
        var backups1 = Directory.GetFiles(backupDirectory, searchPattern).Length;

        platform.Backup(container);
        var backups2 = Directory.GetFiles(backupDirectory, searchPattern).Length;

        platform.SetSettings(settings with { MaxBackupCount = 0 });

        platform.Backup(container);
        var backups3 = Directory.GetFiles(backupDirectory, searchPattern).Length;

        // Assert
        Assert.AreEqual(1, backups1);
        Assert.AreEqual(2, backups2);
        Assert.AreEqual(0, backups3);
    }

    [TestMethod]
    public void T20_UseMapping_True()
    {
        // Arrange
        var path = GetCombinedPath("Steam", "st_76561198371877533");
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

        // Assert
        Assert.IsNull(container.GetJsonObject()!.SelectToken("F2P"));
        Assert.IsNotNull(container.GetJsonObject()!.SelectToken("Version"));
    }

    [TestMethod]
    public void T21_UseMapping_False()
    {
        // Arrange
        var path = GetCombinedPath("Steam", "st_76561198371877533");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Current,
            UseMapping = false,
        };

        // Act
        var platform = new PlatformSteam(path, settings);
        var container = platform.GetSaveContainer(0);
        Guard.IsNotNull(container);

        platform.Load(container);

        // Assert
        Assert.IsNotNull(container.GetJsonObject()!.SelectToken("F2P"));
        Assert.IsNull(container.GetJsonObject()!.SelectToken("Version"));
    }
}
