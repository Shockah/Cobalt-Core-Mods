using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using Shockah.Shared;
using System.Collections.Generic;
using System.IO;

namespace Shockah.Soggins;

[CardMeta(rarity = Rarity.common, upgradesTo = [Upgrade.A, Upgrade.B])]
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
			Upgrade.A => [
				Instance.KokoroApi.ConditionalActions.Make(
					Instance.KokoroApi.ConditionalActions.Equation(
						Instance.KokoroApi.ConditionalActions.Status((Status)Instance.SmugStatus.Id!.Value),
						Instance.Api.GetMinSmug(s.ship) == -2 ? IKokoroApi.IConditionalActionApi.EquationOperator.Equal : IKokoroApi.IConditionalActionApi.EquationOperator.LessThanOrEqual,
						Instance.KokoroApi.ConditionalActions.Constant(-2),
						IKokoroApi.IConditionalActionApi.EquationStyle.State,
						hideOperator: Instance.Api.GetMinSmug(s.ship) == -2
					),
					new AAttack
					{
						damage = GetDmg(s, 3)
					}
				),
				Instance.KokoroApi.ConditionalActions.Make(
					Instance.KokoroApi.ConditionalActions.Equation(
						Instance.KokoroApi.ConditionalActions.Status((Status)Instance.SmugStatus.Id!.Value),
						Instance.Api.GetMinSmug(s.ship) == -3 ? IKokoroApi.IConditionalActionApi.EquationOperator.Equal : IKokoroApi.IConditionalActionApi.EquationOperator.LessThanOrEqual,
						Instance.KokoroApi.ConditionalActions.Constant(-3),
						IKokoroApi.IConditionalActionApi.EquationStyle.State,
						hideOperator: Instance.Api.GetMinSmug(s.ship) == -3
					),
					new AAttack
					{
						damage = GetDmg(s, 2)
					}
				),
				new ADummyAction(),
				new ADummyAction()
			],
			_ => [
				Instance.KokoroApi.ConditionalActions.Make(
					Instance.KokoroApi.ConditionalActions.Equation(
						Instance.KokoroApi.ConditionalActions.Status((Status)Instance.SmugStatus.Id!.Value),
						Instance.Api.GetMinSmug(s.ship) == -3 ? IKokoroApi.IConditionalActionApi.EquationOperator.Equal : IKokoroApi.IConditionalActionApi.EquationOperator.LessThanOrEqual,
						Instance.KokoroApi.ConditionalActions.Constant(-3),
						IKokoroApi.IConditionalActionApi.EquationStyle.State,
						hideOperator: Instance.Api.GetMinSmug(s.ship) == -3
					),
					new AAttack
					{
						damage = GetDmg(s, 5)
					}
				),
				new ADummyAction(),
				new ADummyAction()
			]
		};
}
