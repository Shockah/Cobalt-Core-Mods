using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using System.Collections.Generic;

namespace Shockah.Soggins;

[CardMeta(rarity = Rarity.rare, upgradesTo = [Upgrade.A, Upgrade.B])]
public sealed class EnergyApologyCard : ApologyCard, IRegisterableCard
{
	public void RegisterCard(ICardRegistry registry)
	{
		ExternalCard card = new(
			globalName: $"{GetType().Namespace}.Card.Apology.Energy",
			cardType: GetType(),
			cardArt: Art,
			actualDeck: ModEntry.Instance.ApologiesDeck
		);
		card.AddLocalisation(I18n.ApologyCardName);
		registry.RegisterCard(card);
	}

	public override double GetApologyWeight(State state, Combat combat, int timesGiven)
		=> base.GetApologyWeight(state, combat, timesGiven) * 0.5;

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [new AStatus { targetPlayer = true, status = Status.energyNextTurn, statusAmount = 2 }],
			Upgrade.A => [new AEnergy { changeAmount = 2 }],
			_ => [new AEnergy { changeAmount = 1 }],
		};
}