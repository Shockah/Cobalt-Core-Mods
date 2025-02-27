using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.CatExpansion;

public sealed class StaticShotCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/StaticShot.png"), StableSpr.cards_StunCharge).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "StaticShot", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> new() { cost = 1 };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AAttack { damage = GetDmg(s, 2) },
				new AStatus { targetPlayer = true, status = Status.stunCharge, statusAmount = 1 },
			],
			Upgrade.B => [
				new AAttack { damage = GetDmg(s, 1) },
				new AStatus { targetPlayer = true, status = Status.stunCharge, statusAmount = 2 },
			],
			_ => [
				new AAttack { damage = GetDmg(s, 1) },
				new AStatus { targetPlayer = true, status = Status.stunCharge, statusAmount = 1 },
			],
		};
}