using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;
using Shockah.Shared;

namespace Shockah.Gary;

public sealed class DefensiveTechCard : Card, IRegisterable, IHasCustomCardTraits
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/DefensiveTech.png"), StableSpr.cards_BubbleField).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "DefensiveTech", "name"]).Localize,
		});
		
		ModEntry.Instance.KokoroApi.Finite.SetBaseFiniteUses(entry.UniqueName, Upgrade.None, 2);
		ModEntry.Instance.KokoroApi.Finite.SetBaseFiniteUses(entry.UniqueName, Upgrade.A, 2);
		ModEntry.Instance.KokoroApi.Finite.SetBaseFiniteUses(entry.UniqueName, Upgrade.B, 3);
	}

	public override CardData GetData(State state)
		=> new() { cost = 1 };

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> new HashSet<ICardTraitEntry> { ModEntry.Instance.KokoroApi.Finite.Trait };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				ModEntry.Instance.KokoroApi.Sequence.MakeAction(
					uuid, IKokoroApi.IV2.ISequenceApi.Interval.Turn, 3, 3,
					new AStatus { targetPlayer = true, status = Status.bubbleJuice, statusAmount = 2 }
				).AsCardAction,
				new AStatus { targetPlayer = true, status = Status.bubbleJuice, statusAmount = 1 },
			],
			Upgrade.A => [
				ModEntry.Instance.KokoroApi.Sequence.MakeAction(
					uuid, IKokoroApi.IV2.ISequenceApi.Interval.Turn, 1, 2,
					new AEnergy { changeAmount = 1 }
				).AsCardAction,
				new AStatus { targetPlayer = true, status = Status.bubbleJuice, statusAmount = 1 },
			],
			_ => [
				new AStatus { targetPlayer = true, status = Status.bubbleJuice, statusAmount = 1 },
			],
		};
}