using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;
using Shockah.Shared;

namespace Shockah.Natasha;

internal sealed class TypoCard : Card, IRegisterable, IHasCustomCardTraits
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var entry = helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.NatashaDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Typo.png"), StableSpr.cards_Overdrive).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Typo", "name"]).Localize
		});

		ModEntry.Instance.KokoroApi.Limited.SetBaseLimitedUses(entry.UniqueName, Upgrade.B, 2);
		
		ModEntry.Instance.KokoroApi.Finite.SetBaseFiniteUses(entry.UniqueName, Upgrade.None, 2);
		ModEntry.Instance.KokoroApi.Finite.SetBaseFiniteUses(entry.UniqueName, Upgrade.A, 2);
	}

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> (HashSet<ICardTraitEntry>)(upgrade switch
		{
			Upgrade.B => [ModEntry.Instance.KokoroApi.Limited.Trait],
			_ => [ModEntry.Instance.KokoroApi.Finite.Trait],
		});

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.B => new() { cost = 0, infinite = true },
			_ => new() { cost = 0 },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				ModEntry.Instance.KokoroApi.Sequence.MakeAction(uuid, IKokoroApi.IV2.ISequenceApi.Interval.Combat, 1, 2, new AStatus { targetPlayer = false, status = Status.powerdrive, statusAmount = 1 }).AsCardAction,
				ModEntry.Instance.KokoroApi.Sequence.MakeAction(uuid, IKokoroApi.IV2.ISequenceApi.Interval.Combat, 2, 2, new AStatus { targetPlayer = true, status = Status.powerdrive, statusAmount = 1 }).AsCardAction,
			],
			Upgrade.A => [
				ModEntry.Instance.KokoroApi.Sequence.MakeAction(uuid, IKokoroApi.IV2.ISequenceApi.Interval.Combat, 1, 2, new AStatus { targetPlayer = false, status = Status.overdrive, statusAmount = 1 }).AsCardAction,
				ModEntry.Instance.KokoroApi.Sequence.MakeAction(uuid, IKokoroApi.IV2.ISequenceApi.Interval.Combat, 2, 2, new AStatus { targetPlayer = true, status = Status.overdrive, statusAmount = 2 }).AsCardAction,
			],
			_ => [
				ModEntry.Instance.KokoroApi.Sequence.MakeAction(uuid, IKokoroApi.IV2.ISequenceApi.Interval.Combat, 1, 2, new AStatus { targetPlayer = false, status = Status.overdrive, statusAmount = 1 }).AsCardAction,
				ModEntry.Instance.KokoroApi.Sequence.MakeAction(uuid, IKokoroApi.IV2.ISequenceApi.Interval.Combat, 2, 2, new AStatus { targetPlayer = true, status = Status.overdrive, statusAmount = 1 }).AsCardAction,
			]
		};
}
