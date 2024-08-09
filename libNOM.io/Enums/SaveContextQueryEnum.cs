namespace libNOM.io.Enums;


/// <summary>
/// Specifies the possible contexts a save file can have.
/// <seealso href="https://github.com/monkeyman192/MBINCompiler/blob/development/libMBIN/Source/NMS/GameComponents/GcSaveContextQuery.cs#L7"/>
/// </summary>
// EXTERNAL RELEASE: if any, apply changes from libMBIN.
public enum SaveContextQueryEnum : uint
{
    DontCare,
    Season,
    Main,
    NoSeason,
    NoMain,
}
