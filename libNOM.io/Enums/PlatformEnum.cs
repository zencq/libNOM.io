using System.ComponentModel;

namespace libNOM.io.Enums;


/// <summary>
/// Specifies platforms the game is available on.
/// PlayStation is last as it has the least specific identification characteristics (only Steam like save file in worst case).
/// Otherwise ordered by assumed player base.
/// </summary>
// EXTERNAL RELEASE: If any, add new platform including the whole implementation.
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
