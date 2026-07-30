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

## [1.3.0] - 2026-07-30

### Changed

* Updated to target Dalamud API 15 and .NET 10 (previously API 13 / .NET 9).
* Reworked NPC nameplate icon suppression to bind directly to `INamePlateGui.OnDataUpdate` instead of `OnNamePlateUpdate`, fixing icon flicker.

### Fixed

* Plugin now loads correctly on current Dalamud builds.

## [1.0.0] - 2024-09-11

### Added

* Initial release 🎉
* Removes quest icons (diamonds, exclamation/question marks) from NPC nameplates.
* Works client-side only — no game files modified.
* Zero flickering.
* Multi-tabbed settings window (General / Advanced / About).
