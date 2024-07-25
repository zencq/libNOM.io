using System.Runtime.InteropServices;

using Newtonsoft.Json;

namespace libNOM.io.Global;


public static partial class Common
{
    // No real DeepCopy but good enough to swap and that case is only using this.
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

    // No real DeepCopy but good enough to cache it for Microsoft.Write() and that case is only using this.
    internal static ContainerExtra DeepCopy(ContainerExtra original) => new()
    {
        MicrosoftBlobContainerExtension = original.MicrosoftBlobContainerExtension,
        MicrosoftBlobDataFile = original.MicrosoftBlobDataFile,
        MicrosoftBlobMetaFile = original.MicrosoftBlobMetaFile,
    };

    public static Span<T> DeepCopy<T>(Span<T> original) => DeepCopy(original.ToArray());

    public static T DeepCopy<T>(T original)
    {
        var serialized = JsonConvert.SerializeObject(original);
        return JsonConvert.DeserializeObject<T>(serialized)!;
    }

    internal static bool IsLinux() => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    internal static bool IsMac() => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    internal static bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    internal static bool IsWindowsOrLinux() => IsWindows() || IsLinux();
}
