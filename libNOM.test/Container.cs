using libNOM.io;
using libNOM.io.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace libNOM.test;


[TestClass]
[DeploymentItem("..\\..\\..\\Resources\\TESTSUITE_ARCHIVE.zip")]
public class ContainerTest : CommonTestInitializeCleanup
{
    [TestMethod]
    public void Backup()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };

        // Act
        var platform = new PlatformSteam(path, settings);
        var container = platform.GetSaveContainer(0)!;

        var backups0 = container.BackupCollection.Count;

        platform.Backup(container);
        var backups1 = container.BackupCollection.Count;

        platform.Backup(container);
        var backups2 = container.BackupCollection.Count;

        // Assert
        Assert.AreEqual(0, backups0);
        Assert.AreEqual(1, backups1);
        Assert.AreEqual(2, backups2);

        Assert.AreEqual(2, new PlatformSteam(path, settings).GetSaveContainer(0)!.BackupCollection.Count);
    }

    [TestMethod]
    public void SaveName()
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

        platform.Load(container);

        // Assert
        Assert.AreEqual("Custom Normal", container.SaveName);
    }
}
