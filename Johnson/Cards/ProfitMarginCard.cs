using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Johnson;

internal sealed class ProfitMarginCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.JohnsonDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/ProfitMargin.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "ProfitMargin", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			artTint = "FFFFFF",
			cost = 1,
			exhaust = upgrade == Upgrade.B
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AStatus
				{
					targetPlayer = true,
					status = Status.tempShield,
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
					status = Status.temporaryCheap,
					statusAmount = 1
				}
			],
			Upgrade.B => [
				new AStatus
				{
					targetPlayer = true,
					status = Status.shield,
					statusAmount = 2
				},
				new AStatus
				{
					targetPlayer = true,
					status = Status.temporaryCheap,
					statusAmount = 2
				}
			],
			_ => [
				new AStatus
				{
					targetPlayer = true,
					status = Status.tempShield,
					statusAmount = 2
				},
				new AStatus
				{
					targetPlayer = true,
					status = Status.temporaryCheap,
					statusAmount = 1
				}
			]
		};
}
