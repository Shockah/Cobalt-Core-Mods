using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using System.Collections.Generic;

namespace Shockah.Soggins;

[CardMeta(rarity = Rarity.common, upgradesTo = new Upgrade[] { Upgrade.A, Upgrade.B })]
public sealed class TakeCoverCard : Card, IRegisterableCard
{
	public void RegisterCard(ICardRegistry registry)
	{
		ExternalCard card = new(
			globalName: $"{GetType().Namespace}.Card.TakeCover",
			cardType: GetType(),
			cardArt: ModEntry.Instance.SogginsDeckBorder,
			actualDeck: ModEntry.Instance.SogginsDeck
		);
		card.AddLocalisation(I18n.TakeCoverCardName);
		registry.RegisterCard(card);
	}

	private int GetCost()
		=> upgrade switch
		{
			Upgrade.A => 1,
			Upgrade.B => 0,
			_ => 1,
		};

	private int GetShield()
		=> upgrade switch
		{
			Upgrade.A => 1,
			Upgrade.B => 0,
			_ => 0,
		};

	private int GetTempShield()
		=> upgrade switch
		{
			Upgrade.A => 3,
			Upgrade.B => 2,
			_ => 3,
		};

	public override CardData GetData(State state)
	{
		var data = base.GetData(state);
		data.art = StableSpr.cards_Shield;
		data.cost = GetCost();
		return data;
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> new()
		{
			new AStatus
			{
				status = Status.shield,
				mode = AStatusMode.Set,
				statusAmount = GetShield(),
				targetPlayer = true,
				disabled = flipped
			},
			new AStatus
			{
				status = Status.tempShield,
				statusAmount = GetTempShield(),
				targetPlayer = true,
				disabled = flipped
			},
		};
}
