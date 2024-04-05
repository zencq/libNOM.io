# libNOM.io

## What needs to be done when a new game update is released

### Enums
* Check whether game enums have been updated:
    * `DifficultyEnum.cs` (individual difficulty settings)
    * `DifficultyPresetTypeEnum`
    * `PersistentBaseTypesEnum`
    * `PresetGameModeEnum`
    * `SaveContextQueryEnum`
* Extend `GameVersionEnum` and if there is a new Expedition, `SeasonEnum` as well.
  * If necessary add a `Description` attribute to it.
  * Update `Meta\GameVersion.cs` to include new version in detection.

### IContainer
* Add a `IsVersion<Number><Name>` flag for the new version.

### TransferData
* If something new was added that has an ownership, a new flag needs to be added.
* If something new was added that has an ownership, new methods must be added to
  do the `Transfer` method.

## What needs to be done when the game launches on a new platform

Create a new inherited `Platform` class and implement its details.

### Enums
* Add the platform to the `PlatformEnum` with a description attribute if necessary.

### Convert
* The method `ToSaveFile` needs an update for the output switch.

### PlatformCollection
* If the file content of the new platforms is something special add a case to `AnalyzeFile`.
* Add the new `PlatformEnum` to the switch in `AnalyzePath`.
* If it is a PC platform it must be added to the `Reinitialize` method to detect
  it automatically.
