using Nickel;
using System.Collections.Generic;

namespace Shockah.Dracula;

internal sealed class ScreechCard : Card, IRegisterableCard
{
	public void Register(IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("Screech", new()
		{
			CardType = GetType(),
			Meta = new()
			{
				deck = ModEntry.Instance.DraculaDeck.Deck,
				rarity = Rarity.rare,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Screech", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = upgrade == Upgrade.B ? 3 : 2,
			retain = upgrade == Upgrade.A,
			exhaust = upgrade == Upgrade.B
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AStatus
			{
				targetPlayer = false,
				status = Status.overdrive,
				statusAmount = upgrade == Upgrade.B ? -2 : -1
			}
		];
}
