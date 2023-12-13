using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using System.Collections.Generic;

namespace Shockah.Soggins;

[CardMeta(rarity = Rarity.uncommon, upgradesTo = new Upgrade[] { Upgrade.A, Upgrade.B })]
public sealed class BegForMercyCard : Card, IRegisterableCard, IFrogproofCard
{
	private static ModEntry Instance => ModEntry.Instance;

	public void RegisterCard(ICardRegistry registry)
	{
		ExternalCard card = new(
			globalName: $"{GetType().Namespace}.Card.BegForMercy",
			cardType: GetType(),
			cardArt: ModEntry.Instance.SogginsDeckBorder,
			actualDeck: ModEntry.Instance.SogginsDeck
		);
		card.AddLocalisation(I18n.BegForMercyCardName);
		registry.RegisterCard(card);
	}

	private int GetRequiredSmug()
		=> upgrade switch
		{
			Upgrade.A => -1,
			Upgrade.B => 1,
			_ => -2,
		};

	public override CardData GetData(State state)
	{
		var data = base.GetData(state);
		data.art = StableSpr.cards_Repairs;
		data.cost = 1;
		data.exhaust = upgrade == Upgrade.B;
		return data;
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> new()
		{
			Instance.KokoroApi.MakeConditionalAction(
				Instance.KokoroApi.MakeConditionalActionEquation(
					Instance.KokoroApi.MakeConditionalActionStatusExpression((Status)Instance.SmugStatus.Id!.Value),
					ConditionalActionEquationOperator.LessThan,
					Instance.KokoroApi.MakeConditionalActionIntConstant(GetRequiredSmug())
				),
				new AHeal
				{
					healAmount = 1,
					targetPlayer = true
				}
			)
		};
}
