namespace libNOM.io.Extensions;


internal static class IContainerExtensions
{
    /// <summary>
    /// Check whether this is the IContainer implementation from this library as following calls will only work with it.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    internal static Container ToContainer(this IContainer self)
    {
        if (self is Container nonIContainer)
            return nonIContainer;

        throw new InvalidOperationException("This implementation of IContainer is not supported.");
    }
}
