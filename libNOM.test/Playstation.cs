using System.Text;

using CommunityToolkit.Diagnostics;

using libNOM.io;
using libNOM.io.Interfaces;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace libNOM.test;


// Do not use System.Range for simplicity of the file and performance is not critical.
[TestClass]
[DeploymentItem("../../../Resources/TESTSUITE_ARCHIVE_PLATFORM_GOG.zip")]
[DeploymentItem("../../../Resources/TESTSUITE_ARCHIVE_PLATFORM_MICROSOFT.zip")]
[DeploymentItem("../../../Resources/TESTSUITE_ARCHIVE_PLATFORM_PLAYSTATION.zip")]
[DeploymentItem("../../../Resources/TESTSUITE_ARCHIVE_PLATFORM_STEAM.zip")]
[DeploymentItem("../../../Resources/TESTSUITE_ARCHIVE_PLATFORM_SWITCH.zip")]
public class PlaystationTest : CommonTestClass
{
    #region Constant

    protected const uint MEMORYDAT_LENGTH_ACCOUNTDATA = 0x40000U;
    protected const uint MEMORYDAT_LENGTH_CONTAINER = 0x300000U;
    protected const uint MEMORYDAT_LENGTH_TOTAL = 0x2000000U; // 32 MB

    protected static int MEMORYDAT_META_INDEX_OFFSET => _usesSaveWizard ? 8 : 3;
    protected static int MEMORYDAT_META_INDEX_LENGTH => _usesSaveWizard ? 7 : 2;

    protected static int MEMORYDAT_OFFSET_META => _usesSaveWizard ? 0x40 : 0x0; // 64 : 0
    protected static uint MEMORYDAT_OFFSET_DATA => _usesSaveWizard ? 0x1040U : 0x20000U;
    protected const uint MEMORYDAT_OFFSET_CONTAINER = 0xE0000U;

    protected const uint META_HEADER = 0xCA55E77E;
    protected static int META_LENGTH_TOTAL_VANILLA => (_usesSaveWizard ? 0x30 : 0x20); // 48 : 32
    protected static int META_LENGTH_TOTAL_WAYPOINT => (_usesSaveWizard ? 0x70 : 0x0); // 28 : 0

    protected const string SAVEWIZARD_HEADER = "NOMANSKY";
    protected static readonly byte[] SAVEWIZARD_HEADER_BINARY = Encoding.UTF8.GetBytes(SAVEWIZARD_HEADER);
    protected const int SAVEWIZARD_VERSION_1 = 1;
    protected const int SAVEWIZARD_VERSION_2 = 2;

    #endregion

    #region Meta

    private static uint[] DecryptMeta(IContainer container)
    {
        byte[] meta = [];

        if (container.MetaFile?.Exists == true)
        {
            using var reader = new BinaryReader(File.Open(container.MetaFile!.FullName, FileMode.Open, FileAccess.Read, FileShare.Read));
            if (_usesSaveStreaming)
            {
                if (_usesSaveWizard)
                {
                    meta = reader.ReadBytes(META_LENGTH_TOTAL_WAYPOINT);
                }
            }
            else
            {
                reader.BaseStream.Seek(MEMORYDAT_OFFSET_META + (container.MetaIndex * META_LENGTH_TOTAL_VANILLA), SeekOrigin.Begin);
                meta = reader.ReadBytes(META_LENGTH_TOTAL_VANILLA);
            }
        }

        return ToUInt32(meta);
    }

    private static void AssertCommonMeta(IContainer _, uint[] metaA, uint[] metaB)
    {
        Assert.HasCount(metaA.Length, metaB);

        if (_usesSaveStreaming)
        {
            if (metaA.Length == META_LENGTH_TOTAL_WAYPOINT / sizeof(uint))
            {
                if (_usesSaveWizard)
                {
                    AssertAllAreEqual(BitConverter.ToUInt32(SAVEWIZARD_HEADER_BINARY, 0), metaA[0], metaB[0]);
                    AssertAllAreEqual(BitConverter.ToUInt32(SAVEWIZARD_HEADER_BINARY, 4), metaA[1], metaB[1]);
                    AssertAllAreEqual(2, metaA[2], metaB[2]);
                    AssertAllAreEqual((uint)(MEMORYDAT_OFFSET_META), metaA[3], metaB[3]);
                    AssertAllAreEqual(1, metaA[4], metaB[4]);

                    AssertAllZero(metaA.Skip(6).Take(11), metaB.Skip(6).Take(11));
                    AssertAllAreEqual(META_FORMAT_2, metaA[17], metaB[17]);
                    AssertAllZero(metaA.Skip(18).Take(5), metaB.Skip(6).Take(5));

                    AssertAllZero(metaA[24], metaB[24]);
                    AssertAllAreEqual(1, metaA[25], metaB[25]);
                    AssertAllZero(metaA.Skip(26), metaB.Skip(26));
                }
            }
            else
                throw new AssertFailedException();
        }
        else
        {
            if (metaA.Length == META_LENGTH_TOTAL_VANILLA / sizeof(uint))
            {
                // Nothing to do as already done in AssertMemoryDat().
            }
            else
                throw new AssertFailedException();
        }
    }

    private static void AssertSpecificMeta(WriteResults results, IContainer containerA, IContainer containerB, uint[] metaA, uint[] metaB)
    {
        if (!_usesSaveStreaming)
            AssertAllAreEqual(results.MetaIndex, (uint)(containerA.MetaIndex), (uint)(containerB.MetaIndex), metaA[5], metaB[5]);
    }

    #endregion

    #region Field

    private static bool _usesSaveStreaming;
    private static bool _usesSaveWizard;

    #endregion

    #region memory.dat

