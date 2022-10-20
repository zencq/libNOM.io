namespace libNOM.io.Extensions;


internal static class GuidExtensions
{
    /// <summary>
    /// Returns a string representation of the value of this <see cref="Guid"/> instance, using the N format specifier.
    /// </summary>
    /// <param name="self"></param>
    /// <returns>The value of this <see cref="Guid"/>, represented as a series of uppercase hexadecimal digits in the N format.</returns>
    internal static string ToPath(this Guid self)
    {
        return self.ToString("N").ToUpper();
    }
}
