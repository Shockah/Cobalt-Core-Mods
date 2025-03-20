using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.StarforgeInitiative;

internal sealed class BreadnaughtBasicWeakenCard : CannonColorless, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = Deck.colorless,
				rarity = Rarity.common,
				upgradesTo = [Upgrade.A, Upgrade.B],
				dontOffer = true,
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Breadnaught/Card/BasicWeaken.png"), StableSpr.cards_WeakenHull).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Breadnaught", "card", "BasicWeaken", "name"]).Localize,
		});
	}
	
	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.A => new() { cost = 0, exhaust = true, artTint = "ff3366" },
			Upgrade.B => new() { cost = 2, exhaust = true, artTint = "ff3366" },
			_ => new() { cost = 1, exhaust = true, artTint = "ff3366" },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AAttack { damage = GetDmg(s, 0), weaken = true },
				new AStatus { targetPlayer = true, status = Status.energyLessNextTurn, statusAmount = 1 },
			],
			Upgrade.B => [
				new AAttack { damage = GetDmg(s, 2), weaken = true, moveEnemy = -1 },
				new AStatus { targetPlayer = true, status = Status.energyLessNextTurn, statusAmount = 2 },
			],
			_ => [
				new AAttack { damage = GetDmg(s, 0), weaken = true },
				new AStatus { targetPlayer = true, status = Status.energyLessNextTurn, statusAmount = 1 },
			],
		};
}