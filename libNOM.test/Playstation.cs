using CommunityToolkit.Diagnostics;
using libNOM.io;
using libNOM.io.Enums;
using libNOM.io.Globals;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Text;

namespace libNOM.test;


[TestClass]
[DeploymentItem("..\\..\\..\\Resources\\TESTSUITE_ARCHIVE.zip")]
public class PlaystationTest : CommonTestInitializeCleanup
{
    #region Constant

    protected const uint META_HEADER = 0xCA55E77E;
    protected static int META_LENGTH_TOTAL_VANILLA => (_usesSaveWizard ? 0x30 : 0x20); // 48 : 32
    protected static int META_LENGTH_TOTAL_WAYPOINT => (_usesSaveWizard ? 0x70 : 0x0); // 28 : 0
    protected static int META_OFFSET => _usesSaveWizard ? 0x40 : 0x0; // 64 : 0

    protected static int MEMORYDAT_META_INDEX_OFFSET => _usesSaveWizard ? 8 : 3;
    protected static int MEMORYDAT_META_INDEX_SIZE => _usesSaveWizard ? 7 : 2;
    protected const uint MEMORYDAT_OFFSET_CONTAINER = 0xE0000U;
    protected static uint MEMORYDAT_OFFSET_DATA => _usesSaveWizard ? 0x1040U : 0x20000U;
    protected const uint MEMORYDAT_SIZE_ACCOUNTDATA = 0x40000U;
    protected const uint MEMORYDAT_SIZE_CONTAINER = 0x300000U;
    protected const uint MEMORYDAT_SIZE_TOTAL = 0x2000000U; // 32 MB

    protected const string SAVEWIZARD_HEADER = "NOMANSKY";
    protected static readonly byte[] SAVEWIZARD_HEADER_BINARY = Encoding.UTF8.GetBytes(SAVEWIZARD_HEADER);

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

        using var reader = new BinaryReader(File.Open(container.MetaFile!.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
        if (_usesSaveStreaming)
        {
            if (_usesSaveWizard)
            {
                meta = reader.ReadBytes(META_LENGTH_TOTAL_WAYPOINT);
            }
        }
        else
        {
            reader.BaseStream.Seek(META_OFFSET + (container.MetaIndex * META_LENGTH_TOTAL_VANILLA), SeekOrigin.Begin);
            meta = reader.ReadBytes(META_LENGTH_TOTAL_VANILLA);
        }

        return GetUInt32(meta);
    }

    private static void AssertCommonMeta(Container container, uint[] metaA, uint[] metaB)
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
                    AssertAllAreEqual((uint)(META_OFFSET), metaA[3], metaB[3]);
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
            AssertAllAreEqual(SAVE_FORMAT_2, BitConverter.ToInt32(memoryDatA, 8), BitConverter.ToInt32(memoryDatB, 8));
            AssertAllAreEqual(0x40, BitConverter.ToInt32(memoryDatA, 12), BitConverter.ToInt32(memoryDatB, 12));
            AssertAllAreEqual(11, BitConverter.ToInt32(memoryDatA, 16), BitConverter.ToInt32(memoryDatB, 16));
            AssertAllAreEqual(memoryDatA.Skip(20).Take(0x40 - 20), memoryDatB.Skip(20).Take(0x40 - 20));
        }
        else
        {
            AssertAllAreEqual(memoryDatA.Length, memoryDatB.Length);
            AssertAllAreEqual(MEMORYDAT_SIZE_TOTAL, memoryDatA.Length, memoryDatB.Length);
        }

        var metaA = new uint[12][]; // 5 slots and 1 account
        var metaB = new uint[12][];

