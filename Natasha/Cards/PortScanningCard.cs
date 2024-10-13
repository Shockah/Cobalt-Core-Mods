using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Natasha;

internal sealed class PortScanningCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/PortScanning.png"), StableSpr.cards_WeakenHull).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "PortScanning", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.A => new() { cost = 1, infinite = true },
			_ => new() { cost = 1 }
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				ModEntry.Instance.KokoroApi.Actions.MakeSequenceAction(uuid, 1, 3, new AAttack { damage = GetDmg(s, 1) }),
				ModEntry.Instance.KokoroApi.Actions.MakeSequenceAction(uuid, 2, 3, new AAttack { damage = GetDmg(s, 2) }),
				ModEntry.Instance.KokoroApi.Actions.MakeSequenceAction(uuid, 3, 3, new AAttack { damage = GetDmg(s, 2), brittle = true }),
			],
			_ => [
				ModEntry.Instance.KokoroApi.Actions.MakeSequenceAction(uuid, 1, 3, new AAttack { damage = GetDmg(s, 1) }),
				ModEntry.Instance.KokoroApi.Actions.MakeSequenceAction(uuid, 2, 3, new AAttack { damage = GetDmg(s, 2) }),
				ModEntry.Instance.KokoroApi.Actions.MakeSequenceAction(uuid, 3, 3, new AAttack { damage = GetDmg(s, 2), weaken = true }),
			]
		};
}
