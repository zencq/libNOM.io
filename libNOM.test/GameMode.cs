using CommunityToolkit.Diagnostics;

using libNOM.io;

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

        var normal = platform.GetSaveContainer(4); // 5.hg // 3Auto
        var creative = platform.GetSaveContainer(14); // 15.hg // 8Auto
        var survival = platform.GetSaveContainer(8); // 9.hg // 5Auto
        var permadeath = platform.GetSaveContainer(12); // 13.hg // 7Auto

        Guard.IsNotNull(normal);
        Guard.IsNotNull(creative);
        Guard.IsNotNull(survival);
        Guard.IsNotNull(permadeath);

        // Assert
        Assert.AreEqual(nameof(PresetGameModeEnum.Normal), new PrivateObject(normal).GetFieldOrProperty("GameMode").ToString());
        Assert.AreEqual(nameof(PresetGameModeEnum.Creative), new PrivateObject(creative).GetFieldOrProperty("GameMode").ToString());
        Assert.AreEqual(nameof(PresetGameModeEnum.Survival), new PrivateObject(survival).GetFieldOrProperty("GameMode").ToString());
        Assert.AreEqual(nameof(PresetGameModeEnum.Permadeath), new PrivateObject(permadeath).GetFieldOrProperty("GameMode").ToString());

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

        var normal = platform.GetSaveContainer(4); // 5.hg // 3Auto
        var creative = platform.GetSaveContainer(14); // 15.hg // 8Auto
        var survival = platform.GetSaveContainer(8); // 9.hg // 5Auto
        var permadeath = platform.GetSaveContainer(12); // 13.hg // 7Auto

        Guard.IsNotNull(normal);
        Guard.IsNotNull(creative);
        Guard.IsNotNull(survival);
        Guard.IsNotNull(permadeath);

        // Assert
        Assert.AreEqual(nameof(PresetGameModeEnum.Normal), new PrivateObject(normal).GetFieldOrProperty("GameMode").ToString());
        Assert.AreEqual(nameof(PresetGameModeEnum.Creative), new PrivateObject(creative).GetFieldOrProperty("GameMode").ToString());
        Assert.AreEqual(nameof(PresetGameModeEnum.Survival), new PrivateObject(survival).GetFieldOrProperty("GameMode").ToString());
        Assert.AreEqual(nameof(PresetGameModeEnum.Permadeath), new PrivateObject(permadeath).GetFieldOrProperty("GameMode").ToString());

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

        var normal = platform.GetSaveContainer(16); // 17.hg // 9Auto
        var creative = platform.GetSaveContainer(4); // 5.hg // 3Auto
        var survival = platform.GetSaveContainer(12); // 13.hg // 7Auto
        var relaxed = platform.GetSaveContainer(18); // 19.hg // 10Auto
        var permadeath = platform.GetSaveContainer(14); // 15.hg // 8Auto
        var seasonal = platform.GetSaveContainer(8); // 9.hg // 5Auto

        Guard.IsNotNull(normal);
        Guard.IsNotNull(creative);
        Guard.IsNotNull(survival);
        Guard.IsNotNull(relaxed);
        Guard.IsNotNull(permadeath);
        Guard.IsNotNull(seasonal);

        // Assert
        Assert.AreEqual(nameof(PresetGameModeEnum.Normal), new PrivateObject(normal).GetFieldOrProperty("GameMode").ToString());
        Assert.AreEqual(nameof(PresetGameModeEnum.Normal), new PrivateObject(creative).GetFieldOrProperty("GameMode").ToString());
        Assert.AreEqual(nameof(PresetGameModeEnum.Normal), new PrivateObject(survival).GetFieldOrProperty("GameMode").ToString());
        Assert.AreEqual(nameof(PresetGameModeEnum.Normal), new PrivateObject(relaxed).GetFieldOrProperty("GameMode").ToString());
        Assert.AreEqual(nameof(PresetGameModeEnum.Permadeath), new PrivateObject(permadeath).GetFieldOrProperty("GameMode").ToString());
        Assert.AreEqual(nameof(PresetGameModeEnum.Seasonal), new PrivateObject(seasonal).GetFieldOrProperty("GameMode").ToString());

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

        var normal = platform.GetSaveContainer(16); // 17.hg // 9Auto
        var creative = platform.GetSaveContainer(4); // 5.hg // 3Auto
        var survival = platform.GetSaveContainer(12); // 13.hg // 7Auto
        var relaxed = platform.GetSaveContainer(18); // 19.hg // 10Auto
        var permadeath = platform.GetSaveContainer(14); // 15.hg // 8Auto
        var seasonal = platform.GetSaveContainer(8); // 9.hg // 5Auto

        Guard.IsNotNull(normal);
        Guard.IsNotNull(creative);
        Guard.IsNotNull(survival);
        Guard.IsNotNull(relaxed);
        Guard.IsNotNull(permadeath);
        Guard.IsNotNull(seasonal);

        // Assert
        Assert.AreEqual(nameof(PresetGameModeEnum.Normal), new PrivateObject(normal).GetFieldOrProperty("GameMode").ToString());
        Assert.AreEqual(nameof(PresetGameModeEnum.Normal), new PrivateObject(creative).GetFieldOrProperty("GameMode").ToString());
        Assert.AreEqual(nameof(PresetGameModeEnum.Normal), new PrivateObject(survival).GetFieldOrProperty("GameMode").ToString());
        Assert.AreEqual(nameof(PresetGameModeEnum.Normal), new PrivateObject(relaxed).GetFieldOrProperty("GameMode").ToString());
        Assert.AreEqual(nameof(PresetGameModeEnum.Permadeath), new PrivateObject(permadeath).GetFieldOrProperty("GameMode").ToString());
        Assert.AreEqual(nameof(PresetGameModeEnum.Seasonal), new PrivateObject(seasonal).GetFieldOrProperty("GameMode").ToString());

        Assert.AreEqual(DifficultyPresetTypeEnum.Normal, normal.Difficulty);
        Assert.AreEqual(DifficultyPresetTypeEnum.Creative, creative.Difficulty);
        Assert.AreEqual(DifficultyPresetTypeEnum.Survival, survival.Difficulty);
        Assert.AreEqual(DifficultyPresetTypeEnum.Relaxed, relaxed.Difficulty);
        Assert.AreEqual(DifficultyPresetTypeEnum.Permadeath, permadeath.Difficulty);
        Assert.AreEqual(DifficultyPresetTypeEnum.Normal, seasonal.Difficulty);
    }
}
