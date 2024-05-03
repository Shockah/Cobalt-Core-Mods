using FSPRO;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Bloch;

internal sealed class MindPurgeCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.BlochDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/MindPurge.png"), StableSpr.cards_eunice).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "MindPurge", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = 1,
			description = ModEntry.Instance.Localizations.Localize(["card", "MindPurge", "description", upgrade.ToString()])
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new Action
			{
				ExtraCards = upgrade == Upgrade.A ? 1 : 0,
				CardCountOverride = upgrade == Upgrade.B ? 3 : null,
			}
		];

	private sealed class Action : CardAction
	{
		public int ExtraCards;
		public int? CardCountOverride;

		public override Route? BeginWithRoute(G g, State s, Combat c)
		{
			var route = new MultiCardBrowse()
			{
				mode = CardBrowse.Mode.Browse,
				browseSource = CardBrowse.Source.Hand,
				browseAction = new BrowseAction
				{
					ExtraCards = ExtraCards,
					CardCountOverride = CardCountOverride,
				}
			};
			c.Queue(new ADelay
			{
				time = 0.0,
				timer = 0.0
			});
			if (route.GetCardList(g).Count == 0)
			{
				timer = 0.0;
				return null;
			}
			return route;
		}
	}

	private sealed class BrowseAction : CardAction
	{
		public int ExtraCards;
		public int? CardCountOverride;

		public override string? GetCardSelectText(State s)
			=> ModEntry.Instance.Localizations.Localize(["card", "MindPurge", "browseText"]);

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);

			var cardsToDiscard = c.hand
				.Where(card => this.GetSelectedCards().Any(selectedCard => selectedCard.uuid == card.uuid))
				.ToList();

			for (var i = 0; i < cardsToDiscard.Count; i++)
			{
				var card = cardsToDiscard[i];
				c.hand.Remove(card);
				card.waitBeforeMoving = i * 0.05;
				card.OnDiscard(s, c);
				c.SendCardToDiscard(s, card);
			}

			if (cardsToDiscard.Count == 0)
				return;

			Audio.Play(Event.CardHandling);
			c.QueueImmediate(new ADrawCard { count = CardCountOverride ?? (cardsToDiscard.Count + ExtraCards) });
		}
	}
}
