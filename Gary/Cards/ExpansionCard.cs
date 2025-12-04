using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;
using Shockah.Shared;

namespace Shockah.Gary;

public sealed class ExpansionCard : Card, IRegisterable, IHasCustomCardTraits
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Expansion.png"), StableSpr.cards_ScootRight).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Expansion", "name"]).Localize,
		});
		
		ModEntry.Instance.KokoroApi.Finite.SetBaseFiniteUses(entry.UniqueName, Upgrade.None, 2);
		ModEntry.Instance.KokoroApi.Finite.SetBaseFiniteUses(entry.UniqueName, Upgrade.A, 2);
	}

	public override CardData GetData(State state)
	{
		var data = new CardData
		{
			flippable = true,
			art = flipped ? StableSpr.cards_ScootLeft :  StableSpr.cards_ScootRight,
		};
		return upgrade switch
		{
			Upgrade.B => data with { cost = 2 },
			_ => data with { cost = 1 },
		};
	}

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> upgrade switch
		{
			Upgrade.B => new HashSet<ICardTraitEntry>(),
			_ => new HashSet<ICardTraitEntry> { ModEntry.Instance.KokoroApi.Finite.Trait },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AMove { targetPlayer = true, dir = 1 },
				new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 2 },
				new AStatus { targetPlayer = true, status = Stack.ApmStatus.Status, statusAmount = 2 },
			],
			Upgrade.A => [
				new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 1 },
				new AStatus { targetPlayer = true, status = Stack.ApmStatus.Status, statusAmount = 1 },
			],
			_ => [
				ModEntry.Instance.KokoroApi.Sequence.MakeAction(
					uuid, IKokoroApi.IV2.ISequenceApi.Interval.Turn, 1, 2,
					new AMove { targetPlayer = true, dir = 1 }
				).AsCardAction,
				ModEntry.Instance.KokoroApi.Sequence.MakeAction(
					uuid, IKokoroApi.IV2.ISequenceApi.Interval.Turn, 2, 2,
					new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 1 }
				).AsCardAction,
				new AStatus { targetPlayer = true, status = Stack.ApmStatus.Status, statusAmount = 1 },
			],
		};
}