using Nickel;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class PlaceholderSecretCard : SecretCard, IDraculaCard
{
	public static void Register(IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("Secret.Placeholder", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DraculaDeck.Deck,
				rarity = Rarity.common,
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
