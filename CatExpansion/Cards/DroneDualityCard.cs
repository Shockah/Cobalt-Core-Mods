using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.CatExpansion;

public sealed class DroneDualityCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = Deck.colorless,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/DroneDuality.png"), StableSpr.cards_GoatDrone).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "DroneDuality", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.A => new() { cost = 1, exhaust = true, floppable = true, art = flipped ? StableSpr.cards_Adaptability_Bottom : StableSpr.cards_Adaptability_Top },
			Upgrade.B => new() { cost = 1, exhaust = true },
			_ => new() { cost = 1, exhaust = true, floppable = true, art = flipped ? StableSpr.cards_Adaptability_Bottom : StableSpr.cards_Adaptability_Top },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new ASpawn { thing = new AttackDrone { targetPlayer = false, bubbleShield = true }, disabled = flipped },
				new AStatus { targetPlayer = true, status = Status.droneShift, statusAmount = 1, disabled = flipped },
				new ADummyAction(),
				new ASpawn { thing = new ShieldDrone { targetPlayer = true, bubbleShield = true }, disabled = !flipped },
				new AStatus { targetPlayer = true, status = Status.droneShift, statusAmount = 1, disabled = !flipped },
			],
			Upgrade.B => [
				new ASpawn { thing = new AttackDrone { targetPlayer = false }, offset = -1 },
				new ASpawn { thing = new ShieldDrone { targetPlayer = true } },
				new AStatus { targetPlayer = true, status = Status.droneShift, statusAmount = 1 },
			],
			_ => [
				new ASpawn { thing = new AttackDrone { targetPlayer = false }, disabled = flipped },
				new AStatus { targetPlayer = true, status = Status.droneShift, statusAmount = 1, disabled = flipped },
				new ADummyAction(),
				new ASpawn { thing = new ShieldDrone { targetPlayer = true }, disabled = !flipped },
				new AStatus { targetPlayer = true, status = Status.droneShift, statusAmount = 1, disabled = !flipped },
			],
		};
}