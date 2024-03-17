using System.ComponentModel;

namespace libNOM.cli.Enums;


/// <summary>
/// Specifies the different types of storage persistence used for meta/manifest encryption.
/// Original found in NMS.exe as enum cTkStoragePersistent::Slot.
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
