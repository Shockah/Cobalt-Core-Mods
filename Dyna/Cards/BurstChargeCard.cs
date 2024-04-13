using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dyna;

internal sealed class BurstChargeCard : Card, IRegisterable
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
			//Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/BurstCharge.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "BurstCharge", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = upgrade switch
			{
				Upgrade.A => 2,
				Upgrade.B => 0,
				_ => 1
			}
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new FireChargeAction
				{
					Charge = new BurstCharge(),
					Offset = -1
				},
				new FireChargeAction
				{
					Charge = new BurstCharge(),
					Offset = 1
				}
			],
			_ => [
				new FireChargeAction
				{
					Charge = new BurstCharge()
				}
			]
		};
}
