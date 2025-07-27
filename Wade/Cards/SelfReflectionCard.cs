using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FSPRO;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.Wade;

internal sealed class SelfReflectionCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.WadeDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Card/SelfReflection.png"), StableSpr.cards_colorless).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "SelfReflection", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
	{
		var data = new CardData { artTint = "ffffff", description = ModEntry.Instance.Localizations.Localize(["card", "SelfReflection", "description", upgrade.ToString()]) };
		return upgrade switch
		{
			Upgrade.A => data with { cost = 1, retain = true },
			_ => data with { cost = 1, artTint = "ffffff" },
		};
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new Action { count = 6 },
			],
			_ => [
				new Action { count = 4 },
			],
		};

	private sealed class Action : ADrawCard
	{
		public override void Begin(G g, State s, Combat c)
		{
			timer *= 0.5;
			var cardsSent = 0;

			Iterate();

			if (cardsSent < count && c.discard.Any(CardMatches))
			{
				var deckCopy = s.deck.ToList();
				s.deck.Clear();
				s.ShuffleDeck(true);
				Iterate();
				s.deck.AddRange(deckCopy);
			}
			
			if (cardsSent != 0)
				Audio.Play(Event.CardHandling);

			bool CardMatches(Card card)
				=> card.GetMeta().deck == ModEntry.Instance.WadeDeck.Deck;
			
			void Iterate()
			{
				for (var i = s.deck.Count - 1; i >= 0; i--)
				{
					var card = s.deck[i];
					if (!CardMatches(card))
						continue;

					c.DrawCardIdx(s, i);
					card.waitBeforeMoving = cardsSent * 0.09;
					cardsSent++;

					if (cardsSent >= count)
						return;
				}
			}
		}
	}
}