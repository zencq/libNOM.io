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
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Gog", "DefaultUser");

        // Act
        // Assert
        TestCommonBuildCollection(path, PlatformEnum.Gog, PlatformEnum.Gog);
    }

    [TestMethod]
    public void T01_BuildCollection_Microsoft()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "000901F8A36808E0_29070100B936489ABCE8B9AF3980429C");

        // Act
        // Assert
        TestCommonBuildCollection(path, PlatformEnum.Microsoft, PlatformEnum.Microsoft);
    }

    [TestMethod]
    public void T02_BuildCollection_Playstation_0x7D1()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D1", "SaveWizard", "1");

        // Act
        // Assert
        TestCommonBuildCollection(path, PlatformEnum.Playstation, PlatformEnum.Playstation);
    }

    [TestMethod]
    public void T03_BuildCollection_Playstation_0x7D2()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D2", "SaveWizard", "5");

        // Act
        // Assert
        TestCommonBuildCollection(path, PlatformEnum.Playstation, PlatformEnum.Playstation);
    }

    [TestMethod]
    public void T04_BuildCollection_Steam()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198042453834");

        // Act
        // Assert
        TestCommonBuildCollection(path, PlatformEnum.Steam, PlatformEnum.Steam);
    }

    [TestMethod]
    public void T05_BuildCollection_Switch()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "4");

        // Act
        // Assert
        TestCommonBuildCollection(path, PlatformEnum.Switch, PlatformEnum.Switch);
    }

    [TestMethod]
    public void T06_BuildCollection_Different()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198042453834");

        // Act
        // Assert
        TestCommonBuildCollection(path, PlatformEnum.Steam, PlatformEnum.Microsoft);
    }

    [TestMethod]
    public void T07_BuildCollection_None()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "libNOM.io");
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
    public void T10_AnalyzePath_Gog()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Gog", "DefaultUser");

        // Act
        // Assert
        TestCommonAnalyzePath(path, PlatformEnum.Gog, PlatformEnum.Gog);
    }

    [TestMethod]
    public void T11_AnalyzePath_Microsoft()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "000901F8A36808E0_29070100B936489ABCE8B9AF3980429C");

        // Act
        // Assert
        TestCommonAnalyzePath(path, PlatformEnum.Microsoft, PlatformEnum.Microsoft);
    }

    [TestMethod]
    public void T12_AnalyzePath_Playstation_0x7D1()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D1", "SaveWizard", "1");

        // Act
        // Assert
        TestCommonAnalyzePath(path, PlatformEnum.Playstation, PlatformEnum.Playstation);
    }

    [TestMethod]
    public void T13_AnalyzePath_Playstation_0x7D2()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D2", "SaveWizard", "5");

        // Act
        // Assert
        TestCommonAnalyzePath(path, PlatformEnum.Playstation, PlatformEnum.Playstation);
    }

    [TestMethod]
    public void T14_AnalyzePath_Steam()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198042453834");

        // Act
        // Assert
        TestCommonAnalyzePath(path, PlatformEnum.Steam, PlatformEnum.Steam);
    }

    [TestMethod]
    public void T15_AnalyzePath_Switch()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "4");

        // Act
        // Assert
        TestCommonAnalyzePath(path, PlatformEnum.Switch, PlatformEnum.Switch);
    }

    [TestMethod]
    public void T16_AnalyzePath_Different()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198042453834");

        // Act
        // Assert
        TestCommonAnalyzePath(path, PlatformEnum.Steam, PlatformEnum.Microsoft);
    }

    [TestMethod]
    public void T17_AnalyzePath_None()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "libNOM.io");
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
    public void T20_AnalyzeFile_Gog()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Gog", "DefaultUser", "save.hg");
        var results = new ReadResults(0, "Slot1Auto", true, true, true, false, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4098, 4610, GameVersionEnum.Unknown, "", "", 110965);

        // Act
        // Assert
        TestCommonAnalyzeFile(path, results, PlatformEnum.Steam); // as no preferred platform is specified, Steam will be used first
    }

    [TestMethod]
    public void T21_AnalyzeFile_Microsoft()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "something", "F330AE58758945829C51B41A5BAB7D05", "C65FD0D459C24E079B42E2F982232535");
        var results = new ReadResults(0, "Slot1Auto", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4138, 4650, GameVersionEnum.Outlaws, "", "", 240851); // actually Slot1Manual but index cannot be determined

        // Act
        // Assert
        TestCommonAnalyzeFile(path, results, PlatformEnum.Microsoft);
    }

    [TestMethod]
    public void T22_AnalyzeFile_Playstation_0x7D1()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D1", "SaveWizard", "3", "memory.dat");

        // Act
        var container = PlatformCollection.AnalyzeFile(path);

        // Assert
        Assert.IsNull(container);
    }

    [TestMethod]
    public void T23_AnalyzeFile_Playstation_0x7D2()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D2", "SaveWizard", "6", "savedata11.hg");
        var results = new ReadResults(9, "Slot5Manual", true, true, false, true, true, true, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4146, 4658, GameVersionEnum.Interceptor, "Purfex", "On freighter (Normandy SR3)", 2469490);

        // Act
        // Assert
        TestCommonAnalyzeFile(path, results, PlatformEnum.Playstation);
    }

    [TestMethod]
    public void T24_AnalyzeFile_Steam()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198042453834", "save4.hg");
        var results = new ReadResults(3, "Slot2Manual", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4143, 4655, GameVersionEnum.Fractal, "Playground", "Within Rigonn-Enve Outpost", 919);

        // Act
        // Assert
        TestCommonAnalyzeFile(path, results, PlatformEnum.Steam);
    }

    [TestMethod]
    public void T25_AnalyzeFile_Switch()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "1", "savedata02.hg");
        var results = new ReadResults(0, "Slot1Auto", true, true, false, false, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Creative), DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4139, 5163, GameVersionEnum.Endurance, "", "", 18);

        // Act
        // Assert
        TestCommonAnalyzeFile(path, results, PlatformEnum.Switch);
    }
}
