using libNOM.io;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace libNOM.test;

/// <summary>
/// Only testing read here as it is Steam otherwise.
/// </summary>
[TestClass]
[DeploymentItem("../../../Resources/TESTSUITE_ARCHIVE_PLATFORM_GOG.zip")]
public class GogTest : CommonTestClass
{
    [TestMethod]
    public void T101_Read_DefaultUser()
    {
        // Arrange
        var expectAccountData = false;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Gog", "DefaultUser");
        var results = new ReadResults[]
        {
            new(0, "Slot1Auto", true, true, true, false, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4098, 4610, GameVersionEnum.Unknown, "", "", 110965),
            new(1, "Slot1Manual", true, true, true, false, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4098, 4610, GameVersionEnum.Unknown, "", "", 110910),

            new(2, "Slot2Auto", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, 4647, GameVersionEnum.Emergence, "", "", 19977),
            new(3, "Slot2Manual", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, 4647, GameVersionEnum.Emergence, "", "", 5048),

            new(4, "Slot3Auto", true, true, false, true, true, true, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, 4647, GameVersionEnum.Emergence, "", "", 290804),
            new(5, "Slot3Manual", true, true, false, true, true, true, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, 4647, GameVersionEnum.Emergence, "", "", 277198),

            new(6, "Slot4Auto", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Creative), DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4135, 5159, GameVersionEnum.Emergence, "", "", 1788),
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonRead<PlatformGog>(path, results, expectAccountData, userIdentification);
    }

    [TestMethod]
    public void T200_TransferFromGog()
    {
        // Arrange
        var pathGog = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Gog", "DefaultUser");
        var resultsGog = new ReadResults[]
        {
            new(2, "Slot2Auto", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, 4647, GameVersionEnum.Emergence, "", "", 19977),
            new(3, "Slot2Manual", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, 4647, GameVersionEnum.Emergence, "", "", 5048),
        };
        var slotGog = 1; // get Slot2
        var userDecisionsGog = 1;
        var userIdentificationGog = ReadUserIdentification(pathGog);

        var existingContainersCount = 10; // 7 + 1 (Slot4) + 2 (Slot5)
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Gog", "DefaultUser");
        var transfer = new[] { 3, 4 }; // overwrite Slot4 // create Slot5
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonFileOperationTransfer<PlatformGog, PlatformGog>(pathGog, path, userIdentificationGog, userIdentification, slotGog, userDecisionsGog, transfer, existingContainersCount, resultsGog);
    }

    [TestMethod]
    public void T201_TransferFromMicrosoft()
    {
        // Arrange
        var pathMicrosoft = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "0009000000C73498_29070100B936489ABCE8B9AF3980429C");
        var resultsMicrosoft = new ReadResults[]
        {
            new(2, "Slot2Auto", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, 4647, GameVersionEnum.Frontiers, "", "", 423841),
            new(3, "Slot2Manual", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, 4647, GameVersionEnum.Frontiers, "", "", 419023),
        };
        var slotMicrosoft = 1; // get Slot2
        var userDecisionsMicrosoft = 8;
        var userIdentificationMicrosoft = ReadUserIdentification(pathMicrosoft);

