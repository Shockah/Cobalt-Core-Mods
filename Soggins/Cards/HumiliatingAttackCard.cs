using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using Shockah.Kokoro;
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
		data.cost = 1;
		data.retain = upgrade == Upgrade.B;
		return data;
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				Instance.KokoroApi.Conditional.MakeAction(
					Instance.KokoroApi.Conditional.Equation(
						Instance.KokoroApi.Conditional.Status((Status)Instance.SmugStatus.Id!.Value),
						Instance.Api.GetMinSmug(s.ship) == -2 ? IKokoroApi.IV2.IConditionalApi.EquationOperator.Equal : IKokoroApi.IV2.IConditionalApi.EquationOperator.LessThanOrEqual,
						Instance.KokoroApi.Conditional.Constant(-2),
						IKokoroApi.IV2.IConditionalApi.EquationStyle.State
					).SetShowOperator(Instance.Api.GetMinSmug(s.ship) != -2),
					new AAttack { damage = GetDmg(s, 3) }
				).AsCardAction,
				Instance.KokoroApi.Conditional.MakeAction(
					Instance.KokoroApi.Conditional.Equation(
						Instance.KokoroApi.Conditional.Status((Status)Instance.SmugStatus.Id!.Value),
						Instance.Api.GetMinSmug(s.ship) == -3 ? IKokoroApi.IV2.IConditionalApi.EquationOperator.Equal : IKokoroApi.IV2.IConditionalApi.EquationOperator.LessThanOrEqual,
						Instance.KokoroApi.Conditional.Constant(-3),
						IKokoroApi.IV2.IConditionalApi.EquationStyle.State
					).SetShowOperator(Instance.Api.GetMinSmug(s.ship) != -3),
					new AAttack { damage = GetDmg(s, 2) }
				).AsCardAction,
				new ADummyAction(),
				new ADummyAction()
			],
			_ => [
				Instance.KokoroApi.Conditional.MakeAction(
					Instance.KokoroApi.Conditional.Equation(
						Instance.KokoroApi.Conditional.Status((Status)Instance.SmugStatus.Id!.Value),
						Instance.Api.GetMinSmug(s.ship) == -3 ? IKokoroApi.IV2.IConditionalApi.EquationOperator.Equal : IKokoroApi.IV2.IConditionalApi.EquationOperator.LessThanOrEqual,
						Instance.KokoroApi.Conditional.Constant(-3),
						IKokoroApi.IV2.IConditionalApi.EquationStyle.State
					).SetShowOperator(Instance.Api.GetMinSmug(s.ship) != -3),
					new AAttack { damage = GetDmg(s, 5) }
				).AsCardAction,
				new ADummyAction(),
				new ADummyAction()
			]
		};
}
