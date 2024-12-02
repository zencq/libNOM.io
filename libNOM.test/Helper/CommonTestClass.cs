using System.Text;

using CommunityToolkit.Diagnostics;

using libNOM.io;
using libNOM.io.Interfaces;
using libNOM.io.Models;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;

namespace libNOM.test.Helper;


[TestClass]
public abstract class CommonTestClass
{
    #region Constant

    protected const int FILESYSTEMWATCHER_SLEEP = 10000; // 10 seconds
    protected const int OFFSET_INDEX = 2;
    protected const int TICK_DIVISOR = 10000;

    protected static readonly TimeSpan DELTA_TIMESPAN = TimeSpan.FromHours(2);

    protected static readonly long DELTA_TIMESPAN_SECONDS = (long)(DELTA_TIMESPAN.TotalSeconds);
    protected static readonly long DELTA_TIMESPAN_TICKS = DELTA_TIMESPAN.Ticks;

    protected const string DIRECTORY_TESTSUITE_ARCHIVE = "TESTSUITE_ARCHIVE";
    protected const string DIRECTORY_TESTSUITE_ARCHIVE_TEMPLATE = "TESTSUITE_ARCHIVE_TEMPLATE";

    protected const uint META_FORMAT_2 = 0x7D1; // 2001
    protected const uint META_FORMAT_3 = 0x7D2; // 2002
    protected const uint META_FORMAT_4 = 0x7D3; // 2003

    protected static readonly int[] MUSICVOLUME_INDICES = [1, 7];
    protected const string MUSICVOLUME_JSONPATH = "UserSettingsData.MusicVolume";
    protected const int MUSICVOLUME_NEW_AMOUNT = 100;

    protected static readonly int[] UNITS_INDICES = [2, 48];
    protected const string UNITS_JSONPATH_KEY = "UNITS";
    protected static readonly string[] UNITS_JSONPATH = ["", "PlayerStateData.Units", "", "{0}.PlayerStateData.Units"];
    protected const int UNITS_NEW_AMOUNT = 29070100;

    #endregion

    // //

    #region Assert

    protected static void AssertAllAreEqual(IEnumerable<byte> expected, params IEnumerable<byte>[] actual)
    {
        foreach (var value in actual)
            Assert.IsTrue(expected.SequenceEqual(value));
    }

    protected static void AssertAllAreEqual(int expected, params int[] actual)
    {
        foreach (var value in actual)
            Assert.AreEqual(expected, value);
    }

    protected static void AssertAllAreEqual(uint expected, params uint[] actual)
    {
        foreach (var value in actual)
            Assert.AreEqual(expected, value);
    }

    protected static void AssertAllAreEqual(long expected, params long[] actual)
    {
        foreach (var value in actual)
            Assert.AreEqual(expected, value);
    }

    protected static void AssertAllAreEqual(string expected, params string?[] actual)
    {
        foreach (var value in actual)
            Assert.AreEqual(expected, value);
    }

    protected static void AssertAllNotZero(params IEnumerable<byte>[] actual)
    {
        foreach (var value in actual)
            if (value.Any(i => i == 0))
                throw new AssertFailedException();
    }

    protected static void AssertAllNotZero(params uint[] actual)
    {
        foreach (var value in actual)
            if (value == 0)
                throw new AssertFailedException();
    }

    protected static void AssertAllNotZero(params IEnumerable<uint>[] actual)
    {
        foreach (var value in actual)
            if (value.Any(i => i == 0))
                throw new AssertFailedException();
    }

    protected static void AssertAllZero(params IEnumerable<byte>[] actual)
    {
        foreach (var value in actual)
            if (value.Any(i => i != 0))
                throw new AssertFailedException();
    }

    protected static void AssertAllZero(params uint[] actual)
    {
        foreach (var value in actual)
            if (value != 0)
                throw new AssertFailedException();
    }

    protected static void AssertAllZero(params IEnumerable<uint>[] actual)
    {
        foreach (var value in actual)
            if (value.Any(i => i != 0))
                throw new AssertFailedException();
    }

    protected static void AssertCommonFileOperation(FileOperationResults resultA, FileOperationResults resultB)
    {
        Assert.AreEqual(resultA.GameMode, resultB.GameMode);
        Assert.AreEqual(resultA.Difficulty, resultB.Difficulty);
        Assert.AreEqual(resultA.Season, resultB.Season);
        Assert.AreEqual(resultA.BaseVersion, resultB.BaseVersion);
        Assert.AreEqual(resultA.GameVersion, resultB.GameVersion);
        Assert.AreEqual(resultA.TotalPlayTime, resultB.TotalPlayTime);
    }

