using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using System.Collections.Generic;

namespace Shockah.Soggins;

[CardMeta(rarity = Rarity.uncommon, upgradesTo = new Upgrade[] { Upgrade.A, Upgrade.B })]
public sealed class BetterThanYouCard : Card, IRegisterableCard, IFrogproofCard
{
	private static ModEntry Instance => ModEntry.Instance;

	public void RegisterCard(ICardRegistry registry)
	{
		ExternalCard card = new(
			globalName: $"{GetType().Namespace}.Card.BetterThanYou",
			cardType: GetType(),
			cardArt: ModEntry.Instance.SogginsDeckBorder,
			actualDeck: ModEntry.Instance.SogginsDeck
		);
		card.AddLocalisation(I18n.BetterThanYouCardName);
		registry.RegisterCard(card);
	}

	private string GetText()
		=> upgrade switch
		{
			Upgrade.A => I18n.BetterThanYouCardTextA,
			Upgrade.B => I18n.BetterThanYouCardTextB,
			_ => I18n.BetterThanYouCardText0,
		};

	private int GetCost()
		=> upgrade switch
		{
			Upgrade.A => 0,
			Upgrade.B => 1,
			_ => 1,
		};

	public override CardData GetData(State state)
	{
		var data = base.GetData(state);
		data.art = StableSpr.cards_colorless;
		data.description = GetText();
		data.cost = GetCost();
		return data;
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => new()
			{
				new ADrawFromADeck
				{
					Deck = (Deck)Instance.SogginsDeck.Id!.Value,
					Amount = 10,
					IgnoreCardID = uuid
				}
			},
			_ => new()
			{
				new ADiscardAndDrawFromADeck
				{
					Deck = (Deck)Instance.SogginsDeck.Id!.Value,
					IgnoreCardID = uuid
				}
			}
		};
}