        var existingContainersCount = 10; // 7 + 1 (Slot4) + 2 (Slot5)
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Gog", "DefaultUser");
        var transfer = new[] { 3, 4 }; // overwrite Slot4 // create Slot5
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonFileOperationTransfer<PlatformGog, PlatformMicrosoft>(pathMicrosoft, path, userIdentificationMicrosoft, userIdentification, slotMicrosoft, userDecisionsMicrosoft, transfer, existingContainersCount, resultsMicrosoft);
    }

    [TestMethod]
    public void T202_TransferFromPlaystation_0x7D1()
    {
        // Arrange
        var pathPlaystation = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D1", "SaveWizard", "1");
        var resultsPlaystation = new ReadResults[]
        {
            new(2, "Slot2Auto", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4134, 4646, GameVersionEnum.PrismsWithBytebeatAuthor, "", "", 598862),
            new(3, "Slot2Manual", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4134, 4646, GameVersionEnum.PrismsWithBytebeatAuthor, "", "", 598818),
        };
        var slotPlaystation = 1; // get Slot2
        var userDecisionsPlaystation = 24;
        var userIdentificationPlaystation = ReadUserIdentification(pathPlaystation);

        var existingContainersCount = 10; // 7 + 1 (Slot4) + 2 (Slot5)
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Gog", "DefaultUser");
        var transfer = new[] { 3, 4 }; // overwrite Slot4 // create Slot5
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonFileOperationTransfer<PlatformGog, PlatformPlaystation>(pathPlaystation, path, userIdentificationPlaystation, userIdentification, slotPlaystation, userDecisionsPlaystation, transfer, existingContainersCount, resultsPlaystation);
    }

    [TestMethod]
    public void T203_TransferFromPlaystation_0x7D2()
    {
        // Arrange
        var pathPlaystation = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D2", "SaveWizard", "4");
        var resultsPlaystation = new ReadResults[]
        {
            new(2, "Slot2Auto", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, 4647, GameVersionEnum.Frontiers, "", "", 101604),
            new(3, "Slot2Manual", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, 4647, GameVersionEnum.Frontiers, "", "", 101653),
        };
        var slotPlaystation = 1; // get Slot2
        var userDecisionsPlaystation = 4;
        var userIdentificationPlaystation = ReadUserIdentification(pathPlaystation);

        var existingContainersCount = 10; // 7 + 1 (Slot4) + 2 (Slot5)
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Gog", "DefaultUser");
        var transfer = new[] { 3, 4 }; // overwrite Slot4 // create Slot5
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonFileOperationTransfer<PlatformGog, PlatformPlaystation>(pathPlaystation, path, userIdentificationPlaystation, userIdentification, slotPlaystation, userDecisionsPlaystation, transfer, existingContainersCount, resultsPlaystation);
    }

    [TestMethod]
    public void T204_TransferFromSteam()
    {
        // Arrange
        var pathSteam = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");
        var resultsSteam = new ReadResults[]
        {
            new(2, "Slot2Auto", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Creative), DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4127, 5151, GameVersionEnum.Companions, "", "", 4765),
            new(3, "Slot2Manual", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Creative), DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4127, 5151, GameVersionEnum.Companions, "", "", 4271),
        };
        var slotSteam = 1; // get Slot2
        var userDecisionsSteam = 2;
        var userIdentificationSteam = ReadUserIdentification(pathSteam);

        var existingContainersCount = 10; // 7 + 1 (Slot4) + 2 (Slot5)
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Gog", "DefaultUser");
        var transfer = new[] { 3, 4 }; // overwrite Slot4 // create Slot5
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonFileOperationTransfer<PlatformGog, PlatformSteam>(pathSteam, path, userIdentificationSteam, userIdentification, slotSteam, userDecisionsSteam, transfer, existingContainersCount, resultsSteam);
    }

    [TestMethod]
    public void T205_TransferFromSwitch()
    {
        // Arrange
        var pathSwitch = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "4");
        var resultsSwitch = new ReadResults[]
        {
            new(2, "Slot2Auto", true, true, false, false, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Survival), DifficultyPresetTypeEnum.Survival, SeasonEnum.None, 4139, 5675, GameVersionEnum.Endurance, "", "", 336),
        };
        var slotSwitch = 1; // get Slot2
        var userDecisionsSwitch = 0;
        var userIdentificationSwitch = ReadUserIdentification(pathSwitch);

        var existingContainersCount = 8; // 7 + 1 (Slot?)
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Gog", "DefaultUser");
        var transfer = new[] { 3, 4 }; // overwrite Slot4 // create Slot5
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonFileOperationTransfer<PlatformGog, PlatformSwitch>(pathSwitch, path, userIdentificationSwitch, userIdentification, slotSwitch, userDecisionsSwitch, transfer, existingContainersCount, resultsSwitch);
    }
}
