using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Dracula;

internal sealed class BraceCard : Card, IDraculaCard
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("Brace", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.MavisDeck.Deck,
				rarity = Rarity.common,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = StableSpr.cards_Corrode,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Mavis", "Brace", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new() { cost = 1 };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				ModEntry.Instance.KokoroApi.TempHull.MakeGainAction(2).AsCardAction,
			],
			Upgrade.B => [
				ModEntry.Instance.KokoroApi.TempHull.MakeGainAction(2).AsCardAction,
				ModEntry.Instance.KokoroApi.SpoofedActions.MakeAction(
					ModEntry.Instance.KokoroApi.Conditional.MakeAction(
						new HullCondition { BelowHalf = true, OverrideValue = s.ship.hull + 2 <= s.ship.hullMax / 2 },
						ModEntry.Instance.KokoroApi.TempHull.MakeGainAction(1).AsCardAction
					).AsCardAction,
					ModEntry.Instance.KokoroApi.Conditional.MakeAction(
						new HullCondition { BelowHalf = true },
						ModEntry.Instance.KokoroApi.TempHull.MakeGainAction(1).AsCardAction
					).AsCardAction
				).AsCardAction,
			],
			_ => [
				ModEntry.Instance.KokoroApi.TempHull.MakeGainAction(1).AsCardAction,
				ModEntry.Instance.KokoroApi.SpoofedActions.MakeAction(
					ModEntry.Instance.KokoroApi.Conditional.MakeAction(
						new HullCondition { BelowHalf = true, OverrideValue = s.ship.hull + 1 <= s.ship.hullMax / 2 },
						ModEntry.Instance.KokoroApi.TempHull.MakeGainAction(1).AsCardAction
					).AsCardAction,
					ModEntry.Instance.KokoroApi.Conditional.MakeAction(
						new HullCondition { BelowHalf = true },
						ModEntry.Instance.KokoroApi.TempHull.MakeGainAction(1).AsCardAction
					).AsCardAction
				).AsCardAction,
			]
		};
}
