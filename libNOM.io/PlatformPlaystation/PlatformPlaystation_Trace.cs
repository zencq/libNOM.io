namespace libNOM.io;


// This partial class contains tracing related code.
public partial class PlatformPlaystation : Platform
{
    #region Initialize

    protected override void InitializeTrace()
    {
        base.InitializeTrace();

        // Container
        foreach (var container in SaveContainerCollection)
            container.Trace = new()
            {
                PlaystationOffset = container.Extra.PlaystationOffset,
            };
    }

    #endregion
}
