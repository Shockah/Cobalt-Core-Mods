using System.Collections.Generic;
using System.IO;
using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using Shockah.Shared;

namespace Shockah.Soggins;

[CardMeta(rarity = Rarity.uncommon, upgradesTo = [Upgrade.A, Upgrade.B])]
public sealed class BlastFromThePastCard : Card, IRegisterableCard
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

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.A => new() { cost = 3 },
			Upgrade.B => new() { cost = 1, exhaust = true },
			_ => new() { cost = 3, exhaust = true },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new ASpawn { thing = new Missile { yAnimation = 0, missileType = MissileType.normal, targetPlayer = upgrade == Upgrade.B }, offset = -1 },
			new ASpawn { thing = new Missile { yAnimation = 0, missileType = MissileType.corrode, targetPlayer = upgrade == Upgrade.B } },
			new ASpawn { thing = new Missile { yAnimation = 0, missileType = MissileType.seeker, targetPlayer = upgrade == Upgrade.B }, offset = 1 },
		];
}
