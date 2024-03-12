using Microsoft.VisualStudio.TestTools.UnitTesting;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace libNOM.test;


[TestClass]
[DeploymentItem("../../../Resources/TESTSUITE_ARCHIVE_PLATFORM_MICROSOFT.zip")]
public class ConvertTest : CommonTestClass
{
    [TestMethod]
    public void T00_ToJson()
    {
        // Arrange
        var pathIn = GetCombinedPath("Microsoft", "wgs", "00090000025A963A_29070100B936489ABCE8B9AF3980429C", "4C03DDAC746248A69B66CB7B79A0B58F", "7818252AB45E46868B43B7118290E50F");
        var parentIn = Directory.GetParent(pathIn);

        // Act
        libNOM.io.Globals.Convert.ToJson(pathIn);
        var json = parentIn!.GetFiles("7818252AB45E46868B43B7118290E50F.*.json").FirstOrDefault();

        // Assert
        Assert.IsTrue(json?.Exists == true);
        Assert.IsNotNull(JsonConvert.DeserializeObject(File.ReadAllText(json.FullName)) as JObject);
    }

    [TestMethod]
    public void T01_ToSave()
    {
        // Arrange
        var pathIn = GetCombinedPath("Microsoft", "wgs", "00090000025A963A_29070100B936489ABCE8B9AF3980429C", "4C03DDAC746248A69B66CB7B79A0B58F", "7818252AB45E46868B43B7118290E50F");
        var parentIn = Directory.GetParent(pathIn);

        // Act
        libNOM.io.Globals.Convert.ToSaveFile(pathIn, PlatformEnum.Steam);

        var data = parentIn!.GetFiles($"7818252AB45E46868B43B7118290E50F.{PlatformEnum.Steam}.*.data").First();
        var meta = parentIn!.GetFiles($"7818252AB45E46868B43B7118290E50F.{PlatformEnum.Steam}.*.meta").First();

        // Assert
        Assert.IsTrue(data.Exists);
        Assert.IsTrue(meta.Exists);
    }
}
