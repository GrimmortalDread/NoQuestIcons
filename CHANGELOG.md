# Changelog

All notable changes to No Quest Icons will be documented here.
This project follows [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

* (Put new features here)

### Changed

* (Put improvements or refactors here)

### Fixed

* (Put bug fixes here)

## [1.4.0] - 2026-07-30

### Changed

* Replaced the reflection-based NamePlate event binding with explicit, statically
  typed subscriptions to `INamePlateGui.OnDataUpdate`, `OnPostDataUpdate`,
  `OnNamePlateUpdate`, and `OnPostNamePlateUpdate`. No behavior change for users,
  but a significant internal rework, addressing prior code review feedback about
  auditability.

### Fixed

* Nameplate marker icon flicker that occurred when binding to a single update
  event alone, since the game resets the icon at multiple points in the
  update cycle.

## [1.3.0] - 2026-07-30

### Changed

* Updated to target Dalamud API 15 and .NET 10 (previously API 13 / .NET 9).
* Nameplate icon suppression continues to use reflection to bind to all compatible
  `INamePlateGui` update events, ensuring the marker icon is cleared every frame
  regardless of which specific event(s) the current Dalamud build exposes.

### Fixed

* Plugin now loads correctly on current Dalamud builds.

## [1.0.0] - 2024-09-11

### Added

* Initial release 🎉
* Removes quest icons (diamonds, exclamation/question marks) from NPC nameplates.
* Works client-side only — no game files modified.
* Zero flickering.
* Multi-tabbed settings window (General / Advanced / About).
