using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using libNOM.io;
using libNOM.io.Data;
using libNOM.io.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;

namespace libNOM.test;


// Do not use System.Range for simplicity of the file and performace is not critical.
[TestClass]
[DeploymentItem("..\\..\\..\\Resources\\TESTSUITE_ARCHIVE.zip")]
public class PlaystationTest : CommonTestInitializeCleanup
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

    #region Field

    private static bool _usesSaveStreaming;
    private static bool _usesSaveWizard;

    #endregion

    #region Meta

    /// <see cref="Platform.ReadMeta(Container)"/>
    /// <see cref="PlatformPlaystation.DecryptMeta(Container, byte[])"/>
    private static uint[] DecryptMeta(Container container)
    {
        byte[] meta = Array.Empty<byte>();

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

        return GetUInt32(meta);
    }

    private static void AssertCommonMeta(uint[] metaA, uint[] metaB)
    {
        Assert.AreEqual(metaA.Length, metaB.Length);

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
                    AssertAllAreEqual(SAVE_FORMAT_3, metaA[17], metaB[17]);
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
            AssertAllAreEqual(SAVE_FORMAT_2, BitConverter.ToInt32(memoryDatA, 8), BitConverter.ToInt32(memoryDatB, 8));
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
            metaA[i] = GetUInt32(memoryDatA.Skip(MEMORYDAT_OFFSET_META + i * META_LENGTH_TOTAL_VANILLA).Take(META_LENGTH_TOTAL_VANILLA).ToArray());
            metaB[i] = GetUInt32(memoryDatB.Skip(MEMORYDAT_OFFSET_META + i * META_LENGTH_TOTAL_VANILLA).Take(META_LENGTH_TOTAL_VANILLA).ToArray());

            if (i == 1)
            {
                AssertAllZero(metaA[i], metaB[i]);
                continue;
            }

            AssertAllAreEqual(META_HEADER, metaA[i][0], metaB[i][0]);
            AssertAllAreEqual(SAVE_FORMAT_2, metaA[i][1], metaB[i][1]);
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
    public void T01_Read_0x7D1_Homebrew_1()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D1", "Homebrew", "1");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum SeasonEnum, int BaseVersion, GameVersionEnum GameVersionEnum)[]
        {
            (0, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4119, GameVersionEnum.BeyondWithVehicleCam), // 1Auto
            (1, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4119, GameVersionEnum.BeyondWithVehicleCam), // 1Manual
        };
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformPlaystation(path, settings);

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
            var priect = new PrivateObject(container);

            Assert.AreEqual(results[i].Exists, container.Exists);
            Assert.AreEqual(results[i].IsOld, container.IsOld);
            Assert.AreEqual(results[i].GameMode, (PresetGameModeEnum)(priect.GetFieldOrProperty("GameMode")));
            Assert.AreEqual(results[i].GameDifficulty, container.GameDifficulty);
            Assert.AreEqual(results[i].SeasonEnum, container.Season);
            Assert.AreEqual(results[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(results[i].GameVersionEnum, container.GameVersion);
        }
    }

    [TestMethod]
    public void T02_Read_0x7D1_Homebrew_2()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D1", "Homebrew", "2");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum Season, int BaseVersion, GameVersionEnum Version)[]
        {
            (0, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4119, GameVersionEnum.BeyondWithVehicleCam), // 1Auto
            (1, true, true, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4098, GameVersionEnum.Unknown), // 1Manual
        };
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformPlaystation(path, settings);

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
            var priect = new PrivateObject(container);

            Assert.AreEqual(results[i].Exists, container.Exists);
            Assert.AreEqual(results[i].IsOld, container.IsOld);
            Assert.AreEqual(results[i].GameMode, (PresetGameModeEnum)(priect.GetFieldOrProperty("GameMode")));
            Assert.AreEqual(results[i].GameDifficulty, container.GameDifficulty);
            Assert.AreEqual(results[i].Season, container.Season);
            Assert.AreEqual(results[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(results[i].Version, container.GameVersion);
        }
    }

    [TestMethod]
    public void T03_Read_0x7D1_SaveWizard_1()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D1", "SaveWizard", "1");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum Season, int BaseVersion, GameVersionEnum Version)[]
        {
            (0, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4134, GameVersionEnum.PrismsWithBytebeatAuthor), // 1Auto
            (1, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4134, GameVersionEnum.PrismsWithBytebeatAuthor), // 1Manual
            (2, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4134, GameVersionEnum.PrismsWithBytebeatAuthor), // 2Auto
            (3, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4134, GameVersionEnum.PrismsWithBytebeatAuthor), // 2Manual
            (4, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4134, GameVersionEnum.PrismsWithBytebeatAuthor), // 3Auto
            (5, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4133, GameVersionEnum.PrismsWithBytebeatAuthor), // 3Manual
            (6, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4134, GameVersionEnum.PrismsWithBytebeatAuthor), // 4Auto
            (7, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4124, GameVersionEnum.LivingShip), // 4Manual
            (8, true, false, PresetGameModeEnum.Permadeath, DifficultyPresetTypeEnum.Permadeath, SeasonEnum.None, 4134, GameVersionEnum.PrismsWithBytebeatAuthor), // 5Auto
            (9, true, false, PresetGameModeEnum.Permadeath, DifficultyPresetTypeEnum.Permadeath, SeasonEnum.None, 4134, GameVersionEnum.PrismsWithBytebeatAuthor), // 5Manual
        };
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformPlaystation(path, settings);

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
            var priect = new PrivateObject(container);

            Assert.AreEqual(results[i].Exists, container.Exists);
            Assert.AreEqual(results[i].IsOld, container.IsOld);
            Assert.AreEqual(results[i].GameMode, (PresetGameModeEnum)(priect.GetFieldOrProperty("GameMode")));
            Assert.AreEqual(results[i].GameDifficulty, container.GameDifficulty);
            Assert.AreEqual(results[i].Season, container.Season);
            Assert.AreEqual(results[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(results[i].Version, container.GameVersion);
        }
    }

    [TestMethod]
    public void T04_Read_0x7D1_SaveWizard_2()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D1", "SaveWizard", "2");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum Season, int BaseVersion, GameVersionEnum Version)[]
        {
            (0, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4126, GameVersionEnum.Origins), // 1Auto
            (1, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4134, GameVersionEnum.PrismsWithBytebeatAuthor), // 1Manual
        };
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformPlaystation(path, settings);

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
            var priect = new PrivateObject(container);

            Assert.AreEqual(results[i].Exists, container.Exists);
            Assert.AreEqual(results[i].IsOld, container.IsOld);
            Assert.AreEqual(results[i].GameMode, (PresetGameModeEnum)(priect.GetFieldOrProperty("GameMode")));
            Assert.AreEqual(results[i].GameDifficulty, container.GameDifficulty);
            Assert.AreEqual(results[i].Season, container.Season);
            Assert.AreEqual(results[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(results[i].Version, container.GameVersion);
        }
    }

    [TestMethod]
    public void T05_Read_0x7D1_SaveWizard_3()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D1", "SaveWizard", "3");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum Season, int BaseVersion, GameVersionEnum Version)[]
        {
            (0, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4126, GameVersionEnum.Origins), // 1Auto
            (1, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4126, GameVersionEnum.Origins), // 1Manual
            (2, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4134, GameVersionEnum.PrismsWithBytebeatAuthor), // 2Auto
        };
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformPlaystation(path, settings);

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
            var priect = new PrivateObject(container);

            Assert.AreEqual(results[i].Exists, container.Exists);
            Assert.AreEqual(results[i].IsOld, container.IsOld);
            Assert.AreEqual(results[i].GameMode, (PresetGameModeEnum)(priect.GetFieldOrProperty("GameMode")));
            Assert.AreEqual(results[i].GameDifficulty, container.GameDifficulty);
            Assert.AreEqual(results[i].Season, container.Season);
            Assert.AreEqual(results[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(results[i].Version, container.GameVersion);
        }
    }

    [TestMethod]
    public void T10_Read_0x7D2_Homebrew_1()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D2", "Homebrew", "1");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum SeasonEnum, int BaseVersion, GameVersionEnum GameVersionEnum)[]
        {
            (0, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4119, GameVersionEnum.BeyondWithVehicleCam), // 1Auto
            (1, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, GameVersionEnum.Emergence), // 1Manual
        };
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformPlaystation(path, settings);

        // Assert
        Assert.IsTrue(platform.HasAccountData);
        Assert.AreEqual(results.Length, platform.GetExistingContainers().Count());
        Assert.AreEqual(userIdentification[0], platform.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification[1], platform.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification[2], platform.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK);

        for (var i = 0; i < results.Length; i++)
        {
            var container = platform.GetSaveContainer(results[i].CollectionIndex)!;
            var priect = new PrivateObject(container);

            Assert.AreEqual(results[i].Exists, container.Exists);
            Assert.AreEqual(results[i].IsOld, container.IsOld);
            Assert.AreEqual(results[i].GameMode, (PresetGameModeEnum)(priect.GetFieldOrProperty("GameMode")));
            Assert.AreEqual(results[i].GameDifficulty, container.GameDifficulty);
            Assert.AreEqual(results[i].SeasonEnum, container.Season);
            Assert.AreEqual(results[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(results[i].GameVersionEnum, container.GameVersion);
        }
    }

    [TestMethod]
    public void T11_Read_0x7D2_SaveWizard_1()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D2", "SaveWizard", "1");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum Season, int BaseVersion, GameVersionEnum Version)[]
        {
            (0, true, false, PresetGameModeEnum.Creative, DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4135, GameVersionEnum.Frontiers), // 1Auto
            (2, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, GameVersionEnum.Frontiers), // 2Auto
            (3, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, GameVersionEnum.Frontiers), // 2Manual
        };
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformPlaystation(path, settings);

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
            var priect = new PrivateObject(container);

            Assert.AreEqual(results[i].Exists, container.Exists);
            Assert.AreEqual(results[i].IsOld, container.IsOld);
            Assert.AreEqual(results[i].GameMode, (PresetGameModeEnum)(priect.GetFieldOrProperty("GameMode")));
            Assert.AreEqual(results[i].GameDifficulty, container.GameDifficulty);
            Assert.AreEqual(results[i].Season, container.Season);
            Assert.AreEqual(results[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(results[i].Version, container.GameVersion);
        }
    }

    [TestMethod]
    public void T12_Read_0x7D2_SaveWizard_2()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D2", "SaveWizard", "2");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum Season, int BaseVersion, GameVersionEnum Version, string SaveName, string SaveSummary)[]
        {
            (2, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4141, GameVersionEnum.WaypointWithAgileStat, "1. Haupt", "An Bord von „Sueyuan XI“-Plattform"), // 2Auto
            (3, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4141, GameVersionEnum.WaypointWithAgileStat, "1. Haupt", "Auf dem Frachter (WF-4 Dawajima)"), // 2Manual
        };
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformPlaystation(path, settings);

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
            var priect = new PrivateObject(container);

            Assert.AreEqual(results[i].Exists, container.Exists);
            Assert.AreEqual(results[i].IsOld, container.IsOld);
            Assert.AreEqual(results[i].GameMode, (PresetGameModeEnum)(priect.GetFieldOrProperty("GameMode")));
            Assert.AreEqual(results[i].GameDifficulty, container.GameDifficulty);
            Assert.AreEqual(results[i].Season, container.Season);
            Assert.AreEqual(results[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(results[i].Version, container.GameVersion);
            Assert.AreEqual(results[i].SaveName, container.SaveName);
            Assert.AreEqual(results[i].SaveSummary, container.SaveSummary);
        }
    }

    [TestMethod]
    public void T13_Read_0x7D2_SaveWizard_3()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D2", "SaveWizard", "3");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum Season, int BaseVersion, GameVersionEnum Version)[]
        {
            (0, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, GameVersionEnum.Frontiers), // 1Auto
            (1, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, GameVersionEnum.Frontiers), // 1Manual
            (2, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4134, GameVersionEnum.PrismsWithBytebeatAuthor), // 2Auto
        };
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformPlaystation(path, settings);

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
            var priect = new PrivateObject(container);

            Assert.AreEqual(results[i].Exists, container.Exists);
            Assert.AreEqual(results[i].IsOld, container.IsOld);
            Assert.AreEqual(results[i].GameMode, (PresetGameModeEnum)(priect.GetFieldOrProperty("GameMode")));
            Assert.AreEqual(results[i].GameDifficulty, container.GameDifficulty);
            Assert.AreEqual(results[i].Season, container.Season);
            Assert.AreEqual(results[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(results[i].Version, container.GameVersion);
        }
    }

    [TestMethod]
    public void T14_Read_0x7D2_SaveWizard_4()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D2", "SaveWizard", "4");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum Season, int BaseVersion, GameVersionEnum Version)[]
        {
            (0, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4134, GameVersionEnum.PrismsWithBytebeatAuthor), // 1Auto
            (1, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4134, GameVersionEnum.PrismsWithBytebeatAuthor), // 1Manual
            (2, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, GameVersionEnum.Frontiers), // 2Auto
            (3, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, GameVersionEnum.Frontiers), // 2Manual
            (4, true, false, PresetGameModeEnum.Seasonal, DifficultyPresetTypeEnum.Normal, SeasonEnum.Cartographers, 4135, GameVersionEnum.Frontiers), // 3Auto
            (5, true, false, PresetGameModeEnum.Seasonal, DifficultyPresetTypeEnum.Normal, SeasonEnum.Cartographers, 4135, GameVersionEnum.Frontiers), // 3Manual
        };
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformPlaystation(path, settings);

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
            var priect = new PrivateObject(container);

            Assert.AreEqual(results[i].Exists, container.Exists);
            Assert.AreEqual(results[i].IsOld, container.IsOld);
            Assert.AreEqual(results[i].GameMode, (PresetGameModeEnum)(priect.GetFieldOrProperty("GameMode")));
            Assert.AreEqual(results[i].GameDifficulty, container.GameDifficulty);
            Assert.AreEqual(results[i].Season, container.Season);
            Assert.AreEqual(results[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(results[i].Version, container.GameVersion);
        }
    }

    [TestMethod]
    public void T15_Read_0x7D2_SaveWizard_5()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D2", "SaveWizard", "5");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum Season, int BaseVersion, GameVersionEnum Version, string SaveName, string SaveSummary)[]
        {
            (11, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4145, GameVersionEnum.Interceptor, "Singularity", "Aboard Riesid Station Omega"), // 6Manual
        };
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformPlaystation(path, settings);

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
            var priect = new PrivateObject(container);

            Assert.AreEqual(results[i].Exists, container.Exists);
            Assert.AreEqual(results[i].IsOld, container.IsOld);
            Assert.AreEqual(results[i].GameMode, (PresetGameModeEnum)(priect.GetFieldOrProperty("GameMode")));
            Assert.AreEqual(results[i].GameDifficulty, container.GameDifficulty);
            Assert.AreEqual(results[i].Season, container.Season);
            Assert.AreEqual(results[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(results[i].Version, container.GameVersion);
            Assert.AreEqual(results[i].SaveName, container.SaveName);
            Assert.AreEqual(results[i].SaveSummary, container.SaveSummary);
        }
    }

    [TestMethod]
    public void T16_Read_0x7D2_SaveWizard_6()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D2", "SaveWizard", "6");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum Season, int BaseVersion, GameVersionEnum Version, string SaveName, string SaveSummary)[]
        {
            (9, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4146, GameVersionEnum.Echoes, "Purfex", "On freighter (Normandy SR3)"), // 6Manual
        };
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platform = new PlatformPlaystation(path, settings);

        // Assert
        Assert.IsTrue(platform.HasAccountData);
        Assert.AreEqual(results.Length, platform.GetExistingContainers().Count());
        Assert.AreEqual(userIdentification[0], platform.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification[1], platform.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification[2], platform.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK);

        for (var i = 0; i < results.Length; i++)
        {
            var container = platform.GetSaveContainer(results[i].CollectionIndex)!;
            var priect = new PrivateObject(container);

            Assert.AreEqual(results[i].Exists, container.Exists);
            Assert.AreEqual(results[i].IsOld, container.IsOld);
            Assert.AreEqual(results[i].GameMode, (PresetGameModeEnum)(priect.GetFieldOrProperty("GameMode")));
            Assert.AreEqual(results[i].GameDifficulty, container.GameDifficulty);
            Assert.AreEqual(results[i].Season, container.Season);
            Assert.AreEqual(results[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(results[i].Version, container.GameVersion);
            Assert.AreEqual(results[i].SaveName, container.SaveName);
            Assert.AreEqual(results[i].SaveSummary, container.SaveSummary);
        }
    }

    [TestMethod]
    public void T20_Write_Default_0x7D1_Homebrew()
    {
        // Arrange
        _usesSaveStreaming = false;
        _usesSaveWizard = false;

        var now = DateTimeOffset.UtcNow;
        var nowUnix = now.ToUnixTimeSeconds();
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D1", "Homebrew", "1");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            UseMapping = true,
        };
        var writeCallback = false;

        // Act
        var platformA = new PlatformPlaystation(path, settings);
        var containerA = platformA.GetSaveContainer(0)!;
        var memoryDatA = File.ReadAllBytes(Path.Combine(path, "memory.dat"));
        var metaA = DecryptMeta(containerA);
        var priectA = new PrivateObject(containerA);

        containerA.WriteCallback += () =>
        {
            writeCallback = true;
        };

#pragma warning disable IDE0042 // Deconstruct variable declaration
        platformA.Load(containerA);
        (int Units, long UnixSeconds) valuesOrigin = (containerA.GetJsonValue<int>(UNITS_JSON_PATH), containerA.LastWriteTime!.Value.ToUniversalTime().ToUnixTimeSeconds());

        containerA.SetJsonValue(UNITS_NEW_AMOUNT, UNITS_JSON_PATH);
        platformA.Write(containerA, now);
        (int Units, long UnixSeconds) valuesSet = (containerA.GetJsonValue<int>(UNITS_JSON_PATH), containerA.LastWriteTime!.Value.ToUniversalTime().ToUnixTimeSeconds());

        var platformB = new PlatformPlaystation(path, settings);
        var containerB = platformB.GetSaveContainer(0)!;
        var memoryDatB = File.ReadAllBytes(Path.Combine(path, "memory.dat"));
        var metaB = DecryptMeta(containerB);
        var priectB = new PrivateObject(containerB);

        platformB.Load(containerB);
        (int Units, long UnixSeconds) valuesReload = (containerB.GetJsonValue<int>(UNITS_JSON_PATH), containerB.LastWriteTime!.Value.ToUniversalTime().ToUnixTimeSeconds());
#pragma warning restore IDE0042 // Deconstruct variable declaration

        // Assert
        Assert.IsTrue(writeCallback);

        AssertMemoryDat(memoryDatA, memoryDatB);

        Assert.AreEqual(979316666, valuesOrigin.Units);
        Assert.AreEqual(1601202709, valuesOrigin.UnixSeconds); // 2020-09-27 10:31:49 +00:00
        Assert.AreEqual(UNITS_NEW_AMOUNT, valuesSet.Units);
        Assert.AreEqual(nowUnix, valuesSet.UnixSeconds);

        Assert.AreEqual(UNITS_NEW_AMOUNT, valuesReload.Units);
        Assert.AreEqual(nowUnix, valuesReload.UnixSeconds);

        AssertCommonMeta(metaA, metaB);

        AssertAllAreEqual(2, (uint)(containerA.MetaIndex), (uint)(containerB.MetaIndex), metaA[5], metaB[5]);
    }

    [TestMethod]
    public void T21_Write_Default_0x7D1_SaveWizard()
    {
        // Arrange
        _usesSaveStreaming = false;
        _usesSaveWizard = true;

        var now = DateTimeOffset.UtcNow;
        var nowUnix = now.ToUnixTimeSeconds();
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D1", "SaveWizard", "1");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            UseMapping = true,
        };
        var writeCallback = false;

        // Act
        var platformA = new PlatformPlaystation(path, settings);
        var containerA = platformA.GetSaveContainer(0)!;
        var memoryDatA = File.ReadAllBytes(Path.Combine(path, "memory.dat"));
        var metaA = DecryptMeta(containerA);
        var priectA = new PrivateObject(containerA);

        containerA.WriteCallback += () =>
        {
            writeCallback = true;
        };

#pragma warning disable IDE0042 // Deconstruct variable declaration
        platformA.Load(containerA);
        (int Units, long UnixSeconds) valuesOrigin = (containerA.GetJsonValue<int>(UNITS_JSON_PATH), containerA.LastWriteTime!.Value.ToUniversalTime().ToUnixTimeSeconds());

        containerA.SetJsonValue(UNITS_NEW_AMOUNT, UNITS_JSON_PATH);
        platformA.Write(containerA, now);
        (int Units, long UnixSeconds) valuesSet = (containerA.GetJsonValue<int>(UNITS_JSON_PATH), containerA.LastWriteTime!.Value.ToUniversalTime().ToUnixTimeSeconds());

        var platformB = new PlatformPlaystation(path, settings);
        var containerB = platformB.GetSaveContainer(0)!;
        var memoryDatB = File.ReadAllBytes(Path.Combine(path, "memory.dat"));
        var metaB = DecryptMeta(containerB);
        var priectB = new PrivateObject(containerB);

        platformB.Load(containerB);
        (int Units, long UnixSeconds) valuesReload = (containerB.GetJsonValue<int>(UNITS_JSON_PATH), containerB.LastWriteTime!.Value.ToUniversalTime().ToUnixTimeSeconds());
#pragma warning restore IDE0042 // Deconstruct variable declaration

        // Assert
        Assert.IsTrue(writeCallback);

        AssertMemoryDat(memoryDatA, memoryDatB);

        Assert.AreEqual(1283166770, valuesOrigin.Units);
        Assert.AreEqual(1624402816, valuesOrigin.UnixSeconds); // 2021-06-23 23:00:16 +00:00
        Assert.AreEqual(UNITS_NEW_AMOUNT, valuesSet.Units);
        Assert.AreEqual(nowUnix, valuesSet.UnixSeconds);

        Assert.AreEqual(UNITS_NEW_AMOUNT, valuesReload.Units);
        Assert.AreEqual(nowUnix, valuesReload.UnixSeconds);

        AssertCommonMeta(metaA, metaB);

        AssertAllAreEqual(2, (uint)(containerA.MetaIndex), (uint)(containerB.MetaIndex), metaA[5], metaB[5]);
    }

    [TestMethod]
    public void T22_Write_Default_0x7D2_Homebrew()
    {
        // Arrange
        _usesSaveStreaming = true;
        _usesSaveWizard = false;

        var now = DateTimeOffset.UtcNow;
        var nowUnix = now.ToUnixTimeSeconds();
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D2", "Homebrew", "1");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            UseMapping = true,
        };
        var writeCallback = false;

        // Act
        var platformA = new PlatformPlaystation(path, settings);
        var containerA = platformA.GetSaveContainer(0)!;
        var metaA = DecryptMeta(containerA);

        containerA.WriteCallback += () =>
        {
            writeCallback = true;
        };

#pragma warning disable IDE0042 // Deconstruct variable declaration
        platformA.Load(containerA);
        (int Units, long UnixSeconds) valuesOrigin = (containerA.GetJsonValue<int>(UNITS_JSON_PATH), containerA.LastWriteTime!.Value.ToUniversalTime().ToUnixTimeSeconds());

        containerA.SetJsonValue(UNITS_NEW_AMOUNT, UNITS_JSON_PATH);
        platformA.Write(containerA, now);
        (int Units, long UnixSeconds) valuesSet = (containerA.GetJsonValue<int>(UNITS_JSON_PATH), containerA.LastWriteTime!.Value.ToUniversalTime().ToUnixTimeSeconds());

        var platformB = new PlatformPlaystation(path, settings);
        var containerB = platformB.GetSaveContainer(0)!;
        var metaB = DecryptMeta(containerB);

        platformB.Load(containerB);
        (int Units, long UnixSeconds) valuesReload = (containerB.GetJsonValue<int>(UNITS_JSON_PATH), containerB.LastWriteTime!.Value.ToUniversalTime().ToUnixTimeSeconds());
#pragma warning restore IDE0042 // Deconstruct variable declaration

        // Assert
        Assert.IsTrue(writeCallback);

        Assert.AreEqual(1970043489, valuesOrigin.Units);
        Assert.AreEqual(1641647860, valuesOrigin.UnixSeconds); // 2022-01-08 13:17:40 +00:00
        Assert.AreEqual(UNITS_NEW_AMOUNT, valuesSet.Units);
        Assert.AreEqual(nowUnix, valuesSet.UnixSeconds);

        Assert.AreEqual(UNITS_NEW_AMOUNT, valuesReload.Units);
        Assert.AreEqual(nowUnix, valuesReload.UnixSeconds);

        AssertCommonMeta(metaA, metaB);
    }

    [TestMethod]
    public void T23_Write_Default_0x7D2_Homebrew_Account()
    {
        // Arrange
        _usesSaveStreaming = true;
        _usesSaveWizard = false;

        var now = DateTimeOffset.UtcNow;
        var nowUnix = now.ToUnixTimeSeconds();
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D2", "Homebrew", "1");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            UseMapping = true,
        };
        var writeCallback = false;

        // Act
        var platformA = new PlatformPlaystation(path, settings);
        var containerA = platformA.GetAccountContainer();
        var metaA = DecryptMeta(containerA);

        containerA.WriteCallback += () =>
        {
            writeCallback = true;
        };

