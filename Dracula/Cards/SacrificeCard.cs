using FSPRO;
using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dracula;

internal static class SacrificeExt
{
	public static HashSet<Card> GetSingleUseCardsPlayed(this Combat self)
		=> ModEntry.Instance.Helper.ModData.ObtainModData<HashSet<Card>>(self, "SingleUseCardsPlayed");
}

internal sealed class SacrificeCard : Card, IDraculaCard
{
	private const CardBrowse.Source ExhaustOrSingleUseBrowseSource = (CardBrowse.Source)2137201;
	private const CardBrowse.Source HandDrawDiscardBrowseSource = (CardBrowse.Source)2137202;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
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
			Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/Sacrifice.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Sacrifice", "name"]).Localize
		});

		helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnPlayerPlayCard), (Card card, State state, Combat combat) =>
		{
			if (!card.GetDataWithOverrides(state).singleUse)
				return;
			combat.GetSingleUseCardsPlayed().Add(card);
		}, 0);

		CustomCardBrowse.RegisterCustomCardSource(
			ExhaustOrSingleUseBrowseSource,
			new(
				(_, _, cards) => ModEntry.Instance.Localizations.Localize(["card", "Sacrifice", "ui", "exhaustOrSingleUse"], new { Count = cards.Count }),
				(_, c) => [..c?.exhausted ?? [], .. c?.GetSingleUseCardsPlayed() ?? []]
			)
		);
		CustomCardBrowse.RegisterCustomCardSource(
			HandDrawDiscardBrowseSource,
			new(
				(_, _, cards) => ModEntry.Instance.Localizations.Localize(["card", "Sacrifice", "ui", "handDrawDiscard"], new { Count = cards.Count }),
				(s, c) => [..s.deck, ..c?.hand ?? [], ..c?.discard ?? []]
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
				browseSource = upgrade == Upgrade.A ? HandDrawDiscardBrowseSource : CardBrowse.Source.Hand,
				browseAction = new ExhaustCardBrowseAction
				{
					OnSuccess = [
						new ACardSelect
						{
							browseSource = upgrade == Upgrade.B
								? ExhaustOrSingleUseBrowseSource
								: CardBrowse.Source.ExhaustPile,
							browseAction = upgrade == Upgrade.B
								? new PutCardInHandBrowseAction()
								: new PlayCardFromAnywhereBrowseAction(),
							ignoreCardType = Key()
						}
					]
				},
				omitFromTooltips = true
			},
			new ATooltipAction
			{
				Tooltips = upgrade == Upgrade.B
					? [
						new GlossaryTooltip($"{ModEntry.Instance.Package.Manifest.UniqueName}::Sacrifice::RemovedFromPlay")
						{
							Icon = StableSpr.icons_singleUse,
							TitleColor = Colors.cardtrait,
							Title = ModEntry.Instance.Localizations.Localize(["card", "Sacrifice", "removedFromPlay", "name"]),
							Description = ModEntry.Instance.Localizations.Localize(["card", "Sacrifice", "removedFromPlay", "description"])
						},
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

			c.QueueImmediate([
				new AFixedExhaustOtherCard { uuid = selectedCard.uuid },
				..(OnSuccess ?? [])
			]);
		}
	}

	private sealed class AFixedExhaustOtherCard : AExhaustOtherCard
	{
		public override void Begin(G g, State s, Combat c)
		{
			timer = 0.0;
			if (s.FindCard(uuid) is not Card card)
				return;

			card.ExhaustFX();
			Audio.Play(Event.CardHandling);
			s.RemoveCardFromWhereverItIs(uuid);
			c.SendCardToExhaust(s, card);
			timer = 0.3;
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
				c.GetSingleUseCardsPlayed().Remove(selectedCard);
			s.RemoveCardFromWhereverItIs(selectedCard.uuid);
			c.SendCardToHand(s, selectedCard);
		}
	}
}