# libNOM.io

## What needs to be done when a new game update is released

### Enums
* Check whether game enums have been updated:
    * `DifficultyPresetTypeEnum`
    * `ParticipantTypeEnum`
    * `PersistentBaseTypesEnum`
    * `PresetGameModeEnum`
* Extend `VersionEnum` and if there is a new Expedition, update the `SeasonEnum`
  as well.
* If necessary add a `Description` attribute to it.

### Container
* Add a `IsVersion<Number><Name>` flag for the new version.

### ContainerTransferData
* If something new was added that has an ownership, a new flag needs to be added.

### Global
* For new game modes the `GetGameModeEnum` needs to be updated.
* For new difficulty preset the `DifficultyPresetTypeEnum` needs to be updated.
* Will probably never be the case but if the formula for the version numbers changes,
  the `CalculateBaseVersion` and `CalculateVersion` need an updated as well.

### Platform
* Update constants and getter to reflect new data.
* Both `GetVersionEnum` need to include the new base version or a new JSON key.
* If something new was added that has an ownership, new methods must be added to
  do the transfer.

## What needs to be done when the game launches on a new platform

Create a new inherited `Platform` class and implement its details.

### Enums
* Add the platform with a description attribute if necessary.

### Convert
* The method `ToSaveFile` needs an update for the output switch.

### PlatformCollection
* If the file content of the new platforms is something special add a case to `AnalyzeFile`.
* Add the new `PlatformEnum` to the switch in `AnalyzePath`.
* If it is a PC platform it must be added to the `Reinitialize` method to detect
  it automatically.
