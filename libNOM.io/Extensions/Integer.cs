namespace libNOM.io.Extensions;


internal static class IntegerExtensions
{
    #region typeof(int)

    /// <summary>
    /// Whether this has no proper value in terms of <see cref="Container"/> information.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="force"></param>
    /// <returns></returns>
    internal static bool IsUpdateNecessary(this int self, bool force) => force || self <= 0;

    #endregion

    #region typeof(uint)

    /// <summary>
    /// Whether this has no proper value in terms of <see cref="Container"/> information.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="force"></param>
    /// <returns></returns>
    internal static bool IsUpdateNecessary(this uint self, bool force) => force || self == 0;

    internal static uint RotateLeft(this uint self, int bits)
    {
        return (self << bits) | (self >> (32 - bits));
    }

    #endregion

    #region typeof(ushort)

    /// <inheritdoc cref="IsUpdateNecessary(uint, bool)"/>
    internal static bool IsUpdateNecessary(this ushort self, bool force) => IsUpdateNecessary((uint)(self), force);

    #endregion
}
