using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using System.Collections.Generic;

namespace Shockah.Soggins;

public sealed class EnergyApologyCard : ApologyCard, IRegisterableCard
{
	public void RegisterCard(ICardRegistry registry)
	{
		ExternalCard card = new(
			globalName: $"{GetType().Namespace}.Card.Apology.Energy",
			cardType: GetType(),
			cardArt: ModEntry.Instance.SogginsDeckBorder,
			actualDeck: ModEntry.Instance.SogginsDeck
		);
		card.AddLocalisation(I18n.ApologyCardName);
		registry.RegisterCard(card);
	}

	public override CardData GetData(State state)
	{
		var data = base.GetData(state);
		data.art = StableSpr.cards_ExtraBattery;
		return data;
	}

	public override double GetApologyWeight(State state, Combat combat, int timesGiven)
		=> base.GetApologyWeight(state, combat, timesGiven) * 0.5;

	public override List<CardAction> GetActions(State s, Combat c)
		=> new()
		{
			new AEnergy
			{
				changeAmount = 1
			}
		};
}