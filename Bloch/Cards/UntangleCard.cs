using System.Collections.Generic;
using System.Reflection;
using FSPRO;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.Bloch;

internal sealed class UntangleCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Untangle.png"), StableSpr.cards_FumeCannon).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Untangle", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new() { cost = 0, floppable = true, description = ModEntry.Instance.Localizations.Localize(["card", "Untangle", "description", upgrade.ToString(), flipped ? "flipped" : "normal"]) };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new ScryAction { Amount = 3, disabled = flipped },
				new DiscardChooseAction { Count = 2, disabled = !flipped },
				ModEntry.Instance.KokoroApi.OnDiscard.MakeAction(
					new AAddCard { card = new ThoughtCard(), destination = CardDestination.Hand }
				).AsCardAction,
			],
			Upgrade.A => [
				new ScryAction { Amount = 2, disabled = flipped },
				new DiscardChooseAction { Count = 1, disabled = !flipped },
				ModEntry.Instance.KokoroApi.OnDiscard.MakeAction(
					new AAddCard { card = new ThoughtCard { upgrade = Upgrade.A }, destination = CardDestination.Hand }
				).AsCardAction,
			],
			_ => [
				new ScryAction { Amount = 2, disabled = flipped },
				new DiscardChooseAction { Count = 1, disabled = !flipped },
				ModEntry.Instance.KokoroApi.OnDiscard.MakeAction(
					new AAddCard { card = new ThoughtCard(), destination = CardDestination.Hand }
				).AsCardAction,
			],
		};

	private sealed class DiscardChooseAction : CardAction
	{
		public required int Count;
		
		public override Route? BeginWithRoute(G g, State s, Combat c)
		{
			if (Count <= 0)
			{
				timer = 0.0;
				return null;
			}

			var route = new CardBrowse
			{
				browseSource = CardBrowse.Source.Hand,
				browseAction = new BrowseAction { Count = Count },
			};

			if (Count > 1)
				route = ModEntry.Instance.KokoroApi.MultiCardBrowse
					.MakeRoute(route)
					.SetMinSelected(Count)
					.SetMaxSelected(Count)
					.AsRoute;
			
			if (route.GetCardList(g).Count == 0)
			{
				timer = 0.0;
				return null;
			}
			c.Queue(new ADelay { time = 0.0, timer = 0.0 });
			return route;
		}
	}
	
	private sealed class BrowseAction : CardAction
	{
		public required int Count;
		
		public override string GetCardSelectText(State s)
			=> ModEntry.Instance.Localizations.Localize(["card", "Untangle", "browseText", Count <= 1 ? "single" : "multiple"], new { Count = Count });

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);

			var selectedCards = ModEntry.Instance.KokoroApi.MultiCardBrowse.GetSelectedCards(this) ?? (selectedCard is null ? [] : [selectedCard]);
			for (var i = 0; i < selectedCards.Count; i++)
			{
				var card = selectedCards[i];
				c.hand.Remove(card);
				card.waitBeforeMoving = i * 0.05;
				card.OnDiscard(s, c);
				c.SendCardToDiscard(s, card);
			}

			if (selectedCards.Count == 0)
				return;

			Audio.Play(Event.CardHandling);
		}
	}
}
