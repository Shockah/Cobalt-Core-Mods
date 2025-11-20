using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.Gary;

public sealed class SteadyCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.GaryDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Steady.png"), StableSpr.cards_GoatDrone).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Steady", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.B => new() { cost = 1 },
			Upgrade.A => new() { cost = 0, floppable = true },
			_ => new() { cost = 1, floppable = true },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AStatus { targetPlayer = true, status = Stack.TetrisStatus.Status, statusAmount = 2 },
				new AStatus { targetPlayer = true, status = Stack.JengaStatus.Status, statusAmount = 1 },
			],
			_ => [
				new AStatus { targetPlayer = true, status = Stack.TetrisStatus.Status, statusAmount = 2, disabled = flipped },
				new ADummyAction(),
				new AStatus { targetPlayer = true, status = Stack.JengaStatus.Status, statusAmount = 1, disabled = !flipped },
			],
		};
}