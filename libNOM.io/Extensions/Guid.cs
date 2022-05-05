namespace libNOM.io.Extensions;


internal static class GuidExtensions
{
    /// <summary>
    /// Returns a string representation of the value of this <see cref="Guid"/> instance, using the N format specifier.
    /// </summary>
    /// <param name="source"></param>
    /// <returns>The value of this <see cref="Guid"/>, represented as a series of uppercase hexadecimal digits in the N format.</returns>
    internal static string ToPath(this Guid source)
    {
        return source.ToString("N").ToUpper();
    }
}
