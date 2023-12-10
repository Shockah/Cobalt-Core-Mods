using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using System.Collections.Generic;

namespace Shockah.Soggins;

[CardMeta(rarity = Rarity.common, upgradesTo = new Upgrade[] { Upgrade.A, Upgrade.B })]
public sealed class PressingButtonsCard : Card, IRegisterableCard
{
	public void RegisterCard(ICardRegistry registry)
	{
		ExternalCard card = new(
			globalName: $"{GetType().Namespace}.Card.PressingButtons",
			cardType: GetType(),
			cardArt: ModEntry.Instance.SogginsDeckBorder,
			actualDeck: ModEntry.Instance.SogginsDeck
		);
		card.AddLocalisation(I18n.PressingButtonsCardName);
		registry.RegisterCard(card);
	}

	public override CardData GetData(State state)
	{
		var data = base.GetData(state);
		data.art = StableSpr.cards_Panic;
		data.cost = 1;
		return data;
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => new()
			{
				new AAttack
				{
					damage = GetDmg(s, 1)
				},
				new AMove
				{
					dir = 3,
					targetPlayer = true,
					isRandom = true
				},
				new AAttack
				{
					damage = GetDmg(s, 2)
				}
			},
			Upgrade.B => new()
			{
				new AAttack
				{
					damage = GetDmg(s, 1)
				},
				new AMove
				{
					dir = 2,
					targetPlayer = true,
					isRandom = true
				},
				new AAttack
				{
					damage = GetDmg(s, 1)
				},
				new AMove
				{
					dir = 2,
					targetPlayer = true,
					isRandom = true
				},
				new AAttack
				{
					damage = GetDmg(s, 1)
				}
			},
			_ => new()
			{
				new AAttack
				{
					damage = GetDmg(s, 1)
				},
				new AMove
				{
					dir = 2,
					targetPlayer = true,
					isRandom = true
				},
				new AAttack
				{
					damage = GetDmg(s, 1)
				}
			}
		};
}