    protected static void AssertCommonRead(ReadResults[] results, IPlatform? platform, bool expectAccountData, UserIdentification userIdentification)
    {
        ArgumentNullException.ThrowIfNull(platform, nameof(platform));

        Assert.AreEqual(results.Length, GetExistingContainers(platform).Count());

        if (expectAccountData)
        {
            Assert.IsTrue(platform.HasAccountData);

            var container = platform.GetAccountContainer()!;

            Assert.AreEqual(-2, container.CollectionIndex);
            Assert.AreEqual("AccountData", container.Identifier);
            Assert.AreEqual(0, container.UnknownKeys.Count, $"{container.Identifier}.UnknownKeys: {string.Join(" // ", container.UnknownKeys)}");
        }
        else
            Assert.IsFalse(platform.HasAccountData);

        Assert.AreEqual(userIdentification.LID, platform.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification.UID, platform.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification.USN, platform.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification.PTK, platform.PlatformUserIdentification.PTK);

        for (var i = 0; i < results.Length; i++)
        {
            var container = platform.GetSaveContainer(results[i].CollectionIndex)!;
            var expected = results[i];

            var priject = new PrivateObject(container);

            Assert.AreEqual(expected.CollectionIndex, container.CollectionIndex);
            Assert.AreEqual(expected.Identifier, container.Identifier);
            Assert.AreEqual(expected.Exists, container.Exists);
            Assert.AreEqual(expected.IsCompatible, container.IsCompatible);
            Assert.AreEqual(expected.IsOld, container.IsOld);
            Assert.AreEqual(expected.HasBase, container.HasBase);
            Assert.AreEqual(expected.HasFreighter, container.HasFreighter);
            Assert.AreEqual(expected.HasSettlement, container.HasSettlement);
            Assert.AreEqual(expected.HasActiveExpedition, container.HasActiveExpedition);
            Assert.AreEqual(expected.CanSwitchContext, container.CanSwitchContext);
            Assert.AreEqual(expected.ActiveContext, container.ActiveContext);
            Assert.AreEqual(expected.GameMode, priject.GetFieldOrProperty(nameof(ReadResults.GameMode)).ToString());
            Assert.AreEqual(expected.Difficulty, container.Difficulty);
            Assert.AreEqual(expected.Season, container.Season);
            Assert.AreEqual(expected.BaseVersion, (int)(priject.GetFieldOrProperty(nameof(ReadResults.BaseVersion))));
            Assert.AreEqual(expected.SaveVersion, (int)(priject.GetFieldOrProperty(nameof(ReadResults.SaveVersion))));
            Assert.AreEqual(expected.GameVersion, container.GameVersion);
            Assert.AreEqual(expected.SaveName, container.SaveName);
            Assert.AreEqual(expected.SaveSummary, container.SaveSummary);
            Assert.AreEqual(expected.TotalPlayTime, container.TotalPlayTime);
            Assert.AreEqual(0, container.UnknownKeys.Count, $"{container.Identifier}.UnknownKeys: {string.Join(" // ", container.UnknownKeys)}");
        }
    }

    protected static void AssertCommonSourceTransferData(UserIdentification userIdentification, IPlatform platform, TransferData transfer)
    {
        AssertAllAreEqual(userIdentification.LID!, platform.PlatformUserIdentification.LID, transfer.UserIdentification.LID);
        AssertAllAreEqual(userIdentification.UID!, platform.PlatformUserIdentification.UID, transfer.UserIdentification.UID);
        AssertAllAreEqual(userIdentification.USN!, platform.PlatformUserIdentification.USN, transfer.UserIdentification.USN);
        AssertAllAreEqual(userIdentification.PTK!, platform.PlatformUserIdentification.PTK, transfer.UserIdentification.PTK);
    }

    protected static void AssertCommonTransfer(ReadResults[] results, UserIdentification userIdentification, IPlatform platform, int offset)
    {
        Assert.AreEqual(userIdentification.LID!, platform.PlatformUserIdentification.LID);
        Assert.AreEqual(userIdentification.UID!, platform.PlatformUserIdentification.UID);
        Assert.AreEqual(userIdentification.USN!, platform.PlatformUserIdentification.USN);
        Assert.AreEqual(userIdentification.PTK!, platform.PlatformUserIdentification.PTK);

        for (var i = 0; i <= 2; i += 2)
            for (var j = 0; j < results.Length; j++)
            {
                var collectionIndex = results[j].CollectionIndex + offset + i;
                var container = platform.GetSaveContainer(collectionIndex);
                Guard.IsNotNull(container);
                var expected = results[j];

                var priject = new PrivateObject(container);

                Assert.AreEqual(userIdentification.LID!, GetUserIdentification(container).LID);
                Assert.AreEqual(userIdentification.UID!, GetUserIdentification(container).UID);
                Assert.AreEqual(userIdentification.USN!, GetUserIdentification(container).USN);
                Assert.AreEqual(userIdentification.PTK!, GetUserIdentification(container).PTK);

                Assert.AreEqual(collectionIndex, container.CollectionIndex);

                Assert.AreEqual(expected.Exists, container.Exists);
                Assert.AreEqual(expected.IsCompatible, container.IsCompatible);
                Assert.AreEqual(expected.IsOld, container.IsOld);
                Assert.AreEqual(expected.HasBase, container.HasBase);
                Assert.AreEqual(expected.HasFreighter, container.HasFreighter);
                Assert.AreEqual(expected.HasSettlement, container.HasSettlement);
                Assert.AreEqual(expected.HasActiveExpedition, container.HasActiveExpedition);
                Assert.AreEqual(expected.CanSwitchContext, container.CanSwitchContext);
                Assert.AreEqual(expected.ActiveContext, container.ActiveContext);
                Assert.AreEqual(expected.GameMode, priject.GetFieldOrProperty(nameof(ReadResults.GameMode)).ToString());
                Assert.AreEqual(expected.Difficulty, container.Difficulty);
                Assert.AreEqual(expected.Season, container.Season);
                Assert.AreEqual(expected.BaseVersion, (int)(priject.GetFieldOrProperty(nameof(ReadResults.BaseVersion))));
                Assert.AreEqual(expected.SaveVersion, (int)(priject.GetFieldOrProperty(nameof(ReadResults.SaveVersion))));
                Assert.AreEqual(expected.SaveName, container.SaveName);
                Assert.AreEqual(expected.SaveSummary, container.SaveSummary);
                Assert.AreEqual(expected.TotalPlayTime, container.TotalPlayTime);
                Assert.AreEqual(0, container.UnknownKeys.Count, $"{container.Identifier}.UnknownKeys: {string.Join(" // ", container.UnknownKeys)}");
            }
    }

    protected static void AssertCommonWriteValues(int expectedValue, long expectedUtcTicks, (int Value, long UtcTicks) values)
    {
        Assert.AreEqual(expectedValue, values.Value);
        Assert.AreEqual(expectedUtcTicks, values.UtcTicks, DELTA_TIMESPAN_TICKS);
    }

    #endregion

    #region Convert

    protected static uint[] ToUInt32(byte[] source)
    {
        var result = new uint[source.Length / sizeof(uint)];
        Buffer.BlockCopy(source, 0, result, 0, source.Length);
        return result;
    }

    #endregion

    #region Copy

    /// <see href="https://learn.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories#example"/>
    private static void Copy(string source, string destination)
    {
        var sourceDirectory = new DirectoryInfo(source);

        // Cache directories and files before we start copying.
        var cacheDirectories = sourceDirectory.EnumerateDirectories();
        var cacheFiles = sourceDirectory.EnumerateFiles();

        // Create the destination directory.
        Directory.CreateDirectory(destination);

        // Get the files in the source directory and copy to the destination directory.
        foreach (var file in cacheFiles)
            file.CopyTo(Path.Combine(destination, file.Name));

        // Recursively call this method to copying subdirectories.
        foreach (var directory in cacheDirectories)
            Copy(directory.FullName, Path.Combine(destination, directory.Name));
    }

    #endregion

    #region Getter

    protected static string GetCombinedPath(params string[] paths)
    {
        return Path.Combine([DIRECTORY_TESTSUITE_ARCHIVE, .. paths]);
    }

    protected static IEnumerable<IContainer> GetExistingContainers(IPlatform platform)
    {
        return platform.GetSaveContainers().Where(i => i.Exists);
    }

    protected static FileOperationResults GetFileOperationResults(IContainer container)
    {
        var priject = new PrivateObject(container);
        return new(priject.GetFieldOrProperty(nameof(FileOperationResults.GameMode)).ToString()!, container.Difficulty, container.Season, (int)(priject.GetFieldOrProperty(nameof(FileOperationResults.BaseVersion))), container.GameVersion, container.TotalPlayTime);
    }

    protected static string GetGuid(IEnumerable<byte> source)
    {
        return new Guid(source.ToArray()).ToString("N").ToUpper();
    }

    protected static IEnumerable<IContainer> GetLoadedContainers(IPlatform platform)
    {
        return platform.GetSaveContainers().Where(i => i.IsLoaded);
    }

    protected static string GetString(IEnumerable<byte> source)
    {
        return Encoding.UTF8.GetString(source.ToArray());
    }

    protected static string GetUnicode(IEnumerable<byte> source)
    {
        return Encoding.Unicode.GetString(source.ToArray());
    }

    protected static IEnumerable<IContainer> GetWatcherChangeContainers(IPlatform platform)
    {
        return platform.GetSaveContainers().Where(i => i.HasWatcherChange);
    }

    #endregion

    #region Test

    protected static void TestCommonAnalyzeFile(string path, ReadResults results, PlatformEnum platformResult)
    {
        // Act
        var container = io.Global.Analyze.AnalyzeFile(path)!;
        var priject = new PrivateObject(container);

        // Assert
        Assert.AreEqual(platformResult.ToString(), priject.GetFieldOrProperty("Platform").ToString());

        Assert.AreEqual(results.CollectionIndex, container.CollectionIndex);
        Assert.AreEqual(results.Identifier, container.Identifier);
        Assert.AreEqual(results.Exists, container.Exists);
        Assert.AreEqual(results.IsCompatible, container.IsCompatible);
        Assert.AreEqual(results.IsOld, container.IsOld);
        Assert.AreEqual(results.HasBase, container.HasBase);
        Assert.AreEqual(results.HasFreighter, container.HasFreighter);
        Assert.AreEqual(results.HasSettlement, container.HasSettlement);
        Assert.AreEqual(results.HasActiveExpedition, container.HasActiveExpedition);
        Assert.AreEqual(results.CanSwitchContext, container.CanSwitchContext);
        Assert.AreEqual(results.ActiveContext, container.ActiveContext);
        Assert.AreEqual(results.GameMode, priject.GetFieldOrProperty(nameof(ReadResults.GameMode)).ToString());
        Assert.AreEqual(results.Difficulty, container.Difficulty);
        Assert.AreEqual(results.Season, container.Season);
        Assert.AreEqual(results.BaseVersion, (int)(priject.GetFieldOrProperty(nameof(ReadResults.BaseVersion))));
        Assert.AreEqual(results.SaveVersion, (int)(priject.GetFieldOrProperty(nameof(ReadResults.SaveVersion))));
        Assert.AreEqual(results.SaveName, container.SaveName);
        Assert.AreEqual(results.SaveSummary, container.SaveSummary);
        Assert.AreEqual(results.TotalPlayTime, container.TotalPlayTime);
        Assert.AreEqual(0, container.UnknownKeys.Count, $"{container.Identifier}.UnknownKeys: {string.Join(" // ", container.UnknownKeys)}");
    }

    protected static void TestCommonAnalyzePath(string path, PlatformEnum platformResult, PlatformEnum? platformPreferred)
    {
        // Arrange
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };

        // Act
        var collection = new PlatformCollection();
        var countOrigin = collection.Count();

        var platform = collection.AnalyzePath(path, settings, platformPreferred).FirstOrDefault();
        var countResult = collection.Count();

        // Assert
        Assert.AreEqual(countOrigin + 1, countResult);
        Assert.AreEqual(platformResult, platform?.PlatformEnum);
    }

    protected static void TestCommonBuildCollection(string path, PlatformEnum platformResult, PlatformCollectionSettings collectionSettings)
    {
        // Arrange
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };

        // Act
        var collection = new PlatformCollection(path, settings, collectionSettings);
        var platform = collection.Get(path);

        // Assert
        Assert.AreEqual(platformResult, platform.PlatformEnum);
    }

    protected static void TestCommonFileOperationCopy<TPlatform>(string path, int[] overwrite, int[] create, int[] delete) where TPlatform : IPlatform
    {
        // Arrange
        var containers = new Dictionary<int, IContainer>();
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };

        // Act
        var platform = (TPlatform?)(Activator.CreateInstance(typeof(TPlatform), path, settings))!;

        foreach (var i in overwrite.Concat(create).Concat(delete))
            if (!containers.ContainsKey(i))
            {
                var container = platform.GetSaveContainer(i);
                Guard.IsNotNull(container);
                containers.Add(i, container);
            }

        platform.Copy(containers[overwrite[0]], containers[overwrite[1]]); // overwrite
        platform.Copy(containers[create[0]], containers[create[1]]); // create
        platform.Copy(containers[delete[0]], containers[delete[1]]); // delete

        // Assert
        Assert.IsTrue(containers[overwrite[0]].Exists);
        Assert.IsTrue(containers[overwrite[1]].Exists);
        AssertCommonFileOperation(GetFileOperationResults(containers[overwrite[0]]), GetFileOperationResults(containers[overwrite[1]]));

        Assert.IsTrue(containers[create[0]].Exists);
        Assert.IsTrue(containers[create[1]].Exists);
        AssertCommonFileOperation(GetFileOperationResults(containers[create[0]]), GetFileOperationResults(containers[create[1]]));

        Assert.IsFalse(containers[delete[0]].Exists);
        Assert.IsFalse(containers[delete[1]].Exists);
    }

    protected static void TestCommonFileOperationDelete<TPlatform>(string path, int[] delete) where TPlatform : IPlatform
    {
        // Arrange
        var containers = new List<IContainer>();
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };
        var tag = typeof(TPlatform) == typeof(PlatformMicrosoft) ? libNOM.io.Global.Constants.INCOMPATIBILITY_004 : libNOM.io.Global.Constants.INCOMPATIBILITY_006;

        // Act
        var platform = (TPlatform?)(Activator.CreateInstance(typeof(TPlatform), path, settings))!;

        foreach (var i in delete)
        {
            var container = platform.GetSaveContainer(i);
            Guard.IsNotNull(container);
            containers.Add(container);

            platform.Delete(container);
        }

        // Assert
        foreach (var container in containers)
        {
            Assert.IsFalse(container.Exists);
            Assert.AreEqual(tag, container.IncompatibilityTag);
        }
    }

