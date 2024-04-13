using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dyna;

internal sealed class BastionCard : Card, IRegisterable
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
			Art = StableSpr.cards_Flux,
			//Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/Bastion.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Bastion", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = upgrade == Upgrade.B ? 2 : 1,
			exhaust = true
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AStatus
				{
					targetPlayer = true,
					status = BastionManager.BastionStatus.Status,
					statusAmount = 1
				},
				new AStatus
				{
					targetPlayer = true,
					status = Status.shield,
					statusAmount = 1
				},
				new AStatus
				{
					targetPlayer = true,
					status = Status.tempShield,
					statusAmount = 1
				}
			],
			Upgrade.B => [
				new AStatus
				{
					targetPlayer = true,
					status = BastionManager.BastionStatus.Status,
					statusAmount = 2
				}
			],
			_ => [
				new AStatus
				{
					targetPlayer = true,
					status = BastionManager.BastionStatus.Status,
					statusAmount = 1
				}
			]
		};
}
