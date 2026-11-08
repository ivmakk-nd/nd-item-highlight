# Changelog

All notable changes to this mod are documented in this file. The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- The outlines fade out over 3 seconds after you release the key, and they keep updating while they fade. The `FadeDelay` setting takes 0 to 60 seconds, and 0 turns the outlines off at once.
- The `OutlineColor` setting takes a hex color for the outlines while you hold the key and during the fade. The default is white.
- The `OutlineWidth` setting takes the thickness of the outlines, from 1 to 8.
- The item or door under your crosshair keeps the game's normal outline while you hold the key and during the fade. An item takes your color and thickness again when you look away.

### Changed

- The outlines are thinner by default: 2, where the game's own outline is 4.6. Set `OutlineWidth` to 4.6 for the old look. The game's hover outline without the key does not change.

## [1.0.0] - 2026-10-29

### Added

- Hold Left Alt to outline every item you can pick up within 50 m, through walls, with the game's hover outline.
- No outline for items in or on a car you can drive and for the parts installed on it. Items in wrecks and loose car parts get the outline.
- No outline for items in your hands, in your hotbar, in another player's hands, or in the air after a throw.
- Settings for the key, the radius, and how often the mod looks for new items.

[Unreleased]: https://github.com/ivmakk-nd/nd-item-highlight/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/ivmakk-nd/nd-item-highlight/releases/tag/v1.0.0
