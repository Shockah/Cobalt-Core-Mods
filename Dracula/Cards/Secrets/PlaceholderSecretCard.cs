using Nickel;

namespace Shockah.Dracula;

internal sealed class PlaceholderSecretCard : SecretCard, IDraculaCard
{
	public override void Register(IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("Secret.Placeholder", new()
		{
			CardType = GetType(),
			Meta = new()
			{
				deck = ModEntry.Instance.DraculaDeck.Deck,
				rarity = Rarity.common,
				upgradesTo = [Upgrade.A],
				dontOffer = true,
				unreleased = true
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Secret", "Placeholder", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
	{
		var data = base.GetData(state);
		data.description = ModEntry.Instance.Localizations.Localize(["card", "Secret", "Placeholder", "description"]);
		return data;
	}
}
