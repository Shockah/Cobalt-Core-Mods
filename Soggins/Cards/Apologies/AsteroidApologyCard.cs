using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using System.Collections.Generic;

namespace Shockah.Soggins;

[CardMeta(rarity = Rarity.common, upgradesTo = [Upgrade.A, Upgrade.B])]
public sealed class AsteroidApologyCard : ApologyCard, IRegisterableCard
{
	public void RegisterCard(ICardRegistry registry)
	{
		ExternalCard card = new(
			globalName: $"{GetType().Namespace}.Card.Apology.Asteroid",
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
				new ASpawn { thing = new Asteroid { yAnimation = 0.0 } },
				new ASpawn { thing = new Asteroid { yAnimation = 0.0 } },
			],
			Upgrade.A => [
				new ASpawn { thing = new Asteroid { yAnimation = 0.0, bubbleShield = true } },
			],
			_ => [
				new ASpawn { thing = new Asteroid { yAnimation = 0.0 } },
			],
		};
}