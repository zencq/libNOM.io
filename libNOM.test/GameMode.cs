using libNOM.io;
using libNOM.io.Enums;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace libNOM.test;


[TestClass]
[DeploymentItem("../../../Resources/TESTSUITE_ARCHIVE.zip")]
public class GameModeTest : CommonTestClass
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

        var normal = GetOneSaveContainer(platform, 4); // 5.hg // 3Auto
        var creative = GetOneSaveContainer(platform, 14); // 15.hg // 8Auto
        var survival = GetOneSaveContainer(platform, 8); // 9.hg // 5Auto
        var permadeath = GetOneSaveContainer(platform, 12); // 13.hg // 7Auto

        // Assert
        Assert.AreEqual(PresetGameModeEnum.Normal, GetPrivateFieldOrProperty<PresetGameModeEnum>(normal, "GameMode"));
        Assert.AreEqual(PresetGameModeEnum.Creative, GetPrivateFieldOrProperty<PresetGameModeEnum>(creative, "GameMode"));
        Assert.AreEqual(PresetGameModeEnum.Survival, GetPrivateFieldOrProperty<PresetGameModeEnum>(survival, "GameMode"));
        Assert.AreEqual(PresetGameModeEnum.Permadeath, GetPrivateFieldOrProperty<PresetGameModeEnum>(permadeath, "GameMode"));

        Assert.AreEqual(DifficultyPresetTypeEnum.Normal, normal.Difficulty);
        Assert.AreEqual(DifficultyPresetTypeEnum.Creative, creative.Difficulty);
        Assert.AreEqual(DifficultyPresetTypeEnum.Survival, survival.Difficulty);
        Assert.AreEqual(DifficultyPresetTypeEnum.Permadeath, permadeath.Difficulty);
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

        var normal = GetOneSaveContainer(platform, 4); // 5.hg // 3Auto
        var creative = GetOneSaveContainer(platform, 14); // 15.hg // 8Auto
        var survival = GetOneSaveContainer(platform, 8); // 9.hg // 5Auto
        var permadeath = GetOneSaveContainer(platform, 12); // 13.hg // 7Auto

        // Assert
        Assert.AreEqual(PresetGameModeEnum.Normal, GetPrivateFieldOrProperty<PresetGameModeEnum>(normal, "GameMode"));
        Assert.AreEqual(PresetGameModeEnum.Creative, GetPrivateFieldOrProperty<PresetGameModeEnum>(creative, "GameMode"));
        Assert.AreEqual(PresetGameModeEnum.Survival, GetPrivateFieldOrProperty<PresetGameModeEnum>(survival, "GameMode"));
        Assert.AreEqual(PresetGameModeEnum.Permadeath, GetPrivateFieldOrProperty<PresetGameModeEnum>(permadeath, "GameMode"));

        Assert.AreEqual(DifficultyPresetTypeEnum.Normal, normal.Difficulty);
        Assert.AreEqual(DifficultyPresetTypeEnum.Creative, creative.Difficulty);
        Assert.AreEqual(DifficultyPresetTypeEnum.Survival, survival.Difficulty);
        Assert.AreEqual(DifficultyPresetTypeEnum.Permadeath, permadeath.Difficulty);
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

        var normal = GetOneSaveContainer(platform, 16); // 17.hg // 9Auto
        var creative = GetOneSaveContainer(platform, 4); // 5.hg // 3Auto
        var survival = GetOneSaveContainer(platform, 12); // 13.hg // 7Auto
        var relaxed = GetOneSaveContainer(platform, 18); // 19.hg // 10Auto
        var permadeath = GetOneSaveContainer(platform, 14); // 15.hg // 8Auto
        var seasonal = GetOneSaveContainer(platform, 8); // 9.hg // 5Auto

        // Assert
        Assert.AreEqual(PresetGameModeEnum.Normal, GetPrivateFieldOrProperty<PresetGameModeEnum>(normal, "GameMode"));
        Assert.AreEqual(PresetGameModeEnum.Normal, GetPrivateFieldOrProperty<PresetGameModeEnum>(creative, "GameMode"));
        Assert.AreEqual(PresetGameModeEnum.Normal, GetPrivateFieldOrProperty<PresetGameModeEnum>(survival, "GameMode"));
        Assert.AreEqual(PresetGameModeEnum.Normal, GetPrivateFieldOrProperty<PresetGameModeEnum>(relaxed, "GameMode"));
        Assert.AreEqual(PresetGameModeEnum.Permadeath, GetPrivateFieldOrProperty<PresetGameModeEnum>(permadeath, "GameMode"));
        Assert.AreEqual(PresetGameModeEnum.Seasonal, GetPrivateFieldOrProperty<PresetGameModeEnum>(seasonal, "GameMode"));

        Assert.AreEqual(DifficultyPresetTypeEnum.Normal, normal.Difficulty);
        Assert.AreEqual(DifficultyPresetTypeEnum.Creative, creative.Difficulty);
        Assert.AreEqual(DifficultyPresetTypeEnum.Survival, survival.Difficulty);
        Assert.AreEqual(DifficultyPresetTypeEnum.Relaxed, relaxed.Difficulty);
        Assert.AreEqual(DifficultyPresetTypeEnum.Permadeath, permadeath.Difficulty);
        Assert.AreEqual(DifficultyPresetTypeEnum.Normal, seasonal.Difficulty);
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

        var normal = GetOneSaveContainer(platform, 16); // 17.hg // 9Auto
        var creative = GetOneSaveContainer(platform, 4); // 5.hg // 3Auto
        var survival = GetOneSaveContainer(platform, 12); // 13.hg // 7Auto
        var relaxed = GetOneSaveContainer(platform, 18); // 19.hg // 10Auto
        var permadeath = GetOneSaveContainer(platform, 14); // 15.hg // 8Auto
        var seasonal = GetOneSaveContainer(platform, 8); // 9.hg // 5Auto

        // Assert
        Assert.AreEqual(PresetGameModeEnum.Normal, GetPrivateFieldOrProperty<PresetGameModeEnum>(normal, "GameMode"));
        Assert.AreEqual(PresetGameModeEnum.Normal, GetPrivateFieldOrProperty<PresetGameModeEnum>(creative, "GameMode"));
        Assert.AreEqual(PresetGameModeEnum.Normal, GetPrivateFieldOrProperty<PresetGameModeEnum>(survival, "GameMode"));
        Assert.AreEqual(PresetGameModeEnum.Normal, GetPrivateFieldOrProperty<PresetGameModeEnum>(relaxed, "GameMode"));
        Assert.AreEqual(PresetGameModeEnum.Permadeath, GetPrivateFieldOrProperty<PresetGameModeEnum>(permadeath, "GameMode"));
        Assert.AreEqual(PresetGameModeEnum.Seasonal, GetPrivateFieldOrProperty<PresetGameModeEnum>(seasonal, "GameMode"));

        Assert.AreEqual(DifficultyPresetTypeEnum.Normal, normal.Difficulty);
        Assert.AreEqual(DifficultyPresetTypeEnum.Creative, creative.Difficulty);
        Assert.AreEqual(DifficultyPresetTypeEnum.Survival, survival.Difficulty);
        Assert.AreEqual(DifficultyPresetTypeEnum.Relaxed, relaxed.Difficulty);
        Assert.AreEqual(DifficultyPresetTypeEnum.Permadeath, permadeath.Difficulty);
        Assert.AreEqual(DifficultyPresetTypeEnum.Normal, seasonal.Difficulty);
    }
}
