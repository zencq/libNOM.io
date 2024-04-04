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
        libNOM.io.Global.Convert.ToJson(pathIn);
        var json = parentIn!.EnumerateFiles("7818252AB45E46868B43B7118290E50F.*.json").FirstOrDefault();

        // Assert
        Assert.IsTrue(json?.Exists == true);
        Assert.IsNotNull(JsonConvert.DeserializeObject(File.ReadAllText(json.FullName)) as JObject);
    }

    [TestMethod]
    public void T01_ToSave()
    {
        // Arrange
        var pathIn = GetCombinedPath("Microsoft", "wgs", "00090000025A963A_29070100B936489ABCE8B9AF3980429C", "0B64DCE0E87749C5837162D3D4EA728E", "C0CD238B9A2F46239623768051FBF493");
        var parentIn = Directory.GetParent(pathIn);

        // Act
        libNOM.io.Global.Convert.ToSaveFile(pathIn, PlatformEnum.Steam);

        var data = parentIn!.EnumerateFiles($"C0CD238B9A2F46239623768051FBF493.{PlatformEnum.Steam}.*.data").First();
        var meta = parentIn!.EnumerateFiles($"C0CD238B9A2F46239623768051FBF493.{PlatformEnum.Steam}.*.meta").First();

        // Assert
        Assert.IsTrue(data.Exists);
        Assert.IsTrue(meta.Exists);
    }
}
