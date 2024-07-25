using Nickel;

namespace Shockah.Johnson;

public sealed class ApiImplementation : IJohnsonApi
{
	public IDeckEntry JohnsonDeck
		=> ModEntry.Instance.JohnsonDeck;

	public IStatusEntry CrunchTimeStatus
		=> ModEntry.Instance.CrunchTimeStatus;

	public Tooltip TemporaryUpgradeTooltip
		=> new GlossaryTooltip($"cardtrait.{ModEntry.Instance.Package.Manifest.UniqueName}::TemporaryUpgrade")
		{
			Icon = ModEntry.Instance.TemporaryUpgradeIcon.Sprite,
			TitleColor = Colors.cardtrait,
			Title = ModEntry.Instance.Localizations.Localize(["cardTrait", "TemporaryUpgrade", "name"]),
			Description = ModEntry.Instance.Localizations.Localize(["cardTrait", "TemporaryUpgrade", "description"])
		};

	public bool IsTemporarilyUpgraded(Card card)
		=> card.IsTemporarilyUpgraded();

	public void SetTemporarilyUpgraded(Card card, bool value)
		=> card.SetTemporarilyUpgraded(value);

	public Tooltip GetStrengthenTooltip(int amount)
		=> new GlossaryTooltip($"cardtrait.{ModEntry.Instance.Package.Manifest.UniqueName}::Strengthen")
		{
			Icon = ModEntry.Instance.TemporaryUpgradeIcon.Sprite,
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