    protected static void TestCommonFileOperationMove<TPlatform>(string path, int[] copy, int[] overwrite, int[] delete, int[] create) where TPlatform : IPlatform
    {
        // Arrange
        var containers = new Dictionary<int, IContainer>();
        var results = new Dictionary<int, FileOperationResults>();
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };
        var tag = typeof(TPlatform) == typeof(PlatformMicrosoft) ? libNOM.io.Global.Constants.INCOMPATIBILITY_004 : libNOM.io.Global.Constants.INCOMPATIBILITY_006;

        // Act
        var platform = (TPlatform?)(Activator.CreateInstance(typeof(TPlatform), path, settings))!;

        foreach (var i in copy.Concat(overwrite).Concat(create).Concat(delete))
            if (!containers.ContainsKey(i))
            {
                var container = platform.GetSaveContainer(i);
                Guard.IsNotNull(container);
                containers.Add(i, container);
            }

        if (copy.Length != 0)
            platform.Copy(containers[copy[0]], containers[copy[1]]);

        // After copy to get proper data.
        foreach (var (containerIndex, container) in containers.Where(i => i.Key == overwrite[0] || i.Key == create[0]))
            results.Add(containerIndex, GetFileOperationResults(container));

        platform.Move(containers[overwrite[0]], containers[overwrite[1]]); // overwrite
        platform.Move(containers[delete[0]], containers[delete[1]]); // delete
        platform.Move(containers[create[0]], containers[create[1]]); // create

