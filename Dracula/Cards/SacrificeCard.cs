using Nickel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class SacrificeCard : Card, IDraculaCard
{
	private const CardBrowse.Source ExhaustOrSingleUseNonSacrificeBrowseSource = (CardBrowse.Source)2137201;
	private const CardBrowse.Source HandDrawDiscardNonSacrificeBrowseSource = (CardBrowse.Source)2137202;

	private static bool IsDuringTryPlayCard { get; set; } = false;

	public static void Register(IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("Sacrifice", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DraculaDeck.Deck,
				rarity = Rarity.rare,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Sacrifice", "name"]).Localize
		});

		helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnPlayerPlayCard), (Card card, State state, Combat combat) =>
		{
			if (!card.GetDataWithOverrides(state).singleUse)
				return;
			ModEntry.Instance.KokoroApi.ObtainExtensionData<HashSet<Card>>(combat, "SingleUseCardsPlayed").Add(card);
		}, 0);

		CustomCardBrowse.RegisterCustomCardSource(
			ExhaustOrSingleUseNonSacrificeBrowseSource,
			new(
				(_, _, cards) => ModEntry.Instance.Localizations.Localize(["card", "Sacrifice", "ui", "exhaustOrSingleUse"], new { Count = cards.Count }),
				(_, c) => c.exhausted.Concat(ModEntry.Instance.KokoroApi.ObtainExtensionData<HashSet<Card>>(c, "SingleUseCardsPlayed")).Where(c => c is not SacrificeCard).ToList()
			)
		);
		CustomCardBrowse.RegisterCustomCardSource(
			HandDrawDiscardNonSacrificeBrowseSource,
			new(
				(_, _, cards) => ModEntry.Instance.Localizations.Localize(["card", "Sacrifice", "ui", "handDrawDiscard"], new { Count = cards.Count }),
				(s, c) => s.deck.Concat(c.hand).Concat(c.discard).Where(c => c is not SacrificeCard).ToList()
			)
		);
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = upgrade == Upgrade.B ? 1 : 2,
			exhaust = true,
			description = ModEntry.Instance.Localizations.Localize(["card", "Sacrifice", "description", upgrade.ToString()])
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new ACardSelect
			{
				browseSource = upgrade == Upgrade.A ? HandDrawDiscardNonSacrificeBrowseSource : CardBrowse.Source.Hand,
				browseAction = new ExhaustCardBrowseAction
				{
					OnSuccess = [
						new ACardSelect
						{
							browseSource = upgrade == Upgrade.B
								? ExhaustOrSingleUseNonSacrificeBrowseSource
								: CardBrowse.Source.ExhaustPile,
							browseAction = upgrade == Upgrade.B
								? new PutCardInHandBrowseAction()
								: new PlayCardFromAnywhereBrowseAction(),
						}
					]
				},
				omitFromTooltips = true
			},
			new ATooltipAction
			{
				Tooltips = upgrade == Upgrade.B
					? [
						new TTGlossary("cardtrait.singleUse")
					] : null
			}
		];

	public sealed class ExhaustCardBrowseAction : CardAction
	{
		public required List<CardAction>? OnSuccess;

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			if (selectedCard is null)
				return;
			c.QueueImmediate(new List<CardAction>
			{
				new AExhaustOtherCard
				{
					uuid = selectedCard.uuid
				}
			}.Concat(OnSuccess ?? []).ToList());
		}
	}

	private sealed class PlayCardFromAnywhereBrowseAction : CardAction
	{
		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			if (selectedCard is null)
				return;
			c.QueueImmediate(ModEntry.Instance.KokoroApi.Actions.MakePlaySpecificCardFromAnywhere(selectedCard.uuid));
		}
	}

	private sealed class PutCardInHandBrowseAction : CardAction
	{
		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			if (selectedCard is null)
				return;

			if (selectedCard.GetDataWithOverrides(s).singleUse)
				ModEntry.Instance.KokoroApi.ObtainExtensionData<HashSet<Card>>(c, "SingleUseCardsPlayed").Remove(selectedCard);
			s.RemoveCardFromWhereverItIs(selectedCard.uuid);
			c.SendCardToHand(s, selectedCard);
		}
	}
}
