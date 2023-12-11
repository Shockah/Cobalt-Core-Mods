using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using System.Collections.Generic;

namespace Shockah.Soggins;

[CardMeta(rarity = Rarity.rare, upgradesTo = new Upgrade[] { Upgrade.A, Upgrade.B })]
public sealed class ExtraApologyCard : Card, IRegisterableCard
{
	private static ModEntry Instance => ModEntry.Instance;

	public void RegisterCard(ICardRegistry registry)
	{
		ExternalCard card = new(
			globalName: $"{GetType().Namespace}.Card.ExtraApology",
			cardType: GetType(),
			cardArt: ModEntry.Instance.SogginsDeckBorder,
			actualDeck: ModEntry.Instance.SogginsDeck
		);
		card.AddLocalisation(I18n.ExtraApologyCardName);
		registry.RegisterCard(card);
	}

	private int GetCost()
		=> upgrade switch
		{
			Upgrade.A => 1,
			Upgrade.B => 2,
			_ => 2
		};

	private Status GetStatus()
		=> upgrade switch
		{
			Upgrade.A => (Status)Instance.ExtraApologiesStatus.Id!.Value,
			Upgrade.B => (Status)Instance.ConstantApologiesStatus.Id!.Value,
			_ => (Status)Instance.ExtraApologiesStatus.Id!.Value
		};

	public override CardData GetData(State state)
	{
		var data = base.GetData(state);
		data.art = StableSpr.cards_Serenity;
		data.cost = GetCost();
		data.exhaust = true;
		return data;
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> new()
		{
			new AStatus
			{
				status = GetStatus(),
				statusAmount = 1,
				targetPlayer = true
			}
		};
}
