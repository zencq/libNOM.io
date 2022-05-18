# CHANGELOG

All notable changes to this project will be documented in this file. It uses the
[Keep a Changelog](http://keepachangelog.com/en/1.0.0/) principles and
[Semantic Versioning](https://semver.org/).

## Unreleased

### Added
### Changed
### Deprecated
### Removed
### Fixed
### Security

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
