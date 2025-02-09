namespace libNOM.io.Extensions;


internal static class TypeExtensions
{
    /// <summary>
    /// Checks whether this <see cref="Type"> is <see cref="Nullable"> or not.
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    internal static bool IsNullable(this Type self)
    {
        return !self.IsGenericTypeDefinition && self.IsGenericType && self.GetGenericTypeDefinition() == typeof(Nullable<>);
    }
}
