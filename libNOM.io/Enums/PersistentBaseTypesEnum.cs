﻿namespace libNOM.io.Enums;


/// <summary>
/// Specifies the different types a base can have.
/// </summary>
/// <seealso href="https://github.com/monkeyman192/MBINCompiler/blob/development/libMBIN/Source/NMS/GameComponents/GcPersistentBaseTypes.cs#L7"/>
// EXTERNAL RELEASE: if any, apply changes from libMBIN.
internal enum PersistentBaseTypesEnum : uint
{
    HomePlanetBase,
    FreighterBase,
    ExternalPlanetBase,
    CivilianFreighterBase,
    FriendsPlanetBase,
    FriendsFreighterBase,
    SpaceBase,
    GeneratedPlanetBase,
    GeneratedPlanetBaseEdits,
    PlayerShipBase,
    FriendsShipBase,
}
