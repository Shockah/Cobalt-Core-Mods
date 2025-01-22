using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using System.Collections.Generic;

namespace Shockah.Soggins;

[CardMeta(rarity = Rarity.common, upgradesTo = [Upgrade.A, Upgrade.B])]
public sealed class EvadeApologyCard : ApologyCard, IRegisterableCard
{
	public void RegisterCard(ICardRegistry registry)
	{
		ExternalCard card = new(
			globalName: $"{GetType().Namespace}.Card.Apology.Evade",
			cardType: GetType(),
			cardArt: Art,
			actualDeck: ModEntry.Instance.ApologiesDeck
		);
		card.AddLocalisation(I18n.ApologyCardName);
		registry.RegisterCard(card);
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 3 },
				new AStatus { targetPlayer = true, status = Status.engineStall, statusAmount = 2 },
			],
			Upgrade.A => [
				new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 2 },
			],
			_ => [
				new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 1 },
			],
		};
}