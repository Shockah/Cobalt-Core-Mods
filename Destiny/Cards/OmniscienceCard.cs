using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Destiny;

public sealed class OmniscienceCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Omniscience.png"), StableSpr.cards_dizzy).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Omniscience", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> new() { cost = 1, exhaust = true, art = Enchanted.GetCardArt(this), artTint = "ffffff" };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AStatus { targetPlayer = true, status = Status.maxShard, statusAmount = 1 },
				new AStatus { targetPlayer = true, status = MagicFind.MagicFindStatus.Status, statusAmount = 5 },
			],
			Upgrade.B => [
				new AStatus { targetPlayer = true, status = Status.shard, statusAmount = 2 },
				new AStatus { targetPlayer = true, status = MagicFind.MagicFindStatus.Status, statusAmount = 3 },
			],
			_ => [
				new AStatus { targetPlayer = true, status = MagicFind.MagicFindStatus.Status, statusAmount = 5 },
			],
		};
}