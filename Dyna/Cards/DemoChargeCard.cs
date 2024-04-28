using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dyna;

internal sealed class DemoChargeCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/DemoCharge.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "DemoCharge", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = upgrade == Upgrade.B ? 0 : 1,
			flippable = upgrade == Upgrade.A,
			exhaust = upgrade == Upgrade.B
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new FireChargeAction
				{
					Charge = new DemoCharge()
				},
				new AMove
				{
					targetPlayer = true,
					dir = 1
				}
			],
			_ => [
				new FireChargeAction
				{
					Charge = new DemoCharge()
				}
			]
		};
}
