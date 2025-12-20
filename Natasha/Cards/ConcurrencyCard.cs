using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.Natasha;

internal sealed class ConcurrencyCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Concurrency.png"), StableSpr.cards_MultiShot).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Concurrency", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.B => new() { cost = 1 },
			Upgrade.A => new() { cost = 2 },
			_ => new() { cost = 3 },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AAttack { damage = GetDmg(s, 0) },
				new AAttack { damage = GetDmg(s, 1) },
				new AAttack { damage = GetDmg(s, 2) },
			],
			_ => [
				new AAttack { damage = GetDmg(s, 1) },
				new AAttack { damage = GetDmg(s, 2) },
				new AAttack { damage = GetDmg(s, 3) },
			]
		};
}
