using System.ComponentModel;

namespace libNOM.cli.Enums;


/// <summary>
/// Specifies the different formats a save can be converted into.
/// </summary>
public enum FormatEnum
{
    [Description("JSON")]
    Json,
    Steam,
    Microsoft,
    [Description("GOG.com")]
    Gog,
    [Description("Nintendo Switch")]
    Switch,
    [Description("PlayStation")]
    Playstation,
}
