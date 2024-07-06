using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dyna;

internal sealed class ClearAPathCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/ClearAPath.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "ClearAPath", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = 1,
			exhaust = upgrade == Upgrade.B
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AAttack
			{
				damage = GetDmg(s, upgrade == Upgrade.A ? 1 : 0)
			},
			new AStatus
			{
				targetPlayer = true,
				status = Status.evade,
				statusAmount = upgrade == Upgrade.B ? 2 : 1
			},
			new AStatus
			{
				targetPlayer = true,
				status = Status.droneShift,
				statusAmount = upgrade == Upgrade.B ? 2 : 1
			}
		];
}
