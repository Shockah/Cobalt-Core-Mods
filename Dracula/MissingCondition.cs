using System.Collections.Generic;
using System.Linq;
using Nickel;
using Shockah.Kokoro;

namespace Shockah.Dracula;

public sealed class MissingCondition : IKokoroApi.IV2.IConditionalApi.IBoolExpression
{
	public required Deck Deck;
	public bool Missing = true;

	public bool GetValue(State state, Combat combat)
	{
		if (state.characters.Any(character => character.deckType != Deck))
			return Missing;
		return state.CharacterIsMissing(Deck) == Missing;
	}

	public string GetTooltipDescription(State state, Combat? combat)
	{
		if (ModEntry.Instance.Helper.Content.Characters.V2.LookupByDeck(Deck) is not { } characterEntry)
			return ModEntry.Instance.Localizations.Localize(["condition", "missing", Missing ? "missing" : "present", "generic"]);

		if (Missing)
			return ModEntry.Instance.Localizations.Localize(["condition", "missing", "missing", "specific"], new { Status = Loc.T($"status.{characterEntry.MissingStatus.Status}.name") });
		else
			return ModEntry.Instance.Localizations.Localize(["condition", "missing", "present", "specific"], new { Character = $"<c={DB.decks[Deck].color}>{Character.GetDisplayName(Deck, state)}</c>" });
	}

	public void Render(G g, ref Vec position, bool isDisabled, bool dontRender)
	{
		if (!dontRender)
			Draw.Sprite(
				(Missing ? ModEntry.Instance.MissingIcon : ModEntry.Instance.PresentIcon).Sprite,
				position.x,
				position.y,
				color: isDisabled ? Colors.disabledIconTint : Colors.white
			);
		position.x += 8;
	}

	public IEnumerable<Tooltip> OverrideConditionalTooltip(State state, Combat? combat, Tooltip defaultTooltip, string defaultTooltipDescription)
		=> [
			new GlossaryTooltip($"AConditional::{ModEntry.Instance.Package.Manifest.UniqueName}::MissingCondition::Missing={Missing}")
			{
				Icon = (Missing ? ModEntry.Instance.MissingIcon : ModEntry.Instance.PresentIcon).Sprite,
				TitleColor = Colors.action,
				Title = ModEntry.Instance.Localizations.Localize(["condition", "missing", Missing ? "missing" : "present", "title"]),
				Description = defaultTooltipDescription,
			},
		];
}