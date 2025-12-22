using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Dracula;

internal sealed class SwoopCard : Card, IDraculaCard, IHasCustomCardTraits
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("Swoop", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.MavisDeck.Deck,
				rarity = Rarity.common,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = StableSpr.cards_Cannon,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Mavis", "Swoop", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new() { cost = 1 };

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> new HashSet<ICardTraitEntry> { ModEntry.Instance.KokoroApi.Independent.Trait };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				ModEntry.Instance.KokoroApi.Conditional.MakeAction(
					new MissingCondition { Deck = GetMeta().deck, Missing = false },
					new AStatus { targetPlayer = true, status = ModEntry.Instance.MavisCharacter.MissingStatus.Status, statusAmount = 1 }
				).AsCardAction,
				new MavisSwoopAction { Attack = new() { damage = 2, status = ModEntry.Instance.BleedingStatus.Status, statusAmount = 1 } },
			],
			Upgrade.A => [
				ModEntry.Instance.KokoroApi.Conditional.MakeAction(
					new MissingCondition { Deck = GetMeta().deck, Missing = false },
					new AStatus { targetPlayer = true, status = ModEntry.Instance.MavisCharacter.MissingStatus.Status, statusAmount = 2 }
				).AsCardAction,
				new MavisSwoopAction { Attack = new() { damage = 2 } },
			],
			_ => [
				ModEntry.Instance.KokoroApi.Conditional.MakeAction(
					new MissingCondition { Deck = GetMeta().deck, Missing = false },
					new AStatus { targetPlayer = true, status = ModEntry.Instance.MavisCharacter.MissingStatus.Status, statusAmount = 1 }
				).AsCardAction,
				new MavisSwoopAction { Attack = new() { damage = 2 } },
			]
		};
}