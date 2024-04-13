using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dyna;

internal sealed class ConcussionChargeCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DynaDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = StableSpr.cards_GoatDrone,
			//Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/ConcussionCharge.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "ConcussionCharge", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = upgrade switch
			{
				Upgrade.A => 1,
				Upgrade.B => 3,
				_ => 2
			},
			exhaust = true
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new FireChargeAction
				{
					Charge = new ConcussionCharge()
				},
				new AMove
				{
					targetPlayer = true,
					dir = 1
				},
				new FireChargeAction
				{
					Charge = new ConcussionCharge()
				}
			],
			_ => [
				new FireChargeAction
				{
					Charge = new ConcussionCharge()
				}
			]
		};
}
