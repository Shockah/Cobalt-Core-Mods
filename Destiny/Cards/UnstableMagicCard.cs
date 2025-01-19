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
			artTint = "ffffff",
			description = ModEntry.Instance.Localizations.Localize(["card", "UnstableMagic", "description", upgrade.ToString()]),
			cost = 1,
		};
		return upgrade switch
		{
			Upgrade.A => data with { exhaust = true },
			Upgrade.B => data,
			_ => data with { exhaust = true },
		};
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [new Action { Amount = 3 }],
			Upgrade.B => [new Action { Amount = 1 }],
			_ => [new Action { Amount = 2 }],
		};

	private sealed class Action : CardAction
	{
		public required int Amount;

		public override List<Tooltip> GetTooltips(State s)
			=> [.. Explosive.ExplosiveTrait.Configuration.Tooltips?.Invoke(s, null) ?? []];

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);

			var cards = c.hand
				.Concat(s.deck)
				.Concat(c.discard)
				.Where(card => !ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(s, card, Explosive.ExplosiveTrait))
				.Where(card => !ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(s, card, ModEntry.Instance.Helper.Content.Cards.UnplayableCardTrait))
				.ToList();

			if (cards.Count == 0)
			{
				timer = 0;
				return;
			}

			for (var i = 0; i < Amount; i++)
			{
				if (cards.Count == 0)
					break;

				var index = cards.Count == 1 ? 0 : s.rngActions.NextInt() % cards.Count;
				var card = cards[index];
				cards.RemoveAt(index);
				c.QueueImmediate(ModEntry.Instance.KokoroApi.PlayCardsFromAnywhere.MakeModifyAction(card.uuid, new ActuallyModifyAction()).AsCardAction);
			}
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