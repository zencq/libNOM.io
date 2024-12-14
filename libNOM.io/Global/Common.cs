using System.Runtime.InteropServices;

using Newtonsoft.Json;

namespace libNOM.io.Global;


public static partial class Common
{
    internal static ReadOnlySpan<byte> ConvertHashedIds(ReadOnlySpan<byte> input, ReadOnlySpan<byte> source, ReadOnlySpan<byte> target)
    {
        var result = input;

        var indices = result.IndicesOf(source).ToArray();
        if (indices.Length > 0)
        {
            var value = target.AsSpan();

            for (int i = 0; i < indices.Length; i++)
            {
                var index = indices[i] + ((target.Length - source.Length) * i);

                var before = result[..index];
                var after = result.Slice(index + source.Length, result.Length - before.Length - source.Length);

                result = before.Concat(value).Concat(after);
            }
        }

        return result;
    }

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

    public static Span<T> DeepCopy<T>(ReadOnlySpan<T> original) => original.AsSpan(); // CopyTo a new one Span<T>

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
