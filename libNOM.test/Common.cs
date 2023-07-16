using Ionic.Zip;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

namespace libNOM.test;


[TestClass]
public class CommonTestInitializeCleanup
{
    public static ReadOnlySpan<string> ReadUserIdentification(string path)
    {
        return File.ReadAllLines(Path.Combine(path, "UserIdentification.txt"));
    }

    /// <see href="https://learn.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories#example"/>
    public static void Copy(string source, string destination)
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
