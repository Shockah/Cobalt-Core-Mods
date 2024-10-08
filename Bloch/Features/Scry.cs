﻿using FSPRO;
using Nickel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Bloch;

internal sealed class ScryManager
{
	internal static ISpriteEntry ActionIcon = null!;

	public ScryManager()
	{
		ActionIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Actions/Scry.png"));
	}
}

internal sealed class ScryAction : CardAction
{
	public required int Amount;
	public bool FromInsight;

	public override Icon? GetIcon(State s)
		=> new(ScryManager.ActionIcon.Sprite, Amount, Colors.textMain);

	public override List<Tooltip> GetTooltips(State s)
		=> [
			new GlossaryTooltip($"action.{GetType().Namespace!}::Scry")
			{
				Icon = ScryManager.ActionIcon.Sprite,
				TitleColor = Colors.action,
				Title = ModEntry.Instance.Localizations.Localize(["action", "Scry", "name"]),
				Description = ModEntry.Instance.Localizations.Localize(["action", "Scry", "description"], new { Amount }),
			}
		];

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		if (Amount <= 0)
			return;

		if (s.deck.Count < Amount && c.discard.Count > 0)
		{
			var currentDeck = s.deck.ToList();
			s.deck.Clear();

			foreach (var card in c.discard)
				s.SendCardToDeck(card, doAnimation: true);
			c.discard.Clear();
			s.ShuffleDeck(isMidCombat: true);

			s.deck.AddRange(currentDeck);
		}
	}

	public override Route? BeginWithRoute(G g, State s, Combat c)
	{
		if (Amount <= 0)
		{
			timer = 0;
			return null;
		}

		var cards = s.deck.TakeLast(Amount).Reverse().ToList();
		if (cards.Count == 0)
		{
			timer = 0;
			return null;
		}

		c.Queue(new ADelay { timer = 0.0 });

		return new MultiCardBrowse()
		{
			mode = CardBrowse.Mode.Browse,
			browseSource = CardBrowse.Source.DrawPile,
			browseAction = new BrowseAction { PresentedCards = cards, FromInsight = FromInsight },
			CardsOverride = cards,
			EnabledSorting = false,
		};
	}

	private sealed class BrowseAction : CardAction
	{
		public required List<Card> PresentedCards;
		public required bool FromInsight;

		public override string? GetCardSelectText(State s)
			=> ModEntry.Instance.Localizations.Localize(["action", "Scry", "browseText"]);

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);

			var cardsToDiscard = s.deck
				.Where(card => (ModEntry.Instance.Api.GetSelectedMultiCardBrowseCards(this) ?? []).Any(selectedCard => selectedCard.uuid == card.uuid))
				.ToList();

			for (var i = 0; i < cardsToDiscard.Count; i++)
			{
				var card = cardsToDiscard[i];
				s.deck.Remove(card);
				card.waitBeforeMoving = i * 0.05;
				card.OnDiscard(s, c);
				c.SendCardToDiscard(s, card);
			}

			if (cardsToDiscard.Count != 0)
				Audio.Play(Event.CardHandling);

			foreach (var hook in ModEntry.Instance.HookManager.GetHooksWithProxies(ModEntry.Instance.KokoroApi, s.EnumerateAllArtifacts()))
				hook.OnScryResult(s, c, PresentedCards, cardsToDiscard, FromInsight);
		}
	}
}
