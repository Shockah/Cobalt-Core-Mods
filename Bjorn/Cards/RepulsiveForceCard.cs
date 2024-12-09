using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bjorn;

public sealed class RepulsiveForceCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/RepulsiveForce.png"), StableSpr.cards_Dodge).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "RepulsiveForce", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> upgrade.Switch<CardData>(
			none: () => new() { cost = 1, flippable = true },
			a: () => new() { cost = 1, flippable = true, infinite = true },
			b: () => new() { cost = 1 }
		);

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				ModEntry.Instance.KokoroApi.Spontaneous.MakeAction(
					new AStatus { targetPlayer = true, status = EntanglementManager.EntanglementStatus.Status, statusAmount = 1 }
				).AsCardAction,
				ModEntry.Instance.KokoroApi.Spontaneous.MakeAction(
					new AMove { targetPlayer = true, isRandom = true, dir = 1 }
				).AsCardAction,
				new AStatus { targetPlayer = true, status = RelativityManager.RelativityStatus.Status, statusAmount = 1 },
			],
			_ => [
				ModEntry.Instance.KokoroApi.Spontaneous.MakeAction(
					new AStatus { targetPlayer = true, status = EntanglementManager.EntanglementStatus.Status, statusAmount = 1 }
				).AsCardAction,
				ModEntry.Instance.KokoroApi.Spontaneous.MakeAction(
					new AMove { targetPlayer = true, isRandom = true, dir = 1 }
				).AsCardAction,
				new AMove { targetPlayer = true, dir = 1 },
			]
		};
}
