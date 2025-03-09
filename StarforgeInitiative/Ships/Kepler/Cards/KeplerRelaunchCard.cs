using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.StarforgeInitiative;

internal sealed class KeplerRelaunchCard : Card, IRegisterable
{
	public StuffBase Thing = new FakeDrone();
	
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Kepler/Card/Relaunch.png"), StableSpr.cards_GoatDrone).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Kepler", "card", "Relaunch", "name"]).Localize,
		});
	}
	
	public override CardData GetData(State state)
		=> new() { cost = 1, floppable = true, singleUse = true, temporary = true, retain = true };

	public override List<CardAction> GetActions(State s, Combat c)
	{
		var thingBackwards = Mutil.DeepCopy(Thing);
		thingBackwards.targetPlayer = !thingBackwards.targetPlayer;
		return [
			new ASpawn { thing = Thing, disabled = flipped },
			new ASpawn { thing = thingBackwards, disabled = !flipped },
		];
	}
}