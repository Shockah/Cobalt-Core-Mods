using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using System.Collections.Generic;

namespace Shockah.Soggins;

[CardMeta(rarity = Rarity.common, upgradesTo = new Upgrade[] { Upgrade.A, Upgrade.B })]
public sealed class StopItCard : Card, IRegisterableCard, IFrogproofCard
{
	private static ModEntry Instance => ModEntry.Instance;

	public void RegisterCard(ICardRegistry registry)
	{
		ExternalCard card = new(
			globalName: $"{GetType().Namespace}.Card.StopIt",
			cardType: GetType(),
			cardArt: ModEntry.Instance.SogginsDeckBorder,
			actualDeck: ModEntry.Instance.SogginsDeck
		);
		card.AddLocalisation(I18n.StopItCardName);
		registry.RegisterCard(card);
	}

	private int GetFrogproofing()
		=> upgrade switch
		{
			Upgrade.A => 3,
			Upgrade.B => 5,
			_ => 3,
		};

	private int GetShield()
		=> upgrade switch
		{
			Upgrade.A => 2,
			Upgrade.B => 1,
			_ => 1,
		};

	public override CardData GetData(State state)
	{
		var data = base.GetData(state);
		data.art = StableSpr.cards_Shield;
		data.cost = 1;
		return data;
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> new()
		{
			new AStatus
			{
				status = (Status)Instance.FrogproofingStatus.Id!.Value,
				statusAmount = GetFrogproofing(),
				targetPlayer = true
			},
			new AStatus
			{
				status = Status.shield,
				statusAmount = GetShield(),
				targetPlayer = true
			}
		};
}
