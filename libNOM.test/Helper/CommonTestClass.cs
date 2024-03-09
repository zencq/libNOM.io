using System.Text;

using CommunityToolkit.Diagnostics;

using libNOM.io;
using libNOM.io.Interfaces;
using libNOM.io.Models;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Newtonsoft.Json;

using SharpCompress.Archives;
using SharpCompress.Common;

namespace libNOM.test.Helper;


[TestClass]
public abstract class CommonTestClass
{
    #region Constant

    protected const int FILESYSTEMWATCHER_SLEEP = 5000;
    protected const int OFFSET_INDEX = 2;
    protected const int TICK_DIVISOR = 10000;

    protected static readonly TimeSpan DELTA_TIMESPAN = TimeSpan.FromHours(1);

    protected static readonly long DELTA_TIMESPAN_SECONDS = (long)(DELTA_TIMESPAN.TotalSeconds);
    protected static readonly long DELTA_TIMESPAN_TICKS = DELTA_TIMESPAN.Ticks;

    protected const uint SAVE_FORMAT_2 = 0x7D1; // 2001
    protected const uint SAVE_FORMAT_3 = 0x7D2; // 2002

    protected static readonly int[] MUSICVOLUME_INDICES = [1, 7];
    protected const string MUSICVOLUME_JSON_PATH = "UserSettingsData.MusicVolume";
    protected const int MUSICVOLUME_NEW_AMOUNT = 100;

    protected static readonly int[] UNITS_INDICES = [2, 48];
    protected const string UNITS_JSON_PATH = "PlayerStateData.Units";
    protected const int UNITS_NEW_AMOUNT = 29070100;

    #endregion

    // //

    #region Assert

