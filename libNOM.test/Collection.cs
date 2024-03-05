using libNOM.io;
using libNOM.io.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace libNOM.test;


[TestClass]
[DeploymentItem("../../../Resources/TESTSUITE_ARCHIVE.zip")]
public class CollectionTest : CommonTestClass
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
    public void T10_AnalyzeFile_Gog()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Gog", "DefaultUser", "save.hg");
        (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum Season, int BaseVersion, GameVersionEnum Version) result = (0, true, true, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4098, GameVersionEnum.Unknown); // 1Auto

        // Act
        var container = PlatformCollection.AnalyzeFile(path)!;
        var priect = new PrivateObject(container);

        // Assert
        Assert.IsTrue(container.IsLoaded);

        Assert.AreEqual(result.CollectionIndex, container.CollectionIndex);
        Assert.AreEqual(result.Exists, container.Exists);
        Assert.AreEqual(result.IsOld, container.IsOld);
        Assert.AreEqual(result.GameMode, (PresetGameModeEnum)(priect.GetFieldOrProperty("GameMode")));
        Assert.AreEqual(result.GameDifficulty, container.Difficulty);
        Assert.AreEqual(result.Season, container.Season);
        Assert.AreEqual(result.BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
        Assert.AreEqual(result.Version, container.GameVersion);
    }

    [TestMethod]
    public void T11_AnalyzeFile_Microsoft()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "something", "F330AE58758945829C51B41A5BAB7D05", "C65FD0D459C24E079B42E2F982232535");
        (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum Season, int BaseVersion, GameVersionEnum Version) result = (0, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4138, GameVersionEnum.Outlaws); // 1Auto

        // Act
        var container = PlatformCollection.AnalyzeFile(path)!;
        var priect = new PrivateObject(container);

        // Assert
        Assert.IsTrue(container.IsLoaded);

        Assert.AreEqual(result.Exists, container.Exists);
        Assert.AreEqual(result.IsOld, container.IsOld);
        Assert.AreEqual(result.GameMode, (PresetGameModeEnum)(priect.GetFieldOrProperty("GameMode")));
        Assert.AreEqual(result.GameDifficulty, container.Difficulty);
        Assert.AreEqual(result.Season, container.Season);
        Assert.AreEqual(result.BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
        Assert.AreEqual(result.Version, container.GameVersion);
    }

    [TestMethod]
    public void T12_AnalyzeFile_Playstation_0x7D1()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D1", "SaveWizard", "3", "memory.dat");

        // Act
        var container = PlatformCollection.AnalyzeFile(path);

        // Assert
        Assert.IsNull(container);
    }

    [TestMethod]
    public void T14_AnalyzeFile_Playstation_0x7D2()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D2", "SaveWizard", "6", "savedata11.hg");
        (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum Season, int BaseVersion, GameVersionEnum Version, string SaveName, string SaveSummary) result = (9, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4146, GameVersionEnum.Echoes, "Purfex", "On freighter (Normandy SR3)"); // 6Manual

        // Act
        var container = PlatformCollection.AnalyzeFile(path)!;
        var priect = new PrivateObject(container);

        // Assert
        Assert.IsTrue(container.IsLoaded);

        Assert.AreEqual(result.CollectionIndex, container.CollectionIndex);
        Assert.AreEqual(result.Exists, container.Exists);
        Assert.AreEqual(result.IsOld, container.IsOld);
        Assert.AreEqual(result.GameMode, (PresetGameModeEnum)(priect.GetFieldOrProperty("GameMode")));
        Assert.AreEqual(result.GameDifficulty, container.Difficulty);
        Assert.AreEqual(result.Season, container.Season);
        Assert.AreEqual(result.BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
        Assert.AreEqual(result.Version, container.GameVersion);
        Assert.AreEqual(result.SaveName, container.SaveName);
        Assert.AreEqual(result.SaveSummary, container.SaveSummary);
    }

    [TestMethod]
    public void T15_AnalyzeFile_Steam()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198042453834", "save4.hg");
        (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum Season, int BaseVersion, GameVersionEnum Version, string SaveName, string SaveSummary) result = (3, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4143, GameVersionEnum.Fractal, "Playground", "Within Rigonn-Enve Outpost"); // 2Manual

        // Act
        var container = PlatformCollection.AnalyzeFile(path)!;
        var priect = new PrivateObject(container);

        // Assert
        Assert.IsTrue(container.IsLoaded);

        Assert.AreEqual(result.CollectionIndex, container.CollectionIndex);
        Assert.AreEqual(result.Exists, container.Exists);
        Assert.AreEqual(result.IsOld, container.IsOld);
        Assert.AreEqual(result.GameMode, (PresetGameModeEnum)(priect.GetFieldOrProperty("GameMode")));
        Assert.AreEqual(result.GameDifficulty, container.Difficulty);
        Assert.AreEqual(result.Season, container.Season);
        Assert.AreEqual(result.BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
        Assert.AreEqual(result.Version, container.GameVersion);
        Assert.AreEqual(result.SaveName, container.SaveName);
        Assert.AreEqual(result.SaveSummary, container.SaveSummary);
    }

    [TestMethod]
    public void T16_AnalyzeFile_Switch()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "1", "savedata02.hg");
        (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum Season, int BaseVersion, GameVersionEnum Version) result = (0, true, false, PresetGameModeEnum.Creative, DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4139, GameVersionEnum.Endurance); // 1Auto

        // Act
        var container = PlatformCollection.AnalyzeFile(path)!;
        var priect = new PrivateObject(container);

        // Assert
        Assert.IsTrue(container.IsLoaded);

        Assert.AreEqual(result.CollectionIndex, container.CollectionIndex);
        Assert.AreEqual(result.Exists, container.Exists);
        Assert.AreEqual(result.IsOld, container.IsOld);
        Assert.AreEqual(result.GameMode, (PresetGameModeEnum)(priect.GetFieldOrProperty("GameMode")));
        Assert.AreEqual(result.GameDifficulty, container.Difficulty);
        Assert.AreEqual(result.Season, container.Season);
        Assert.AreEqual(result.BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
        Assert.AreEqual(result.Version, container.GameVersion);
    }
}
