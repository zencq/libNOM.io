using Ionic.Zip;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace libNOM.test;


[TestClass]
public class CommonTestInitializeCleanup
{
    [TestInitialize]
    public void ExtractArchive()
    {
        using var zipArchive = new ZipFile($"{nameof(Properties.Resources.TESTSUITE_ARCHIVE)}.zip")
        {
            Encryption = EncryptionAlgorithm.WinZipAes256,
            Password = Properties.Resources.TESTSUITE_PASSWORD,
        };
        zipArchive.ExtractAll(nameof(Properties.Resources.TESTSUITE_ARCHIVE), ExtractExistingFileAction.DoNotOverwrite);
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
