using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.StarforgeInitiative;

internal sealed class KeplerToggleBayCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = KeplerShip.ShipDeck.Deck,
				rarity = Rarity.common,
				dontOffer = true,
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Kepler/Card/ToggleBay.png"), StableSpr.cards_Terminal).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Kepler", "card", "ToggleBay", "name"]).Localize,
		});
	}
	
	public override CardData GetData(State state)
		=> new()
		{
			cost = 0, retain = true, singleUse = true, temporary = true,
			description = ModEntry.Instance.Localizations.Localize(["ship", "Kepler", "card", "ToggleBay", "description"]),
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [new KeplerToggleBaysAction()];
}