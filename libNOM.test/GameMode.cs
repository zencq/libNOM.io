using libNOM.io;
using libNOM.io.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace libNOM.test;


[TestClass]
[DeploymentItem("..\\..\\..\\Resources\\TESTSUITE_ARCHIVE.zip")]
public class GameModeTest : CommonTestInitializeCleanup
{
    [TestMethod]
    public void CustomHollow()
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
        var ambient = platform.GetSaveContainer(18)!; // 19.hg // 10Auto
        var permadeath = platform.GetSaveContainer(14)!; // 15.hg // 8Auto
        var seasonal = platform.GetSaveContainer(8)!; // 9.hg // 5Auto

        // Assert
        Assert.AreEqual(PresetGameModeEnum.Normal, normal.GameModeEnum);
        Assert.AreEqual(PresetGameModeEnum.Creative, creative.GameModeEnum);
        Assert.AreEqual(PresetGameModeEnum.Survival, survival.GameModeEnum);
        Assert.AreEqual(PresetGameModeEnum.Ambient, ambient.GameModeEnum);
        Assert.AreEqual(PresetGameModeEnum.Permadeath, permadeath.GameModeEnum);
        Assert.AreEqual(PresetGameModeEnum.Seasonal, seasonal.GameModeEnum);
    }

    [TestMethod]
    public void CustomFull()
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
        var ambient = platform.GetSaveContainer(18)!; // 19.hg // 10Auto
        var permadeath = platform.GetSaveContainer(14)!; // 15.hg // 8Auto
        var seasonal = platform.GetSaveContainer(8)!; // 9.hg // 5Auto

        // Assert
        Assert.AreEqual(PresetGameModeEnum.Normal, normal.GameModeEnum);
        Assert.AreEqual(PresetGameModeEnum.Creative, creative.GameModeEnum);
        Assert.AreEqual(PresetGameModeEnum.Survival, survival.GameModeEnum);
        Assert.AreEqual(PresetGameModeEnum.Ambient, ambient.GameModeEnum);
        Assert.AreEqual(PresetGameModeEnum.Permadeath, permadeath.GameModeEnum);
        Assert.AreEqual(PresetGameModeEnum.Seasonal, seasonal.GameModeEnum);
    }

    [TestMethod]
    public void VanillaHollow()
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
        Assert.AreEqual(PresetGameModeEnum.Normal, normal.GameModeEnum);
        Assert.AreEqual(PresetGameModeEnum.Creative, creative.GameModeEnum);
        Assert.AreEqual(PresetGameModeEnum.Survival, survival.GameModeEnum);
        Assert.AreEqual(PresetGameModeEnum.Permadeath, permadeath.GameModeEnum);
    }

    [TestMethod]
    public void VanillaFull()
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
        Assert.AreEqual(PresetGameModeEnum.Normal, normal.GameModeEnum);
        Assert.AreEqual(PresetGameModeEnum.Creative, creative.GameModeEnum);
        Assert.AreEqual(PresetGameModeEnum.Survival, survival.GameModeEnum);
        Assert.AreEqual(PresetGameModeEnum.Permadeath, permadeath.GameModeEnum);
    }
}
