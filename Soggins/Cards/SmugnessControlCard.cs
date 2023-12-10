using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using System.Collections.Generic;

namespace Shockah.Soggins;

[CardMeta(rarity = Rarity.common, upgradesTo = new Upgrade[] { Upgrade.A, Upgrade.B })]
public sealed class SmugnessControlCard : Card, IRegisterableCard, IFrogproofCard
{
	private static ModEntry Instance => ModEntry.Instance;

	public void RegisterCard(ICardRegistry registry)
	{
		ExternalCard card = new(
			globalName: $"{GetType().Namespace}.Card.SmugnessControl",
			cardType: GetType(),
			cardArt: ModEntry.Instance.SogginsDeckBorder,
			actualDeck: ModEntry.Instance.SogginsDeck
		);
		card.AddLocalisation(I18n.SmugnessControlCardName);
		registry.RegisterCard(card);
	}

	private int GetCost()
		=> upgrade switch
		{
			Upgrade.A => 1,
			Upgrade.B => 0,
			_ => 1,
		};

	private int GetTopSmug()
		=> upgrade switch
		{
			Upgrade.A => 3,
			Upgrade.B => 1,
			_ => 2,
		};

	private int GetBottomSmug()
		=> -GetTopSmug();

	private int GetTempShield()
		=> upgrade switch
		{
			Upgrade.A => 3,
			Upgrade.B => 1,
			_ => 2,
		};

	public override CardData GetData(State state)
	{
		var data = base.GetData(state);
		data.art = flipped ? StableSpr.cards_Adaptability_Bottom : StableSpr.cards_Adaptability_Top;
		data.cost = GetCost();
		data.floppable = true;
		return data;
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> new()
		{
			new AStatus
			{
				status = (Status)Instance.SmugStatus.Id!.Value,
				statusAmount = GetTopSmug(),
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
			new ADummyAction(),
			new AStatus
			{
				status = (Status)Instance.SmugStatus.Id!.Value,
				statusAmount = GetBottomSmug(),
				targetPlayer = true,
				disabled = !flipped
			},
			new AStatus
			{
				status = Status.tempShield,
				statusAmount = GetTempShield(),
				targetPlayer = true,
				disabled = !flipped
			}
		};
}
