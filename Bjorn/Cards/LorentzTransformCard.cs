using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bjorn;

public sealed class LorentzTransformCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.BjornDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/LorentzTransform.png"), StableSpr.cards_Inverter).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "LorentzTransform", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> upgrade.Switch<CardData>(
			none: () => new() { cost = 0 },
			a: () => new() { cost = 0 },
			b: () => new() { cost = 1 }
		);

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade.Switch<List<CardAction>>(
			none: () => [
				new AVariableHint { status = Status.evade, secondStatus = Status.droneShift },
				new AStatus { targetPlayer = true, status = RelativityManager.RelativityStatus.Status, statusAmount = s.ship.Get(Status.evade) + s.ship.Get(Status.droneShift), xHint = 1 },
				new AStatus { targetPlayer = true, mode = AStatusMode.Set, status = Status.evade, statusAmount = 0, timer = 0 },
				new AStatus { targetPlayer = true, mode = AStatusMode.Set, status = Status.droneShift, statusAmount = 0 },
			],
			a: () => [
				new AVariableHint { status = Status.evade, secondStatus = Status.droneShift },
				new AStatus { targetPlayer = true, status = RelativityManager.RelativityStatus.Status, statusAmount = s.ship.Get(Status.evade) + s.ship.Get(Status.droneShift), xHint = 1, timer = 0 },
				new AStatus { targetPlayer = true, status = RelativityManager.RelativityStatus.Status, statusAmount = 1 },
				new AStatus { targetPlayer = true, mode = AStatusMode.Set, status = Status.evade, statusAmount = 0, timer = 0 },
				new AStatus { targetPlayer = true, mode = AStatusMode.Set, status = Status.droneShift, statusAmount = 0 },
			],
			b: () => [
				new AStatus { targetPlayer = true, mode = AStatusMode.Set, status = RelativityManager.RelativityStatus.Status, statusAmount = 2 },
				new AStatus { targetPlayer = true, mode = AStatusMode.Set, status = Status.evade, statusAmount = 0, timer = 0 },
				new AStatus { targetPlayer = true, mode = AStatusMode.Set, status = Status.droneShift, statusAmount = 0 },
			]
		);
}