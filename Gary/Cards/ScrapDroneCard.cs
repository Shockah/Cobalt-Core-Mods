using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.Gary;

public sealed class ScrapDroneCard : Card, IRegisterable, IHasCustomCardTraits
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/ScrapDrone.png"), StableSpr.cards_goat).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "ScrapDrone", "name"]).Localize,
		});
		
		ModEntry.Instance.KokoroApi.Limited.SetBaseLimitedUses(entry.UniqueName, Upgrade.None, 3);
		ModEntry.Instance.KokoroApi.Limited.SetBaseLimitedUses(entry.UniqueName, Upgrade.A, 5);
		ModEntry.Instance.KokoroApi.Finite.SetBaseFiniteUses(entry.UniqueName, Upgrade.B, 2);
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.B => new() { cost = 1 },
			_ => new() { cost = 1, infinite = true },
		};

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> upgrade switch
		{
			Upgrade.B => new HashSet<ICardTraitEntry> { ModEntry.Instance.KokoroApi.Finite.Trait },
			_ => new HashSet<ICardTraitEntry> { ModEntry.Instance.KokoroApi.Limited.Trait },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AStatus { targetPlayer = true, status = CramManager.CramStatus.Status, statusAmount = 1 },
			new ASpawn { thing = new AttackDrone { targetPlayer = false } },
		];
}