        for (int i = 0; i < metaA.Length; i++)
        {
            metaA[i] = GetUInt32(memoryDatA.Skip(META_OFFSET + i * META_LENGTH_TOTAL_VANILLA).Take(META_LENGTH_TOTAL_VANILLA).ToArray());
            metaB[i] = GetUInt32(memoryDatB.Skip(META_OFFSET + i * META_LENGTH_TOTAL_VANILLA).Take(META_LENGTH_TOTAL_VANILLA).ToArray());

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
                AssertAllAreEqual(MEMORYDAT_SIZE_ACCOUNTDATA, metaA[i][4], metaB[i][4]);
            }
            else if (i < 4 || _usesSaveWizard)
            {
                AssertAllAreEqual(MEMORYDAT_OFFSET_CONTAINER + (i - OFFSET_INDEX) * MEMORYDAT_SIZE_CONTAINER, metaA[i][3], metaB[i][3]);
                AssertAllAreEqual(MEMORYDAT_SIZE_CONTAINER, metaA[i][4], metaB[i][4]);
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
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameModeEnum, SeasonEnum SeasonEnum, int BaseVersion, GameVersionEnum GameVersionEnum)[]
        {
            (0, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4119, GameVersionEnum.BeyondWithVehicleCam), // 1Auto
            (1, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4119, GameVersionEnum.BeyondWithVehicleCam), // 1Manual
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
            Assert.AreEqual(results[i].Exists, container.Exists);
            Assert.AreEqual(results[i].IsOld, container.IsOld);
            Assert.AreEqual(results[i].GameModeEnum, container.GameModeEnum);
            Assert.AreEqual(results[i].SeasonEnum, container.SeasonEnum);
            Assert.AreEqual(results[i].BaseVersion, container.BaseVersion);
            Assert.AreEqual(results[i].GameVersionEnum, container.GameVersionEnum);
        }
    }

    [TestMethod]
    public void T02_Read_0x7D1_Homebrew_2()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D1", "Homebrew", "2");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, SeasonEnum Season, int BaseVersion, GameVersionEnum Version)[]
        {
            (0, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4119, GameVersionEnum.BeyondWithVehicleCam), // 1Auto
            (1, true, true, PresetGameModeEnum.Normal, SeasonEnum.None, 4098, GameVersionEnum.Unknown), // 1Manual
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
            Assert.AreEqual(results[i].Exists, container.Exists);
            Assert.AreEqual(results[i].IsOld, container.IsOld);
            Assert.AreEqual(results[i].GameMode, container.GameModeEnum);
            Assert.AreEqual(results[i].Season, container.SeasonEnum);
            Assert.AreEqual(results[i].BaseVersion, container.BaseVersion);
            Assert.AreEqual(results[i].Version, container.GameVersionEnum);
        }
    }

    [TestMethod]
    public void T03_Read_0x7D1_SaveWizard_1()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D1", "SaveWizard", "1");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, SeasonEnum Season, int BaseVersion, GameVersionEnum Version)[]
        {
            (0, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4134, GameVersionEnum.PrismsWithBytebeatAuthor), // 1Auto
            (1, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4134, GameVersionEnum.PrismsWithBytebeatAuthor), // 1Manual
            (2, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4134, GameVersionEnum.PrismsWithBytebeatAuthor), // 2Auto
            (3, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4134, GameVersionEnum.PrismsWithBytebeatAuthor), // 2Manual
            (4, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4134, GameVersionEnum.PrismsWithBytebeatAuthor), // 3Auto
            (5, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4133, GameVersionEnum.PrismsWithBytebeatAuthor), // 3Manual
            (6, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4134, GameVersionEnum.PrismsWithBytebeatAuthor), // 4Auto
            (7, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4124, GameVersionEnum.LivingShip), // 4Manual
            (8, true, false, PresetGameModeEnum.Permadeath, SeasonEnum.None, 4134, GameVersionEnum.PrismsWithBytebeatAuthor), // 5Auto
            (9, true, false, PresetGameModeEnum.Permadeath, SeasonEnum.None, 4134, GameVersionEnum.PrismsWithBytebeatAuthor), // 5Manual
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
            Assert.AreEqual(results[i].Exists, container.Exists);
            Assert.AreEqual(results[i].IsOld, container.IsOld);
            Assert.AreEqual(results[i].GameMode, container.GameModeEnum);
            Assert.AreEqual(results[i].Season, container.SeasonEnum);
            Assert.AreEqual(results[i].BaseVersion, container.BaseVersion);
            Assert.AreEqual(results[i].Version, container.GameVersionEnum);
        }
    }

    [TestMethod]
    public void T04_Read_0x7D1_SaveWizard_2()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D1", "SaveWizard", "2");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, SeasonEnum Season, int BaseVersion, GameVersionEnum Version)[]
        {
            (0, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4126, GameVersionEnum.Origins), // 1Auto
            (1, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4134, GameVersionEnum.PrismsWithBytebeatAuthor), // 1Manual
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
            Assert.AreEqual(results[i].Exists, container.Exists);
            Assert.AreEqual(results[i].IsOld, container.IsOld);
            Assert.AreEqual(results[i].GameMode, container.GameModeEnum);
            Assert.AreEqual(results[i].Season, container.SeasonEnum);
            Assert.AreEqual(results[i].BaseVersion, container.BaseVersion);
            Assert.AreEqual(results[i].Version, container.GameVersionEnum);
        }
    }

    [TestMethod]
    public void T05_Read_0x7D1_SaveWizard_3()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D1", "SaveWizard", "3");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, SeasonEnum Season, int BaseVersion, GameVersionEnum Version)[]
        {
            (0, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4126, GameVersionEnum.Origins), // 1Auto
            (1, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4126, GameVersionEnum.Origins), // 1Manual
            (2, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4134, GameVersionEnum.PrismsWithBytebeatAuthor), // 2Auto
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
            Assert.AreEqual(results[i].Exists, container.Exists);
            Assert.AreEqual(results[i].IsOld, container.IsOld);
            Assert.AreEqual(results[i].GameMode, container.GameModeEnum);
            Assert.AreEqual(results[i].Season, container.SeasonEnum);
            Assert.AreEqual(results[i].BaseVersion, container.BaseVersion);
            Assert.AreEqual(results[i].Version, container.GameVersionEnum);
        }
    }

    [TestMethod]
    public void T10_Read_0x7D2_Homebrew_1()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D2", "Homebrew", "1");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameModeEnum, SeasonEnum SeasonEnum, int BaseVersion, GameVersionEnum GameVersionEnum)[]
        {
            (0, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4119, GameVersionEnum.BeyondWithVehicleCam), // 1Auto
            (1, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4135, GameVersionEnum.Emergence), // 1Manual
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
            Assert.AreEqual(results[i].Exists, container.Exists);
            Assert.AreEqual(results[i].IsOld, container.IsOld);
            Assert.AreEqual(results[i].GameModeEnum, container.GameModeEnum);
            Assert.AreEqual(results[i].SeasonEnum, container.SeasonEnum);
            Assert.AreEqual(results[i].BaseVersion, container.BaseVersion);
            Assert.AreEqual(results[i].GameVersionEnum, container.GameVersionEnum);
        }
    }

    [TestMethod]
    public void T11_Read_0x7D2_SaveWizard_1()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D2", "SaveWizard", "1");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, SeasonEnum Season, int BaseVersion, GameVersionEnum Version)[]
        {
            (0, true, false, PresetGameModeEnum.Creative, SeasonEnum.None, 4135, GameVersionEnum.Frontiers), // 1Auto
            (2, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4135, GameVersionEnum.Frontiers), // 2Auto
            (3, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4135, GameVersionEnum.Frontiers), // 2Manual
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
            Assert.AreEqual(results[i].Exists, container.Exists);
            Assert.AreEqual(results[i].IsOld, container.IsOld);
            Assert.AreEqual(results[i].GameMode, container.GameModeEnum);
            Assert.AreEqual(results[i].Season, container.SeasonEnum);
            Assert.AreEqual(results[i].BaseVersion, container.BaseVersion);
            Assert.AreEqual(results[i].Version, container.GameVersionEnum);
        }
    }

    [TestMethod]
    public void T12_Read_0x7D2_SaveWizard_2()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D2", "SaveWizard", "2");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, SeasonEnum Season, int BaseVersion, GameVersionEnum Version)[]
        {
            (2, true, false, PresetGameModeEnum.Unspecified, SeasonEnum.None, 4141, GameVersionEnum.WaypointWithAgileStat), // 2Auto
            (3, true, false, PresetGameModeEnum.Unspecified, SeasonEnum.None, 4141, GameVersionEnum.WaypointWithAgileStat), // 2Manual
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
            Assert.AreEqual(results[i].Exists, container.Exists);
            Assert.AreEqual(results[i].IsOld, container.IsOld);
            Assert.AreEqual(results[i].GameMode, container.GameModeEnum);
            Assert.AreEqual(results[i].Season, container.SeasonEnum);
            Assert.AreEqual(results[i].BaseVersion, container.BaseVersion);
            Assert.AreEqual(results[i].Version, container.GameVersionEnum);
        }
    }

    [TestMethod]
    public void T13_Read_0x7D2_SaveWizard_3()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D2", "SaveWizard", "3");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, SeasonEnum Season, int BaseVersion, GameVersionEnum Version)[]
        {
            (0, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4135, GameVersionEnum.Frontiers), // 1Auto
            (1, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4135, GameVersionEnum.Frontiers), // 1Manual
            (2, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4134, GameVersionEnum.PrismsWithBytebeatAuthor), // 2Auto
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
            Assert.AreEqual(results[i].Exists, container.Exists);
            Assert.AreEqual(results[i].IsOld, container.IsOld);
            Assert.AreEqual(results[i].GameMode, container.GameModeEnum);
            Assert.AreEqual(results[i].Season, container.SeasonEnum);
            Assert.AreEqual(results[i].BaseVersion, container.BaseVersion);
            Assert.AreEqual(results[i].Version, container.GameVersionEnum);
        }
    }

    [TestMethod]
    public void T14_Read_0x7D2_SaveWizard_4()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D2", "SaveWizard", "4");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, SeasonEnum Season, int BaseVersion, GameVersionEnum Version)[]
        {
            (0, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4134, GameVersionEnum.PrismsWithBytebeatAuthor), // 1Auto
            (1, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4134, GameVersionEnum.PrismsWithBytebeatAuthor), // 1Manual
            (2, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4135, GameVersionEnum.Frontiers), // 2Auto
            (3, true, false, PresetGameModeEnum.Normal, SeasonEnum.None, 4135, GameVersionEnum.Frontiers), // 2Manual
            (4, true, false, PresetGameModeEnum.Seasonal, SeasonEnum.Cartographers, 4135, GameVersionEnum.Frontiers), // 3Auto
            (5, true, false, PresetGameModeEnum.Seasonal, SeasonEnum.Cartographers, 4135, GameVersionEnum.Frontiers), // 3Manual
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
            Assert.AreEqual(results[i].Exists, container.Exists);
            Assert.AreEqual(results[i].IsOld, container.IsOld);
            Assert.AreEqual(results[i].GameMode, container.GameModeEnum);
            Assert.AreEqual(results[i].Season, container.SeasonEnum);
            Assert.AreEqual(results[i].BaseVersion, container.BaseVersion);
            Assert.AreEqual(results[i].Version, container.GameVersionEnum);
        }
    }

    [TestMethod]
    public void T15_Read_0x7D2_SaveWizard_5()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D2", "SaveWizard", "5");
        var results = new (int CollectionIndex, bool Exists, bool IsOld, PresetGameModeEnum GameMode, SeasonEnum Season, int BaseVersion, GameVersionEnum Version)[]
        {
            (11, true, false, PresetGameModeEnum.Unspecified, SeasonEnum.None, 4145, GameVersionEnum.Interceptor), // 6Manual
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
            Assert.AreEqual(results[i].Exists, container.Exists);
            Assert.AreEqual(results[i].IsOld, container.IsOld);
            Assert.AreEqual(results[i].GameMode, container.GameModeEnum);
            Assert.AreEqual(results[i].Season, container.SeasonEnum);
            Assert.AreEqual(results[i].BaseVersion, container.BaseVersion);
            Assert.AreEqual(results[i].Version, container.GameVersionEnum);
        }
    }

    [TestMethod]
    public void T20_Write_Default_0x7D1_Homebrew()
    {
        _usesSaveStreaming = false;
        _usesSaveWizard = false;

        var now = DateTimeOffset.UtcNow;
        var nowUnix = now.ToUnixTimeSeconds();
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D1", "Homebrew", "1");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };
        var writeCallback = false;

        // Act
        var platformA = new PlatformPlaystation(path, settings);
        var containerA = platformA.GetSaveContainer(0)!;
        var memoryDatA = File.ReadAllBytes(Path.Combine(path, "memory.dat"));
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
        var memoryDatB = File.ReadAllBytes(Path.Combine(path, "memory.dat"));
        var metaB = DecryptMeta(containerB);

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

        AssertCommonMeta(containerA, metaA, metaB);
        AssertAllAreEqual(4119, (uint)(containerA.BaseVersion), (uint)(containerB.BaseVersion));
        AssertAllAreEqual(2, (uint)(containerA.MetaIndex), (uint)(containerB.MetaIndex), metaA[5], metaB[5]);
    }

    [TestMethod]
    public void T21_Write_Default_0x7D1_SaveWizard()
    {
        _usesSaveStreaming = false;
        _usesSaveWizard = true;

        var now = DateTimeOffset.UtcNow;
        var nowUnix = now.ToUnixTimeSeconds();
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D1", "SaveWizard", "1");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };
        var writeCallback = false;

        // Act
        var platformA = new PlatformPlaystation(path, settings);
        var containerA = platformA.GetSaveContainer(0)!;
        var memoryDatA = File.ReadAllBytes(Path.Combine(path, "memory.dat"));
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
        var memoryDatB = File.ReadAllBytes(Path.Combine(path, "memory.dat"));
        var metaB = DecryptMeta(containerB);

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

        AssertCommonMeta(containerA, metaA, metaB);
        AssertAllAreEqual(4134, (uint)(containerA.BaseVersion), (uint)(containerB.BaseVersion));
        AssertAllAreEqual(2, (uint)(containerA.MetaIndex), (uint)(containerB.MetaIndex), metaA[5], metaB[5]);
    }

    [TestMethod]
    public void T22_Write_Default_0x7D2_Homebrew()
    {
        _usesSaveStreaming = true;
        _usesSaveWizard = false;

        var now = DateTimeOffset.UtcNow;
        var nowUnix = now.ToUnixTimeSeconds();
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D2", "Homebrew", "1");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
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

        AssertCommonMeta(containerA, metaA, metaB);
    }

    [TestMethod]
    public void T23_Write_Default_0x7D2_Homebrew_Account()
    {
        _usesSaveStreaming = true;
        _usesSaveWizard = false;

        var now = DateTimeOffset.UtcNow;
        var nowUnix = now.ToUnixTimeSeconds();
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D2", "Homebrew", "1");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
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

        AssertCommonMeta(containerA, metaA, metaB);
    }

    [TestMethod]
    public void T24_Write_Default_0x7D2_SaveWizard()
    {
        _usesSaveStreaming = true;
        _usesSaveWizard = true;

        var now = DateTimeOffset.UtcNow;
        var nowUnix = now.ToUnixTimeSeconds();
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D2", "SaveWizard", "1");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
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

        AssertCommonMeta(containerA, metaA, metaB);
    }

    [TestMethod]
    public void T25_Write_SetLastWriteTime_False()
    {
        _usesSaveStreaming = false;
        _usesSaveWizard = false;

        var now = DateTimeOffset.UtcNow;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D1", "Homebrew", "1");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            SetLastWriteTime = false,
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
    public void T26_Write_WriteAlways_True_0x7D1()
    {
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
    public void T27_Write_WriteAlways_True_0x7D2()
    {
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
    public void T28_Write_WriteAlways_False_0x7D1()
    {
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
    public void T29_Write_WriteAlways_False_0x7D2()
    {
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
    public void T30_FileSystemWatcher_0x7D1()
    {
        _usesSaveStreaming = false;
        _usesSaveWizard = false;

        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D1", "Homebrew", "1");
        var pathSave = Path.Combine(path, "memory.dat");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            Watcher = true,
        };

        // Act
        var bytes = File.ReadAllBytes(pathSave);
        var platform = new PlatformPlaystation(path, settings);

        var container = platform.GetSaveContainer(0)!;
        platform.Load(container);

        File.WriteAllBytes(pathSave, bytes);
        Thread.Sleep(FILESYSTEMWATCHER_SLEEP);
        var watchers1 = platform.GetWatcherContainers();
        var count1 = watchers1.Count();
        var synced1 = container.IsSynced;

        container.SetJsonValue(UNITS_NEW_AMOUNT, UNITS_JSON_PATH);
        var synced2 = container.IsSynced;

        File.WriteAllBytes(pathSave, bytes);
        Thread.Sleep(FILESYSTEMWATCHER_SLEEP);
        var watchers2 = platform.GetWatcherContainers();
        var count2 = watchers2.Count();
        var synced3 = container.IsSynced;

        var watcherContainer2 = watchers2.FirstOrDefault();
        Guard.IsNotNull(watcherContainer2);
        platform.OnWatcherDecision(watcherContainer2, false);
        var synced4 = container.IsSynced;

        File.WriteAllBytes(pathSave, bytes);
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
    public void T31_FileSystemWatcher_0x7D2()
    {
        _usesSaveStreaming = true;
        _usesSaveWizard = false;

        var now = DateTimeOffset.UtcNow;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D2", "Homebrew", "1");
        var pathSave = Path.Combine(path, "savedata02.hg");
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            Watcher = true,
        };

        // Act
        var bytes = File.ReadAllBytes(pathSave);
        var platform = new PlatformPlaystation(path, settings);

        var container = platform.GetSaveContainer(0)!;
        platform.Load(container);

        File.WriteAllBytes(pathSave, bytes);
        Thread.Sleep(FILESYSTEMWATCHER_SLEEP);
        var watchers1 = platform.GetWatcherContainers();
        var count1 = watchers1.Count();
        var synced1 = container.IsSynced;

        container.SetJsonValue(UNITS_NEW_AMOUNT, UNITS_JSON_PATH);
        var synced2 = container.IsSynced;

        File.WriteAllBytes(pathSave, bytes);
        Thread.Sleep(FILESYSTEMWATCHER_SLEEP);
        var watchers2 = platform.GetWatcherContainers();
        var count2 = watchers2.Count();
        var synced3 = container.IsSynced;

        var watcherContainer2 = watchers2.FirstOrDefault();
        Guard.IsNotNull(watcherContainer2);
        platform.OnWatcherDecision(watcherContainer2, false);
        var synced4 = container.IsSynced;

        File.WriteAllBytes(pathSave, bytes);
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
    public void T32_Copy_0x7D1()
    {
        // Arrange
        _usesSaveStreaming = false;
        _usesSaveWizard = false;

        var now = DateTimeOffset.UtcNow;
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "0x7D1", "Homebrew", "1");
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
        platform.Copy(container0, container1); // 1Auto -> 1Manual (create)
        platform.Copy(container6, container4); // 4Auto -> 3Auto (delete)

        // Assert
        Assert.IsTrue(container2.Exists);
        Assert.AreEqual(container0.GameModeEnum, container2.GameModeEnum);
        Assert.AreEqual(container0.SeasonEnum, container2.SeasonEnum);
        Assert.AreEqual(container0.BaseVersion, container2.BaseVersion);
        Assert.AreEqual(container0.GameVersionEnum, container2.GameVersionEnum);

        Assert.IsTrue(container1.Exists);
        Assert.AreEqual(container0.GameModeEnum, container1.GameModeEnum);
        Assert.AreEqual(container0.SeasonEnum, container1.SeasonEnum);
        Assert.AreEqual(container0.BaseVersion, container1.BaseVersion);
        Assert.AreEqual(container0.GameVersionEnum, container1.GameVersionEnum);

        Assert.IsFalse(container4.Exists);
    }

    [TestMethod]
    public void T33_Copy_0x7D2()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "4");
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
        platform.Copy(container0, container1); // 1Auto -> 1Manual (create)
        platform.Copy(container6, container4); // 4Auto -> 3Auto (delete)

        // Assert
        Assert.IsTrue(container2.Exists);
        Assert.AreEqual(container0.GameModeEnum, container2.GameModeEnum);
        Assert.AreEqual(container0.SeasonEnum, container2.SeasonEnum);
        Assert.AreEqual(container0.BaseVersion, container2.BaseVersion);
        Assert.AreEqual(container0.GameVersionEnum, container2.GameVersionEnum);

        Assert.IsTrue(container1.Exists);
        Assert.AreEqual(container0.GameModeEnum, container1.GameModeEnum);
        Assert.AreEqual(container0.SeasonEnum, container1.SeasonEnum);
        Assert.AreEqual(container0.BaseVersion, container1.BaseVersion);
        Assert.AreEqual(container0.GameVersionEnum, container1.GameVersionEnum);

        Assert.IsFalse(container4.Exists);
    }

    [TestMethod]
    public void T34_Delete_0x7D1()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "4");
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
    public void T35_Delete_0x7D2()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "4");
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
    public void T36_Move_0x7D1()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "4");
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
        var container5 = platform.GetSaveContainer(5)!; // 3Manual
        var container9 = platform.GetSaveContainer(9)!; // 5Manual

        var gameModeEnum2 = container2.GameModeEnum;
        var seasonEnum2 = container2.SeasonEnum;
        var baseVersion2 = container2.BaseVersion;
        var versionEnum2 = container2.GameVersionEnum;
        platform.Copy(container4, container5);
        platform.Move(container2, container5); // overwrite

        platform.Move(container1, container0); // delete

        var gameModeEnum4 = container4.GameModeEnum;
        var seasonEnum4 = container4.SeasonEnum;
        var baseVersion4 = container4.BaseVersion;
        var versionEnum4 = container4.GameVersionEnum;
        platform.Move(container4, container9); // move

        // Assert
        Assert.IsFalse(container2.Exists);
        Assert.IsTrue(container5.Exists);
        Assert.AreEqual(gameModeEnum2, container5.GameModeEnum);
        Assert.AreEqual(seasonEnum2, container5.SeasonEnum);
        Assert.AreEqual(baseVersion2, container5.BaseVersion);
        Assert.AreEqual(versionEnum2, container5.GameVersionEnum);

        Assert.IsFalse(container0.Exists);
        Assert.IsFalse(container1.Exists);
        Assert.AreEqual(libNOM.io.Globals.Constants.INCOMPATIBILITY_006, container0.IncompatibilityTag);
        Assert.AreEqual(libNOM.io.Globals.Constants.INCOMPATIBILITY_006, container1.IncompatibilityTag);

        Assert.IsFalse(container4.Exists);
        Assert.IsTrue(container9.Exists);
        Assert.AreEqual(gameModeEnum4, container9.GameModeEnum);
        Assert.AreEqual(seasonEnum4, container9.SeasonEnum);
        Assert.AreEqual(baseVersion4, container9.BaseVersion);
        Assert.AreEqual(versionEnum4, container9.GameVersionEnum);
    }

    [TestMethod]
    public void T37_Move_0x7D2()
    {
        // Arrange
        var path = Path.Combine(nameof(Properties.Resources.TESTSUITE_ARCHIVE), "Platform", "Playstation", "4");
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
        var container5 = platform.GetSaveContainer(5)!; // 3Manual
        var container9 = platform.GetSaveContainer(9)!; // 5Manual

        var gameModeEnum2 = container2.GameModeEnum;
        var seasonEnum2 = container2.SeasonEnum;
        var baseVersion2 = container2.BaseVersion;
        var versionEnum2 = container2.GameVersionEnum;
        platform.Copy(container4, container5);
        platform.Move(container2, container5); // overwrite

        platform.Move(container1, container0); // delete

        var gameModeEnum4 = container4.GameModeEnum;
        var seasonEnum4 = container4.SeasonEnum;
        var baseVersion4 = container4.BaseVersion;
        var versionEnum4 = container4.GameVersionEnum;
        platform.Move(container4, container9); // move

        // Assert
        Assert.IsFalse(container2.Exists);
        Assert.IsTrue(container5.Exists);
        Assert.AreEqual(gameModeEnum2, container5.GameModeEnum);
        Assert.AreEqual(seasonEnum2, container5.SeasonEnum);
        Assert.AreEqual(baseVersion2, container5.BaseVersion);
        Assert.AreEqual(versionEnum2, container5.GameVersionEnum);

        Assert.IsFalse(container0.Exists);
        Assert.IsFalse(container1.Exists);
        Assert.AreEqual(libNOM.io.Globals.Constants.INCOMPATIBILITY_006, container0.IncompatibilityTag);
        Assert.AreEqual(libNOM.io.Globals.Constants.INCOMPATIBILITY_006, container1.IncompatibilityTag);

        Assert.IsFalse(container4.Exists);
        Assert.IsTrue(container9.Exists);
        Assert.AreEqual(gameModeEnum4, container9.GameModeEnum);
        Assert.AreEqual(seasonEnum4, container9.SeasonEnum);
        Assert.AreEqual(baseVersion4, container9.BaseVersion);
        Assert.AreEqual(versionEnum4, container9.GameVersionEnum);
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
