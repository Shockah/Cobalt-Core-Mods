using Nickel;
using System.Collections.Generic;

namespace Shockah.Dracula;

internal sealed class ClonedLeechCard : Card, IRegisterableCard
{
	public void Register(IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("Leech", new()
		{
			CardType = GetType(),
			Meta = new()
			{
				deck = ModEntry.Instance.DraculaDeck.Deck,
				rarity = Rarity.common,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Leech", "name"]).Localize
		});
	}

	private int Damage
		=> upgrade switch
		{
			Upgrade.A => 3,
			Upgrade.B => 2,
			_ => 2,
		};

	private int Heal
		=> upgrade switch
		{
			Upgrade.A => 2,
			Upgrade.B => 1,
			_ => 1,
		};

	public override CardData GetData(State state)
		=> new()
		{
			cost = 2,
			exhaust = upgrade != Upgrade.B
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AAttack
			{
				damage = Damage,
				piercing = true,
				stunEnemy = true
			},
			new AHeal
			{
				targetPlayer = true,
				healAmount = Heal
			}
		];
}
