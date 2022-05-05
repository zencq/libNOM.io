using System.ComponentModel;

namespace libNOM.io.Enums;


/// <summary>
/// Specifies platforms the game is avaiable on.
/// </summary>
public enum PlatformEnum
{
    Unknown,
    [Description("GOG.com")]
    Gog,
    Steam,
    Microsoft,
    [Description("PlayStation")]
    Playstation,
    [Description("Nintendo Switch")]
    Switch,
}
