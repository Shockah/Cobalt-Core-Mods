using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using System.Collections.Generic;

namespace Shockah.Soggins;

public sealed class EvadeApologyCard : ApologyCard, IRegisterableCard
{
	public void RegisterCard(ICardRegistry registry)
	{
		ExternalCard card = new(
			globalName: $"{GetType().Namespace}.Card.Apology.Evade",
			cardType: GetType(),
			cardArt: Art,
			actualDeck: ModEntry.Instance.SogginsDeck
		);
		card.AddLocalisation(I18n.ApologyCardName);
		registry.RegisterCard(card);
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AStatus
			{
				status = Status.evade,
				statusAmount = 1,
				targetPlayer = true
			}
		];
}