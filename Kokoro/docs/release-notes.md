[← back to readme](README.md)

# Release notes

## Upcoming release

* `EvadeHook`/`DroneShiftHook` APIs now allow disabling other elements just for the rendering or actual action parts.

## 2.2.0
Released 12 January 2025.

* Kokoro now replaces all `shardcost` actions on the fly with its own action costs.
* Extended the action costs system, allowing hooks to modify costs on the fly and add additional resource providers.
* Added a new action info API.
* Added a new API to visually display card modifications mid-combat.

## 2.1.1
Released 8 January 2025.

* Improved performance of the Action Costs system.

## 2.1.0
Released 17 December 2024.

* `MultiCardBrowse` now preserves the order of selected cards.
* Added an option to manually set the cards selected by `MultiCardBrowse`.
* Added missing implementations for some methods of the Finite card trait API.
* Added vanilla shard cost icons to Kokoro's action cost system.

## 2.0.4
Released 9 December 2024.

* Fixed `EnergyAsStatus` not working and having incorrect tooltips.

## 2.0.3
Released 6 December 2024.

* Fixed the Finite trait that now instead of not working, was crashing the game.

## 2.0.2
Released 6 December 2024.

* Fixed the Finite trait not working.

## 2.0.1
Released 5 December 2024.

* Fixed status tooltip overrides not applying.
* Fixed a potential issue with hook arguments during nested calls.

## 2.0.0
Released 4 December 2024.

* Refactored and implemented a new version (V2) of the API.
* [V2] Implemented the Finite card trait.
* [V2] Incorporated Bloch's `MultiCardBrowse`.
* [V2] Incorporated Bloch's `OnDiscard`.
* [V2] Incorporated Bloch's `OnTurnEnd`.
* [V2] Incorporated Bloch's `Spontaneous`.
* [V2] Incorporated Johnson's Temporary Upgrades.
* [V2] Incorporated Natasha's `TimesPlayed`.
* [V2] Incorporated Natasha's `Sequence`.
* [V2] Incorporated Natasha's `Limited`.
* [V1] Added an extra required method to the `ICustomCardBrowseSource`.
* [V1] Removed `MidrowScorching`.
* [V1] Removed `WormStatus`.
* [V1] `EvadeHook`/`DroneShiftHook` APIs are no longer usable in this version. If you are using them in your mods, please switch to the V2 version.

## 1.12.1
Released 25 August 2024.

* The `RegisterTypeForExtensionData` method is no longer required, and no longer does anything.

## 1.12.0
Released 20 August 2024.

* Updated for an upcoming Nickel release.
* Added `ICustomCardBrowseSource`.

## 1.11.3
Released 17 July 2024.

* Updated for Cobalt Core 1.1.2.
* General optimizations.

## 1.11.2
Released 14 July 2024.

* The Oxidation status now flashes when about to turn into Corrode.
* Switched to using Nickel's newly exposed proxy manager, potentially improving performance and compatibility.

## 1.11.1
Released 12 July 2024.

* Fixed the mod being completely broken on clean install.

## 1.11.0
Released 11 July 2024.

* Removed debug menu patches that are now part of vanilla (which stops Kokoro from erroring).
* Added a proper tooltip for a `HasStatus` conditional description for enemies.
* Added additional conditional description styles: `EnemyState`, `EnemyPossession`, `EnemyPossessionComparison` and `ToEnemyPossessionComparison`.

## 1.10.2
Released 29 June 2024.

* Fixed Droneshift being usable with no midrow objects.

## 1.10.1
Released 14 June 2024.

* Fixed the compact font being scaled out of proportions when currently using the non-pixel font.

## 1.10.0
Released 2 June 2024.

* Added an API to override conditional action tooltips.
* Fixed not being able to force the droneshift buttons to appear if needed.

## 1.9.0
Released 31 May 2024.

* Added Temp Shield Next Turn and Shield Next Turn statuses.
* Changed Evade/Droneshift hook priorities a bit.

## 1.8.1
Released 5 May 2024.

* Fixed Redraw buttons appearing on enemy turns and on cards not in hand.

## 1.8.0
Released 4 May 2024.

* Added the Redraw status.
* Added a spoofed action API.

## 1.7.1
Released 23 April 2024.

* Extended available card text width by 1 pixel for modded cards.

## 1.7.0
Released 23 April 2024.

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