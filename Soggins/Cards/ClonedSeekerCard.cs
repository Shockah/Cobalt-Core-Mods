using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using Shockah.Shared;
using System.Collections.Generic;
using System.IO;

namespace Shockah.Soggins;

[CardMeta(rarity = Rarity.rare, upgradesTo = new Upgrade[] { Upgrade.A, Upgrade.B })]
public sealed class ClonedSeekerCard : Card, IRegisterableCard
{
	private static ModEntry Instance => ModEntry.Instance;

	private static ExternalSprite Art = null!;

	public void RegisterArt(ISpriteRegistry registry)
	{
		Art = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.CardArt.Seeker",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "CardArt", "Seeker.png"))
		);
	}

	public void RegisterCard(ICardRegistry registry)
	{
		ExternalCard card = new(
			globalName: $"{GetType().Namespace}.Card.Seeker",
			cardType: GetType(),
			cardArt: Art,
			actualDeck: ModEntry.Instance.SogginsDeck
		);
		card.AddLocalisation(I18n.SeekerCardName);
		registry.RegisterCard(card);
	}

	public override CardData GetData(State state)
	{
		var data = base.GetData(state);
		data.cost = upgrade == Upgrade.B ? 2 : 1;
		return data;
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => new()
			{
				new AMove
				{
					dir = -3,
					targetPlayer = true
				},
				new ASpawn
				{
					thing = new Missile
					{
						yAnimation = 0.0,
						missileType = MissileType.seeker,
					}
				},
				new ADummyAction(),
				new ADummyAction()
			},
			Upgrade.B => new()
			{
				new ASpawn
				{
					thing = new Missile
					{
						yAnimation = 0.0,
						missileType = MissileType.seeker,
					}
				},
				new AMove
				{
					dir = -3,
					targetPlayer = true
				},
				new ASpawn
				{
					thing = new Missile
					{
						yAnimation = 0.0,
						missileType = MissileType.seeker,
					}
				},
				new ADummyAction()
			},
			_ => new()
			{
				new ASpawn
				{
					thing = new Missile
					{
						yAnimation = 0.0,
						missileType = MissileType.seeker,
					}
				},
				new ADummyAction(),
				new ADummyAction()
			}
		};
}
