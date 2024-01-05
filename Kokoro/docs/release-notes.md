[← back to readme](README.md)

# Release notes

## Upcoming version

* Fixed conditional actions having wrong tooltips if used to check if the player just has a status at all.
* For players and modders: Fixed `APlaySpecificCardFromAnywhere` (and in turn `APlayRandomCardsFromAnywhere`) getting into an endless loop if the hand ends up being full in between cards being played (apparent in the Soggins' Do Something! card).
* For modders: Added `IStatusLogicHook.OnStatusTurnTrigger` and `IStatusLogicHook.HandleStatusTurnAutoStep` APIs.
* For modders: Added resource cost action APIs.
* For modders: Added continue/stop action APIs
* For modders: Added an API to make outgoing X assignment actions.

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