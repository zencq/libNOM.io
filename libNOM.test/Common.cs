using System.Text;

using libNOM.io;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SharpCompress.Archives;
using SharpCompress.Common;

namespace libNOM.test;


[TestClass]
public class CommonTestInitializeCleanup
{
    #region Constant

    protected const uint SAVE_FORMAT_2 = 0x7D1; // 2001
    protected const uint SAVE_FORMAT_3 = 0x7D2; // 2002

    protected const int FILESYSTEMWATCHER_SLEEP = 5000;

    protected const int OFFSET_INDEX = 2;

    protected static readonly int[] MUSICVOLUME_INDICES = [1, 7];
    protected const string MUSICVOLUME_JSON_PATH = "UserSettingsData.MusicVolume";
    protected const int MUSICVOLUME_NEW_AMOUNT = 100;
    protected const long TICK_DIVISOR = 10000;
    protected static readonly int[] UNITS_INDICES = [2, 48];
    protected const string UNITS_JSON_PATH = "PlayerStateData.Units";
    protected const int UNITS_NEW_AMOUNT = 29070100;

    #endregion

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

    protected static void AssertAllAreEqual(string expected, params string[] actual)
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

    #endregion

    #region Helper

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

    protected static T GetPrivateFieldOrProperty<T>(object obj, string name)
    {
        return (T)(new PrivateObject(obj).GetFieldOrProperty(name));
    }

    // alias for frequently used

    protected static string GetGuid(IEnumerable<byte> source)
    {
        return new Guid(source.ToArray()).ToString("N").ToUpper();
    }

    protected static IEnumerable<Container> GetExistingContainers(Platform platform)
    {
        return platform.GetSaveContainers().Where(i => i.Exists);
    }

    protected static IEnumerable<Container> GetLoadedContainers(Platform platform)
    {
        return platform.GetSaveContainers().Where(i => i.IsLoaded);
    }

    protected static Container GetOneSaveContainer(Platform platform, int collectionIndex)
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

    protected static IEnumerable<Container> GetWatcherChangeContainers(Platform platform)
    {
        return platform.GetSaveContainers().Where(i => i.HasWatcherChange);
    }

    internal static DateTimeOffset NullifyTicks(DateTimeOffset timestamp)
    {
        var ticks = timestamp.Ticks % TICK_DIVISOR; // get last four digits
        return timestamp.Subtract(new TimeSpan(ticks));
    }

    protected static string[] ReadUserIdentification(string path)
    {
        return File.ReadAllLines(Path.Combine(path, "UserIdentification.txt"));
    }

    protected static uint[] ToUInt32(byte[] source)
    {
        var result = new uint[source.Length / sizeof(uint)];
        Buffer.BlockCopy(source, 0, result, 0, source.Length);
        return result;
    }

    #endregion

    [TestInitialize]
    public void ExtractArchive()
    {
        var template = $"{nameof(Properties.Resources.TESTSUITE_ARCHIVE)}_ZIP";
        var working = nameof(Properties.Resources.TESTSUITE_ARCHIVE);

        if (!Directory.Exists(template))
        {
            //using SharpCompress.Archives;
            //using SharpCompress.Common;
            Directory.CreateDirectory(template);
            using var zipArchive = ArchiveFactory.Open($"{nameof(Properties.Resources.TESTSUITE_ARCHIVE)}.zip", new()
            {
                Password = Properties.Resources.TESTSUITE_PASSWORD,
            });
            foreach (var entry in zipArchive.Entries)
            {
                if (entry.IsDirectory)
                    continue;

                entry.WriteToDirectory(template, new ExtractionOptions
                {
                    ExtractFullPath = true,
                    Overwrite = true,
                });
            }

            //using ICSharpCode.SharpZipLib.Zip;
            //using var zipArchive = new ZipFile($"{nameof(Properties.Resources.TESTSUITE_ARCHIVE)}.zip")
            //{
            //    Password = Properties.Resources.TESTSUITE_PASSWORD,
            //};
            //foreach (ZipEntry entry in zipArchive)
            //    if (entry.IsFile)
            //    {
            //        string outputFile = Path.Combine(template, entry.Name);

            //        Directory.CreateDirectory(Path.GetDirectoryName(outputFile)!);

            //        using var input = zipArchive.GetInputStream(entry);
            //        using var output = File.Create(outputFile);
            //        var buffer = new byte[4096];
            //        int bytesRead;
            //        while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
            //            output.Write(buffer, 0, bytesRead);
            //    }

            //using Aspose.Zip;
            //using var zipArchive = new Archive($"{nameof(Properties.Resources.TESTSUITE_ARCHIVE)}.zip", new ArchiveLoadOptions()
            //{
            //    DecryptionPassword = Properties.Resources.TESTSUITE_PASSWORD,
            //});
            //zipArchive.ExtractToDirectory(template);

            //using Ionic.Zip;
            //using var zipArchive = new ZipFile($"{nameof(Properties.Resources.TESTSUITE_ARCHIVE)}.zip")
            //{
            //    Encryption = EncryptionAlgorithm.WinZipAes256,
            //    Password = Properties.Resources.TESTSUITE_PASSWORD,
            //};
            //zipArchive.ExtractAll(template, ExtractExistingFileAction.DoNotOverwrite);
        }
        if (!Directory.Exists(working))
        {
            Copy(template, working);
        }
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
