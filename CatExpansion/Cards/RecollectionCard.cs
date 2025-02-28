using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FSPRO;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.CatExpansion;

public sealed class RecollectionCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = Deck.colorless,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Card/Recollection.png"), StableSpr.cards_riggs).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Recollection", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> new() { cost = 1, description = ModEntry.Instance.Localizations.Localize(["card", "Recollection", "description", upgrade.ToString()]) };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new DrawTempCardsAction { Amount = 1 },
				new ADrawCard { count = 4 },
			],
			Upgrade.B => [
				new DrawTempCardsAction { Amount = 3 },
			],
			_ => [
				new DrawTempCardsAction { Amount = 1 },
				new ADrawCard { count = 2 },
			],
		};

	private sealed class DrawTempCardsAction : CardAction
	{
		public required int Amount;

		public override List<Tooltip> GetTooltips(State s)
			=> [
				.. new ADrawCard { count = Amount }.GetTooltips(s),
				new TTGlossary("cardtrait.temporary"),
			];

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			var amountLeft = Amount;

			if (amountLeft > 0)
			{
				for (var i = s.deck.Count - 1; i >= 0; i--)
				{
					if (!IsCardMatching(s.deck[i]))
						continue;
				
					DrawCard(s.deck[i]);
				
					if (amountLeft <= 0)
						break;
				}
			}

			if (amountLeft > 0)
			{
				var matchingDiscardedCards = c.discard.Where(IsCardMatching).ToList();
				if (matchingDiscardedCards.Count < amountLeft)
					matchingDiscardedCards = matchingDiscardedCards.Shuffle(s.rngShuffle).ToList();
				
				foreach (var card in matchingDiscardedCards)
				{
					DrawCard(card);
					
					if (amountLeft <= 0)
						break;
				}
			}

			if (amountLeft >= Amount)
				return;

			Audio.Play(Event.CardHandling);

			bool IsCardMatching(Card card)
				=> ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(s, card, ModEntry.Instance.Helper.Content.Cards.TemporaryCardTrait);
			
			void DrawCard(Card card)
			{
				if (!IsCardMatching(card))
					return;
				
				s.RemoveCardFromWhereverItIs(card.uuid);
				c.SendCardToHand(s, card);
				card.waitBeforeMoving = (Amount - amountLeft) * 0.09;
				amountLeft--;
			}
		}
	}
}