    /// <see cref="PlatformMicrosoft.ParseContainersIndex()"/>
    private static void AssertMemoryDat(byte[] memoryDatA, byte[] memoryDatB)
    {
        if (_usesSaveWizard)
        {
            AssertAllAreEqual(SAVEWIZARD_HEADER_BINARY, memoryDatA.Take(SAVEWIZARD_HEADER.Length), memoryDatB.Take(SAVEWIZARD_HEADER.Length));
            //try
            //{
            AssertAllAreEqual(META_FORMAT_1, BitConverter.ToInt32(memoryDatA, 8), BitConverter.ToInt32(memoryDatB, 8));
            //}
            //catch (AssertFailedException)
            //{
            //    AssertAllAreEqual(SAVEWIZARD_VERSION_1, BitConverter.ToInt32(memoryDatA, 8), BitConverter.ToInt32(memoryDatB, 8));
            //}
            AssertAllAreEqual(0x40, BitConverter.ToInt32(memoryDatA, 12), BitConverter.ToInt32(memoryDatB, 12));
            AssertAllAreEqual(11, BitConverter.ToInt32(memoryDatA, 16), BitConverter.ToInt32(memoryDatB, 16));
            AssertAllAreEqual(11, BitConverter.ToInt32(memoryDatA, 16), BitConverter.ToInt32(memoryDatB, 16));
            AssertAllAreEqual(MEMORYDAT_LENGTH_TOTAL, BitConverter.ToInt32(memoryDatA, 20), BitConverter.ToInt32(memoryDatB, 20));
        }
        else
        {
            AssertAllAreEqual(memoryDatA.Length, memoryDatB.Length);
            AssertAllAreEqual(MEMORYDAT_LENGTH_TOTAL, memoryDatA.Length, memoryDatB.Length);
        }

        var metaA = new uint[12][]; // 5 slots and 1 account
        var metaB = new uint[12][];

        for (int i = 0; i < metaA.Length; i++)
        {
            metaA[i] = ToUInt32([.. memoryDatA.Skip(MEMORYDAT_OFFSET_META + i * META_LENGTH_TOTAL_VANILLA).Take(META_LENGTH_TOTAL_VANILLA)]);
            metaB[i] = ToUInt32([.. memoryDatB.Skip(MEMORYDAT_OFFSET_META + i * META_LENGTH_TOTAL_VANILLA).Take(META_LENGTH_TOTAL_VANILLA)]);

            if (i == 1)
            {
                AssertAllZero(metaA[i], metaB[i]);
                continue;
            }

            AssertAllAreEqual(META_HEADER, metaA[i][0], metaB[i][0]);
            AssertAllAreEqual(META_FORMAT_1, metaA[i][1], metaB[i][1]);
            if (i == 0)
            {
                AssertAllAreEqual(0x20000U, metaA[i][3], metaB[i][3]); // MEMORYDAT_OFFSET_DATA
                AssertAllAreEqual(MEMORYDAT_LENGTH_ACCOUNTDATA, metaA[i][4], metaB[i][4]);
            }
            else if (i < 4 || _usesSaveWizard)
            {
                AssertAllAreEqual(MEMORYDAT_OFFSET_CONTAINER + (i - OFFSET_INDEX) * MEMORYDAT_LENGTH_CONTAINER, metaA[i][3], metaB[i][3]);
                AssertAllAreEqual(MEMORYDAT_LENGTH_CONTAINER, metaA[i][4], metaB[i][4]);
                AssertAllAreEqual(i, metaA[i][5], metaB[i][5]);
            }
            else
            {
                AssertAllAreEqual(uint.MaxValue, metaA[i][5], metaB[i][5]);
                continue;
            }

            if (_usesSaveWizard)
            {
                AssertAllAreEqual(1, metaA[i][9], metaB[i][9]);
                AssertAllZero(metaA[i].Skip(10), metaB[i].Skip(10));
            }

            if (_usesSaveWizard)
            {
                // We currently do not need the full save here.
                //var saveA = memoryDatA.Skip((int)(metaA[i][MEMORYDAT_META_INDEX_OFFSET])).Take((int)(metaA[i][MEMORYDAT_META_INDEX_SIZE])).ToArray();
                //var saveB = memoryDatB.Skip((int)(metaB[i][MEMORYDAT_META_INDEX_OFFSET])).Take((int)(metaB[i][MEMORYDAT_META_INDEX_SIZE])).ToArray();

                var headerA = memoryDatA.Skip((int)(metaA[i][MEMORYDAT_META_INDEX_OFFSET]) - SAVEWIZARD_HEADER.Length).Take(SAVEWIZARD_HEADER.Length).ToArray();
                var headerB = memoryDatB.Skip((int)(metaB[i][MEMORYDAT_META_INDEX_OFFSET]) - SAVEWIZARD_HEADER.Length).Take(SAVEWIZARD_HEADER.Length).ToArray();
                AssertAllAreEqual(SAVEWIZARD_HEADER_BINARY, headerA, headerB);
            }
            else
            {
                var saveA = memoryDatA.Skip((int)(metaA[i][MEMORYDAT_META_INDEX_OFFSET])).Take((int)(metaA[i][4])).ToArray();
                var saveB = memoryDatB.Skip((int)(metaB[i][MEMORYDAT_META_INDEX_OFFSET])).Take((int)(metaB[i][4])).ToArray();

                AssertAllNotZero(saveA.Take(0x10), saveA.Skip((int)(metaA[i][2]) - 0x11).Take(0x10), saveB.Take(0x10), saveB.Skip((int)(metaB[i][2]) - 0x11).Take(0x10));
                AssertAllZero(saveA[(int)(metaA[i][2]) - 1], saveB[(int)(metaB[i][2]) - 1]);
            }
        }
    }

    #endregion

