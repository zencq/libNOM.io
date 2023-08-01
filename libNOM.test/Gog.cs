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
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, SeasonEnum Season, int BaseVersion, VersionEnum Version)[]
        {
            (0, true, true, PresetGameModeEnum.Normal, SeasonEnum.None, 4098, VersionEnum.Unknown), // 1Auto
            (1, true, true, PresetGameModeEnum.Normal, SeasonEnum.None, 4098, VersionEnum.Unknown), // 1Manual
            (2, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4135, VersionEnum.Emergence), // 2Auto
            (3, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4135, VersionEnum.Emergence), // 2Manual
            (4, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4135, VersionEnum.Emergence), // 3Auto
            (5, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4135, VersionEnum.Emergence), // 3Manual
            (6, true, false, PresetGameModeEnum.Creative, SeasonEnum.None, 4135, VersionEnum.Emergence), // 4Auto
        };
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
            UseExternalSourcesForUserIdentification = false,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformGog(path, settings);

        // Assert
        Assert.IsFalse(platform.HasAccountData);
        Assert.AreEqual(results.Length, platform.GetExistingContainers().Count());
        Assert.AreEqual(userIdentification[0], platform.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification[1], platform.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification[2], platform.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK);

        for (var i = 0; i < results.Length; i++)
        {
            var container = platform.GetSaveContainer(results[i].CollectionIndex)!;
            Assert.AreEqual(results[i].Exists, container.Exists);
            Assert.AreEqual(results[i].IsOld, container.IsOld);
            Assert.AreEqual(results[i].GameMode, container.GameModeEnum);
            Assert.AreEqual(results[i].Season, container.SeasonEnum);
            Assert.AreEqual(results[i].BaseVersion, container.BaseVersion);
            Assert.AreEqual(results[i].Version, container.VersionEnum);
        }
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
