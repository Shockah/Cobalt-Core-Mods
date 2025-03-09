using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.StarforgeInitiative;

internal sealed class KeplerSwarmModeCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Kepler/Card/SwarmMode.png"), StableSpr.cards_Terminal).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Kepler", "card", "SwarmMode", "name"]).Localize,
		});
	}
	
	public override CardData GetData(State state)
		=> new()
		{
			cost = 1, retain = true, singleUse = true, temporary = true, flippable = true,
			description = ModEntry.Instance.Localizations.Localize(["ship", "Kepler", "card", "SwarmMode", "description"]),
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [new AActivateAllParts { partType = PType.missiles }];

	public override void OnFlip(G g)
	{
		base.OnFlip(g);
		if (g.state.route is not Combat combat)
			return;
		combat.QueueImmediate(new KeplerToggleBaysAction());
	}
}