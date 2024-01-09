using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class DispersionCard : Card, IDraculaCard
{
	public static void Register(IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("Dispersion", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
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
				new AStatus
				{
					targetPlayer = true,
					status = ModEntry.Instance.TransfusionStatus.Status,
					statusAmount = 3
				},
				new AStatus
				{
					targetPlayer = false,
					status = ModEntry.Instance.TransfusionStatus.Status,
					statusAmount = 3
				}
			],
			_ => [
				new AHurt
				{
					targetPlayer = true,
					hurtAmount = upgrade == Upgrade.A ? 2 : 1
				},
				new AStatus
				{
					targetPlayer = true,
					status = ModEntry.Instance.TransfusionStatus.Status,
					statusAmount = upgrade == Upgrade.A ? 4 : 2
				}
			]
		};
}
