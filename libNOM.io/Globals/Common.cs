using System.Runtime.InteropServices;

using Newtonsoft.Json;

namespace libNOM.io.Globals;


internal static partial class Common
{
    internal static T DeepCopy<T>(T original)
    {
        var serialized = JsonConvert.SerializeObject(original);
        return JsonConvert.DeserializeObject<T>(serialized)!;
    }

    internal static bool IsWindowsOrLinux() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    internal static bool IsMac() => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
}