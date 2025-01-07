using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Destiny;

public sealed class ResearchCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DestinyDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Research.png"), StableSpr.cards_dizzy).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Research", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> new() { cost = 1, exhaust = true };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new ADrawCard { count = 8 },
				new AStatus { targetPlayer = true, status = Status.shard, statusAmount = 3 },
			],
			Upgrade.B => [
				new ADrawCard { count = 4 },
				new AStatus { targetPlayer = true, status = Status.shard, statusAmount = 3 },
				new AStatus { targetPlayer = true, status = MagicFind.MagicFindStatus.Status, statusAmount = 2 },
			],
			_ => [
				new ADrawCard { count = 4 },
				new AStatus { targetPlayer = true, status = Status.shard, statusAmount = 3 },
			],
		};
}