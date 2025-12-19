using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Dracula;

internal sealed class JoltCard : Card, IDraculaCard, IHasCustomCardTraits
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("Jolt", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.MavisDeck.Deck,
				rarity = Rarity.common,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = StableSpr.cards_Dodge,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Mavis", "Jolt", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new() { cost = 0 };

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> new HashSet<ICardTraitEntry> { ModEntry.Instance.KokoroApi.Independent.Trait };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				ModEntry.Instance.KokoroApi.TempHull.MakeLossAction(1).AsCardAction,
				ModEntry.Instance.KokoroApi.Conditional.MakeAction(
					new HullCondition { BelowHalf = false },
					ModEntry.Instance.KokoroApi.TempHull.MakeLossAction(1).AsCardAction
				).SetShowQuestionMark(false).AsCardAction,
				new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 1 },
			],
			Upgrade.B => [
				new AHurt { targetPlayer = true, hurtAmount = 1 },
				new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 2 },
			],
			_ => [
				ModEntry.Instance.KokoroApi.TempHull.MakeLossAction(1).AsCardAction,
				new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 1 },
			]
		};
}