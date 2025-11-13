[← back to readme](README.md)

# Release notes

## 1.8.3
Released 13 November 2025.

* Removed Perfect Shield from the Energy Condenser (Dizzy-Peri duo) artifact's tooltip, as it is no longer relevant.
* Shortened the tooltip header of duo artifacts, making most (all?) of them fit in one line.
* Added Custom Run Options integration.

## 1.8.2
Released 14 September 2025.

* Changed when the Evaporator (Books-Max duo) artifact triggers.
* Fixed duo artifacts being offered right away, if the character has a starter artifact.

## 1.8.1
Released 2 August 2025.

* Fixed the new Boot Sequence upside option crashing if no duos are available.

## 1.8.0
Released 29 July 2025.

* Added a new Boot Sequence upside option.
* Fixed the Quantum Sanctuary (CAT-Dizzy duo) artifact not reducing max shield as advertised.
* Fixed CAT duo artifacts appearing in the generic artifacts section in the Codex.

## 1.7.0
Released 29 July 2025.

* Fixed for Cobalt Core 1.2.5.

## 1.6.0
Released 22 June 2025.

* Reworked the Energy Condenser (Dizzy-Peri duo) artifact.

## 1.5.1
Released 1 May 2025.

* Slowed down duo artifact pulsing color animation during offerings.
* Switched duo artifact offerings from checking for non-boss offerings to specifically common offerings, which fixes some potential issues with (upcoming) mods.
* Improved wording of some artifacts.

## 1.5.0
Released 25 March 2025.

* Changed the Shardblade Infuser (Books-Peri duo) artifact.
* The Quantum Sanctuary (CAT-Dizzy duo) artifact now leaves you at 1 max shield (up from 0).
* The Corrosive Payload (Dizzy-Isaac duo) artifact now applies 2 Oxidation (up from 1).
* Fixed some artifact tooltips having an unneeded divider.
* Improved wording of some artifacts.
* Small optimization of artifact rewards.

## 1.4.0
Released 16 March 2025.

* Added an option to change how duo artifacts are offered, and changed the default mode: duo artifacts now only get offered once immediately after satisfying their prerequisites, and if not picked, become part of the Common artifact pool for that run. Any other limitations still apply.
* Clarified how the Frozen Control Rods (Dizzy-Drake duo) artifact works, and changed its implementation to be potentially more compatible with other mods.

## 1.3.1
Released 3 March 2025.

* Fixed the Combat Spreadsheets (Max-Peri duo) artifact crashing when playing cards from outside of your hand via various means.

## 1.3.0
Released 22 February 2025.

* Replaced the Trojan Drive (Drake-Max duo) artifact with CO2 Fire Extinguisher.
* The Gashapon.EXE (Max-CAT duo) artifact can now give you any positive status from any card in the game (instead of a predefined list of vanilla statuses).
* The Dynamo card from the Dynamo (Dizzy-Max duo) artifact can no longer be played if you can't afford its status cost.
* Fixed the Energy Condenser (Dizzy-Peri duo) triggering when enemies gain shield.
* Improved the timing of duo artifact pulsing all around.
* Improved Energy Condenser (Dizzy-Peri duo) wording.

## 1.2.2
Released 20 February 2025.

* Fixed the Emergency Box (Dizzy-Riggs duo) granting evade when the enemy loses any shield (and the player is at 0 shield).
* Fixed tooltips potentially not being scrollable.

## 1.2.1
Released 12 January 2025.

* The Aegis Transmuter (Books-Dizzy duo) artifact now uses Kokoro action cost functionality, making it work with more mechanics and cards.

## 1.2.0
Released 4 December 2024.

* Updated to latest Nickel and Kokoro.

## 1.1.4
Released 10 August 2024.

* The Combat Spreadsheets (Max-Peri duo) artifact got its effects swapped (now the leftmost attack fires an extra shot, while the rightmost attack deals more damage).
* Fixed CAT duo artifacts never being offered.
* Fixed the Combat Training Simulator (CAT-Peri duo) and Combat Spreadsheets (Max-Peri duo) artifacts not looking at wrapped actions for considering which cards are attacks.

## 1.1.3
Released 17 July 2024.

* General optimizations.

## 1.1.2
Released 6 July 2024.

* Fixed the wrong settings file being used.

## 1.1.1
Released 6 July 2024.

* Implemented mod setting profiles.

## 1.1.0
Released 5 July 2024.

* Added mod settings.
* Fixed Smart Launch System (the CAT-Isaac duo) breaking on multi-bay ships.
* Improved load time a bit.

## 1.0.9
Released 27 April 2024.

* Fixed not being able to scroll down past any duo artifact in the codex while playing on a gamepad.

## 1.0.8
Released 26 April 2024.

* Duo artifacts now appear in each character's section in the Codex, instead of their own section at the end.

## 1.0.7
Released 23 April 2024.

* Added possible duos to character tooltips on the new run screen.
* Fixed Drone Overclock (the Isaac-Drake duo) not working with Shield Drones as advertised.
* Fixed HARRIER Protocol (the Peri-Riggs duo) not limiting Evade as advertised.
* Improved some tooltip texts.

## 1.0.6
Released 17 March 2024.

* Fixed CAT duo artifacts never being available.
* Character tooltips now show the list of possible duo artifacts when a character is eligible for one.

## 1.0.5
Released 29 February 2024.

* Updated to the latest Kokoro.

## 1.0.4
Released 3 February 2024.

* Now comes with a `nickel.json` file.
* Compatibility with the [More Difficulty Options](https://github.com/TheJazMaster/MoreDifficulties) mod.
* Added complementary APIs to use with Nickel mods.

## 1.0.3
Released 6 January 2024.

* Updated to the latest Kokoro.

## 1.0.2
Released 17 December 2023.

* Fixed Shard tooltips not appearing correctly on the latest game beta.
* Duo artifact-specific cards are now internally using their own deck, which should not matter for most things. It does make them Frogproof when played with the [Soggins](https://github.com/Shockah/Cobalt-Core-Mods/tree/master/Soggins) mod, though.

## 1.0.1
Released 15 December 2023.

* Modded character duo artifacts now also include a duo combo tooltip.
* Fixed modded character duo artifacts not having a colored pulse on the Artifact Reward screen.

## 1.0.0
Released 15 December 2023.

* Initial release.