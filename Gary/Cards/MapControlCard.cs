using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.Gary;

public sealed class MapControlCard : Card, IRegisterable, IHasCustomCardTraits
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var entry = helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.GaryDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/MapControl.png"), StableSpr.cards_GoatDrone).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "MapControl", "name"]).Localize,
		});
		
		ModEntry.Instance.KokoroApi.Finite.SetBaseFiniteUses(entry.UniqueName, 3);
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.B => new() { cost = 1 },
			_ => new() { cost = 1, flippable = true },
		};

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> new HashSet<ICardTraitEntry> { ModEntry.Instance.KokoroApi.Finite.Trait };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new ASpawn { thing = new Asteroid() },
				new AStatus { targetPlayer = true, status = Status.droneShift, statusAmount = 1 },
			],
			Upgrade.A => [
				new AStatus { targetPlayer = true, status = Stack.ApmStatus.Status, statusAmount = 1 },
				new ASpawn { thing = new Asteroid() },
				new ADroneMove { dir = 1 },
			],
			_ => [
				new ASpawn { thing = new Asteroid() },
				new ADroneMove { dir = 1 },
			],
		};
}