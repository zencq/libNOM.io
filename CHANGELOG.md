# CHANGELOG

All notable changes to this project will be documented in this file. It uses the
[Keep a Changelog](http://keepachangelog.com/en/1.0.0/) principles and
[Semantic Versioning](https://semver.org/).

## Unreleased

### Known Issues
### Added
* Now targeting .NET 8 as per the [.NET release lifecycle](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)
* Holiday 2023 Expeditions
* Support for game version **Omega 4.50**
    * IsExpedition flag has been replaced in favor of HasActiveExpedition
    * New property CanSwitchContext to indicated whether it is possible to switch between primary save and expedition
* Omega Expedition
### Changed
* `GetSaveContainers` replaces all previous SaveContainer getter
* Bump CommunityToolkit.Diagnostics from 8.2.1 to 8.2.2
* Bump CommunityToolkit.HighPerformance from 8.2.1 to 8.2.2
* Bump libNOM.map from 0.9.2 to 0.10.0
### Deprecated
### Removed
* `GetMaximumSlots` but you can still get it with `GetSaveContainers().Count() / 2`
### Fixed
* If JObject is set in a Container, ensure it will be stored with the configured obfuscation
### Security

## 0.6.0 (2023-09-11)

### Added
* Now targeting .NET 7 as per the [.NET release lifecycle](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)
* Now publishing to [NuGet Gallery](https://www.nuget.org/packages/libNOM.io)
* A setting to choose between writing always or only if a container is unsynced
* A privacy setting to decide whether external sources should be used to to improve
  user identification (e.g. launcher configs or API calls)
* An IPlatform interface you can use instead of the abstract base class
* Lots of UnitTests
* Support for game version **Echoes 4.40**
* Voyagers Expedition
* Support for macOS when Steam is used
* Probably more...

### Changed
* Bump K4os.Compression.LZ4 from 1.3.5 to 1.3.6
* Bump libNOM.map from 0.9.1 to 0.9.2
* Renamed the settings _LastWriteTime_ and _Mapping_
* Default value for some settings
* Names of the IsVersion flags now include the version number as well
* DifficultyPresetTypeEnum has been added and therefore PresetGameModeEnum is now
  only used internal
* More getter and setter for JSON tokens and values
* Probably more...

### Removed
* Maybe some things...

### Fixed
* The _Mapping_ settings is now only used to determine input/output and not for
  modifying things internally
* A number of different issues reported on [Discord](https://discord.gg/nomnom-762409407488720918) and the [NomNom repository](https://github.com/zencq/NomNom/milestone/10)

## 0.5.3 (2023-06-24)

### Added
* Support for game versions up to **Singularity 4.30**
* Singularity Expedition

## 0.5.2 (2023-03-19)

### Added
* Support for game version **Fractal 4.10**
* Utopia Expedition

### Fixed
* OutOfMemoryException while reading Microsoft account files (4.10 only)

## 0.5.1 (2022-11-24)

### Added
* Holiday 2022 Expeditions

## 0.5.0 (2022-10-31)

### Added
* OS dependent default paths for Steam
* Support for game version **Waypoint 4.00**
* Support for the Switch platform

### Changed
* PlatformCollection itself is now iterable and therefore `Get()` was removed
* Threshold is now the lowest ever used base version

### Fixed
* Explicitly do not use compression for account data
* SaveWizard usage detection if savedata00.hg file (account data) is present
* Handling for compression when PlayStation platform is used
* Save Transfer for old saves
* Getter for deobfuscated JSON string (mostly old SaveWizard memory.dat)
* Unable to copy/transfer a save to an empty slot

## 0.4.1 (2022-08-10)

### Changed
* Further adaptions for the Microsoft platform
* Now targeting .NET Standard 2.x and supported versions in the [.NET release lifecycle](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)

### Fixed
* HasAccountData flag not properly calculated in same cases

## 0.4.0 (2022-07-25)

### Changed
* Use separate setter for `GameModeEnum` that automatically adds/removes the creative primary mission
* Lots of tweaks to improve compatibility

### Removed
* .NET Framework as explicit target as .NET Standard is enough

### Fixed
* Total size of a save (data + meta) not correctly calculated for use in containers.index
* Crash when there is a entry in the containers.index without actual files

## 0.3.1 (2022-05-25)

### Added
* Support for game version **Leviathan 3.90**

### Fixed
* Calculation of GameMode version number when switching between Seasonal and others
* Calculation of SeasonEnum in rare occasions

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
    * Whether to use the built-in File Watcher

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
    * GOG.com (PC)
    * Microsoft Store (PC)
    * PlayStation 4
    * Steam (PC)
    * Xbox
* Backup
* Convert game-readable files to human-readable
* Convert human- and game-readable files to the format of a specified platform
* Copy
* Delete
* Move
* Restore
* Swap
* Transfer between different platform instances
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
