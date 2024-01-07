using Nickel;
using System.Collections.Generic;

namespace Shockah.Dracula;

internal sealed class BiteCard : Card, IRegisterableCard
{
	public void Register(IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("Bite", new()
		{
			CardType = GetType(),
			Meta = new()
			{
				deck = ModEntry.Instance.DraculaDeck.Deck,
				rarity = Rarity.common,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Bite", "name"]).Localize
		});
	}

	private int Bleeding
		=> upgrade switch
		{
			Upgrade.A => 2,
			Upgrade.B => 1,
			_ => 1,
		};

	public override CardData GetData(State state)
		=> new()
		{
			cost = 1,
			infinite = upgrade == Upgrade.B
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AAttack
			{
				damage = 1,
				status = ModEntry.Instance.BleedingStatus.Status,
				statusAmount = Bleeding
			}
		];
}
