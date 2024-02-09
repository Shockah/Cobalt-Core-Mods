using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using Shockah.Shared;
using System.Collections.Generic;
using System.IO;

namespace Shockah.Soggins;

[CardMeta(rarity = Rarity.uncommon, upgradesTo = [Upgrade.A, Upgrade.B])]
public sealed class BlastFromThePastCard : Card, IRegisterableCard, IFrogproofCard
{
	private static ModEntry Instance => ModEntry.Instance;

	private static ExternalSprite Art = null!;

	public void RegisterArt(ISpriteRegistry registry)
	{
		Art = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.CardArt.BlastFromThePast",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "CardArt", "BlastFromThePast.png"))
		);
	}

	public void RegisterCard(ICardRegistry registry)
	{
		ExternalCard card = new(
			globalName: $"{GetType().Namespace}.Card.BlastFromThePast",
			cardType: GetType(),
			cardArt: Art,
			actualDeck: ModEntry.Instance.SogginsDeck
		);
		card.AddLocalisation(I18n.BlastFromThePastCardName);
		registry.RegisterCard(card);
	}

	private int GetCost()
		=> upgrade switch
		{
			Upgrade.A => 4,
			Upgrade.B => 1,
			_ => 2,
		};

	public override CardData GetData(State state)
	{
		var data = base.GetData(state);
		data.cost = GetCost();
		data.exhaust = upgrade != Upgrade.None;
		return data;
	}

	public bool IsFrogproof(State state, Combat? combat)
		=> upgrade != Upgrade.B;

	public override List<CardAction> GetActions(State s, Combat c)
	{
		bool backwards = upgrade != Upgrade.A;
		List<CardAction> actions = new();

		if (upgrade == Upgrade.None)
			actions.Add(Instance.KokoroApi.ConditionalActions.Make(
				Instance.KokoroApi.ConditionalActions.Equation(
					Instance.KokoroApi.ConditionalActions.Status((Status)Instance.SmugStatus.Id!.Value),
					Instance.Api.GetMinSmug(s.ship) == -3 ? IKokoroApi.IConditionalActionApi.EquationOperator.Equal : IKokoroApi.IConditionalActionApi.EquationOperator.LessThanOrEqual,
					Instance.KokoroApi.ConditionalActions.Constant(-3),
					IKokoroApi.IConditionalActionApi.EquationStyle.State,
					hideOperator: Instance.Api.GetMinSmug(s.ship) == -3
				),
				new AStatus
				{
					status = Status.backwardsMissiles,
					statusAmount = 1,
					targetPlayer = true
				}
			));

		actions.Add(new ASpawn
		{
			thing = new Missile
			{
				yAnimation = 0.0,
				missileType = MissileType.normal,
				targetPlayer = backwards
			},
			offset = -1
		});
		actions.Add(new ASpawn
		{
			thing = new Missile
			{
				yAnimation = 0.0,
				missileType = MissileType.corrode,
				targetPlayer = backwards
			}
		});
		actions.Add(new ASpawn
		{
			thing = new Missile
			{
				yAnimation = 0.0,
				missileType = MissileType.seeker,
				targetPlayer = backwards
			},
			offset = 1
		});

		return actions;
	}
}
