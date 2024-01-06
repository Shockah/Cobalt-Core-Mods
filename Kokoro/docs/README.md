# Kokoro

Kokoro is a utility/library mod - a little bit for players, and a lot for modders.

Game issues fixed by the mod:
* Second Opinions card not working with modded characters.
* Scrolling in the Artifacts section of the Codex.
* X actions not fading out properly when inactive (no vanilla cards do that though, only modded).
* Statuses sometimes not fitting between Evade buttons.
* Run history failing with custom characters.

Modder features:
* Dev menu now shows card/artifact/status names.
* Dev menu search for cards/artifacts/statuses is now case-insensitive.
* The Cards tab of the dev menu now allows choosing the card upgrade.
* The Cards tab deck filtering now includes modded decks.

APIs available to modders:
* **Pre-made custom actions**: Immediately exhaust all cards in hand; play a specific card from anywhere; play random cards from anywhere.
* **Wrapped action API**: Register wrapped actions, allowing the game to handle them properly, for example flipping move actions correctly.
* **Conditional actions**: Actions based on statuses, cards in hand, X values, but also allows creating custom conditions.
* **Resource cost actions**: Actions which only trigger after removing some resource -- a status (on the player or enemy), energy, etc.
* **Continue/Stop actions**: Actions which stop other actions from triggering.
* **Hidden actions**: Actions which do not display on the card, and yet execute their effects normally.
* **Vanilla action extensions**: outgoing X assignments, energy X assignments, energy modification actions.
* **Extension data**: Allows storing and persisting arbitrary data on any game objects, including but not limited to `Combat`, `State` or `StuffBase`.
* **API proxy extensions**: Allows proxying any object to any interface, which for example can be used to simulate custom artifact hooks.
* **Card rendering hooks**: Modify how cards are rendered -- scale card text, move/scale/rotate actions.
* **Status rendering hooks**: Show/hide statuses anytime, render a status as bars (like Shards), customize status bar rendering, override status tooltips.
* **Status logic hooks**: Control status state modification -- freeze a status, clamp it to certain values, make a status not be affected by Boost, etc.
* **Artifact icon rendering hook**: Allows custom overlays other than numbers.
* **Evade/Droneshift hooks**: Override if evade/droneshift is possible or add alternative costs. Do actions after evade/droneshift.
* **Oxidation status**: 7 Oxidation turns into 1 Corrode.
* **Worm status**: Cancels enemy intents at random on turn start.
* **Scorching midrow object status**: Scorching midrow objects take damage each turn.
* Some other generic helpers.

## See also
* [Latest release](https://github.com/Shockah/Cobalt-Core-Mods/releases/tag/release%2Fkokoro-1.2.0)
* [Release notes](release-notes.md)