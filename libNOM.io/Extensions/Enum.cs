namespace libNOM.io.Extensions;


internal static class EnumExtensions
{
    internal static IEnumerable<T> GetValues<T>() where T : struct, Enum
    {
#if NETSTANDARD2_0_OR_GREATER
        return (IEnumerable<T>)(Enum.GetValues(typeof(T)));
#else
        return Enum.GetValues<T>();
#endif
    }

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
