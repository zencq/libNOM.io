namespace libNOM.io.Extensions;


internal static class BooleanExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="self"></param>
    /// <param name="setpoint"></param>
    /// <returns>Returns true if this has a value and it is equal to the setpoint.</returns>
    internal static bool IsValue(this bool? self, bool setpoint)
    {
        return self.HasValue && self.Value == setpoint;
    }
}
