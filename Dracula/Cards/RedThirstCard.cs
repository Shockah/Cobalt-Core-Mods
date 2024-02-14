using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class RedThirstCard : Card, IDraculaCard
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("RedThirst", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DraculaDeck.Deck,
				rarity = Rarity.rare,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = StableSpr.cards_ExtraBattery,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "RedThirst", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = 0,
			exhaust = true,
			retain = upgrade == Upgrade.A,
			buoyant = upgrade == Upgrade.A
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AStatus
				{
					targetPlayer = true,
					status = Status.energyNextTurn,
					statusAmount = 2
				},
				new AStatus
				{
					targetPlayer = true,
					status = Status.drawNextTurn,
					statusAmount = 1
				},
				new AStatus
				{
					targetPlayer = false,
					status = ModEntry.Instance.BloodMirrorStatus.Status,
					statusAmount = 1
				}
			],
			_ => [
				new AEnergy
				{
					changeAmount = 2
				},
				new ADrawCard
				{
					count = 1
				},
				new AStatus
				{
					targetPlayer = false,
					status = ModEntry.Instance.BloodMirrorStatus.Status,
					statusAmount = 1
				}
			]
		};
}