    [TestMethod]
    public void T101_Read_0x7D1_Homebrew_1()
    {
        // Arrange
        var expectAccountData = false;
        var path = GetCombinedPath("Playstation", "0x7D1", "Homebrew", "1");
        var results = new ReadResults[]
        {
            new(0, "Slot1Auto", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4119, 4631, GameVersionEnum.BeyondWithVehicleCam, "", "", 700345),
            new(1, "Slot1Manual", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4119, 4631, GameVersionEnum.BeyondWithVehicleCam, "", "", 700424),
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonRead<PlatformPlaystation>(path, results, expectAccountData, userIdentification);
    }

    [TestMethod]
    public void T102_Read_0x7D1_Homebrew_2()
    {
        // Arrange
        var expectAccountData = false;
        var path = GetCombinedPath("Playstation", "0x7D1", "Homebrew", "2");
        var results = new ReadResults[]
        {
            new(0, "Slot1Auto", true, true, false, false, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4119, 4631, GameVersionEnum.BeyondWithVehicleCam, "", "", 245769),
            new(1, "Slot1Manual", true, true, true, false, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4098, 4610, GameVersionEnum.Unknown, "", "", 244590),
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonRead<PlatformPlaystation>(path, results, expectAccountData, userIdentification);
    }

    [TestMethod]
    public void T111_Read_0x7D1_SaveWizard_1()
    {
        // Arrange
        var expectAccountData = false;
        var path = GetCombinedPath("Playstation", "0x7D1", "SaveWizard", "1");
        var results = new ReadResults[]
        {
            new(0, "Slot1Auto", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4134, 4646, GameVersionEnum.PrismsWithByteBeatAuthor, "", "", 497116),
            new(1, "Slot1Manual", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4134, 4646, GameVersionEnum.PrismsWithByteBeatAuthor, "", "", 496677),

            new(2, "Slot2Auto", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4134, 4646, GameVersionEnum.PrismsWithByteBeatAuthor, "", "", 598862),
            new(3, "Slot2Manual", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4134, 4646, GameVersionEnum.PrismsWithByteBeatAuthor, "", "", 598818),

            new(4, "Slot3Auto", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4134, 4646, GameVersionEnum.PrismsWithByteBeatAuthor, "", "", 240664),
            new(5, "Slot3Manual", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4133, 4645, GameVersionEnum.PrismsWithByteBeatAuthor, "", "", 231306),

            new(6, "Slot4Auto", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4134, 4646, GameVersionEnum.PrismsWithByteBeatAuthor, "", "", 773342),
            new(7, "Slot4Manual", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4124, 4636, GameVersionEnum.LivingShip, "", "", 771852),

            new(8, "Slot5Auto", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Permadeath), DifficultyPresetTypeEnum.Permadeath, SeasonEnum.None, 4134, 6694, GameVersionEnum.PrismsWithByteBeatAuthor, "", "", 11005),
            new(9, "Slot5Manual", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Permadeath), DifficultyPresetTypeEnum.Permadeath, SeasonEnum.None, 4134, 6694, GameVersionEnum.PrismsWithByteBeatAuthor, "", "", 9925),
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonRead<PlatformPlaystation>(path, results, expectAccountData, userIdentification);
    }

    [TestMethod]
    public void T112_Read_0x7D1_SaveWizard_2()
    {
        // Arrange
        var expectAccountData = false;
        var path = GetCombinedPath("Playstation", "0x7D1", "SaveWizard", "2");
        var results = new ReadResults[]
        {
            new(0, "Slot1Auto", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4126, 4638, GameVersionEnum.Origins, "", "", 1708767),
            new(1, "Slot1Manual", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4134, 4646, GameVersionEnum.PrismsWithByteBeatAuthor, "", "", 1708810),
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonRead<PlatformPlaystation>(path, results, expectAccountData, userIdentification);
    }

    [TestMethod]
    public void T113_Read_0x7D1_SaveWizard_3()
    {
        // Arrange
        var expectAccountData = false;
        var path = GetCombinedPath("Playstation", "0x7D1", "SaveWizard", "3");
        var results = new ReadResults[]
        {
            new(0, "Slot1Auto", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4126, 4638, GameVersionEnum.Origins, "", "", 868213),
            new(1, "Slot1Manual", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4126, 4638, GameVersionEnum.Origins, "", "", 868293),

            new(2, "Slot2Auto", true, true, false, false, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4134, 4646, GameVersionEnum.PrismsWithByteBeatAuthor, "", "", 217),
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonRead<PlatformPlaystation>(path, results, expectAccountData, userIdentification);
    }

    [TestMethod]
    public void T121_Read_0x7D2_Homebrew_1()
    {
        // Arrange
        var expectAccountData = true;
        var path = GetCombinedPath("Playstation", "0x7D2", "Homebrew", "1");
        var results = new ReadResults[]
        {
            new(0, "Slot1Auto", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4119, 4631, GameVersionEnum.BeyondWithVehicleCam, "", "", 720722),
            new(1, "Slot1Manual", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, 4647, GameVersionEnum.Emergence, "", "", 720858),
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonRead<PlatformPlaystation>(path, results, expectAccountData, userIdentification);
    }

    [TestMethod]
    public void T131_Read_0x7D2_SaveWizard_1()
    {
        // Arrange
        var expectAccountData = false;
        var path = GetCombinedPath("Playstation", "0x7D2", "SaveWizard", "1");
        var results = new ReadResults[]
        {
            new(0, "Slot1Auto", true, true, false, false, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Creative), DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4135, 5159, GameVersionEnum.Frontiers, "", "", 26),

            new(2, "Slot2Auto", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, 4647, GameVersionEnum.Frontiers, "", "", 16328),
            new(3, "Slot2Manual", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, 4647, GameVersionEnum.Frontiers, "", "", 13744),
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonRead<PlatformPlaystation>(path, results, expectAccountData, userIdentification);
    }

    [TestMethod]
    public void T132_Read_0x7D2_SaveWizard_2()
    {
        // Arrange
        var expectAccountData = false;
        var path = GetCombinedPath("Playstation", "0x7D2", "SaveWizard", "2");
        var results = new ReadResults[]
        {
            new(2, "Slot2Auto", true, true, false, true, true, true, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4141, 4653, GameVersionEnum.WaypointWithAgileStat, "1. Haupt", "An Bord von „Sueyuan XI“-Plattform", 385220),
            new(3, "Slot2Manual", true, true, false, true, true, true, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4141, 4653, GameVersionEnum.WaypointWithAgileStat, "1. Haupt", "Auf dem Frachter (WF-4 Dawajima)", 385249),
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonRead<PlatformPlaystation>(path, results, expectAccountData, userIdentification);
    }

    [TestMethod]
    public void T133_Read_0x7D2_SaveWizard_3()
    {
        // Arrange
        var expectAccountData = false;
        var path = GetCombinedPath("Playstation", "0x7D2", "SaveWizard", "3");
        var results = new ReadResults[]
        {
            new(0, "Slot1Auto", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, 4647, GameVersionEnum.Frontiers, "", "", 868385),
            new(1, "Slot1Manual", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, 4647, GameVersionEnum.Frontiers, "", "", 868417),

            new(2, "Slot2Auto", true, true, false, false, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4134, 4646, GameVersionEnum.PrismsWithByteBeatAuthor, "", "", 217),
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonRead<PlatformPlaystation>(path, results, expectAccountData, userIdentification);
    }

    [TestMethod]
    public void T134_Read_0x7D2_SaveWizard_4()
    {
        // Arrange
        var expectAccountData = false;
        var path = GetCombinedPath("Playstation", "0x7D2", "SaveWizard", "4");
        var results = new ReadResults[]
        {
            new(0, "Slot1Auto", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4134, 4646, GameVersionEnum.PrismsWithByteBeatAuthor, "", "", 509021),
            new(1, "Slot1Manual", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4134, 4646, GameVersionEnum.PrismsWithByteBeatAuthor, "", "", 503072),

            new(2, "Slot2Auto", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, 4647, GameVersionEnum.Frontiers, "", "", 101604),
            new(3, "Slot2Manual", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, 4647, GameVersionEnum.Frontiers, "", "", 101653),

            new(4, "Slot3Auto", true, true, false, true, false, false, true, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Seasonal), DifficultyPresetTypeEnum.Normal, SeasonEnum.Cartographers, 4135, 203815, GameVersionEnum.Frontiers, "", "", 22083),
            new(5, "Slot3Manual", true, true, false, true, false, false, true, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Seasonal), DifficultyPresetTypeEnum.Normal, SeasonEnum.Cartographers, 4135, 203815, GameVersionEnum.Frontiers, "", "", 21868),
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonRead<PlatformPlaystation>(path, results, expectAccountData, userIdentification);
    }

    [TestMethod]
    public void T135_Read_0x7D2_SaveWizard_5()
    {
        // Arrange
        var expectAccountData = false;
        var path = GetCombinedPath("Playstation", "0x7D2", "SaveWizard", "5");
        var results = new ReadResults[]
        {
            new(11, "Slot6Manual", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4145, 4657, GameVersionEnum.Interceptor, "Singularity", "Aboard Riesid Station Omega", 50613),
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonRead<PlatformPlaystation>(path, results, expectAccountData, userIdentification);
    }

    [TestMethod]
    public void T136_Read_0x7D2_SaveWizard_6()
    {
        // Arrange
        var expectAccountData = true;
        var path = GetCombinedPath("Playstation", "0x7D2", "SaveWizard", "6");
        var results = new ReadResults[]
        {
            new(9, "Slot5Manual", true, true, false, true, true, true, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4146, 4658, GameVersionEnum.Echoes, "Purfex", "On freighter (Normandy SR3)", 2469490),
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonRead<PlatformPlaystation>(path, results, expectAccountData, userIdentification);
    }

    [TestMethod]
    public void T137_Read_0x7D2_SaveWizard_7()
    {
        // Arrange
        var expectAccountData = false;
        var path = GetCombinedPath("Playstation", "0x7D2", "SaveWizard", "7");
        var results = new ReadResults[]
        {
            new(3, "Slot2Manual", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4146, 4658, GameVersionEnum.Echoes, "Eggsave", "Aboard the Space Anomaly", 23349),

            new(9, "Slot5Manual", true, true, false, true, true, true, true, true, SaveContextQueryEnum.Main, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4148, 4660, GameVersionEnum.Omega, "Purfex", "Within HydroFarm Paradise", 2721229),

            new(10, "Slot6Auto", true, true, false, true, true, true, true, true, SaveContextQueryEnum.Main, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4148, 4660, GameVersionEnum.Omega, "Purfex", "Within HydroFarm Paradise", 2742841),
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonRead<PlatformPlaystation>(path, results, expectAccountData, userIdentification);
    }

    [TestMethod]
    public void T200_Write_Default_0x7D1_Homebrew()
    {
        // Arrange
        _usesSaveStreaming = false;
        _usesSaveWizard = false;

        var containerIndex = 0;
        var originUnits = 979316666; // 979.316.666
        var originUtcTicks = 637367995090000000; // 2020-09-27 10:31:49 +00:00
        var path = GetCombinedPath("Playstation", "0x7D1", "Homebrew", "1");
        var results = new WriteResults(2, uint.MaxValue, ushort.MaxValue, ushort.MaxValue, uint.MaxValue, "", "", byte.MaxValue);

        // Act
        // Assert
        var memoryDatA = File.ReadAllBytes(Path.Combine(path, "memory.dat"));

        TestCommonWriteDefaultSave<PlatformPlaystation>(path, containerIndex, originUnits, originUtcTicks, results, DecryptMeta, AssertCommonMeta, AssertSpecificMeta);

        var memoryDatB = File.ReadAllBytes(Path.Combine(path, "memory.dat"));

        AssertMemoryDat(memoryDatA, memoryDatB);
    }

    [TestMethod]
    public void T210_Write_Default_0x7D1_SaveWizard()
    {
        // Arrange
        _usesSaveStreaming = false;
        _usesSaveWizard = true;

        var containerIndex = 0;
        var originUnits = 1283166770; // 1.283.166.770
        var originUtcTicks = 637599996160000000; // 2021-06-22 23:00:16 +00:00
        var path = GetCombinedPath("Playstation", "0x7D1", "SaveWizard", "1");
        var results = new WriteResults(2, uint.MaxValue, ushort.MaxValue, ushort.MaxValue, uint.MaxValue, "", "", byte.MaxValue);

        // Act
        // Assert
        var memoryDatA = File.ReadAllBytes(Path.Combine(path, "memory.dat"));

        TestCommonWriteDefaultSave<PlatformPlaystation>(path, containerIndex, originUnits, originUtcTicks, results, DecryptMeta, AssertCommonMeta, AssertSpecificMeta);

        var memoryDatB = File.ReadAllBytes(Path.Combine(path, "memory.dat"));

        AssertMemoryDat(memoryDatA, memoryDatB);
    }

    [TestMethod]
    public void T220_Write_Default_0x7D2_Homebrew_Account()
    {
        // Arrange
        _usesSaveStreaming = true;
        _usesSaveWizard = false;

        var originMusicVolume = 80; // 80
        var originUtcTicks = 637772446600000000; // 2022-01-08 13:17:40 +00:00
        var path = GetCombinedPath("Playstation", "0x7D2", "Homebrew", "1");

        // Act
        // Assert
        TestCommonWriteDefaultAccount<PlatformPlaystation>(path, originMusicVolume, originUtcTicks, DecryptMeta, AssertCommonMeta);
    }

    [TestMethod]
    public void T221_Write_Default_0x7D2_Homebrew()
    {
        // Arrange
        _usesSaveStreaming = true;
        _usesSaveWizard = false;

        var containerIndex = 0;
        var originUnits = 1970043489; // 1.970.043.489
        var originUtcTicks = 637772446600000000; // 2022-01-08 13:17:40 +00:00
        var path = GetCombinedPath("Playstation", "0x7D2", "Homebrew", "1");
        var results = new WriteResults(6, 4139, (ushort)(PresetGameModeEnum.Creative), (ushort)(SeasonEnum.None), 51, "", "", (byte)(DifficultyPresetTypeEnum.Normal));

        // Act
        // Assert
        TestCommonWriteDefaultSave<PlatformPlaystation>(path, containerIndex, originUnits, originUtcTicks, results, DecryptMeta, AssertCommonMeta, AssertSpecificMeta);
    }

    [TestMethod]
    public void T230_Write_Default_0x7D2_SaveWizard_Account()
    {
        // Arrange
        _usesSaveStreaming = true;
        _usesSaveWizard = true;

        var originMusicVolume = 0; // 0
        var originUtcTicks = 638288037120000000; // 2023-08-28 07:15:12 +00:00
        var path = GetCombinedPath("Playstation", "0x7D2", "SaveWizard", "6");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            UseMapping = true,
        };

        // Act
        var platformA = new PlatformPlaystation(path, settings);
        var containerA = platformA.GetAccountContainer();
        Guard.IsNotNull(containerA);
        var metaA = DecryptMeta(containerA);

        // Assert
        TestCommonWriteDefaultAccount<PlatformPlaystation>(path, originMusicVolume, originUtcTicks, DecryptMeta, (_, _, _) => { });

        var platformB = new PlatformPlaystation(path, settings);
        var containerB = platformB.GetAccountContainer();
        Guard.IsNotNull(containerB);
        var metaB = DecryptMeta(containerB);

        // Fixed header and all equal except SizeDecompressed (meta[2]).
        Assert.AreEqual(META_HEADER, metaA[0], metaB[0]);
        Assert.AreEqual(META_FORMAT_2, metaA[1], metaB[1]);
        Assert.IsTrue(metaA.Skip(3).SequenceEqual(metaB.Skip(3)));
    }

    [TestMethod]
    public void T231_Write_Default_0x7D2_SaveWizard()
    {
        // Arrange
        _usesSaveStreaming = true;
        _usesSaveWizard = true;

        var containerIndex = 0;
        var originUnits = 0; // 0
        var originUtcTicks = 637669617960000000; // 2021-09-11 12:56:36 +00:00
        var path = GetCombinedPath("Playstation", "0x7D2", "SaveWizard", "1");
        var results = new WriteResults(2, 4135, (ushort)(PresetGameModeEnum.Creative), (ushort)(SeasonEnum.None), 26, "", "", (byte)(DifficultyPresetTypeEnum.Creative));

        // Act
        // Assert
        TestCommonWriteDefaultSave<PlatformPlaystation>(path, containerIndex, originUnits, originUtcTicks, results, DecryptMeta, AssertCommonMeta, AssertSpecificMeta);
    }

    [TestMethod]
    public void T240_Write_SetLastWriteTime_False()
    {
        // Arrange
        _usesSaveStreaming = false;
        _usesSaveWizard = false;

        var containerIndex = 0;
        var originUnits = 979316666; // 979.316.666
        var originUtcTicks = 637367995090000000; // 2020-09-27 10:31:49 +00:00
        var path = GetCombinedPath("Playstation", "0x7D1", "Homebrew", "1");

        // Act
        // Assert
        TestCommonWriteSetLastWriteTimeFalse<PlatformPlaystation>(path, containerIndex, originUnits, originUtcTicks);
    }

    [TestMethod]
    public void T250_Write_WriteAlways_False_0x7D1()
    {
        // Arrange
        _usesSaveStreaming = false;
        _usesSaveWizard = false;

        var containerIndex = 0;
        var path = GetCombinedPath("Playstation", "0x7D1", "Homebrew", "1");

        // Act
        // Assert
        TestCommonWriteWriteAlwaysFalse<PlatformPlaystation>(path, containerIndex);
    }

    [TestMethod]
    public void T251_Write_WriteAlways_False_0x7D2()
    {
        // Arrange
        _usesSaveStreaming = true;
        _usesSaveWizard = false;

        var containerIndex = 0;
        var path = GetCombinedPath("Playstation", "0x7D2", "Homebrew", "1");

        // Act
        // Assert
        TestCommonWriteWriteAlwaysFalse<PlatformPlaystation>(path, containerIndex);
    }

    [TestMethod]
    public void T252_Write_WriteAlways_True_0x7D1()
    {
        // Arrange
        _usesSaveStreaming = false;
        _usesSaveWizard = false;

        var containerIndex = 0;
        var path = GetCombinedPath("Playstation", "0x7D1", "Homebrew", "1");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            SetLastWriteTime = false, // no interfering
            WriteAlways = true,
        };
        var writeCallback = false;

        // Act
        var platformA = new PlatformPlaystation(path, settings);
        var containerA = platformA.GetSaveContainer(containerIndex);
        Guard.IsNotNull(containerA);

        containerA.PropertiesChangedCallback += () =>
        {
            writeCallback = true;
        };

        platformA.Load(containerA);
        containerA.DataFile!.Refresh();
        var timeOrigin = containerA.DataFile!.LastWriteTimeUtc.Ticks;

        platformA.Write(containerA);
        containerA.DataFile!.Refresh();
        var timeSet = containerA.DataFile!.LastWriteTimeUtc.Ticks;

        var platformB = new PlatformPlaystation(path, settings);
        var containerB = platformB.GetSaveContainer(containerIndex);
        Guard.IsNotNull(containerB);

        platformB.Load(containerB);
        containerB.DataFile!.Refresh();
        var timeReload = containerA.DataFile!.LastWriteTimeUtc.Ticks;

        // Assert
        Assert.IsTrue(writeCallback);

        Assert.AreNotEqual(timeOrigin, timeSet);
        Assert.AreNotEqual(timeOrigin, timeReload);

        Assert.AreEqual(timeSet, timeReload);
    }

    [TestMethod]
    public void T253_Write_WriteAlways_True_0x7D2()
    {
        // Arrange
        _usesSaveStreaming = true;
        _usesSaveWizard = false;

        var containerIndex = 0;
        var path = GetCombinedPath("Playstation", "0x7D2", "Homebrew", "1");

        // Act
        // Assert
        TestCommonWriteWriteAlwaysTrue<PlatformPlaystation>(path, containerIndex);
    }

    [TestMethod]
    public void T300_FileSystemWatcher_0x7D1()
    {
        // Arrange
        _usesSaveStreaming = false;
        _usesSaveWizard = false;

        var containerIndex = 0;
        var path = GetCombinedPath("Playstation", "0x7D1", "Homebrew", "1");
        var pathWatching = Path.Combine(path, "memory.dat");

        // Act
        // Assert
        TestCommonFileSystemWatcher<PlatformPlaystation>(path, pathWatching, containerIndex);
    }

    [TestMethod]
    public void T301_FileSystemWatcher_0x7D2()
    {
        // Arrange
        _usesSaveStreaming = true;
        _usesSaveWizard = false;

        var containerIndex = 0;
        var path = GetCombinedPath("Playstation", "0x7D2", "Homebrew", "1");
        var pathWatching = Path.Combine(path, "savedata02.hg");

        // Act
        // Assert
        TestCommonFileSystemWatcher<PlatformPlaystation>(path, pathWatching, containerIndex);
    }

    [TestMethod]
    public void T302_Copy_0x7D1()
    {
        // Arrange
        var copyOverwrite = new[] { 0, 2 }; // 1Auto -> 2Auto (overwrite)
        var copyCreate = new[] { 0, 4 }; // 1Auto -> 3Auto (create)
        var copyDelete = new[] { 6, 1 }; // 4Auto -> 1Manual (delete)
        var path = GetCombinedPath("Playstation", "0x7D1", "SaveWizard", "3");

        // Act
        // Assert
        TestCommonFileOperationCopy<PlatformPlaystation>(path, copyOverwrite, copyCreate, copyDelete);
    }

    [TestMethod]
    public void T303_Copy_0x7D2()
    {
        // Arrange
        var copyOverwrite = new[] { 0, 2 }; // 1Auto -> 2Auto (overwrite)
        var copyCreate = new[] { 0, 4 }; // 1Auto -> 3Auto (create)
        var copyDelete = new[] { 6, 1 }; // 4Auto -> 1Manual (delete)
        var path = GetCombinedPath("Playstation", "0x7D2", "SaveWizard", "3");

        // Act
        // Assert
        TestCommonFileOperationCopy<PlatformPlaystation>(path, copyOverwrite, copyCreate, copyDelete);
    }

    [TestMethod]
    public void T304_Delete_0x7D1()
    {
        // Arrange
        var deleteDelete = new[] { 0 }; // 1Auto
        var path = GetCombinedPath("Playstation", "0x7D1", "Homebrew", "1");

        // Act
        // Assert
        TestCommonFileOperationDelete<PlatformPlaystation>(path, deleteDelete);
    }

    [TestMethod]
    public void T305_Delete_0x7D2()
    {
        // Arrange
        var deleteDelete = new[] { 0 }; // 1Auto
        var path = GetCombinedPath("Playstation", "0x7D2", "Homebrew", "1");

        // Act
        // Assert
        TestCommonFileOperationDelete<PlatformPlaystation>(path, deleteDelete);
    }

    [TestMethod]
    public void T306_Move_0x7D1()
    {
        // Arrange
        var moveCopy = new[] { 2, 3 }; // 2Auto -> 2Manual
        var moveOverwrite = new[] { 2, 1 }; // 2Auto -> 1Auto (overwrite)
        var moveDelete = new[] { 4, 0 }; // 3Auto -> 1Auto (delete)
        var moveCreate = new[] { 3, 9 }; // 2Manual -> 5Manual (create)
        var path = GetCombinedPath("Playstation", "0x7D1", "SaveWizard", "3");

        // Act
        // Assert
        TestCommonFileOperationMove<PlatformPlaystation>(path, moveCopy, moveOverwrite, moveDelete, moveCreate);
    }

    [TestMethod]
    public void T307_Move_0x7D2()
    {
        // Arrange
        var moveCopy = new[] { 2, 3 }; // 2Auto -> 2Manual
        var moveOverwrite = new[] { 2, 1 }; // 2Auto -> 1Auto (overwrite)
        var moveDelete = new[] { 4, 0 }; // 3Auto -> 1Auto (delete)
        var moveCreate = new[] { 3, 9 }; // 2Manual -> 5Manual (create)
        var path = GetCombinedPath("Playstation", "0x7D2", "SaveWizard", "3");

        // Act
        // Assert
        TestCommonFileOperationMove<PlatformPlaystation>(path, moveCopy, moveOverwrite, moveDelete, moveCreate);
    }

    [TestMethod]
    public void T308_Swap_0x7D1()
    {
        // Arrange
        var path = GetCombinedPath("Playstation", "0x7D1", "SaveWizard", "1");
        var results = new ReadResults[]
        {
            // before swap 7, "Slot4Manual"
            new(8, "Slot5Auto", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4124, 4636, GameVersionEnum.LivingShip, "", "", 771852),

            // before swap 8, "Slot5Auto"
            new(7, "Slot4Manual", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Permadeath), DifficultyPresetTypeEnum.Permadeath, SeasonEnum.None, 4134, 6694, GameVersionEnum.PrismsWithByteBeatAuthor, "", "", 11005),
        };
        var swapSwap = new[] { 7, 8 }; // 4Manual <-> 5Auto

        // Act
        // Assert
        TestCommonFileOperationSwap<PlatformPlaystation>(path, results, swapSwap);
    }

    [TestMethod]
    public void T309_Swap_0x7D2()
    {
        // Arrange
        var path = GetCombinedPath("Playstation", "0x7D2", "SaveWizard", "7");
        var results = new ReadResults[]
        {
            // before swap 3, "Slot2Manual"
            new(10, "Slot6Auto", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4146, 4658, GameVersionEnum.Echoes, "Eggsave", "Aboard the Space Anomaly", 23349),

            // before swap 10, "Slot6Auto"
            new(3, "Slot2Manual", true, true, false, true, true, true, true, true, SaveContextQueryEnum.Main, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4148, 4660, GameVersionEnum.Omega, "Purfex", "Within HydroFarm Paradise", 2742841),
        };
        var swapSwap = new[] { 3, 10 }; // 2Manual <-> 6Auto

        // Act
        // Assert
        TestCommonFileOperationSwap<PlatformPlaystation>(path, results, swapSwap);
    }

    [TestMethod]
    public void T400_TransferFromGog_To_0x7D1()
    {
        // Arrange
        var pathGog = GetCombinedPath("Gog", "DefaultUser");
        var resultsGog = new ReadResults[]
        {
            new(2, "Slot2Auto", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, 4647, GameVersionEnum.Emergence, "", "", 19977),
            new(3, "Slot2Manual", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, 4647, GameVersionEnum.Emergence, "", "", 5048),
        };
        var slotGog = 1; // get Slot2
        var userDecisionsGog = 1;
        var userIdentificationGog = ReadUserIdentification(pathGog);

        var existingContainersCount = 6; // 3 + 1 (Slot2) + 2 (Slot3)
        var path = GetCombinedPath("Playstation", "0x7D1", "SaveWizard", "3");
        var transfer = new[] { 1, 2 }; // overwrite Slot2 // create Slot3
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonFileOperationTransfer<PlatformPlaystation, PlatformGog>(pathGog, path, userIdentificationGog, userIdentification, slotGog, userDecisionsGog, transfer, existingContainersCount, resultsGog);
    }

    [TestMethod]
    public void T401_TransferFromGog_To_0x7D2()
    {
        // Arrange
        var pathGog = GetCombinedPath("Gog", "DefaultUser");
        var resultsGog = new ReadResults[]
        {
            new(2, "Slot2Auto", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, 4647, GameVersionEnum.Emergence, "", "", 19977),
            new(3, "Slot2Manual", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, 4647, GameVersionEnum.Emergence, "", "", 5048),
        };
        var slotGog = 1; // get Slot2
        var userDecisionsGog = 1;
        var userIdentificationGog = ReadUserIdentification(pathGog);

        var existingContainersCount = 8; // 6 + 2 (Slot?)
        var path = GetCombinedPath("Playstation", "0x7D2", "SaveWizard", "4");
        var transfer = new[] { 2, 3 }; // overwrite Slot3 // create Slot4
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonFileOperationTransfer<PlatformPlaystation, PlatformGog>(pathGog, path, userIdentificationGog, userIdentification, slotGog, userDecisionsGog, transfer, existingContainersCount, resultsGog);
    }

    [TestMethod]
    public void T402_TransferFromMicrosoft_To_0x7D1()
    {
        // Arrange
        var pathMicrosoft = GetCombinedPath("Microsoft", "wgs", "0009000000C73498_29070100B936489ABCE8B9AF3980429C");
        var resultsMicrosoft = new ReadResults[]
        {
            new(2, "Slot2Auto", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, 4647, GameVersionEnum.Frontiers, "", "", 423841),
            new(3, "Slot2Manual", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, 4647, GameVersionEnum.Frontiers, "", "", 419023),
        };
        var slotMicrosoft = 1; // get Slot2
        var userDecisionsMicrosoft = 8;
        var userIdentificationMicrosoft = ReadUserIdentification(pathMicrosoft);

        var existingContainersCount = 6; // 3 + 1 (Slot2) + 2 (Slot3)
        var path = GetCombinedPath("Playstation", "0x7D1", "SaveWizard", "3");
        var transfer = new[] { 1, 2 }; // overwrite Slot2 // create Slot3
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonFileOperationTransfer<PlatformPlaystation, PlatformMicrosoft>(pathMicrosoft, path, userIdentificationMicrosoft, userIdentification, slotMicrosoft, userDecisionsMicrosoft, transfer, existingContainersCount, resultsMicrosoft);
    }

    [TestMethod]
    public void T403_TransferFromMicrosoft_To_0x7D2()
    {
        // Arrange
        var pathMicrosoft = GetCombinedPath("Microsoft", "wgs", "0009000000C73498_29070100B936489ABCE8B9AF3980429C");
        var resultsMicrosoft = new ReadResults[]
        {
            new(2, "Slot2Auto", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, 4647, GameVersionEnum.Frontiers, "", "", 423841),
            new(3, "Slot2Manual", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, 4647, GameVersionEnum.Frontiers, "", "", 419023),
        };
        var slotMicrosoft = 1; // get Slot2
        var userDecisionsMicrosoft = 8;
        var userIdentificationMicrosoft = ReadUserIdentification(pathMicrosoft);

        var existingContainersCount = 8; // 6 + 2 (Slot?)
        var path = GetCombinedPath("Playstation", "0x7D2", "SaveWizard", "4");
        var transfer = new[] { 2, 3 }; // overwrite Slot3 // create Slot4
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonFileOperationTransfer<PlatformPlaystation, PlatformMicrosoft>(pathMicrosoft, path, userIdentificationMicrosoft, userIdentification, slotMicrosoft, userDecisionsMicrosoft, transfer, existingContainersCount, resultsMicrosoft);
    }

    [TestMethod]
    public void T404_TransferFromPlaystation_0x7D1_To_0x7D1()
    {
        // Arrange
        var pathPlaystation = GetCombinedPath("Playstation", "0x7D1", "SaveWizard", "1");
        var resultsPlaystation = new ReadResults[]
        {
            new(6, "Slot4Auto", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4134, 4646, GameVersionEnum.PrismsWithByteBeatAuthor, "", "", 773342),
            new(7, "Slot4Manual", true, true, false, true, true, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4124, 4636, GameVersionEnum.LivingShip, "", "", 771852),
        };
        var slotPlaystation = 3; // get Slot4
        var userDecisionsPlaystation = 8;
        var userIdentificationPlaystation = ReadUserIdentification(pathPlaystation);

        var existingContainersCount = 6; // 3 + 1 (Slot2) + 2 (Slot3)
        var path = GetCombinedPath("Playstation", "0x7D1", "SaveWizard", "3");
        var transfer = new[] { 1, 2 }; // overwrite Slot2 // create Slot3
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonFileOperationTransfer<PlatformPlaystation, PlatformPlaystation>(pathPlaystation, path, userIdentificationPlaystation, userIdentification, slotPlaystation, userDecisionsPlaystation, transfer, existingContainersCount, resultsPlaystation);
    }

    [TestMethod]
    public void T405_TransferFromPlaystation_0x7D1_To_0x7D2()
    {
        // Arrange
        var pathPlaystation = GetCombinedPath("Playstation", "0x7D1", "SaveWizard", "1");
        var resultsPlaystation = new ReadResults[]
        {
            new(8, "Slot5Auto", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Permadeath), DifficultyPresetTypeEnum.Permadeath, SeasonEnum.None, 4134, 6694, GameVersionEnum.PrismsWithByteBeatAuthor, "", "", 11005),
            new(9, "Slot5Manual", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Permadeath), DifficultyPresetTypeEnum.Permadeath, SeasonEnum.None, 4134, 6694, GameVersionEnum.PrismsWithByteBeatAuthor, "", "", 9925),
        };
        var slotPlaystation = 4; // get Slot5
        var userDecisionsPlaystation = 1;
        var userIdentificationPlaystation = ReadUserIdentification(pathPlaystation);

        var existingContainersCount = 8; // 6 + 2 (Slot?)
        var path = GetCombinedPath("Playstation", "0x7D2", "SaveWizard", "4");
        var transfer = new[] { 2, 3 }; // overwrite Slot3 // create Slot4
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonFileOperationTransfer<PlatformPlaystation, PlatformPlaystation>(pathPlaystation, path, userIdentificationPlaystation, userIdentification, slotPlaystation, userDecisionsPlaystation, transfer, existingContainersCount, resultsPlaystation);
    }

    [TestMethod]
    public void T406_TransferFromPlaystation_0x7D2_To_0x7D1()
    {
        // Arrange
        var pathPlaystation = GetCombinedPath("Playstation", "0x7D2", "SaveWizard", "1");
        var resultsPlaystation = new ReadResults[]
        {
            new(0, "Slot1Auto", true, true, false, false, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Creative), DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4135, 5159, GameVersionEnum.Frontiers, "", "", 26),
        };
        var slotPlaystation = 0; // get Slot1
        var userDecisionsPlaystation = 0;
        var userIdentificationPlaystation = ReadUserIdentification(pathPlaystation);

        var existingContainersCount = 4; // 3 + 1 (Slot?)
        var path = GetCombinedPath("Playstation", "0x7D1", "SaveWizard", "3");
        var transfer = new[] { 1, 2 }; // overwrite Slot2 // create Slot3
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonFileOperationTransfer<PlatformPlaystation, PlatformPlaystation>(pathPlaystation, path, userIdentificationPlaystation, userIdentification, slotPlaystation, userDecisionsPlaystation, transfer, existingContainersCount, resultsPlaystation);
    }

    [TestMethod]
    public void T407_TransferFromPlaystation_0x7D2_To_0x7D2()
    {
        // Arrange
        var pathPlaystation = GetCombinedPath("Playstation", "0x7D2", "SaveWizard", "2");
        var resultsPlaystation = new ReadResults[]
        {
            new(2, "Slot2Auto", true, true, false, true, true, true, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4141, 4653, GameVersionEnum.WaypointWithAgileStat, "1. Haupt", "An Bord von „Sueyuan XI“-Plattform", 385220),
            new(3, "Slot2Manual", true, true, false, true, true, true, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Normal), DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4141, 4653, GameVersionEnum.WaypointWithAgileStat, "1. Haupt", "Auf dem Frachter (WF-4 Dawajima)", 385249),
        };
        var slotPlaystation = 1; // get Slot2
        var userDecisionsPlaystation = 13;
        var userIdentificationPlaystation = ReadUserIdentification(pathPlaystation);

        var existingContainersCount = 8; // 6 + 2 (Slot?)
        var path = GetCombinedPath("Playstation", "0x7D2", "SaveWizard", "4");
        var transfer = new[] { 2, 3 }; // overwrite Slot3 // create Slot4
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonFileOperationTransfer<PlatformPlaystation, PlatformPlaystation>(pathPlaystation, path, userIdentificationPlaystation, userIdentification, slotPlaystation, userDecisionsPlaystation, transfer, existingContainersCount, resultsPlaystation);
    }

    [TestMethod]
    public void T408_TransferFromSteam_To_0x7D1()
    {
        // Arrange
        var pathSteam = GetCombinedPath("Steam", "st_76561198371877533");
        var resultsSteam = new ReadResults[]
        {
            new(2, "Slot2Auto", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Creative), DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4127, 5151, GameVersionEnum.Companions, "", "", 4765),
            new(3, "Slot2Manual", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Creative), DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4127, 5151, GameVersionEnum.Companions, "", "", 4271),
        };
        var slotSteam = 1; // get Slot2
        var userDecisionsSteam = 2;
        var userIdentificationSteam = ReadUserIdentification(pathSteam);

