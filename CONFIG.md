# Configuration

Every setting lives in `BepInEx\config\com.ivmakk.nomaddrive.itemhighlight.cfg`, written the first time you run the game with the mod installed. Edit it with any text editor while the game is closed. Changes apply at the next game start. A value out of its range applies as the nearest limit.

## General

| Setting | Default | Values | What it does |
|---|---|---|---|
| `HighlightKey` | `LeftAlt` | A Unity key name, optionally with modifiers: `H + LeftControl` | The key to hold. Write a combination main key first. Other keys you hold at the same time (Shift to sprint) do not stop it. |
| `Radius` | `50` | `5` to `100` | How far from you, in meters, items get the outline. |
| `SearchInterval` | `2` | `1` to `10` | How often, in seconds, the mod looks for items that are new in the world while you hold the key (loot that loads in while you drive, an item another player drops). Items it already found update twice a second. A higher value costs less CPU, and new items take longer to get the outline. |
| `Verbose` | `false` | `true`, `false` | Writes extra lines to `BepInEx\LogOutput.log` for troubleshooting. Keep it off in normal play. |
