using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Johnson;

internal sealed class OvertimeCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/Overtime.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Overtime", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			artTint = "FFFFFF",
			cost = upgrade == Upgrade.B ? 3 : 2,
			exhaust = upgrade != Upgrade.B
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new ADrawCard
				{
					count = 2
				},
				new ADiscountHand
				{
					Amount = -1
				}
			],
			_ => [
				new ADiscountHand
				{
					Amount = -1
				}
			],
		};
}
