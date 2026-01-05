using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.StarforgeInitiative;

internal sealed class BreadnaughtBasicMinigunCard : CannonColorless, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Breadnaught/Card/BasicMinigun.png"), StableSpr.cards_DrakeCannon).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Breadnaught", "card", "BasicMinigun", "name"]).Localize,
		});
	}
	
	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.B => new() { cost = 1, temporary = true, artTint = "ff3366" },
			_ => new() { cost = 2, temporary = true, artTint = "ff3366" },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AAttack { piercing = true, damage = GetDmg(s, 3), status = Status.heat, statusAmount = 2 },
			],
			Upgrade.B => [
				new AAttack { piercing = true, damage = GetDmg(s, 2), status = Status.heat, statusAmount = 1 },
			],
			_ => [
				new AAttack { piercing = true, damage = GetDmg(s, 2), status = Status.heat, statusAmount = 2 },
			],
		};
}