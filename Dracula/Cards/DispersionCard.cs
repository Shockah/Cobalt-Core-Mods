using Nickel;
using System.Collections.Generic;

namespace Shockah.Dracula;

internal sealed class DispersionCard : Card, IDraculaCard
{
	public void Register(IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("Dispersion", new()
		{
			CardType = GetType(),
			Meta = new()
			{
				deck = ModEntry.Instance.DraculaDeck.Deck,
				rarity = Rarity.uncommon,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Dispersion", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = upgrade == Upgrade.B ? 1 : 0,
			exhaust = true
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AHurt
				{
					targetPlayer = true,
					hurtAmount = 2
				},
				new AHeal
				{
					targetPlayer = true,
					healAmount = 1
				},
				new AStatus
				{
					targetPlayer = true,
					status = ModEntry.Instance.TransfusionStatus.Status,
					statusAmount = 3
				}
			],
			_ => [
				new AHurt
				{
					targetPlayer = true,
					hurtAmount = upgrade == Upgrade.A ? 1 : 2
				},
				ModEntry.Instance.KokoroApi.Actions.MakeEnergyX(
					tooltipOverride: GetDataWithOverrides(s).cost
				),
				new AHeal
				{
					targetPlayer = true,
					healAmount = c.energy - GetDataWithOverrides(s).cost,
					xHint = 1
				},
				ModEntry.Instance.KokoroApi.Actions.MakeEnergy(new AStatus
				{
					targetPlayer = true,
					mode = AStatusMode.Set,
					statusAmount = upgrade == Upgrade.A ? 1 : 0
				})
			]
		};
}
