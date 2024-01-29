using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Johnson;

internal sealed class BuyLowCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/BuyLow.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "BuyLow", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			artTint = "FFFFFF",
			cost = upgrade == Upgrade.B ? 1 : 0,
			description = ModEntry.Instance.Localizations.Localize(["card", "BuyLow", "description", upgrade.ToString()])
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AAddCard
			{
				destination = upgrade == Upgrade.B ? CardDestination.Deck : CardDestination.Hand,
				card = new BurnOutCard
				{
					discount = upgrade == Upgrade.B ? -1 : 0
				},
				amount = upgrade switch
				{
					Upgrade.A => 3,
					Upgrade.B => 4,
					_ => 2
				}
			}
		];
}
