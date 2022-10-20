namespace libNOM.io.Extensions;


public static class HumanizerLikeExtensions
{
    /// <summary>
    /// Denumerates an integer into the Enum it was originally Humanized from.
    /// </summary>
    /// <typeparam name="TTargetEnum">The target enum.</typeparam>
    /// <param name="self">The integer to be converted.</param>
    /// <exception cref="InvalidOperationException"/>
    /// <returns></returns>
    /// <seealso href="https://github.com/Humanizr/Humanizer/blob/main/src/Humanizer/EnumDehumanizeExtensions.cs"/>
    public static TTargetEnum DenumerateTo<TTargetEnum>(this int self) where TTargetEnum : struct, IComparable, IFormattable
    {
        object? match = Enum.GetValues(typeof(TTargetEnum)).Cast<Enum>().FirstOrDefault(value => value.Numerate() == self);

        if (match is null)
            throw new InvalidOperationException("Couldn't find any enum member that matches the integer " + self);

        return (TTargetEnum)(match);
    }

    /// <summary>
    /// Turns an enum member into a number.
    /// </summary>
    /// <param name="self">The enum member to be numerated.</param>
    /// <returns></returns>
    /// <seealso href="https://github.com/Humanizr/Humanizer/blob/main/src/Humanizer/EnumHumanizeExtensions.cs"/>
    public static int Numerate(this Enum self)
    {
        return System.Convert.ToInt32(self);
    }
}
