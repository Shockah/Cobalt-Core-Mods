using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Johnson;

internal sealed class Quarter2Card : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.JohnsonDeck.Deck,
				rarity = ModEntry.GetCardRarity(typeof(Quarter1Card)),
				upgradesTo = [Upgrade.A, Upgrade.B],
				dontOffer = true
			},
			Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/Quarters/2.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Quarter2", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			artTint = "FFFFFF",
			cost = 1,
			temporary = true,
			exhaust = true,
			description = ModEntry.Instance.Localizations.Localize(["card", "Quarter2", "description", upgrade.ToString()])
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AAddCard
				{
					destination = CardDestination.Deck,
					card = new Quarter3Card()
				},
				new AAddCard
				{
					destination = CardDestination.Discard,
					card = new Quarter3Card(),
					omitFromTooltips = true
				}
			],
			_ => [
				new AAddCard
				{
					destination = CardDestination.Deck,
					insertRandomly = upgrade != Upgrade.A,
					card = new Quarter3Card()
				}
			]
		};
}
