using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FSPRO;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.NewLight;

internal class MarksmansDodgeCard : GuardianCard, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var entry = helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.GuardianDeck.Deck,
				rarity = RARITY,
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Ability.png"), StableSpr.cards_riggs).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["Card", "Ability", "MarksmansDodge", "Name"]).Localize,
		});
		
		Abilities.SetAbilityType(entry.UniqueName, Abilities.UTILITY_ABILITY);
		Abilities.SetBaseCooldown(entry.UniqueName, 4);
	}

	public override CardData GetData(State state)
	{
		var data = base.GetData(state);
		return upgrade switch
		{
			Upgrade.A => data with { cost = 0, description = ModEntry.Instance.Localizations.Localize(["Card", "Ability", "MarksmansDodge", "Description", upgrade.ToString()]) },
			_ => data with { cost = 0, flippable = true, description = ModEntry.Instance.Localizations.Localize(["Card", "Ability", "MarksmansDodge", "Description", upgrade.ToString(), flipped ? "Left" : "Right"]) },
		};
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AMove { targetPlayer = true, dir = 2 },
				new Action(),
			],
			Upgrade.A => [
				new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 1 },
				new Action(),
			],
			_ => [
				new AMove { targetPlayer = true, dir = 1 },
				new Action(),
			],
		};
	
	private sealed class Action : CardAction
	{
		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);

			const int amount = 1;
			var amountLeft = amount;

			for (var i = s.deck.Count - 1; i >= 0; i--)
			{
				if (!IsCardMatching(s.deck[i]))
					continue;
				
				DoCard(s.deck[i]);
				
				if (amountLeft <= 0)
					break;
			}

			if (amountLeft > 0)
			{
				var matchingDiscardedCards = c.discard.Where(IsCardMatching).ToList();
				if (matchingDiscardedCards.Count < amountLeft)
					matchingDiscardedCards = matchingDiscardedCards.Shuffle(s.rngShuffle).ToList();
				
				foreach (var card in matchingDiscardedCards)
				{
					DoCard(card);
					
					if (amountLeft <= 0)
						break;
				}
			}

			if (amountLeft >= amount)
				return;

			Audio.Play(Event.CardHandling);

			bool IsCardMatching(Card card)
				=> card is WeaponCard;
			
			void DoCard(Card card)
			{
				if (!IsCardMatching(card))
					return;
				
				s.RemoveCardFromWhereverItIs(card.uuid);
				c.SendCardToHand(s, card);
				card.discount--;
				card.waitBeforeMoving = (amount - amountLeft) * 0.09;
				amountLeft--;
			}
		}
	}
}