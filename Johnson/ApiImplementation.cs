using Nickel;

namespace Shockah.Johnson;

public sealed class ApiImplementation : IJohnsonApi
{
	public IDeckEntry JohnsonDeck
		=> ModEntry.Instance.JohnsonDeck;

	public IStatusEntry CrunchTimeStatus
		=> ModEntry.Instance.CrunchTimeStatus;

	public ICardTraitEntry StrengthenCardTrait
		=> StrengthenManager.Trait;

	public Tooltip GetStrengthenTooltip(int amount)
		=> new GlossaryTooltip($"cardtrait.{ModEntry.Instance.Package.Manifest.UniqueName}::Strengthen")
		{
			Icon = ModEntry.Instance.StrengthenIcon.Sprite,
			TitleColor = Colors.cardtrait,
			Title = ModEntry.Instance.Localizations.Localize(["cardTrait", "Strengthen", "name"]),
			Description = ModEntry.Instance.Localizations.Localize(["cardTrait", "Strengthen", "description"], new { Damage = amount })
		};

	public int GetStrengthen(Card card)
		=> card.GetStrengthen();

	public void SetStrengthen(Card card, int value)
		=> card.SetStrengthen(value);

	public void AddStrengthen(Card card, int value)
		=> card.AddStrengthen(value);

	public CardAction MakeStrengthenAction(int cardId, int amount)
		=> new AStrengthen { CardId = cardId, Amount = amount };

	public CardAction MakeStrengthenHandAction(int amount)
		=> new AStrengthenHand { Amount = amount };
}
