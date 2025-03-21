using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bloch;

internal sealed class DistressCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.BlochDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Distress.png"), StableSpr.cards_DiceRoll).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Distress", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.A => new() { cost = 0, flippable = true, infinite = true, retain = true },
			Upgrade.B => new() { cost = 0 },
			_ => new() { cost = 0, flippable = true, infinite = true },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AStatus { targetPlayer = true, status = AuraManager.VeilingStatus.Status, statusAmount = 1 },
				new DiscardSideAction { Amount = 2, Left = flipped },
			],
			Upgrade.B => [
				new AStatus { targetPlayer = true, status = AuraManager.IntensifyStatus.Status, statusAmount = 1 },
				new AStatus { targetPlayer = true, status = AuraManager.VeilingStatus.Status, statusAmount = 2 },
				new ADiscard()
			],
			_ => [
				new AStatus { targetPlayer = true, status = AuraManager.VeilingStatus.Status, statusAmount = 1 },
				new DiscardSideAction { Amount = 2, Left = flipped },
			]
		};
}