        var existingContainersCount = 6; // 3 + 1 (Slot2) + 2 (Slot3)
        var path = GetCombinedPath("Playstation", "0x7D1", "SaveWizard", "3");
        var transfer = new[] { 1, 2 }; // overwrite Slot2 // create Slot3
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonFileOperationTransfer<PlatformPlaystation, PlatformSteam>(pathSteam, path, userIdentificationSteam, userIdentification, slotSteam, userDecisionsSteam, transfer, existingContainersCount, resultsSteam);
    }

    [TestMethod]
    public void T409_TransferFromSteam_To_0x7D2()
    {
        // Arrange
        var pathSteam = GetCombinedPath("Steam", "st_76561198371877533");
        var resultsSteam = new ReadResults[]
        {
            new(2, "Slot2Auto", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Creative), DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4127, 5151, GameVersionEnum.Companions, "", "", 4765),
            new(3, "Slot2Manual", true, true, false, true, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Creative), DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4127, 5151, GameVersionEnum.Companions, "", "", 4271),
        };
        var slotSteam = 1; // get Slot2
        var userDecisionsSteam = 2;
        var userIdentificationSteam = ReadUserIdentification(pathSteam);

        var existingContainersCount = 8; // 6 + 2 (Slot?)
        var path = GetCombinedPath("Playstation", "0x7D2", "SaveWizard", "4");
        var transfer = new[] { 2, 3 }; // overwrite Slot3 // create Slot4
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonFileOperationTransfer<PlatformPlaystation, PlatformSteam>(pathSteam, path, userIdentificationSteam, userIdentification, slotSteam, userDecisionsSteam, transfer, existingContainersCount, resultsSteam);
    }

    [TestMethod]
    public void T410_TransferFromSwitch_To_0x7D1()
    {
        // Arrange
        var pathSwitch = GetCombinedPath("Switch", "4");
        var resultsSwitch = new ReadResults[]
        {
            new(2, "Slot2Auto", true, true, false, false, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Survival), DifficultyPresetTypeEnum.Survival, SeasonEnum.None, 4139, 5675, GameVersionEnum.Endurance, "", "", 336),
        };
        var slotSwitch = 1; // get Slot2
        var userDecisionsSwitch = 0;
        var userIdentificationSwitch = ReadUserIdentification(pathSwitch);

        var existingContainersCount = 4; // 3 + 1 (Slot?)
        var path = GetCombinedPath("Playstation", "0x7D1", "SaveWizard", "3");
        var transfer = new[] { 1, 2 }; // overwrite Slot2 // create Slot3
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonFileOperationTransfer<PlatformPlaystation, PlatformSwitch>(pathSwitch, path, userIdentificationSwitch, userIdentification, slotSwitch, userDecisionsSwitch, transfer, existingContainersCount, resultsSwitch);
    }

    [TestMethod]
    public void T411_TransferFromSwitch_To_0x7D2()
    {
        // Arrange
        var pathSwitch = GetCombinedPath("Switch", "4");
        var resultsSwitch = new ReadResults[]
        {
            new(2, "Slot2Auto", true, true, false, false, false, false, false, false, SaveContextQueryEnum.DontCare, nameof(PresetGameModeEnum.Survival), DifficultyPresetTypeEnum.Survival, SeasonEnum.None, 4139, 5675, GameVersionEnum.Endurance, "", "", 336),
        };
        var slotSwitch = 1; // get Slot2
        var userDecisionsSwitch = 0;
        var userIdentificationSwitch = ReadUserIdentification(pathSwitch);

        var existingContainersCount = 6; // 6 - 1  (Slot?) + 1 (Slot?)
        var path = GetCombinedPath("Playstation", "0x7D2", "SaveWizard", "4");
        var transfer = new[] { 2, 3 }; // overwrite Slot3 // create Slot4
        var userIdentification = ReadUserIdentification(path);

        // Act
        // Assert
        TestCommonFileOperationTransfer<PlatformPlaystation, PlatformSwitch>(pathSwitch, path, userIdentificationSwitch, userIdentification, slotSwitch, userDecisionsSwitch, transfer, existingContainersCount, resultsSwitch);
    }
}
