using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.CatExpansion;

public sealed class PriorityQueueCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/PriorityQueue.png"), StableSpr.cards_Overclock).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "PriorityQueue", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.A => new() { cost = 1, retain = true },
			Upgrade.B => new() { cost = 1 },
			_ => new() { cost = 1 },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AStatus { targetPlayer = true, status = Status.temporaryCheap, statusAmount = 1 },
				new AStatus { targetPlayer = true, status = Status.boost, statusAmount = 1 },
			],
			Upgrade.B => [
				new AStatus { targetPlayer = true, status = Status.temporaryCheap, statusAmount = 1 },
				new AStatus { targetPlayer = true, status = Status.boost, statusAmount = 1 },
				new ADrawCard { count = 1 },
			],
			_ => [
				new AStatus { targetPlayer = true, status = Status.temporaryCheap, statusAmount = 1 },
				new AStatus { targetPlayer = true, status = Status.boost, statusAmount = 1 },
			],
		};
}