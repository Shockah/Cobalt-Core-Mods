[← back to readme](README.md)

# Release notes

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