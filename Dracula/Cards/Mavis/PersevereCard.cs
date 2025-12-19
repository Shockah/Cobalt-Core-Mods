using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.Dracula;

internal sealed class PersevereCard : Card, IDraculaCard, IHasCustomCardTraits
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var entry = helper.Content.Cards.RegisterCard("Persevere", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.MavisDeck.Deck,
				rarity = Rarity.rare,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = StableSpr.cards_Corrode,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Mavis", "Persevere", "name"]).Localize
		});
		
		ModEntry.Instance.KokoroApi.Finite.SetBaseFiniteUses(entry.UniqueName, Upgrade.A, 2);
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.B => new() { cost = 1, floppable = true, exhaust = true, art = flipped ? StableSpr.cards_MiningDrill_Bottom : StableSpr.cards_MiningDrill_Top },
			_ => new() { cost = 0, floppable = true, art = flipped ? StableSpr.cards_Adaptability_Bottom : StableSpr.cards_Adaptability_Top },
		};

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> upgrade switch
		{
			Upgrade.A => new HashSet<ICardTraitEntry> { ModEntry.Instance.KokoroApi.Independent.Trait, ModEntry.Instance.KokoroApi.Finite.Trait },
			_ => new HashSet<ICardTraitEntry> { ModEntry.Instance.KokoroApi.Independent.Trait },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AHeal { targetPlayer = true, healAmount = 1, disabled = flipped },
				ModEntry.Instance.KokoroApi.TempHull.MakeGainAction(2).AsCardAction.Disabled(flipped),
				new ADummyAction(),
				ModEntry.Instance.KokoroApi.TempHull.MakeLossAction(3).AsCardAction.Disabled(!flipped),
			],
			Upgrade.A => [
				ModEntry.Instance.KokoroApi.TempHull.MakeGainAction(1).AsCardAction.Disabled(flipped),
				new ADummyAction(),
				ModEntry.Instance.KokoroApi.TempHull.MakeLossAction(1).AsCardAction.Disabled(!flipped),
			],
			_ => [
				ModEntry.Instance.KokoroApi.TempHull.MakeGainAction(2).AsCardAction.Disabled(flipped),
				new ADummyAction(),
				ModEntry.Instance.KokoroApi.TempHull.MakeLossAction(2).AsCardAction.Disabled(!flipped),
			]
		};
}
