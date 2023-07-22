using libNOM.io;
using libNOM.io.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace libNOM.test;

/// <summary>
/// Only testing read here as it is Steam otherwise.
/// </summary>
[TestClass]
[DeploymentItem("..\\..\\..\\Resources\\TESTSUITE_ARCHIVE.zip")]
public class GogTest : CommonTestInitializeCleanup
{
    [TestMethod]
    public void T01_Read_DefaultUser()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Gog", "DefaultUser");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformGog(path, settings);

        // Assert
        Assert.IsFalse(platform.HasAccountData);
        Assert.AreEqual(7, platform.GetExistingContainers().Count());
        Assert.AreEqual(userIdentification[0], platform.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification[1], platform.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification[2], platform.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK);

        var container0 = platform.GetSaveContainer(0)!; // 1Auto
        Assert.IsTrue(container0.Exists);
        Assert.IsTrue(container0.IsOld);
        Assert.AreEqual(PresetGameModeEnum.Normal, container0.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container0.SeasonEnum);
        Assert.AreEqual(4098, container0.BaseVersion);
        Assert.AreEqual(VersionEnum.Unknown, container0.VersionEnum);

        var container1 = platform.GetSaveContainer(1)!; // 1Manual
        Assert.IsTrue(container1.Exists);
        Assert.IsTrue(container1.IsOld);
        Assert.AreEqual(PresetGameModeEnum.Normal, container1.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container1.SeasonEnum);
        Assert.AreEqual(4098, container1.BaseVersion);
        Assert.AreEqual(VersionEnum.Unknown, container1.VersionEnum);

        var container2 = platform.GetSaveContainer(2)!; // 2Auto
        Assert.IsTrue(container2.Exists);
        Assert.IsFalse(container2.IsOld);
        Assert.AreEqual(PresetGameModeEnum.Normal, container2.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container2.SeasonEnum);
        Assert.AreEqual(4135, container2.BaseVersion);
        Assert.AreEqual(VersionEnum.Emergence, container2.VersionEnum);

        var container3 = platform.GetSaveContainer(3)!; // 2Manual
        Assert.IsTrue(container3.Exists);
        Assert.IsFalse(container3.IsOld);
        Assert.AreEqual(PresetGameModeEnum.Normal, container3.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container3.SeasonEnum);
        Assert.AreEqual(4135, container3.BaseVersion);
        Assert.AreEqual(VersionEnum.Emergence, container3.VersionEnum);

        var container4 = platform.GetSaveContainer(4)!; // 3Auto
        Assert.IsTrue(container4.Exists);
        Assert.IsFalse(container4.IsOld);
        Assert.AreEqual(PresetGameModeEnum.Normal, container4.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container4.SeasonEnum);
        Assert.AreEqual(4135, container4.BaseVersion);
        Assert.AreEqual(VersionEnum.Emergence, container4.VersionEnum);

        var container5 = platform.GetSaveContainer(5)!; // 3Manual
        Assert.IsTrue(container5.Exists);
        Assert.IsFalse(container5.IsOld);
        Assert.AreEqual(PresetGameModeEnum.Normal, container5.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container5.SeasonEnum);
        Assert.AreEqual(4135, container5.BaseVersion);
        Assert.AreEqual(VersionEnum.Emergence, container5.VersionEnum);

        var container6 = platform.GetSaveContainer(6)!; // 4Auto
        Assert.IsTrue(container6.Exists);
        Assert.IsFalse(container6.IsOld);
        Assert.AreEqual(PresetGameModeEnum.Creative, container6.GameModeEnum);
        Assert.AreEqual(SeasonEnum.None, container6.SeasonEnum);
        Assert.AreEqual(4135, container6.BaseVersion);
        Assert.AreEqual(VersionEnum.Emergence, container6.VersionEnum);
    }

    [TestMethod]
    public void TransferToGog()
    {
        // Arrange
        // Act

        // ... Read User/Read User/Transfer/Compare

        // Assert
    }

    [TestMethod]
    public void TransferToMicrosoft()
    {
        // Arrange
        // Act
        // Assert
    }

    [TestMethod]
    public void TransferToPlaystation()
    {
        // Arrange
        // Act
        // Assert
    }

    [TestMethod]
    public void TransferToSteam()
    {
        // Arrange
        // Act
        // Assert
    }

    [TestMethod]
    public void TransferToSwitch()
    {
        // Arrange
        // Act
        // Assert
    }
}