#pragma warning disable IDE0042 // Deconstruct variable declaration
        platformA.Load(containerA);
        (int MusicVolume, long UnixSeconds) valuesOrigin = (containerA.GetJsonValue<int>(MUSICVOLUME_JSON_PATH), containerA.LastWriteTime!.Value.ToUniversalTime().ToUnixTimeSeconds());

        containerA.SetJsonValue(MUSICVOLUME_NEW_AMOUNT, MUSICVOLUME_JSON_PATH);
        platformA.Write(containerA, now);
        (int MusicVolume, long UnixSeconds) valuesSet = (containerA.GetJsonValue<int>(MUSICVOLUME_JSON_PATH), containerA.LastWriteTime!.Value.ToUniversalTime().ToUnixTimeSeconds());

        var platformB = new PlatformPlaystation(path, settings);
        var containerB = platformB.GetAccountContainer();
        var metaB = DecryptMeta(containerB);

        platformB.Load(containerB);
        (int MusicVolume, long UnixSeconds) valuesReload = (containerB.GetJsonValue<int>(MUSICVOLUME_JSON_PATH), containerB.LastWriteTime!.Value.ToUniversalTime().ToUnixTimeSeconds());
#pragma warning restore IDE0042 // Deconstruct variable declaration

        // Assert
        Assert.IsTrue(writeCallback);

        Assert.AreEqual(80, valuesOrigin.MusicVolume);
        Assert.AreEqual(1641647860, valuesOrigin.UnixSeconds); // 2022-01-08 13:17:40 +00:00
        Assert.AreEqual(MUSICVOLUME_NEW_AMOUNT, valuesSet.MusicVolume);
        Assert.AreEqual(nowUnix, valuesSet.UnixSeconds);

        Assert.AreEqual(MUSICVOLUME_NEW_AMOUNT, valuesReload.MusicVolume);
        Assert.AreEqual(nowUnix, valuesReload.UnixSeconds);

        AssertCommonMeta(metaA, metaB);
    }

    [TestMethod]
    public void T24_Write_Default_0x7D2_SaveWizard()
    {
        // Arrange
        _usesSaveStreaming = true;
        _usesSaveWizard = true;

        var now = DateTimeOffset.UtcNow;
        var nowUnix = now.ToUnixTimeSeconds();
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D2", "SaveWizard", "1");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            UseMapping = true,
        };
        var writeCallback = false;

        // Act
        var platformA = new PlatformPlaystation(path, settings);
        var containerA = platformA.GetSaveContainer(0)!;
        var metaA = DecryptMeta(containerA);

        containerA.WriteCallback += () =>
        {
            writeCallback = true;
        };

