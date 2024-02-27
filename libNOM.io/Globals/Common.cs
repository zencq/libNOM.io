using System.Runtime.InteropServices;

namespace libNOM.io.Globals;


internal static partial class Common
{
    internal static bool IsWindowsOrLinux() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    internal static bool IsMac() => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
}
