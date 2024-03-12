using System.Runtime.InteropServices;

using Newtonsoft.Json;

namespace libNOM.io.Globals;


internal static partial class Common
{
    // No real DeepCopy but good enough to swap and this case is only used there.
    internal static Container DeepCopy(Container original)
    {
        var copy = new Container(-1, null!, original.Extra)
        {
            Exists = true, // fake it to get data
            LastWriteTime = original.LastWriteTime,
            SaveVersion = original.SaveVersion,
        };
        copy.SetJsonObject(original.GetJsonObject());
        return copy;
    }

    internal static T DeepCopy<T>(T original)
    {
        var serialized = JsonConvert.SerializeObject(original);
        return JsonConvert.DeserializeObject<T>(serialized)!;
    }

    internal static bool IsWindowsOrLinux() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    internal static bool IsMac() => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
}