#pragma warning disable IDE0042 // Deconstruct variable declaration
        platformA.Load(containerA);
        (int Units, long UnixSeconds) valuesOrigin = (containerA.GetJsonValue<int>(UNITS_JSON_PATH), containerA.LastWriteTime!.Value.ToUniversalTime().ToUnixTimeSeconds());

        containerA.SetJsonValue(UNITS_NEW_AMOUNT, UNITS_JSON_PATH);
        platformA.Write(containerA, now);
        (int Units, long UnixSeconds) valuesSet = (containerA.GetJsonValue<int>(UNITS_JSON_PATH), containerA.LastWriteTime!.Value.ToUniversalTime().ToUnixTimeSeconds());

        var platformB = new PlatformPlaystation(path, settings);
        var containerB = platformB.GetSaveContainer(0)!;
        var metaB = DecryptMeta(containerB);

        platformB.Load(containerB);
        (int Units, long UnixSeconds) valuesReload = (containerB.GetJsonValue<int>(UNITS_JSON_PATH), containerB.LastWriteTime!.Value.ToUniversalTime().ToUnixTimeSeconds());
#pragma warning restore IDE0042 // Deconstruct variable declaration

        // Assert
        Assert.IsTrue(writeCallback);

        Assert.AreEqual(0, valuesOrigin.Units);
        Assert.AreEqual(1631364996, valuesOrigin.UnixSeconds); // 2021-09-11 12:56:36 +00:00
        Assert.AreEqual(UNITS_NEW_AMOUNT, valuesSet.Units);
        Assert.AreEqual(nowUnix, valuesSet.UnixSeconds);

        Assert.AreEqual(UNITS_NEW_AMOUNT, valuesReload.Units);
        Assert.AreEqual(nowUnix, valuesReload.UnixSeconds);

        AssertCommonMeta(metaA, metaB);
    }

    [TestMethod]
    public void T25_Write_Default_0x7D2_SaveWizard_Account()
    {
        // Arrange
        _usesSaveStreaming = true;
        _usesSaveWizard = true;

        var now = DateTimeOffset.UtcNow;
        var nowUnix = now.ToUnixTimeSeconds();
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D2", "SaveWizard", "6");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            UseMapping = true,
        };
        var writeCallback = false;

        // Act
        var platformA = new PlatformPlaystation(path, settings);
        var containerA = platformA.GetAccountContainer();
        var metaA = DecryptMeta(containerA);

        containerA.WriteCallback += () =>
        {
            writeCallback = true;
        };

#pragma warning disable IDE0042 // Deconstruct variable declaration
        platformA.Load(containerA);
        (int MusicVolume, long UnixSeconds) valuesOrigin = (containerA.GetJsonValue<int>(MUSICVOLUME_JSON_PATH), containerA.LastWriteTime!.Value.ToUniversalTime().ToUnixTimeSeconds());

        containerA.SetJsonValue(MUSICVOLUME_NEW_AMOUNT, MUSICVOLUME_JSON_PATH);
        platformA.Write(containerA, now);
        (int MusicVolume, long UnixSeconds) valuesSet = (containerA.GetJsonValue<int>(MUSICVOLUME_JSON_PATH), containerA.LastWriteTime!.Value.ToUniversalTime().ToUnixTimeSeconds());

        var platformB = new PlatformPlaystation(path, settings);
        var containerB = platformB.GetAccountContainer();
        var metaB = DecryptMeta(containerB);

        platformB.Load(containerB);
        (int MusicVolume, long UnixSeconds) valuesReload = (containerB.GetJsonValue<int>(MUSICVOLUME_JSON_PATH), containerB.LastWriteTime!.Value.ToUniversalTime().ToUnixTimeSeconds());
#pragma warning restore IDE0042 // Deconstruct variable declaration

        // Assert
        Assert.IsTrue(writeCallback);

        Assert.AreEqual(0, valuesOrigin.MusicVolume);
        Assert.AreEqual(1693206912, valuesOrigin.UnixSeconds); // 2023-08-28 07:15:12 +00:00
        Assert.AreEqual(MUSICVOLUME_NEW_AMOUNT, valuesSet.MusicVolume);
        Assert.AreEqual(nowUnix, valuesSet.UnixSeconds);

        Assert.AreEqual(MUSICVOLUME_NEW_AMOUNT, valuesReload.MusicVolume);
        Assert.AreEqual(nowUnix, valuesReload.UnixSeconds);

        // Fixed header and all equal except SizeDecompressed (meta[2]).
        Assert.AreEqual(META_HEADER, metaA[0], metaB[0]);
        Assert.AreEqual(SAVE_FORMAT_3, metaA[1], metaB[1]);
        Assert.IsTrue(metaA.Skip(3).SequenceEqual(metaB.Skip(3)));
    }

    [TestMethod]
    public void T26_Write_SetLastWriteTime_False()
    {
        // Arrange
        _usesSaveStreaming = false;
        _usesSaveWizard = false;

        var now = DateTimeOffset.UtcNow;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D1", "Homebrew", "1");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            SetLastWriteTime = false,
            UseMapping = true,
        };
        var writeCallback = false;

        // Act
        var platformA = new PlatformPlaystation(path, settings);
        var containerA = platformA.GetSaveContainer(0)!;

        containerA.WriteCallback += () =>
        {
            writeCallback = true;
        };

#pragma warning disable IDE0042 // Deconstruct variable declaration
        platformA.Load(containerA);
        (int Units, long UnixSeconds) valuesOrigin = (containerA.GetJsonValue<int>(UNITS_JSON_PATH), containerA.LastWriteTime!.Value.ToUniversalTime().ToUnixTimeSeconds());

        containerA.SetJsonValue(UNITS_NEW_AMOUNT, UNITS_JSON_PATH);
        platformA.Write(containerA, now);
        (int Units, long UnixSeconds) valuesSet = (containerA.GetJsonValue<int>(UNITS_JSON_PATH), containerA.LastWriteTime!.Value.ToUniversalTime().ToUnixTimeSeconds());

        var platformB = new PlatformPlaystation(path, settings);
        var containerB = platformB.GetSaveContainer(0)!;

        platformB.Load(containerB);
        (int Units, long UnixSeconds) valuesReload = (containerB.GetJsonValue<int>(UNITS_JSON_PATH), containerB.LastWriteTime!.Value.ToUniversalTime().ToUnixTimeSeconds());
