using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Natasha;

internal sealed class DeprogramCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.NatashaDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B],
				dontOffer = true,
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Deprogram.png"), StableSpr.cards_hacker).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Deprogram", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.A => new() { cost = 1, floppable = true, retain = true, temporary = true },
			Upgrade.B => new() { cost = 1, floppable = true, exhaust = true, temporary = true },
			_ => new() { cost = 1, floppable = true, temporary = true },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AStatus { targetPlayer = false, status = Status.rockFactory, statusAmount = 1, disabled = flipped },
				new ADummyAction(),
				new AStatus { targetPlayer = true, status = Status.rockFactory, statusAmount = 1, disabled = !flipped },
			],
			_ => [
				new AStatus { targetPlayer = false, status = Reprogram.DeprogrammedStatus.Status, statusAmount = 1, disabled = flipped },
				new ADummyAction(),
				new ASpawn { thing = new Asteroid(), disabled = !flipped },
			]
		};
}
