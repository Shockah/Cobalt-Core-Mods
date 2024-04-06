using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dyna;

internal sealed class PerkUpCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DynaDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			//Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/PerkUp.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "PerkUp", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = 0,
			exhaust = true,
			retain = true
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AVariableHint
				{
					status = Status.energyLessNextTurn
				},
				new AStatus
				{
					targetPlayer = true,
					status = Status.energyNextTurn,
					statusAmount = s.ship.Get(Status.energyLessNextTurn) * 2,
					xHint = 2
				},
				new AStatus
				{
					targetPlayer = true,
					status = Status.energyNextTurn,
					statusAmount = 1
				}
			],
			Upgrade.B => [
				new AVariableHint
				{
					status = Status.energyLessNextTurn
				},
				new AStatus
				{
					targetPlayer = true,
					status = Status.energyNextTurn,
					statusAmount = s.ship.Get(Status.energyLessNextTurn) * 2,
					xHint = 2
				},
				new AEnergy
				{
					changeAmount = 1
				}
			],
			_ => [
				new AVariableHint
				{
					status = Status.energyLessNextTurn
				},
				new AStatus
				{
					targetPlayer = true,
					status = Status.energyNextTurn,
					statusAmount = s.ship.Get(Status.energyLessNextTurn) * 2,
					xHint = 2
				}
			]
		};
}
