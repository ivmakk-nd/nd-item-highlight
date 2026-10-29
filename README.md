# Item Highlight

Hold a key to outline every item you can pick up around you.

Small items are easy to miss in tall grass, in houses, and around wrecks, and the game outlines an item only when you look straight at it. Hold Left Alt and every item you can pick up within 50 m gets the same outline, even behind walls. Release the key and the outlines go away.

## Features

- **Through walls**: the outline shows through walls, car bodies, and hills, so you see what is inside a house before you go in.
- **Your car stays quiet**: items in or on a car you can drive and the parts installed on it get no outline. Items in wrecks and loose car parts do.
- **Only what you can take**: items in your hands, in your hotbar, or in another player's hands get no outline.
- **Settings**: change the key and the radius, up to 100 m.

## Multiplayer

Not tested in co-op. The outlines show only on your own screen and the mod sends nothing over the network, so each player can install it or not, with or without the mod on the host.

## Save safety

The mod stores nothing in your save, so you can remove it at any time.

## Requirements

- Nomad Drive (Early Access), the update of 28 October 2026 or later.
- [Nomad Drive BepInEx Pack](https://www.nexusmods.com/nomaddrive/mods/7).

## Installation

1. Install the [BepInEx pack](https://www.nexusmods.com/nomaddrive/mods/7).
2. Extract this mod's zip into the game folder (the folder with the game .exe). The DLL lands in BepInEx\plugins. Full path example:
   `C:\Program Files (x86)\Steam\steamapps\common\Nomad Drive\BepInEx\plugins\ItemHighlight.dll`

## Uninstall

Delete `ItemHighlight.dll` from the `BepInEx\plugins` folder.

## Configuration

To change mod settings, edit `BepInEx\config\com.ivmakk.nomaddrive.itemhighlight.cfg` while the game is closed (see [CONFIG.md](CONFIG.md) for every setting). Changes apply at the next game start.

## Build

The build needs the game and the BepInEx pack installed, and the .NET SDK 10.

```
dotnet build src/ItemHighlight.csproj -c Release
dotnet test tests/ItemHighlight.Tests
```

The build looks for the game in `C:\Program Files (x86)\Steam\steamapps\common\Nomad Drive`. For another folder, pass `-p:GameDir=<game folder>` or set a `GameDir` environment variable. Add `-p:Package=true` to make the install zip in `dist/`.

## License

Licensed under the GNU General Public License v3.0. Copyright (C) 2026 ivmakk.

Do not reupload this mod or a changed copy without credit and a link to this repository.
