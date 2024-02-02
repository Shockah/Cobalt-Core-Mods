using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Johnson;

internal sealed class Quarter3Card : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/Quarters/3.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Quarter3", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			artTint = "FFFFFF",
			cost = 2,
			temporary = true,
			exhaust = true,
			description = ModEntry.Instance.Localizations.Localize(["card", "Quarter3", "description", upgrade.ToString()])
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AAddCard
				{
					destination = CardDestination.Deck,
					card = new DeadlineCard()
				},
				new AAddCard
				{
					destination = CardDestination.Discard,
					card = new DeadlineCard(),
					omitFromTooltips = true
				}
			],
			_ => [
				new AAddCard
				{
					destination = CardDestination.Deck,
					insertRandomly = upgrade != Upgrade.A,
					card = new DeadlineCard()
				}
			]
		};
}
