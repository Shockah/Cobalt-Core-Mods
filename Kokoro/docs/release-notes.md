[← back to readme](README.md)

# Release notes

## Future release

* For players: Fixed the Second Opinions card not working with modded characters.
* For players and modders: Added conditional action tooltips.
* For modders: Wrapped action API, allowing the game to handle wrapped actions properly, for example flipping move actions correctly.
* For modders: Added hidden actions.
* For modders: Added card rendering hooks: modify how cards are rendered -- scale card text, move/scale/rotate actions.

## 1.0.1
Released 15 December 2023.

* Fixed a `StackOverflowException` after serializing the same type a lot (most likely through `Mutil.DeepCopy`).

## 1.0.0
Released 15 December 2023.

* Initial release.