        // Assert
        Assert.IsFalse(containers[overwrite[0]].Exists);
        Assert.IsTrue(containers[overwrite[1]].Exists);
        AssertCommonFileOperation(results[overwrite[0]], GetFileOperationResults(containers[overwrite[1]]));

        Assert.IsFalse(containers[delete[0]].Exists);
        Assert.IsFalse(containers[delete[1]].Exists);
        Assert.AreEqual(tag, containers[delete[0]].IncompatibilityTag);
        Assert.AreEqual(tag, containers[delete[1]].IncompatibilityTag);

        Assert.IsFalse(containers[create[0]].Exists);
        Assert.IsTrue(containers[create[1]].Exists);
        AssertCommonFileOperation(results[create[0]], GetFileOperationResults(containers[create[1]]));
    }

    protected static void TestCommonFileOperationSwap<TPlatform>(string path, ReadResults[] results, int[] swap) where TPlatform : IPlatform
    {
        // Arrange
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
        };

        // Act
        var platform = (TPlatform?)(Activator.CreateInstance(typeof(TPlatform), path, settings))!;

        platform.Swap(platform.GetSaveContainer(swap[0])!, platform.GetSaveContainer(swap[1])!);

        // Assert
        foreach (var expected in results)
        {
            var container = platform.GetSaveContainer(expected.CollectionIndex)!;
            var priject = new PrivateObject(container);

            Assert.AreEqual(expected.CollectionIndex, container.CollectionIndex);
            Assert.AreEqual(expected.Identifier, container.Identifier);
            Assert.AreEqual(expected.Exists, container.Exists);
            Assert.AreEqual(expected.IsCompatible, container.IsCompatible);
            Assert.AreEqual(expected.IsOld, container.IsOld);
            Assert.AreEqual(expected.HasBase, container.HasBase);
            Assert.AreEqual(expected.HasFreighter, container.HasFreighter);
            Assert.AreEqual(expected.HasSettlement, container.HasSettlement);
            Assert.AreEqual(expected.HasActiveExpedition, container.HasActiveExpedition);
            Assert.AreEqual(expected.CanSwitchContext, container.CanSwitchContext);
            Assert.AreEqual(expected.ActiveContext, container.ActiveContext);
            Assert.AreEqual(expected.GameMode, priject.GetFieldOrProperty(nameof(ReadResults.GameMode)).ToString());
            Assert.AreEqual(expected.Difficulty, container.Difficulty);
            Assert.AreEqual(expected.Season, container.Season);
            Assert.AreEqual(expected.BaseVersion, (int)(priject.GetFieldOrProperty(nameof(ReadResults.BaseVersion))));
            Assert.AreEqual(expected.SaveVersion, (int)(priject.GetFieldOrProperty(nameof(ReadResults.SaveVersion))));
            Assert.AreEqual(expected.SaveName, container.SaveName);
            Assert.AreEqual(expected.SaveSummary, container.SaveSummary);
            Assert.AreEqual(expected.TotalPlayTime, container.TotalPlayTime);
            Assert.AreEqual(0, container.UnknownKeys.Count, $"{container.Identifier}.UnknownKeys: {string.Join(" // ", container.UnknownKeys)}");
        }
    }

    protected static void TestCommonFileOperationTransfer<TPlatform, TSource>(string pathSource, string path, UserIdentification userIdentificationSource, UserIdentification userIdentification, int source, int userDecisionsSource, int[] transfer, int existingContainersCount, ReadResults[] results) where TPlatform : IPlatform where TSource : IPlatform
    {
        // Arrange
        var offset = (transfer[0] - source) * 2;
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
            UseExternalSourcesForUserIdentification = false,
        };

        // Act
        var platformSource = (TSource?)(Activator.CreateInstance(typeof(TSource), pathSource, settings))!;
        var transferSource = platformSource.GetSourceTransferData(source);

        var platform = (TPlatform?)(Activator.CreateInstance(typeof(TPlatform), path, settings))!;

        foreach (var i in transfer)
            platform.Transfer(transferSource, i);

        // Assert
        Assert.AreEqual(userDecisionsSource, transferSource.TransferBaseUserDecision.Count);
        Assert.AreEqual(existingContainersCount, GetExistingContainers(platform).Count());

        AssertCommonSourceTransferData(userIdentificationSource, platformSource, transferSource);
        AssertCommonTransfer(results, userIdentification, platform, offset);
    }

    protected static void TestCommonFileSystemWatcher<TPlatform>(string path, string pathWatching, int containerIndex) where TPlatform : IPlatform
    {
        // Arrange
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            UseMapping = true,
            Watcher = true,
        };

        libNOM.io.Global.Constants.JSONPATH_EXTENSION[UNITS_JSONPATH_KEY] = UNITS_JSONPATH;

        // Act
        var bytes = File.ReadAllBytes(pathWatching);

        var platform = (TPlatform?)(Activator.CreateInstance(typeof(TPlatform), path, settings))!;
        var container = platform.GetSaveContainer(containerIndex);
        Guard.IsNotNull(container);

        platform.Load(container);

        // Container is synced and therefore WatcherChange should resolve itself.
        File.WriteAllBytes(pathWatching, bytes);
        Thread.Sleep(FILESYSTEMWATCHER_SLEEP);
        var watchers1 = GetWatcherChangeContainers(platform);
        var count1 = watchers1.Count();
        var synced1 = container.IsSynced;

        // Container not synced after setting a value.
        container.SetJsonValue(UNITS_NEW_AMOUNT, UNITS_JSONPATH_KEY);
        var synced2 = container.IsSynced;

        // Container is not synced and WatcherChange needs to resolved manually.
        File.WriteAllBytes(pathWatching, bytes);
        Thread.Sleep(FILESYSTEMWATCHER_SLEEP);
        var watchers2 = GetWatcherChangeContainers(platform);
        var count2 = watchers2.Count();
        var synced3 = container.IsSynced;

        // Container still not synced due to negative decision.
        var watcherContainer2 = watchers2.FirstOrDefault();
        Guard.IsNotNull(watcherContainer2);
        platform.OnWatcherDecision(watcherContainer2, false);
        var synced4 = container.IsSynced;

        // Container still not synced and WatcherChange needs to resolved manually.
        File.WriteAllBytes(pathWatching, bytes);
        Thread.Sleep(FILESYSTEMWATCHER_SLEEP);
        var watchers3 = GetWatcherChangeContainers(platform);
        var count3 = watchers3.Count();
        var synced5 = container.IsSynced;

        // Container will be overwritten and is synced again due to positive decision.
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

    protected static void TestCommonGameModeCustom(string path, LoadingStrategyEnum loadingStrategy, int[] containerIndices)
    {
        // Arrange
        var settings = new PlatformSettings
        {
            LoadingStrategy = loadingStrategy,
        };

        // Act
        var platform = new PlatformSteam(path, settings);

        var normal = platform.GetSaveContainer(containerIndices[0]);
        var creative = platform.GetSaveContainer(containerIndices[1]);
        var survival = platform.GetSaveContainer(containerIndices[2]);
        var relaxed = platform.GetSaveContainer(containerIndices[3]);
        var permadeath = platform.GetSaveContainer(containerIndices[4]);
        var seasonal = platform.GetSaveContainer(containerIndices[5]);

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

    protected static void TestCommonGameModeVanilla(string path, LoadingStrategyEnum loadingStrategy, int[] containerIndices)
    {
        // Arrange
        var settings = new PlatformSettings
        {
            LoadingStrategy = loadingStrategy,
        };

        // Act
        var platform = new PlatformSteam(path, settings);

        var normal = platform.GetSaveContainer(containerIndices[0]);
        var creative = platform.GetSaveContainer(containerIndices[1]);
        var survival = platform.GetSaveContainer(containerIndices[2]);
        var permadeath = platform.GetSaveContainer(containerIndices[3]);

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

    protected static void TestCommonRead<TPlatform>(string path, ReadResults[] results, bool expectAccountData, UserIdentification userIdentification) where TPlatform : IPlatform
    {
        // Arrange
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Full,
            Trace = true,
            UseExternalSourcesForUserIdentification = false,
            UseMapping = true, // to check UnknownKeys
        };

        // Act
        var platform = (TPlatform?)(Activator.CreateInstance(typeof(TPlatform), path, settings));

        // Assert
        AssertCommonRead(results, platform, expectAccountData, userIdentification);
    }

    protected static void TestCommonWriteDefaultAccount<TPlatform>(string path, int originMusicVolume, long originUtcTicks, Func<IContainer, uint[]> DecryptMeta, Action<IContainer, uint[], uint[]> AssertCommonMeta) where TPlatform : IPlatform
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            UseMapping = true,
        };
        var writeCallback = false;

        // Act
        var platformA = (TPlatform?)(Activator.CreateInstance(typeof(TPlatform), path, settings))!;
        var containerA = platformA.GetAccountContainer();
        Guard.IsNotNull(containerA);
        var metaA = DecryptMeta(containerA);

        containerA.PropertiesChangedCallback += () =>
        {
            writeCallback = true;
        };

        platformA.Load(containerA);
        (int MusicVolume, long UtcTicks) valuesOrigin = (containerA.GetJsonValue<int>(MUSICVOLUME_JSONPATH), containerA.LastWriteTime!.Value.UtcTicks);

        containerA.SetJsonValue(MUSICVOLUME_NEW_AMOUNT, MUSICVOLUME_JSONPATH);
        platformA.Write(containerA, now);
        (int MusicVolume, long UtcTicks) valuesSet = (containerA.GetJsonValue<int>(MUSICVOLUME_JSONPATH), containerA.LastWriteTime!.Value.UtcTicks);

        var platformB = (TPlatform?)(Activator.CreateInstance(typeof(TPlatform), path, settings))!;
        var containerB = platformB.GetAccountContainer();
        Guard.IsNotNull(containerB);
        var metaB = DecryptMeta(containerB);

        platformB.Load(containerB);
        (int MusicVolume, long UtcTicks) valuesReload = (containerB.GetJsonValue<int>(MUSICVOLUME_JSONPATH), containerB.LastWriteTime!.Value.UtcTicks);

        // Assert
        Assert.IsTrue(writeCallback);

        AssertCommonWriteValues(originMusicVolume, originUtcTicks, valuesOrigin);
        AssertCommonWriteValues(MUSICVOLUME_NEW_AMOUNT, now.UtcTicks, valuesSet);
        AssertCommonWriteValues(MUSICVOLUME_NEW_AMOUNT, now.UtcTicks, valuesReload);

        AssertCommonMeta(containerA, metaA, metaB);
    }

    protected static void TestCommonWriteDefaultSave<TPlatform>(string path, int containerIndex, int originUnits, long originUtcTicks, WriteResults results, Func<IContainer, uint[]> DecryptMeta, Action<IContainer, uint[], uint[]> AssertCommonMeta, Action<WriteResults, IContainer, IContainer, uint[], uint[]> AssertSpecificMeta) where TPlatform : IPlatform
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            UseMapping = true,
        };
        var writeCallback = false;

        libNOM.io.Global.Constants.JSONPATH_EXTENSION[UNITS_JSONPATH_KEY] = UNITS_JSONPATH;

        // Act
        var platformA = (TPlatform?)(Activator.CreateInstance(typeof(TPlatform), path, settings))!;
        var containerA = platformA.GetSaveContainer(containerIndex);
        Guard.IsNotNull(containerA);
        var metaA = DecryptMeta(containerA);

        containerA.PropertiesChangedCallback += () =>
        {
            writeCallback = true;
        };

        platformA.Load(containerA);
        (int Units, long UtcTicks) valuesOrigin = (containerA.GetJsonValue<int>(UNITS_JSONPATH_KEY), containerA.LastWriteTime!.Value.UtcTicks);

        containerA.SetJsonValue(UNITS_NEW_AMOUNT, UNITS_JSONPATH_KEY);
        platformA.Write(containerA, now);
        (int Units, long UtcTicks) valuesSet = (containerA.GetJsonValue<int>(UNITS_JSONPATH_KEY), containerA.LastWriteTime!.Value.UtcTicks);

        var platformB = (TPlatform?)(Activator.CreateInstance(typeof(TPlatform), path, settings))!;
        var containerB = platformB.GetSaveContainer(containerIndex);
        Guard.IsNotNull(containerB);
        var metaB = DecryptMeta(containerB);

        platformB.Load(containerB);
        (int Units, long UtcTicks) valuesReload = (containerB.GetJsonValue<int>(UNITS_JSONPATH_KEY), containerB.LastWriteTime!.Value.UtcTicks);

        // Assert
        Assert.IsTrue(writeCallback);

        AssertCommonWriteValues(originUnits, originUtcTicks, valuesOrigin);
        AssertCommonWriteValues(UNITS_NEW_AMOUNT, now.UtcTicks, valuesSet);
        AssertCommonWriteValues(UNITS_NEW_AMOUNT, now.UtcTicks, valuesReload);

        AssertCommonMeta(containerA, metaA, metaB);
        AssertSpecificMeta(results, containerA, containerB, metaA, metaB);
    }

    protected static void TestCommonWriteSetLastWriteTimeFalse<TPlatform>(string path, int containerIndex, int originUnits, long originUtcTicks) where TPlatform : IPlatform
    {
        // Arrange
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            SetLastWriteTime = false,
            UseMapping = true,
        };
        var writeCallback = false;

        libNOM.io.Global.Constants.JSONPATH_EXTENSION[UNITS_JSONPATH_KEY] = UNITS_JSONPATH;

        // Act
        var platformA = (TPlatform?)(Activator.CreateInstance(typeof(TPlatform), path, settings))!;
        var containerA = platformA.GetSaveContainer(containerIndex);
        Guard.IsNotNull(containerA);

        containerA.PropertiesChangedCallback += () =>
        {
            writeCallback = true;
        };

        platformA.Load(containerA);
        (int Units, long UtcTicks) valuesOrigin = (containerA.GetJsonValue<int>(UNITS_JSONPATH_KEY), containerA.LastWriteTime!.Value.UtcTicks);

        containerA.SetJsonValue(UNITS_NEW_AMOUNT, UNITS_JSONPATH_KEY);
        platformA.Write(containerA, DateTimeOffset.UtcNow);
        (int Units, long UtcTicks) valuesSet = (containerA.GetJsonValue<int>(UNITS_JSONPATH_KEY), containerA.LastWriteTime!.Value.UtcTicks);

        var platformB = (TPlatform?)(Activator.CreateInstance(typeof(TPlatform), path, settings))!;
        var containerB = platformB.GetSaveContainer(containerIndex);
        Guard.IsNotNull(containerB);

        platformB.Load(containerB);
        (int Units, long UtcTicks) valuesReload = (containerB.GetJsonValue<int>(UNITS_JSONPATH_KEY), containerB.LastWriteTime!.Value.UtcTicks);

        // Assert
        Assert.IsTrue(writeCallback);

        AssertCommonWriteValues(originUnits, originUtcTicks, valuesOrigin);
        AssertCommonWriteValues(UNITS_NEW_AMOUNT, originUtcTicks, valuesSet);
        AssertCommonWriteValues(UNITS_NEW_AMOUNT, originUtcTicks, valuesReload);
    }

    protected static void TestCommonWriteWriteAlwaysFalse<TPlatform>(string path, int containerIndex) where TPlatform : IPlatform
    {
        // Arrange
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            SetLastWriteTime = false, // no interfering
            WriteAlways = false,
        };
        var writeCallback = false;

        // Act
        var platformA = (TPlatform?)(Activator.CreateInstance(typeof(TPlatform), path, settings))!;
        var containerA = platformA.GetSaveContainer(containerIndex);
        Guard.IsNotNull(containerA);

        containerA.PropertiesChangedCallback += () =>
        {
            writeCallback = true;
        };

        platformA.Load(containerA);
        containerA.DataFile!.Refresh();
        var lengthOrigin = containerA.DataFile!.Length;

        platformA.Write(containerA);
        containerA.DataFile!.Refresh();
        var lengthSet = containerA.DataFile!.Length;

        var platformB = (TPlatform?)(Activator.CreateInstance(typeof(TPlatform), path, settings))!;
        var containerB = platformB.GetSaveContainer(containerIndex);
        Guard.IsNotNull(containerB);

        platformB.Load(containerB);
        containerB.DataFile!.Refresh();
        var lengthReload = containerA.DataFile!.Length;

        // Assert
        Assert.IsTrue(writeCallback);

        Assert.AreEqual(lengthOrigin, lengthSet);
        Assert.AreEqual(lengthOrigin, lengthReload); // then lengthSet and lengthReload AreEqual too
    }

    protected static void TestCommonWriteWriteAlwaysTrue<TPlatform>(string path, int containerIndex) where TPlatform : IPlatform
    {
        // Arrange
        var settings = new PlatformSettings
        {
            LoadingStrategy = LoadingStrategyEnum.Hollow,
            SetLastWriteTime = false, // no interfering
            WriteAlways = true,
        };
        var writeCallback = false;

        // Act
        var platformA = (TPlatform?)(Activator.CreateInstance(typeof(TPlatform), path, settings))!;
        var containerA = platformA.GetSaveContainer(containerIndex);
        Guard.IsNotNull(containerA);

        containerA.PropertiesChangedCallback += () =>
        {
            writeCallback = true;
        };

        platformA.Load(containerA);
        containerA.DataFile!.Refresh();
        var lengthOrigin = containerA.DataFile!.Length;

        platformA.Write(containerA);
        containerA.DataFile!.Refresh();
        var lengthSet = containerA.DataFile!.Length;

        var platformB = (TPlatform?)(Activator.CreateInstance(typeof(TPlatform), path, settings))!;
        var containerB = platformB.GetSaveContainer(containerIndex);
        Guard.IsNotNull(containerB);

        platformB.Load(containerB);
        containerB.DataFile!.Refresh();
        var lengthReload = containerA.DataFile!.Length;

        // Assert
        Assert.IsTrue(writeCallback);

        Assert.AreNotEqual(lengthOrigin, lengthSet);
        Assert.AreNotEqual(lengthOrigin, lengthReload);

        Assert.AreEqual(lengthSet, lengthReload);
    }

    #endregion

    #region UserIdentification

    protected static UserIdentification GetUserIdentification(IContainer container)
    {
        var priject = new PrivateObject(new PrivateObject(container).GetFieldOrProperty("UserIdentification"));
        return new()
        {
            LID = priject.GetFieldOrProperty("LID").ToString(),
            UID = priject.GetFieldOrProperty("UID").ToString(),
            USN = priject.GetFieldOrProperty("USN").ToString(),
            PTK = priject.GetFieldOrProperty("PTK").ToString(),
        };
    }

    protected static UserIdentification ReadUserIdentification(string path)
    {
        var userIdentification = File.ReadAllLines(Path.Combine(path, "UserIdentification.txt"));
        return new()
        {
            LID = userIdentification[0],
            PTK = userIdentification[3],
            UID = userIdentification[1],
            USN = userIdentification[2],
        };
    }

    #endregion

    [TestInitialize]
    public void ExtractArchive()
    {
        if (!Directory.Exists(DIRECTORY_TESTSUITE_ARCHIVE_TEMPLATE))
        {
            Directory.CreateDirectory(DIRECTORY_TESTSUITE_ARCHIVE_TEMPLATE);

            foreach (var file in Directory.EnumerateFiles(".", $"{DIRECTORY_TESTSUITE_ARCHIVE}*.zip"))
            {
                using var zipArchive = ZipArchive.Open(file, new()
                {
                    Password = Properties.Resources.TESTSUITE_PASSWORD,
                });
                foreach (var entry in zipArchive.Entries.Where(i => !i.IsDirectory))
                    entry.WriteToDirectory(DIRECTORY_TESTSUITE_ARCHIVE_TEMPLATE, new ExtractionOptions
                    {
                        ExtractFullPath = true,
                        Overwrite = true,
                        PreserveFileTime = true,
                    });
            }
        }
        if (!Directory.Exists(DIRECTORY_TESTSUITE_ARCHIVE))
            Copy(DIRECTORY_TESTSUITE_ARCHIVE_TEMPLATE, DIRECTORY_TESTSUITE_ARCHIVE);
    }

    [TestCleanup]
    public void DirectoryCleanup()
    {
        if (Directory.Exists(DIRECTORY_TESTSUITE_ARCHIVE))
            Directory.Delete(DIRECTORY_TESTSUITE_ARCHIVE, true);

        if (Directory.Exists("backup"))
            Directory.Delete("backup", true);

        if (Directory.Exists("download"))
            Directory.Delete("download", true);
    }
}
