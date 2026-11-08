# Configuration

Every setting lives in `BepInEx\config\com.ivmakk.nomaddrive.itemhighlight.cfg`, written the first time you run the game with the mod installed. Edit it with any text editor while the game is closed. Changes apply at the next game start. A value out of its range applies as the nearest limit.

## General

| Setting | Default | Values | What it does |
|---|---|---|---|
| `HighlightKey` | `LeftAlt` | A Unity key name, optionally with modifiers: `H + LeftControl` | The key to hold. Write a combination main key first. Other keys you hold at the same time (Shift to sprint) do not stop it. |
| `Radius` | `50` | `5` to `100` | How far from you, in meters, items get the outline. |
| `SearchInterval` | `2` | `1` to `10` | How often, in seconds, the mod looks for items that are new in the world while you hold the key and during the fade (loot that loads in while you drive, an item another player drops). Items it already found update twice a second. A higher value costs less CPU, and new items take longer to get the outline. |
| `OutlineColor` | `#FFFFFF` | A hex color, `#RRGGBB`. The `#` is optional. | The color of the outlines while you hold the key and during the fade. Colors to try: `#7CFF4F`, `#00D8FF`. The item or door under the crosshair always keeps the game's own outline. When you look away from an item, it takes this color again. A value that is not a hex color uses white and writes one warning to the log. |
| `OutlineWidth` | `2` | `1` to `8` | The thickness of the outlines while you hold the key. The fade thins them from this value. The game's own outline is `4.6`. The item or door under the crosshair always keeps the game's own thickness. When you look away from an item, it takes this thickness again. |
| `FadeDelay` | `3` | `0` to `60` | How long, in seconds, the outlines take to fade after you release the key. The fade starts at the release, and the outlines get thinner until they are gone. At `0` the outlines go off at once. The mod keeps updating the outlines during the fade, so a long `FadeDelay` costs the same as holding the key for that time. A menu or a switch to another window ends the fade at once. |
| `Verbose` | `false` | `true`, `false` | Writes extra lines to `BepInEx\LogOutput.log` for troubleshooting. Keep it off in normal play. |
