namespace libNOM.io;


/// <summary>
/// Holds a source and a destination container for slot operations.
/// </summary>
public record struct ContainerOperationData
{
    public Container Destination;

    public Container Source;
}
