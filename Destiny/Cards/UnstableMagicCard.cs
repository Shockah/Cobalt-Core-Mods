using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FSPRO;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.Destiny;

public sealed class UnstableMagicCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DestinyDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/UnstableMagic.png"), StableSpr.cards_dizzy).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "UnstableMagic", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
	{
		var data = new CardData
		{
			art = Enchanted.GetCardArt(this),
			artTint = "ffffff",
			description = ModEntry.Instance.Localizations.Localize(["card", "UnstableMagic", "description", upgrade.ToString()]),
		};
		return upgrade switch
		{
			Upgrade.A => data with { cost = 0 },
			Upgrade.B => data with { cost = 1 },
			_ => data with { cost = 1 },
		};
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [new Action { InHand = true, InDrawPile = true, InDiscardPile = true }],
			Upgrade.B => [new Action { InHand = true, InDrawPile = false, InDiscardPile = false }],
			_ => [new Action { InHand = true, InDrawPile = true, InDiscardPile = true }],
		};

	private sealed class Action : CardAction
	{
		public required bool InHand;
		public required bool InDrawPile;
		public required bool InDiscardPile;

		public override List<Tooltip> GetTooltips(State s)
			=> [.. Explosive.ExplosiveTrait.Configuration.Tooltips?.Invoke(s, null) ?? []];

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);

			IEnumerable<Card> cardsEnumerable = [];
			if (InHand)
				cardsEnumerable = cardsEnumerable.Concat(c.hand);
			if (InDrawPile)
				cardsEnumerable = cardsEnumerable.Concat(s.deck);
			if (InDiscardPile)
				cardsEnumerable = cardsEnumerable.Concat(c.discard);

			cardsEnumerable = cardsEnumerable.Where(card => !ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(s, card, Explosive.ExplosiveTrait));

			var cards = cardsEnumerable.ToList();
			if (cards.Count == 0)
			{
				timer = 0;
				return;
			}

			var card = cards.Count == 1 ? cards[0] : cards[s.rngActions.NextInt() % cards.Count];
			c.QueueImmediate(ModEntry.Instance.KokoroApi.PlayCardsFromAnywhere.MakeModifyAction(card.uuid, new ActuallyModifyAction()).AsCardAction);
		}
	}

	private sealed class ActuallyModifyAction : CardAction
	{
		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;

			if (selectedCard is null)
				return;
			
			ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(s, selectedCard, Explosive.ExplosiveTrait, true, permanent: false);
			Audio.Play(Event.Status_PowerUp);
		}
	}
}