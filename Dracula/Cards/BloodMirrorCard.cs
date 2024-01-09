using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class BloodMirrorCard : Card, IDraculaCard
{
	public static void Register(IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("BloodMirror", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DraculaDeck.Deck,
				rarity = Rarity.common,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "BloodMirror", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = upgrade == Upgrade.B ? 3 : 2,
			exhaust = upgrade == Upgrade.B
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AStatus
				{
					targetPlayer = true,
					status = ModEntry.Instance.BloodMirrorStatus.Status,
					statusAmount = 1
				},
				new AHurt
				{
					targetPlayer = true,
					hurtAmount = 1
				},
				new AHeal
				{
					targetPlayer = true,
					healAmount = 1
				}
			],
			Upgrade.B => [
				new AStatus
				{
					targetPlayer = true,
					status = ModEntry.Instance.BloodMirrorStatus.Status,
					statusAmount = 3
				}
			],
			_ => [
				new AStatus
				{
					targetPlayer = true,
					status = ModEntry.Instance.BloodMirrorStatus.Status,
					statusAmount = 1
				}
			]
		};
}
