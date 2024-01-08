using Nickel;
using System.Collections.Generic;

namespace Shockah.Dracula;

internal sealed class SecretRestorativeCard : SecretCard, IDraculaCard
{
	public override void Register(IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("Secret.Restorative", new()
		{
			CardType = GetType(),
			Meta = new()
			{
				deck = ModEntry.Instance.DraculaDeck.Deck,
				rarity = Rarity.common,
				upgradesTo = [Upgrade.A],
				dontOffer = true
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Secret", "Restorative", "name"]).Localize
		});
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AStatus
			{
				targetPlayer = true,
				status = ModEntry.Instance.TransfusionStatus.Status,
				statusAmount = 2
			}
		];
}
