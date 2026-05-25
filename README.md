# TargetToggle

A lightweight Kerbal Space Program plugin that draws a magenta edge marker
pointing toward your selected target when it's off-screen. The arrow respects
the stock vessel-label toggle (F4) and the hide-UI toggle (F2), stays hidden in
map view, and can be turned off entirely in the in-game difficulty settings.

## Features
- Magenta edge indicator pointing to off-screen targets
- Correctly handles targets behind the camera
- Tied to the stock F4 vessel-labels setting
- Hides with the UI on F2 and in map view
- Toggle in Settings -> Difficulty -> TargetToggle

## Installation
1. Download the latest release.
2. Extract the zip into your KSP install so that `GameData/TargetToggle/`
   sits alongside `GameData/Squad/`.
3. Launch the game.

## Compatibility
- KSP 1.12.x

## Building
Targets .NET Framework 4.7.2. You will need to fix the assembly reference
paths in `TargetToggle.csproj` to point at your own KSP install's
`KSP_x64_Data/Managed/` folder, then build in Release configuration.

## License
MIT - see LICENSE.
