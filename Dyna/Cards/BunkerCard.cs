using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dyna;

internal sealed class BunkerCard : Card, IRegisterable
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
			Art = StableSpr.cards_BigShield,
			//Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/Bunker.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Bunker", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = upgrade == Upgrade.B ? 0 : 2
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AStatus
			{
				targetPlayer = true,
				status = Status.shield,
				statusAmount = upgrade switch
				{
					Upgrade.A => 3,
					Upgrade.B => 1,
					_ => 2
				}
			},
			new AStatus
			{
				targetPlayer = true,
				status = Status.tempShield,
				statusAmount = 1
			},
			new AStatus
			{
				targetPlayer = true,
				status = upgrade == Upgrade.B ? Status.energyLessNextTurn : Status.energyNextTurn,
				statusAmount = 1
			}
		];
}
