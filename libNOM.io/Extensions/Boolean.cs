namespace libNOM.io.Extensions;


internal static class BooleanExtensions
{
    /// <summary>
    /// Whether this has no proper value in terms of <see cref="Container"/> information.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="force"></param>
    /// <returns></returns>
    internal static bool IsUpdateNecessary(this bool self, bool force) => force || !self; // false can actually a proper value but is also the default, so we do not know what it is
}
