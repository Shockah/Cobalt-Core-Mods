using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using Shockah.Shared;
using System.Collections.Generic;
using System.IO;

namespace Shockah.Soggins;

[CardMeta(rarity = Rarity.uncommon, upgradesTo = new Upgrade[] { Upgrade.A, Upgrade.B })]
public sealed class HumiliatingAttackCard : Card, IRegisterableCard, IFrogproofCard
{
	private static ModEntry Instance => ModEntry.Instance;

	private static ExternalSprite Art = null!;

	public void RegisterArt(ISpriteRegistry registry)
	{
		Art = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.CardArt.HumiliatingAttack",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "CardArt", "HumiliatingAttack.png"))
		);
	}

	public void RegisterCard(ICardRegistry registry)
	{
		ExternalCard card = new(
			globalName: $"{GetType().Namespace}.Card.HumiliatingAttack",
			cardType: GetType(),
			cardArt: Art,
			actualDeck: ModEntry.Instance.SogginsDeck
		);
		card.AddLocalisation(I18n.HumiliatingAttackCardName);
		registry.RegisterCard(card);
	}

	public override CardData GetData(State state)
	{
		var data = base.GetData(state);
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
						ConditionalActionEquationOperator.LessThanOrEqual,
						Instance.KokoroApi.MakeConditionalActionIntConstant(-2),
						hideOperator: -2 == Instance.Api.GetMinSmug(s.ship)
					),
					new AAttack
					{
						damage = GetDmg(s, 3)
					}
				),
				Instance.KokoroApi.MakeConditionalAction(
					Instance.KokoroApi.MakeConditionalActionEquation(
						Instance.KokoroApi.MakeConditionalActionStatusExpression((Status)Instance.SmugStatus.Id!.Value),
						ConditionalActionEquationOperator.LessThanOrEqual,
						Instance.KokoroApi.MakeConditionalActionIntConstant(-3),
						hideOperator: -3 == Instance.Api.GetMinSmug(s.ship)
					),
					new AAttack
					{
						damage = GetDmg(s, 2)
					}
				),
				new ADummyAction(),
				new ADummyAction()
			},
			_ => new()
			{
				Instance.KokoroApi.MakeConditionalAction(
					Instance.KokoroApi.MakeConditionalActionEquation(
						Instance.KokoroApi.MakeConditionalActionStatusExpression((Status)Instance.SmugStatus.Id!.Value),
						ConditionalActionEquationOperator.LessThanOrEqual,
						Instance.KokoroApi.MakeConditionalActionIntConstant(-3),
						hideOperator: -3 == Instance.Api.GetMinSmug(s.ship)
					),
					new AAttack
					{
						damage = GetDmg(s, 5)
					}
				),
				new ADummyAction(),
				new ADummyAction()
			}
		};
}
