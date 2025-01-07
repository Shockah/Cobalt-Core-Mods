using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
		var description = ModEntry.Instance.Localizations.Localize(["card", "UnstableMagic", "description", upgrade.ToString()]);
		return upgrade switch
		{
			Upgrade.A => new() { cost = 0, description = description },
			Upgrade.B => new() { cost = 1, description = description },
			_ => new() { cost = 1, description = description },
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

			cardsEnumerable = cardsEnumerable.Where(card => !ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(s, card, ExplosiveManager.ExplosiveTrait));

			var cards = cardsEnumerable.ToList();
			if (cards.Count == 0)
			{
				timer = 0;
				return;
			}

			var card = cards.Count == 1 ? cards[0] : cards[s.rngActions.NextInt() % cards.Count];
			ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(s, card, ExplosiveManager.ExplosiveTrait, true, permanent: false);
		}
	}
}