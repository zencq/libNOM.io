using System.Text.RegularExpressions;

namespace libNOM.io;


#region Container

internal record struct SwitchContainer
{

}

public partial class Container
{
    internal SwitchContainer? Switch { get; set; }
}

#endregion

#region PlatformDirectoryData

internal record class PlatformDirectoryDataSwitch : PlatformDirectoryData
{
    internal override string[] AnchorFileGlob { get; } = new[] { "game_data.sav" };

    internal override Regex[] AnchorFileRegex { get; } = new Regex[] { new("game_data\\.sav", RegexOptions.Compiled) }; // TODO wait for Switch save
}

#endregion

public partial class PlatformSwitch : Platform
{
    #region Property

    #region Exposed Values

    public override bool CanDelete { get; } = false;

    public override bool CanCreate { get; } = false;

    public override bool CanRead { get; } = false;

    public override bool CanUpdate { get; } = false;

    #endregion

    #region Platform Indicator

    internal static PlatformDirectoryData DirectoryData { get; } = new PlatformDirectoryDataSwitch();

    internal override PlatformDirectoryData PlatformDirectoryData { get; } = DirectoryData; // { get; }

    protected override string PlatformArchitecture { get; } = "???"; // TODO wait for Switch save

    public override PlatformEnum PlatformEnum { get; } = PlatformEnum.Switch;

    protected override string PlatformToken { get; } = "???"; // TODO wait for Switch save

    #endregion

    #endregion

    #region Constructor

    public PlatformSwitch() : base(null, null) { }

    public PlatformSwitch(DirectoryInfo? directory) : base(directory, null) { }

    public PlatformSwitch(DirectoryInfo? directory, PlatformSettings? platformSettings) : base(directory, platformSettings) { }

    protected override void InitializeComponent(DirectoryInfo? directory, PlatformSettings? platformSettings)
    {
        base.InitializeComponent(directory, platformSettings);
    }

    #endregion

    #region Read

    #region Create

    protected override Container CreateContainer(int metaIndex, object? extra)
    {
        throw new NotImplementedException();
    }

    #endregion

    #endregion

    #region Write

    protected override byte[] CreateMeta(Container container, byte[] data, int decompressedSize)
    {
        throw new NotImplementedException();
    }

    #endregion
}
