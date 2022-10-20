using System.ComponentModel;

namespace libNOM.io.Enums;


/// <summary>
/// Specifies platforms the game is available on.
/// PlayStation is last as it has the least specific identification characteristics (only Steam like save file in worst case).
/// </summary>
public enum PlatformEnum
{
    Unknown,
    Steam,
    Microsoft,
    [Description("GOG.com")]
    Gog,
    [Description("Nintendo Switch")]
    Switch,
    [Description("PlayStation")]
    Playstation,
}
