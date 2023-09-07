using libNOM.io;
using libNOM.io.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace libNOM.test;


[TestClass]
[DeploymentItem("..\\..\\..\\Resources\\TESTSUITE_ARCHIVE.zip")]
public class CollectionTest : CommonTestInitializeCleanup
{
    [TestMethod]
    public void T01_BuildCollection_Gog()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Gog", "DefaultUser");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };

        // Act
        var collection = new PlatformCollection(path, settings, PlatformEnum.Gog);
        var platform = collection.Get(path);

        // Assert
        Assert.AreEqual(PlatformEnum.Gog, platform.PlatformEnum);
    }

    [TestMethod]
    public void T02_BuildCollection_Microsoft()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "000901F8A36808E0_29070100B936489ABCE8B9AF3980429C");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };

        // Act
        var collection = new PlatformCollection(path, settings, PlatformEnum.Microsoft);
        var platform = collection.Get(path);

        // Assert
        Assert.AreEqual(PlatformEnum.Microsoft, platform.PlatformEnum);
    }

    [TestMethod]
    public void T03_BuildCollection_Playstation_0x7D1()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D1", "SaveWizard", "1");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };

        // Act
        var collection = new PlatformCollection(path, settings, PlatformEnum.Playstation);
        var platform = collection.Get(path);

        // Assert
        Assert.AreEqual(PlatformEnum.Playstation, platform.PlatformEnum);
    }

    [TestMethod]
    public void T04_BuildCollection_Playstation_0x7D2()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D2", "SaveWizard", "5");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };

        // Act
        var collection = new PlatformCollection(path, settings, PlatformEnum.Playstation);
        var platform = collection.Get(path);

        // Assert
        Assert.AreEqual(PlatformEnum.Playstation, platform.PlatformEnum);
    }

    [TestMethod]
    public void T05_BuildCollection_Steam()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198042453834");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };

        // Act
        var collection = new PlatformCollection(path, settings, PlatformEnum.Steam);
        var platform = collection.Get(path);

        // Assert
        Assert.AreEqual(PlatformEnum.Steam, platform.PlatformEnum);
    }

    [TestMethod]
    public void T06_BuildCollection_Switch()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "4");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };

        // Act
        var collection = new PlatformCollection(path, settings, PlatformEnum.Switch);
        var platform = collection.Get(path);

        // Assert
        Assert.AreEqual(PlatformEnum.Switch, platform.PlatformEnum);
    }

    [TestMethod]
    public void T07_BuildCollection_None()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "X");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };

        // Act
        var collection = new PlatformCollection(path, settings);

        // Assert
        Assert.ThrowsException<KeyNotFoundException>(() => collection.Get(path));
    }

    [TestMethod]
    public void T08_BuildCollection_After()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198042453834");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };

        // Act
        var collection = new PlatformCollection();
        var count1 = collection.Count();

        var platform = collection.AnalyzePath(path, settings, PlatformEnum.Steam);
        var count2 = collection.Count();

        // Assert
        Assert.AreEqual(count1 + 1, count2);
        Assert.AreEqual(PlatformEnum.Steam, platform?.PlatformEnum);
    }

    [TestMethod]
    public void T09_BuildCollection_Different()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198042453834");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };

        // Act
        var collection = new PlatformCollection(path, settings, PlatformEnum.Microsoft);
        var platform = collection.Get(path);

        // Assert
        Assert.AreEqual(PlatformEnum.Steam, platform.PlatformEnum);
    }

    [TestMethod]
    public void T10_AnalyzeFile()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198042453834");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };

        // Act
        var collection = new PlatformCollection(path, settings, PlatformEnum.Microsoft);
        var platform = collection.Get(path);

        // Assert
        Assert.AreEqual(PlatformEnum.Steam, platform.PlatformEnum);
    }
}
