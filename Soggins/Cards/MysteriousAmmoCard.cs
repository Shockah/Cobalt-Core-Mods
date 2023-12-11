using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using System.Collections.Generic;

namespace Shockah.Soggins;

[CardMeta(rarity = Rarity.common, upgradesTo = new Upgrade[] { Upgrade.A, Upgrade.B })]
public sealed class MysteriousAmmoCard : Card, IRegisterableCard, IFrogproofCard
{
	public void RegisterCard(ICardRegistry registry)
	{
		ExternalCard card = new(
			globalName: $"{GetType().Namespace}.Card.MysteriousAmmo",
			cardType: GetType(),
			cardArt: ModEntry.Instance.SogginsDeckBorder,
			actualDeck: ModEntry.Instance.SogginsDeck
		);
		card.AddLocalisation(I18n.MysteriousAmmoCardName);
		registry.RegisterCard(card);
	}

	private StuffBase GetPayload()
		=> upgrade switch
		{
			Upgrade.A => new Missile
			{
				yAnimation = 0.0,
				missileType = MissileType.heavy
			},
			Upgrade.B => new Missile
			{
				yAnimation = 0.0,
				missileType = MissileType.seeker
			},
			_ => new Missile
			{
				yAnimation = 0.0,
				missileType = MissileType.normal
			},
		};

	public override CardData GetData(State state)
	{
		var data = base.GetData(state);
		data.art = StableSpr.cards_SeekerMissileCard;
		data.cost = 1;
		return data;
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> new()
		{
			new ASpawn
			{
				thing = GetPayload()
			},
			new AStatus
			{
				status = Status.backwardsMissiles,
				statusAmount = 2,
				targetPlayer = true
			}
		};
}
