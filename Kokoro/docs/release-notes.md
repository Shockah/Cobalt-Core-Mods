[← back to readme](README.md)

# Release notes

## 1.7.0

* Added an API to choose the destination for `ACardOffering`/`CardReward`.
* Proxied hooks are now marked with a proxy interface, allowing better integration.
* Kokoro wrapped actions now get their `whoDidThis` set.

## 1.6.0
Released 18 April 2024.

* Added an API to choose a font for a given card's text.
* Added a custom compact variant of the Pinch font.
* Added Nickel-compatible APIs for getting the Oxidation and Worm statuses.
* Fixed card (action) rendering matrix transformations breaking in certain scenarios.
* Fixed wrapped attack actions visually losing their stun icon.
* Fixed hand card count condition tooltip.

## 1.5.0
Released 29 February 2024.

* Expanded upon the evade and droneshift APIs (thanks to [TheJazMaster](https://github.com/TheJazMaster)!).

## 1.4.0
Released 13 February 2024.

* Added extra evade and droneshift related APIs.
* Fixed the `APlaySpecificCardFromAnywhere` action's behavior.

## 1.3.1
Released 22 January 2024.

* Ported Nickel's fix for deserializing non-primitive `ModData` to `ExtensionData`.
* Fixed Timestop trying to decrease even at 0, and decreasing twice per turn.

## 1.3.0
Released 20 January 2024.

* **SOME MODS USING THIS VERSION CANNOT WORK IN THE LEGACY MOD LOADER ANYMORE, UNLESS THEY FIX THEIR COPIED API DECLARATIONS. IT'S RECOMMENDED TO SWITCH TO NICKEL.**
* Added an extra overload to `IStatusRenderHook.OverrideStatusTooltips`, passing in the `Ship` the status is on.
* Added a way to temporarily stop card rendering transformations.
* Fixed card rendering transformations randomly stopping working.
* Fixed Oxidation status tooltip.

## 1.2.1
Released 13 January 2024.

* Now comes with a `nickel.json` file.
* Fixed Engine Lock being decremented too early.
* Fixed card action scaling hooks.

## 1.2.0
Released 6 January 2024.

* Fixed conditional actions having wrong tooltips if used to check if the player just has a status at all.
* For players and modders: Fixed `APlaySpecificCardFromAnywhere` (and in turn `APlayRandomCardsFromAnywhere`) getting into an endless loop if the hand ends up being full in between cards being played (apparent in the Soggins' Do Something! card).
* For modders: Added `IStatusLogicHook.OnStatusTurnTrigger` and `IStatusLogicHook.HandleStatusTurnAutoStep` APIs.
* For modders: Added resource cost action APIs.
* For modders: Added continue/stop action APIs.
* For modders: Added an API to make outgoing X assignment actions.
* For modders: Added APIs to make energy X assignment and energy modification actions.
* For modders: Removed the `notnull` constraint from extension data.

## 1.1.1
Released 28 December 2023.

* For players: Fixed run history failing with custom characters, causing problems with mods like [Codex Helper](https://github.com/Shockah/Cobalt-Core-Mods/tree/master/CodexHelper).
* For modders: Fixed proxying of private types with public methods when using hooks.

## 1.1.0
Released 17 December 2023.

* For players: Fixed the Second Opinions card not working with modded characters.
* For players and modders: Added conditional action tooltips.
* For modders: Dev menu improvements.
* For modders: Wrapped action API, allowing the game to handle wrapped actions properly, for example flipping move actions correctly.
* For modders: Added hidden actions.
* For modders: Added card rendering hooks: modify how cards are rendered -- scale card text, move/scale/rotate actions.

## 1.0.1
Released 15 December 2023.

* Fixed a `StackOverflowException` after serializing the same type a lot (most likely through `Mutil.DeepCopy`).

## 1.0.0
Released 15 December 2023.

* Initial release.