namespace libNOM.io.Extensions;


public static class HumanizerLikeExtensions
{
    /// <summary>
    /// Denumerates an integer into the Enum it was originally Humanized from.
    /// </summary>
    /// <typeparam name="TTargetEnum">The target enum.</typeparam>
    /// <param name="input">The integer to be converted.</param>
    /// <exception cref="InvalidOperationException"/>
    /// <returns></returns>
    /// <seealso href="https://github.com/Humanizr/Humanizer/blob/main/src/Humanizer/EnumDehumanizeExtensions.cs"/>
    public static TTargetEnum DenumerateTo<TTargetEnum>(this int input) where TTargetEnum : struct, IComparable, IFormattable
    {
        object? match = Enum.GetValues(typeof(TTargetEnum)).Cast<Enum>().FirstOrDefault(value => value.Numerate() == input);

        if (match is null)
            throw new InvalidOperationException("Couldn't find any enum member that matches the integer " + input);

        return (TTargetEnum)(match);
    }

    /// <summary>
    /// Turns an enum member into a number.
    /// </summary>
    /// <param name="input">The enum member to be numerated.</param>
    /// <returns></returns>
    /// <seealso href="https://github.com/Humanizr/Humanizer/blob/main/src/Humanizer/EnumHumanizeExtensions.cs"/>
    public static int Numerate(this Enum input)
    {
        return System.Convert.ToInt32(input);
    }
}
