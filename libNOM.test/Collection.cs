using libNOM.io;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace libNOM.test;


[TestClass]
[DeploymentItem("../../../Resources/TESTSUITE_ARCHIVE_PLATFORM_GOG.zip")]
[DeploymentItem("../../../Resources/TESTSUITE_ARCHIVE_PLATFORM_MICROSOFT.zip")]
[DeploymentItem("../../../Resources/TESTSUITE_ARCHIVE_PLATFORM_PLAYSTATION.zip")]
[DeploymentItem("../../../Resources/TESTSUITE_ARCHIVE_PLATFORM_STEAM.zip")]
[DeploymentItem("../../../Resources/TESTSUITE_ARCHIVE_PLATFORM_SWITCH.zip")]
public class CollectionTest : CommonTestClass
{
    [TestMethod]
    public void T00_BuildCollection_Gog()
    {
        // Arrange
        var path = GetCombinedPath("Gog", "DefaultUser");

        // Act
        // Assert
        TestCommonBuildCollection(path, PlatformEnum.Gog, new() { PreferredPlatform = PlatformEnum.Gog });
    }

    [TestMethod]
    public void T01_BuildCollection_Microsoft()
    {
        // Arrange
        var path = GetCombinedPath("Microsoft", "wgs", "000901F8A36808E0_29070100B936489ABCE8B9AF3980429C");

        // Act
        // Assert
        TestCommonBuildCollection(path, PlatformEnum.Microsoft, new() { PreferredPlatform = PlatformEnum.Microsoft });
    }

    [TestMethod]
    public void T02_BuildCollection_Playstation_0x7D1()
    {
        // Arrange
        var path = GetCombinedPath("Playstation", "0x7D1", "SaveWizard", "1");

        // Act
        // Assert
        TestCommonBuildCollection(path, PlatformEnum.Playstation, new() { PreferredPlatform = PlatformEnum.Playstation });
    }

    [TestMethod]
    public void T03_BuildCollection_Playstation_0x7D2()
    {
        // Arrange
        var path = GetCombinedPath("Playstation", "0x7D2", "SaveWizard", "5");

        // Act
        // Assert
        TestCommonBuildCollection(path, PlatformEnum.Playstation, new() { PreferredPlatform = PlatformEnum.Playstation });
    }

    [TestMethod]
    public void T04_BuildCollection_Steam()
    {
        // Arrange
        var path = GetCombinedPath("Steam", "st_76561198042453834");

        // Act
        // Assert
        TestCommonBuildCollection(path, PlatformEnum.Steam, new() { PreferredPlatform = PlatformEnum.Steam });
    }

    [TestMethod]
    public void T05_BuildCollection_Switch()
    {
        // Arrange
        var path = GetCombinedPath("Switch", "4");

        // Act
        // Assert
        TestCommonBuildCollection(path, PlatformEnum.Switch, new() { PreferredPlatform = PlatformEnum.Switch });
    }

    [TestMethod]
    public void T06_BuildCollection_Different()
    {
        // Arrange
        var path = GetCombinedPath("Steam", "st_76561198042453834");

        // Act
        // Assert
        TestCommonBuildCollection(path, PlatformEnum.Steam, new() { PreferredPlatform = PlatformEnum.Microsoft });
    }

    [TestMethod]
    public void T07_BuildCollection_None()
    {
        // Arrange
        var path = GetCombinedPath("libNOM.io");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };

        // Act
        var collection = new PlatformCollection(path, settings);

        // Assert
        Assert.ThrowsException<KeyNotFoundException>(() => collection.Get(path));
    }
}
