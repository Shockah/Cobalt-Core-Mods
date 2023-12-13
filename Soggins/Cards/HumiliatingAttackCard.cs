using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using System.Collections.Generic;

namespace Shockah.Soggins;

[CardMeta(rarity = Rarity.uncommon, upgradesTo = new Upgrade[] { Upgrade.A, Upgrade.B })]
public sealed class HumiliatingAttackCard : Card, IRegisterableCard, IFrogproofCard
{
	private static ModEntry Instance => ModEntry.Instance;

	public void RegisterCard(ICardRegistry registry)
	{
		ExternalCard card = new(
			globalName: $"{GetType().Namespace}.Card.HumiliatingAttack",
			cardType: GetType(),
			cardArt: ModEntry.Instance.SogginsDeckBorder,
			actualDeck: ModEntry.Instance.SogginsDeck
		);
		card.AddLocalisation(I18n.HumiliatingAttackCardName);
		registry.RegisterCard(card);
	}

	public override CardData GetData(State state)
	{
		var data = base.GetData(state);
		data.art = StableSpr.cards_FumeCannon;
		data.cost = 2;
		data.retain = upgrade == Upgrade.B;
		return data;
	}

	public bool IsFrogproof(State state, Combat? combat)
		=> upgrade != Upgrade.B;

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => new()
			{
				Instance.KokoroApi.MakeConditionalAction(
					Instance.KokoroApi.MakeConditionalActionEquation(
						Instance.KokoroApi.MakeConditionalActionStatusExpression((Status)Instance.SmugStatus.Id!.Value),
						ConditionalActionEquationOperator.LessThan,
						Instance.KokoroApi.MakeConditionalActionIntConstant(-1)
					),
					new AAttack
					{
						damage = GetDmg(s, 3)
					}
				),
				Instance.KokoroApi.MakeConditionalAction(
					Instance.KokoroApi.MakeConditionalActionEquation(
						Instance.KokoroApi.MakeConditionalActionStatusExpression((Status)Instance.SmugStatus.Id!.Value),
						ConditionalActionEquationOperator.LessThan,
						Instance.KokoroApi.MakeConditionalActionIntConstant(-2)
					),
					new AAttack
					{
						damage = GetDmg(s, 2)
					}
				)
			},
			_ => new()
			{
				Instance.KokoroApi.MakeConditionalAction(
					Instance.KokoroApi.MakeConditionalActionEquation(
						Instance.KokoroApi.MakeConditionalActionStatusExpression((Status)Instance.SmugStatus.Id!.Value),
						ConditionalActionEquationOperator.LessThan,
						Instance.KokoroApi.MakeConditionalActionIntConstant(-2)
					),
					new AAttack
					{
						damage = GetDmg(s, 5)
					}
				)
			}
		};
}
