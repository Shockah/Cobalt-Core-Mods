using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Johnson;

internal sealed class Quarter1Card : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/Quarters/1.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Quarter1", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			artTint = "FFFFFF",
			cost = 0,
			exhaust = true,
			description = ModEntry.Instance.Localizations.Localize(["card", "Quarter1", "description", upgrade.ToString()])
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AAddCard { destination = CardDestination.Deck, card = new Quarter2Card() },
				new AAddCard { destination = CardDestination.Discard, card = new Quarter2Card(), omitFromTooltips = true },
				new ADummyAction() { dialogueSelector = $".Played::{Key()}" },
			],
			_ => [
				new AAddCard { destination = CardDestination.Deck, insertRandomly = upgrade != Upgrade.A, card = new Quarter2Card() },
				new ADummyAction() { dialogueSelector = $".Played::{Key()}" },
			]
		};
}
