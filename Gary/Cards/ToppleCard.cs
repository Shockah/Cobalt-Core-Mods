using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.Gary;

public sealed class ToppleCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.GaryDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Topple.png"), StableSpr.cards_SmallBoulder).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Topple", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.A => new() { cost = 0, floppable = true, retain = true, art = flipped ? StableSpr.cards_Adaptability_Bottom : StableSpr.cards_Adaptability_Top },
			_ => new() { cost = 0 },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new ASpawn { thing = new SpaceMine().SetWobbly() }.SetStacked(),
			],
			Upgrade.A => [
				new ASpawn { thing = new Asteroid().SetWobbly(), disabled = flipped }.SetStacked(),
				new ADummyAction(),
				new AStatus { targetPlayer = true, status = Stack.ApmStatus.Status, statusAmount = 1, disabled = !flipped },
			],
			_ => [
				new ASpawn { thing = new Asteroid().SetWobbly() }.SetStacked(),
			],
		};
}