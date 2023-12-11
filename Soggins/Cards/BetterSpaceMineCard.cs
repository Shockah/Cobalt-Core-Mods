using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using System.Collections.Generic;

namespace Shockah.Soggins;

[CardMeta(rarity = Rarity.common, upgradesTo = new Upgrade[] { Upgrade.A, Upgrade.B })]
public sealed class BetterSpaceMineCard : Card, IRegisterableCard
{
	public void RegisterCard(ICardRegistry registry)
	{
		ExternalCard card = new(
			globalName: $"{GetType().Namespace}.Card.BetterSpaceMine",
			cardType: GetType(),
			cardArt: ModEntry.Instance.SogginsDeckBorder,
			actualDeck: ModEntry.Instance.SogginsDeck
		);
		card.AddLocalisation(I18n.BetterSpaceMineCardName);
		registry.RegisterCard(card);
	}

	public override CardData GetData(State state)
	{
		var data = base.GetData(state);
		data.art = StableSpr.cards_goat;
		data.cost = 2;
		return data;
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => new()
			{
				new ASpawn
				{
					thing = new SpaceMine
					{
						yAnimation = 0.0,
						bigMine = true
					}
				},
				new ASpawn
				{
					thing = new SpaceMine
					{
						yAnimation = 0.0
					},
					offset = 1
				}
			},
			Upgrade.B => new()
			{
				new ASpawn
				{
					thing = new SpaceMine
					{
						yAnimation = 0.0
					}
				},
				new ASpawn
				{
					thing = new Asteroid
					{
						yAnimation = 0.0
					},
					offset = 1
				},
				new ASpawn
				{
					thing = new Asteroid
					{
						yAnimation = 0.0
					},
					offset = 2
				}
			},
			_ => new()
			{
				new ASpawn
				{
					thing = new SpaceMine
					{
						yAnimation = 0.0
					}
				},
				new ASpawn
				{
					thing = new SpaceMine
					{
						yAnimation = 0.0
					},
					offset = 1
				}
			}
		};
}
