using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.Gary;

public sealed class MatryoshkaCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Matryoshka.png"), StableSpr.cards_GoatDrone).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Matryoshka", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> new() { cost = 2, exhaust = true };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new ASpawn { thing = new Asteroid() }.SetStacked(),
				new ASpawn { thing = new Asteroid() }.SetStacked(),
				new ASpawn { thing = new RepairKit() }.SetStacked(),
			],
			Upgrade.A => [
				new ASpawn { thing = new RepairKit() }.SetStacked(),
				new ASpawn { thing = new Asteroid() }.SetStacked(),
			],
			_ => [
				new ASpawn { thing = new RepairKit() }.SetStacked(),
				new ASpawn { thing = new Asteroid() }.SetStacked(),
				new ASpawn { thing = new Asteroid() }.SetStacked(),
			],
		};
}