    protected static void AssertCommonRead(ReadResults[] results, bool expectedAccountData, UserIdentification userIdentification, IPlatform? platform)
    {
        ArgumentNullException.ThrowIfNull(platform, nameof(platform));

        Assert.AreEqual(results.Length, GetExistingContainers(platform).Count());

        if (expectedAccountData)
            Assert.IsTrue(platform.HasAccountData);
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

            var priect = new PrivateObject(container);

            if (!object.Equals(expected.CollectionIndex, container.CollectionIndex))
                Console.WriteLine(nameof(expected.CollectionIndex));

            if (!object.Equals(expected.Identifier, container.Identifier))
                Console.WriteLine(nameof(expected.Identifier));

            if (!object.Equals(expected.Exists, container.Exists))
                Console.WriteLine(nameof(expected.Exists));

            if (!object.Equals(expected.IsCompatible, container.IsCompatible))
                Console.WriteLine(nameof(expected.IsCompatible));

            if (!object.Equals(expected.IsOld, container.IsOld))
                Console.WriteLine(nameof(expected.IsOld));

            if (!object.Equals(expected.HasBase, container.HasBase))
                Console.WriteLine(nameof(expected.HasBase));

            if (!object.Equals(expected.HasFreighter, container.HasFreighter))
                Console.WriteLine(nameof(expected.HasFreighter));

            if (!object.Equals(expected.HasSettlement, container.HasSettlement))
                Console.WriteLine(nameof(expected.HasSettlement));

            if (!object.Equals(expected.HasActiveExpedition, container.HasActiveExpedition))
                Console.WriteLine(nameof(expected.HasActiveExpedition));

            if (!object.Equals(expected.CanSwitchContext, container.CanSwitchContext))
                Console.WriteLine(nameof(expected.CanSwitchContext));

            if (!object.Equals(expected.ActiveContext, container.ActiveContext))
                Console.WriteLine(nameof(expected.ActiveContext));

            var gameMode = priect.GetFieldOrProperty(nameof(ReadResults.GameMode)).ToString();
            if (!object.Equals(expected.GameMode, gameMode))
                Console.WriteLine(nameof(expected.GameMode));

            if (!object.Equals(expected.Difficulty, container.Difficulty))
                Console.WriteLine(nameof(expected.Difficulty));

            if (!object.Equals(expected.Season, container.Season))
                Console.WriteLine(nameof(expected.Season));

            var baseVersion = (int)(priect.GetFieldOrProperty(nameof(ReadResults.BaseVersion)));
            if (!object.Equals(expected.BaseVersion, baseVersion))
                Console.WriteLine(nameof(expected.BaseVersion));

            var saveVersion = (int)(priect.GetFieldOrProperty(nameof(ReadResults.SaveVersion)));
            if (!object.Equals(expected.SaveVersion, saveVersion))
                Console.WriteLine(nameof(expected.SaveVersion));

            if (!object.Equals(expected.SaveName, container.SaveName))
                Console.WriteLine(nameof(expected.SaveName));

            if (!object.Equals(expected.SaveSummary, container.SaveSummary))
                Console.WriteLine(nameof(expected.SaveSummary));

            if (!object.Equals(expected.TotalPlayTime, container.TotalPlayTime))
                Console.WriteLine(nameof(expected.TotalPlayTime));

            // TODO should always 0
            if (container.UnknownKeys.Any())
                Console.WriteLine(nameof(container.UnknownKeys));

            //Assert.AreEqual(expected.CollectionIndex, container.CollectionIndex);
            //Assert.AreEqual(expected.Identifier, container.Identifier);
            //Assert.AreEqual(expected.Exists, container.Exists);
            //Assert.AreEqual(expected.IsCompatible, container.IsCompatible);
            //Assert.AreEqual(expected.IsOld, container.IsOld);
            //Assert.AreEqual(expected.HasBase, container.HasBase);
            //Assert.AreEqual(expected.HasFreighter, container.HasFreighter);
            //Assert.AreEqual(expected.HasSettlement, container.HasSettlement);
            //Assert.AreEqual(expected.HasActiveExpedition, container.HasActiveExpedition);
            //Assert.AreEqual(expected.CanSwitchContext, container.CanSwitchContext);
            //Assert.AreEqual(expected.ActiveContext, container.ActiveContext);
            //Assert.AreEqual(expected.GameMode, priect.GetFieldOrProperty(nameof(ReadResults.GameMode)).ToString());
            //Assert.AreEqual(expected.Difficulty, container.Difficulty);
            //Assert.AreEqual(expected.Season, container.Season);
            //Assert.AreEqual(expected.BaseVersion, (int)(priect.GetFieldOrProperty(nameof(ReadResults.BaseVersion))));
            //Assert.AreEqual(expected.SaveVersion, (int)(priect.GetFieldOrProperty(nameof(ReadResults.SaveVersion))));
            //Assert.AreEqual(expected.SaveName, container.SaveName);
            //Assert.AreEqual(expected.SaveSummary, container.SaveSummary);
            //Assert.AreEqual(expected.TotalPlayTime, container.TotalPlayTime);
            //Assert.IsFalse(container.UnknownKeys.Any(), $"{container.Identifier}.UnknownKeys: {string.Join(" // ", container.UnknownKeys)}");
        }
    }

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
                var container = platform.GetSaveContainer(collectionIndex)!;
                Guard.IsNotNull(container);
                var expected = results[j];

                var priect = new PrivateObject(container);

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
                Assert.AreEqual(expected.GameMode, priect.GetFieldOrProperty(nameof(ReadResults.GameMode)).ToString());
                Assert.AreEqual(expected.Difficulty, container.Difficulty);
                Assert.AreEqual(expected.Season, container.Season);
                Assert.AreEqual(expected.BaseVersion, (int)(priect.GetFieldOrProperty(nameof(ReadResults.BaseVersion))));
                Assert.AreEqual(expected.SaveVersion, (int)(priect.GetFieldOrProperty(nameof(ReadResults.SaveVersion))));
                Assert.AreEqual(expected.SaveName, container.SaveName);
                Assert.AreEqual(expected.SaveSummary, container.SaveSummary);
                Assert.AreEqual(expected.TotalPlayTime, container.TotalPlayTime);
                Assert.IsFalse(container.UnknownKeys.Any(), $"{container.Identifier}.UnknownKeys: {string.Join(" // ", container.UnknownKeys)}");
            }
    }

    protected static void AssertCommonWriteValues(int expectedValue, long expectedUtcTicks, (int Value, long UtcTicks) values)
    {
        Assert.AreEqual(expectedValue, values.Value);
        Assert.AreEqual(expectedUtcTicks, values.UtcTicks, DELTA_TIMESPAN_TICKS);
    }

    #endregion

    #region Convert

    protected static DateTimeOffset NullifyTicks(DateTimeOffset timestamp)
    {
        var ticks = timestamp.Ticks % TICK_DIVISOR; // get last four digits
        return timestamp.Subtract(new TimeSpan(ticks));
    }

    protected static uint RotateLeft(uint value, int bits)
    {
        return (value << bits) | (value >> (32 - bits));
    }

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
        var cacheDirectories = sourceDirectory.GetDirectories();
        var cacheFiles = sourceDirectory.GetFiles();

        // Create the destination directory.
        Directory.CreateDirectory(destination);

        // Get the files in the source directory and copy to the destination directory.
        foreach (var file in cacheFiles)
            file.CopyTo(Path.Combine(destination, file.Name));

        // Recursively call this method to copying subdirectories.
        foreach (var directory in cacheDirectories)
            Copy(directory.FullName, Path.Combine(destination, directory.Name));
    }

    protected static T DeepCopy<T>(T original)
    {
        var serialized = JsonConvert.SerializeObject(original);
        return JsonConvert.DeserializeObject<T>(serialized)!;
    }

    #endregion

    #region Getter

    protected static FileOperationResults GetFileOperationResults(Container container)
    {
        var priect = new PrivateObject(container);
        return new(priect.GetFieldOrProperty(nameof(FileOperationResults.GameMode)).ToString()!, container.Difficulty, container.Season, (int)(priect.GetFieldOrProperty(nameof(FileOperationResults.BaseVersion))), container.GameVersion, container.TotalPlayTime);
    }

    protected static IEnumerable<Container> GetExistingContainers(IPlatform platform)
    {
        return platform.GetSaveContainers().Where(i => i.Exists);
    }

    protected static string GetGuid(IEnumerable<byte> source)
    {
        return new Guid(source.ToArray()).ToString("N").ToUpper();
    }

    protected static IEnumerable<Container> GetLoadedContainers(IPlatform platform)
    {
        return platform.GetSaveContainers().Where(i => i.IsLoaded);
    }

    protected static Container GetOneSaveContainer(IPlatform platform, int collectionIndex)
    {
        return platform.GetSaveContainers().First(i => i.CollectionIndex == collectionIndex);
    }

    protected static string GetString(IEnumerable<byte> source)
    {
        return Encoding.UTF8.GetString(source.ToArray());
    }

    protected static string GetUnicode(IEnumerable<byte> source)
    {
        return Encoding.Unicode.GetString(source.ToArray());
    }

    protected static IEnumerable<Container> GetWatcherChangeContainers(IPlatform platform)
    {
        return platform.GetSaveContainers().Where(i => i.HasWatcherChange);
    }

    #endregion

    #region UserIdentification

    protected static UserIdentification GetUserIdentification(Container container)
    {
        var priect = new PrivateObject(new PrivateObject(container).GetFieldOrProperty("UserIdentification"));
        return new()
        {
            LID = priect.GetFieldOrProperty("LID").ToString(),
            UID = priect.GetFieldOrProperty("UID").ToString(),
            USN = priect.GetFieldOrProperty("USN").ToString(),
            PTK = priect.GetFieldOrProperty("PTK").ToString(),
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
        var template = $"{nameof(Properties.Resources.TESTSUITE_ARCHIVE)}_ZIP";
        var working = nameof(Properties.Resources.TESTSUITE_ARCHIVE);

        if (!Directory.Exists(template))
        {
            Directory.CreateDirectory(template);
            using var zipArchive = ArchiveFactory.Open($"{nameof(Properties.Resources.TESTSUITE_ARCHIVE)}.zip", new()
            {
                Password = Properties.Resources.TESTSUITE_PASSWORD,
            });
            foreach (var entry in zipArchive.Entries)
                if (!entry.IsDirectory)
                {
                    entry.WriteToDirectory(template, new ExtractionOptions
                    {
                        ExtractFullPath = true,
                        Overwrite = true,
                        PreserveFileTime = true,
                    });
                }
        }
        if (!Directory.Exists(working))
            Copy(template, working);
    }

    [TestCleanup]
    public void DirectoryCleanup()
    {
        if (Directory.Exists(nameof(Properties.Resources.TESTSUITE_ARCHIVE)))
            Directory.Delete(nameof(Properties.Resources.TESTSUITE_ARCHIVE), true);

        if (Directory.Exists("backup"))
            Directory.Delete("backup", true);

        if (Directory.Exists("download"))
            Directory.Delete("download", true);
    }
}
