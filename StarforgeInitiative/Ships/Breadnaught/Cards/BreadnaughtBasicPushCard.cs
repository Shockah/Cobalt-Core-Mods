using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.StarforgeInitiative;

internal sealed class BreadnaughtBasicPushCard : CannonColorless, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Breadnaught/Card/BasicPush.png"), StableSpr.cards_ShiftShot).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Breadnaught", "card", "BasicPush", "name"]).Localize,
		});
	}
	
	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.A => new() { cost = 2, temporary = true, flippable = true, artTint = "ff3366" },
			_ => new() { cost = 2, temporary = true, artTint = "ff3366" },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AAttack { damage = GetDmg(s, 2), moveEnemy = -2 },
			],
			_ => [
				new AAttack { damage = GetDmg(s, 2), moveEnemy = -1 },
			],
		};
}