[← back to readme](README.md)

# Release notes

## Upcoming release

* Added the `AttackLogic` feature.

## 2.15.1
Released 21 December 2025.

* Fixed Regain Hull Later and Lose Hull Later appearing seemingly out of nowhere.

## 2.15.0
Released 20 December 2025.

* Added the `TempHull` feature.
* Added the `Independent` feature.
* Added the feature of triggering statuses immediately (part of `StatusLogic`).

## 2.14.0
Released 7 December 2025.

* Added the `AllowCancel` property for `InPlaceCardUpgrade` and `TemporaryUpgrades` APIs.

## 2.13.0
Released 3 December 2025.

* Added the `OnExhaust` feature.
* Partially reimplemented Limited and Finite. They are no longer related to the Exhaust and Infinite traits.
* Improved the tooltips of Limited and Finite.
* Fixed "Times Played This Turn" not resetting under some circumstances.

## 2.12.2
Released 13 November 2025.

* Fixed some asset files not being included in the release.

## 2.12.1
Released 13 November 2025.

* Fixed Oxidation not applying Corrode.
* Updated visuals for Sequence actions.

## 2.12.0
Released 18 September 2025.

* Added the Temp Strafe status.
* Added `GetStatusesToCallTurnTriggerHooksFor` to the `StatusLogic` hooks.
* Turn trigger hooks now by default only get called for non-zero statuses, instead of all statuses in the game.

## 2.11.5
Released 16 September 2025.

* Improved performance by a bit.

## 2.11.4
Released 15 August 2025.

* Fixed cost actions crashing the game if you have a negative amount of the required resource.

## 2.11.3
Released 1 August 2025.

* Fixed custom card browse sources.

## 2.11.2
Released 27 June 2025.

* Fixed Perfect Shield and Engine Lock not being affected by Timestop.

## 2.11.1
Released 27 June 2025. Reverted.

* ~~Fixed Timestop not working when at only 1 stack of it.~~

## 2.11.0
Released 22 June 2025.

* Added an API to control the priority of turn start/end status handling.
* Improved visuals of the Fleeting and Limited card traits.
* Fixed Oxidation sometimes not turning into Corrode, depending on timing of its gain.
* Fixed `VariableHintTargetPlayer` API access name, leaving `VariableHintTargetPlayerTargetPlayer` as obsolete.

## 2.10.1
Released 8 May 2025.

* Fixed `PlayCardsFromAnywhere`-`ModifyCardsAnywhere` finishing before it actually did its thing (fixes Destiny's Unstable Magic card).

## 2.10.0
Released 1 May 2025.

* Added the Fleeting and Heavy card traits (previously implemented and used in the Louis and Marielle mods).
* Fixed Limited/Finite traits and Sequence actions sometimes getting garbled icons (related to screen shake).
* Improved tooltip wording of Impulsive, On Discard and On Turn End.

## 2.9.0
Released 25 April 2025.

* Renamed Spontaneous to Impulsive.
* Added a way to hide On Discard / On Turn End / Impulsive icons and/or tooltips.

## 2.8.1
Released 12 April 2025.

* Fixed extra sounds being played and delays whenever a turn ends.

## 2.8.0
Released 12 April 2025.

* Limited and Temp Upgrade card traits now render at 70% alpha, akin to Exhaust and Temporary card traits.
* Added Overdrive Next Turn, Underdrive, Pulsedrive and Minidrive statuses.

## 2.7.0
Released 30 March 2025.

* Added an API to get the Limited/Finite icon for a given amount.

## 2.6.0
Released 28 March 2025.

* Added temporary upgrade hooks.
* Added an API to specify a custom action for in-place card upgrades.
* Added an API to retrieve an action that would change the amount of a given action cost resource.

## 2.5.2
Released 25 March 2025.

* Fixed shard action costs with disabled actions attempting to pay for that cost even though the action should not happen.

## 2.5.1
Released 20 March 2025.

* Changed how Kokoro handles shard costs on cards, making it more compatible with other mods.

## 2.5.0
Released 15 March 2025.

* Added an API to copy and modify `IStatusInfoRenderer` arguments (to pass in to child renderers).
* Fixed the action cost API hook not being called on failed payments.

## 2.4.1
Released 10 March 2025.

* Fixed artifact-based Kokoro hooks not working.

## 2.4.0
Released 6 March 2025.

* Added an API to customize status info rendering.
* Deprecated `OverrideStatusRenderingAsBars`.
* Removed the action info interaction API.
* Limited 1 now only applies Exhaust while in combat.
* Finite 2+ now only applies Infinite while in combat.
* Fixed action cost and sequence actions not setting their actual actions' `whoDidThis` property.
* Fixed missing exhaust animations in some actions.
* Fixed mid-combat card offerings not animating properly.

## 2.3.1
Released 18 January 2025.

* Fixed the Redraw buttons sometimes appearing when another screen is currently on.

## 2.3.0
Released 17 January 2025.

* `EvadeHook`/`DroneShiftHook` APIs now allow disabling other elements just for the rendering or just for the actual action parts.
* Fixed several features sorting hooks in reverse order, causing various issues, like Timestop not working.

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