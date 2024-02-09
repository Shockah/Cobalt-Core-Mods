using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using System.Collections.Generic;

namespace Shockah.Soggins;

public sealed class MoveApologyCard : ApologyCard, IRegisterableCard
{
	public void RegisterCard(ICardRegistry registry)
	{
		ExternalCard card = new(
			globalName: $"{GetType().Namespace}.Card.Apology.Move",
			cardType: GetType(),
			cardArt: Art,
			actualDeck: ModEntry.Instance.SogginsDeck
		);
		card.AddLocalisation(I18n.ApologyCardName);
		registry.RegisterCard(card);
	}

	public override CardData GetData(State state)
	{
		var data = base.GetData(state);
		data.flippable = true;
		return data;
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AMove
			{
				targetPlayer = true,
				dir = 1
			}
		];
}