#pragma warning restore IDE0042 // Deconstruct variable declaration

        // Assert
        Assert.IsTrue(writeCallback);

        Assert.AreEqual(979316666, valuesOrigin.Units);
        Assert.AreEqual(1601202709, valuesOrigin.UnixSeconds); // 2020-09-27 10:31:49 +00:00
        Assert.AreEqual(UNITS_NEW_AMOUNT, valuesSet.Units);
        Assert.AreEqual(1601202709, valuesSet.UnixSeconds);

        Assert.AreEqual(UNITS_NEW_AMOUNT, valuesReload.Units);
        Assert.AreEqual(1601202709, valuesReload.UnixSeconds);
    }

    [TestMethod]
    public void T27_Write_WriteAlways_True_0x7D1()
    {
        // Arrange
        _usesSaveStreaming = false;
        _usesSaveWizard = false;

        var now = DateTimeOffset.UtcNow;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D1", "Homebrew", "1");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            SetLastWriteTime = false, // no interfering
            WriteAlways = true,
        };
        var writeCallback = false;

        // Act
        var platformA = new PlatformPlaystation(path, settings);
        var containerA = platformA.GetSaveContainer(0)!;

        containerA.WriteCallback += () =>
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
        var containerB = platformB.GetSaveContainer(0)!;

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
    public void T28_Write_WriteAlways_True_0x7D2()
    {
        // Arrange
        _usesSaveStreaming = true;
        _usesSaveWizard = false;

        var now = DateTimeOffset.UtcNow;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D2", "Homebrew", "1");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            SetLastWriteTime = false, // no interfering
            WriteAlways = true,
        };
        var writeCallback = false;

        // Act
        var platformA = new PlatformPlaystation(path, settings);
        var containerA = platformA.GetSaveContainer(0)!;

        containerA.WriteCallback += () =>
        {
            writeCallback = true;
        };

        platformA.Load(containerA);
        containerA.DataFile!.Refresh();
        var lengthOrigin = containerA.DataFile!.Length;

        platformA.Write(containerA);
        containerA.DataFile!.Refresh();
        var lengthSet = containerA.DataFile!.Length;

        var platformB = new PlatformPlaystation(path, settings);
        var containerB = platformB.GetSaveContainer(0)!;

        platformB.Load(containerB);
        containerB.DataFile!.Refresh();
        var lengthReload = containerA.DataFile!.Length;

        // Assert
        Assert.IsTrue(writeCallback);

        Assert.AreNotEqual(lengthOrigin, lengthSet);
        Assert.AreNotEqual(lengthOrigin, lengthReload);

        Assert.AreEqual(lengthSet, lengthReload);
    }

    [TestMethod]
    public void T29_Write_WriteAlways_False_0x7D1()
    {
        // Arrange
        _usesSaveStreaming = false;
        _usesSaveWizard = false;

        var now = DateTimeOffset.UtcNow;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D1", "Homebrew", "1");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            SetLastWriteTime = false, // no interfering
            WriteAlways = false,
        };
        var writeCallback = false;

        // Act
        var platformA = new PlatformPlaystation(path, settings);
        var containerA = platformA.GetSaveContainer(0)!;

        containerA.WriteCallback += () =>
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
        var containerB = platformB.GetSaveContainer(0)!;

        platformB.Load(containerB);
        containerB.DataFile!.Refresh();
        var timeReload = containerA.DataFile!.LastWriteTimeUtc.Ticks;

        // Assert
        Assert.IsTrue(writeCallback);

        Assert.AreEqual(timeOrigin, timeSet);
        Assert.AreEqual(timeOrigin, timeReload);
    }

    [TestMethod]
    public void T30_Write_WriteAlways_False_0x7D2()
    {
        // Arrange
        _usesSaveStreaming = true;
        _usesSaveWizard = false;

        var now = DateTimeOffset.UtcNow;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D2", "Homebrew", "1");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            SetLastWriteTime = false, // no interfering
            WriteAlways = false,
        };
        var writeCallback = false;

        // Act
        var platformA = new PlatformPlaystation(path, settings);
        var containerA = platformA.GetSaveContainer(0)!;

        containerA.WriteCallback += () =>
        {
            writeCallback = true;
        };

        platformA.Load(containerA);
        containerA.DataFile!.Refresh();
        var lengthOrigin = containerA.DataFile!.Length;

        platformA.Write(containerA);
        containerA.DataFile!.Refresh();
        var lengthSet = containerA.DataFile!.Length;

        var platformB = new PlatformPlaystation(path, settings);
        var containerB = platformB.GetSaveContainer(0)!;

        platformB.Load(containerB);
        containerB.DataFile!.Refresh();
        var lengthReload = containerA.DataFile!.Length;

        // Assert
        Assert.IsTrue(writeCallback);

        Assert.AreEqual(lengthOrigin, lengthSet);
        Assert.AreEqual(lengthOrigin, lengthReload);
    }

    [TestMethod]
    public void T40_FileSystemWatcher_0x7D1()
    {
        // Arrange
        _usesSaveStreaming = false;
        _usesSaveWizard = false;

        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D1", "Homebrew", "1");
        var pathWatching = Path.Combine(path, "memory.dat");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            UseMapping = true,
            Watcher = true,
        };

        // Act
        var bytes = File.ReadAllBytes(pathWatching);
        var platform = new PlatformPlaystation(path, settings);

        var container = platform.GetSaveContainer(0)!;
        platform.Load(container);

        File.WriteAllBytes(pathWatching, bytes);
        Thread.Sleep(FILESYSTEMWATCHER_SLEEP);
        var watchers1 = platform.GetWatcherContainers();
        var count1 = watchers1.Count();
        var synced1 = container.IsSynced;

        container.SetJsonValue(UNITS_NEW_AMOUNT, UNITS_JSON_PATH);
        var synced2 = container.IsSynced;

        File.WriteAllBytes(pathWatching, bytes);
        Thread.Sleep(FILESYSTEMWATCHER_SLEEP);
        var watchers2 = platform.GetWatcherContainers();
        var count2 = watchers2.Count();
        var synced3 = container.IsSynced;

        var watcherContainer2 = watchers2.FirstOrDefault();
        Guard.IsNotNull(watcherContainer2);
        platform.OnWatcherDecision(watcherContainer2, false);
        var synced4 = container.IsSynced;

        File.WriteAllBytes(pathWatching, bytes);
        Thread.Sleep(FILESYSTEMWATCHER_SLEEP);
        var watchers3 = platform.GetWatcherContainers();
        var count3 = watchers3.Count();
        var synced5 = container.IsSynced;

        var watcherContainer3 = watchers3.FirstOrDefault();
        Guard.IsNotNull(watcherContainer3);
        platform.OnWatcherDecision(watcherContainer3, true);
        var synced6 = container.IsSynced;

        // Assert
        Assert.AreEqual(0, count1);
        Assert.IsTrue(synced1);

        Assert.IsFalse(synced2);

        Assert.AreEqual(1, count2);
        Assert.IsFalse(synced3);

        Assert.AreEqual(container, watcherContainer2);
        Assert.IsFalse(synced4);

        Assert.AreEqual(1, count3);
        Assert.IsFalse(synced5);

        Assert.AreEqual(container, watcherContainer3);
        Assert.IsTrue(synced6);
    }

    [TestMethod]
    public void T41_FileSystemWatcher_0x7D2()
    {
        // Arrange
        _usesSaveStreaming = true;
        _usesSaveWizard = false;

        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D2", "Homebrew", "1");
        var pathWatching = Path.Combine(path, "savedata02.hg");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            UseMapping = true,
            Watcher = true,
        };

        // Act
        var bytes = File.ReadAllBytes(pathWatching);
        var platform = new PlatformPlaystation(path, settings);

        var container = platform.GetSaveContainer(0)!;
        platform.Load(container);

        File.WriteAllBytes(pathWatching, bytes);
        Thread.Sleep(FILESYSTEMWATCHER_SLEEP);
        var watchers1 = platform.GetWatcherContainers();
        var count1 = watchers1.Count();
        var synced1 = container.IsSynced;

        container.SetJsonValue(UNITS_NEW_AMOUNT, UNITS_JSON_PATH);
        var synced2 = container.IsSynced;

        File.WriteAllBytes(pathWatching, bytes);
        Thread.Sleep(FILESYSTEMWATCHER_SLEEP);
        var watchers2 = platform.GetWatcherContainers();
        var count2 = watchers2.Count();
        var synced3 = container.IsSynced;

        var watcherContainer2 = watchers2.FirstOrDefault();
        Guard.IsNotNull(watcherContainer2);
        platform.OnWatcherDecision(watcherContainer2, false);
        var synced4 = container.IsSynced;

        File.WriteAllBytes(pathWatching, bytes);
        Thread.Sleep(FILESYSTEMWATCHER_SLEEP);
        var watchers3 = platform.GetWatcherContainers();
        var count3 = watchers3.Count();
        var synced5 = container.IsSynced;

        var watcherContainer3 = watchers3.FirstOrDefault();
        Guard.IsNotNull(watcherContainer3);
        platform.OnWatcherDecision(watcherContainer3, true);
        var synced6 = container.IsSynced;

        // Assert
        Assert.AreEqual(0, count1);
        Assert.IsTrue(synced1);

        Assert.IsFalse(synced2);

        Assert.AreEqual(1, count2);
        Assert.IsFalse(synced3);

        Assert.AreEqual(container, watcherContainer2);
        Assert.IsFalse(synced4);

        Assert.AreEqual(1, count3);
        Assert.IsFalse(synced5);

        Assert.AreEqual(container, watcherContainer3);
        Assert.IsTrue(synced6);
    }

    [TestMethod]
    public void T42_Copy_0x7D1()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D1", "SaveWizard", "3");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };

        // Act
        var platform = new PlatformPlaystation(path, settings);

        var container0 = platform.GetSaveContainer(0)!; // 1Auto
        var container1 = platform.GetSaveContainer(1)!; // 1Manual
        var container2 = platform.GetSaveContainer(2)!; // 2Auto
        var container4 = platform.GetSaveContainer(4)!; // 3Auto
        var container6 = platform.GetSaveContainer(6)!; // 4Auto

        platform.Copy(container0, container2); // 1Auto -> 2Auto (overwrite)
        platform.Copy(container0, container4); // 1Auto -> 3Auto (create)
        platform.Copy(container6, container1); // 4Auto -> 1Manual (delete)

        // Assert
        var priect0 = new PrivateObject(container0);
        var priect2 = new PrivateObject(container2);
        var priect4 = new PrivateObject(container4);

        Assert.IsTrue(container2.Exists);
        Assert.AreEqual((PresetGameModeEnum)(priect0.GetFieldOrProperty("GameMode")), (PresetGameModeEnum)(priect2.GetFieldOrProperty("GameMode")));
        Assert.AreEqual(container0.GameDifficulty, container2.GameDifficulty);
        Assert.AreEqual(container0.Season, container2.Season);
        Assert.AreEqual((int)(priect0.GetFieldOrProperty("BaseVersion")), (int)(priect2.GetFieldOrProperty("BaseVersion")));
        Assert.AreEqual(container0.GameVersion, container2.GameVersion);

        Assert.IsTrue(container4.Exists);
        Assert.AreEqual((PresetGameModeEnum)(priect0.GetFieldOrProperty("GameMode")), (PresetGameModeEnum)(priect4.GetFieldOrProperty("GameMode")));
        Assert.AreEqual(container0.GameDifficulty, container4.GameDifficulty);
        Assert.AreEqual(container0.Season, container4.Season);
        Assert.AreEqual((int)(priect0.GetFieldOrProperty("BaseVersion")), (int)(priect4.GetFieldOrProperty("BaseVersion")));
        Assert.AreEqual(container0.GameVersion, container4.GameVersion);

        Assert.IsFalse(container1.Exists);
    }

    [TestMethod]
    public void T43_Copy_0x7D2()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D2", "SaveWizard", "3");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };

        // Act
        var platform = new PlatformPlaystation(path, settings);

        var container0 = platform.GetSaveContainer(0)!; // 1Auto
        var container1 = platform.GetSaveContainer(1)!; // 1Manual
        var container2 = platform.GetSaveContainer(2)!; // 2Auto
        var container4 = platform.GetSaveContainer(4)!; // 3Auto
        var container6 = platform.GetSaveContainer(6)!; // 4Auto

        platform.Copy(container0, container2); // 1Auto -> 2Auto (overwrite)
        platform.Copy(container0, container4); // 1Auto -> 3Auto (create)
        platform.Copy(container6, container1); // 4Auto -> 1Manual (delete)

        // Assert
        var priect0 = new PrivateObject(container0);
        var priect2 = new PrivateObject(container2);
        var priect4 = new PrivateObject(container4);

        Assert.IsTrue(container2.Exists);
        Assert.AreEqual((PresetGameModeEnum)(priect0.GetFieldOrProperty("GameMode")), (PresetGameModeEnum)(priect2.GetFieldOrProperty("GameMode")));
        Assert.AreEqual(container0.GameDifficulty, container2.GameDifficulty);
        Assert.AreEqual(container0.Season, container2.Season);
        Assert.AreEqual((int)(priect0.GetFieldOrProperty("BaseVersion")), (int)(priect2.GetFieldOrProperty("BaseVersion")));
        Assert.AreEqual(container0.GameVersion, container2.GameVersion);

        Assert.IsTrue(container4.Exists);
        Assert.AreEqual((PresetGameModeEnum)(priect0.GetFieldOrProperty("GameMode")), (PresetGameModeEnum)(priect4.GetFieldOrProperty("GameMode")));
        Assert.AreEqual(container0.GameDifficulty, container4.GameDifficulty);
        Assert.AreEqual(container0.Season, container4.Season);
        Assert.AreEqual((int)(priect0.GetFieldOrProperty("BaseVersion")), (int)(priect4.GetFieldOrProperty("BaseVersion")));
        Assert.AreEqual(container0.GameVersion, container4.GameVersion);

        Assert.IsFalse(container1.Exists);
    }

    [TestMethod]
    public void T44_Delete_0x7D1()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D1", "Homebrew", "1");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };

        // Act
        var platform = new PlatformPlaystation(path, settings);

        var container0 = platform.GetSaveContainer(0)!; // 1Auto

        platform.Delete(container0);

        // Assert
        Assert.IsFalse(container0.Exists);
        Assert.AreEqual(libNOM.io.Globals.Constants.INCOMPATIBILITY_006, container0.IncompatibilityTag);
    }

    [TestMethod]
    public void T45_Delete_0x7D2()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D2", "Homebrew", "1");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };

        // Act
        var platform = new PlatformPlaystation(path, settings);

        var container0 = platform.GetSaveContainer(0)!; // 1Auto

        platform.Delete(container0);

        // Assert
        Assert.IsFalse(container0.Exists);
        Assert.AreEqual(libNOM.io.Globals.Constants.INCOMPATIBILITY_006, container0.IncompatibilityTag);
    }

    [TestMethod]
    public void T46_Move_0x7D1()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D1", "SaveWizard", "3");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };

        // Act
        var platform = new PlatformPlaystation(path, settings);

        var container0 = platform.GetSaveContainer(0)!; // 1Auto
        var container1 = platform.GetSaveContainer(1)!; // 1Manual
        var container2 = platform.GetSaveContainer(2)!; // 2Auto
        var container3 = platform.GetSaveContainer(3)!; // 2Manual
        var container4 = platform.GetSaveContainer(4)!; // 3Auto
        var container9 = platform.GetSaveContainer(9)!; // 5Manual

        var priect2 = new PrivateObject(container2);
        var priect3 = new PrivateObject(container3);

        var gameModeEnum2 = (PresetGameModeEnum)(priect2.GetFieldOrProperty("GameMode"));
        var gameDifficultyEnum2 = container2.GameDifficulty;
        var seasonEnum2 = container2.Season;
        var baseVersion2 = (int)(priect2.GetFieldOrProperty("BaseVersion"));
        var versionEnum2 = container2.GameVersion;
        var totalPlayTime2 = container2.TotalPlayTime;
        platform.Copy(container2, container3);
        platform.Move(container2, container1); // overwrite

        platform.Move(container4, container0); // delete

        var gameModeEnum3 = (PresetGameModeEnum)(priect3.GetFieldOrProperty("GameMode"));
        var gameDifficultyEnum3 = container3.GameDifficulty;
        var seasonEnum3 = container3.Season;
        var baseVersion3 = (int)(priect3.GetFieldOrProperty("BaseVersion"));
        var versionEnum3 = container3.GameVersion;
        var totalPlayTime3 = container3.TotalPlayTime;
        platform.Move(container3, container9); // move

        // Assert
        var priect1 = new PrivateObject(container1);
        var priect9 = new PrivateObject(container9);

        Assert.IsFalse(container2.Exists); Assert.IsTrue(container1.Exists);
        Assert.AreEqual(gameModeEnum2, (PresetGameModeEnum)(priect1.GetFieldOrProperty("GameMode")));
        Assert.AreEqual(gameDifficultyEnum2, container1.GameDifficulty);
        Assert.AreEqual(seasonEnum2, container1.Season);
        Assert.AreEqual(baseVersion2, (int)(priect1.GetFieldOrProperty("BaseVersion")));
        Assert.AreEqual(versionEnum2, container1.GameVersion);
        Assert.AreEqual(totalPlayTime2, container1.TotalPlayTime);

        Assert.IsFalse(container0.Exists);
        Assert.IsFalse(container4.Exists);
        Assert.AreEqual(libNOM.io.Globals.Constants.INCOMPATIBILITY_006, container0.IncompatibilityTag);
        Assert.AreEqual(libNOM.io.Globals.Constants.INCOMPATIBILITY_006, container4.IncompatibilityTag);

        Assert.IsFalse(container3.Exists); Assert.IsTrue(container9.Exists);
        Assert.AreEqual(gameModeEnum3, (PresetGameModeEnum)(priect9.GetFieldOrProperty("GameMode")));
        Assert.AreEqual(gameDifficultyEnum3, container9.GameDifficulty);
        Assert.AreEqual(seasonEnum3, container9.Season);
        Assert.AreEqual(baseVersion3, (int)(priect9.GetFieldOrProperty("BaseVersion")));
        Assert.AreEqual(versionEnum3, container9.GameVersion);
        Assert.AreEqual(totalPlayTime3, container9.TotalPlayTime);
    }

    [TestMethod]
    public void T47_Move_0x7D2()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D2", "SaveWizard", "3");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };

        // Act
        var platform = new PlatformPlaystation(path, settings);

        var container0 = platform.GetSaveContainer(0)!; // 1Auto
        var container1 = platform.GetSaveContainer(1)!; // 1Manual
        var container2 = platform.GetSaveContainer(2)!; // 2Auto
        var container3 = platform.GetSaveContainer(3)!; // 2Manual
        var container4 = platform.GetSaveContainer(4)!; // 3Auto
        var container9 = platform.GetSaveContainer(9)!; // 5Manual

        var priect2 = new PrivateObject(container2);
        var priect3 = new PrivateObject(container3);

        var gameModeEnum2 = (PresetGameModeEnum)(priect2.GetFieldOrProperty("GameMode"));
        var gameDifficultyEnum2 = container2.GameDifficulty;
        var seasonEnum2 = container2.Season;
        var baseVersion2 = (int)(priect2.GetFieldOrProperty("BaseVersion"));
        var versionEnum2 = container2.GameVersion;
        var totalPlayTime2 = container2.TotalPlayTime;
        platform.Copy(container2, container3);
        platform.Move(container2, container1); // overwrite

        platform.Move(container4, container0); // delete

        var gameModeEnum3 = (PresetGameModeEnum)(priect3.GetFieldOrProperty("GameMode"));
        var gameDifficultyEnum3 = container3.GameDifficulty;
        var seasonEnum3 = container3.Season;
        var baseVersion3 = (int)(priect3.GetFieldOrProperty("BaseVersion"));
        var versionEnum3 = container3.GameVersion;
        var totalPlayTime3 = container3.TotalPlayTime;
        platform.Move(container3, container9); // move

        // Assert
        var priect1 = new PrivateObject(container1);
        var priect9 = new PrivateObject(container9);

        Assert.IsFalse(container2.Exists); Assert.IsTrue(container1.Exists);
        Assert.AreEqual(gameModeEnum2, (PresetGameModeEnum)(priect1.GetFieldOrProperty("GameMode")));
        Assert.AreEqual(gameDifficultyEnum2, container1.GameDifficulty);
        Assert.AreEqual(seasonEnum2, container1.Season);
        Assert.AreEqual(baseVersion2, (int)(priect1.GetFieldOrProperty("BaseVersion")));
        Assert.AreEqual(versionEnum2, container1.GameVersion);
        Assert.AreEqual(totalPlayTime2, container1.TotalPlayTime);

        Assert.IsFalse(container0.Exists);
        Assert.IsFalse(container4.Exists);
        Assert.AreEqual(libNOM.io.Globals.Constants.INCOMPATIBILITY_006, container0.IncompatibilityTag);
        Assert.AreEqual(libNOM.io.Globals.Constants.INCOMPATIBILITY_006, container4.IncompatibilityTag);

        Assert.IsFalse(container3.Exists); Assert.IsTrue(container9.Exists);
        Assert.AreEqual(gameModeEnum3, (PresetGameModeEnum)(priect9.GetFieldOrProperty("GameMode")));
        Assert.AreEqual(gameDifficultyEnum3, container9.GameDifficulty);
        Assert.AreEqual(seasonEnum3, container9.Season);
        Assert.AreEqual(baseVersion3, (int)(priect9.GetFieldOrProperty("BaseVersion")));
        Assert.AreEqual(versionEnum3, container9.GameVersion);
        Assert.AreEqual(totalPlayTime3, container9.TotalPlayTime);
    }

    [TestMethod]
    public void T50_TransferFromGog_To_0x7D1()
    {
        // Arrange
        var pathGog = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Gog", "DefaultUser");
        var resultsGog = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum Season, int BaseVersion, GameVersionEnum Version)[]
        {
            (2, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, GameVersionEnum.Emergence), // 2Auto
            (3, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, GameVersionEnum.Emergence), // 2Manual
        };
        var userIdentificationGog = ReadUserIdentification(pathGog);

        var offset = 0;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D1", "SaveWizard", "3");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
            UseExternalSourcesForUserIdentification = false,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platformGog = new PlatformGog(pathGog, settings);
        var transfer = platformGog.PrepareTransferSource(1);

        var platform = new PlatformPlaystation(path, settings);
        platform.PrepareTransferDestination(1);
        platform.PrepareTransferDestination(2);

        platform.Transfer(transfer, 1); // overwrite
        var container2 = platform.GetSaveContainer(2)!;
        var priect2 = new PrivateObject(container2);
        var userIdentification2 = (UserIdentificationData)(priect2.GetFieldOrProperty("UserIdentification"));

        platform.Transfer(transfer, 2); // create
        var container4 = platform.GetSaveContainer(4)!;
        var priect4 = new PrivateObject(container4);
        var userIdentification4 = (UserIdentificationData)(priect4.GetFieldOrProperty("UserIdentification"));

        // Assert
        AssertAllAreEqual(1, transfer.TransferBaseUserDecision.Count);
        Assert.AreEqual(6, platform.GetExistingContainers().Count()); // + 1 + 2

        AssertAllAreEqual(userIdentificationGog[0], platformGog.PlatformUserIdentification.LID!, transfer.UserIdentification.LID!);
        AssertAllAreEqual(userIdentificationGog[1], platformGog.PlatformUserIdentification.UID!, transfer.UserIdentification.UID!);
        AssertAllAreEqual(userIdentificationGog[2], platformGog.PlatformUserIdentification.USN!, transfer.UserIdentification.USN!);
        AssertAllAreEqual(userIdentificationGog[3], platformGog.PlatformUserIdentification.PTK!, transfer.UserIdentification.PTK!);

        AssertAllAreEqual(userIdentification[0], platform.PlatformUserIdentification.LID!, userIdentification2.LID!, userIdentification4.LID!);
        AssertAllAreEqual(userIdentification[1], platform.PlatformUserIdentification.UID!, userIdentification2.UID!, userIdentification4.UID!);
        AssertAllAreEqual(userIdentification[2], platform.PlatformUserIdentification.USN!, userIdentification2.USN!, userIdentification4.USN!);
        AssertAllAreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK!, userIdentification2.PTK!, userIdentification4.PTK!);

        for (var i = 0; i < resultsGog.Length; i++)
        {
            var container = platform.GetSaveContainer(resultsGog[i].CollectionIndex + offset)!;
            var priect = new PrivateObject(container);

            Assert.AreEqual(resultsGog[i].Exists, container.Exists);
            Assert.AreEqual(resultsGog[i].IsOld, container.IsOld);
            Assert.AreEqual(resultsGog[i].GameMode, (PresetGameModeEnum)(priect.GetFieldOrProperty("GameMode")));
            Assert.AreEqual(resultsGog[i].GameDifficulty, container.GameDifficulty);
            Assert.AreEqual(resultsGog[i].Season, container.Season);
            Assert.AreEqual(resultsGog[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(resultsGog[i].Version, container.GameVersion);
        }
    }

    [TestMethod]
    public void T51_TransferFromGog_To_0x7D2()
    {
        // Arrange
        var pathGog = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Gog", "DefaultUser");
        var resultsGog = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum Season, int BaseVersion, GameVersionEnum Version)[]
        {
            (2, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, GameVersionEnum.Emergence), // 2Auto
            (3, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, GameVersionEnum.Emergence), // 2Manual
        };
        var userIdentificationGog = ReadUserIdentification(pathGog);

        var offset = 2;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D2", "SaveWizard", "4");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
            UseExternalSourcesForUserIdentification = false,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platformGog = new PlatformGog(pathGog, settings);
        var transfer = platformGog.PrepareTransferSource(1);

        var platform = new PlatformPlaystation(path, settings);
        platform.PrepareTransferDestination(2);
        platform.PrepareTransferDestination(3);

        platform.Transfer(transfer, 2); // overwrite
        var container4 = platform.GetSaveContainer(4)!;
        var priect4 = new PrivateObject(container4);
        var userIdentification4 = (UserIdentificationData)(priect4.GetFieldOrProperty("UserIdentification"));

        platform.Transfer(transfer, 3); // create
        var container6 = platform.GetSaveContainer(6)!;
        var priect6 = new PrivateObject(container6);
        var userIdentification6 = (UserIdentificationData)(priect6.GetFieldOrProperty("UserIdentification"));

        // Assert
        AssertAllAreEqual(1, transfer.TransferBaseUserDecision.Count);
        Assert.AreEqual(8, platform.GetExistingContainers().Count()); // + 2

        AssertAllAreEqual(userIdentificationGog[0], platformGog.PlatformUserIdentification.LID!, transfer.UserIdentification.LID!);
        AssertAllAreEqual(userIdentificationGog[1], platformGog.PlatformUserIdentification.UID!, transfer.UserIdentification.UID!);
        AssertAllAreEqual(userIdentificationGog[2], platformGog.PlatformUserIdentification.USN!, transfer.UserIdentification.USN!);
        AssertAllAreEqual(userIdentificationGog[3], platformGog.PlatformUserIdentification.PTK!, transfer.UserIdentification.PTK!);

        AssertAllAreEqual(userIdentification[0], platform.PlatformUserIdentification.LID!, userIdentification4.LID!, userIdentification6.LID!);
        AssertAllAreEqual(userIdentification[1], platform.PlatformUserIdentification.UID!, userIdentification4.UID!, userIdentification6.UID!);
        AssertAllAreEqual(userIdentification[2], platform.PlatformUserIdentification.USN!, userIdentification4.USN!, userIdentification6.USN!);
        AssertAllAreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK!, userIdentification4.PTK!, userIdentification6.PTK!);

        for (var i = 0; i < resultsGog.Length; i++)
        {
            var container = platform.GetSaveContainer(resultsGog[i].CollectionIndex + offset)!;
            var priect = new PrivateObject(container);

            Assert.AreEqual(resultsGog[i].Exists, container.Exists);
            Assert.AreEqual(resultsGog[i].IsOld, container.IsOld);
            Assert.AreEqual(resultsGog[i].GameMode, (PresetGameModeEnum)(priect.GetFieldOrProperty("GameMode")));
            Assert.AreEqual(resultsGog[i].GameDifficulty, container.GameDifficulty);
            Assert.AreEqual(resultsGog[i].Season, container.Season);
            Assert.AreEqual(resultsGog[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(resultsGog[i].Version, container.GameVersion);
        }
    }

    [TestMethod]
    public void T52_TransferFromMicrosoft_To_0x7D1()
    {
        // Arrange
        var pathMicrosoft = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "0009000000C73498_29070100B936489ABCE8B9AF3980429C");
        var resultsMicrosoft = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum Season, int BaseVersion, GameVersionEnum Version)[]
        {
            (2, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, GameVersionEnum.Frontiers), // 2Auto
            (3, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, GameVersionEnum.Frontiers), // 2Manual
        };
        var userIdentificationMicrosoft = ReadUserIdentification(pathMicrosoft);

        var offset = 0;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D1", "SaveWizard", "3");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
            UseExternalSourcesForUserIdentification = false,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platformMicrosoft = new PlatformMicrosoft(pathMicrosoft, settings);
        var transfer = platformMicrosoft.PrepareTransferSource(1);

        var platform = new PlatformPlaystation(path, settings);
        platform.PrepareTransferDestination(1);
        platform.PrepareTransferDestination(2);

        platform.Transfer(transfer, 1); // overwrite
        var container2 = platform.GetSaveContainer(2)!;
        var priect2 = new PrivateObject(container2);
        var userIdentification2 = (UserIdentificationData)(priect2.GetFieldOrProperty("UserIdentification"));

        platform.Transfer(transfer, 2); // create
        var container4 = platform.GetSaveContainer(4)!;
        var priect4 = new PrivateObject(container4);
        var userIdentification4 = (UserIdentificationData)(priect4.GetFieldOrProperty("UserIdentification"));

        // Assert
        AssertAllAreEqual(8, transfer.TransferBaseUserDecision.Count);
        Assert.AreEqual(6, platform.GetExistingContainers().Count()); // + 1 + 2

        AssertAllAreEqual(userIdentificationMicrosoft[0], platformMicrosoft.PlatformUserIdentification.LID!, transfer.UserIdentification.LID!);
        AssertAllAreEqual(userIdentificationMicrosoft[1], platformMicrosoft.PlatformUserIdentification.UID!, transfer.UserIdentification.UID!);
        AssertAllAreEqual(userIdentificationMicrosoft[2], platformMicrosoft.PlatformUserIdentification.USN!, transfer.UserIdentification.USN!);
        AssertAllAreEqual(userIdentificationMicrosoft[3], platformMicrosoft.PlatformUserIdentification.PTK!, transfer.UserIdentification.PTK!);

        AssertAllAreEqual(userIdentification[0], platform.PlatformUserIdentification.LID!, userIdentification2.LID!, userIdentification4.LID!);
        AssertAllAreEqual(userIdentification[1], platform.PlatformUserIdentification.UID!, userIdentification2.UID!, userIdentification4.UID!);
        AssertAllAreEqual(userIdentification[2], platform.PlatformUserIdentification.USN!, userIdentification2.USN!, userIdentification4.USN!);
        AssertAllAreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK!, userIdentification2.PTK!, userIdentification4.PTK!);

        for (var i = 0; i < resultsMicrosoft.Length; i++)
        {
            var container = platform.GetSaveContainer(resultsMicrosoft[i].CollectionIndex + offset)!;
            var priect = new PrivateObject(container);

            Assert.AreEqual(resultsMicrosoft[i].Exists, container.Exists);
            Assert.AreEqual(resultsMicrosoft[i].IsOld, container.IsOld);
            Assert.AreEqual(resultsMicrosoft[i].GameMode, (PresetGameModeEnum)(priect.GetFieldOrProperty("GameMode")));
            Assert.AreEqual(resultsMicrosoft[i].GameDifficulty, container.GameDifficulty);
            Assert.AreEqual(resultsMicrosoft[i].Season, container.Season);
            Assert.AreEqual(resultsMicrosoft[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(resultsMicrosoft[i].Version, container.GameVersion);
        }
    }

    [TestMethod]
    public void T53_TransferFromMicrosoft_To_0x7D2()
    {
        // Arrange
        var pathMicrosoft = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Microsoft", "wgs", "0009000000C73498_29070100B936489ABCE8B9AF3980429C");
        var resultsMicrosoft = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum Season, int BaseVersion, GameVersionEnum Version)[]
        {
            (2, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, GameVersionEnum.Frontiers), // 2Auto
            (3, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4135, GameVersionEnum.Frontiers), // 2Manual
        };
        var userIdentificationMicrosoft = ReadUserIdentification(pathMicrosoft);

        var offset = 2;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D2", "SaveWizard", "4");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
            UseExternalSourcesForUserIdentification = false,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platformMicrosoft = new PlatformMicrosoft(pathMicrosoft, settings);
        var transfer = platformMicrosoft.PrepareTransferSource(1);

        var platform = new PlatformPlaystation(path, settings);
        platform.PrepareTransferDestination(2);
        platform.PrepareTransferDestination(3);

        platform.Transfer(transfer, 2); // overwrite
        var container4 = platform.GetSaveContainer(4)!;
        var priect4 = new PrivateObject(container4);
        var userIdentification4 = (UserIdentificationData)(priect4.GetFieldOrProperty("UserIdentification"));

        platform.Transfer(transfer, 3); // create
        var container6 = platform.GetSaveContainer(6)!;
        var priect6 = new PrivateObject(container6);
        var userIdentification6 = (UserIdentificationData)(priect6.GetFieldOrProperty("UserIdentification"));

        // Assert
        AssertAllAreEqual(8, transfer.TransferBaseUserDecision.Count);
        Assert.AreEqual(8, platform.GetExistingContainers().Count()); // + 2

        AssertAllAreEqual(userIdentificationMicrosoft[0], platformMicrosoft.PlatformUserIdentification.LID!, transfer.UserIdentification.LID!);
        AssertAllAreEqual(userIdentificationMicrosoft[1], platformMicrosoft.PlatformUserIdentification.UID!, transfer.UserIdentification.UID!);
        AssertAllAreEqual(userIdentificationMicrosoft[2], platformMicrosoft.PlatformUserIdentification.USN!, transfer.UserIdentification.USN!);
        AssertAllAreEqual(userIdentificationMicrosoft[3], platformMicrosoft.PlatformUserIdentification.PTK!, transfer.UserIdentification.PTK!);

        AssertAllAreEqual(userIdentification[1], platform.PlatformUserIdentification.UID!, userIdentification4.UID!, userIdentification6.UID!);
        AssertAllAreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK!, userIdentification4.PTK!, userIdentification6.PTK!);

        for (var i = 0; i < resultsMicrosoft.Length; i++)
        {
            var container = platform.GetSaveContainer(resultsMicrosoft[i].CollectionIndex + offset)!;
            var priect = new PrivateObject(container);

            Assert.AreEqual(resultsMicrosoft[i].Exists, container.Exists);
            Assert.AreEqual(resultsMicrosoft[i].IsOld, container.IsOld);
            Assert.AreEqual(resultsMicrosoft[i].GameMode, (PresetGameModeEnum)(priect.GetFieldOrProperty("GameMode")));
            Assert.AreEqual(resultsMicrosoft[i].GameDifficulty, container.GameDifficulty);
            Assert.AreEqual(resultsMicrosoft[i].Season, container.Season);
            Assert.AreEqual(resultsMicrosoft[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(resultsMicrosoft[i].Version, container.GameVersion);
        }
    }

    [TestMethod]
    public void T54_TransferFromPlaystation_0x7D1_To_0x7D1()
    {
        // Arrange
        var pathPlaystation = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D1", "SaveWizard", "1");
        var resultsPlaystation = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum Season, int BaseVersion, GameVersionEnum Version)[]
        {
            (6, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4134, GameVersionEnum.PrismsWithBytebeatAuthor), // 4Auto
            (7, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Normal, SeasonEnum.None, 4124, GameVersionEnum.LivingShip), // 4Manual
        };
        var userIdentificationPlaystation = ReadUserIdentification(pathPlaystation);

        var offset = -4;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D1", "SaveWizard", "3");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
            UseExternalSourcesForUserIdentification = false,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platformPlaystation = new PlatformPlaystation(pathPlaystation, settings);
        var transfer = platformPlaystation.PrepareTransferSource(3);

        var platform = new PlatformPlaystation(path, settings);
        platform.PrepareTransferDestination(1);
        platform.PrepareTransferDestination(2);

        platform.Transfer(transfer, 1); // overwrite
        var container2 = platform.GetSaveContainer(2)!;
        var priect2 = new PrivateObject(container2);
        var userIdentification2 = (UserIdentificationData)(priect2.GetFieldOrProperty("UserIdentification"));

        platform.Transfer(transfer, 2); // create
        var container4 = platform.GetSaveContainer(4)!;
        var priect4 = new PrivateObject(container4);
        var userIdentification4 = (UserIdentificationData)(priect4.GetFieldOrProperty("UserIdentification"));

        // Assert
        AssertAllAreEqual(8, transfer.TransferBaseUserDecision.Count);
        Assert.AreEqual(6, platform.GetExistingContainers().Count()); // + 1 + 2

        AssertAllAreEqual(userIdentificationPlaystation[0], platformPlaystation.PlatformUserIdentification.LID!, transfer.UserIdentification.LID!);
        AssertAllAreEqual(userIdentificationPlaystation[1], platformPlaystation.PlatformUserIdentification.UID!, transfer.UserIdentification.UID!);
        AssertAllAreEqual(userIdentificationPlaystation[2], platformPlaystation.PlatformUserIdentification.USN!, transfer.UserIdentification.USN!);
        AssertAllAreEqual(userIdentificationPlaystation[3], platformPlaystation.PlatformUserIdentification.PTK!, transfer.UserIdentification.PTK!);

        AssertAllAreEqual(userIdentification[0], platform.PlatformUserIdentification.LID!, userIdentification2.LID!, userIdentification4.LID!);
        AssertAllAreEqual(userIdentification[1], platform.PlatformUserIdentification.UID!, userIdentification2.UID!, userIdentification4.UID!);
        AssertAllAreEqual(userIdentification[2], platform.PlatformUserIdentification.USN!, userIdentification2.USN!, userIdentification4.USN!);
        AssertAllAreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK!, userIdentification2.PTK!, userIdentification4.PTK!);

        for (var i = 0; i < resultsPlaystation.Length; i++)
        {
            var container = platform.GetSaveContainer(resultsPlaystation[i].CollectionIndex + offset)!;
            var priect = new PrivateObject(container);

            Assert.AreEqual(resultsPlaystation[i].Exists, container.Exists);
            Assert.AreEqual(resultsPlaystation[i].IsOld, container.IsOld);
            Assert.AreEqual(resultsPlaystation[i].GameMode, (PresetGameModeEnum)(priect.GetFieldOrProperty("GameMode")));
            Assert.AreEqual(resultsPlaystation[i].GameDifficulty, container.GameDifficulty);
            Assert.AreEqual(resultsPlaystation[i].Season, container.Season);
            Assert.AreEqual(resultsPlaystation[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(resultsPlaystation[i].Version, container.GameVersion);
        }
    }

    [TestMethod]
    public void T55_TransferFromPlaystation_0x7D1_To_0x7D2()
    {
        // Arrange
        var pathPlaystation = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D1", "SaveWizard", "1");
        var resultsPlaystation = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum Season, int BaseVersion, GameVersionEnum Version)[]
        {
            (8, true, false, PresetGameModeEnum.Permadeath, DifficultyPresetTypeEnum.Permadeath, SeasonEnum.None, 4134, GameVersionEnum.PrismsWithBytebeatAuthor), // 5Auto
            (9, true, false, PresetGameModeEnum.Permadeath, DifficultyPresetTypeEnum.Permadeath, SeasonEnum.None, 4134, GameVersionEnum.PrismsWithBytebeatAuthor), // 5Manual
        };
        var userIdentificationPlaystation = ReadUserIdentification(pathPlaystation);

        var offset = -4;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D2", "SaveWizard", "4");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
            UseExternalSourcesForUserIdentification = false,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platformPlaystation = new PlatformPlaystation(pathPlaystation, settings);
        var transfer = platformPlaystation.PrepareTransferSource(4);

        var platform = new PlatformPlaystation(path, settings);
        platform.PrepareTransferDestination(2);
        platform.PrepareTransferDestination(3);

        platform.Transfer(transfer, 2); // overwrite
        var container4 = platform.GetSaveContainer(4)!;
        var priect4 = new PrivateObject(container4);
        var userIdentification4 = (UserIdentificationData)(priect4.GetFieldOrProperty("UserIdentification"));

        platform.Transfer(transfer, 3); // create
        var container6 = platform.GetSaveContainer(6)!;
        var priect6 = new PrivateObject(container6);
        var userIdentification6 = (UserIdentificationData)(priect6.GetFieldOrProperty("UserIdentification"));

        // Assert
        AssertAllAreEqual(1, transfer.TransferBaseUserDecision.Count);
        Assert.AreEqual(8, platform.GetExistingContainers().Count()); // + 2

        AssertAllAreEqual(userIdentificationPlaystation[0], platformPlaystation.PlatformUserIdentification.LID!, transfer.UserIdentification.LID!);
        AssertAllAreEqual(userIdentificationPlaystation[1], platformPlaystation.PlatformUserIdentification.UID!, transfer.UserIdentification.UID!);
        AssertAllAreEqual(userIdentificationPlaystation[2], platformPlaystation.PlatformUserIdentification.USN!, transfer.UserIdentification.USN!);
        AssertAllAreEqual(userIdentificationPlaystation[3], platformPlaystation.PlatformUserIdentification.PTK!, transfer.UserIdentification.PTK!);

        AssertAllAreEqual(userIdentification[0], platform.PlatformUserIdentification.LID!, userIdentification4.LID!, userIdentification6.LID!);
        AssertAllAreEqual(userIdentification[1], platform.PlatformUserIdentification.UID!, userIdentification4.UID!, userIdentification6.UID!);
        AssertAllAreEqual(userIdentification[2], platform.PlatformUserIdentification.USN!, userIdentification4.USN!, userIdentification6.USN!);
        AssertAllAreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK!, userIdentification4.PTK!, userIdentification6.PTK!);

        for (var i = 0; i < resultsPlaystation.Length; i++)
        {
            var container = platform.GetSaveContainer(resultsPlaystation[i].CollectionIndex + offset)!;
            var priect = new PrivateObject(container);

            Assert.AreEqual(resultsPlaystation[i].Exists, container.Exists);
            Assert.AreEqual(resultsPlaystation[i].IsOld, container.IsOld);
            Assert.AreEqual(resultsPlaystation[i].GameMode, (PresetGameModeEnum)(priect.GetFieldOrProperty("GameMode")));
            Assert.AreEqual(resultsPlaystation[i].GameDifficulty, container.GameDifficulty);
            Assert.AreEqual(resultsPlaystation[i].Season, container.Season);
            Assert.AreEqual(resultsPlaystation[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(resultsPlaystation[i].Version, container.GameVersion);
        }
    }

    [TestMethod]
    public void T56_TransferFromPlaystation_0x7D2_To_0x7D1()
    {
        // Arrange
        var pathPlaystation = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D2", "SaveWizard", "1");
        var resultsPlaystation = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum Season, int BaseVersion, GameVersionEnum Version)[]
        {
            (0, true, false, PresetGameModeEnum.Creative, DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4135, GameVersionEnum.Frontiers), // 1Auto
        };
        var userIdentificationPlaystation = ReadUserIdentification(pathPlaystation);

        var offset = 2;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D1", "SaveWizard", "3");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
            UseExternalSourcesForUserIdentification = false,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platformPlaystation = new PlatformPlaystation(pathPlaystation, settings);
        var transfer = platformPlaystation.PrepareTransferSource(0);

        var platform = new PlatformPlaystation(path, settings);
        platform.PrepareTransferDestination(1);
        platform.PrepareTransferDestination(2);

        platform.Transfer(transfer, 1); // overwrite
        var container2 = platform.GetSaveContainer(2)!;
        var priect2 = new PrivateObject(container2);
        var userIdentification2 = (UserIdentificationData)(priect2.GetFieldOrProperty("UserIdentification"));

        platform.Transfer(transfer, 2); // create
        var container4 = platform.GetSaveContainer(4)!;
        var priect4 = new PrivateObject(container4);
        var userIdentification4 = (UserIdentificationData)(priect4.GetFieldOrProperty("UserIdentification"));

        // Assert
        AssertAllAreEqual(0, transfer.TransferBaseUserDecision.Count);
        Assert.AreEqual(4, platform.GetExistingContainers().Count()); // + 1

        AssertAllAreEqual(userIdentificationPlaystation[0], platformPlaystation.PlatformUserIdentification.LID!, transfer.UserIdentification.LID!);
        AssertAllAreEqual(userIdentificationPlaystation[1], platformPlaystation.PlatformUserIdentification.UID!, transfer.UserIdentification.UID!);
        AssertAllAreEqual(userIdentificationPlaystation[2], platformPlaystation.PlatformUserIdentification.USN!, transfer.UserIdentification.USN!);
        AssertAllAreEqual(userIdentificationPlaystation[3], platformPlaystation.PlatformUserIdentification.PTK!, transfer.UserIdentification.PTK!);

        AssertAllAreEqual(userIdentification[0], platform.PlatformUserIdentification.LID!, userIdentification2.LID!, userIdentification4.LID!);
        AssertAllAreEqual(userIdentification[1], platform.PlatformUserIdentification.UID!, userIdentification2.UID!, userIdentification4.UID!);
        AssertAllAreEqual(userIdentification[2], platform.PlatformUserIdentification.USN!, userIdentification2.USN!, userIdentification4.USN!);
        AssertAllAreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK!, userIdentification2.PTK!, userIdentification4.PTK!);

        for (var i = 0; i < resultsPlaystation.Length; i++)
        {
            var container = platform.GetSaveContainer(resultsPlaystation[i].CollectionIndex + offset)!;
            var priect = new PrivateObject(container);

            Assert.AreEqual(resultsPlaystation[i].Exists, container.Exists);
            Assert.AreEqual(resultsPlaystation[i].IsOld, container.IsOld);
            Assert.AreEqual(resultsPlaystation[i].GameMode, (PresetGameModeEnum)(priect.GetFieldOrProperty("GameMode")));
            Assert.AreEqual(resultsPlaystation[i].GameDifficulty, container.GameDifficulty);
            Assert.AreEqual(resultsPlaystation[i].Season, container.Season);
            Assert.AreEqual(resultsPlaystation[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(resultsPlaystation[i].Version, container.GameVersion);
        }
    }

    [TestMethod]
    public void T57_TransferFromPlaystation_0x7D2_To_0x7D2()
    {
        // Arrange
        var pathPlaystation = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D2", "SaveWizard", "2");
        var resultsPlaystation = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum Season, int BaseVersion, GameVersionEnum Version, string SaveName, string SaveSummary)[]
        {
            (2, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4141, GameVersionEnum.WaypointWithAgileStat, "1. Haupt", "An Bord von „Sueyuan XI“-Plattform"), // 2Auto
            (3, true, false, PresetGameModeEnum.Normal, DifficultyPresetTypeEnum.Custom, SeasonEnum.None, 4141, GameVersionEnum.WaypointWithAgileStat, "1. Haupt", "Auf dem Frachter (WF-4 Dawajima)"), // 2Manual
        };
        var userIdentificationPlaystation = ReadUserIdentification(pathPlaystation);

        var offset = 2;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D2", "SaveWizard", "4");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
            UseExternalSourcesForUserIdentification = false,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platformPlaystation = new PlatformPlaystation(pathPlaystation, settings);
        var transfer = platformPlaystation.PrepareTransferSource(1);

        var platform = new PlatformPlaystation(path, settings);
        platform.PrepareTransferDestination(2);
        platform.PrepareTransferDestination(3);

        platform.Transfer(transfer, 2); // overwrite
        var container4 = platform.GetSaveContainer(4)!;
        var priect4 = new PrivateObject(container4);
        var userIdentification4 = (UserIdentificationData)(priect4.GetFieldOrProperty("UserIdentification"));

        platform.Transfer(transfer, 3); // create
        var container6 = platform.GetSaveContainer(6)!;
        var priect6 = new PrivateObject(container6);
        var userIdentification6 = (UserIdentificationData)(priect6.GetFieldOrProperty("UserIdentification"));

        // Assert
        AssertAllAreEqual(13, transfer.TransferBaseUserDecision.Count);
        Assert.AreEqual(8, platform.GetExistingContainers().Count()); // + 2

        AssertAllAreEqual(userIdentificationPlaystation[0], platformPlaystation.PlatformUserIdentification.LID!, transfer.UserIdentification.LID!);
        AssertAllAreEqual(userIdentificationPlaystation[1], platformPlaystation.PlatformUserIdentification.UID!, transfer.UserIdentification.UID!);
        AssertAllAreEqual(userIdentificationPlaystation[2], platformPlaystation.PlatformUserIdentification.USN!, transfer.UserIdentification.USN!);
        AssertAllAreEqual(userIdentificationPlaystation[3], platformPlaystation.PlatformUserIdentification.PTK!, transfer.UserIdentification.PTK!);

        AssertAllAreEqual(userIdentification[0], platform.PlatformUserIdentification.LID!, userIdentification4.LID!, userIdentification6.LID!);
        AssertAllAreEqual(userIdentification[1], platform.PlatformUserIdentification.UID!, userIdentification4.UID!, userIdentification6.UID!);
        AssertAllAreEqual(userIdentification[2], platform.PlatformUserIdentification.USN!, userIdentification4.USN!, userIdentification6.USN!);
        AssertAllAreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK!, userIdentification4.PTK!, userIdentification6.PTK!);

        for (var i = 0; i < resultsPlaystation.Length; i++)
        {
            var container = platform.GetSaveContainer(resultsPlaystation[i].CollectionIndex + offset)!;
            var priect = new PrivateObject(container);

            Assert.AreEqual(resultsPlaystation[i].Exists, container.Exists);
            Assert.AreEqual(resultsPlaystation[i].IsOld, container.IsOld);
            Assert.AreEqual(resultsPlaystation[i].GameMode, (PresetGameModeEnum)(priect.GetFieldOrProperty("GameMode")));
            Assert.AreEqual(resultsPlaystation[i].GameDifficulty, container.GameDifficulty);
            Assert.AreEqual(resultsPlaystation[i].Season, container.Season);
            Assert.AreEqual(resultsPlaystation[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(resultsPlaystation[i].Version, container.GameVersion);
            Assert.AreEqual(resultsPlaystation[i].SaveName, container.SaveName);
            Assert.AreEqual(resultsPlaystation[i].SaveSummary, container.SaveSummary);
        }
    }

    [TestMethod]
    public void T58_TransferFromSteam_To_0x7D1()
    {
        // Arrange
        var pathSteam = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");
        var resultsSteam = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum Season, int BaseVersion, GameVersionEnum Version)[]
        {
            (2, true, false, PresetGameModeEnum.Creative, DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4127, GameVersionEnum.Companions), // 2Auto
            (3, true, false, PresetGameModeEnum.Creative, DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4127, GameVersionEnum.Companions), // 2Manual
        };
        var userIdentificationSteam = ReadUserIdentification(pathSteam);

        var offset = 2;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D1", "SaveWizard", "3");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
            UseExternalSourcesForUserIdentification = false,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platformSteam = new PlatformSteam(pathSteam, settings);
        var transfer = platformSteam.PrepareTransferSource(1);

        var platform = new PlatformPlaystation(path, settings);
        platform.PrepareTransferDestination(1);
        platform.PrepareTransferDestination(2);

        platform.Transfer(transfer, 1); // overwrite
        var container2 = platform.GetSaveContainer(2)!;
        var priect2 = new PrivateObject(container2);
        var userIdentification2 = (UserIdentificationData)(priect2.GetFieldOrProperty("UserIdentification"));

        platform.Transfer(transfer, 2); // create
        var container4 = platform.GetSaveContainer(4)!;
        var priect4 = new PrivateObject(container4);
        var userIdentification4 = (UserIdentificationData)(priect4.GetFieldOrProperty("UserIdentification"));

        // Assert
        AssertAllAreEqual(2, transfer.TransferBaseUserDecision.Count);
        Assert.AreEqual(6, platform.GetExistingContainers().Count()); // + 1 + 2

        AssertAllAreEqual(userIdentificationSteam[0], platformSteam.PlatformUserIdentification.LID!, transfer.UserIdentification.LID!);
        AssertAllAreEqual(userIdentificationSteam[1], platformSteam.PlatformUserIdentification.UID!, transfer.UserIdentification.UID!);
        AssertAllAreEqual(userIdentificationSteam[2], platformSteam.PlatformUserIdentification.USN!, transfer.UserIdentification.USN!);
        AssertAllAreEqual(userIdentificationSteam[3], platformSteam.PlatformUserIdentification.PTK!, transfer.UserIdentification.PTK!);

        AssertAllAreEqual(userIdentification[0], platform.PlatformUserIdentification.LID!, userIdentification2.LID!, userIdentification4.LID!);
        AssertAllAreEqual(userIdentification[1], platform.PlatformUserIdentification.UID!, userIdentification2.UID!, userIdentification4.UID!);
        AssertAllAreEqual(userIdentification[2], platform.PlatformUserIdentification.USN!, userIdentification2.USN!, userIdentification4.USN!);
        AssertAllAreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK!, userIdentification2.PTK!, userIdentification4.PTK!);

        for (var i = 0; i < resultsSteam.Length; i++)
        {
            var container = platform.GetSaveContainer(resultsSteam[i].CollectionIndex + offset)!;
            var priect = new PrivateObject(container);

            Assert.AreEqual(resultsSteam[i].Exists, container.Exists);
            Assert.AreEqual(resultsSteam[i].IsOld, container.IsOld);
            Assert.AreEqual(resultsSteam[i].GameMode, (PresetGameModeEnum)(priect.GetFieldOrProperty("GameMode")));
            Assert.AreEqual(resultsSteam[i].GameDifficulty, container.GameDifficulty);
            Assert.AreEqual(resultsSteam[i].Season, container.Season);
            Assert.AreEqual(resultsSteam[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(resultsSteam[i].Version, container.GameVersion);
        }
    }

    [TestMethod]
    public void T59_TransferFromSteam_To_0x7D2()
    {
        // Arrange
        var pathSteam = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Steam", "st_76561198371877533");
        var resultsSteam = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum Season, int BaseVersion, GameVersionEnum Version)[]
        {
            (2, true, false, PresetGameModeEnum.Creative, DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4127, GameVersionEnum.Companions), // 2Auto
            (3, true, false, PresetGameModeEnum.Creative, DifficultyPresetTypeEnum.Creative, SeasonEnum.None, 4127, GameVersionEnum.Companions), // 2Manual
        };
        var userIdentificationSteam = ReadUserIdentification(pathSteam);

        var offset = 2;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D2", "SaveWizard", "4");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
            UseExternalSourcesForUserIdentification = false,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platformSteam = new PlatformSteam(pathSteam, settings);
        var transfer = platformSteam.PrepareTransferSource(1);

        var platform = new PlatformPlaystation(path, settings);
        platform.PrepareTransferDestination(2);
        platform.PrepareTransferDestination(3);

        platform.Transfer(transfer, 2); // overwrite
        var container4 = platform.GetSaveContainer(4)!;
        var priect4 = new PrivateObject(container4);
        var userIdentification4 = (UserIdentificationData)(priect4.GetFieldOrProperty("UserIdentification"));

        platform.Transfer(transfer, 3); // create
        var container6 = platform.GetSaveContainer(6)!;
        var priect6 = new PrivateObject(container6);
        var userIdentification6 = (UserIdentificationData)(priect6.GetFieldOrProperty("UserIdentification"));

        // Assert
        AssertAllAreEqual(2, transfer.TransferBaseUserDecision.Count);
        Assert.AreEqual(8, platform.GetExistingContainers().Count()); // + 2

        AssertAllAreEqual(userIdentificationSteam[0], platformSteam.PlatformUserIdentification.LID!, transfer.UserIdentification.LID!);
        AssertAllAreEqual(userIdentificationSteam[1], platformSteam.PlatformUserIdentification.UID!, transfer.UserIdentification.UID!);
        AssertAllAreEqual(userIdentificationSteam[2], platformSteam.PlatformUserIdentification.USN!, transfer.UserIdentification.USN!);
        AssertAllAreEqual(userIdentificationSteam[3], platformSteam.PlatformUserIdentification.PTK!, transfer.UserIdentification.PTK!);

        AssertAllAreEqual(userIdentification[0], platform.PlatformUserIdentification.LID!, userIdentification4.LID!, userIdentification6.LID!);
        AssertAllAreEqual(userIdentification[1], platform.PlatformUserIdentification.UID!, userIdentification4.UID!, userIdentification6.UID!);
        AssertAllAreEqual(userIdentification[2], platform.PlatformUserIdentification.USN!, userIdentification4.USN!, userIdentification6.USN!);
        AssertAllAreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK!, userIdentification4.PTK!, userIdentification6.PTK!);

        for (var i = 0; i < resultsSteam.Length; i++)
        {
            var container = platform.GetSaveContainer(resultsSteam[i].CollectionIndex + offset)!;
            var priect = new PrivateObject(container);

            Assert.AreEqual(resultsSteam[i].Exists, container.Exists);
            Assert.AreEqual(resultsSteam[i].IsOld, container.IsOld);
            Assert.AreEqual(resultsSteam[i].GameMode, (PresetGameModeEnum)(priect.GetFieldOrProperty("GameMode")));
            Assert.AreEqual(resultsSteam[i].GameDifficulty, container.GameDifficulty);
            Assert.AreEqual(resultsSteam[i].Season, container.Season);
            Assert.AreEqual(resultsSteam[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(resultsSteam[i].Version, container.GameVersion);
        }
    }

    [TestMethod]
    public void T60_TransferFromSwitch_To_0x7D1()
    {
        // Arrange
        var pathSwitch = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "4");
        var resultsSwitch = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum Season, int BaseVersion, GameVersionEnum Version)[]
        {
            (2, true, false, PresetGameModeEnum.Survival, DifficultyPresetTypeEnum.Survival, SeasonEnum.None, 4139, GameVersionEnum.Endurance), // 2Auto
        };
        var userIdentificationSwitch = ReadUserIdentification(pathSwitch);

        var offset = 2;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D1", "SaveWizard", "3");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
            UseExternalSourcesForUserIdentification = false,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platformSwitch = new PlatformSwitch(pathSwitch, settings);
        var transfer = platformSwitch.PrepareTransferSource(1);

        var platform = new PlatformPlaystation(path, settings);
        platform.PrepareTransferDestination(1);
        platform.PrepareTransferDestination(2);

        platform.Transfer(transfer, 1); // overwrite
        var container2 = platform.GetSaveContainer(2)!;
        var priect2 = new PrivateObject(container2);
        var userIdentification2 = (UserIdentificationData)(priect2.GetFieldOrProperty("UserIdentification"));

        platform.Transfer(transfer, 2); // create
        var container4 = platform.GetSaveContainer(4)!;
        var priect4 = new PrivateObject(container4);
        var userIdentification4 = (UserIdentificationData)(priect4.GetFieldOrProperty("UserIdentification"));

        // Assert
        AssertAllAreEqual(0, transfer.TransferBaseUserDecision.Count);
        Assert.AreEqual(4, platform.GetExistingContainers().Count()); // + 1

        AssertAllAreEqual(userIdentificationSwitch[0], platformSwitch.PlatformUserIdentification.LID!, transfer.UserIdentification.LID!);
        AssertAllAreEqual(userIdentificationSwitch[1], platformSwitch.PlatformUserIdentification.UID!, transfer.UserIdentification.UID!);
        AssertAllAreEqual(userIdentificationSwitch[2], platformSwitch.PlatformUserIdentification.USN!, transfer.UserIdentification.USN!);
        AssertAllAreEqual(userIdentificationSwitch[3], platformSwitch.PlatformUserIdentification.PTK!, transfer.UserIdentification.PTK!);

        AssertAllAreEqual(userIdentification[0], platform.PlatformUserIdentification.LID!, userIdentification2.LID!, userIdentification4.LID!);
        AssertAllAreEqual(userIdentification[1], platform.PlatformUserIdentification.UID!, userIdentification2.UID!, userIdentification4.UID!);
        AssertAllAreEqual(userIdentification[2], platform.PlatformUserIdentification.USN!, userIdentification2.USN!, userIdentification4.USN!);
        AssertAllAreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK!, userIdentification2.PTK!, userIdentification4.PTK!);

        for (var i = 0; i < resultsSwitch.Length; i++)
        {
            var container = platform.GetSaveContainer(resultsSwitch[i].CollectionIndex + offset)!;
            var priect = new PrivateObject(container);

            Assert.AreEqual(resultsSwitch[i].Exists, container.Exists);
            Assert.AreEqual(resultsSwitch[i].IsOld, container.IsOld);
            Assert.AreEqual(resultsSwitch[i].GameMode, (PresetGameModeEnum)(priect.GetFieldOrProperty("GameMode")));
            Assert.AreEqual(resultsSwitch[i].GameDifficulty, container.GameDifficulty);
            Assert.AreEqual(resultsSwitch[i].Season, container.Season);
            Assert.AreEqual(resultsSwitch[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(resultsSwitch[i].Version, container.GameVersion);
        }
    }

    [TestMethod]
    public void T61_TransferFromSwitch_To_0x7D2()
    {
        // Arrange
        var pathSwitch = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Switch", "4");
        var resultsSwitch = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, DifficultyPresetTypeEnum GameDifficulty, SeasonEnum Season, int BaseVersion, GameVersionEnum Version)[]
        {
            (2, true, false, PresetGameModeEnum.Survival, DifficultyPresetTypeEnum.Survival, SeasonEnum.None, 4139, GameVersionEnum.Endurance), // 2Auto
        };
        var userIdentificationSwitch = ReadUserIdentification(pathSwitch);

        var offset = 2;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D2", "SaveWizard", "4");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
            UseExternalSourcesForUserIdentification = false,
        };
        var userIdentification = ReadUserIdentification(path);

        // Act
        var platformSwitch = new PlatformSwitch(pathSwitch, settings);
        var transfer = platformSwitch.PrepareTransferSource(1);

        var platform = new PlatformPlaystation(path, settings);
        platform.PrepareTransferDestination(2);
        platform.PrepareTransferDestination(3);

        platform.Transfer(transfer, 2); // overwrite
        var container4 = platform.GetSaveContainer(4)!;
        var priect4 = new PrivateObject(container4);
        var userIdentification4 = (UserIdentificationData)(priect4.GetFieldOrProperty("UserIdentification"));

        platform.Transfer(transfer, 3); // create
        var container6 = platform.GetSaveContainer(6)!;
        var priect6 = new PrivateObject(container6);
        var userIdentification6 = (UserIdentificationData)(priect6.GetFieldOrProperty("UserIdentification"));

        // Assert
        AssertAllAreEqual(0, transfer.TransferBaseUserDecision.Count);
        Assert.AreEqual(6, platform.GetExistingContainers().Count()); // - 1 + 1

        AssertAllAreEqual(userIdentificationSwitch[0], platformSwitch.PlatformUserIdentification.LID!, transfer.UserIdentification.LID!);
        AssertAllAreEqual(userIdentificationSwitch[1], platformSwitch.PlatformUserIdentification.UID!, transfer.UserIdentification.UID!);
        AssertAllAreEqual(userIdentificationSwitch[2], platformSwitch.PlatformUserIdentification.USN!, transfer.UserIdentification.USN!);
        AssertAllAreEqual(userIdentificationSwitch[3], platformSwitch.PlatformUserIdentification.PTK!, transfer.UserIdentification.PTK!);

        AssertAllAreEqual(userIdentification[0], platform.PlatformUserIdentification.LID!, userIdentification4.LID!, userIdentification6.LID!);
        AssertAllAreEqual(userIdentification[1], platform.PlatformUserIdentification.UID!, userIdentification4.UID!, userIdentification6.UID!);
        AssertAllAreEqual(userIdentification[2], platform.PlatformUserIdentification.USN!, userIdentification4.USN!, userIdentification6.USN!);
        AssertAllAreEqual(userIdentification[3], platform.PlatformUserIdentification.PTK!, userIdentification4.PTK!, userIdentification6.PTK!);

        for (var i = 0; i < resultsSwitch.Length; i++)
        {
            var container = platform.GetSaveContainer(resultsSwitch[i].CollectionIndex + offset)!;
            var priect = new PrivateObject(container);

            Assert.AreEqual(resultsSwitch[i].Exists, container.Exists);
            Assert.AreEqual(resultsSwitch[i].IsOld, container.IsOld);
            Assert.AreEqual(resultsSwitch[i].GameMode, (PresetGameModeEnum)(priect.GetFieldOrProperty("GameMode")));
            Assert.AreEqual(resultsSwitch[i].GameDifficulty, container.GameDifficulty);
            Assert.AreEqual(resultsSwitch[i].Season, container.Season);
            Assert.AreEqual(resultsSwitch[i].BaseVersion, (int)(priect.GetFieldOrProperty("BaseVersion")));
            Assert.AreEqual(resultsSwitch[i].Version, container.GameVersion);
        }
    }
}
