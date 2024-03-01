namespace libNOM.io.Extensions;


internal static class EnumExtensions
{
    internal static T? Parse<T>(string? value) where T : struct, Enum
    {
        if (value is null)
            return null;

#if NETSTANDARD2_0
        return (T)(Enum.Parse(typeof(T), value));
#else
        return Enum.Parse<T>(value);
#endif
    }
}
