# CHANGELOG

All notable changes to this project will be documented in this file. It uses the
[Keep a Changelog](http://keepachangelog.com/en/1.0.0/) principles and [Semantic Versioning](https://semver.org/)
since 1.0.0.

## Unreleased (0.10.0)

### Known Issues
### Added
* `GetString` extension for `JToken` that is now used by the one for `JObject`
* Support for game version **Worlds 5.00**
* Liquidators Expedition
### Changed
* Renamed `IsVersion452OmegaWithV2` in `IContainer` to `IsVersion452OmegaWithMicrosoftV2`
* Renamed `COUNT_SAVE*` constants to `MAX_SAVE*` and made them publicly available
### Deprecated
### Removed
### Fixed
* Crash if `Platform.GetHashCode()` is used
### Security

## 0.9.0 (2024-06-07)

### Added
* `PlatformCollection.Contains(string path)` overload
* Support for game version **Adrift 4.70**
* Adrift Expedition

### Changed
* `PlatformSettings.MaxBackupCount <= 0` is disabling the backup feature again
  * Existing backups will be deleted the next time one would be created otherwise
  * To make it unlimited-like set it to `int.MaxValue`
* `Constants.LOWEST_SUPPORTED_VERSION` is publicly accessible
* Using the `IContainer` interface instead of `Container` directly, in interfaces
  and other public methods
* `Platform.RestartToApply` is now an enum for more precision on when a game restart
  is required

### Fixed
* The values for the properties `Container.ActiveContext` and `Container.CanSwitchContext`
  are now properly set even if the `Container` is not fully loaded

## 0.8.0 (2024-04-05)

### Added
* CLI
  * Analyze single files or whole directories and print information about it
  * Convert between JSON and actual save formats
  * Perform file operations
* `PlatformCollectionSettings` to configure `PlatformCollection`
* Support for game version **Orbital 4.60**

### Changed
* `PlatformSettings.MaxBackupCount <= 0` is unlimited and not unintentionally disabling
  the backup feature
* `Container.ThrowHelperIsLoaded` now shows incompatibility if any
* Replace preferred platform in constructors of `PlatformCollection` with new
  `PlatformCollectionSettings`
* Moved `Settings` to its own namespace
* Bump *K4os.Compression.LZ4* from 1.3.6 to 1.3.8
* Bump *libNOM.map* from 0.11.0 to 0.12.0

## 0.7.0 (2024-03-13)

### Added
* Now targeting .NET 8 as per the [.NET release lifecycle](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)
* Holiday 2023 Expeditions
* Support for game version **Omega 4.50**
    * `IsExpedition` flag has been replaced in favor of `HasActiveExpedition`
    * New property `CanSwitchContext` to indicated whether it is possible to switch
      between primary save and expedition
    * New property `ActiveContext` to indicated the active context
    * `GetJson...` getter in `Container` have been extended to also take a context
      value (without that parameter `ActiveContext` is used)
* Support for new data file format used on Microsoft platform since game version
  **Omega 4.52**
* Omega Expedition

### Changed
* `GetMaximumSlots` is now an extension of `IEnumerable<Container>`
* `GetSaveContainers()` replaces all previous SaveContainers getter
* To transfer you only have to call `GetSourceTransferData` and `Transfer` now,
  the destination preparation is done automatically
* Bump *CommunityToolkit.Diagnostics* from 8.2.1 to 8.2.2
* Bump *CommunityToolkit.HighPerformance* from 8.2.1 to 8.2.2
* Bump *libNOM.map* from 0.9.2 to 0.11.0

### Removed
* Dedicated getter for paths in `Platform.Settings`

### Fixed
* If a new `JObject` is set in a `Container`, ensure it will be stored with the
  configured obfuscation

## 0.6.0 (2023-09-11)

### Added
* Now targeting .NET 7 as per the [.NET release lifecycle](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)
* Now publishing to [NuGet Gallery](https://www.nuget.org/packages/libNOM.io)
* A setting `WriteAlways` to choose between writing always or only if a container
  is not synced
* A privacy setting to decide whether external sources should be used to improve
  user identification (e.g. launcher configs or API calls)
* An `IPlatform` interface you can use instead of the abstract base class
* Lots of UnitTests
* Support for game version **Echoes 4.40**
* Voyagers Expedition
* Support for macOS if Steam is used
* Probably more...

### Changed
* Bump *K4os.Compression.LZ4* from 1.3.5 to 1.3.6
* Bump *libNOM.map* from 0.9.1 to 0.9.2
* Renamed the setting `LastWriteTime` to `SetLastWriteTime` and `Mapping` to
  `UseMapping`
* Default value for some settings
* Names of the `IsVersion` flags now include the version number as well
* `DifficultyPresetTypeEnum` has been added and therefore `PresetGameModeEnum` is
  now only used internal
* More getter and setter for JSON tokens and values
* Probably more...

### Removed
* Maybe some things...

### Fixed
* The `Mapping` setting is now only used to determine input/output and not for
  modifying things internally
* A number of different issues reported on [Discord](https://discord.gg/nomnom-762409407488720918)
  and the [NomNom repository](https://github.com/zencq/NomNom/milestone/10)

## 0.5.3 (2023-06-24)

### Added
* Support for game versions up to **Singularity 4.30**
* Singularity Expedition

## 0.5.2 (2023-03-19)

### Added
* Support for game version **Fractal 4.10**
* Utopia Expedition

### Fixed
* `OutOfMemoryException` while reading Microsoft account files (4.10 only)

## 0.5.1 (2022-11-24)

### Added
* Holiday 2022 Expeditions

## 0.5.0 (2022-10-31)

### Added
* OS dependent default paths for Steam
* Support for game version **Waypoint 4.00**
* Support for the Switch platform

### Changed
* `PlatformCollection` itself is now iterable and therefore `Get()` was removed
* Threshold is now the lowest ever used base version

### Fixed
* Explicitly do not use compression for account data
* *SaveWizard* usage detection if *savedata00.hg* file (account data) is present
* Handling for compression when PlayStation platform is used
* Save Transfer for old saves
* Getter for deobfuscated JSON string (mostly old SaveWizard *memory.dat*)
* Unable to copy/transfer a save to an empty slot

## 0.4.1 (2022-08-10)

### Changed
* Further adaptions for the Microsoft platform
* Now targeting .NET Standard 2.x and supported versions in the [.NET release lifecycle](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)

### Fixed
* `HasAccountData` flag not properly calculated in same cases

## 0.4.0 (2022-07-25)

### Changed
* Use separate setter for `GameModeEnum` that automatically adds/removes the creative
  primary mission
* Lots of tweaks to improve compatibility

### Removed
* .NET Framework as explicit target as .NET Standard is enough

### Fixed
* Total size of a save (data + meta) not correctly calculated for use in
  *containers.index*
* Crash when there is a entry in the *containers.index* without actual files

## 0.3.1 (2022-05-25)

### Added
* Support for game version **Leviathan 3.90**

### Fixed
* Calculation of game mode version number when switching between `Seasonal` and
  others
* Calculation of `SeasonEnum` in rare occasions

## 0.3.0 (2022-05-18)

### Changed
* Moved data records to separate `Data` namespace

### Fixed
* A few things regarding the Microsoft platform

## 0.2.0 (2022-05-05)

### Added
* Additional settings
    * Whether to deobfuscate
    * How many backups should be kept
    * Whether to use the built-in file watcher

### Changed
* A lot!

### Removed
* Maybe some things...

### Fixed
* Everything that has not worked properly in the first iteration

## 0.1.0 (2022-03-14)

### Added
* Detect all available PC platforms of the current user in their default locations
* Detect platform in a custom location
* Support for the following platforms
    * GOG.com (Windows PC)
    * Microsoft Store (Windows PC)
    * PlayStation 4
    * Steam (Windows PC)
    * Xbox
* `Backup`/`Restore`
* Convert game-readable files to human-readable
* Convert human- and game-readable files to the format of a specified platform
* `Copy`
* `Delete`
* `Move`
* `Swap`
* `Transfer` between different platform instances
    * Select what you want to transfer (result depends on upload status)
        * Bases (interface to select which)
        * ByteBeats (3.51+)
        * Discoveries
        * Settlement (3.60+)
* Write
* Built-in watcher to detect background changes incl. an interface for user decisions
* Built-in loading strategies defined via settings (see below)
* Use settings to customize the behavior
    * Paths for backups, downloads, etc.
    * Whether to update modification time
    * Which loading strategy to use
