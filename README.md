# libNOM.io

![Maintained](https://img.shields.io/maintenance/yes/2024)
[![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/zencq/libNOM.io/pipeline.yml?logo=github)](https://github.com/zencq/libNOM.io/actions/workflows/pipeline.yml)
[![Maintainability](https://api.codeclimate.com/v1/badges/5f2e527d62758832d38b/maintainability)](https://codeclimate.com/github/zencq/libNOM.io/maintainability)

[![.NET | Standard 2.0 - 2.1 | 8 - 9](https://img.shields.io/badge/.NET-Standard%202.0%20--%202.1%20%7C%208%20--%209-lightgrey)](https://dotnet.microsoft.com/en-us/)
[![C# 13](https://img.shields.io/badge/C%23-13-lightgrey)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![GitHub Release](https://img.shields.io/github/v/release/zencq/libNOM.io?logo=github)](https://github.com/zencq/libNOM.io/releases/latest)
[![NuGet Version](https://img.shields.io/nuget/v/libNOM.io?logo=nuget&label=release)](https://www.nuget.org/packages/libNOM.io/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/libNOM.io?logo=nuget)](https://www.nuget.org/packages/libNOM.io/)

## Introduction

The `libNOM` label is a collection of .NET class libraries originally developed
and used in [NomNom](https://github.com/zencq/NomNom), the most complete savegame
editor for [No Man's Sky](https://www.nomanssky.com/).

`libNOM.io` can be used to read and write save files for all supported platforms
as well as performing related actions.

## Getting Started

Currently save formats `2001` (**Foundation 1.10** to **Prisms 3.53**), `2002`
(**Frontiers 3.60** to **Adrift 4.72**) including the extension made in **Waypoint 4.00**,
and `2003` (**Worlds Part I 5.00** and up) are supported.
The original format `2000` that was used in the vanilla game is not supported. If
you are interested in it, have a look at the [nms-savetool by MetaIdea](https://github.com/MetaIdea/nms-savetool).

The lowest officially supported game version is **Beyond 2.11** to enable support
for homebrew users on PlayStation 4 which could not update further at some point.
You can use older saves nonetheless, but the game version cannot be determined and
it will be marked as `IsOld`.

Each platform has anchor file patterns to check whether it is worth to look further
into the selected directory. This must be in or one level below the selected one.

### Compatibility

* Apple
    * Notes: Currently only available via Steam (see below).
* [GOG.com](https://www.gog.com/game/no_mans_sky) (Windows PC)
    * Location: **%AppData%\HelloGames\NMS\DefaultUser**
    * File Patterns: **save\*.hg**
* [PlayStation 4](https://store.playstation.com/?resolve=EP2034-CUSA03952_00-NOMANSSKYHG00001)
    * File Patterns: **memory.dat**, **savedata\*.hg**
    * Notes: There are a few options to do this. The only one that does not require
      homebrew is [SaveWizard](https://www.savewizard.net). Two other tools that
      are confirmed working are [Save Mounter](https://github.com/ChendoChap/Playstation-4-Save-Mounter)
      and [Apollo](https://github.com/bucanero/apollo-ps4) but require homebrew.
      Results of other tools may or may not work but the code is as generic as possible.
* [PlayStation 5](https://store.playstation.com/?resolve=EP2034-CUSA03952_00-NOMANSSKYHG00001)
    * Notes: This version of the game is not supported due to restrictions on the
      console itself. By playing the PlayStation 4 version on it, you can still
      save edit with [a few additional steps](https://docs.google.com/document/d/1QoD2-PNlX-HeR5K1zuPGLMLBcX4_wknkhzc43-9bEq4/edit?usp=sharing).
* [Steam](https://store.steampowered.com/app/275850/No_Mans_Sky/) (PC)
    * Location
      * Windows: **%AppData%\HelloGames\NMS\st\_\<SteamID\>**
      * SteamDeck: **~/.local/share/Steam/steamapps/compatdata/275850/pfx/drive_c/users/steamuser/Application Data/HelloGames/NMS/st\_\<SteamID\>**
      * macOS: **~/Library/Application Support/HelloGames/NMS/st\_\<SteamID\>**
    * File Patterns: **save\*.hg**
    * Notes: If you use a cloud gaming service like GeForce NOW you can still use
      it by starting the game to trigger synchronization from/to the cloud.
* [Microsoft Store](https://www.microsoft.com/p/no-mans-sky/bqvqtl3pch05) (Windows PC)
    * Location: **%LocalAppData%\Packages\HelloGames.NoMansSky_bs190hzg1sesy\SystemAppData\wgs\\<XboxID\>_29070100B936489ABCE8B9AF3980429C**
    * File Patterns: **containers.index**
    * Notes: Reloading of modified saves while the game is running does not work.
* [Nintendo Switch](https://www.nintendo.com/store/products/no-mans-sky-switch)
    * File Patterns: **manifest\*.dat**
    * Notes: To get your saves you need homebrew software on your Switch. [EdiZon](https://github.com/WerWolv/EdiZon)
      and [JKSV](https://github.com/J-D-K/JKSV) are confirmed working. Results of
      other tools may or may not work but the code is as generic as possible.
* [Xbox One/Series X\|S](https://www.microsoft.com/p/no-mans-sky/bqvqtl3pch05)
    * Notes: Not directly supported but can easily achieved with cloud sync via
      the Microsoft Store. The synchronization is triggered short after you close
      the game (no need to load a save). This also works for Xbox Cloud Gaming.

### Usage

Here you'll find an example usage.
```csharp
var path = "...";
var settings = new PlatformSettings { LoadingStrategy = LoadingStrategyEnum.Current };

var collection = new PlatformCollection(); // detects all available PC platforms on a machine
var platform = collection.AnalyzePath(path, settings); // get platform in path and add to collection

var collection = new PlatformCollection(path); // additionally analyzes path
var platform = collection.Get(path); // get a previously detected platform with this path

var account = platform.GetAccountContainer(); // always loaded if exists
var save = platform.GetContainer(0); // Slot1Auto // loaded by default if LoadingStrategyEnum.Full

platform.Load(save); // needs to be loaded before you can modify its JSON

// Get the entire object or parts of it to modify on your own or get and set values directly.
// The getter and setter except multiple expressions but only the first valid one will be returned.
JsonObject jsonObject = save.GetJsonObject();
JToken? jsonToken = save.GetJsonToken("JSONPath1");
IEnumerable<JToken> jsonTokens = save.GetJsonTokens("JSONPath1");
int? jsonObject = save.GetJsonValue<int>("PlayerStateData.UniverseAddress.GalacticAddress.VoxelZ");
int? jsonObject = save.GetJsonValue<int>(new[] { 2, 0, 1, 2 }); // as above but with indices

save.SetJsonValue(1, "PlayerStateData.UniverseAddress.GalacticAddress.PlanetIndex");
save.SetJsonValue(1, new[] { 2, 0, 1, 4 }); // save as above

platform.Write(container);
```

## Projects using libNOM.io
* [NomNom](https://github.com/zencq/NomNom) (2022-03-14)
* [NMS Companion](https://www.nexusmods.com/nomanssky/mods/1879) (2022-05-01)

## License

This project is licensed under the GNU GPLv3 license - see the [LICENSE](LICENSE)
file for details.

## Authors

* **Christian Engelhardt** (zencq) - [GitHub](https://github.com/cengelha)

## Credits

Thanks to the following people for their help in one way or another.

* [u/Gumsk](https://www.reddit.com/r/NoMansSkyTheGame/comments/lk6yk6/how_to_move_a_gamepass_save_to_steam/) - Working out how to properly move a save from one platform to another
* [MetaIdea](https://github.com/MetaIdea/nms-savetool) - Decrypt and encrypt Steam saves
* [u/MegaGold_Fighter](https://www.reddit.com/r/NoMansSkyMods/comments/hhe2he/ps4_nms_save_editing_general_guide/) - [Storm21](https://psxtools.de/index.php?user/38756-storm21/) - Helping and verifying to make PlayStation support possible
* [Moo](https://discord.gg/22ZAU9H) - Helping and verifying to make Microsoft Store support possible

## Dependencies

* [.NET Community Toolkit](https://github.com/CommunityToolkit/dotnet) - Diagnostics and high performance helper
* [K4os.Compression.LZ4](https://www.nuget.org/packages/K4os.Compression.LZ4/) - Compression and decompression
* [LazyCache](https://www.nuget.org/packages/LazyCache) - Caching when a file is updated in the background
* [libNOM.map](https://www.nuget.org/packages/libNOM.map) - Obfuscation and deobfuscation
* [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json/) - Handle JSON objects
* [SpookilySharp](https://www.nuget.org/packages/SpookilySharp/) - Creating SpookyHash
