using libNOM.io;
using libNOM.io.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace libNOM.test;


[TestClass]
[DeploymentItem("../../../Resources/TESTSUITE_ARCHIVE.zip")]
public class GameModeTest : CommonTestInitializeCleanup
{
    [TestMethod]
    public void T01_VanillaHollow()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "GameMode", "Vanilla");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };

        // Act
        var platform = new PlatformSteam(path, settings);

        var normal = platform.GetSaveContainer(4)!; // 5.hg // 3Auto
        var creative = platform.GetSaveContainer(14)!; // 15.hg // 8Auto
        var survival = platform.GetSaveContainer(8)!; // 9.hg // 5Auto
        var permadeath = platform.GetSaveContainer(12)!; // 13.hg // 7Auto

        // Assert
        Assert.AreEqual(PresetGameModeEnum.Normal, GetPrivateFieldOrProperty<PresetGameModeEnum>(normal, "GameMode"));
        Assert.AreEqual(PresetGameModeEnum.Creative, GetPrivateFieldOrProperty<PresetGameModeEnum>(creative, "GameMode"));
        Assert.AreEqual(PresetGameModeEnum.Survival, GetPrivateFieldOrProperty<PresetGameModeEnum>(survival, "GameMode"));
        Assert.AreEqual(PresetGameModeEnum.Permadeath, GetPrivateFieldOrProperty<PresetGameModeEnum>(permadeath, "GameMode"));

        Assert.AreEqual(DifficultyPresetTypeEnum.Normal, normal.GameDifficulty);
        Assert.AreEqual(DifficultyPresetTypeEnum.Creative, creative.GameDifficulty);
        Assert.AreEqual(DifficultyPresetTypeEnum.Survival, survival.GameDifficulty);
        Assert.AreEqual(DifficultyPresetTypeEnum.Permadeath, permadeath.GameDifficulty);
    }

    [TestMethod]
    public void T02_VanillaFull()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "GameMode", "Vanilla");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };

        // Act
        var platform = new PlatformSteam(path, settings);

        var normal = platform.GetSaveContainer(4)!; // 5.hg // 3Auto
        var creative = platform.GetSaveContainer(14)!; // 15.hg // 8Auto
        var survival = platform.GetSaveContainer(8)!; // 9.hg // 5Auto
        var permadeath = platform.GetSaveContainer(12)!; // 13.hg // 7Auto

        // Assert
        Assert.AreEqual(PresetGameModeEnum.Normal, GetPrivateFieldOrProperty<PresetGameModeEnum>(normal, "GameMode"));
        Assert.AreEqual(PresetGameModeEnum.Creative, GetPrivateFieldOrProperty<PresetGameModeEnum>(creative, "GameMode"));
        Assert.AreEqual(PresetGameModeEnum.Survival, GetPrivateFieldOrProperty<PresetGameModeEnum>(survival, "GameMode"));
        Assert.AreEqual(PresetGameModeEnum.Permadeath, GetPrivateFieldOrProperty<PresetGameModeEnum>(permadeath, "GameMode"));

        Assert.AreEqual(DifficultyPresetTypeEnum.Normal, normal.GameDifficulty);
        Assert.AreEqual(DifficultyPresetTypeEnum.Creative, creative.GameDifficulty);
        Assert.AreEqual(DifficultyPresetTypeEnum.Survival, survival.GameDifficulty);
        Assert.AreEqual(DifficultyPresetTypeEnum.Permadeath, permadeath.GameDifficulty);
    }

    [TestMethod]
    public void T03_CustomHollow()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "GameMode", "Custom");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };

        // Act
        var platform = new PlatformSteam(path, settings);

        var normal = platform.GetSaveContainer(16)!; // 17.hg // 9Auto
        var creative = platform.GetSaveContainer(4)!; // 5.hg // 3Auto
        var survival = platform.GetSaveContainer(12)!; // 13.hg // 7Auto
        var relaxed = platform.GetSaveContainer(18)!; // 19.hg // 10Auto
        var permadeath = platform.GetSaveContainer(14)!; // 15.hg // 8Auto
        var seasonal = platform.GetSaveContainer(8)!; // 9.hg // 5Auto

        // Assert
        Assert.AreEqual(PresetGameModeEnum.Normal, GetPrivateFieldOrProperty<PresetGameModeEnum>(normal, "GameMode"));
        Assert.AreEqual(PresetGameModeEnum.Normal, GetPrivateFieldOrProperty<PresetGameModeEnum>(creative, "GameMode"));
        Assert.AreEqual(PresetGameModeEnum.Normal, GetPrivateFieldOrProperty<PresetGameModeEnum>(survival, "GameMode"));
        Assert.AreEqual(PresetGameModeEnum.Normal, GetPrivateFieldOrProperty<PresetGameModeEnum>(relaxed, "GameMode"));
        Assert.AreEqual(PresetGameModeEnum.Permadeath, GetPrivateFieldOrProperty<PresetGameModeEnum>(permadeath, "GameMode"));
        Assert.AreEqual(PresetGameModeEnum.Seasonal, GetPrivateFieldOrProperty<PresetGameModeEnum>(seasonal, "GameMode"));

        Assert.AreEqual(DifficultyPresetTypeEnum.Normal, normal.GameDifficulty);
        Assert.AreEqual(DifficultyPresetTypeEnum.Creative, creative.GameDifficulty);
        Assert.AreEqual(DifficultyPresetTypeEnum.Survival, survival.GameDifficulty);
        Assert.AreEqual(DifficultyPresetTypeEnum.Relaxed, relaxed.GameDifficulty);
        Assert.AreEqual(DifficultyPresetTypeEnum.Permadeath, permadeath.GameDifficulty);
        Assert.AreEqual(DifficultyPresetTypeEnum.Normal, seasonal.GameDifficulty);
    }

    [TestMethod]
    public void T04_CustomFull()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "GameMode", "Custom");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };

        // Act
        var platform = new PlatformSteam(path, settings);

        var normal = platform.GetSaveContainer(16)!; // 17.hg // 9Auto
        var creative = platform.GetSaveContainer(4)!; // 5.hg // 3Auto
        var survival = platform.GetSaveContainer(12)!; // 13.hg // 7Auto
        var relaxed = platform.GetSaveContainer(18)!; // 19.hg // 10Auto
        var permadeath = platform.GetSaveContainer(14)!; // 15.hg // 8Auto
        var seasonal = platform.GetSaveContainer(8)!; // 9.hg // 5Auto

        // Assert
        Assert.AreEqual(PresetGameModeEnum.Normal, GetPrivateFieldOrProperty<PresetGameModeEnum>(normal, "GameMode"));
        Assert.AreEqual(PresetGameModeEnum.Normal, GetPrivateFieldOrProperty<PresetGameModeEnum>(creative, "GameMode"));
        Assert.AreEqual(PresetGameModeEnum.Normal, GetPrivateFieldOrProperty<PresetGameModeEnum>(survival, "GameMode"));
        Assert.AreEqual(PresetGameModeEnum.Normal, GetPrivateFieldOrProperty<PresetGameModeEnum>(relaxed, "GameMode"));
        Assert.AreEqual(PresetGameModeEnum.Permadeath, GetPrivateFieldOrProperty<PresetGameModeEnum>(permadeath, "GameMode"));
        Assert.AreEqual(PresetGameModeEnum.Seasonal, GetPrivateFieldOrProperty<PresetGameModeEnum>(seasonal, "GameMode"));

        Assert.AreEqual(DifficultyPresetTypeEnum.Normal, normal.GameDifficulty);
        Assert.AreEqual(DifficultyPresetTypeEnum.Creative, creative.GameDifficulty);
        Assert.AreEqual(DifficultyPresetTypeEnum.Survival, survival.GameDifficulty);
        Assert.AreEqual(DifficultyPresetTypeEnum.Relaxed, relaxed.GameDifficulty);
        Assert.AreEqual(DifficultyPresetTypeEnum.Permadeath, permadeath.GameDifficulty);
        Assert.AreEqual(DifficultyPresetTypeEnum.Normal, seasonal.GameDifficulty);
    }
}
