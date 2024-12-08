using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Natasha;

internal sealed class HijackEnginesCard : Card, IRegisterable
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
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/HijackEngines.png"), StableSpr.cards_TrashFumes).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "HijackEngines", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.A => new() { cost = 1, flippable = true },
			_ => new() { cost = 2, flippable = true },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AStatus { targetPlayer = false, status = flipped ? Status.autododgeLeft : Status.autododgeRight, statusAmount = 1 },
				new AAttack { damage = GetDmg(s, 0) },
			],
			_ => [
				new AStatus { targetPlayer = false, status = flipped ? Status.autododgeLeft : Status.autododgeRight, statusAmount = 1 },
			]
		};
}
