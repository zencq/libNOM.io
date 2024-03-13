using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace libNOM.test;


[TestClass]
[DeploymentItem("../../../Resources/TESTSUITE_ARCHIVE_GAMEMODE.zip")]
public class GameModeTest : CommonTestClass
{
    [TestMethod]
    public void T00_VanillaHollow()
    {
        // Arrange
        //  4 //  5.hg // 3Auto
        // 14 // 15.hg // 8Auto
        //  8 //  9.hg // 5Auto
        // 12 // 13.hg // 7Auto
        var containerIndices = new int[] { 4, 14, 8, 12 };
        var loadingStrategy = LoadingStrategyEnum.Hollow;
        var path = GetCombinedPath("GameMode", "Vanilla");

        // Act
        // Assert
        TestCommonGameModeVanilla(path, loadingStrategy, containerIndices);
    }

    [TestMethod]
    public void T01_VanillaFull()
    {
        // Arrange
        //  4 //  5.hg // 3Auto
        // 14 // 15.hg // 8Auto
        //  8 //  9.hg // 5Auto
        // 12 // 13.hg // 7Auto
        var containerIndices = new int[] { 4, 14, 8, 12 };
        var loadingStrategy = LoadingStrategyEnum.Full;
        var path = GetCombinedPath("GameMode", "Vanilla");

        // Act
        // Assert
        TestCommonGameModeVanilla(path, loadingStrategy, containerIndices);
    }

    [TestMethod]
    public void T02_CustomHollow()
    {
        // Arrange
        // 16 // 17.hg //  9Auto
        //  4 //  5.hg //  3Auto
        // 12 // 13.hg //  7Auto
        // 18 // 19.hg // 10Auto
        // 14 // 15.hg //  8Auto
        //  8 //  9.hg //  5Auto
        var containerIndices = new int[] { 16, 4, 12, 18, 14, 8 };
        var loadingStrategy = LoadingStrategyEnum.Hollow;
        var path = GetCombinedPath("GameMode", "Custom");

        // Act
        // Assert
        TestCommonGameModeCustom(path, loadingStrategy, containerIndices);
    }

    [TestMethod]
    public void T03_CustomFull()
    {
        // Arrange
        // 16 // 17.hg //  9Auto
        //  4 //  5.hg //  3Auto
        // 12 // 13.hg //  7Auto
        // 18 // 19.hg // 10Auto
        // 14 // 15.hg //  8Auto
        //  8 //  9.hg //  5Auto
        var containerIndices = new int[] { 16, 4, 12, 18, 14, 8 };
        var loadingStrategy = LoadingStrategyEnum.Full;
        var path = GetCombinedPath("GameMode", "Custom");

        // Act
        // Assert
        TestCommonGameModeCustom(path, loadingStrategy, containerIndices);
    }
}
