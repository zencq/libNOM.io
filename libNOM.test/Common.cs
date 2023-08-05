using Ionic.Zip;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;

namespace libNOM.test;


[TestClass]
public class CommonTestInitializeCleanup
{
    #region Constant

    protected const uint SAVE_FORMAT_2 = 0x7D1; // 2001
    protected const uint SAVE_FORMAT_3 = 0x7D2; // 2002

    protected const int FILESYSTEMWATCHER_SLEEP = 2000;
    protected static readonly int[] MUSICVOLUME_INDICES = new[] { 1, 7 };
    protected const string MUSICVOLUME_JSON_PATH = "UserSettingsData.MusicVolume";
    protected const int MUSICVOLUME_NEW_AMOUNT = 100;
    protected static readonly int[] UNITS_INDICES = new[] { 2, 48 };
    protected const string UNITS_JSON_PATH = "PlayerStateData.Units";
    protected const int UNITS_NEW_AMOUNT = 29070100;

    #endregion

    #region Assert

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

    /// <see cref="libNOM.io.Extensions.IEnumerableExtensions.GetGuid(IEnumerable{byte})"/>
    /// <see cref="libNOM.io.Extensions.GuidExtensions.ToPath(Guid)"/>
    internal static string GetGuid(IEnumerable<byte> source)
    {
        return new Guid(source.ToArray()).ToString("N").ToUpper();
    }

    /// <see cref="libNOM.io.Extensions.IEnumerableExtensions.GetUInt32(IEnumerable{byte})"/>
    protected static uint[] GetUInt32(byte[] source)
    {
        var result = new uint[source.Length / sizeof(uint)];
        Buffer.BlockCopy(source, 0, result, 0, source.Length);

        return result;
    }
    /// <see cref="libNOM.io.Extensions.IEnumerableExtensions.GetUInt32(IEnumerable{byte})"/>
    protected static string GetUnicode(IEnumerable<byte> source)
    {
        return Encoding.Unicode.GetString(source.ToArray());
    }

    protected static ReadOnlySpan<string> ReadUserIdentification(string path)
    {
        return File.ReadAllLines(Path.Combine(path, "UserIdentification.txt"));
    }

    #endregion

    [TestInitialize]
    public void ExtractArchive()
    {
        var template = $"{nameof(Properties.Resources.TESTSUITE_ARCHIVE)}_ZIP";
        var working = nameof(Properties.Resources.TESTSUITE_ARCHIVE);

        if (!Directory.Exists(template))
        {
            using var zipArchive = new ZipFile($"{nameof(Properties.Resources.TESTSUITE_ARCHIVE)}.zip")
            {
                Encryption = EncryptionAlgorithm.WinZipAes256,
                Password = Properties.Resources.TESTSUITE_PASSWORD,
            };
            zipArchive.ExtractAll(template, ExtractExistingFileAction.DoNotOverwrite);
        }
        if (!Directory.Exists(working))
        {
            Copy(template, working);
        }
    }

    [TestCleanup]
    public void DirectoryCleanup()
    {
        Directory.Delete(nameof(Properties.Resources.TESTSUITE_ARCHIVE), true);

        if (Directory.Exists("backup"))
            Directory.Delete("backup", true);

        if (Directory.Exists("download"))
            Directory.Delete("download", true);
    }
}
