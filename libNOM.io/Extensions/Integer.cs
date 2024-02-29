namespace libNOM.io.Extensions;


internal static class IntegerExtensions
{
    #region typeof(uint)

    internal static uint RotateLeft(this uint self, int bits)
    {
        return (self << bits) | (self >> (32 - bits));
    }

    #endregion
}
