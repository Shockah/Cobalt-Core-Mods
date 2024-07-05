using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using System.Collections.Generic;

namespace Shockah.Soggins;

[CardMeta(dontOffer = true, rarity = Rarity.common, unreleased = true)]
public sealed class RandomPlaceholderApologyCard : ApologyCard, IRegisterableCard
{
	public void RegisterCard(ICardRegistry registry)
	{
		ExternalCard card = new(
			globalName: $"{GetType().Namespace}.Card.Apology.Blank",
			cardType: GetType(),
			cardArt: Art,
			actualDeck: ModEntry.Instance.ApologiesDeck
		);
		card.AddLocalisation(I18n.ApologyCardName);
		registry.RegisterCard(card);
	}

	public override CardData GetData(State state)
	{
		var data = base.GetData(state);
		data.description = I18n.BlankApologyCardText;
		return data;
	}

	public override double GetApologyWeight(State state, Combat combat, int timesGiven)
		=> 0;

	public override List<CardAction> GetActions(State s, Combat c)
		=> [new ADummyAction()];
}