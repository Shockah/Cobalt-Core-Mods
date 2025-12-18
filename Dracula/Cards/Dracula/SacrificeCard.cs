using FSPRO;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;
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
			Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/Dracula/Sacrifice.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Dracula", "Sacrifice", "name"]).Localize
		});

		helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnPlayerPlayCard), (Card card, State state, Combat combat) =>
		{
			if (!card.GetDataWithOverrides(state).singleUse)
				return;
			combat.GetSingleUseCardsPlayed().Add(card);
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = upgrade == Upgrade.B ? 1 : 2,
			exhaust = true,
			description = ModEntry.Instance.Localizations.Localize(["card", "Dracula", "Sacrifice", "description", upgrade.ToString()])
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new ACardSelect
				{
					browseSource = CardBrowse.Source.Hand,
					browseAction = new ExhaustCardBrowseAction
					{
						OnSuccess = [
							ModEntry.Instance.KokoroApi.CustomCardBrowseSource.ModifyCardSelect(new ACardSelect
							{
								browseAction = new PutCardInHandBrowseAction(),
								ignoreCardType = Key()
							}).SetCustomBrowseSource(new ExhaustOrSingleUseBrowseSource()).AsCardAction
						]
					},
				},
				new ATooltipAction
				{
					Tooltips = [
						new GlossaryTooltip($"{ModEntry.Instance.Package.Manifest.UniqueName}::Sacrifice::RemovedFromPlay")
						{
							Icon = StableSpr.icons_singleUse,
							TitleColor = Colors.cardtrait,
							Title = ModEntry.Instance.Localizations.Localize(["card", "Dracula", "Sacrifice", "removedFromPlay", "name"]),
							Description = ModEntry.Instance.Localizations.Localize(["card", "Dracula", "Sacrifice", "removedFromPlay", "description"])
						},
					]
				}
			],
			Upgrade.A => [
				ModEntry.Instance.KokoroApi.CustomCardBrowseSource.ModifyCardSelect(new ACardSelect
				{
					browseAction = new ExhaustCardBrowseAction
					{
						OnSuccess = [
							new ACardSelect
							{
								browseSource = CardBrowse.Source.ExhaustPile,
								browseAction = new PlayCardFromAnywhereBrowseAction(),
								ignoreCardType = Key()
							}
						]
					}
				}).SetCustomBrowseSource(new HandDrawDiscardBrowseSource()).AsCardAction,
			],
			_ => [
				new ACardSelect
				{
					browseSource = CardBrowse.Source.Hand,
					browseAction = new ExhaustCardBrowseAction
					{
						OnSuccess = [
							new ACardSelect
							{
								browseSource = CardBrowse.Source.ExhaustPile,
								browseAction = new PlayCardFromAnywhereBrowseAction(),
								ignoreCardType = Key()
							}
						]
					}
				},
			]
		};

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
			if (s.FindCard(uuid) is not { } card)
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

			c.QueueImmediate(ModEntry.Instance.KokoroApi.PlayCardsFromAnywhere.MakeAction(selectedCard.uuid).AsCardAction);
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

	public sealed class ExhaustOrSingleUseBrowseSource : IKokoroApi.IV2.ICustomCardBrowseSourceApi.ICustomCardBrowseSource
	{
		public IReadOnlyList<Tooltip> GetSearchTooltips(State state)
			=> [new GlossaryTooltip("action.searchCardNew")
			{	
				Icon = StableSpr.icons_searchCardNew,
				TitleColor = Colors.action,
				Title = Loc.T("action.searchCardNew.name"),
				Description = ModEntry.Instance.Localizations.Localize(["card", "Dracula", "Sacrifice", "searchAction", "exhaustOrSingleUse"]),
			}];

		public string GetTitle(State state, Combat? combat, IReadOnlyList<Card> cards)
			=> ModEntry.Instance.Localizations.Localize(["card", "Dracula", "Sacrifice", "ui", "exhaustOrSingleUse"], new { Count = cards.Count });

		public IReadOnlyList<Card> GetCards(State state, Combat? combat)
			=> [
				.. combat?.exhausted ?? [],
				.. combat?.GetSingleUseCardsPlayed() ?? []
			];
	}

	public sealed class HandDrawDiscardBrowseSource : IKokoroApi.IV2.ICustomCardBrowseSourceApi.ICustomCardBrowseSource
	{
		public IReadOnlyList<Tooltip> GetSearchTooltips(State state)
			=> [new GlossaryTooltip("action.searchCardNew")
			{	
				Icon = StableSpr.icons_searchCardNew,
				TitleColor = Colors.action,
				Title = Loc.T("action.searchCardNew.name"),
				Description = ModEntry.Instance.Localizations.Localize(["card", "Dracula", "Sacrifice", "searchAction", "handDrawDiscard"]),
			}];
		
		public string GetTitle(State state, Combat? combat, IReadOnlyList<Card> cards)
			=> ModEntry.Instance.Localizations.Localize(["card", "Dracula", "Sacrifice", "ui", "handDrawDiscard"], new { Count = cards.Count });

		public IReadOnlyList<Card> GetCards(State state, Combat? combat)
			=> [
				.. state.deck,
				.. combat?.hand ?? [],
				.. combat?.discard ?? []
			];
	}
}