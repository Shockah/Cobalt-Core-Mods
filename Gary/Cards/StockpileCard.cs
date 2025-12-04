using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.Gary;

public sealed class StockpileCard : Card, IRegisterable, IHasCustomCardTraits
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Stockpile.png"), StableSpr.cards_Inverter).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Stockpile", "name"]).Localize,
		});
		
		ModEntry.Instance.KokoroApi.Limited.SetBaseLimitedUses(entry.UniqueName, Upgrade.B, 5);
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.B => new() { cost = 0, infinite = true },
			_ => new() { cost = 1, recycle = true },
		};

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> upgrade switch
		{
			Upgrade.B => new HashSet<ICardTraitEntry> { ModEntry.Instance.KokoroApi.Fleeting.Trait, ModEntry.Instance.KokoroApi.Limited.Trait },
			Upgrade.A => new HashSet<ICardTraitEntry>(),
			_ => new HashSet<ICardTraitEntry> { ModEntry.Instance.KokoroApi.Fleeting.Trait },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AStatus { targetPlayer = true, status = Stack.ApmStatus.Status, statusAmount = 2 },
				new AStatus { targetPlayer = true, status = Status.droneShift, statusAmount = 1 },
				new AStatus { targetPlayer = true, status = Status.drawLessNextTurn, statusAmount = 1 },
			],
			_ => [
				new AStatus { targetPlayer = true, status = Stack.ApmStatus.Status, statusAmount = 1 },
				new AStatus { targetPlayer = true, status = Status.droneShift, statusAmount = 1 },
				new AStatus { targetPlayer = true, status = Status.drawNextTurn, statusAmount = 1 },
			],
		};
}