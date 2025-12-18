using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Dracula;

internal sealed class StabilizeCard : Card, IDraculaCard,  IHasCustomCardTraits
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var entry = helper.Content.Cards.RegisterCard("Stabilize", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.MavisDeck.Deck,
				rarity = Rarity.uncommon,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = StableSpr.cards_Corrode,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Mavis", "Stabilize", "name"]).Localize
		});
		
		ModEntry.Instance.KokoroApi.Finite.SetBaseFiniteUses(entry.UniqueName, Upgrade.None, 2);
		ModEntry.Instance.KokoroApi.Finite.SetBaseFiniteUses(entry.UniqueName, Upgrade.A, 3);
		ModEntry.Instance.KokoroApi.Finite.SetBaseFiniteUses(entry.UniqueName, Upgrade.B, 2);
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.B => new() { cost = 0, retain = true },
			_ => new() { cost = 0 },
		};

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> new HashSet<ICardTraitEntry> { ModEntry.Instance.KokoroApi.Finite.Trait };

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			ModEntry.Instance.KokoroApi.Conditional.MakeAction(
				new HullCondition { BelowHalf = false },
				ModEntry.Instance.KokoroApi.TempHull.MakeLossAction(1).AsCardAction
			).AsCardAction,
			ModEntry.Instance.KokoroApi.SpoofedActions.MakeAction(
				ModEntry.Instance.KokoroApi.Conditional.MakeAction(
					new HullCondition { BelowHalf = true, OverrideValue = s.ship.hull - 1 <= s.ship.hullMax / 2 },
					ModEntry.Instance.KokoroApi.TempHull.MakeGainAction(1).AsCardAction
				).AsCardAction,
				ModEntry.Instance.KokoroApi.Conditional.MakeAction(
					new HullCondition { BelowHalf = true },
					ModEntry.Instance.KokoroApi.TempHull.MakeGainAction(1).AsCardAction
				).AsCardAction
			).AsCardAction,
		];
}
